using System;
using System.Collections.Generic;
using Assets.Code;
using CommunityLib;

namespace ShadowsNarrath
{
    public class UAAI_Archivist : AIChallenge
    {
        public UA_Archivist archivist;

        public UAAI_Archivist(UA_Archivist agent) : base(agent.map)
        {
            this.archivist = agent;
        }

        public override ChallengeData evaluate(List<Challenge> challenges, UA agent)
        {
            ChallengeData best = null;
            double bestUtility = double.MinValue;

            foreach (Challenge ch in challenges)
            {
                double utility = 0;

                if (ch is Ch_PlantMystery)
                {
                    utility = EvaluatePlantMystery(ch.location);
                }
                else if (ch is Ch_AnnotateMystery)
                {
                    utility = EvaluateAnnotateMystery(ch.location);
                }

                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    best = new ChallengeData { challenge = ch, utility = utility };
                }
            }

            return best;
        }

        private double EvaluatePlantMystery(Location loc)
        {
            if (loc == null) return -100;

            // Check if location already has a Mystery
            foreach (Property pr in loc.properties)
            {
                if (pr is Property_Mystery) return -100;
            }

            // Check infiltration
            if (!loc.isInfiltrated) return -100;

            double utility = 50;

            // Prefer locations with large settlements
            if (loc.settlement != null)
            {
                utility += loc.settlement.population / 100.0;
            }

            // Prefer locations near heroes (they'll investigate)
            foreach (Unit u in loc.units)
            {
                if (u != null && u.person != null && u.person.isHero())
                {
                    utility += 20;
                }
            }

            // Prefer locations near libraries/arcane sites
            if (loc.settlement != null && loc.settlement.isHoly)
            {
                utility += 15;
            }

            return utility;
        }

        private double EvaluateAnnotateMystery(Location loc)
        {
            if (loc == null) return -100;

            Property_Mystery mystery = null;
            foreach (Property pr in loc.properties)
            {
                if (pr is Property_Mystery pm)
                {
                    mystery = pm;
                    break;
                }
            }

            if (mystery == null) return -100;

            // Already boosted
            if (mystery.annotationBoostTurns > 0) return -50;

            double utility = 30;

            // Higher stage = more valuable to boost
            utility += mystery.stage * 10;

            return utility;
        }

        public override bool shouldAvoid(Location loc, UA agent)
        {
            // Avoid non-Seeker heroes and the Chosen One (unless Seeker)
            foreach (Unit u in loc.units)
            {
                if (u == null || u.person == null) continue;
                if (u == agent) continue;

                if (u.person.isHero())
                {
                    if (Kernel_Narrath.instance == null) return true;
                    if (!Kernel_Narrath.instance.IsSeeker(u.person))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
