using System;
using Assets.Code;

namespace ShadowsNarrath
{
    public class Q_InvestigateMystery : Quest
    {
        public Property_Mystery mystery;

        public Q_InvestigateMystery(Location loc, Property_Mystery mystery) : base(loc)
        {
            this.mystery = mystery;
        }

        public override string getName()
        {
            switch (mystery.stage)
            {
                case 1: return "Investigate the Oddity";
                case 2: return "Decipher the Pattern";
                case 3: return "Confront the Revelation";
                case 4: return "Cross the Threshold";
                case 5: return "Face the Mouth";
                default: return "Investigate Mystery";
            }
        }

        public override string getDescription()
        {
            switch (mystery.stage)
            {
                case 1:
                    return "Strange occurrences have been reported — inexplicable inscriptions, sounds at the edge of hearing. " +
                        "A hero should investigate and determine the source.";
                case 2:
                    return "A disturbing pattern has emerged. The inscriptions form a coherent but incomplete message. " +
                        "Scholars are needed to decode what is written — and what is missing.";
                case 3:
                    return "The Revelation is upon us. Those who have studied the pattern report visions and understanding " +
                        "that comes at terrible cost. A powerful hero must confront this knowledge directly.";
                case 4:
                    return "The Threshold has been breached. Reality itself seems thin at this location. " +
                        "Only the most capable investigators should approach — the danger is extreme.";
                case 5:
                    return "The Mouth speaks endlessly in half-words. Those who enter cannot leave. " +
                        "This is no longer an investigation — it is a last stand.";
                default:
                    return "Something requires investigation.";
            }
        }

        public override double getPriority()
        {
            if (mystery == null) return 0;
            return mystery.GetQuestPriority();
        }

        public override int getComplexity()
        {
            if (mystery == null) return 30;
            return mystery.GetQuestComplexity();
        }

        public override bool isComplete()
        {
            // Check if Mystery still exists
            if (mystery == null || mystery.location == null) return true;

            bool stillExists = false;
            foreach (Property pr in mystery.location.properties)
            {
                if (pr == mystery)
                {
                    stillExists = true;
                    break;
                }
            }

            return !stillExists;
        }

        public override void onComplete(Person hero)
        {
            if (mystery == null || hero == null) return;
            if (Kernel_Narrath.instance == null) return;

            // Add investigation progress based on hero's Lore stat
            int lore = hero.stat_lore + Kernel_Narrath.instance.GetStatModifier(hero, "lore");
            int progress = getComplexity() * (Math.Max(1, lore) / 3);
            mystery.AddInvestigationProgress(progress);

            // Grant Fragments based on Mystery stage at time of completion
            int fragmentGain = mystery.stage; // +1 per stage, minimum Fragment 1
            int currentFragment = Kernel_Narrath.instance.GetFragmentLevel(hero);

            // Chosen One immunity to Stage 1 and Stage 2 investigation
            if (hero.isChosenOne && mystery.stage <= 2)
            {
                return;
            }

            if (currentFragment < fragmentGain)
            {
                Kernel_Narrath.instance.SetFragmentLevel(hero, fragmentGain);
            }
            else
            {
                // Even if already at a higher fragment level, investigating still has effect
                // For Seekers: investigating a Stage 3+ Mystery always grants +1
                if (mystery.stage >= 3 && Kernel_Narrath.instance.IsSeeker(hero))
                {
                    Kernel_Narrath.instance.IncrementFragment(hero);
                }
            }

            // Stage 3 advancement: hero who causes it becomes a Seeker
            if (mystery.stage >= 3 && currentFragment < 3)
            {
                Kernel_Narrath.instance.SetFragmentLevel(hero, 3);
            }
        }
    }
}
