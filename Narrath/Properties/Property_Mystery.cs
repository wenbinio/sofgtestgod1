using System;
using System.Collections.Generic;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Property_Mystery : Property
    {
        // Stage thresholds for investigation progress
        public static readonly int[] STAGE_THRESHOLDS = { 0, 50, 80, 120, 200 };

        // Menace values per stage to attract hero attention
        public static readonly double[] STAGE_MENACE = { 0, 0.5, 2.0, 5.0, 8.0, 12.0 };

        public int stage = 1;
        public int investigationProgress = 0;
        public God_Narrath parentGod;
        public int annotationBoostTurns = 0;

        public Property_Mystery(Location loc, God_Narrath god) : base(loc)
        {
            this.parentGod = god;
        }

        public override string getName()
        {
            switch (stage)
            {
                case 1: return "Mystery: The Oddity";
                case 2: return "Mystery: The Pattern";
                case 3: return "Mystery: The Revelation";
                case 4: return "Mystery: The Threshold";
                case 5: return "Mystery: The Mouth";
                default: return "Mystery";
            }
        }

        public override string getDescription()
        {
            switch (stage)
            {
                case 1:
                    return "The townsfolk report hearing a sound at the edge of perception. Not a voice — half a voice. The first syllable of something.";
                case 2:
                    return "Those who linger here begin to notice patterns in the architecture, the weather, the movement of birds. Patterns that almost form words.";
                case 3:
                    return "She speaks now only in fragments. Not madness — she is closer to understanding than anyone alive. That is precisely the problem.";
                case 4:
                    return "The settlement thrums with unspoken meaning. Those who pass through find themselves lingering, drawn by a question they cannot articulate.";
                case 5:
                    return "The sentence hangs in the air, perpetually unfinished. Those who hear it cannot leave. They strain toward a completion that will never come.";
                default:
                    return "Something incomplete lingers here.";
            }
        }

        public override double getMenace()
        {
            if (stage >= 0 && stage < STAGE_MENACE.Length)
                return STAGE_MENACE[stage];
            return 0;
        }

        public override void turnTick(Location loc)
        {
            if (loc == null) return;

            // Check if warded
            if (Kernel_Narrath.instance != null && Kernel_Narrath.instance.activeWards.ContainsKey(loc))
            {
                // Ward of Silence active: no progression, halved Fragment exposure
                return;
            }

            // Decrement annotation boost
            if (annotationBoostTurns > 0)
                annotationBoostTurns--;

            // Apply stage effects
            ApplyStageEffects(loc);
        }

        private void ApplyStageEffects(Location loc)
        {
            switch (stage)
            {
                case 1:
                    // Small menace increase to attract hero attention (handled via getMenace())
                    break;

                case 2:
                    // Madness +0.5/turn
                    AddMadness(loc, 0.5);
                    break;

                case 3:
                    // Madness +1.0/turn
                    AddMadness(loc, 1.0);
                    // Grant Fragment 1 to all heroes/rulers at this location
                    GrantFragmentsAtLocation(loc, 1);
                    break;

                case 4:
                    // Madness +2.0/turn
                    AddMadness(loc, 2.0);
                    // All heroes/rulers who pass through gain Fragment 1
                    GrantFragmentsAtLocation(loc, 1);
                    // Population decline: -1% per turn
                    ApplyPopulationDecline(loc, 0.01);
                    // Ruler personality erosion
                    if (loc.settlement != null && loc.settlement.ruler != null)
                    {
                        if (Eleven.random.NextDouble() < 1.0 / 20.0)
                        {
                            FlipRandomTrait(loc.settlement.ruler);
                        }
                    }
                    break;

                case 5:
                    // Madness +3.0/turn
                    AddMadness(loc, 3.0);
                    // Heroes cannot voluntarily leave (handled in Hooks_Narrath)
                    // Connected locations: Fragment 1 exposure for heroes/rulers present
                    if (loc.links != null)
                    {
                        foreach (Link link in loc.links)
                        {
                            Location connected = link.other(loc);
                            if (connected != null)
                            {
                                GrantFragmentsAtLocation(connected, 1);
                            }
                        }
                    }
                    // Population decline: -3% per turn
                    ApplyPopulationDecline(loc, 0.03);
                    break;
            }
        }

        private void AddMadness(Location loc, double amount)
        {
            if (loc.settlement != null)
            {
                loc.settlement.madness += amount;
                if (loc.settlement.madness > 1.0)
                    loc.settlement.madness = 1.0;
            }
        }

        private void GrantFragmentsAtLocation(Location loc, int minFragment)
        {
            if (Kernel_Narrath.instance == null) return;

            foreach (Unit u in loc.units)
            {
                if (u == null || u.person == null) continue;

                // Chosen One immunity to low-stage exposure
                if (u.person.isChosenOne && stage <= 2) continue;

                int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                if (current < minFragment)
                {
                    Kernel_Narrath.instance.SetFragmentLevel(u.person, minFragment);
                }
            }

            // Also affect ruler
            if (loc.settlement != null && loc.settlement.ruler != null)
            {
                Person ruler = loc.settlement.ruler;
                if (!ruler.isChosenOne || stage >= 3)
                {
                    int current = Kernel_Narrath.instance.GetFragmentLevel(ruler);
                    if (current < minFragment)
                    {
                        Kernel_Narrath.instance.SetFragmentLevel(ruler, minFragment);
                    }
                }
            }
        }

        private void ApplyPopulationDecline(Location loc, double rate)
        {
            if (loc.settlement != null)
            {
                int loss = (int)Math.Max(1, loc.settlement.population * rate);
                loc.settlement.population -= loss;
                if (loc.settlement.population < 0)
                    loc.settlement.population = 0;
            }
        }

        private void FlipRandomTrait(Person person)
        {
            if (person == null || person.traits == null || person.traits.Count == 0) return;

            // Remove a random trait and replace with its opposite (if available)
            int index = Eleven.random.Next(person.traits.Count);
            person.traits.RemoveAt(index);
        }

        public void AddInvestigationProgress(int amount)
        {
            if (Kernel_Narrath.instance != null && Kernel_Narrath.instance.activeWards.ContainsKey(this.location))
            {
                return; // Ward of Silence blocks progress
            }

            investigationProgress += amount;
            CheckStageAdvancement();
        }

        private void CheckStageAdvancement()
        {
            if (stage >= 5) return;

            int threshold = STAGE_THRESHOLDS[stage]; // threshold to advance from current stage
            if (investigationProgress >= threshold)
            {
                investigationProgress -= threshold;
                AdvanceStage();
            }
        }

        public void AdvanceStage()
        {
            if (stage >= 5) return;

            stage++;

            // Report to god for seal-breaking
            if (parentGod != null)
            {
                parentGod.addMysteryAdvancement();
            }

            // Stage 2: Investigating hero loses 1 sanity (handled at quest level)

            // Stage 3: Spawn Stage 1 Mysteries at up to 2 connected locations
            if (stage == 3)
            {
                SpreadToConnectedLocations(2);
            }

            // Stage 3: The hero who caused advancement becomes a Seeker
            // (Handled in Q_InvestigateMystery upon completion)
        }

        private void SpreadToConnectedLocations(int maxSpread)
        {
            if (location == null || location.links == null) return;

            int spread = 0;
            List<Location> candidates = new List<Location>();

            foreach (Link link in location.links)
            {
                Location connected = link.other(location);
                if (connected == null) continue;

                bool hasMystery = false;
                foreach (Property pr in connected.properties)
                {
                    if (pr is Property_Mystery)
                    {
                        hasMystery = true;
                        break;
                    }
                }

                if (!hasMystery)
                {
                    candidates.Add(connected);
                }
            }

            // Sort by population (prefer larger settlements)
            candidates.Sort((a, b) =>
            {
                int popA = a.settlement?.population ?? 0;
                int popB = b.settlement?.population ?? 0;
                return popB.CompareTo(popA);
            });

            foreach (Location target in candidates)
            {
                if (spread >= maxSpread) break;
                Property_Mystery newMystery = new Property_Mystery(target, parentGod);
                target.properties.Add(newMystery);
                spread++;
            }
        }

        public double GetQuestPriority()
        {
            double priority = stage * 20.0;
            if (annotationBoostTurns > 0)
            {
                priority *= 1.5;
            }
            return priority;
        }

        public int GetQuestComplexity()
        {
            switch (stage)
            {
                case 1: return 30;
                case 2: return 50;
                case 3: return 80;
                case 4: return 100;
                default: return 30;
            }
        }
    }
}
