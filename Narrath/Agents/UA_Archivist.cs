using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class UA_Archivist : UA
    {
        public UA_Archivist(Location loc, Overmind overmind) : base(loc, overmind)
        {
            this.person = new Person(loc.map);
            this.person.unit = this;
            this.person.name = "The Archivist";
            this.person.stat_lore = 4;
            this.person.stat_intrigue = 3;
            this.person.stat_command = 1;
            this.person.stat_might = 1;

            // Add traits
            this.person.traits.Add(new Trait_CuratorOfGaps());
            this.person.traits.Add(new Trait_Annotator());
            this.person.traits.Add(new Trait_KindredRecognition());

            this.hp = 20;
            this.maxHp = 20;
            this.profileScore = 2;
            this.menaceScore = 3;
        }

        public override string getName()
        {
            return "The Archivist";
        }

        public override string getDescription()
        {
            return "A scholar consumed by the pursuit of incomplete knowledge. " +
                "The Archivist plants Mysteries in the world — fragments of Narrath's utterance " +
                "inscribed in places where the curious will find them. High Lore allows effective " +
                "placement, while moderate Intrigue helps avoid detection. Seekers recognize the " +
                "Archivist as a kindred spirit and will not attack or report them.";
        }

        public override Sprite getPortrait()
        {
            return EventManager.getImg("ShadowsNarrath.agent_archivist.png");
        }

        public override List<Challenge> getChallenges()
        {
            List<Challenge> challenges = new List<Challenge>();
            challenges.Add(new Ch_PlantMystery(this.location));
            challenges.Add(new Ch_AnnotateMystery(this.location));
            return challenges;
        }
    }
}
