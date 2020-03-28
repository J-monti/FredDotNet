using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class SeasonalityTimestepMap : TimestepMap
  {
    public SeasonalityTimestepMap(string _name)
      : base(_name)
    {
    }

    void read_map()
    {
      ifstream* ts_input = new ifstream(this->map_file_name);

      if (!ts_input->is_open())
      {
        Utils::fred_abort("Help!  Can't read %s Timestep Map\n", this->map_file_name);
        abort();
      }

      string line;
      if (getline(*ts_input, line))
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
          Utils::fred_abort(
          "First line has to specify either #line_format or #structured_format; see primary_case_schedule-0.txt for an example. ");
        }
      }
      else
      {
        Utils::fred_abort("Nothing in the file!");
      }
    }

    void read_map_line(ifstream* ts_input)
    {
      // There is a file, lets read in the data structure.
      this->values = new map<int, int>;
      string line;
      while (getline(*ts_input, line))
      {
        Seasonality_Timestep* cts = new Seasonality_Timestep();
        if (cts->parse_line_format(line))
        {
          insert(cts);
        }
      }
    }

    void read_map_structured(ifstream* ts_input)
    {
    }

    void insert(Seasonality_Timestep* ct)
    {
      this->seasonality_timestep_map.push_back(ct);
    }

    void print()
    {
      return;
      cout << "\n";
      cout << this->name << " Seasonality_Timestep_Map size = " << (int)this->seasonality_timestep_map.size()
           << "\n";
      vector<Seasonality_Timestep*>::iterator itr;
      for (itr = begin(); itr != end(); ++itr)
      {
        (*itr)->print();
      }
      cout << "\n";
    }

    int parse_month_from_date_string(string date_string, string format_string)
    {
      string temp_str;
      string current_date = Date::get_date_string();
      if (format_string.compare(current_date) == 0)
      {
        size_t pos_1, pos_2;
        pos_1 = date_string.find('-');
        if (pos_1 != string::npos)
        {
          pos_2 = date_string.find('-', pos_1 + 1);
          if (pos_2 != string::npos)
          {
            temp_str = date_string.substr(pos_1 + 1, pos_2 - pos_1 - 1);
            int i;
            istringstream my_stream(temp_str);
            if (my_stream >> i)
              return i;
          }
        }
        return -1;
      }
      return -1;
    }

    int parse_day_of_month_from_date_string(string date_string, string format_string)
    {
      string temp_str;
      string current_date = Date::get_date_string();
      if (format_string.compare(current_date) == 0)
      {
        size_t pos;
        pos = date_string.find('-', date_string.find('-') + 1);
        if (pos != string::npos)
        {
          temp_str = date_string.substr(pos + 1);
          int i;
          istringstream my_stream(temp_str);
          if (my_stream >> i)
            return i;
        }
        return -1;
      }
      return -1;
    }

    int parse_year_from_date_string(string date_string, string format_string)
    {
      string temp_str;
      string current_date = Date::get_date_string();
      if (format_string.compare(current_date) == 0)
      {
        size_t pos;
        pos = date_string.find('-');
        if (pos != string::npos)
        {
          temp_str = date_string.substr(0, pos);
          int i;
          istringstream my_stream(temp_str);
          if (my_stream >> i)
            return i;
        }
        return -1;
      }
      return -1;
    }
  }
}
