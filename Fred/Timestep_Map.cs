using System;
using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public class Timestep_Map : Dictionary<int, int>
  {
    protected string name;             // Name of the map
    protected string map_file_name;
    protected int current_value;       // Holds the current value of th map.

    public Timestep_Map()
    {
      name = "";
      current_value = -1;
    }

    public Timestep_Map(string name)
    {
      this.name = name;
      current_value = 0;

      string map_file_param;
      // Need Special parsing if this is an array from input
      // Allows for Disease specific values
      if (name.Contains("["))
      {
        string name_tmp;
        string number;
        int found = name.LastIndexOf("[");
        int found2 = name.LastIndexOf("]");
        name_tmp = name.Substring(0, found);
        number = name.Substring(found + 1, found2);
        map_file_param = $"{name_tmp}_file[{number}]";
      }
      else
      {
        map_file_param = $"{name}_file";
      }

      // Read the filename from params
      FredParameters.GetParameter(map_file_param, ref map_file_name);
      map_file_name = Utils.get_fred_file_name(map_file_name);
      // If this parameter is "none", then there is no map
    }

    // Utility Members
    public int get_value_for_timestep(int ts, int offset) // returns the value for the given time-step - delay
    {
      if ((ts - offset) < 0)
      {
        current_value = 0;
      }
      else
      {
        if (this.ContainsKey(ts - offset))
        {
          current_value = this[ts - offset];
        }
      }

      return current_value;
    }

    public bool is_empty()
    {
      return this.Count == 0;
    }

    public virtual void print()
    {
      Console.WriteLine();
      Console.WriteLine($" Time-step Map  {this.Count}");
      foreach (var kvp in this)
      {
        Console.WriteLine($"{kvp.Key:D5}: {kvp.Value:D10}");
      }
      Console.WriteLine();
    }

    public virtual void read_map()
    {
      this.Clear();
      if (map_file_name == "none")
      {
        return;
      }

      if (!File.Exists(map_file_name))
      {
        Utils.fred_abort("Help!  Can't read {0} Time-step Map", map_file_name);
      }

      // There is a file, lets read in the data structure.
      string line;
      using var fp = new StreamReader(map_file_name);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        var tokens = line.Split(' ');
        this.Add(Convert.ToInt32(tokens[0]), Convert.ToInt32(tokens[1]));
      }
    }
  }
}
