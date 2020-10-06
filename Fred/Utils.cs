using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Fred
{
  public static class Utils
  {
    private static string ErrorFilename;
    private static DateTime startTime;
    private static Stopwatch stopwatch = new Stopwatch();
    private static Stopwatch day_stopwatch = new Stopwatch();

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

    internal static void fred_start_timer()
    {
      startTime = DateTime.Now;
      stopwatch.Start();
    }

    internal static void fred_start_initialization_timer()
    {
      throw new NotImplementedException();
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

    internal static void fred_open_output_files()
    {
      int run = Global.Simulation_run_number;
      var filename  = string.Empty;
      var directory = Global.Simulation_directory;

      // ErrorLog file is created at the first warning or error
      Global.ErrorLogfp = null;
      ErrorFilename = string.Format("{0}/err{1}.txt", directory, run);

      filename = string.Format("{0}/out{1}.txt", directory, run);
      Global.Outfp = new StreamWriter(filename);
      if (Global.Outfp == null)
      {
        Utils.fred_abort("Can't open{0}", filename);
      }
      Global.Tracefp = null;
      if (Global.Tracefilebase != "none")
      {
        filename = string.Format("{0}/trace{1}.txt", directory, run);
        Global.Tracefp = new StreamWriter(filename);
        if (Global.Tracefp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
      Global.Infectionfp = null;
      if (Global.Track_infection_events > 0)
      {
        filename = string.Format("{0}/infections{1}.txt", directory, run);
        Global.Infectionfp = new StreamWriter(filename);
        if (Global.Infectionfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
      Global.VaccineTracefp = null;
      if (Global.VaccineTracefilebase != "none")
      {
        filename = string.Format("{0}/vacctr{1}.txt", directory, run);
        Global.VaccineTracefp = new StreamWriter(filename);
        if (Global.VaccineTracefp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
      Global.Birthfp = null;
      if (Global.Enable_Population_Dynamics)
      {
        filename = string.Format("{0}/births{1}.txt", directory, run);
        Global.Birthfp = new StreamWriter(filename);
        if (Global.Birthfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
      Global.Deathfp = null;
      if (Global.Enable_Population_Dynamics)
      {
        filename = string.Format("{0}/deaths{1}.txt", directory, run);
        Global.Deathfp = new StreamWriter(filename);
        if (Global.Deathfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
      Global.Immunityfp = null;
      if (Global.Immunityfilebase != "none")
      {
        filename = string.Format("{0}/immunity{1}.txt", directory, run);
        Global.Immunityfp = new StreamWriter(filename);
        if (Global.Immunityfp == null)
        {
          Utils.fred_abort("Help! Can't open{0}", filename);
        }
        Global.Report_Immunity = true;
      }
      Global.Householdfp = null;
      if (Global.Print_Household_Locations)
      {
        filename = string.Format("{0}/households.txt", directory);
        Global.Householdfp = new StreamWriter(filename);
        if (Global.Householdfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }

      Global.Tractfp = null;
      if (Global.Report_Epidemic_Data_By_Census_Tract)
      {
        filename = string.Format("{0}/tracts{1}.txt", directory, run);
        Global.Tractfp = new StreamWriter(filename);
        if (Global.Tractfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }

      Global.IncomeCatfp = null;
      if (Global.Report_Mean_Household_Stats_Per_Income_Category)
      {
        filename = string.Format("{0}/income_category{1}.txt", directory, run);
        Global.IncomeCatfp = new StreamWriter(filename);
        if (Global.IncomeCatfp == null)
        {
          Utils.fred_abort("Can't open{0}", filename);
        }
      }
    }

    public static void assert(bool value)
    {
      if (!value)
      {
        throw new Exception();
      }
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

    public static string get_fred_file_name(string ending)
    {
      if (ending.StartsWith("$FRED_HOME"))
      {
        var env = Environment.GetEnvironmentVariable("FRED_HOME");
        return ending.Replace("$FRED_HOME", env);
      }

      return ending;
    }

    /// <summary>
    ///  Any consecutive white-space (including tabs, newlines) is replaced with whatever is in normalizeTo.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="normalizeTo">Character which is replacing whitespace.</param>
    /// <remarks>Based on http://stackoverflow.com/a/25023688/897326 </remarks>
    public static string NormalizeWhiteSpace(string input, char normalizeTo = ' ')
    {
      if (string.IsNullOrEmpty(input))
      {
        return string.Empty;
      }

      var output = new StringBuilder();
      bool skipped = false;

      foreach (char c in input)
      {
        if (char.IsWhiteSpace(c))
        {
          if (!skipped)
          {
            output.Append(normalizeTo);
            skipped = true;
          }
        }
        else
        {
          skipped = false;
          output.Append(c);
        }
      }

      return output.ToString();
    }

    internal static void fred_print_lap_time(string format, params object[] args)
    {
      FRED_VERBOSE(0, "{0} - {1}", string.Format(format, args), stopwatch.Elapsed);
    }

    internal static void fred_print_initialization_timer()
    {
      fred_print_lap_time("Initialization Complete");
    }

    internal static void fred_print_wall_time(string format, params object[] args)
    {
      FRED_VERBOSE(0, "{0} - {1}", string.Format(format, args), DateTime.Now);
    }

    internal static void fred_start_day_timer()
    {
      day_stopwatch.Reset();
      day_stopwatch.Start();
    }

    internal static void fred_print_day_timer(int day)
    {
      day_stopwatch.Stop();
      FRED_STATUS(0, "DAY_TIMER day {0} took {1} seconds\n\n", day, day_stopwatch.Elapsed);
    }

    internal static void fred_start_sim_timer()
    {
      Global.Simulation_Stopwatch = new Stopwatch();
      Global.Simulation_Stopwatch.Start();
    }

    internal static void fred_print_sim_time()
    {
      Global.Simulation_Stopwatch.Stop();
      FRED_STATUS(0, "\nFRED simulation complete. Excluding initialization, {0} days -- took {1}", Global.Days, Global.Simulation_Stopwatch.Elapsed);
    }

    internal static void fred_end()
    {
      // This is a function that cleans up FRED and exits
      if (Global.Outfp != null)
      {
        Global.Outfp.Flush();
        Global.Outfp.Dispose();
      }
      if (Global.Tracefp != null)
      {
        Global.Tracefp.Flush();
        Global.Tracefp.Dispose();
      }
      if (Global.Infectionfp != null)
      {
        Global.Infectionfp.Flush();
        Global.Infectionfp.Dispose();
      }
      if (Global.VaccineTracefp != null)
      {
        Global.VaccineTracefp.Flush();
        Global.VaccineTracefp.Dispose();
      }
      if (Global.Prevfp != null)
      {
        Global.Prevfp.Flush();
        Global.Prevfp.Dispose();
      }
      if (Global.Incfp != null)
      {
        Global.Incfp.Flush();
        Global.Incfp.Dispose();
      }
      if (Global.Immunityfp != null)
      {
        Global.Immunityfp.Flush();
        Global.Immunityfp.Dispose();
      }
      if (Global.Householdfp != null)
      {
        Global.Householdfp.Flush();
        Global.Householdfp.Dispose();
      }
      if (Global.Tractfp != null)
      {
        Global.Tractfp.Flush();
        Global.Tractfp.Dispose();
      }
      if (Global.IncomeCatfp != null)
      {
        Global.IncomeCatfp.Flush();
        Global.IncomeCatfp.Dispose();
      }
    }
  }
}
