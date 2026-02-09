using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_Unwrite : Trait
    {
        public Trait_Unwrite()
        {
            this.name = "Unwrite";
            this.description = "The Amanuensis can speak words of negation, erasing aspects of settlements from existence. " +
                "Enables the Unwrite challenge — removing trade routes, fortifications, or ruler claims.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override void turnTick(Person person)
        {
            // Passive trait — enables Ch_Unwrite
        }
    }
}
