using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_Unwriting : Power
    {
        public P_Unwriting(Map map) : base(map)
        {
            this.name = "Unwriting";
            this.description = "Erase one aspect of a settlement: a ruler's claim (succession crisis), " +
                "a trade route, or a defensive structure. What was written is unwritten.";
        }

        public override int getCost()
        {
            return 4;
        }

        public override int getMinSeal()
        {
            return 7;
        }

        public override bool validTarget(Location target)
        {
            if (target == null || target.settlement == null) return false;
            return true;
        }

        public override void cast(Location target)
        {
            if (target == null || target.settlement == null) return;

            // Build list of available erasure options
            List<Action> options = new List<Action>();

            // Option 1: Erase Ruler's Claim
            if (target.settlement.ruler != null)
            {
                options.Add(() =>
                {
                    target.settlement.ruler = null;
                });
            }

            // Option 2: Erase fortification
            if (target.settlement.fortification > 0)
            {
                options.Add(() =>
                {
                    target.settlement.fortification = 0;
                });
            }

            // Option 3: Erase a trade route
            if (target.links != null && target.links.Count > 1)
            {
                options.Add(() =>
                {
                    int index = Eleven.random.Next(target.links.Count);
                    Link link = target.links[index];
                    Location other = link.other(target);
                    target.links.Remove(link);
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

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_unwriting.png");
        }
    }
}
