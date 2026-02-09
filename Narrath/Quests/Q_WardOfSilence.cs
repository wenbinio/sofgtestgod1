using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Q_WardOfSilence : Quest
    {
        public Property_Mystery mystery;

        public Q_WardOfSilence(Location loc, Property_Mystery mystery) : base(loc)
        {
            this.mystery = mystery;
        }

        public override string getName()
        {
            return "Ward of Silence";
        }

        public override string getDescription()
        {
            return "Establish a ward of silence to suppress the Mystery's progression. " +
                "Investigation progress will be halted and Fragment exposure halved while the ward is maintained. " +
                "However, the warding hero must remain stationed at the location — leaving will break the ward.";
        }

        public override double getPriority()
        {
            if (mystery == null) return 0;
            return mystery.stage * 15.0; // Lower priority than investigation
        }

        public override int getComplexity()
        {
            return 20; // Quick to establish
        }

        public override void onComplete(Person hero)
        {
            if (mystery == null || hero == null) return;
            if (mystery.location == null) return;

            // Place the ward
            Property_WardOfSilence ward = new Property_WardOfSilence(mystery.location, hero);
            mystery.location.properties.Add(ward);

            if (Kernel_Narrath.instance != null)
            {
                Kernel_Narrath.instance.activeWards[mystery.location] = hero;
            }
        }
    }
}
