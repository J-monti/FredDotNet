using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class PlaceInitData
  {
    char s[80];
    char place_type;
    char place_subtype;
    long int admin_id;
    int income;
    unsigned char deme_id;
    fred::geo lat, lon;
    bool is_group_quarters;
    int county;
    int census_tract_index;
    int num_workers_assigned;
    int group_quarters_units;
    char gq_type[8];
    char gq_workplace[32];

    void setup(char _s[], char _place_type, char _place_subtype, const char* _lat, const char* _lon,
        unsigned char _deme_id, int _county, int _census_tract_index, const char* _income, bool _is_group_quarters,
      int _num_workers_assigned, int _group_quarters_units, const char* _gq_type, const char* _gq_workplace) {
    place_type = _place_type;
    place_subtype = _place_subtype;
    strcpy(s, _s);
    sscanf(_lat, "%f", &lat);
    sscanf(_lon, "%f", &lon);
    county = _county;
    census_tract_index = _census_tract_index;
    sscanf(_income, "%d", &income);

    if(!(lat >= -90 && lat <= 90) || !(lon >= -180 && lon <= 180)) {
      printf("BAD LAT-LON: type = %c lat = %f  lon = %f  inc = %d  s = %s\n", place_type, lat, lon, income, s);
    lat = 34.999999;
    }
  assert(lat >= -90 && lat <= 90);
  assert(lon >= -180 && lon <= 180);

  is_group_quarters = _is_group_quarters;
    num_workers_assigned = _num_workers_assigned;
    group_quarters_units = _group_quarters_units;
    strcpy(gq_type, _gq_type);
  strcpy(gq_workplace, _gq_workplace);
}

Place_Init_Data(char _s[], char _place_type, char _place_subtype, const char* _lat, const char* _lon,
    unsigned char _deme_id, int _county = -1, int _census_tract = -1, const char* _income = "0",
    bool _is_group_quarters = false, int _num_workers_assigned = 0, int _group_quarters_units = 0,
      const char* gq_type = "X", const char* gq_workplace = "")
{
  setup(_s, _place_type, _place_subtype, _lat, _lon, _deme_id, _county, _census_tract, _income, _is_group_quarters,
      _num_workers_assigned, _group_quarters_units, gq_type, gq_workplace);
}

bool operator <(const Place_Init_Data & other) const {

    if(place_type != other.place_type) {
      return place_type<other.place_type;
    } else if(strcmp(s, other.s) < 0) {
      return true;
    } else {
      return false;
    }
  }

  const std::string to_string() const {
    std::stringstream ss;
ss << "Place Init Data ";
    ss << place_type << " ";
    ss << lat << " ";
    ss << lon << " ";
    ss << census_tract_index << " ";
    ss << s << " ";
    ss << int (deme_id) << " ";
    ss << num_workers_assigned << std::endl;
    return ss.str();
  }
  }
}
