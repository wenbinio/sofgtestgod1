using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_CompelInvestigation : Power
    {
        public P_CompelInvestigation(Map map) : base(map)
        {
            this.name = "Compel Investigation";
            this.description = "Force a hero to abandon their current quest and prioritize the nearest Mystery. " +
                "The hero is compelled for 5 turns, during which Mystery investigation has maximum utility.";
        }

        public override int getCost()
        {
            return 2;
        }

        public override int getMinSeal()
        {
            return 3;
        }

        public override bool validTarget(Unit target)
        {
            if (target == null || target.person == null) return false;
            return target.person.isHero();
        }

        public override void cast(Unit target)
        {
            if (target == null || target.person == null) return;
            if (Kernel_Narrath.instance == null) return;

            // Set compel flag for 5 turns
            Kernel_Narrath.instance.compelledHeroes[target.person] = 5;
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_compel.png");
        }
    }
}
