using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class SeasonalityTimestep
  {
    List<Dictionary<int, int>> sim_day_ranges;
    double lat, lon;
    double seasonalityValue;

    bool is_complete, loc;
    public SeasonalityTimestep()
      : base()
    {
      is_complete = false;
      lat = 51.47795;
      lon = 00.00000;
      seasonalityValue = 0;
      loc = false;
      sim_day_ranges = new List<Dictionary<int, int>>();
    }

    public bool parse_line_format(string tsStr)
    {

      int simDayStart, simDayEnd;

      if (tsStr.size() <= 0 || tsStr.at(0) == '#')
      { // empty line or comment
        return false;
      }
      else
      {
        vector<string> tsVec;
        size_t p1 = 0;
        size_t p2 = 0;
        while (p2 < tsStr.npos)
        {
          p2 = tsStr.find(" ", p1);
          tsVec.push_back(tsStr.substr(p1, p2 - p1));
          p1 = p2 + 1;
        }
        int n = tsVec.size();
        if (n < 3)
        {
          Utils::fred_abort("Need to specify at least SimulationDayStart, \
              SimulationDayEnd and SeasonalityValue for Seasonality_Timestep_Map. ");
        }
        else
        {
          if (tsVec[0].find('-') == 2 && tsVec[1].find('-') == 2)
          {
            // start and end specify only MM-DD (repeat this calendar date's value every year)
            parseMMDD(tsVec[0], tsVec[1]);
          }
          else
          {
            // start and end specified as (integer) sim days
            stringstream(tsVec[0]) >> simDayStart;
            stringstream(tsVec[1]) >> simDayEnd;
            sim_day_ranges.push_back(pair<int, int>(simDayStart, simDayEnd));
          }
          stringstream(tsVec[2]) >> seasonalityValue;
          if (n > 3)
          {
            stringstream(tsVec[3]) >> lat;
            stringstream(tsVec[4]) >> lon;
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
      for (unsigned i = 0; i < sim_day_ranges.size(); i++)
      {
        if (t >= sim_day_ranges[i].first && t <= sim_day_ranges[i].second)
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

    public bool has_location()
    {
      return loc;
    }

    public double get_lon()
    {
      if (!loc)
      {
        Utils::fred_abort("Tried to access location that was not specified.\
            Calls to get_lon() and get_lat() should be preceeded by has_location()");
      }
      return lon;
    }

    public double get_lat()
    {
      if (!loc)
      {
        Utils::fred_abort("Tried to access location that was not specified.\
          Calls to get_lon() and get_lat() should be preceeded by has_location()");
      }
      return lat;
    }

    public void print()
    {
      /*
	for(unsigned i = 0; i < sim_day_ranges.size(); i++) {
	Date * tmp_start_date = Global::Sim_Start_Date->clone();
	Date * tmp_end_date = Global::Sim_Start_Date->clone();
	tmp_start_date->advance(sim_day_ranges[i].first);
	tmp_end_date->advance(sim_day_ranges[i].second);
        printf("start day = %d (%s), end day = %d (%s), seasonality value = %f\n",
	sim_day_ranges[i].first, tmp_start_date->to_string().c_str(),
	sim_day_ranges[i].second, tmp_end_date->to_string().c_str(),
	seasonalityValue);
        delete tmp_start_date;
        delete tmp_end_date;
	}
      */
    }
  }
}
