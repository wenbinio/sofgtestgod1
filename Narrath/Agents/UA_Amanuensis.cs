using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class UA_Amanuensis : UA
    {
        public UA_Amanuensis(Location loc, Overmind overmind) : base(loc, overmind)
        {
            this.person = new Person(loc.map);
            this.person.unit = this;
            this.person.name = "The Amanuensis";
            this.person.stat_lore = 5;
            this.person.stat_command = 4;
            this.person.stat_intrigue = 1;
            this.person.stat_might = 3;

            // Add traits
            this.person.traits.Add(new Trait_TheLastSyllable());
            this.person.traits.Add(new Trait_Unwrite());
            this.person.traits.Add(new Trait_Fragile());

            this.hp = 15;    // Very low for a late-game agent
            this.maxHp = 15;
            this.profileScore = 5;
            this.menaceScore = 8;
        }

        public override string getName()
        {
            return "The Amanuensis";
        }

        public override string getDescription()
        {
            return "A Seeker frozen at the moment of completion — mouth open, eyes unfocused, " +
                "forever on the verge of finishing the sentence. All heroes within two hexes are " +
                "compelled to move toward the Amanuensis, drawn by the promise of understanding. " +
                "Upon arriving, they find only more questions. Killing the Amanuensis is a pyrrhic " +
                "victory: its death location becomes a Stage 4 Mystery, and all nearby characters " +
                "gain Fragments from the psychic shockwave.";
        }

        public override Sprite getPortrait()
        {
            return EventManager.getImg("ShadowsNarrath.agent_amanuensis.png");
        }

        public override void turnTick(Map map)
        {
            base.turnTick(map);

            if (this.location == null) return;

            // Trait_TheLastSyllable: compel heroes within 2 hexes
            CompelNearbyHeroes(map);
        }

        private void CompelNearbyHeroes(Map map)
        {
            if (Kernel_Narrath.instance == null) return;
            if (this.location == null) return;

            // Find all heroes within 2 hexes
            HashSet<Location> nearbyLocations = new HashSet<Location>();
            nearbyLocations.Add(this.location);

            // 1 hex away
            if (this.location.links != null)
            {
                foreach (Link link in this.location.links)
                {
                    Location neighbor = link.other(this.location);
                    if (neighbor != null)
                    {
                        nearbyLocations.Add(neighbor);

                        // 2 hexes away
                        if (neighbor.links != null)
                        {
                            foreach (Link link2 in neighbor.links)
                            {
                                Location neighbor2 = link2.other(neighbor);
                                if (neighbor2 != null)
                                {
                                    nearbyLocations.Add(neighbor2);
                                }
                            }
                        }
                    }
                }
            }

            foreach (Location loc in nearbyLocations)
            {
                foreach (Unit u in loc.units)
                {
                    if (u == null || u.person == null || u == this) continue;
                    if (!u.person.isHero()) continue;

                    // Heroes arriving at the Amanuensis location gain Fragment 2
                    if (loc == this.location)
                    {
                        int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                        if (current < 2)
                        {
                            Kernel_Narrath.instance.SetFragmentLevel(u.person, 2);
                        }
                    }
                }
            }
        }

        public override List<Challenge> getChallenges()
        {
            List<Challenge> challenges = new List<Challenge>();
            challenges.Add(new Ch_Unwrite(this.location));
            return challenges;
        }

        public override void die(Map map, string reason)
        {
            // Pyrrhic death: location becomes Stage 4 Mystery
            if (this.location != null)
            {
                Property_Mystery existing = null;
                foreach (Property pr in this.location.properties)
                {
                    if (pr is Property_Mystery pm)
                    {
                        existing = pm;
                        break;
                    }
                }

                if (existing != null)
                {
                    while (existing.stage < 4)
                    {
                        existing.AdvanceStage();
                    }
                }
                else if (Kernel_Narrath.narrath != null)
                {
                    Property_Mystery mystery = new Property_Mystery(this.location, Kernel_Narrath.narrath);
                    mystery.stage = 4;
                    this.location.properties.Add(mystery);
                }

                // All characters within 2 hexes gain +2 Fragments
                HashSet<Location> nearbyLocations = new HashSet<Location>();
                nearbyLocations.Add(this.location);

                if (this.location.links != null)
                {
                    foreach (Link link in this.location.links)
                    {
                        Location neighbor = link.other(this.location);
                        if (neighbor != null)
                        {
                            nearbyLocations.Add(neighbor);
                            if (neighbor.links != null)
                            {
                                foreach (Link link2 in neighbor.links)
                                {
                                    Location neighbor2 = link2.other(neighbor);
                                    if (neighbor2 != null)
                                    {
                                        nearbyLocations.Add(neighbor2);
                                    }
                                }
                            }
                        }
                    }
                }

                if (Kernel_Narrath.instance != null)
                {
                    foreach (Location loc in nearbyLocations)
                    {
                        foreach (Unit u in loc.units)
                        {
                            if (u == null || u.person == null) continue;
                            int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                            Kernel_Narrath.instance.SetFragmentLevel(u.person, current + 2);
                        }
                    }
                }
            }

            // Amanuensis does NOT respawn
            if (Kernel_Narrath.instance != null)
            {
                Kernel_Narrath.instance.amanuensisAlive = false;
            }

            base.die(map, reason);
        }
    }
}
