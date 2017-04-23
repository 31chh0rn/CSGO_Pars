using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    class RoundData
    {
        private long matchID;
        private float roundStartTime;
        private float roundEndTime;
        private int round;
        private long ctID;
        private long tID;
        private float plantTime;
        private float defuseTime;
        private char plantedSite;
        private long winner;

        public void newRound()
        {
            roundStartTime = 0;
            plantTime = 0;
            defuseTime = 0;
            winner = 0;
        }

        public long getWinnerID()
        {
            return winner;
        }

        public long MatchID
        {
            get { return matchID; }
            set { matchID = value; }
        }

        public float RoundStartTime
        {
            get { return roundStartTime; }
            set { roundStartTime = value; }
        }

        public float RoundEndTime
        {
            get { return roundEndTime; }
            set { roundEndTime = value; }
        }

        public int Round
        {
            get { return round; }
            set { round = value; }
        }

        public long CtID
        {
            get { return ctID; }
            set { ctID = value; }
        }

        public long TID
        {
            get { return tID; }
            set { tID = value; }
        }

        public float PlantTime
        {
            get { return plantTime; }
            set { plantTime = value; }
        }

        public float DefuseTime
        {
            get { return defuseTime; }
            set { defuseTime = value; }
        }

        public char PlantedSite
        {
            get { return plantedSite; }
            set { plantedSite = value; }
        }

        public DemoInfo.Team Winner
        {
            get {
                if (winner == ctID)
                    return DemoInfo.Team.CounterTerrorist;
                else if (winner == tID)
                    return DemoInfo.Team.Terrorist;
                else return DemoInfo.Team.Spectate;
            }
            set {
                if (value == DemoInfo.Team.CounterTerrorist)
                    winner = ctID;
                else if (value == DemoInfo.Team.Terrorist)
                    winner = tID;
            }
        }
    }
}
