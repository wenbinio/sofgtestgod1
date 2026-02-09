using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Ch_AnnotateMystery : Challenge
    {
        public Ch_AnnotateMystery(Location loc) : base(loc)
        {
        }

        public override string getName()
        {
            return "Annotate Mystery";
        }

        public override string getDescription()
        {
            return "Add marginalia to the existing inscriptions — hints, arrows, underlinings " +
                "that make the Mystery more noticeable, more compelling. Stronger heroes will be " +
                "drawn to investigate this location specifically.";
        }

        public override string getRestriction()
        {
            if (location == null) return "Requires a location.";

            bool hasMystery = false;
            foreach (Property pr in location.properties)
            {
                if (pr is Property_Mystery)
                {
                    hasMystery = true;
                    break;
                }
            }

            if (!hasMystery)
                return "Requires a location with an existing Mystery.";

            return null;
        }

        public override int getComplexity()
        {
            return 20;
        }

        public override int getBaseStat()
        {
            // Intrigue-based
            return 2; // Index for Intrigue stat
        }

        public override double getProfile()
        {
            return 1;
        }

        public override double getMenace()
        {
            return 1;
        }

        public override void complete(UA agent)
        {
            if (location == null) return;

            foreach (Property pr in location.properties)
            {
                if (pr is Property_Mystery mystery)
                {
                    // Boost quest priority for 30 turns (50% increase)
                    mystery.annotationBoostTurns = 30;
                    break;
                }
            }
        }
    }
}
