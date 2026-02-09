using System;
using System.Collections.Generic;
using Assets.Code;
using CommunityLib;

namespace ShadowsNarrath
{
    public class Hooks_Narrath : Hooks
    {
        public Hooks_Narrath(Map map) : base(map)
        {
        }

        public void RegisterHooks()
        {
            // Register with CommunityLib's hook system
            CommunityLib.HookManager.RegisterHooks(this);
        }

        // NOTE: The following hooks were designed for Narrath's hero AI modification needs
        // but are NOT currently available in CommunityLib. These methods will never be called.
        // Alternative implementations are needed via ModKernel overrides or Harmony patches.
        // TODO: Either contribute these hooks to CommunityLib or implement via Harmony patches

        /*
        // Hook: Modify hero quest utility to prioritize Mystery investigation
        public override double onGetQuestUtility(Quest quest, Person hero, double currentUtility)
        {
            if (Kernel_Narrath.instance == null) return currentUtility;

            if (quest is Q_InvestigateMystery investigateQuest)
            {
                int fragmentLevel = Kernel_Narrath.instance.GetFragmentLevel(hero);

                // Fragment 1: slightly increased priority
                if (fragmentLevel >= 1)
                {
                    currentUtility *= 1.25;
                }

                // Fragment 2: strongly prioritizes Mystery investigation
                if (fragmentLevel >= 2)
                {
                    currentUtility *= 2.0;
                }

                // Fragment 3 (Seeker): abandons non-Mystery quests
                if (fragmentLevel >= 3)
                {
                    currentUtility = double.MaxValue / 2; // Near-max priority
                }

                // Compelled heroes: maximum utility
                if (Kernel_Narrath.instance.IsCompelled(hero))
                {
                    currentUtility = double.MaxValue;
                }
            }
            else
            {
                // Seekers abandon non-Mystery quests
                int fragmentLevel = Kernel_Narrath.instance.GetFragmentLevel(hero);
                if (fragmentLevel >= 3)
                {
                    // Check if any Mystery exists on the map
                    bool mysteryExists = false;
                    foreach (Location loc in map.locations)
                    {
                        if (loc == null) continue;
                        foreach (Property pr in loc.properties)
                        {
                            if (pr is Property_Mystery)
                            {
                                mysteryExists = true;
                                break;
                            }
                        }
                        if (mysteryExists) break;
                    }

                    if (mysteryExists)
                    {
                        currentUtility = 0; // Abandon non-Mystery quests
                    }
                }
            }

            return currentUtility;
        }

        // Hook: Modify hero movement to trap heroes at Stage 5 Mysteries
        public override bool onIsLocationValidForMovement(Location loc, Unit unit)
        {
            if (unit == null || unit.person == null) return true;

            // Stage 5 Mysteries: heroes at this location cannot voluntarily leave
            if (unit.location != null)
            {
                foreach (Property pr in unit.location.properties)
                {
                    if (pr is Property_Mystery mystery && mystery.stage >= 5)
                    {
                        // Hero is at a Stage 5 Mystery — cannot leave
                        if (unit.person.isHero() && !(unit is UA))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Hook: Amanuensis attraction — heroes within 2 hexes move toward it
        public override Location onGetHeroMoveTarget(Unit hero, Location currentTarget)
        {
            if (hero == null || hero.person == null) return currentTarget;
            if (Kernel_Narrath.instance == null) return currentTarget;

            // Find any Amanuensis on the map
            foreach (Unit u in map.units)
            {
                if (u is UA_Amanuensis amanuensis && amanuensis.location != null)
                {
                    // Check if hero is within 2 hexes
                    if (IsWithinHexRange(hero.location, amanuensis.location, 2))
                    {
                        if (hero.location != amanuensis.location)
                        {
                            return amanuensis.location;
                        }
                    }
                }
            }

            return currentTarget;
        }

        // Hook: Kindred Recognition — Seekers don't attack the Archivist
        public override double onGetAgentThreat(UA agent, Unit evaluator, double currentThreat)
        {
            if (agent is UA_Archivist && evaluator != null && evaluator.person != null)
            {
                if (Kernel_Narrath.instance != null && Kernel_Narrath.instance.IsSeeker(evaluator.person))
                {
                    return 0; // Seekers ignore the Archivist
                }
            }

            return currentThreat;
        }

        // Helper: Check if two locations are within a given hex range
        private bool IsWithinHexRange(Location a, Location b, int range)
        {
            if (a == null || b == null) return false;
            if (a == b) return true;

            // BFS within range
            HashSet<Location> visited = new HashSet<Location>();
            Queue<(Location loc, int depth)> queue = new Queue<(Location, int)>();
            queue.Enqueue((a, 0));
            visited.Add(a);

            while (queue.Count > 0)
            {
                var (current, depth) = queue.Dequeue();
                if (current == b) return true;
                if (depth >= range) continue;

                if (current.links != null)
                {
                    foreach (Link link in current.links)
                    {
                        Location neighbor = link.other(current);
                        if (neighbor != null && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, depth + 1));
                        }
                    }
                }
            }

            return false;
        }
        */

        // NOTE: The above hooks don't exist in CommunityLib yet. 
        // Hero AI modification will need to be implemented through:
        // 1. Harmony patches on quest selection logic
        // 2. ModKernel.unitAgentAI() for non-agent hero behavior
        // 3. Direct manipulation in Quest.onComplete() callbacks
    }
}
