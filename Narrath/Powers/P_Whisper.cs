using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_Whisper : Power
    {
        public P_Whisper(Map map) : base(map)
        {
            this.name = "Whisper";
            this.description = "Reach through dreams to whisper a fragment of the utterance. " +
                "The target hero gains Fragment 1. At Seal 5+, targets with Fragment 1 are elevated to Fragment 2.";
        }

        public override int getCost()
        {
            return 1;
        }

        public override int getMinSeal()
        {
            return 0; // Available from start
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

            Person hero = target.person;

            // Chosen One immune to ambient whisper
            if (hero.isChosenOne) return;

            int current = Kernel_Narrath.instance.GetFragmentLevel(hero);

            if (current == 0)
            {
                Kernel_Narrath.instance.SetFragmentLevel(hero, 1);
            }
            else if (current == 1 && Kernel_Narrath.narrath != null)
            {
                // At Seal 5+, upgrade to Fragment 2
                int brokenSeals = 0;
                for (int i = 0; i < God_Narrath.SEAL_THRESHOLDS.Length; i++)
                {
                    if (Kernel_Narrath.narrath.mysteryAdvancementCount >= God_Narrath.SEAL_THRESHOLDS[i])
                        brokenSeals++;
                }

                if (brokenSeals >= 5)
                {
                    Kernel_Narrath.instance.SetFragmentLevel(hero, 2);
                }
            }
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_whisper.png");
        }
    }
}
