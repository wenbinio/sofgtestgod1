using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_Annotator : Trait
    {
        public Trait_Annotator()
        {
            this.name = "Annotator";
            this.description = "A talent for making the incomplete more compelling. " +
                "Enables the Annotate Mystery challenge — boosting a Mystery's attractiveness to hero investigators.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override void turnTick(Person person)
        {
            // Passive trait — enables Ch_AnnotateMystery
        }
    }
}
