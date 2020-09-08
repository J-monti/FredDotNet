using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Seasonality_Timestep_Map : Timestep_Map
  {
    private readonly List<Seasonality_Timestep> seasonality_timestep_map = new List<Seasonality_Timestep>();

    public Seasonality_Timestep_Map(string _name)
      : base (_name)
    {
    }

    public override void read_map()
    {
      if (!File.Exists(this.map_file_name))
      {
        Utils.fred_abort("Help!  Can't read %s Time-step Map\n", this.map_file_name);
      }

      using var ts_input = new StreamReader(this.map_file_name);
      string line = ts_input.ReadLine();
      if (!string.IsNullOrEmpty(line))
      {
        if (line == "#line_format")
        {
          read_map_line(ts_input);
        }
        else if (line == "#structured_format")
        {
          read_map_structured(ts_input);
        }
        else
        {
          Utils.fred_abort(
          "First line has to specify either #line_format or #structured_format; see primary_case_schedule-0.txt for an example. ");
        }
      }
      else
      {
        Utils.fred_abort("Nothing in the file!");
      }
    }

    public void read_map_line(TextReader ts_input)
    {
      // There is a file, lets read in the data structure.
      this.Clear();
      string line;
      while (ts_input.Peek() != -1)
      {
        line = ts_input.ReadLine();
        var cts = new Seasonality_Timestep();
        if (cts.parse_line_format(line))
        {
          insert(cts);
        }
      }
    }

    public void read_map_structured(TextReader ts_input) { } // Not implemented...
    
    public override void print()
    {
      //cout << "\n";
      //cout << this->name << " Seasonality_Timestep_Map size = " << (int)this->seasonality_timestep_map.size()
      //     << "\n";
      //vector<Seasonality_Timestep*>::iterator itr;
      //for (itr = begin(); itr != end(); ++itr)
      //{
      //  (*itr)->print();
      //}
      //cout << "\n";
    }

    private void insert(Seasonality_Timestep ct)
    {
      this.seasonality_timestep_map.Add(ct);
    }

    public List<Seasonality_Timestep> get_map()
    {
      return this.seasonality_timestep_map;
    }
  }
}
