using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Place_Init_Data
  {
    public string s;
    public char place_type;
    public char place_subtype;
    public long admin_id;
    public int income;
    public char deme_id;
    public FredGeo lat, lon;
    public bool is_group_quarters;
    public int county;
    public int census_tract_index;
    public int num_workers_assigned;
    public int group_quarters_units;
    public string gq_type;
    public string gq_workplace;

    public Place_Init_Data(string _s, char _place_type, char _place_subtype, string _lat, string _lon,
        char _deme_id, int _county = -1, int _census_tract = -1, string _income = "0",
        bool _is_group_quarters = false, int _num_workers_assigned = 0, int _group_quarters_units = 0,
          string gq_type = "X", string gq_workplace = "")
    {
      setup(_s, _place_type, _place_subtype, _lat, _lon, _deme_id, _county, _census_tract, _income, _is_group_quarters,
          _num_workers_assigned, _group_quarters_units, gq_type, gq_workplace);
    }

    private void setup(string _s, char _place_type, char _place_subtype, string _lat, string _lon,
        char _deme_id, int _county, int _census_tract_index, string _income, bool _is_group_quarters,
      int _num_workers_assigned, int _group_quarters_units, string _gq_type, string _gq_workplace)
    {
      place_type = _place_type;
      place_subtype = _place_subtype;
      s = _s;
      this.deme_id = _deme_id;
      this.lat = new FredGeo(Convert.ToDouble(_lat));
      this.lon = new FredGeo(Convert.ToDouble(_lon));
      this.county = _county;
      this.census_tract_index = _census_tract_index;
      this.income = Convert.ToInt32(_income);

      if (!(lat.Value >= -90 && lat.Value <= 90) || !(lon.Value >= -180 && lon.Value <= 180))
      {
        Console.WriteLine("BAD LAT-LON: type = {0} lat = {1}  lon = {2}  inc = {3}  s = {4}", place_type, lat, lon, income, s);
        lat.Value = 34.999999;
      }

      Utils.assert(lat.Value >= -90 && lat.Value <= 90);
      Utils.assert(lon.Value >= -180 && lon.Value <= 180);
      is_group_quarters = _is_group_quarters;
      num_workers_assigned = _num_workers_assigned;
      group_quarters_units = _group_quarters_units;
      gq_type = _gq_type;
      gq_workplace = _gq_workplace;
    }

    public override string ToString()
    {
      return $"Place Init Data {place_type} {lat} {lon} {census_tract_index} {s} {deme_id} {num_workers_assigned}";
    }

    public override bool Equals(object obj)
    {
      return obj is Place_Init_Data data &&
             s == data.s &&
             place_type == data.place_type;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(s, place_type);
    }
  }
}
