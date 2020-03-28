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

    public static void SetKmPerDegree(double lat)
    {
      lat *= DEG_TO_RAD;
      double cosine = Math.Cos(lat);
      km_per_deg_longitude = cosine * KM_PER_DEG_LAT;
      km_per_deg_latitude = KM_PER_DEG_LAT;
    }

    public static double HaversineDistance(double lon1, double lat1, double lon2, double lat2)
    {
      // convert to radians
      lat1 *= DEG_TO_RAD;
      lon1 *= DEG_TO_RAD;
      lat2 *= DEG_TO_RAD;
      lon2 *= DEG_TO_RAD;
      double latH = Math.Sin(0.5 * (lat2 - lat1));
      latH *= latH;
      double lonH = Math.Sin(0.5 * (lon2 - lon1));
      lonH *= lonH;
      double a = latH + Math.Cos(lat1) * Math.Cos(lat2) * lonH;
      double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      double dist = EARTH_RADIUS * c;
      return dist;
    }

    public static double SphericalCosineDistance(double lon1, double lat1, double lon2, double lat2)
    {
      // convert to radians
      lat1 *= DEG_TO_RAD;
      lon1 *= DEG_TO_RAD;
      lat2 *= DEG_TO_RAD;
      lon2 *= DEG_TO_RAD;
      return Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1)) * EARTH_RADIUS;
    }

    public static double SphericalProjectionDistance(double lon1, double lat1, double lon2, double lat2)
    {
      // convert to radians
      lat1 *= DEG_TO_RAD;
      lon1 *= DEG_TO_RAD;
      lat2 *= DEG_TO_RAD;
      lon2 *= DEG_TO_RAD;
      double dlat = (lat2 - lat1);
      dlat *= dlat;
      double dlon = (lon2 - lon1);
      double tmp = Math.Cos(0.5 * (lat1 + lat2)) * dlon;
      tmp *= tmp;
      return EARTH_RADIUS * Math.Sqrt(dlat + tmp);
    }

    public static double GetX(double lon)
    {
      return (lon + 180.0) * km_per_deg_longitude;
    }

    public static double GetY(double lat)
    {
      return (lat + 90.0) * km_per_deg_latitude;
    }
    public static double GetLongitude(double x)
    {
      return x / km_per_deg_longitude - 180.0;
    }
    public static double GetLatitude(double y)
    {
      return y / km_per_deg_latitude - 90.0;
    }

    public static double XYDistance(double lat1, double lon1, double lat2, double lon2)
    {
      double x1 = GetX(lon1); double y1 = GetY(lat1);
      double x2 = GetX(lon2); double y2 = GetY(lat2);
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public static double XSizeToDegreeLongitude(double xsize)
    {
      return xsize / km_per_deg_longitude;
    }

    public static double YSizeToDegreeLatitude(double ysize)
    {
      return ysize / km_per_deg_latitude;
    }
  }
}
