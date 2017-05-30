using CSGO_DemoPars.Utils;
using DemoInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Model
{
    class PositionData
    {
        private long matchID;
        private long steamID;
        private long tick;
        private MyVector position;
        private MyVector viewDirection;

        public long MatchID
        {
            get { return matchID; }
            set { matchID = value; }
        }

        public long SteamID
        {
            get { return steamID; }
            set { steamID = value; }
        }

        public long Tick
        {
            get { return tick; }
            set { tick = value; }
        }

        public MyVector Position
        {
            get { return position; }
            set { position = value; }
        }

        public MyVector ViewDirection
        {
            get { return viewDirection; }
            set { viewDirection = value; }
        }
    }
}
