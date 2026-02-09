using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Kernel_Narrath : ModKernel
    {
        public static Kernel_Narrath instance;
        public static God_Narrath narrath;

        // Fragment tracking: maps Person to their Fragment level (0-5)
        public Dictionary<Person, int> fragmentLevels = new Dictionary<Person, int>();

        // Compel flags: maps Person to remaining compelled turns
        public Dictionary<Person, int> compelledHeroes = new Dictionary<Person, int>();

        // Silenced heroes: maps Person to a flag that skips their next action
        public Dictionary<Person, bool> silencedHeroes = new Dictionary<Person, bool>();

        // Seeker lateral spread tracker: maps Person to turns spent at current location
        public Dictionary<Person, int> seekerProximityTurns = new Dictionary<Person, int>();

        // Ward of Silence: maps Location to the hero maintaining the ward
        public Dictionary<Location, Person> activeWards = new Dictionary<Location, Person>();

        // Burn the Writings suppression: maps Person to remaining suppression turns
        public Dictionary<Person, int> burnWritingsSuppression = new Dictionary<Person, int>();

        // Echo respawn timer
        public int echoRespawnTimer = -1;
        public bool echoAlive = false;

        // Amanuensis state
        public bool amanuensisSpawned = false;
        public bool amanuensisAlive = false;

        public override void beforeMapGen(Map map)
        {
            instance = this;
        }

        public override void afterMapGen(Map map)
        {
            // Register CommunityLib hooks
            foreach (ModKernel mk in map.mods)
            {
                if (mk is CommunityLib.ModCore communityCore)
                {
                    Hooks_Narrath hooks = new Hooks_Narrath(map);
                    hooks.RegisterHooks();
                    break;
                }
            }
        }

        public override void onTurnStart(Map map)
        {
            // Find our god instance
            if (narrath == null)
            {
                foreach (God g in map.overmind.gods)
                {
                    if (g is God_Narrath gn)
                    {
                        narrath = gn;
                        break;
                    }
                }
            }

            if (narrath == null) return;

            // Process silenced heroes: skip their action this turn
            List<Person> toUnsilence = new List<Person>();
            foreach (var kvp in silencedHeroes)
            {
                if (kvp.Value && kvp.Key.unit != null)
                {
                    // Mark for unsilence after this turn
                    toUnsilence.Add(kvp.Key);
                }
            }
            foreach (Person p in toUnsilence)
            {
                silencedHeroes.Remove(p);
            }

            // Ensure Q_InvestigateMystery quests exist at all Mystery locations
            foreach (Location loc in map.locations)
            {
                if (loc == null) continue;

                Property_Mystery mystery = null;
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery pm)
                    {
                        mystery = pm;
                        break;
                    }
                }

                if (mystery != null)
                {
                    // Check if quest already exists
                    bool hasQuest = false;
                    if (loc.quests != null)
                    {
                        foreach (var q in loc.quests)
                        {
                            if (q is Q_InvestigateMystery)
                            {
                                hasQuest = true;
                                break;
                            }
                        }
                    }

                    if (!hasQuest)
                    {
                        Q_InvestigateMystery quest = new Q_InvestigateMystery(loc, mystery);
                        loc.addQuest(quest);
                    }
                }
            }

            // Decrement compel timers
            List<Person> expiredCompels = new List<Person>();
            foreach (var kvp in compelledHeroes)
            {
                compelledHeroes[kvp.Key] = kvp.Value - 1;
                if (kvp.Value - 1 <= 0)
                {
                    expiredCompels.Add(kvp.Key);
                }
            }
            foreach (Person p in expiredCompels)
            {
                compelledHeroes.Remove(p);
            }

            // Decrement burn writings suppression
            List<Person> expiredBurn = new List<Person>();
            foreach (var kvp in burnWritingsSuppression)
            {
                burnWritingsSuppression[kvp.Key] = kvp.Value - 1;
                if (kvp.Value - 1 <= 0)
                {
                    expiredBurn.Add(kvp.Key);
                }
            }
            foreach (Person p in expiredBurn)
            {
                burnWritingsSuppression.Remove(p);
            }
        }

        public override void onTurnEnd(Map map)
        {
            if (narrath == null) return;

            ProcessFragmentEffects(map);
            ProcessEchoRespawn(map);
        }

        private void ProcessFragmentEffects(Map map)
        {
            // Clean up dead/removed persons
            List<Person> toRemove = new List<Person>();
            foreach (var kvp in fragmentLevels)
            {
                if (kvp.Key == null || kvp.Key.isDead)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (Person p in toRemove)
            {
                fragmentLevels.Remove(p);
            }

            // Process lateral spread for Fragment 2+ characters
            Dictionary<Person, int> spreadTargets = new Dictionary<Person, int>();
            foreach (var kvp in fragmentLevels)
            {
                Person person = kvp.Key;
                int level = kvp.Value;
                if (level < 2) continue;
                if (person.unit == null) continue;

                // Check if suppressed by Burn the Writings
                if (burnWritingsSuppression.ContainsKey(person)) continue;

                Location loc = person.unit.location;
                if (loc == null) continue;

                // Find all other characters at this location
                foreach (Unit u in loc.units)
                {
                    if (u == null || u.person == null || u.person == person) continue;

                    Person target = u.person;

                    // Chosen One immunity to ambient exposure
                    if (target.isChosenOne && level < 3) continue;

                    int targetFragment = GetFragmentLevel(target);
                    if (targetFragment >= level) continue; // Only spread to lower levels

                    // Calculate spread chance
                    double chance = 0;
                    if (level == 2) chance = 0.15;
                    else if (level >= 3)
                    {
                        chance = 0.25;
                        // Seekers get +5% per turn in same location
                        if (seekerProximityTurns.ContainsKey(person))
                        {
                            chance += 0.05 * seekerProximityTurns[person];
                            seekerProximityTurns[person]++;
                        }
                        else
                        {
                            seekerProximityTurns[person] = 1;
                        }
                    }

                    // Fragment 4: automatic spread, no chance roll
                    if (level >= 4)
                    {
                        if (!seekerProximityTurns.ContainsKey(person))
                            seekerProximityTurns[person] = 0;
                        seekerProximityTurns[person]++;

                        if (seekerProximityTurns[person] >= 10)
                        {
                            if (!spreadTargets.ContainsKey(target) || spreadTargets[target] < targetFragment + 1)
                                spreadTargets[target] = targetFragment + 1;
                            seekerProximityTurns[person] = 0;
                        }
                        continue;
                    }

                    if (Eleven.random.NextDouble() < chance)
                    {
                        if (!spreadTargets.ContainsKey(target) || spreadTargets[target] < targetFragment + 1)
                            spreadTargets[target] = targetFragment + 1;
                    }
                }
            }

            // Apply spread
            foreach (var kvp in spreadTargets)
            {
                SetFragmentLevel(kvp.Key, kvp.Value);
            }

            // Process Fragment 4 Silencing
            foreach (var kvp in fragmentLevels)
            {
                if (kvp.Value != 4) continue;
                Person person = kvp.Key;
                if (person.unit == null) continue;

                Location loc = person.unit.location;
                if (loc == null) continue;

                // ~1/30 chance per turn
                if (Eleven.random.NextDouble() < 1.0 / 30.0)
                {
                    foreach (Unit u in loc.units)
                    {
                        if (u == null || u.person == null || u.person == person) continue;
                        if (u.person.isHero())
                        {
                            silencedHeroes[u.person] = true;
                            IncrementFragment(u.person);
                            break;
                        }
                    }
                }
            }

            // Process Fragment 5 completions
            List<Person> completions = new List<Person>();
            foreach (var kvp in fragmentLevels)
            {
                if (kvp.Value >= 5)
                {
                    completions.Add(kvp.Key);
                }
            }

            foreach (Person person in completions)
            {
                ProcessFragmentCompletion(person, map);
            }
        }

        public void ProcessFragmentCompletion(Person person, Map map)
        {
            if (person == null) return;

            Location loc = person.unit?.location;
            if (loc == null && person.rulerOf >= 0 && person.rulerOf < map.locations.Length)
            {
                loc = map.locations[person.rulerOf];
            }

            // Spawn or advance Mystery at their location
            if (loc != null)
            {
                Property_Mystery existing = null;
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery pm)
                    {
                        existing = pm;
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.AdvanceStage();
                }
                else
                {
                    Property_Mystery newMystery = new Property_Mystery(loc, narrath);
                    newMystery.stage = 3;
                    loc.properties.Add(newMystery);
                }
            }

            // Grief spread: anyone who liked/loved this person gains +1 Fragment
            foreach (var kvp2 in person.relations)
            {
                if (kvp2.Value >= 50) // Liked/loved threshold
                {
                    Person griever = kvp2.Key;
                    if (griever != null && !griever.isDead)
                    {
                        // Chosen One is NOT immune to grief spread
                        IncrementFragment(griever);
                    }
                }
            }

            // Report seal advancement
            if (narrath != null)
            {
                narrath.addMysteryAdvancement();
                narrath.addCompletionScore();
            }

            // Remove the character (erasure)
            fragmentLevels.Remove(person);
            if (person.unit != null)
            {
                person.unit.die(map, "Completed the utterance. Where they stood, there is nothing.");
            }
        }

        private void ProcessEchoRespawn(Map map)
        {
            if (!echoAlive && echoRespawnTimer > 0)
            {
                echoRespawnTimer--;
                if (echoRespawnTimer <= 0 && narrath != null)
                {
                    narrath.SpawnEcho(map);
                }
            }
        }

        // Fragment management methods
        public int GetFragmentLevel(Person person)
        {
            if (person == null) return 0;
            if (fragmentLevels.TryGetValue(person, out int level))
                return level;
            return 0;
        }

        public void SetFragmentLevel(Person person, int level)
        {
            if (person == null) return;
            level = Math.Max(0, Math.Min(5, level));
            if (level == 0)
            {
                fragmentLevels.Remove(person);
            }
            else
            {
                fragmentLevels[person] = level;
            }
        }

        public void IncrementFragment(Person person)
        {
            if (person == null) return;
            int current = GetFragmentLevel(person);
            SetFragmentLevel(person, current + 1);
        }

        public bool IsSeeker(Person person)
        {
            return GetFragmentLevel(person) >= 3;
        }

        public bool IsCompelled(Person person)
        {
            return compelledHeroes.ContainsKey(person) && compelledHeroes[person] > 0;
        }

        // Stat modifications for Fragments
        public int GetStatModifier(Person person, string stat)
        {
            int level = GetFragmentLevel(person);
            if (level == 0) return 0;

            int penalty = -level; // -1 per fragment level

            // Fragment 3+: Lore and Command get a +2 bonus (net: level-2)
            if (level >= 3 && (stat == "lore" || stat == "command"))
            {
                penalty += 2;
            }

            // Fragment 4+: extra -2 to Intrigue and Might
            if (level >= 4 && (stat == "intrigue" || stat == "might"))
            {
                penalty -= 2;
            }

            return penalty;
        }

        // Save/Load support
        public override int[] onSave_ExtensionData(Map map)
        {
            List<int> data = new List<int>();

            // Save fragment data
            data.Add(fragmentLevels.Count);
            foreach (var kvp in fragmentLevels)
            {
                data.Add(map.personIndex(kvp.Key));
                data.Add(kvp.Value);
            }

            // Save seal advancement progress
            if (narrath != null)
            {
                data.Add(narrath.mysteryAdvancementCount);
            }
            else
            {
                data.Add(0);
            }

            // Save echo state
            data.Add(echoAlive ? 1 : 0);
            data.Add(echoRespawnTimer);

            // Save amanuensis state
            data.Add(amanuensisSpawned ? 1 : 0);
            data.Add(amanuensisAlive ? 1 : 0);

            return data.ToArray();
        }

        public override void onLoad_ExtensionData(Map map, int[] saveData)
        {
            instance = this;

            if (saveData == null || saveData.Length == 0) return;

            int index = 0;

            // Load fragment data
            int fragmentCount = saveData[index++];
            for (int i = 0; i < fragmentCount; i++)
            {
                int personIdx = saveData[index++];
                int level = saveData[index++];
                Person p = map.personByIndex(personIdx);
                if (p != null)
                {
                    fragmentLevels[p] = level;
                }
            }

            // Load seal advancement progress
            int advCount = saveData[index++];

            // Load echo state
            echoAlive = saveData[index++] == 1;
            echoRespawnTimer = saveData[index++];

            // Load amanuensis state
            amanuensisSpawned = saveData[index++] == 1;
            amanuensisAlive = saveData[index++] == 1;

            // Find our god and restore state
            foreach (God g in map.overmind.gods)
            {
                if (g is God_Narrath gn)
                {
                    narrath = gn;
                    narrath.mysteryAdvancementCount = advCount;
                    break;
                }
            }
        }

        // World panic integration
        public override void populatingWorldPanicReasons(List<ReasonMsg> reasons, Map map)
        {
            if (narrath == null) return;

            // Count Fragment 5 completions (tracked via score)
            if (narrath.completionCount > 0)
            {
                reasons.Add(new ReasonMsg("Inexplicable Disappearances", narrath.completionCount * 2.0));
            }

            // Count active Mysteries
            int mysteryCount = 0;
            foreach (Location loc in map.locations)
            {
                if (loc == null) continue;
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery)
                    {
                        mysteryCount++;
                        break;
                    }
                }
            }

            if (mysteryCount >= 5)
            {
                reasons.Add(new ReasonMsg("Spreading Mysteries", mysteryCount * 1.5));
            }
        }
    }
}
