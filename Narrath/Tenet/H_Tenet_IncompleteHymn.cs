using System;
using System.Collections.Generic;
using Assets.Code;

namespace ShadowsNarrath
{
    public class H_Tenet_IncompleteHymn : HolyTenet
    {
        public H_Tenet_IncompleteHymn(HolyOrder order) : base(order)
        {
            this.name = "The Incomplete Hymn";
            this.description = "A hymn that trails off mid-verse. Those who sing it find themselves " +
                "compelled to seek the missing words. The congregation's devotion becomes a vector " +
                "for Narrath's influence.";
            this.minLevel = -2;
            this.maxLevel = 0;
        }

        public override string getLevelDescription(int level)
        {
            switch (level)
            {
                case -1:
                    return "Settlements following this faith have a 5% chance per turn of any hero or ruler present " +
                        "gaining Fragment 1. Mysteries in these settlements advance 25% faster.";
                case -2:
                    return "As above, plus acolytes actively seek out and spread incomplete hymns. " +
                        "Creates a passive Fragment infrastructure throughout the faithful settlements.";
                default:
                    return "The hymn is complete — its influence is suppressed.";
            }
        }

        public override void turnTick(HolyOrder order)
        {
            if (Kernel_Narrath.instance == null) return;

            int level = this.level;
            if (level >= 0) return;

            // Get all settlements following this faith
            foreach (Location loc in order.map.locations)
            {
                if (loc == null || loc.settlement == null) continue;
                if (loc.settlement.holyOrder != order) continue;

                // Level -1 and -2: 5% chance per turn for heroes/rulers to gain Fragment 1
                if (Eleven.random.NextDouble() < 0.05)
                {
                    foreach (Unit u in loc.units)
                    {
                        if (u == null || u.person == null) continue;
                        if (u.person.isHero() || u.person.rulerOf >= 0)
                        {
                            // Chosen One immune to ambient exposure
                            if (u.person.isChosenOne) continue;

                            int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                            if (current < 1)
                            {
                                Kernel_Narrath.instance.SetFragmentLevel(u.person, 1);
                            }
                        }
                    }
                }

                // Mysteries advance 25% faster at level -1 and -2
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery mystery)
                    {
                        // Add bonus investigation progress (25% of normal per-turn)
                        mystery.AddInvestigationProgress(3);
                        break;
                    }
                }

                // Level -2: additional passive Fragment infrastructure
                if (level <= -2)
                {
                    // Double the Fragment exposure chance
                    if (Eleven.random.NextDouble() < 0.05)
                    {
                        foreach (Unit u in loc.units)
                        {
                            if (u == null || u.person == null) continue;
                            if (u.person.isChosenOne) continue;

                            int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                            if (current < 1)
                            {
                                Kernel_Narrath.instance.SetFragmentLevel(u.person, 1);
                            }
                        }
                    }
                }
            }
        }
    }
}
