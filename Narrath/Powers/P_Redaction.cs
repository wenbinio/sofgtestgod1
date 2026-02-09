using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class P_Redaction : Power
    {
        public P_Redaction(Map map) : base(map)
        {
            this.name = "Redaction";
            this.description = "Remove one personality trait from a Seeker (Fragment 3+) and transfer it to a random ruler " +
                "connected to the Seeker's current location. The Seeker loses depth; the ruler gains unexpected personality shifts.";
        }

        public override int getCost()
        {
            return 3;
        }

        public override int getMinSeal()
        {
            return 5;
        }

        public override bool validTarget(Unit target)
        {
            if (target == null || target.person == null) return false;
            if (Kernel_Narrath.instance == null) return false;

            // Must be a Seeker (Fragment 3+)
            return Kernel_Narrath.instance.GetFragmentLevel(target.person) >= 3;
        }

        public override void cast(Unit target)
        {
            if (target == null || target.person == null) return;

            Person seeker = target.person;
            if (seeker.traits == null || seeker.traits.Count == 0) return;

            // Remove a random trait from the Seeker
            int traitIndex = Eleven.random.Next(seeker.traits.Count);
            var removedTrait = seeker.traits[traitIndex];
            seeker.traits.RemoveAt(traitIndex);

            // Find a random ruler connected to the Seeker's location
            if (target.location == null || target.location.links == null) return;

            List<Person> nearbyRulers = new List<Person>();
            foreach (Link link in target.location.links)
            {
                Location connected = link.other(target.location);
                if (connected?.settlement?.ruler != null)
                {
                    nearbyRulers.Add(connected.settlement.ruler);
                }
            }

            // Also check the Seeker's current location
            if (target.location.settlement?.ruler != null)
            {
                nearbyRulers.Add(target.location.settlement.ruler);
            }

            if (nearbyRulers.Count > 0)
            {
                Person ruler = nearbyRulers[Eleven.random.Next(nearbyRulers.Count)];
                ruler.traits.Add(removedTrait);
            }
        }

        public override Sprite getIcon()
        {
            return EventManager.getImg("ShadowsNarrath.power_redaction.png");
        }
    }
}
