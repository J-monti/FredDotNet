using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fred.Tests
{
  [TestClass]
  public class TestFred
  {
    private const string FRED_DB_PATH = @"C:\Users\JonathanMontiverdi\source\repos\FredDotNet\fred.db";

    [TestMethod]
    public void RunThatIsh()
    {
      var fred = new FredMain();
      fred.run();
    }

    [TestMethod]
    public void ImportStates()
    {
      string line;
      string[] headers;
      var connectionBuilder = new SqliteConnectionStringBuilder
      {
        DataSource = FRED_DB_PATH
      };
      var database = new SqliteConnection(connectionBuilder.ConnectionString);
      database.Open();
      var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      using var reader = new StreamReader(Path.Combine(local, @"FRED\populations\state_geocodes.csv"));
      while (reader.Peek() != -1)
      {
        line = reader.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
          if (line == "## HEADERS ##")
          {
            // next line should be the headers... skip it!
            line = reader.ReadLine();
            headers = line.Split(',');
          }
          continue;
        }

        var data = line.Split('\t');
        var region = Convert.ToInt32(data[0]);
        var division = Convert.ToInt32(data[1]);
        var state = Convert.ToInt32(data[2]);
        var name = data[3];
        if (division == 0 && state == 0)
        {
          // this is a region
          var command = database.CreateCommand();
          command.CommandText = "INSERT INTO Regions (Id, Name) VALUES (@id, @name)";
          command.Parameters.AddWithValue("@id", region);
          command.Parameters.AddWithValue("@name", name);
          //command.ExecuteNonQuery();
        }
        else if (state == 0)
        {
          // this.is a division
          var command = database.CreateCommand();
          command.CommandText = "INSERT INTO Divisions (Id, Name, RegionId) VALUES (@id, @name, @regionId)";
          command.Parameters.AddWithValue("@id", division);
          command.Parameters.AddWithValue("@name", name);
          command.Parameters.AddWithValue("@regionId", region);
          //command.ExecuteNonQuery();
        }
        else
        {
          // a state
          var command = database.CreateCommand();
          command.CommandText = "INSERT INTO States (Id, Name, RegionId, DivisionId) VALUES (@id, @name, @regionId, @divisionId)";
          command.Parameters.AddWithValue("@id", state);
          command.Parameters.AddWithValue("@name", name);
          command.Parameters.AddWithValue("@regionId", region);
          command.Parameters.AddWithValue("@divisionId", division);
          //command.ExecuteNonQuery();
        }
      }

      database.Dispose();
    }

    [TestMethod]
    public void ImportCounties()
    {
      string line;
      string[] headers;
      var connectionBuilder = new SqliteConnectionStringBuilder
      {
        DataSource = FRED_DB_PATH
      };
      var database = new SqliteConnection(connectionBuilder.ConnectionString);
      database.Open();
      var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      using var reader = new StreamReader(Path.Combine(local, @"FRED\populations\all_geocodes.csv"));
      while (reader.Peek() != -1)
      {
        line = reader.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
          if (line == "## HEADERS ##")
          {
            // next line should be the headers... skip it!
            line = reader.ReadLine();
            headers = line.Split(',');
          }
          continue;
        }

        var data = line.Split('\t');
        var summary = data[0];
        var stateId = Convert.ToInt32(data[1]);
        var countyFips = data[2];
        var countySubdivision = data[3];
        var place = data[4];
        var consolidatedCity = data[5];
        var name = data[6];
        if (countyFips == "000" && countySubdivision == "00000" && place == "00000" && consolidatedCity == "00000")
        {
          // this is a state
          continue;
        }
        else if (countyFips != "000" && countySubdivision == "00000")
        {
          // this.is a county
          var command = database.CreateCommand();
          command.CommandText = "INSERT INTO Counties (Name, StateId, Fips) VALUES (@name, @stateId, @countyFips)";
          command.Parameters.AddWithValue("@name", name);
          command.Parameters.AddWithValue("@stateId", stateId);
          command.Parameters.AddWithValue("@countyFips", countyFips);
          //command.ExecuteNonQuery();
        }
      }

      database.Dispose();
    }

    [TestMethod]
    public void ImportCensusTracts()
    {
      string line;
      string[] headers;
      var connectionBuilder = new SqliteConnectionStringBuilder
      {
        DataSource = FRED_DB_PATH
      };
      var database = new SqliteConnection(connectionBuilder.ConnectionString);
      database.Open();
      var counties = this.ReadCounties(database);
      var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      using var reader = new StreamReader(Path.Combine(local, @"FRED\populations\2010_Census_Tract_to_2010_PUMA.txt"));
      while (reader.Peek() != -1)
      {
        line = reader.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
          if (line == "## HEADERS ##")
          {
            // next line should be the headers... skip it!
            line = reader.ReadLine();
            headers = line.Split(',');
          }
          continue;
        }

        var data = line.Split(',');
        var stateId = Convert.ToInt32(data[0]);
        var countyFips = data[1];
        var tract = data[2];
        var pumaCode = data[3];
        if (counties.ContainsKey(stateId))
        {
          var county = counties[stateId].FirstOrDefault(c => c.Fips == countyFips);
          if (county != null)
          {
            var command = database.CreateCommand();
            command.CommandText = "INSERT INTO CensusTracts (Tract, StateId, CountyId, PumaCode) VALUES (@tract, @stateId, @countyId, @pumaCode)";
            command.Parameters.AddWithValue("@tract", tract);
            command.Parameters.AddWithValue("@stateId", stateId);
            command.Parameters.AddWithValue("@countyId", county.Id);
            command.Parameters.AddWithValue("@pumaCode", pumaCode);
            //command.ExecuteNonQuery();
          }
        }
      }

      database.Dispose();
    }

    [TestMethod]
    public void TestImportSchools()
    {
      string line;
      int lineIndex = 0;
      string[] headers;
      var connectionBuilder = new SqliteConnectionStringBuilder
      {
        DataSource = FRED_DB_PATH
      };
      var database = new SqliteConnection(connectionBuilder.ConnectionString);
      database.Open();
      //var levels = new List<string>();
      //var grades = new List<string>();
      //var schoolTypes = new List<string>();
      var counties = this.ReadCounties(database);
      var levels = this.ReadLevels(database);
      var grades = this.ReadGrades(database);
      var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      using var reader = new StreamReader(Path.Combine(local, @"FRED\populations\USA_Public_Schools.csv"));
      while (reader.Peek() != -1)
      {
        lineIndex++;
        line = reader.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
          if (line == "## HEADERS ##")
          {
            // next line should be the headers... skip it!
            line = reader.ReadLine();
            headers = line.Split(',');
          }
          continue;
        }
        var data = line.Split(',');
        var name = Clean(data[3]);
        var stateName = Clean(data[1]);
        var countyName = Clean(data[4]);
        var countyFips = Clean(data[5]);
        var ncesId = Clean(data[6]);
        var stateFips = Clean(data[7]);
        //var stateId = Convert.ToInt32(stateFips);
        var address = Clean(data[8]);
        var city = Clean(data[11]);
        var zip = Clean(data[12]);
        var level = Clean(data[18]);
        //if (!levels.Contains(level)) { levels.Add(level); }
        var lowestGrade = Clean(data[19]);
        //if (!grades.Contains(lowestGrade)) { grades.Add(lowestGrade); }
        var highestGrade = Clean(data[20]);
        //if (!grades.Contains(highestGrade)) { grades.Add(highestGrade); }
        var hasPreK = Clean(data[21]);
        var hasKinder = Clean(data[22]);
        var has01Grade = Clean(data[23]);
        var has02Grade = Clean(data[24]);
        var has03Grade = Clean(data[25]);
        var has04Grade = Clean(data[26]);
        var has05Grade = Clean(data[27]);
        var has06Grade = Clean(data[28]);
        var has07Grade = Clean(data[29]);
        var has08Grade = Clean(data[30]);
        var has09Grade = Clean(data[31]);
        var has10Grade = Clean(data[32]);
        var has11Grade = Clean(data[33]);
        var has12Grade = Clean(data[34]);
        var has13Grade = Clean(data[35]);
        var schoolType = Clean(data[36]);
        //if (!schoolTypes.Contains(schoolType)) { schoolTypes.Add(schoolType); }
        var isCharter = Clean(data[37]);
        var isMagnet = Clean(data[38]);
        var latitude = Clean(data[39]);
        var longitude = Clean(data[40]);
        var stateFromCounty = countyFips.Substring(0, 2);
        var countyOnlyFips = countyFips.Substring(2);
        var stateId = Convert.ToInt32(stateFromCounty);
        var county = counties[stateId].FirstOrDefault(c => c.Fips == countyOnlyFips);
        var command = database.CreateCommand();
        command.CommandText = "INSERT INTO Schools (Name, StateId, CountyId, Address, City, Zip, NcesId, Level, LowestGrade, HighestGrade, " +
          "HasPreK, HasKindergarten, Has1st, Has2nd, Has3rd, Has4th, Has5th, Has6th, Has7th, Has8th, Has9th, Has10th, Has11th, Has12th, Has13th, " +
          "SchoolType, IsCharter, IsMagnet, Latitude, Longitude) " +
          "VALUES (@name, @stateId, @countyId, @address, @city, @zip, @ncesId, @level, @lowestGrade, @highestGrade, " +
          "@hasPreK, @hasKinder, @has01Grade, @has02Grade, @has03Grade, @has04Grade, @has05Grade, @has06Grade, @has07Grade, @has08Grade, " +
          "@has09Grade, @has10Grade, @has11Grade, @has12Grade, @has13Grade, @schoolType, @isCharter, @isMagnet, @latitude, @longitude)";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@stateId", stateId);
        command.Parameters.AddWithValue("@countyId", county.Id);
        command.Parameters.AddWithValue("@address", address);
        command.Parameters.AddWithValue("@city", city);
        command.Parameters.AddWithValue("@zip", zip);
        command.Parameters.AddWithValue("@ncesId", ncesId);
        command.Parameters.AddWithValue("@level", levels[level]);
        command.Parameters.AddWithValue("@lowestGrade", grades[lowestGrade]);
        command.Parameters.AddWithValue("@highestGrade", grades[highestGrade]);
        command.Parameters.AddWithValue("@hasPreK", hasPreK.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@hasKinder", hasKinder.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has01Grade", has01Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has02Grade", has02Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has03Grade", has03Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has04Grade", has04Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has05Grade", has05Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has06Grade", has06Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has07Grade", has07Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has08Grade", has08Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has09Grade", has09Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has10Grade", has10Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has11Grade", has11Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has12Grade", has12Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@has13Grade", has13Grade.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@schoolType", schoolType?.Split('-')[0]);
        command.Parameters.AddWithValue("@isCharter", isCharter.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@isMagnet", isMagnet.StartsWith('1') ? 1 : 0);
        command.Parameters.AddWithValue("@latitude", latitude);
        command.Parameters.AddWithValue("@longitude", longitude);
        //command.ExecuteNonQuery();
      }
      reader.Dispose();
    }

    private string Clean(string value)
    {
      if (value.StartsWith('='))
      {
        return value.Substring(2, value.Length - 3);
      }

      return value;
    }

    private Dictionary<int, List<County>> ReadCounties(SqliteConnection connection)
    {
      var command = connection.CreateCommand();
      command.CommandText =
      @"
        SELECT Id, Name, StateId, Fips
        FROM Counties
      ";

      var cali = 0;
      var data = new Dictionary<int, List<County>>();
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var stateId = reader.GetInt32(2);
        var fips = reader.GetString(3);
        if (stateId == 6)
        {
          cali++;
        }

        if (!data.ContainsKey(stateId))
        {
          data.Add(stateId, new List<County>());
        }

        data[stateId].Add(new County(id, name, stateId, fips));
      }

      command.Dispose();
      return data;
    }

    private Dictionary<string, int> ReadLevels(SqliteConnection connection)
    {
      var command = connection.CreateCommand();
      command.CommandText =
      @"
        SELECT Id, Level
        FROM SchoolLevels
      ";

      var data = new Dictionary<string, int>();
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        var id = reader.GetInt32(0);
        var level = reader.GetString(1);
        data.Add(level, id);
      }

      command.Dispose();
      return data;
    }

    private Dictionary<string, int> ReadGrades(SqliteConnection connection)
    {
      var command = connection.CreateCommand();
      command.CommandText =
      @"
        SELECT Id, Grade
        FROM SchoolGrades
      ";

      var data = new Dictionary<string, int>();
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        var id = reader.GetInt32(0);
        var grade = reader.GetString(1);
        data.Add(grade, id);
      }

      command.Dispose();
      return data;
    }
  }
}
