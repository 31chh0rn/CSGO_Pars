using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    class MatchData
    {
        private DateTime matchTime;
        private string map;
        private long team1;
        private long team2;

        public DateTime MatchTime
        {
            get { return matchTime; }
            set { matchTime = value; }
        }

        public string Map
        {
            get { return map; }
            set { map = value; }
        }

        public long Team1
        {
            get { return team1; }
            set { team1 = value; }
        }

        public long Team2
        {
            get { return team2; }
            set { team2 = value; }
        }
    }
}
