using System;
using System.Collections.Generic;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Ch_Unwrite : Challenge
    {
        public Ch_Unwrite(Location loc) : base(loc)
        {
        }

        public override string getName()
        {
            return "Unwrite";
        }

        public override string getDescription()
        {
            return "Erase one aspect of this settlement from existence. A trade route, a fortification, " +
                "a ruler's claim, a holy site — the Amanuensis speaks a word of negation, and what was " +
                "written is unwritten. The effect is conspicuous and threatening.";
        }

        public override string getRestriction()
        {
            if (location == null) return "Requires a location.";
            if (location.settlement == null) return "Requires a settlement.";
            return null;
        }

        public override int getComplexity()
        {
            return 60;
        }

        public override int getBaseStat()
        {
            // Lore-based
            return 3;
        }

        public override double getProfile()
        {
            return 5;
        }

        public override double getMenace()
        {
            return 8;
        }

        public override void complete(UA agent)
        {
            if (location == null || location.settlement == null) return;

            // Choose what to erase based on what's available
            List<Action> options = new List<Action>();

            // Option 1: Erase Ruler's Claim
            if (location.settlement.ruler != null)
            {
                options.Add(() =>
                {
                    location.settlement.ruler = null;
                });
            }

            // Option 2: Erase fortification
            if (location.settlement.fortification > 0)
            {
                options.Add(() =>
                {
                    location.settlement.fortification = 0;
                });
            }

            // Option 3: Erase a trade route (remove a link)
            if (location.links != null && location.links.Count > 1)
            {
                options.Add(() =>
                {
                    // Remove a random non-essential trade link
                    int index = Eleven.random.Next(location.links.Count);
                    Link link = location.links[index];
                    Location other = link.other(location);
                    location.links.Remove(link);
                    if (other != null)
                    {
                        other.links.Remove(link);
                    }
                });
            }

            if (options.Count > 0)
            {
                int choice = Eleven.random.Next(options.Count);
                options[choice]();
            }
        }
    }
}
