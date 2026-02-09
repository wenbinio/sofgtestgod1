using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_Fragile : Trait
    {
        public Trait_Fragile()
        {
            this.name = "Fragile Vessel";
            this.description = "The Amanuensis exists at the threshold of completion — body barely coherent, " +
                "held together by the force of the unfinished word. Only 15 HP, but killing the Amanuensis " +
                "is a pyrrhic victory: the death location becomes a Stage 4 Mystery, and all characters " +
                "within 2 hexes gain +2 Fragments from the psychic shockwave of released utterance.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override void turnTick(Person person)
        {
            // Death effect handled in UA_Amanuensis.die()
        }
    }
}
