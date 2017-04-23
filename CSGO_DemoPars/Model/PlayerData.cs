using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    class PlayerData
    {
        private string playerName;
        private long steamID;

        public string PlayerName
        {
            get { return playerName; }
            set { playerName = value; }
        }

        public long SteamID
        {
            get { return steamID; }
            set { steamID = value; }
        }
    }
}
