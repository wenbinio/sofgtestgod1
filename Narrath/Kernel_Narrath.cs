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

        // Fragment spread chance constants
        public const double FRAGMENT_2_SPREAD_CHANCE = 0.15;
        public const double SEEKER_BASE_SPREAD_CHANCE = 0.25;
        public const double SEEKER_PROXIMITY_SPREAD_BONUS = 0.05;
        public const int FRAGMENT_4_AUTO_SPREAD_TURNS = 10;
        public const double FRAGMENT_4_SILENCE_CHANCE = 1.0 / 30.0;

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

        public override void onStartGamePresssed(Map map, List<God> gods)
        {
            gods.Add(new God_Narrath());
        }

        public override void afterMapGen(Map map)
        {
            // Only initialize if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

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

        public override void afterLoading(Map map)
        {
            base.afterLoading(map);

            // Only initialize if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

            // Re-register CommunityLib hooks after loading
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

        public override void afterMapGenAfterHistorical(Map map)
        {
            base.afterMapGenAfterHistorical(map);

            // Only run if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

            // Tutorial message for players
            map.world.prefabStore.popMsgHint(
                map.overmind.god.getName() + " is a god whose power grows through investigation, not confrontation. " +
                "Your Archivist agent can plant Mysteries at infiltrated locations. " +
                "Heroes will be compelled to investigate these Mysteries, believing them to be threats to resolve. " +
                "\n\nBut investigation does not destroy Mysteries — it advances them. As heroes study the incomplete utterance, " +
                "they gain Fragments of understanding, which slowly consume them. The more skilled the investigator, the faster " +
                "they fall. Your seals break not with time, but with each Mystery stage advancement." +
                "\n\nSpread Mysteries widely. Let the heroes do your work. Watch them destroy themselves in pursuit of understanding.",
                map.overmind.god.getName()
            );
        }

        public override void onCheatEntered(string command)
        {
            // Only run if Narrath is the active god
            if (map == null) { return; } // For tutorial situations
            if (map.overmind.god is God_Narrath == false) { return; }

            base.onCheatEntered(command);

            if (command == "mystery1")
            {
                // Spawn Stage 1 Mystery at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null && narrath != null)
                {
                    // Check if Mystery already exists
                    bool hasMyster = false;
                    foreach (Property pr in loc.properties)
                    {
                        if (pr is Property_Mystery)
                        {
                            hasMyster = true;
                            break;
                        }
                    }
                    if (!hasMyster)
                    {
                        Property_Mystery mystery = new Property_Mystery(loc, narrath);
                        mystery.stage = 1;
                        loc.properties.Add(mystery);
                    }
                }
            }
            else if (command == "mystery3")
            {
                // Spawn Stage 3 Mystery at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null && narrath != null)
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
                        existing.stage = 3;
                    }
                    else
                    {
                        Property_Mystery mystery = new Property_Mystery(loc, narrath);
                        mystery.stage = 3;
                        loc.properties.Add(mystery);
                    }
                }
            }
            else if (command == "fragment1")
            {
                // Grant Fragment 1 to selected unit
                if (GraphicalMap.selectedUnit?.person != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedUnit.person, 1);
                }
                else if (GraphicalMap.selectedHex?.location?.person() != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedHex.location.person(), 1);
                }
            }
            else if (command == "fragment3")
            {
                // Grant Fragment 3 (Seeker) to selected unit
                if (GraphicalMap.selectedUnit?.person != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedUnit.person, 3);
                }
                else if (GraphicalMap.selectedHex?.location?.person() != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedHex.location.person(), 3);
                }
            }
            else if (command == "fragment5")
            {
                // Grant Fragment 5 (Completion) to selected unit - will trigger erasure
                if (GraphicalMap.selectedUnit?.person != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedUnit.person, 5);
                }
                else if (GraphicalMap.selectedHex?.location?.person() != null)
                {
                    SetFragmentLevel(GraphicalMap.selectedHex.location.person(), 5);
                }
            }
            else if (command == "seals")
            {
                // Show current seal advancement progress
                if (narrath != null)
                {
                    string msg = "Mystery Advancement Count: " + narrath.mysteryAdvancementCount + "\n\n";
                    for (int i = 0; i < God_Narrath.SEAL_THRESHOLDS.Length; i++)
                    {
                        int threshold = God_Narrath.SEAL_THRESHOLDS[i];
                        bool broken = narrath.mysteryAdvancementCount >= threshold;
                        msg += "Seal " + (i + 1) + ": " + (broken ? "BROKEN" : "Sealed") + 
                               " (threshold: " + threshold + ")\n";
                    }
                    map.world.prefabStore.popMsg(msg);
                }
            }
            else if (command == "echo")
            {
                // Spawn The Echo at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null && narrath != null)
                {
                    if (!echoAlive)
                    {
                        UA_Echo echo = new UA_Echo(loc, map.overmind);
                        map.units.Add(echo);
                        loc.units.Add(echo);
                        echoAlive = true;
                        echoRespawnTimer = -1;
                    }
                }
            }
            else if (command == "amanuensis")
            {
                // Spawn The Amanuensis at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null && narrath != null)
                {
                    if (!amanuensisSpawned)
                    {
                        UA_Amanuensis amanuensis = new UA_Amanuensis(loc, map.overmind);
                        map.units.Add(amanuensis);
                        loc.units.Add(amanuensis);
                        amanuensisSpawned = true;
                        amanuensisAlive = true;
                    }
                }
            }
            else if (command == "mystery5")
            {
                // Spawn or advance to Stage 5 Mystery at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null && narrath != null)
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
                        existing.stage = 5;
                    }
                    else
                    {
                        Property_Mystery mystery = new Property_Mystery(loc, narrath);
                        mystery.stage = 5;
                        loc.properties.Add(mystery);
                    }
                }
            }
            else if (command == "advmystery")
            {
                // Advance existing Mystery by 1 stage at selected location (tests AdvanceStage logic)
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null)
                {
                    foreach (Property pr in loc.properties)
                    {
                        if (pr is Property_Mystery pm)
                        {
                            pm.AdvanceStage();
                            map.world.prefabStore.popMsg("Mystery advanced to Stage " + pm.stage);
                            break;
                        }
                    }
                }
            }
            else if (command == "addinvprog")
            {
                // Add 50 investigation progress to Mystery at selected location
                Location loc = GraphicalMap.selectedHex?.location;
                if (loc != null)
                {
                    foreach (Property pr in loc.properties)
                    {
                        if (pr is Property_Mystery pm)
                        {
                            pm.AddInvestigationProgress(50);
                            map.world.prefabStore.popMsg("Added 50 investigation progress. Total: " + pm.investigationProgress + ", Stage: " + pm.stage);
                            break;
                        }
                    }
                }
            }
            else if (command == "diagnose")
            {
                // Comprehensive state diagnostic
                string msg = "=== NARRATH DIAGNOSTICS ===\n\n";

                // Fragment tracking
                msg += "FRAGMENTS (" + fragmentLevels.Count + " tracked):\n";
                int[] fragmentCounts = new int[6];
                int deadFragments = 0;
                foreach (var kvp in fragmentLevels)
                {
                    if (kvp.Key == null || kvp.Key.isDead)
                    {
                        deadFragments++;
                        continue;
                    }
                    if (kvp.Value >= 0 && kvp.Value <= 5)
                        fragmentCounts[kvp.Value]++;
                }
                for (int i = 1; i <= 5; i++)
                    msg += "  Fragment " + i + ": " + fragmentCounts[i] + " characters\n";
                if (deadFragments > 0)
                    msg += "  WARNING: " + deadFragments + " dead/null entries (will be cleaned)\n";

                // Mysteries
                int[] mysteryCounts = new int[6];
                int totalMysteries = 0;
                foreach (Location loc in map.locations)
                {
                    if (loc == null) continue;
                    foreach (Property pr in loc.properties)
                    {
                        if (pr is Property_Mystery pm)
                        {
                            totalMysteries++;
                            if (pm.stage >= 1 && pm.stage <= 5)
                                mysteryCounts[pm.stage]++;
                            break;
                        }
                    }
                }
                msg += "\nMYSTERIES (" + totalMysteries + " total):\n";
                for (int i = 1; i <= 5; i++)
                    msg += "  Stage " + i + ": " + mysteryCounts[i] + "\n";

                // Seals
                if (narrath != null)
                {
                    msg += "\nSEALS:\n";
                    msg += "  Advancement count: " + narrath.mysteryAdvancementCount + "\n";
                    int brokenCount = 0;
                    for (int i = 0; i < God_Narrath.SEAL_THRESHOLDS.Length; i++)
                    {
                        if (narrath.mysteryAdvancementCount >= God_Narrath.SEAL_THRESHOLDS[i])
                            brokenCount++;
                    }
                    msg += "  Broken seals: " + brokenCount + "/9\n";
                    if (brokenCount < 9)
                        msg += "  Next seal at: " + God_Narrath.SEAL_THRESHOLDS[brokenCount] + "\n";
                }

                // Active effects
                msg += "\nACTIVE EFFECTS:\n";
                msg += "  Compelled heroes: " + compelledHeroes.Count + "\n";
                msg += "  Silenced heroes: " + silencedHeroes.Count + "\n";
                msg += "  Active wards: " + activeWards.Count + "\n";
                msg += "  Burn suppression: " + burnWritingsSuppression.Count + "\n";
                msg += "  Proximity tracking: " + seekerProximityTurns.Count + "\n";

                // Agent status
                msg += "\nAGENTS:\n";
                msg += "  Echo alive: " + echoAlive + "\n";
                if (!echoAlive && echoRespawnTimer > 0)
                    msg += "  Echo respawn in: " + echoRespawnTimer + " turns\n";
                msg += "  Amanuensis spawned: " + amanuensisSpawned + "\n";
                msg += "  Amanuensis alive: " + amanuensisAlive + "\n";

                // Apocalypse score
                if (narrath != null)
                {
                    msg += "\nSCORING:\n";
                    msg += "  Completions: " + narrath.completionCount + "\n";
                    msg += "  Apocalypse score: " + narrath.getApocalypseScore(map).ToString("F1") + "\n";
                }

                map.world.prefabStore.popMsg(msg);
            }
            else if (command == "testcascade")
            {
                // Test cascade: create 3 mysteries and 3 seekers to verify cascade mechanics
                if (narrath == null) return;
                int placed = 0;
                foreach (Location loc in map.locations)
                {
                    if (loc == null || loc.settlement == null) continue;
                    if (placed >= 3) break;

                    bool hasMystery = false;
                    foreach (Property pr in loc.properties)
                    {
                        if (pr is Property_Mystery) { hasMystery = true; break; }
                    }
                    if (!hasMystery)
                    {
                        Property_Mystery mystery = new Property_Mystery(loc, narrath);
                        mystery.stage = 3;
                        loc.properties.Add(mystery);
                        placed++;
                    }
                }

                int seekersMade = 0;
                foreach (Unit u in map.units)
                {
                    if (u == null || u.person == null) continue;
                    if (seekersMade >= 3) break;
                    if (u.person.isHero() && GetFragmentLevel(u.person) < 3)
                    {
                        SetFragmentLevel(u.person, 3);
                        seekersMade++;
                    }
                }

                map.world.prefabStore.popMsg("Test cascade setup: " + placed + " Stage 3 Mysteries, " + seekersMade + " Seekers created.\nUse 'diagnose' to monitor state changes over turns.");
            }
            else if (command == "breakseal")
            {
                // Add enough advancement to break the next seal
                if (narrath != null)
                {
                    int currentBroken = 0;
                    for (int i = 0; i < God_Narrath.SEAL_THRESHOLDS.Length; i++)
                    {
                        if (narrath.mysteryAdvancementCount >= God_Narrath.SEAL_THRESHOLDS[i])
                            currentBroken++;
                    }
                    if (currentBroken < God_Narrath.SEAL_THRESHOLDS.Length)
                    {
                        narrath.mysteryAdvancementCount = God_Narrath.SEAL_THRESHOLDS[currentBroken];
                        map.world.prefabStore.popMsg("Seal " + (currentBroken + 1) + " broken! Advancement set to " + narrath.mysteryAdvancementCount);
                    }
                    else
                    {
                        map.world.prefabStore.popMsg("All seals already broken.");
                    }
                }
            }
            else if (command == "narrath_help")
            {
                // Show all available cheat commands
                string msg = "=== NARRATH CHEAT COMMANDS ===\n\n";
                msg += "MYSTERIES:\n";
                msg += "  mystery1 - Place Stage 1 Mystery at selected hex\n";
                msg += "  mystery3 - Place/set Stage 3 Mystery at selected hex\n";
                msg += "  mystery5 - Place/set Stage 5 Mystery at selected hex\n";
                msg += "  advmystery - Advance Mystery by 1 stage at selected hex\n";
                msg += "  addinvprog - Add 50 investigation progress at selected hex\n";
                msg += "\nFRAGMENTS:\n";
                msg += "  fragment1 - Grant Fragment 1 to selected unit\n";
                msg += "  fragment3 - Grant Fragment 3 (Seeker) to selected unit\n";
                msg += "  fragment5 - Grant Fragment 5 (Completion) to selected unit\n";
                msg += "\nAGENTS:\n";
                msg += "  echo - Spawn The Echo at selected hex\n";
                msg += "  amanuensis - Spawn The Amanuensis at selected hex\n";
                msg += "\nSEALS & DIAGNOSTICS:\n";
                msg += "  seals - Show seal advancement status\n";
                msg += "  breakseal - Break the next seal\n";
                msg += "  diagnose - Full state diagnostic\n";
                msg += "  testcascade - Setup 3 mysteries + 3 seekers for cascade test\n";
                msg += "  narrath_help - Show this help message\n";
                map.world.prefabStore.popMsg(msg);
            }
        }

        public override void onTurnStart(Map map)
        {
            // Only run if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

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

            // Decrement compel timers (rebuild to avoid modifying dictionary during iteration)
            Dictionary<Person, int> newCompels = new Dictionary<Person, int>();
            foreach (var kvp in compelledHeroes)
            {
                int remaining = kvp.Value - 1;
                if (remaining > 0)
                {
                    newCompels[kvp.Key] = remaining;
                }
            }
            compelledHeroes = newCompels;

            // Decrement burn writings suppression (rebuild to avoid modifying dictionary during iteration)
            Dictionary<Person, int> newBurn = new Dictionary<Person, int>();
            foreach (var kvp in burnWritingsSuppression)
            {
                int remaining = kvp.Value - 1;
                if (remaining > 0)
                {
                    newBurn[kvp.Key] = remaining;
                }
            }
            burnWritingsSuppression = newBurn;
        }

        public override void onTurnEnd(Map map)
        {
            // Only run if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

            if (narrath == null) return;

            ProcessFragmentEffects(map);
            ProcessEchoRespawn(map);
        }

        private void ProcessFragmentEffects(Map map)
        {
            // Clean up dead/removed persons from all tracking dictionaries
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
                seekerProximityTurns.Remove(p);
                burnWritingsSuppression.Remove(p);
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
                    if (level == 2) chance = FRAGMENT_2_SPREAD_CHANCE;
                    else if (level == 3)
                    {
                        chance = SEEKER_BASE_SPREAD_CHANCE;
                        // Seekers get +5% per turn in same location
                        if (seekerProximityTurns.ContainsKey(person))
                        {
                            chance += SEEKER_PROXIMITY_SPREAD_BONUS * seekerProximityTurns[person];
                            seekerProximityTurns[person]++;
                        }
                        else
                        {
                            seekerProximityTurns[person] = 1;
                        }
                    }

                    // Fragment 4+: automatic spread, no chance roll
                    if (level >= 4)
                    {
                        if (!seekerProximityTurns.ContainsKey(person))
                            seekerProximityTurns[person] = 0;
                        seekerProximityTurns[person]++;

                        if (seekerProximityTurns[person] >= FRAGMENT_4_AUTO_SPREAD_TURNS)
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
                if (Eleven.random.NextDouble() < FRAGMENT_4_SILENCE_CHANCE)
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

            try
            {
                int index = 0;

                // Load fragment data
                if (index >= saveData.Length) return;
                int fragmentCount = saveData[index++];
                for (int i = 0; i < fragmentCount; i++)
                {
                    if (index + 1 >= saveData.Length) return;
                    int personIdx = saveData[index++];
                    int level = saveData[index++];
                    Person p = map.personByIndex(personIdx);
                    if (p != null)
                    {
                        fragmentLevels[p] = level;
                    }
                }

                // Load seal advancement progress
                if (index >= saveData.Length) return;
                int advCount = saveData[index++];

                // Load echo state
                if (index + 1 >= saveData.Length) return;
                echoAlive = saveData[index++] == 1;
                echoRespawnTimer = saveData[index++];

                // Load amanuensis state
                if (index + 1 >= saveData.Length) return;
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
            catch (IndexOutOfRangeException)
            {
                // Gracefully handle corrupted save data
            }
        }

        // World panic integration
        public override void populatingWorldPanicReasons(List<ReasonMsg> reasons, Map map)
        {
            // Only run if Narrath is the active god
            if (map.overmind.god is God_Narrath == false) { return; }

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
