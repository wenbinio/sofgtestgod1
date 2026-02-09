using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_Palimpsest : Power
    {
        public P_Palimpsest(Map map) : base(map)
        {
            this.name = "Palimpsest";
            this.description = "Copy a Mystery's resonance to a connected location. " +
                "A Stage 1 Mystery spawns at the connected location with the highest population that doesn't already have a Mystery.";
        }

        public override int getCost()
        {
            return 2;
        }

        public override int getMinSeal()
        {
            return 2;
        }

        public override bool validTarget(Location target)
        {
            if (target == null) return false;

            // Target must have an existing Mystery
            foreach (Property pr in target.properties)
            {
                if (pr is Property_Mystery)
                    return true;
            }

            return false;
        }

        public override void cast(Location target)
        {
            if (target == null || Kernel_Narrath.narrath == null) return;

            // Find connected location with highest population that has no Mystery
            Location bestTarget = null;
            int bestPop = -1;

            if (target.links != null)
            {
                foreach (Link link in target.links)
                {
                    Location connected = link.other(target);
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
                        int pop = connected.settlement?.population ?? 0;
                        if (pop > bestPop)
                        {
                            bestPop = pop;
                            bestTarget = connected;
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                Property_Mystery newMystery = new Property_Mystery(bestTarget, Kernel_Narrath.narrath);
                bestTarget.properties.Add(newMystery);
            }
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_palimpsest.png");
        }
    }
}
