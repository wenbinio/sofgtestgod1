using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_Glossolalia : Power
    {
        public P_Glossolalia(Map map) : base(map)
        {
            this.name = "Glossolalia";
            this.description = "All Seekers (Fragment 3+) on the map gain +1 Fragment, regardless of distance. " +
                "The targeted Resonance Point (Stage 4+ Mystery) pulses, increasing madness by 10% at all adjacent locations. " +
                "Any Fragment 5 completions are processed immediately.";
        }

        public override int getCost()
        {
            return 3;
        }

        public override int getMinSeal()
        {
            return 6;
        }

        public override bool validTarget(Location target)
        {
            if (target == null) return false;

            // Target must be a Resonance Point (Stage 4+ Mystery)
            foreach (Property pr in target.properties)
            {
                if (pr is Property_Mystery mystery && mystery.stage >= 4)
                    return true;
            }

            return false;
        }

        public override void cast(Location target)
        {
            if (Kernel_Narrath.instance == null) return;

            // All Seekers gain +1 Fragment
            List<Person> seekers = new List<Person>();
            foreach (var kvp in Kernel_Narrath.instance.fragmentLevels)
            {
                if (kvp.Value >= 3)
                {
                    seekers.Add(kvp.Key);
                }
            }

            foreach (Person seeker in seekers)
            {
                Kernel_Narrath.instance.IncrementFragment(seeker);
            }

            // Process any new Fragment 5 completions immediately
            List<Person> completions = new List<Person>();
            foreach (var kvp in Kernel_Narrath.instance.fragmentLevels)
            {
                if (kvp.Value >= 5)
                {
                    completions.Add(kvp.Key);
                }
            }

            foreach (Person person in completions)
            {
                Kernel_Narrath.instance.ProcessFragmentCompletion(person, target.map);
            }

            // Pulse: adjacent locations gain +10% Madness
            if (target.links != null)
            {
                foreach (Link link in target.links)
                {
                    Location connected = link.other(target);
                    if (connected?.settlement != null)
                    {
                        connected.settlement.madness += 0.10;
                        if (connected.settlement.madness > 1.0)
                            connected.settlement.madness = 1.0;
                    }
                }
            }
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_glossolalia.png");
        }
    }
}
