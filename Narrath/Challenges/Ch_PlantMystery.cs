using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Ch_PlantMystery : Challenge
    {
        public Ch_PlantMystery(Location loc) : base(loc)
        {
        }

        public override string getName()
        {
            return "Plant Mystery";
        }

        public override string getDescription()
        {
            return "Inscribe fragments of Narrath's utterance into this location — hidden in architecture, " +
                "woven into records, etched into the edges of perception. The curious will find them. " +
                "They always do.";
        }

        public override string getRestriction()
        {
            if (location == null) return "Requires a location.";

            if (!location.isInfiltrated)
                return "Requires an infiltrated location.";

            foreach (Property pr in location.properties)
            {
                if (pr is Property_Mystery)
                    return "This location already has a Mystery.";
            }

            return null; // No restriction — challenge is available
        }

        public override int getComplexity()
        {
            return 30;
        }

        public override int getBaseStat()
        {
            // Lore-based
            return 3; // Index for Lore stat
        }

        public override double getProfile()
        {
            return 2;
        }

        public override double getMenace()
        {
            return 3;
        }

        public override void complete(UA agent)
        {
            if (location == null || Kernel_Narrath.narrath == null) return;

            Property_Mystery mystery = new Property_Mystery(location, Kernel_Narrath.narrath);
            location.properties.Add(mystery);
        }
    }
}
