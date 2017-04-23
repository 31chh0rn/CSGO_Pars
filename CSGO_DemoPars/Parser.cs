using System;
using System.Collections.Generic;
using System.Linq;
using DemoInfo;
using MySql.Data.MySqlClient;
using System.IO.MemoryMappedFiles;
using CSGO_DemoPars.Model;
using System.IO;

namespace CSGO_DemoPars
{
    class Parser
    {
        private Database database;

        public void parserFile(System.IO.FileInfo file)
        {
            parserFile(file, file.CreationTime);
        }

        public void parserFile(System.IO.FileInfo file, DateTime matchTime) {
            MatchData matchData = new MatchData();

            long matchID = 0, ctID = 0, tID = 0;
            bool hasMatchStarted = false;
            int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0, round = 0;
            float roundStartTime = 0;
            RoundData roundData = new RoundData();
            List<Player> players = new List<Player>();
            List<KillData> roundKills = new List<KillData>();

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

                        long team1ID = addTeam(demoParser.TClanName, Team.Terrorist);
                        long team2ID = addTeam(demoParser.CTClanName, Team.CounterTerrorist);

                        matchData.Team1 = team1ID;
                        matchData.Team2 = team2ID;
                        matchID = database.insertMatch(matchData);
                    };

                    //TODO: Get real Match start (chat search for game is live?)
                    demoParser.RoundStart += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        roundData.newRound();
                        round++;
                        roundStartTime = demoParser.CurrentTime;

                        roundData.MatchID = matchID;
                        roundData.RoundStartTime = roundStartTime;
                        roundData.Round = round;
                        roundData.CtID = database.getTeamByName(demoParser.CTClanName);
                        roundData.TID = database.getTeamByName(demoParser.TClanName);

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
                            player.SteamID = p.SteamID;

                            database.insertPlayer(player);
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

                        ctEquipValue = demoParser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.CurrentEquipmentValue);
                        tEquipValue = demoParser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.CurrentEquipmentValue);
                    };

                    demoParser.BombPlanted += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        roundData.PlantedSite = e.Site;
                        roundData.PlantTime = demoParser.CurrentTime;
                    };

                    demoParser.BombDefused += (sender, e) =>
                    {
                        if (!hasMatchStarted)
                            return;

                        roundData.DefuseTime = demoParser.CurrentTime;
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
                        if (!hasMatchStarted)
                            return;
                    };

                    demoParser.RoundEnd += (sender, e) =>
                    {
                        if (!hasMatchStarted || round < 1)
                            return;

                        roundData.Winner = e.Winner;
                        roundData.RoundEndTime = demoParser.CurrentTime;
                        long id = database.insertRound(roundData);

                        foreach (KillData kill in roundKills)
                        {
                            kill.RoundID = id;
                            database.insertKill(kill);
                        }
                    };

                    demoParser.WinPanelMatch += (sender, e) =>
                    {
                        Console.WriteLine("match ended");
                        hasMatchStarted = false;
                    };
                    demoParser.ParseToEnd();
                }
            }
        }
        public void setDatabase(Database database)
        {
            this.database = database;
        }
    }
}
