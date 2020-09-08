using System;

namespace Fred
{
  public static class DateExtensions
  {

    private static readonly int[,] day_table = new int[2, 13] {
      {0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31},
      {0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31}
    };

    private static readonly int[,] doomsday_month_val = new int[2, 13] {
      {0, 31, 28, 7, 4, 9, 6, 11, 8, 5, 10, 7, 12},
      {0, 32, 29, 7, 4, 9, 6, 11, 8, 5, 10, 7, 12}
    };

    public static bool is_leap_year(this DateTime date)
    {
      if (date.Year % 400 == 0)
        return true;
      if (date.Year % 100 == 0)
        return false;
      if (date.Year % 4 == 0)
        return true;
      return false;
    }

    public static int get_doomsday_century(this DateTime date)
    {
      int century = date.Year - (date.Year % 100);
      int r = -1;
      switch (century % 400)
      {
        case 0:
          r = 2;
          break;
        case 100:
          r = 0;
          break;
        case 200:
          r = 5;
          break;
        case 300:
          r = 3;
          break;
      }
      return r;
    }

    public static int get_days_in_month(this DateTime date)
    {
      return day_table[is_leap_year(date) ? 1 : 0, date.Month];
    }

    public static int get_doomsday_month(this DateTime date)
    {
      return doomsday_month_val[is_leap_year(date) ? 1 : 0, date.Month];
    }

    public static int get_day_of_week(this DateTime date)
    {
      int x = 0, y = 0;
      int weekday = -1;
      int ddcentury = -1;
      int ddmonth = get_doomsday_month(date);
      int century = date.Year - (date.Year % 100);

      ddcentury = get_doomsday_century(date);

      if (ddcentury < 0) return -1;
      if (ddmonth < 0) return -1;
      if (ddmonth > date.Day)
      {
        weekday = (7 - ((ddmonth - date.Day) % 7) + ddmonth);
      }
      else
      {
        weekday = date.Day;
      }

      x = (weekday - ddmonth);
      x %= 7;
      y = Convert.ToInt32(ddcentury + (date.Year - century) + (Math.Floor((double)((date.Year - century) / 4))));
      y %= 7;
      weekday = (x + y) % 7;

      return weekday;
    }
  }
}
