using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using CSGO_DemoPars.Model;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace CSGO_DemoPars
{
    class Database
    {
        private MySqlTransaction transaction;
        private String dbName = "csgopars";
        MySqlConnection connection;
        MySqlCommand matchesCmd, roundsCmd, playersCmd, teamCmd, matchTeamCmd, killsCmd, grenadeCmd, movementCmd, playerTeamCmd;

        public Database()
        {
            connection = new MySqlConnection();
            connection.ConnectionString = ConfigurationManager.ConnectionStrings["MySqlConnectionString"].ConnectionString;
            connection.Open();

            setupDatabase();

            matchesCmd = connection.CreateCommand();
            matchesCmd.CommandText = @"INSERT IGNORE INTO matches (Date, Map) VALUES (@matchDate, @map)";

            playersCmd = connection.CreateCommand();
            playersCmd.CommandText = @"INSERT IGNORE INTO `players` (SteamID, Name) VALUES (@steamID, @playerName)";

            teamCmd = connection.CreateCommand();
            teamCmd.CommandText = @"INSERT IGNORE INTO `teams` (Name) VALUES (@teamName)";

            matchTeamCmd = connection.CreateCommand();
            matchTeamCmd.CommandText = @"INSERT IGNORE INTO `matchTeam` (MatchID, TeamID) VALUES (@matchID, @teamID)";

            movementCmd = connection.CreateCommand();
            movementCmd.CommandText = @"INSERT IGNORE INTO `movement` (MatchID, PlayerID, Tick, PositionX, PositionY, PositionZ, ViewDirectionX, ViewDirectionY) 
                                                   VALUES (@matchID, @playerID, @tick, @positionX, @positionY, @positionZ, @viewDirectionX, @viewDirectionY)";

            roundsCmd = connection.CreateCommand();
            roundsCmd.CommandText = @"INSERT IGNORE INTO `rounds` (MatchID, Round, RoundTime, WinnerID, WinnerSide, PlantTime, PlantedSite, DefuseTime) 
                                                  VALUES (@matchID, @round, @roundTime, @winnerID, @winnerSide, @plantTime, @plantedSite, @defuseTime)";

            killsCmd = connection.CreateCommand();
            killsCmd.CommandText = @"INSERT IGNORE INTO `kills` (matchID, roundID, killerID, victimID, assisterID, roundTime) 
                                                 VALUES (@matchID, @roundID, @killerID, @victimID, @assisterID, @roundTime)";

            grenadeCmd = connection.CreateCommand();
            grenadeCmd.CommandText = @"INSERT IGNORE INTO `team` (matchID, round, winn, type, throwTime) 
                                                   VALUES (@round, @thrownByPlayerID, @type, @throwTime)";

            playerTeamCmd = connection.CreateCommand();
            playerTeamCmd.CommandText = @"INSERT IGNORE INTO `playerteam` (matchID, playerID, teamID) 
                                                   VALUES (@matchID, @playerID, @teamID)";
        }

        ~Database()
        {
            connection.Close();
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
	                PRIMARY KEY (`ID`)
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

            MySqlCommand createMovementTable = connection.CreateCommand();
            createMovementTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS `movement` (
	                `ID` INT NOT NULL AUTO_INCREMENT,
	                `MatchID` INT NULL,
	                `PlayerID` BIGINT NULL,
	                `Tick` INT NULL,
	                `PositionX` FLOAT NULL DEFAULT NULL,
	                `PositionY` FLOAT NULL DEFAULT NULL,
	                `PositionZ` FLOAT NULL DEFAULT NULL,
                    `ViewDirectionX` FLOAT NULL DEFAULT NULL,
                    `ViewDirectionY` FLOAT NULL DEFAULT NULL,
	                UNIQUE INDEX `PlayerID_MatchID_Tick` (`PlayerID`, `MatchID`, `Tick`),
	                PRIMARY KEY (`ID`),
	                CONSTRAINT `MovementPlayerID` FOREIGN KEY (`PlayerID`) REFERENCES `players` (`SteamID`),
	                CONSTRAINT `MovementMatchID` FOREIGN KEY (`MatchID`) REFERENCES `matches` (`ID`)
                ) COLLATE='utf8_general_ci' ENGINE=InnoDB;";

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
                    `WinnerSide` INT NULL,
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

            MySqlCommand creatematchTeamTable = connection.CreateCommand();
            creatematchTeamTable.CommandText = String.Format("USE {0};", dbName) +
                @"CREATE TABLE IF NOT EXISTS  `matchTeam` (
	                `MatchID` INT(11) NOT NULL,
	                `TeamID` INT(11) NOT NULL,
                    PRIMARY KEY(`MatchID`, `TeamID`),
	                INDEX `FK_Team` (`TeamID`),
	                CONSTRAINT `FK_MatchID` FOREIGN KEY (`MatchID`) REFERENCES `matches` (`ID`),
	                CONSTRAINT `FK_Team` FOREIGN KEY (`TeamID`) REFERENCES `teams` (`ID`)
                ) COLLATE='utf8_general_ci' ENGINE = InnoDB;";


            MySqlCommand foreignKeyChecks = connection.CreateCommand();
            foreignKeyChecks.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";

            foreignKeyChecks.ExecuteNonQuery();
            createMatchTable.ExecuteNonQuery();
            createPlayerTable.ExecuteNonQuery();
            createTeamTable.ExecuteNonQuery();
            createMovementTable.ExecuteNonQuery();
            createPlayerTeamTable.ExecuteNonQuery();
            createRoundTable.ExecuteNonQuery();
            createkillsTable.ExecuteNonQuery();
            creatematchTeamTable.ExecuteNonQuery();

            foreignKeyChecks.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
            foreignKeyChecks.ExecuteNonQuery();
        }

        private void insertMatchTeam(long matchID, long teamID)
        {
            matchTeamCmd.Parameters.AddWithValue("@matchID", matchID);
            matchTeamCmd.Parameters.AddWithValue("@teamID", teamID);
            matchTeamCmd.ExecuteNonQuery();
            matchTeamCmd.Parameters.Clear();
        }

        public long insertMatch(MatchData matchData)
        {
            long matchID = getMatchByDateAndTeam(matchData.MatchTime, matchData.Team1);
            if (matchID != 0)
                return matchID;

            matchesCmd.Parameters.AddWithValue("@matchDate", matchData.MatchTime);
            matchesCmd.Parameters.AddWithValue("@map", matchData.Map);

            matchesCmd.ExecuteNonQuery();
            matchesCmd.Parameters.Clear();

            matchID = matchesCmd.LastInsertedId;
            insertMatchTeam(matchID, matchData.Team1);
            insertMatchTeam(matchID, matchData.Team2);

            return matchID;
        }

        public long insertRound(RoundData roundData)
        {
            roundsCmd.Parameters.AddWithValue("@matchID", roundData.MatchID);
            roundsCmd.Parameters.AddWithValue("@round", roundData.Round);
            roundsCmd.Parameters.AddWithValue("@roundTime", roundData.RoundEndTime - roundData.RoundStartTime);
            roundsCmd.Parameters.AddWithValue("@winnerID", roundData.Winner);
            roundsCmd.Parameters.AddWithValue("@winnerSide", (long)roundData.WinnerSide);
            roundsCmd.Parameters.AddWithValue("@plantTime", roundData.PlantTime);
            roundsCmd.Parameters.AddWithValue("@plantedSite", roundData.PlantedSite);
            roundsCmd.Parameters.AddWithValue("@defuseTime", roundData.DefuseTime);

            roundsCmd.ExecuteNonQuery();
            roundsCmd.Parameters.Clear();

            return roundsCmd.LastInsertedId;
        }

        public void insertPlayer(PlayerData player)
        {
            playersCmd.Parameters.AddWithValue("@steamID", player.SteamID);
            playersCmd.Parameters.AddWithValue("@playerName", player.PlayerName);

            playersCmd.ExecuteNonQuery();
            playersCmd.Parameters.Clear();
        }

        public long insertTeam(TeamData team)
        {
            teamCmd.Parameters.AddWithValue("@teamName", team.TeamName);
            teamCmd.ExecuteNonQuery();
            teamCmd.Parameters.Clear();

            return getTeamByName(team.TeamName);
        }

        public void insertPlayerTeam(long matchID, long teamID, long playerSteamID)
        {
            long playerID = getPlayerBySteamID(playerSteamID);
            playerTeamCmd.Parameters.AddWithValue("@matchID", matchID);
            playerTeamCmd.Parameters.AddWithValue("@teamID", teamID);
            playerTeamCmd.Parameters.AddWithValue("@playerID", playerID);

            playerTeamCmd.ExecuteNonQuery();
            playerTeamCmd.Parameters.Clear();
        }

        public void insertKill(KillData kill)
        {
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

        public void insertPosition(List<PositionData> positionData)
        {
            StringBuilder builder = new StringBuilder(@"INSERT IGNORE INTO `movement` (MatchID, PlayerID, Tick, PositionX, PositionY, PositionZ, ViewDirectionX, ViewDirectionY) VALUES ");

            List<string> rows = new List<string>();
            CultureInfo decimalFormat = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
            foreach (PositionData pos in positionData)
            {
                rows.Add(string.Format("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})", 
                    MySqlHelper.EscapeString(pos.MatchID.ToString()), 
                    MySqlHelper.EscapeString(pos.SteamID.ToString()), 
                    MySqlHelper.EscapeString(pos.Tick.ToString()), 
                    MySqlHelper.EscapeString(pos.Position.X.ToString("G9", decimalFormat)), 
                    MySqlHelper.EscapeString(pos.Position.Y.ToString("G9", decimalFormat)), 
                    MySqlHelper.EscapeString(pos.Position.Z.ToString("G9", decimalFormat)), 
                    MySqlHelper.EscapeString(pos.ViewDirection.X.ToString("G9", decimalFormat)), 
                    MySqlHelper.EscapeString(pos.ViewDirection.Y.ToString("G9", decimalFormat))
                ));
            }
            builder.Append(string.Join(",", rows)).Append(";");

            movementCmd = connection.CreateCommand();
            movementCmd.CommandText = builder.ToString();
            movementCmd.ExecuteNonQuery();
            movementCmd.Parameters.Clear();
        }

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

        public long getMatchByDateAndTeam(DateTime mt, long teamID)
        {
            MySqlCommand queryMatchID = connection.CreateCommand();
            
            queryMatchID.CommandText = "SELECT ID FROM `matches` m INNER JOIN `matchteam` mt ON m.ID = mt.MatchID WHERE m.Date = @matchTime AND mt.TeamID = @teamID";
            queryMatchID.Parameters.AddWithValue("@matchTime", new DateTime(mt.Year, mt.Month, mt.Day, mt.Hour, mt.Minute, mt.Second));
            queryMatchID.Parameters.AddWithValue("@teamID", teamID);
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
