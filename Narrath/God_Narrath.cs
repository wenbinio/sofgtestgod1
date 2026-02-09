using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class God_Narrath : God
    {
        // Seal advancement thresholds based on cumulative Mystery stage advancements
        public static readonly int[] SEAL_THRESHOLDS = { 3, 8, 15, 25, 40, 60, 85, 115, 150 };

        // Mystery advancement counter for seal breaking
        public int mysteryAdvancementCount = 0;

        // Completion counter for panic/scoring
        public int completionCount = 0;

        public override string getName()
        {
            return "Narrath, That Which Was Half-Spoken";
        }

        public override void setup(Map map)
        {
            base.setup(map);

            // Register powers with seal level requirements
            powers.Add(new P_Whisper(map));                // Seal 0
            powerLevelReqs.Add(0);
            powers.Add(new P_Palimpsest(map));             // Seal 2
            powerLevelReqs.Add(2);
            powers.Add(new P_CompelInvestigation(map));    // Seal 3
            powerLevelReqs.Add(3);
            powers.Add(new P_Redaction(map));              // Seal 5
            powerLevelReqs.Add(5);
            powers.Add(new P_Glossolalia(map));            // Seal 6
            powerLevelReqs.Add(6);
            powers.Add(new P_Unwriting(map));              // Seal 7
            powerLevelReqs.Add(7);
            powers.Add(new P_TheCompletion(map));          // Seal 9
            powerLevelReqs.Add(9);
        }

        public override void onStart(Map map)
        {
            base.onStart(map);

            // Spawn starting agent: The Archivist
            Location tomb = map.locations[map.overmind.elderTomb];
            if (tomb != null)
            {
                UA_Archivist archivist = new UA_Archivist(tomb, map.overmind);
                map.units.Add(archivist);
                tomb.units.Add(archivist);
            }
        }

        public override bool checkSealBreak(int sealIndex)
        {
            if (sealIndex < 0 || sealIndex >= SEAL_THRESHOLDS.Length) return false;
            return mysteryAdvancementCount >= SEAL_THRESHOLDS[sealIndex];
        }

        public override void onSealBroken(Map map, int sealIndex)
        {
            base.onSealBroken(map, sealIndex);

            // Each seal break grants +1 max power and +1 current power
            this.maxPower++;
            this.power = Math.Min(this.power + 1, this.maxPower);

            // Spawn Echo at Seal 4
            if (sealIndex == 3 && Kernel_Narrath.instance != null)
            {
                SpawnEcho(map);
            }

            // Spawn Amanuensis at Seal 7
            if (sealIndex == 6 && Kernel_Narrath.instance != null)
            {
                SpawnAmanuensis(map);
            }
        }

        public void SpawnEcho(Map map)
        {
            if (Kernel_Narrath.instance == null) return;
            if (Kernel_Narrath.instance.echoAlive) return;

            Location tomb = map.locations[map.overmind.elderTomb];
            if (tomb != null)
            {
                UA_Echo echo = new UA_Echo(tomb, map.overmind);
                map.units.Add(echo);
                tomb.units.Add(echo);
                Kernel_Narrath.instance.echoAlive = true;
                Kernel_Narrath.instance.echoRespawnTimer = -1;
            }
        }

        public void SpawnAmanuensis(Map map)
        {
            if (Kernel_Narrath.instance == null) return;
            if (Kernel_Narrath.instance.amanuensisSpawned) return;

            Location tomb = map.locations[map.overmind.elderTomb];
            if (tomb != null)
            {
                UA_Amanuensis amanuensis = new UA_Amanuensis(tomb, map.overmind);
                map.units.Add(amanuensis);
                tomb.units.Add(amanuensis);
                Kernel_Narrath.instance.amanuensisSpawned = true;
                Kernel_Narrath.instance.amanuensisAlive = true;
            }
        }

        public void addMysteryAdvancement()
        {
            mysteryAdvancementCount++;
        }

        public void addCompletionScore()
        {
            completionCount++;
        }

        // Apocalypse score calculation
        public override double getApocalypseScore(Map map)
        {
            double score = 0;

            foreach (Location loc in map.locations)
            {
                if (loc == null) continue;
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery mystery)
                    {
                        if (mystery.stage == 4) score += 0.5;
                        if (mystery.stage == 5) score += 2.0;
                        break;
                    }
                }
            }

            // Fragment 5 completions: +5 per completion
            score += completionCount * 5.0;

            return score;
        }

        public override Sprite getGodPortrait(World world)
        {
            // Load from Art/god_narrath.png if available
            return EventManager.getImg("ShadowsNarrath.god_narrath.png");
        }

        public override string getDescFlavour()
        {
            return "An incomplete cosmic utterance — a sentence begun at the dawn of reality that was never finished. " +
                "Those who encounter fragments of it cannot resist trying to complete it, and the pursuit of that completion consumes them.";
        }

        public override string getDescMechanics()
        {
            return "Narrath creates Mysteries that heroes are compelled to investigate, and their investigation is itself the vector of apocalypse. " +
                "The more competent the heroes, the faster they destroy themselves.";
        }
    }
}
