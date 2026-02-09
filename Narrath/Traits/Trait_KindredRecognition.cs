using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_KindredRecognition : Trait
    {
        public Trait_KindredRecognition()
        {
            this.name = "Kindred Recognition";
            this.description = "Seekers — those who carry Fragment 3 or higher — recognize the Archivist as a fellow traveler " +
                "on the path of completion. They will not attack or report this agent.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override double getThreatModifier(Person evaluator)
        {
            // If the evaluator is a Seeker, threat is zero
            if (Kernel_Narrath.instance != null && Kernel_Narrath.instance.IsSeeker(evaluator))
            {
                return 0;
            }
            return 1.0;
        }

        public override void turnTick(Person person)
        {
            // Passive trait — handled via getThreatModifier
        }
    }
}
