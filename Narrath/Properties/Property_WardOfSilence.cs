using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Property_WardOfSilence : Property
    {
        public Person wardingHero;

        public Property_WardOfSilence(Location loc, Person hero) : base(loc)
        {
            this.wardingHero = hero;
        }

        public override string getName()
        {
            return "Ward of Silence";
        }

        public override string getDescription()
        {
            string heroName = wardingHero != null ? wardingHero.getName() : "A hero";
            return heroName + " maintains a ward of silence here, suppressing the Mystery's progression. " +
                "Investigation progress is halted, and Fragment exposure is halved. The hero must remain to maintain the ward.";
        }

        public override void turnTick(Location loc)
        {
            // Check if the warding hero is still at this location
            if (wardingHero == null || wardingHero.isDead)
            {
                RemoveWard(loc);
                return;
            }

            bool heroPresent = false;
            if (wardingHero.unit != null && wardingHero.unit.location == loc)
            {
                heroPresent = true;
            }

            if (!heroPresent)
            {
                RemoveWard(loc);
            }
        }

        private void RemoveWard(Location loc)
        {
            if (Kernel_Narrath.instance != null)
            {
                Kernel_Narrath.instance.activeWards.Remove(loc);
            }
            loc.properties.Remove(this);
        }
    }
}
