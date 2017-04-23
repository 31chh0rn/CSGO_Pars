using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using CSGO_DemoPars.Model;
using System.Runtime.CompilerServices;

namespace CSGO_DemoPars
{
    class Database
    {
        private String dbName = "csgopars";
        MySqlConnection connection;
        MySqlCommand matchesCmd, roundsCmd, playersCmd, teamCmd, killsCmd, grenadeCmd;

        public Database()
        {
            connection = new MySqlConnection();
            connection.ConnectionString = ConfigurationManager.ConnectionStrings["MySqlConnectionString"].ConnectionString;
            connection.Open();

            setupDatabase();

            matchesCmd = connection.CreateCommand();
            matchesCmd.CommandText = @"INSERT IGNORE INTO matches (Date, Map, Team1, Team2) 
                                                   VALUES (@matchDate, @map, @team1ID, @team2ID)";

            playersCmd = connection.CreateCommand();
            playersCmd.CommandText = @"INSERT IGNORE INTO `players` (SteamID, Name) 
                                                   VALUES (@steamID, @playerName)";

            teamCmd = connection.CreateCommand();
            teamCmd.CommandText = @"INSERT IGNORE `teams` (Name) 
                                                   VALUES (@teamName)";

            roundsCmd = connection.CreateCommand();
            roundsCmd.CommandText = @"INSERT IGNORE INTO `rounds` (MatchID, Round, RoundTime, WinnerID, PlantTime, PlantedSite, DefuseTime) 
                                                  VALUES (@matchID, @round, @roundTime, @winnerID, @plantTime, @plantedSite, @defuseTime)";

            killsCmd = connection.CreateCommand();
            killsCmd.CommandText = @"INSERT IGNORE INTO `kills` (matchID, roundID, killerID, victimID, assisterID, roundTime) 
                                                 VALUES (@matchID, @roundID, @killerID, @victimID, @assisterID, @roundTime)";

            grenadeCmd = connection.CreateCommand();
            grenadeCmd.CommandText = @"INSERT IGNORE `team` (matchID, round, winn, type, throwTime) 
                                                   VALUES (@round, @thrownByPlayerID, @type, @throwTime)";
        }

        ~Database()
        {
            connection.Clone();
        }

        private void setupDatabase()
        {
            MySqlCommand createDatabase = connection.CreateCommand();
            createDatabase.CommandText = "CREATE DATABASE IF NOT EXISTS csgopars";
            createDatabase.ExecuteNonQuery();

            MySqlCommand createMatchTable = connection.CreateCommand();
            createMatchTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `matches` (
                    `ID` INT NOT NULL AUTO_INCREMENT,
	                `Date` DATETIME NULL,
	                `Map` VARCHAR(50) NULL,
                    `Team1` INT NULL,
                    `Team2` INT NULL,
	                PRIMARY KEY (`ID`),
                    UNIQUE INDEX `MatchesDateTeam1Team2` (`Date`, `Team1`, `Team2`),
                    CONSTRAINT `MatchesTeam1` FOREIGN KEY (`Team1`) REFERENCES `teams` (`ID`),
	                CONSTRAINT `MatchesTeam2` FOREIGN KEY (`Team2`) REFERENCES `teams` (`ID`)
                ) COLLATE='utf8_general_ci' ENGINE = InnoDB;";

            MySqlCommand createPlayerTable = connection.CreateCommand();
            createPlayerTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `players` (
                    `ID` INT NOT NULL AUTO_INCREMENT,
                    `SteamID` BIGINT NOT NULL,
                    `Name` VARCHAR(50) NOT NULL,
                    PRIMARY KEY(`ID`),
                    UNIQUE INDEX `SteamID` (`SteamID`)
                ) COLLATE = 'utf8_general_ci' ENGINE = InnoDB;";

            MySqlCommand createTeamTable = connection.CreateCommand();
            createTeamTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `teams` (
                    `ID` INT NOT NULL AUTO_INCREMENT,
                    `Name` VARCHAR(50) NOT NULL,
                    PRIMARY KEY(`ID`),
                    UNIQUE INDEX `Name` (`Name`)
                ) COLLATE = 'utf8_general_ci' ENGINE = InnoDB;";

            MySqlCommand createPlayerTeamTable = connection.CreateCommand();
            createPlayerTeamTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `playerteam` (
	                `MatchID` INT NOT NULL,
	                `PlayerID` INT NOT NULL,
	                `TeamID` INT NOT NULL,
	                PRIMARY KEY (`MatchID`, `PlayerID`, `TeamID`),
	                CONSTRAINT `PlayerTeamMatchID` FOREIGN KEY (`MatchID`) REFERENCES `matches` (`ID`),
                    CONSTRAINT `PlayerTeamPlayerID` FOREIGN KEY (`PlayerID`) REFERENCES `players` (`ID`),
                    CONSTRAINT `PlayerTeamTeamID` FOREIGN KEY (`TeamID`) REFERENCES `teams` (`ID`)
                ) COLLATE='utf8_general_ci' ENGINE=InnoDB;";

            MySqlCommand createRoundTable = connection.CreateCommand();
            createRoundTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `rounds` (
                    `ID` INT NOT NULL AUTO_INCREMENT,
                    `MatchID` INT NULL,
                    `Round` INT NULL,
                    `RoundTime` INT NULL,
                    `WinnerID` INT NULL,
                    `PlantTime` INT NULL,
                    `PlantedSite` CHAR(1) NULL,
                    `DefuseTime` INT NULL,
                    PRIMARY KEY(`ID`),
                    INDEX `RoundMatchID` (`MatchID`),
                    INDEX `RoundWinnerID` (`WinnerID`),
                    UNIQUE INDEX `MatchID_Round` (`MatchID`, `Round`),
                    CONSTRAINT `RoundMatchID` FOREIGN KEY (`MatchID`) REFERENCES `matches` (`ID`),
                    CONSTRAINT `RoundWinnerID` FOREIGN KEY (`WinnerID`) REFERENCES `teams` (`ID`)
                ) COLLATE = 'utf8_general_ci' ENGINE = InnoDB;";

            MySqlCommand createkillsTable = connection.CreateCommand();
            createkillsTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `kills` (
	                `ID` INT NOT NULL AUTO_INCREMENT,
	                `MatchID` INT NULL,
	                `RoundID` INT NULL,
	                `KillerID` INT NULL,
	                `VictimID` INT NULL,
	                `AssisterID` INT NULL,
	                `RoundTime` INT NULL,
	                PRIMARY KEY (`ID`),
                    INDEX `KillMatchID` (`MatchID`),
	                INDEX `KillRoundID` (`RoundID`),
	                INDEX `KillKillerID` (`KillerID`),
	                INDEX `KillVictimID` (`VictimID`),
	                INDEX `KillAssisterID` (`AssisterID`),
	                UNIQUE INDEX `MatchID_RoundID_KillerID_VictimID` (`MatchID`, `RoundID`, `KillerID`, `VictimID`),
	                CONSTRAINT `KillMatchID` FOREIGN KEY (`MatchID`) REFERENCES `matches` (`ID`),
                    CONSTRAINT `KillRoundID` FOREIGN KEY (`RoundID`) REFERENCES `rounds` (`ID`),
                    CONSTRAINT `KillKillerID` FOREIGN KEY (`KillerID`) REFERENCES `players` (`ID`),
                    CONSTRAINT `KillVictimID` FOREIGN KEY (`VictimID`) REFERENCES `players` (`ID`),
                    CONSTRAINT `KillAssisterID` FOREIGN KEY (`AssisterID`) REFERENCES `rounds` (`ID`)
                ) COLLATE='utf8_general_ci' ENGINE=InnoDB;";

            MySqlCommand foreignKeyChecks = connection.CreateCommand();
            foreignKeyChecks.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";

            foreignKeyChecks.ExecuteNonQuery();
            createMatchTable.ExecuteNonQuery();
            createPlayerTable.ExecuteNonQuery();
            createTeamTable.ExecuteNonQuery();
            createPlayerTeamTable.ExecuteNonQuery();
            createRoundTable.ExecuteNonQuery();
            createkillsTable.ExecuteNonQuery();

            foreignKeyChecks.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
            foreignKeyChecks.ExecuteNonQuery();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long insertMatch(MatchData matchData)
        {
            matchesCmd.Parameters.AddWithValue("@matchDate", matchData.MatchTime);
            matchesCmd.Parameters.AddWithValue("@map", matchData.Map);
            matchesCmd.Parameters.AddWithValue("@team1ID", matchData.Team1);
            matchesCmd.Parameters.AddWithValue("@team2ID", matchData.Team2);

            matchesCmd.ExecuteNonQuery();
            matchesCmd.Parameters.Clear();

            return getMatchByDateAndTeams(matchData.MatchTime, matchData.Team1, matchData.Team2);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long insertRound(RoundData roundData)
        {
            roundsCmd.Parameters.AddWithValue("@matchID", roundData.MatchID);
            roundsCmd.Parameters.AddWithValue("@round", roundData.Round);
            roundsCmd.Parameters.AddWithValue("@roundTime", roundData.RoundEndTime - roundData.RoundStartTime);
            roundsCmd.Parameters.AddWithValue("@winnerID", roundData.Winner);
            roundsCmd.Parameters.AddWithValue("@plantTime", roundData.PlantTime);
            roundsCmd.Parameters.AddWithValue("@plantedSite", roundData.PlantedSite);
            roundsCmd.Parameters.AddWithValue("@defuseTime", roundData.DefuseTime);

            roundsCmd.ExecuteNonQuery();
            roundsCmd.Parameters.Clear();

            return roundsCmd.LastInsertedId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void insertPlayer(PlayerData player)
        {
            playersCmd.Parameters.AddWithValue("@steamID", player.SteamID);
            playersCmd.Parameters.AddWithValue("@playerName", player.PlayerName);

            playersCmd.ExecuteNonQuery();
            playersCmd.Parameters.Clear();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long insertTeam(TeamData team)
        {
            teamCmd.Parameters.AddWithValue("@teamName", team.TeamName);
            teamCmd.ExecuteNonQuery();
            teamCmd.Parameters.Clear();

            return getTeamByName(team.TeamName);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void insertKill(KillData kill)
        {
            //TODO: assister null
            killsCmd.Parameters.AddWithValue("@matchID", kill.MatchID);
            killsCmd.Parameters.AddWithValue("@roundID", kill.RoundID);
            int killerID = getPlayerBySteamID(kill.KillerID);
            int victimID = getPlayerBySteamID(kill.VictimID);
            int assisterID = getPlayerBySteamID(kill.AssisterID);

            killsCmd.Parameters.AddWithValue("@killerID", killerID != 0 ? (object)killerID : DBNull.Value);
            killsCmd.Parameters.AddWithValue("@victimID", victimID != 0 ? (object)victimID : DBNull.Value);
            killsCmd.Parameters.AddWithValue("@assisterID", assisterID != 0 ? (object)assisterID : DBNull.Value);
            killsCmd.Parameters.AddWithValue("@roundTime", kill.RoundTime);

            killsCmd.ExecuteNonQuery();
            killsCmd.Parameters.Clear();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getPlayerBySteamID(long steamID)
        {
            MySqlCommand queryPlayerID = connection.CreateCommand();
            queryPlayerID.CommandText = "SELECT ID FROM players WHERE SteamID = @steamID;";
            queryPlayerID.Parameters.AddWithValue("@steamID", steamID);
            queryPlayerID.ExecuteNonQuery();

            int id = 0;
            MySqlDataReader reader = queryPlayerID.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                id = (int)reader.GetValue(0);
            }
            reader.Close();

            return id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long getMatchByDateAndTeams(DateTime mt, long team1ID, long team2ID)
        {
            MySqlCommand queryMatchID = connection.CreateCommand();
            queryMatchID.CommandText = "SELECT ID FROM `matches` WHERE Date = @matchTime AND (Team1 = @team1ID OR Team1 = @team2ID) AND (Team2 = @team1ID OR Team2 = @team2ID);";
            queryMatchID.Parameters.AddWithValue("@matchTime", new DateTime(mt.Year, mt.Month, mt.Day, mt.Hour, mt.Minute, mt.Second));
            queryMatchID.Parameters.AddWithValue("@team1ID", team1ID);
            queryMatchID.Parameters.AddWithValue("@team2ID", team2ID);
            queryMatchID.ExecuteNonQuery();

            long id = 0;
            MySqlDataReader reader = queryMatchID.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                id = (long)(int)reader.GetValue(0);
            }
            reader.Close();

            return id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long getTeamByName(String name)
        {
            MySqlCommand queryTeamID = connection.CreateCommand();
            queryTeamID.CommandText = "SELECT ID FROM `teams` WHERE Name LIKE @name;";
            queryTeamID.Parameters.AddWithValue("@name", name);
            queryTeamID.ExecuteNonQuery();

            long id = 0;
            MySqlDataReader reader = queryTeamID.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                id = (long)(int)reader.GetValue(0);
            }
            reader.Close();

            return id;
        }
    }
}
