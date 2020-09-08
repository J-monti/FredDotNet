using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public static class Travel
  {
    private static double mean_trip_duration;   // mean days per trip
    private static double[] Travel_Duration_Cdf;   // cdf for trip duration
    private static int max_Travel_Duration = 0;   // number of days in cdf
    private static Age_Map travel_age_prob = new Age_Map();
    private static Events return_queue = new Events();
    private static List<hub_record> hubs = new List<hub_record>();
    private static int[,] trips_per_day;
    private static int num_hubs;

    public static void setup(string directory)
    {
      Utils.assert(Global.Enable_Travel);
      read_hub_file();
      read_trips_per_day_file();
      setup_travelers_per_hub();
      travel_age_prob = new Age_Map("Travel Age Probability");
      travel_age_prob.read_from_input("travel_age_prob");
    }

    public static void read_hub_file()
    {
      string hub_file = string.Empty;
      FredParameters.GetParameter("travel_hub_file", ref hub_file);

      if (!File.Exists(hub_file))
      {
        Utils.fred_abort("Help! Can't open travel_hub_file {0}", hub_file);
      }
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "reading travel_hub_file {0}", hub_file);
      }
      hubs.Clear();
      using var fp = new StreamReader(hub_file);
      while (fp.Peek() != -1)
      {
        var line = fp.ReadLine();
        var tokens = line.Split(' ');
        var hub = new hub_record
        {
          users = new List<Person>(),
          id = Convert.ToInt32(tokens[0]),
          lat = Convert.ToInt32(tokens[1]),
          lon = Convert.ToInt32(tokens[2]),
          pop = Convert.ToInt32(tokens[3])
        };
        hubs.Add(hub);
      }
      fp.Dispose();

      num_hubs = hubs.Count;
      Console.Write("num_hubs = {0}", num_hubs);
      trips_per_day = new int[num_hubs, num_hubs];
    }

    public static void read_trips_per_day_file()
    {
      string trips_per_day_file = string.Empty;
      FredParameters.GetParameter("trips_per_day_file", ref trips_per_day_file);
      if (!File.Exists(trips_per_day_file))
      {
        Utils.fred_abort("Help! Can't open trips_per_day_file {0}", trips_per_day_file);
      }
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "reading trips_per_day_file {0}", trips_per_day_file);
      }

      using var fp = new StreamReader(trips_per_day_file);
      var contents = fp.ReadToEnd();
      contents = Utils.NormalizeWhiteSpace(contents);
      fp.Dispose();
      int index = 0;
      var tokens = contents.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < num_hubs; ++i)
      {
        for (int j = 0; j < num_hubs; ++j)
        {
          trips_per_day[i, j] = Convert.ToInt32(tokens[index]);
          index++;
        }
      }

      if (Global.Verbose > 1)
      {
        for (int i = 0; i < num_hubs; ++i)
        {
          for (int j = 0; j < num_hubs; ++j)
          {
            Console.WriteLine("{0} ", trips_per_day[i, j]);
          }
          Console.WriteLine();
        }

        Utils.FRED_STATUS(0, "finished reading trips_per_day_file {0}", trips_per_day_file);
      }
    }

    public static void setup_travelers_per_hub()
    {
      int households = Global.Places.get_number_of_households();
      Utils.FRED_VERBOSE(0, "Preparing to set households: %li \n", households);
      for (int i = 0; i < households; ++i)
      {
        var h = Global.Places.get_household_ptr(i);
        double h_lat = h.get_latitude();
        double h_lon = h.get_longitude();
        int census_tract_index = h.get_census_tract_index();
        long h_id = Global.Places.get_census_tract_with_index(census_tract_index);
        int c = h.get_county_index();
        int h_county = Global.Places.get_fips_of_county_with_index(c);
        Utils.FRED_VERBOSE(2, "h_id: %li h_county: %i \n", h_id, h_county);
        // find the travel hub closest to this household
        double max_distance = 166.0;    // travel at most 100 miles to nearest airport
        double min_dist = 100000000.0;
        int closest = -1;
        for (int j = 0; j < num_hubs; ++j)
        {
          double dist = Geo.xy_distance(h_lat, h_lon, hubs[j].lat, hubs[j].lon);
          if (dist < max_distance && dist < min_dist)
          {
            closest = j;
            min_dist = dist;
          }
          //Assign travelers to hub based on 'county' rather than distance
          if (hubs[j].id == h_county)
          {
            closest = j;
            min_dist = dist;
          }
        }
        if (closest > -1)
        {
          Utils.FRED_VERBOSE(1, "h_id: %li from county: %i  assigned to the airport: %i, distance:  %f\n", h_id, h_county, hubs[closest].id, min_dist);
          // add everyone in the household to the user list for this hub
          int housemates = h.get_size();
          for (int k = 0; k < housemates; ++k)
          {
            var person = h.get_enrollee(k);
            hubs[closest].users.Add(person);
          }
        }
      }

      // adjustment for partial user base
      for (int i = 0; i < num_hubs; ++i)
      {
        var hub = hubs[i];
        hub.pct = Convert.ToInt32(0.5 + (100.0 * hub.users.Count) / hub.pop);
        hubs[i] = hub;
      }
      // print hubs
      for (int i = 0; i < num_hubs; ++i)
      {
        Console.WriteLine("Hub {0}: lat = {1} lon = {2} users = {3} pop = {4} pct = {5}",
         hubs[i].id, hubs[i].lat, hubs[i].lon, hubs[i].users.Count,
         hubs[i].pop, hubs[i].pct);
      }
    }

    //public static void setup_travel_lists();

    public static void update_travel(int day)
    {
      if (!Global.Enable_Travel)
      {
        return;
      }

      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "update_travel entered day %d\n", day);
      }

      // initiate new trips
      for (int i = 0; i < num_hubs; ++i)
      {
        if (hubs[i].users.Count == 0)
        {
          continue;
        }
        for (int j = 0; j < num_hubs; ++j)
        {
          if (hubs[j].users.Count == 0)
          {
            continue;
          }
          int successful_trips = 0;
          int count = Convert.ToInt32((trips_per_day[i, j] * hubs[i].pct + 0.5) / 100);
          Utils.FRED_VERBOSE(1, "TRIPCOUNT day %d i %d j %d count %d\n", day, i, j, count);
          for (int t = 0; t < count; ++t)
          {
            // select a potential traveler determined by travel_age_prob param
            Person traveler = null;
            Person host = null;
            int attempts = 0;
            while (traveler == null && attempts < 100)
            {
              // select a random member of the source hub's user group
              int v = FredRandom.Next(0, hubs[i].users.Count - 1);
              traveler = hubs[i].users[v];
              double prob_travel_by_age = travel_age_prob.find_value(traveler.get_real_age());
              if (prob_travel_by_age < FredRandom.NextDouble())
              {
                traveler = null;
              }
              attempts++;
            }
            if (traveler != null)
            {
              // select a potential travel host determined by travel_age_prob param
              attempts = 0;
              while (host == null && attempts < 100)
              {
                // select a random member of the destination hub's user group
                int v = FredRandom.Next(0, hubs[j].users.Count - 1);
                host = hubs[j].users[v];

                //	          double prob_travel_by_age = travel_age_prob.find_value(host.get_real_age());
                //	          if(prob_travel_by_age < Random.draw_random()) {
                //	            host = null;
                //	          }

                attempts++;
              }
            }
            // travel occurs only if both traveler and host are not already traveling
            if (traveler != null && (!traveler.get_travel_status()) &&
               host != null && (!host.get_travel_status()))
            {
              // put traveler in travel status
              traveler.start_traveling(host);
              if (traveler.get_travel_status())
              {
                // put traveler on list for given number of days to travel
                int duration = FredRandom.DrawFromDistribution(max_Travel_Duration, Travel_Duration_Cdf);
                int return_sim_day = day + duration;
                Travel.add_return_event(return_sim_day, traveler);
                traveler.get_activities().set_return_from_travel_sim_day(return_sim_day);
                Utils.FRED_STATUS(1, "RETURN_FROM_TRAVEL EVENT ADDED today %d duration %d returns %d id %d age %d\n",
                day, duration, return_sim_day, traveler.get_id(), traveler.get_age());
                successful_trips++;
              }
            }
          }
          Utils.FRED_VERBOSE(1, "DAY %d SRC = %d DEST = %d TRIPS = %d\n", day, hubs[i].id, hubs[j].id, successful_trips);
        }
      }

      // process travelers who are returning home
      Travel.find_returning_travelers(day);

      if (Global.Verbose > 1)
      {
        Utils.FRED_STATUS(0, "update_travel finished");
      }

      return;
    }

    public static void find_returning_travelers(int day)
    {
      int size = return_queue.get_size(day);
      for (int i = 0; i < size; i++)
      {
        var person = return_queue.get_event(day, i);
        Utils.FRED_STATUS(1, "RETURNING FROM TRAVEL today %d id %d age %d\n",
        day, person.get_id(), person.get_age());
        person.stop_traveling();
      }
      return_queue.clear_events(day);

    }

    public static void terminate_person(Person person)
    {
      if (!person.get_travel_status())
      {
        return;
      }
      int return_day = person.get_activities().get_return_from_travel_sim_day();
      Utils.assert(Global.Simulation_Day <= return_day);
      delete_return_event(return_day, person);
    }

    public static void add_return_event(int day, Person person)
    {
      return_queue.add_event(day, person);
    }

    public static void delete_return_event(int day, Person person)
    {
      return_queue.delete_event(day, person);
    }

    public static void quality_control(string directory) { }
  }
}
