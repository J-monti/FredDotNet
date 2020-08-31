using System;

namespace Fred
{
  public static class Geo
  {
    public const double DEG_TO_RAD = 0.017453292519943295769236907684886; // PI/180
                                                                          // see http://andrew.hedges.name/experiments/haversine/
    private const double EARTH_RADIUS = 6373.0; // earth's radius in kilometers
    private const double KM_PER_DEG_LAT = 111.325; // assuming spherical earth

    // US Mean latitude-longitude (http://www.travelmath.com/country/United+States)
    //private const double MEAN_US_LON = -97.0; // near Wichita, KS
    //private const double MEAN_US_LAT = 38.0; // near Wichita, KS

    // from http://www.ariesmar.com/degree-latitude.php
    private const double MEAN_US_KM_PER_DEG_LON = 87.832; // at 38 deg N
    private const double MEAN_US_KM_PER_DEG_LAT = 110.996; // 
    
    public static double km_per_deg_longitude = MEAN_US_KM_PER_DEG_LON;
    public static double km_per_deg_latitude = MEAN_US_KM_PER_DEG_LAT;

    /**
     * Sets the kilometers per degree longitude at a given latitiude
     *
     * @param lat the latitude to set KM / degree
     */
    public static void set_km_per_degree(FredGeo lat)
    {
      var latVal = lat.Value * Geo.DEG_TO_RAD;
      double cosine = Math.Cos(latVal);
      Geo.km_per_deg_longitude = cosine * Geo.KM_PER_DEG_LAT;
      Geo.km_per_deg_latitude = Geo.KM_PER_DEG_LAT;
    }

    /**
     * @param lon1
     * @param lat1
     * @param lon2
     * @param lat2
     *
     * @return the haversine distance between the two points on the Earth's surface
     */
    public static double haversine_distance(FredGeo lon1, FredGeo lat1, FredGeo lon2, FredGeo lat2)
    {
      // convert to radians
      var lat1Val = lat1.Value * DEG_TO_RAD;
      var lon1Val = lon1.Value * DEG_TO_RAD;
      var lat2Val = lat2.Value * DEG_TO_RAD;
      var lon2Val = lon2.Value * DEG_TO_RAD;
      var latH = Math.Sin(0.5 * (lat2Val - lat1Val));
      latH *= latH;
      var lonH = Math.Sin(0.5 * (lon2Val - lon1Val));
      lonH *= lonH;
      double a = latH + Math.Cos(lat1Val) * Math.Cos(lat2Val) * lonH;
      double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      double dist = EARTH_RADIUS * c;
      return dist;
    }

    /**
     * @param lon1
     * @param lat1
     * @param lon2
     * @param lat2
     *
     * @return the spherical cosine distance between the two points on the Earth's surface
     */
    public static double spherical_cosine_distance(FredGeo lon1, FredGeo lat1, FredGeo lon2, FredGeo lat2)
    {
      // convert to radians
      var lat1Val = lat1.Value * DEG_TO_RAD;
      var lon1Val = lon1.Value * DEG_TO_RAD;
      var lat2Val = lat2.Value * DEG_TO_RAD;
      var lon2Val = lon2.Value * DEG_TO_RAD;
      return Math.Acos(Math.Sin(lat1Val) * Math.Sin(lat2Val) + Math.Cos(lat1Val) * Math.Cos(lat2Val) * Math.Cos(lon2Val - lon1Val)) * EARTH_RADIUS;
    }

    /**
     * @param lon1
     * @param lat1
     * @param lon2
     * @param lat2
     *
     * @return the spherical projection distance between the two points on the Earth's surface
     */
    public static double spherical_projection_distance(FredGeo lon1, FredGeo lat1, FredGeo lon2, FredGeo lat2)
    {
      // convert to radians
      var lat1Val = lat1.Value * DEG_TO_RAD;
      var lon1Val = lon1.Value * DEG_TO_RAD;
      var lat2Val = lat2.Value * DEG_TO_RAD;
      var lon2Val = lon2.Value * DEG_TO_RAD;
      double dlat = (lat2Val - lat1Val);
      dlat *= dlat;
      double dlon = lon2Val - lon1Val;
      double tmp = Math.Cos(0.5 * (lat1Val + lat2Val)) * dlon;
      tmp *= tmp;
      return EARTH_RADIUS * Math.Sqrt(dlat + tmp);
    }

    public static double get_x(FredGeo lon)
    {
      return (lon.Value + 180.0) * km_per_deg_longitude;
    }

    public static double get_y(FredGeo lat)
    {
      return (lat.Value + 90.0) * km_per_deg_latitude;
    }

    public static FredGeo get_longitude(double x)
    {
      return new FredGeo { Value = (double)(x / km_per_deg_longitude - 180.0) };
    }

    public static FredGeo get_latitude(double y)
    {
      return new FredGeo { Value = (double)(y / km_per_deg_latitude - 90.0) };
    }

    public static double xy_distance(FredGeo lat1, FredGeo lon1, FredGeo lat2, FredGeo lon2)
    {
      double x1 = get_x(lon1); double y1 = get_y(lat1);
      double x2 = get_x(lon2); double y2 = get_y(lat2);
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public static double xsize_to_degree_longitude(double xsize)
    {
      return (xsize / km_per_deg_longitude);
    }

    public static double ysize_to_degree_latitude(double ysize)
    {
      return (ysize / km_per_deg_latitude);
    }
  }
}
