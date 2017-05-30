using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    class TeamData
    {
        private string teamName;
        private List<long> players;
        public string TeamName
        {
            get { return teamName; }
            set { teamName = value; }
        }

        public List<long> Players { get => players; set => players = value; }
    }
}
