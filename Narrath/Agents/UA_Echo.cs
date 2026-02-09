using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;

namespace ShadowsNarrath
{
    public class UA_Echo : UA
    {
        public UA_Echo(Location loc, Overmind overmind) : base(loc, overmind)
        {
            this.person = new Person(loc.map);
            this.person.unit = this;
            this.person.name = "The Echo";
            this.person.stat_lore = 1;
            this.person.stat_intrigue = 1;
            this.person.stat_command = 1;
            this.person.stat_might = 1;

            this.hp = 1;
            this.maxHp = 1;
            this.profileScore = 10; // High profile — conspicuous
            this.menaceScore = 2;    // Low menace — doesn't register as dangerous
        }

        public override string getName()
        {
            return "The Echo";
        }

        public override string getDescription()
        {
            return "A wandering fragment of Narrath's utterance given form. The Echo moves randomly, " +
                "whispering half-words that linger in the minds of those nearby. It cannot fight, " +
                "cannot hide, cannot act — it simply exists, spreading fragments of the incomplete " +
                "sentence to all who encounter it. Heroes may try to 'rescue' or 'investigate' " +
                "this whispering figure, exposing themselves to its influence.";
        }

        public override Sprite getPortrait()
        {
            return EventManager.getImg("ShadowsNarrath.agent_echo.png");
        }

        public override void turnTick(Map map)
        {
            base.turnTick(map);

            if (this.location == null) return;

            // Passive effects at current location
            ApplyPassiveEffects(map);

            // Random movement
            MoveRandomly();
        }

        private void ApplyPassiveEffects(Map map)
        {
            if (Kernel_Narrath.instance == null) return;

            Location loc = this.location;

            // All heroes/rulers present gain Fragment 1 exposure (15% chance)
            foreach (Unit u in loc.units)
            {
                if (u == null || u.person == null || u == this) continue;

                if (Eleven.random.NextDouble() < 0.15)
                {
                    int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                    if (current < 1)
                    {
                        Kernel_Narrath.instance.SetFragmentLevel(u.person, 1);
                    }
                }
            }

            // If at a location with a Mystery: advance by +10 investigation progress
            foreach (Property pr in loc.properties)
            {
                if (pr is Property_Mystery mystery)
                {
                    mystery.AddInvestigationProgress(10);
                    break;
                }
            }

            // If at a location with a Seeker: Seeker gains +1 Fragment
            foreach (Unit u in loc.units)
            {
                if (u == null || u.person == null || u == this) continue;
                if (Kernel_Narrath.instance.IsSeeker(u.person))
                {
                    Kernel_Narrath.instance.IncrementFragment(u.person);
                }
            }
        }

        private void MoveRandomly()
        {
            if (this.location == null || this.location.links == null) return;
            if (this.location.links.Count == 0) return;

            int index = Eleven.random.Next(this.location.links.Count);
            Link link = this.location.links[index];
            Location target = link.other(this.location);

            if (target != null)
            {
                this.location.units.Remove(this);
                this.location = target;
                target.units.Add(this);
            }
        }

        public override void die(Map map, string reason)
        {
            // On death: location gains Stage 1 Mystery, killing hero gains Fragment 1
            if (this.location != null)
            {
                bool hasMystery = false;
                foreach (Property pr in this.location.properties)
                {
                    if (pr is Property_Mystery)
                    {
                        hasMystery = true;
                        break;
                    }
                }

                if (!hasMystery && Kernel_Narrath.narrath != null)
                {
                    Property_Mystery mystery = new Property_Mystery(this.location, Kernel_Narrath.narrath);
                    this.location.properties.Add(mystery);
                }
            }

            // Find the killer and give them Fragment 1
            // The killer is typically the last unit that attacked this unit
            foreach (Unit u in this.location.units)
            {
                if (u != null && u.person != null && u.person.isHero() && u != this)
                {
                    if (Kernel_Narrath.instance != null)
                    {
                        int current = Kernel_Narrath.instance.GetFragmentLevel(u.person);
                        if (current < 1)
                        {
                            Kernel_Narrath.instance.SetFragmentLevel(u.person, 1);
                        }
                    }
                    break;
                }
            }

            // Set respawn timer
            if (Kernel_Narrath.instance != null)
            {
                Kernel_Narrath.instance.echoAlive = false;
                Kernel_Narrath.instance.echoRespawnTimer = 20;
            }

            base.die(map, reason);
        }
    }
}
