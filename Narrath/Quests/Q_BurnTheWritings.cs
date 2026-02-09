using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Q_BurnTheWritings : Quest
    {
        public Q_BurnTheWritings(Location loc) : base(loc)
        {
        }

        public override string getName()
        {
            return "Burn the Writings";
        }

        public override string getDescription()
        {
            return "Destroy the writings of a Seeker to suppress their ability to spread Fragments. " +
                "The suppression lasts for 50 turns — the Seeker will eventually produce more writings. " +
                "Warning: the hero who burns the writings will be exposed to Fragment influence in the process.";
        }

        public override double getPriority()
        {
            // Check if there's a Seeker here
            if (location == null || Kernel_Narrath.instance == null) return 0;

            foreach (Unit u in location.units)
            {
                if (u == null || u.person == null) continue;
                if (Kernel_Narrath.instance.GetFragmentLevel(u.person) >= 2)
                {
                    return 25.0; // Moderate priority
                }
            }

            return 0;
        }

        public override int getComplexity()
        {
            return 15;
        }

        public override void onComplete(Person hero)
        {
            if (location == null || hero == null || Kernel_Narrath.instance == null) return;

            // Find a Seeker at this location and suppress their spread
            foreach (Unit u in location.units)
            {
                if (u == null || u.person == null) continue;
                if (Kernel_Narrath.instance.GetFragmentLevel(u.person) >= 2)
                {
                    Kernel_Narrath.instance.burnWritingsSuppression[u.person] = 50;
                    break;
                }
            }

            // The investigating hero gains Fragment 1 from exposure
            int current = Kernel_Narrath.instance.GetFragmentLevel(hero);
            if (current < 1)
            {
                Kernel_Narrath.instance.SetFragmentLevel(hero, 1);
            }
        }
    }
}
