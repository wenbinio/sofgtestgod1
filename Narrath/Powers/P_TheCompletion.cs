using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_TheCompletion : Power
    {
        public P_TheCompletion(Map map) : base(map)
        {
            this.name = "The Completion";
            this.description = "Everyone, everywhere, falls silent. They are listening. They almost hear it. They will never hear it. " +
                "That is the apocalypse — not destruction, but the eternal, exquisite failure to understand.\n\n" +
                "All Mysteries advance 1 stage. All Seekers gain +1 Fragment. All completions trigger immediately, " +
                "generating cascading Mysteries and Fragment shockwaves.";
        }

        public override int getCost()
        {
            return 5; // Minimum 5, uses all remaining power
        }

        public override int getMinSeal()
        {
            return 9; // Awakening
        }

        public override bool validTarget(Map map)
        {
            // Global power — no target selection
            return true;
        }

        public override void cast(Map map)
        {
            if (Kernel_Narrath.instance == null || Kernel_Narrath.narrath == null) return;

            // Consume all remaining power
            Kernel_Narrath.narrath.power = 0;

            // All Mysteries advance 1 stage
            foreach (Location loc in map.locations)
            {
                if (loc == null) continue;
                foreach (Property pr in loc.properties)
                {
                    if (pr is Property_Mystery mystery)
                    {
                        mystery.AdvanceStage();
                        break;
                    }
                }
            }

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

            // Process all Fragment 5 completions immediately
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
                Kernel_Narrath.instance.ProcessFragmentCompletion(person, map);
            }
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_completion.png");
        }
    }
}
