using System;
using System.Collections.Generic;
using System.Linq;
using DemoInfo;
using MySql.Data.MySqlClient;
using System.IO.MemoryMappedFiles;
using CSGO_DemoPars.Model;
using System.IO;
using CSGO_DemoPars.Utils;

namespace CSGO_DemoPars
{
    class Parser
    {
        private String fileName;
        private int reservedLine; // For console progress
        private Database database;
        private List<RoundData> roundData;
        private Dictionary<long, MyVector> position;
        private Dictionary<long, MyVector> viewDirection;

        public void parserFile(System.IO.FileInfo file, int reservedLine)
        {
            this.fileName = file.Name;
            this.reservedLine = reservedLine;
            roundData = new List<RoundData>();
            position = new Dictionary<long, MyVector>();
            viewDirection = new Dictionary<long, MyVector>();
            parserFile(file, file.CreationTime);
        }

        public void parserFile(System.IO.FileInfo file, DateTime matchTime) {
            MatchData matchData = new MatchData();

            long matchID = 0, ctID = 0, tID = 0;
            bool hasMatchStarted = false, isFreezetimeOver = false, lastRoundHalf = false;
            int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0, currentRound = 0;
            float roundStartTime = 0;
            RoundData round = new RoundData();
            List<Player> players = new List<Player>();
            List<KillData> roundKills = new List<KillData>();
            List<PositionData> positionData = new List<PositionData>();

            File.SetAttributes(file.FullName, FileAttributes.Normal);
            using (var mmf = MemoryMappedFile.CreateFromFile(file.FullName))
            using (var memoryStream = mmf.CreateViewStream())
            {
                using (var demoParser = new DemoParser(memoryStream))
                {
                    demoParser.ParseHeader();

                    matchData.Map = demoParser.Map;
                    matchData.MatchTime = matchTime;

                    demoParser.MatchStarted += (sender, e) =>
                    {
                        hasMatchStarted = true;
                        players = demoParser.PlayingParticipants.ToList();

                        foreach (var p in demoParser.PlayingParticipants.ToList())
                        {
                            PlayerData player = new PlayerData();
                            player.SteamID = p.SteamID;
                            player.PlayerName = p.Name;
                            database.insertPlayer(player);
                        }

                        Func<String, Team, long> addTeam = (String teamName, Team team) => {
                            // Generate a unique TeamName from the SteamIDs of all players
                            if (teamName == "")
                            {
                                int teamHash = 0;
                                demoParser.PlayingParticipants.Where(p => p.Team == team).ToList().ForEach(p => teamHash ^= p.SteamID.GetHashCode());
                                teamName = "Team " + teamHash.ToString();
                            }
                            TeamData teamData = new TeamData();
                            teamData.TeamName = teamName;
                            
                            return database.insertTeam(teamData);
                        };

                        //How much money had each team at the start of the round?
                        ctStartroundMoney = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
                        tStartroundMoney = demoParser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);

                        //And how much they did they save from the last round?
                        ctSaveAmount = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
                        tSaveAmount = demoParser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);

                        tID = addTeam(demoParser.TClanName, Team.Terrorist);
                        ctID = addTeam(demoParser.CTClanName, Team.CounterTerrorist);

                        matchData.Team1 = tID;
                        matchData.Team2 = ctID;
                        matchID = database.insertMatch(matchData);

                        foreach (var p in demoParser.PlayingParticipants.ToList())
                        {
                            if (p.Team == Team.CounterTerrorist)
                                database.insertPlayerTeam(matchID, ctID, p.SteamID);
                            else if (p.Team == Team.Terrorist)
                                database.insertPlayerTeam(matchID, tID, p.SteamID);
                        }

                        // RoundStart is called before MatchStarted, pull this into a extra function
                        round.newRound();
                        currentRound++;
                        roundStartTime = demoParser.CurrentTime;

                        round.CtID = ctID;
                        round.TID = tID;
                        round.MatchID = matchID;
                        round.RoundStartTime = roundStartTime;
                        round.Round = currentRound;
                        // -------------------------------------------------------------------------
                    };

                    demoParser.LastRoundHalf += (sender, e) =>
                    {
                        lastRoundHalf = true;
                    };

                    //TODO: Get real Match start (chat search for game is live?)
                    demoParser.RoundStart += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        isFreezetimeOver = false;
                        round.newRound();
                        currentRound++;
                        roundStartTime = demoParser.CurrentTime;

                        round.CtID = ctID;
                        round.TID = tID;
                        round.MatchID = matchID;
                        round.RoundStartTime = roundStartTime;
                        round.Round = currentRound;

                        //How much money had each team at the start of the round?
                        ctStartroundMoney = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
                        tStartroundMoney = demoParser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);

                        //And how much they did they save from the last round?
                        ctSaveAmount = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
                        tSaveAmount = demoParser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
                    };

                    demoParser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                    {
                        if (!hasMatchStarted || e.Killer == null || e.Victim == null)
                            return;

                        Action<Player> addPlayer = (Player p) => {
                            PlayerData player = new PlayerData();
                            player.SteamID = p.SteamID;

                            database.insertPlayer(player);
                            if (p.Team == Team.CounterTerrorist)
                                database.insertPlayerTeam(matchID, ctID, p.SteamID);
                            else if (p.Team == Team.Terrorist)
                                database.insertPlayerTeam(matchID, tID, p.SteamID);

                        };

                        // Check for players that werde not ingame when the game started.
                        if (!players.Contains(e.Killer))
                            addPlayer(e.Killer);
                        if (!players.Contains(e.Victim))
                            addPlayer(e.Victim);
                        if (e.Assister != null && !players.Contains(e.Assister))
                            addPlayer(e.Assister);

                        KillData killData = new KillData();
                        killData.MatchID = matchID;
                        killData.KillerID = e.Killer == null ? 0 : e.Killer.SteamID;
                        killData.VictimID = e.Victim == null ? 0 : e.Victim.SteamID;
                        killData.AssisterID = e.Assister == null ? 0 : e.Assister.SteamID;
                        killData.RoundTime = demoParser.CurrentTime - roundStartTime;

                        roundKills.Add(killData);
                    };

                    demoParser.FreezetimeEnded += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        isFreezetimeOver = true;
                        ctEquipValue = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.CurrentEquipmentValue);
                        tEquipValue = demoParser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.CurrentEquipmentValue);
                    };

                    demoParser.BombPlanted += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        round.PlantedSite = e.Site;
                        round.PlantTime = demoParser.CurrentTime;
                    };

                    demoParser.BombDefused += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        round.DefuseTime = demoParser.CurrentTime;
                    };

                    demoParser.FlashNadeExploded += (sender, e) =>
                    {
                        /*
                        foreach (Player player in e.FlashedPlayers)
                        {
                            if (player.Team == e.ThrownBy.Team)
                                Console.WriteLine("     Teamflashed {0}", player.Name);
                            else
                                Console.WriteLine("     flashed {0}", player.Name);
                        }
                        */
                    };

                    demoParser.PlayerHurt += (sender, e) =>
                    {
                        /*
                        if (e.Weapon.Weapon.ToString() == "      HE")
                            //Console.WriteLine(e.Weapon.Weapon.ToString());
                        */
                    };

                    demoParser.TickDone += (sender, e) =>
                    {
                        MultiProgressbar.UpdateProgress(reservedLine, fileName, demoParser.ParsingProgess);
                        
                        if (!hasMatchStarted || !isFreezetimeOver)
                            return;

                        foreach (Player p in demoParser.PlayingParticipants.ToList())
                        {
                            PositionData tickPositionData = new PositionData();
                            tickPositionData.MatchID = matchID;

                            tickPositionData.SteamID = p.SteamID;
                            tickPositionData.Tick = demoParser.CurrentTick;
                            tickPositionData.Position = new MyVector(p.Position.X, p.Position.Y, p.Position.Z);
                            tickPositionData.ViewDirection = new MyVector(p.ViewDirectionX, p.ViewDirectionY, 0);

                            if (!position.ContainsKey(p.SteamID)) {
                                position.Add(p.SteamID, new MyVector(p.Position.X, p.Position.Y, p.Position.Z));
                                positionData.Add(tickPositionData);
                            } else if (!position[p.SteamID].Equals(tickPositionData.Position)) {
                                positionData.Add(tickPositionData);
                            }

                            if (!viewDirection.ContainsKey(p.SteamID)) viewDirection.Add(p.SteamID, new MyVector(p.ViewDirectionX, p.ViewDirectionY, 0));
                            if (viewDirection[p.SteamID] == tickPositionData.ViewDirection) tickPositionData.ViewDirection = null;

                            position[p.SteamID] = tickPositionData.Position;
                        }
                    };

                    demoParser.RoundEnd += (sender, e) =>
                    {
                        if (!hasMatchStarted || currentRound < 1)
                            return;

                        round.Winner = e.Winner == Team.CounterTerrorist ? ctID : tID;
                        round.WinnerSide = e.Winner;
                        round.RoundEndTime = demoParser.CurrentTime;
                        long id = database.insertRound(round);

                        foreach (KillData kill in roundKills)
                        {
                            kill.RoundID = id;
                            database.insertKill(kill);
                        }

                        if (lastRoundHalf) {
                            long temp = ctID;
                            ctID = tID;
                            tID = temp;
                            lastRoundHalf = false;
                        }
                    };

                    demoParser.WinPanelMatch += (sender, e) =>
                    {
                        database.insertPosition(positionData);
                        positionData.Clear();
                        hasMatchStarted = false;
                    };
                    demoParser.ParseToEnd();
                }
            }
        }

        public void setDatabase(Database database)
        {
            this.database = database;
            this.database = new Database();
        }
    }
}
