using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public static class Date
  {
    public const int MAX_DATES = (250 * 366);
    private static int year;
    private static int month;
    private static int day_of_month;
    private static int day_of_week;
    private static int day_of_year;
    private static int epi_week;
    private static int epi_year;
    private static int today; // index of Global::Simulation_Day in date array
    private static int sim_start_index; // index of Global::Simulation_Day=0 in date array
    private static date_t[] date;

    // names of days of week
    private readonly static string[] day_of_week_string = new []{
      "Sun","Mon","Tue","Wed","Thu","Fri","Sat"
    };

    // static const int EPOCH_START_YEAR = 1700;
    private static readonly int[,] day_table = new int[2, 13] {
      {0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31},
      {0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31}
    };

    private static readonly int[,] doomsday_month_val = new int[2, 13] {
      {0, 31, 28, 7, 4, 9, 6, 11, 8, 5, 10, 7, 12},
      {0, 32, 29, 7, 4, 9, 6, 11, 8, 5, 10, 7, 12}
    };

    public const int SUNDAY = 0;
    public const int MONDAY = 1;
    public const int TUESDAY = 2;
    public const int WEDNESDAY = 3;
    public const int THURSDAY = 4;
    public const int FRIDAY = 5;
    public const int SATURDAY = 6;
    public const int JANUARY = 1;
    public const int FEBRUARY = 2;
    public const int MARCH = 3;
    public const int APRIL = 4;
    public const int MAY = 5;
    public const int JUNE = 6;
    public const int JULY = 7;
    public const int AUGUST = 8;
    public const int SEPTEMBER = 9;
    public const int OCTOBER = 10;
    public const int NOVEMBER = 11;
    public const int DECEMBER = 12;
    public const int INVALID = -1;

    public static void setup_dates(string date_string)
    {
      // extract date from date string
      int _year;
      int _month;
      int _day_of_month;
      var dateSplit = date_string.Split('-');
      if (dateSplit.Length < 3)
      {
        Utils.fred_abort("setup_dates cannot parse date string {0}", date_string);
        return;
      }
      else
      {
        _year = Convert.ToInt32(dateSplit[0]);
        _month = Convert.ToInt32(dateSplit[1]);
        _day_of_month = Convert.ToInt32(dateSplit[2]);
      }

      date = new date_t[MAX_DATES];
      int epoch_year = _year - 125;
      date[0].year = epoch_year;
      date[0].month = 1;
      date[0].day_of_month = 1;
      date[0].day_of_year = 1;

      // assign the right epi week number:
      int jan_1_day_of_week = Date.get_day_of_week(epoch_year, 1, 1);
      int dec_31_day_of_week = (jan_1_day_of_week + (Date.is_leap_year(epoch_year) ? 365 : 364)) % 7;
      bool short_week;
      if (jan_1_day_of_week < 3)
      {
        date[0].epi_week = 1;
        date[0].epi_year = epoch_year;
        short_week = false;
      }
      else
      {
        date[0].epi_week = 52;
        date[0].epi_year = epoch_year - 1;
        short_week = true;
      }

      for (int i = 0; i < MAX_DATES - 1; i++)
      {
        int new_year = date[i].year;
        int new_month = date[i].month;
        int new_day_of_month = date[i].day_of_month + 1;
        int new_day_of_year = date[i].day_of_year + 1;
        int new_day_of_week = (date[i].day_of_week + 1) % 7;
        if (new_day_of_month > Date.get_days_in_month(new_month, new_year))
        {
          new_day_of_month = 1;
          if (new_month < 12)
          {
            new_month++;
          }
          else
          {
            new_year++;
            new_month = 1;
            new_day_of_year = 1;
          }
        }
        date[i + 1].year = new_year;
        date[i + 1].month = new_month;
        date[i + 1].day_of_month = new_day_of_month;
        date[i + 1].day_of_year = new_day_of_year;
        date[i + 1].day_of_week = new_day_of_week;

        // set epi_week and epi_year
        if (new_month == 1 && new_day_of_month == 1)
        {
          jan_1_day_of_week = new_day_of_week;
          dec_31_day_of_week = (jan_1_day_of_week + (Date.is_leap_year(new_year) ? 365 : 364)) % 7;
          if (jan_1_day_of_week <= 3)
          {
            date[i + 1].epi_week = 1;
            date[i + 1].epi_year = new_year;
            short_week = false;
          }
          else
          {
            date[i + 1].epi_week = date[i].epi_week;
            date[i + 1].epi_year = date[i].epi_year;
            short_week = true;
          }
        }
        else
        {
          if ((new_month == 1) && short_week && (new_day_of_month <= 7 - jan_1_day_of_week))
          {
            date[i + 1].epi_week = date[i].epi_week;
            date[i + 1].epi_year = date[i].epi_year;
          }
          else
          {
            if ((new_month == 12) &&
                (dec_31_day_of_week < 3) &&
                (31 - dec_31_day_of_week) <= new_day_of_month)
            {
              date[i + 1].epi_week = 1;
              date[i + 1].epi_year = new_year + 1;
            }
            else
            {
              date[i + 1].epi_week = (short_week ? 0 : 1) + (jan_1_day_of_week + new_day_of_year - 1) / 7;
              date[i + 1].epi_year = new_year;
            }
          }
        }


        // set offset 
        if (date[i].year == _year && date[i].month == _month && date[i].day_of_month == _day_of_month)
        {
          today = i;
        }
      }
      year = date[today].year;
      month = date[today].month;
      day_of_month = date[today].day_of_month;
      day_of_week = date[today].day_of_week;
      day_of_year = date[today].day_of_year;
      epi_week = date[today].epi_week;
      epi_year = date[today].epi_year;
      sim_start_index = today;
    }

    public static void update()
    {
      today++;
      year = date[today].year;
      month = date[today].month;
      day_of_month = date[today].day_of_month;
      day_of_week = date[today].day_of_week;
      day_of_year = date[today].day_of_year;
      epi_week = date[today].epi_week;
      epi_year = date[today].epi_year;
    }
    public static int get_year() { return year; }
    public static int get_year(int sim_day) { return date[sim_start_index + sim_day].year; }
    public static int get_month() { return month; }
    public static int get_month(int sim_day) { return date[sim_start_index + sim_day].month; }
    public static int get_day_of_month() { return day_of_month; }
    public static int get_day_of_month(int sim_day) { return date[sim_start_index + sim_day].day_of_month; }
    public static int get_day_of_week() { return day_of_week; }
    public static int get_day_of_week(int sim_day) { return date[sim_start_index + sim_day].day_of_week; }
    public static int get_day_of_year() { return day_of_year; }
    public static int get_day_of_year(int sim_day) { return date[sim_start_index + sim_day].day_of_year; }
    public static int get_epi_week() { return epi_week; }
    public static int get_epi_week(int sim_day) { return date[sim_start_index + sim_day].epi_week; }
    public static int get_epi_year() { return epi_year; }
    public static int get_epi_year(int sim_day) { return date[sim_start_index + sim_day].epi_year; }
    public static bool is_weekend()
    {
      int day = get_day_of_week();
      return (day == SATURDAY || day == SUNDAY);
    }
    public static bool is_weekend(int sim_day)
    {
      int day = get_day_of_week(sim_day);
      return (day == SATURDAY || day == SUNDAY);
    }
    public static bool is_weekday() { return (is_weekend() == false); }
    public static bool is_weekday(int sim_day) { return (is_weekend(sim_day) == false); }
    public static bool is_leap_year() { return is_leap_year(year); }
    public static bool is_leap_year(int year)
    {
      if (year % 400 == 0)
        return true;
      if (year % 100 == 0)
        return false;
      if (year % 4 == 0)
        return true;
      return false;
    }

    public static string get_date_string()
    {
      return string.Format("{0}-{1}-{2}", year, month, day_of_month);
    }
    public static string get_day_of_week_string()
    {
      return day_of_week_string[day_of_week];
    }
    public static int get_sim_day(int y, int m, int d)
    {
      if ((Date.is_leap_year(y) == false) && m == 2 && d == 29)
      {
        d = 28;
      }
      int yr = Date.get_year(0);
      int day = (y - yr) * 365;
      while (Date.get_year(day) < y)
      {
        day += 365;
      }
      while (Date.get_month(day) < m)
      {
        day -= Date.get_days_in_month(m, y);
      }
      while (Date.get_month(day) > m)
      {
        day -= Date.get_days_in_month(m, y);
      }
      day += d - Date.get_day_of_month(day);
      Utils.FRED_STATUS(0, "{0}-{1}-{2} {3}={4}-{5}", y, m, d, Date.get_year(day), Date.get_month(day), Date.get_day_of_month(day));
      return day;
    }

    private static int get_doomsday_month(int month, int year)
    {
      return Date.doomsday_month_val[(Date.is_leap_year(year) ? 1 : 0), month];
    }
    private static int get_doomsday_century(int year)
    {
      int century = year - (year % 100);
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

    private static int get_day_of_week(int year, int month, int day_of_month)
    {
      int x = 0, y = 0;
      int weekday = -1;
      int ddcentury = -1;
      int ddmonth = Date.get_doomsday_month(month, year);
      int century = year - (year % 100);

      ddcentury = Date.get_doomsday_century(year);

      if (ddcentury < 0) return -1;
      if (ddmonth < 0) return -1;
      if (ddmonth > day_of_month)
      {
        weekday = (7 - ((ddmonth - day_of_month) % 7) + ddmonth);
      }
      else
      {
        weekday = day_of_month;
      }

      x = (weekday - ddmonth);
      x %= 7;
      y = ddcentury + (year - century) + Convert.ToInt32(Math.Floor((double)((year - century) / 4)));
      y %= 7;
      weekday = (x + y) % 7;

      return weekday;
    }

    private static int get_days_in_month(int month, int year)
    {
      return day_table[(Date.is_leap_year(year) ? 1 : 0), month];
    }

    private struct date_t
    {
      public int year;
      public int month;
      public int day_of_month;
      public int day_of_week;
      public int day_of_year;
      public int epi_week;
      public int epi_year;
    };
  }
}
