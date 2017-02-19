using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using DemoInfo;
using System.IO.MemoryMappedFiles;
using System.Threading;
using MySql.Data.MySqlClient;

namespace CSGO_DemoPars
{
    class Program
    {
        static void Main(string[] args)
        {
            // First, check wether the user needs assistance:
            if (args.Length == 0 || args[0] == "--help")
            {
                PrintHelp();
                return;
            }

            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./Test.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);


            //placeholder variables for database inputs that are not in the demo
            DateTime matchTime = DateTime.Now;

            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(args[0]);
            

            try
            {
                files = root.GetFiles("*.dem");
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }


            // Parallel approach as reading different files are independent tasks
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, file =>
            {
                matchTime.AddHours(1);
                long matchID = 0, ctID = 0, tID = 0;
                bool hasMatchStarted = false;
                int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0, round = 0, ctScore = 0, tScore = 0;
                float ctWay = 0, tWay = 0, roundStartTime = 0, roundEndTime = 0, plantTime = 0, defuseTime = 0;
                List<Player> players = new List<Player>(), tTeam = new List<Player>(), ctTeam = new List<Player>();
                string MyConnectionString = "Server=Localhost;Database=csgo_pars;Uid=root;Pwd=root;";
                MySqlConnection connection = new MySqlConnection(MyConnectionString);
                MySqlCommand playersCmd, roundsCmd, killsCmd, matchesCmd, tCmd, ctCmd, grenadeCmd;
                connection.Open();
                try
                {
                    //Setup tabele specific command strings for ez use later on
                    playersCmd = connection.CreateCommand();
                    playersCmd.CommandText = @"insert into players (playerID, steamID, playerName) 
                                                            values (@playerID, @matchID, @playerName)";

                    roundsCmd = connection.CreateCommand();
                    roundsCmd.CommandText = "insert into rounds (matchID, roundStart, roundNumber, ctID, tID, roundEnd, winnerID, bombPlant, bombDefuse) values (";

                    killsCmd = connection.CreateCommand();
                    killsCmd.CommandText = "insert into kills (matchID, roundID, killerID, killedID, assisterID, roundTime) values ";

                    matchesCmd = connection.CreateCommand();
                    matchesCmd.CommandText = @"insert into matches (matchDate, map) 
                                                            values (@matchDate, @map)";

                    playersCmd = connection.CreateCommand();
                    playersCmd.CommandText = @"insert into players (steamID, playerName)
                                               select * from ( select @steamID, @playerName) as tmp
                                               where not exists( select steamID from players where steamID = @steamID ) limit 1";

                    tCmd = connection.CreateCommand();
                    tCmd.CommandText = @"insert into team (player1ID, player2ID, player3ID, player4ID, player5ID, teamName) 
                                              select * from ( select @player1ID, @player2ID, @player3ID, @player4ID, @player5ID, @teamName) as tmp
                                              where not exists( select player1ID, player2ID, player3ID, player4ID, player5ID, teamName 
                                                               from team where player1ID = @player1ID and player2ID = @player2ID and player3ID = @player3ID and player4ID = @player4ID and player5ID = @player5ID and teamName = @teamName ) limit 1";

                    ctCmd = connection.CreateCommand();
                    ctCmd.CommandText = @"insert into team (player1ID, player2ID, player3ID, player4ID, player5ID, teamName) 
                                              select * from ( select @player1ID, @player2ID, @player3ID, @player4ID, @player5ID, @teamName) as tmp
                                              where not exists( select player1ID, player2ID, player3ID, player4ID, player5ID, teamName 
                                                               from team where player1ID = @player1ID and player2ID = @player2ID and player3ID = @player3ID and player4ID = @player4ID and player5ID = @player5ID and teamName = @teamName ) limit 1";

                    grenadeCmd = connection.CreateCommand();
                    grenadeCmd.CommandText = @"insert into team (roundID, thrownByPlayerID, type, throwTime) 
                                                         values (@roundID, @thrownByPlayerID, @type, @throwTime)";




                    // Okay, first we need to initalize a demo-parser
                    // Mapping the complete file into RAM to reduce IO latency (should still be viable with 8 threads not shure if IO can keep up)
                    using (var mmf = MemoryMappedFile.CreateFromFile(file.FullName))
                    using (var memoryStream = mmf.CreateViewStream())
                    {
                        // By using "using" we make sure that the memoryStream is properly disposed
                        // the same goes for the DemoParser which NEEDS to be disposed (else it'll
                        // leak memory and kittens will die. 


                        // TODO: Setup Python crawler to name files with match date and time at least.

                        //matchesCmd.Parameters.AddWithValue("@matchDate", file.Name);
                        matchesCmd.Parameters.AddWithValue("@matchDate", matchTime);

                        using (var parser = new DemoParser(memoryStream))
                        {
                            // So now we've initialized a demo-parser. 
                            // let's parse the head of the demo-file to get which map the match is on!
                            // this is always the first step you need to do.
                            parser.ParseHeader();

                            // and now, do some magic: grab the match!
                            string map = parser.Map;
                            matchesCmd.Parameters.AddWithValue("@map", map);
                            //matchesCmd.Parameters.Add("@matchID", MySqlDbType.Int32, 11).Direction = System.Data.ParameterDirection.Output;
                            matchesCmd.ExecuteNonQuery();
                            matchID = matchesCmd.LastInsertedId;

                            /*
                                // And now, generate the filename of the resulting file
                                string outputFileName = file.Name + "." + map + ".csv";
                                // and open it. 
                                var outputStream = new StreamWriter(outputFileName);

                                //And write a header so you know what is what in the resulting file
                                outputStream.WriteLine(GenerateCSVHeader());
                            */

                            // Cool! Now let's get started generating the analysis-data. 

                            //Let's just declare some stuff we need to remember

                            // Here we'll save how far a player has travelled each round. 
                            // Here we remember wether the match has started yet. 
                           


                            // Since most of the parsing is done via "Events" in CS:GO, we need to use them. 
                            // So you bind to events in C# as well. 
                            // AFTER we have bound the events, we start the parser!
                            parser.MatchStarted += (sender, e) =>
                            {
                                hasMatchStarted = true;
                                
                                //get teams
                                players = parser.PlayingParticipants.ToList();
                                tTeam = players.Where(a => a.Team == Team.Terrorist).ToList();
                                ctTeam = players.Where(a => a.Team == Team.CounterTerrorist).ToList();

                                //fill player table 
                                foreach (var p in players)
                                {
                                    playersCmd.Parameters.AddWithValue("@steamID", p.SteamID);
                                    playersCmd.Parameters.AddWithValue("@playerName", p.Name);
                                    Console.WriteLine("add player :" + p.SteamID.ToString() + " = " + p.Name);
                                    playersCmd.ExecuteNonQuery();
                                    playersCmd.Parameters.Clear();
                                }
                                //sort teams to unify teamcomposition in SQL => Player1 will always be the same player if the team is identical etc.
                                //TODO: steamID as PK? is there a way to put muliple steam IDs under a different foreign key?
                                tTeam.OrderBy(Player => Player.SteamID);
                                tCmd.Parameters.AddWithValue("@player1ID", tTeam[0].SteamID);
                                tCmd.Parameters.AddWithValue("@player2ID", tTeam[1].SteamID);
                                tCmd.Parameters.AddWithValue("@player3ID", tTeam[2].SteamID);
                                tCmd.Parameters.AddWithValue("@player4ID", tTeam[3].SteamID);
                                tCmd.Parameters.AddWithValue("@player5ID", tTeam[4].SteamID);
                                tCmd.Parameters.AddWithValue("@teamName", parser.TClanName);
                                //teamCmd.Parameters.AddWithValue("@teamName", tTeam.GroupBy(player => player.AdditionaInformations.Clantag).OrderByDescending(group => group.Count()).Select(p => p.Key).First());       //finds the most common Clantag of all the teams players
                                tCmd.ExecuteNonQuery();
                                tID = tCmd.LastInsertedId;

                                ctTeam.OrderBy(Player => Player.SteamID);
                                ctCmd.Parameters.AddWithValue("@player1ID", ctTeam[0].SteamID);
                                ctCmd.Parameters.AddWithValue("@player2ID", ctTeam[1].SteamID);
                                ctCmd.Parameters.AddWithValue("@player3ID", ctTeam[2].SteamID);
                                ctCmd.Parameters.AddWithValue("@player4ID", ctTeam[3].SteamID);
                                ctCmd.Parameters.AddWithValue("@player5ID", ctTeam[4].SteamID);
                                ctCmd.Parameters.AddWithValue("@teamName", parser.CTClanName);
                                //teamCmd.Parameters.AddWithValue("@teamName", ctTeam.GroupBy(player => player.AdditionaInformations.Clantag).OrderByDescending(group => group.Count()).Select(p => p.Key).First());       //finds the most common Clantag of all the teams players
                                ctCmd.ExecuteNonQuery();
                                ctID = ctCmd.LastInsertedId;
                                Console.WriteLine("matchstart");
                               

                                //Okay let's output who's really in this game!
                                /*
                                    Console.WriteLine("Participants: ");
                                    Console.WriteLine("  Terrorits \"{0}\": ", parser.CTClanName);
                                    foreach (var player in parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist))
                                        Console.WriteLine("    {0} {1} (Steamid: {2})", player.AdditionaInformations.Clantag, player.Name, player.SteamID);

                                    Console.WriteLine("  Counter-Terrorits \"{0}\": ", parser.TClanName);
                                    foreach (var player in parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist))
                                    Console.WriteLine("    {0} {1} (Steamid: {2})", player.AdditionaInformations.Clantag, player.Name, player.SteamID);



                                    // Okay, problem: At the end of the demo
                                    // a player might have already left the game,
                                    // so we need to store some information
                                    // about the players before they left :)
                                    ingame.AddRange(parser.PlayingParticipants);
                                */
                            };

                            //TODO: Get real Match start (chat search for game is live?)
                            parser.RoundStart += (sender, e) =>
                            {
                                if (!hasMatchStarted)
                                    return;
                                
                                round++;
                                roundStartTime = parser.CurrentTime;

                                //@matchID, @roundStart, @roundNumber, @ctID, @tID, @roundEnd, @WinTeam, @bombPlant, @bombDefuse
                                roundsCmd.CommandText += matchID.ToString() + ", ";
                                roundsCmd.CommandText += roundStartTime.ToString() + ", ";
                                roundsCmd.CommandText += round.ToString() + ", ";
                                roundsCmd.CommandText += ctID.ToString() + ", ";
                                roundsCmd.CommandText += tID.ToString() + ", ";


                                //How much money had each team at the start of the round?
                                ctStartroundMoney = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
                                tStartroundMoney = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);

                                //And how much they did they save from the last round?
                                ctSaveAmount = parser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
                                tSaveAmount = parser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);

                            };

                            parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                            {
                                if (!hasMatchStarted)
                                    return;

                                //the killer is null if you're killed by the world - eg. by falling
                                if (e.Killer != null && e.Victim != null)
                                {

                                    
                                    // Check for players that werde not ingame when the game started.
                                    if (!players.Contains(e.Killer))
                                    {
                                        Console.WriteLine("missing" + e.Killer.Name + " : " + e.Killer.SteamID);
                                        players.Add(e.Killer);
                                        playersCmd.Parameters.AddWithValue("@steamID", e.Killer.SteamID);
                                        playersCmd.Parameters.AddWithValue("@playerName", e.Killer.Name);
                                        playersCmd.ExecuteNonQuery();
                                        playersCmd.Parameters.Clear();
                                    }

                                    if (!players.Contains(e.Victim))
                                    {
                                        Console.WriteLine("missing" + e.Victim.Name + " : " + e.Victim.SteamID);
                                        players.Add(e.Victim);
                                        playersCmd.Parameters.AddWithValue("@steamID", e.Victim.SteamID);
                                        playersCmd.Parameters.AddWithValue("@playerName", e.Victim.Name);
                                        playersCmd.ExecuteNonQuery();
                                        playersCmd.Parameters.Clear();
                                    }

                                   
                                    if (e.Assister != null && !players.Contains(e.Assister))
                                    {
                                        Console.WriteLine("missing" + e.Assister.Name + " : " + e.Assister.SteamID);
                                        players.Add(e.Assister);
                                        playersCmd.Parameters.AddWithValue("@steamID", e.Assister.SteamID);
                                        playersCmd.Parameters.AddWithValue("@playerName", e.Assister.Name);
                                        playersCmd.ExecuteNonQuery();
                                        playersCmd.Parameters.Clear();
                                    }

                                    //@matchid, @roundid, @killerid, @killedid, @assisterid, @roundtime
                                    float roundTime = parser.CurrentTime - roundStartTime;
                                    if (e.Assister == null)
                                        killsCmd.CommandText += "(" + matchID.ToString() + ", " + round.ToString() + ", " + e.Killer.SteamID.ToString() + ", " + e.Victim.SteamID.ToString() + ", 0, " + roundTime.ToString() + "), ";
                                    else
                                        killsCmd.CommandText += "(" + matchID.ToString() + ", " + round.ToString() + ", " + e.Killer.SteamID.ToString() + ", " + e.Victim.SteamID.ToString() + ", " + e.Assister.SteamID.ToString() + ", " + roundTime.ToString() + "), ";
                               }
                            };

                            parser.FreezetimeEnded += (sender, e) =>
                            {
                                if (!hasMatchStarted)
                                    return;

                                // At the end of the freezetime (when players can start walking)
                                // calculate the equipment value of each team!
                                ctEquipValue = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.CurrentEquipmentValue);
                                tEquipValue = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.CurrentEquipmentValue);
                            };


                            parser.BombPlanted += (sender, e) =>
                            {
                                if (!hasMatchStarted)
                                    return;

                                plantTime = parser.CurrentTime;
                            };

                            parser.BombDefused += (sender, e) =>
                            {
                                if (!hasMatchStarted)
                                    return;
                                defuseTime = parser.CurrentTime;
                            };

                            parser.FlashNadeExploded += (sender, e) =>
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


                            parser.PlayerHurt += (sender, e) =>
                            {
                                /*
                                if (e.Weapon.Weapon.ToString() == "      HE")
                                    //Console.WriteLine(e.Weapon.Weapon.ToString());
                                */
                            };

                            parser.TickDone += (sender, e) =>
                            {
                                if (!hasMatchStarted)
                                    return;
                            };

                            //So now lets do some fancy output
                            parser.RoundEnd += (sender, e) =>
                            {
                                roundEndTime = parser.CurrentTime;
                                ctScore = parser.CTScore;
                                tScore = parser.TScore;
                                //Console.WriteLine("round {0} ended", round);
                                if (!hasMatchStarted || round < 1)
                                    return;
                                 
                                roundsCmd.CommandText += roundEndTime + ", ";

                                if (parser.CTScore > ctScore)
                                    roundsCmd.CommandText += ctID.ToString() + ", ";
                                else
                                    roundsCmd.CommandText += tID.ToString() + ", ";

                                roundsCmd.CommandText += plantTime.ToString() + ", ";
                                roundsCmd.CommandText += defuseTime.ToString() + ");";
                                roundsCmd.ExecuteNonQuery();
                                roundsCmd.CommandText = "insert into rounds (matchID, roundStart, roundNumber, ctID, tID, roundEnd, winnerID, bombPlant, bombDefuse) values (";

                                killsCmd.CommandText = killsCmd.CommandText.Remove(killsCmd.CommandText.Length - 2, 2) + ";";
                                killsCmd.ExecuteNonQuery();
                                killsCmd.CommandText = "insert into kills (matchID, roundID, killerID, killedID, assisterID, roundTime) values";

                                ctCmd.Parameters.Clear();
                                tCmd.Parameters.Clear();
                                matchesCmd.Parameters.Clear();
                                playersCmd.Parameters.Clear();
                                roundsCmd.Parameters.Clear();
                                // We do this in a method-call since we'd else need to duplicate code
                                // The much parameters are there because I simply extracted a method
                                // Sorry for this - you should be able to read it anywys :)
                                //PrintRoundResults(parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);
                            };

                            parser.WinPanelMatch += (sender, e) =>
                            {
                                Console.WriteLine("match ended");
                                hasMatchStarted = false;
                            };
                            //Now let's parse the demo!
                            parser.ParseToEnd();

                        
                        //And output the result of the last round again. 
                        // PrintRoundResults(parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);



                        //Lets just display an end-game-scoreboard!

                        //Console.WriteLine("Finished! Results: ");
                        //Console.WriteLine("  Terrorits \"{0}\": ", parser.CTClanName);

                        //foreach (var player in ingame.Where(a => a.Team == Team.Terrorist))
                        //    Console.WriteLine(
                        //        "    {0} {1} (Steamid: {2}): K: {3}, D: {4}, A: {5}",
                        //        player.AdditionaInformations.Clantag,
                        //        player.Name, player.SteamID,
                        //        player.AdditionaInformations.Kills,
                        //        player.AdditionaInformations.Deaths,
                        //        player.AdditionaInformations.Assists
                        //    );

                        //Console.WriteLine("  Counter-Terrorits \"{0}\": ", parser.TClanName);
                        //foreach (var player in ingame.Where(a => a.Team == Team.CounterTerrorist))
                        //    Console.WriteLine(
                        //        "    {0} {1} (Steamid: {2}): K: {3}, D: {4}, A: {5}",
                        //        player.AdditionaInformations.Clantag,
                        //        player.Name, player.SteamID,
                        //        player.AdditionaInformations.Kills,
                        //        player.AdditionaInformations.Deaths,
                        //        player.AdditionaInformations.Assists
                        //    );

                        //outputStream.Close();

                        }
                    }
                }



                catch (Exception)
                {
                    Console.SetOut(oldOut);
                    writer.Close();
                    ostrm.Close();
                    Console.WriteLine("Done");
                    throw;
                }
                finally
                {
                   

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            });
        }


        static string GenerateCSVHeader()
        {
            return string.Format(
                "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};",
                "Round-Number", // parser.CTScore + parser.TScore, //Round-Number
                "CT-Score", // parser.CTScore,
                "T-Score", // parser.TScore,
                           //how many CTs are still alive?
                "SurvivingCTs", // parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.CounterTerrorist),
                                //how many Ts are still alive?
                "SurvivingTs", // parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.Terrorist),
                "CT-StartMoney", // ctStartroundMoney,
                "T-StartMoney", // tStartroundMoney,
                "CT-EquipValue", // ctEquipValue,
                "T-EquipValue", // tEquipValue,
                "CT-SavedFromLastRound", // ctSaveAmount,
                "T-SavedFromLastRound", // tSaveAmount,
                "WalkedCTWay", // ctWay,
                "WalkedTWay", // tWay,
                              //The kills of all CTs so far
                "CT-Kills", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Kills),
                "T-Kills", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Kills),
                           //The deaths of all CTs so far
                "CT-Deaths", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Deaths),
                "T-Deaths", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Deaths),
                            //The assists of all CTs so far
                "CT-Assists", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Assists),
                "T-Assists", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Assists),
                "BombPlanted", // plants,
                "BombDefused", // defuses,
                "TopfraggerName", // "\"" + topfragger.Key.Name + "\"", //The name of the topfragger this round
                "TopfraggerSteamid", // topfragger.Key.SteamID, //The steamid of the topfragger this round
                "TopfraggerKillsThisRound" // topfragger.Value //The amount of kills he got
            );
        }

        static void PrintHelp()
        {
            string fileName = Path.GetFileName((Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("CS:GO Demo-Statistics-Generator");
            Console.WriteLine("http://github.com/moritzuehling/demostatistics-creator");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Usage: {0} [--help] [--scoreboard] file1.dem [file2.dem ...]", fileName);
            Console.WriteLine("--help");
            Console.WriteLine("    Displays this help");
            Console.WriteLine("--frags");
            Console.WriteLine("    Displays only the frags happening in this demo in the format. Cannot be used with anything else. Only works with 1 file. ");
            Console.WriteLine("    Output-format (example): <Hyper><76561198014874496><CT> + <centralize><76561198085059888><CT> [M4A1][HS][Wall] <percy><76561197996475850><T>");
            Console.WriteLine("--scoreboard");
            Console.WriteLine("    Displays only the scoreboards on every round_end event. Cannot be used with anything else. Only works with 1 file. ");

            Console.WriteLine("file1.dem");
            Console.WriteLine("    Path to a demo to be parsed. The resulting file with have the same name, ");
            Console.WriteLine("    except that it'll end with \".dem.[map].csv\", where [map] is the map.");
            Console.WriteLine("    The resulting file will be a CSV-File containing some statistics generated");
            Console.WriteLine("    by this program, and can be viewed with (for example) LibreOffice");
            Console.WriteLine("[file2.dem ...]");
            Console.WriteLine("    You can specify more than one file at a time.");




        }

        static void PrintRoundResults(DemoParser parser, StreamWriter outputStream, int ctStartroundMoney, int tStartroundMoney, int ctEquipValue, int tEquipValue, int ctSaveAmount, int tSaveAmount, float ctWay, float tWay, int defuses, int plants, Dictionary<Player, int> killsThisRound)
        {
            //okay, get the topfragger:
            var topfragger = killsThisRound.OrderByDescending(x => x.Value).FirstOrDefault();
            if (topfragger.Equals(default(KeyValuePair<Player, int>)))
                topfragger = new KeyValuePair<Player, int>(new Player(), 0);
            //At the end of each round, let's write down some statistics!
            outputStream.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};", parser.CTScore + parser.TScore, //Round-Number
            parser.CTScore, parser.TScore, //how many CTs are still alive?
            parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.CounterTerrorist), //how many Ts are still alive?
            parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.Terrorist), ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, //The kills of all CTs so far
            parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Kills), parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Kills), //The deaths of all CTs so far
            parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Deaths), parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Deaths), //The assists of all CTs so far
            parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Assists), parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Assists), plants, defuses, "\"" + topfragger.Key.Name + "\"", //The name of the topfragger this round
            topfragger.Key.SteamID, //The steamid of the topfragger this round
            topfragger.Value//The amount of kills he got
            ));
        }
    }
}
