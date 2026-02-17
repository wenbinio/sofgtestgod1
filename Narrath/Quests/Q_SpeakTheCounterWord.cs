using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Q_SpeakTheCounterWord : Quest
    {
        public Property_Mystery mystery;

        public Q_SpeakTheCounterWord(Location loc, Property_Mystery mystery) : base(loc)
        {
            this.mystery = mystery;
        }

        public override string getName()
        {
            return "Speak the Counter-Word";
        }

        public override string getDescription()
        {
            return "The Chosen One speaks a word of undoing, reducing the Mystery by one stage. " +
                "If the Mystery is at Stage 1, it is removed entirely. This is the most powerful counter " +
                "to Narrath's influence, but it is slow and can only address one location at a time. " +
                "Only the Chosen One can perform this act.";
        }

        public override double getPriority()
        {
            if (mystery == null) return 0;
            return mystery.stage * 30.0; // High priority for Chosen One
        }

        public override int getComplexity()
        {
            return 80; // Expensive
        }

        public override string getRestriction()
        {
            return "Only the Chosen One can speak the Counter-Word.";
        }

        public override bool canBeUndertakenBy(Person person)
        {
            return person != null && person.isChosenOne;
        }

        public override void onComplete(Person hero)
        {
            if (mystery == null || hero == null) return;
            if (mystery.location == null) return;

            // Reduce Mystery by 1 stage
            mystery.stage--;

            // Reset investigation progress for the current stage
            mystery.investigationProgress = 0;

            if (mystery.stage <= 0)
            {
                // Remove the Mystery entirely
                mystery.location.properties.Remove(mystery);
            }
        }
    }
}
