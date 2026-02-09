using System;
using System.Collections.Generic;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Trait_TheLastSyllable : Trait
    {
        public Trait_TheLastSyllable()
        {
            this.name = "The Last Syllable";
            this.description = "All heroes within two hexes are compelled to move toward the Amanuensis, " +
                "drawn by the promise of understanding the utterance's final word. " +
                "Upon arriving adjacent, they gain Fragment 2 immediately.";
        }

        public override int getMaxLevel()
        {
            return 1;
        }

        public override void turnTick(Person person)
        {
            // Compulsion effect handled in UA_Amanuensis.turnTick() and Hooks_Narrath
        }
    }
}
