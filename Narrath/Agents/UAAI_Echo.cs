using System;
using System.Collections.Generic;
using Assets.Code;
using CommunityLib;

namespace ShadowsNarrath
{
    public class UAAI_Echo : AIChallenge
    {
        public UA_Echo echo;

        public UAAI_Echo(UA_Echo agent) : base(agent.map)
        {
            this.echo = agent;
        }

        public override ChallengeData evaluate(List<Challenge> challenges, UA agent)
        {
            // Echo doesn't use challenges — it just moves randomly
            // Movement is handled in UA_Echo.turnTick()
            return null;
        }
    }
}
