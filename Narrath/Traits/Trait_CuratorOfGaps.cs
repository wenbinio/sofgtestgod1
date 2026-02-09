using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_CuratorOfGaps : Trait
    {
        public Trait_CuratorOfGaps()
        {
            this.name = "Curator of Gaps";
            this.description = "The Archivist has devoted their existence to cataloguing what is missing. " +
                "Enables the Plant Mystery challenge — inscribing fragments of Narrath's utterance into infiltrated locations.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override void turnTick(Person person)
        {
            // Passive trait — enables Ch_PlantMystery
        }
    }
}
