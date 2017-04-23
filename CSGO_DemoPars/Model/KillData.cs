using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    //killsCmd.CommandText += "(" + matchID.ToString() + ", " + round.ToString() + ", " + e.Killer.SteamID.ToString() + ", " + e.Victim.SteamID.ToString() + ", 0, " + roundTime.ToString() + "), ";
    class KillData
    {
        private long matchID;
        private long roundID;
        private long killerID;
        private long victimID;
        private long assisterID;
        private float roundTime;

        public long MatchID
        {
            get { return matchID; }
            set { matchID = value; }
        }

        public long RoundID
        {
            get {
                return roundID;
            }

            set {
                roundID = value;
            }
        }

        public long KillerID
        {
            get {
                return killerID;
            }

            set {
                killerID = value;
            }
        }

        public long VictimID
        {
            get {
                return victimID;
            }

            set {
                victimID = value;
            }
        }

        public long AssisterID
        {
            get {
                return assisterID;
            }

            set {
                assisterID = value;
            }
        }

        public float RoundTime
        {
            get {
                return roundTime;
            }

            set {
                roundTime = value;
            }
        }
    }
}
