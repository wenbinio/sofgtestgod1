using System;
using System.Collections.Generic;
using Assets.Code;
using CommunityLib;

namespace ShadowsNarrath
{
    public class UAAI_Amanuensis : AIChallenge
    {
        public UA_Amanuensis amanuensis;

        public UAAI_Amanuensis(UA_Amanuensis agent) : base(agent.map)
        {
            this.amanuensis = agent;
        }

        public override ChallengeData evaluate(List<Challenge> challenges, UA agent)
        {
            ChallengeData best = null;
            double bestUtility = double.MinValue;

            foreach (Challenge ch in challenges)
            {
                if (ch is Ch_Unwrite)
                {
                    double utility = EvaluateUnwrite(ch.location);
                    if (utility > bestUtility)
                    {
                        bestUtility = utility;
                        best = new ChallengeData { challenge = ch, utility = utility };
                    }
                }
            }

            return best;
        }

        private double EvaluateUnwrite(Location loc)
        {
            if (loc == null) return -100;
            if (loc.settlement == null) return -100;

            double utility = 40;

            // Prefer locations with large populations
            utility += loc.settlement.population / 50.0;

            // Prefer locations with fortifications
            if (loc.settlement.fortification > 0)
            {
                utility += 20;
            }

            return utility;
        }
    }
}
