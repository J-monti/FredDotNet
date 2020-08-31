using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Fred
{
  public static class Utils
  {
    public static void FRED_STATUS(int verbosity, string format, params object[] args)
    {
      if (Global.Verbose > verbosity)
      {
        var argString = string.Empty;
        foreach (var arg in args)
        {
          argString += $" {arg}";
        }
        Console.WriteLine($"{format} *** ARGS: {argString}", args);
        Global.Statusfp.WriteLine($"{format} *** ARGS: {argString}", args);
      }
    }

    public static void fred_report(string format, params object[] args)
    {
      var argString = string.Empty;
      foreach (var arg in args)
      {
        argString += $" {arg}";
      }
      Console.WriteLine($"{format} *** ARGS: {argString}", args);
      Global.Outfp.WriteLine($"{format} *** ARGS: {argString}", args);
      Global.Statusfp.WriteLine($"{format} *** ARGS: {argString}", args);
    }

    public static void FRED_VERBOSE(int verbosity, string format, params object[] args)
    {
      if (Global.Verbose > verbosity)
      {
        var argString = string.Empty;
        foreach (var arg in args)
        {
          argString += $" {arg}";
        }
        Console.WriteLine($"{format} *** ARGS: {argString}", args);
      }
    }

    public static void FRED_CONDITIONAL_VERBOSE(int verbosity, bool condition, string format, params object[] args)
    {
      if (condition)
      {
        var argString = string.Empty;
        foreach (var arg in args)
        {
          argString += $" {arg}";
        }
        FRED_VERBOSE(verbosity, $"{format} *** ARGS: {argString}", args);
      }
    }

    public static void fred_abort(string format, params object[] args)
    {
      var argString = string.Empty;
      foreach (var arg in args)
      {
        argString += $" {arg}";
      }
      Console.WriteLine($"{format} *** ARGS: {argString}", args);
      Global.ErrorLogfp.WriteLine($"{format} *** ARGS: {argString}", args);
      throw new Exception(string.Format($"{format} *** ARGS: {argString}", args));
    }

    public static void assert(bool value)
    {
      if (!value)
      {
        throw new Exception();
      }
    }

    public static void get_fred_file_name(ref string map_file_name)
    {
      var fredHome = Environment.GetEnvironmentVariable("FRED_HOME");
      if (!string.IsNullOrWhiteSpace(fredHome))
      {
        map_file_name = Path.Combine(fredHome, map_file_name);
        return;
      }
      map_file_name = Path.Combine(Directory.GetCurrentDirectory(), map_file_name);
    }


    private static Stopwatch s_EpidemicTimer = new Stopwatch();
    public static void fred_start_epidemic_timer()
    {
      s_EpidemicTimer.Stop();
      s_EpidemicTimer.Start();
    }

    public static void fred_print_epidemic_timer(string message)
    {
      FRED_STATUS(0, $"{message} - {s_EpidemicTimer.Elapsed}");
    }

    public static void track_value(int day, string key, int value, int id = 0)
    {
      var key_str = id == 0 ? key : $"{key}_{id}";
      Global.Daily_Tracker.set_index_key_pair(day, key_str, value);
    }

    public static void track_value(int day, string key, double value, int id = 0)
    {
      var key_str = id == 0 ? key : $"{key}_{id}";
      Global.Daily_Tracker.set_index_key_pair(day, key_str, value);
    }

    public static void track_value(int day, string key, string value, int id = 0)
    {
      var key_str = id == 0 ? key : $"{key}_{id}";
      Global.Daily_Tracker.set_index_key_pair(day, key_str, value);
    }
  }
}
