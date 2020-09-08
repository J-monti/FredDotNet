using System;
using System.Collections.Generic;

namespace Fred
{
  public class Seasonality_Timestep
  {
    //int simDayStart, simDayEnd;
    private readonly List<Tuple<int, int>> sim_day_ranges = new List<Tuple<int, int>>();
    private FredGeo lat;
    private FredGeo lon;
    private double seasonalityValue;
    private bool is_complete, loc;
    public Seasonality_Timestep()
    {
      is_complete = false;
      lat = 51.47795;
      lon = 00.00000;
      seasonalityValue = 0;
      loc = false;
    }

    public bool parse_line_format(string tsStr)
    {
      int simDayStart, simDayEnd;
      if (string.IsNullOrWhiteSpace(tsStr) || tsStr.Trim()[0] == '#')
      { // empty line or comment
        return false;
      }
      else
      {
        var tsVec = tsStr.Split(' ');
        int n = tsVec.Length;
        if (n < 3)
        {
          Utils.fred_abort("Need to specify at least SimulationDayStart, SimulationDayEnd and SeasonalityValue for Seasonality_Timestep_Map. ");
        }
        else
        {
          if (tsVec[0].IndexOf('-') == 2 && tsVec[1].IndexOf('-') == 2)
          {
            // start and end specify only MM-DD (repeat this calendar date's value every year)
            parseMMDD(tsVec[0], tsVec[1]);
          }
          else
          {
            // start and end specified as (integer) sim days
            simDayStart = Convert.ToInt32(tsVec[0]);
            simDayEnd = Convert.ToInt32(tsVec[1]);
            sim_day_ranges.Add(new Tuple<int, int>(simDayStart, simDayEnd));
          }
          seasonalityValue = Convert.ToDouble(tsVec[2]);
          if (n > 3)
          {
            lat = Convert.ToDouble(tsVec[3]);
            lon = Convert.ToDouble(tsVec[4]);
            loc = true;
          }
        }
        is_complete = true;
      }
      return is_complete;
    }


    public bool is_applicable(int ts, int offset)
    {
      int t = ts - offset;
      for (int i = 0; i < sim_day_ranges.Count; i++)
      {
        if (t >= sim_day_ranges[i].Item1 && t <= sim_day_ranges[i].Item2)
        {
          return true;
        }
      }
      return false;
      //return t >= simDayStart && t <= simDayEnd;
    }

    public double get_seasonality_value()
    {
      return seasonalityValue;
    }

    public  bool has_location()
    {
      return loc;
    }

    public double get_lon()
    {
      if (!loc)
      {
        Utils.fred_abort("Tried to access location that was not specified. Calls to get_lon() and get_lat() should be preceeded by has_location()");
      }
      return lon;
    }

    public double get_lat()
    {
      if (!loc)
      {
        Utils.fred_abort("Tried to access location that was not specified. Calls to get_lon() and get_lat() should be preceeded by has_location()");
      }
      return lat;
    }

    public void print()
    {
      /*
  for(unsigned i = 0; i < sim_day_ranges.size(); i++) {
  Date * tmp_start_date = Global.Sim_Start_Date.clone();
  Date * tmp_end_date = Global.Sim_Start_Date.clone();
  tmp_start_date.advance(sim_day_ranges[i].first);
  tmp_end_date.advance(sim_day_ranges[i].second);
        printf("start day = %d (%s), end day = %d (%s), seasonality value = %f\n",
  sim_day_ranges[i].first, tmp_start_date.to_string().c_str(),
  sim_day_ranges[i].second, tmp_end_date.to_string().c_str(),
  seasonalityValue);
        delete tmp_start_date;
        delete tmp_end_date;
  }
      */
    }

    private bool parseMMDD(string startMMDD, string endMMDD)
    {
      return true;
    }
  }
}
