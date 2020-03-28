using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Travel
  {
    private static List<Events> s_returnQueue = new List<Events>();
    void setup(string directory)
    {
      read_hub_file();
      read_trips_per_day_file();
      setup_travelers_per_hub();
      travel_age_prob = new Age_Map("Travel Age Probability");
      travel_age_prob.read_from_input("travel_age_prob");
    }

    void read_hub_file()
    {
      char hub_file[FRED_STRING_SIZE];
      Params::get_param_from_string("travel_hub_file", hub_file);
      FILE* fp = Utils::fred_open_file(hub_file);
      if (fp == NULL)
      {
        Utils::fred_abort("Help! Can't open travel_hub_file %s\n", hub_file);
      }
      if (Global::Verbose > 0)
      {
        fprintf(Global::Statusfp, "reading travel_hub_file %s\n", hub_file);
        fflush(stdout);
      }
      hubs.clear();
      hub_t hub;
      hub.users.clear();
      while (fscanf(fp, "%d %lf %lf %d", &hub.id, &hub.lat, &hub.lon, &hub.pop) == 4)
      {
        hubs.push_back(hub);
      }
      fclose(fp);
      num_hubs = (int)hubs.size();
      printf("num_hubs = %d\n", num_hubs);
      trips_per_day = new int*[num_hubs];
      for (int i = 0; i < num_hubs; ++i)
      {
        trips_per_day[i] = new int[num_hubs];
      }
    }

    void read_trips_per_day_file()
    {
      char trips_per_day_file[FRED_STRING_SIZE];
      Params::get_param_from_string("trips_per_day_file", trips_per_day_file);
      FILE* fp = Utils::fred_open_file(trips_per_day_file);
      if (fp == NULL)
      {
        Utils::fred_abort("Help! Can't open trips_per_day_file %s\n", trips_per_day_file);
      }
      if (Global::Verbose > 0)
      {
        fprintf(Global::Statusfp, "reading trips_per_day_file %s\n", trips_per_day_file);
        fflush(stdout);
      }
      for (int i = 0; i < num_hubs; ++i)
      {
        for (int j = 0; j < num_hubs; ++j)
        {
          int n;
          if (fscanf(fp, "%d ", &n) != 1)
          {
            Utils::fred_abort("ERROR: read failed on file %s", trips_per_day_file);
          }
          trips_per_day[i][j] = n;
        }
      }
      fclose(fp);
      if (Global::Verbose > 1)
      {
        for (int i = 0; i < num_hubs; ++i)
        {
          for (int j = 0; j < num_hubs; ++j)
          {
            printf("%d ", trips_per_day[i][j]);
          }
          printf("\n");
        }
        fflush(stdout);
        fprintf(Global::Statusfp, "finished reading trips_per_day_file %s\n", trips_per_day_file);
      }
    }

    void setup_travelers_per_hub()
    {
      int households = Global::Places.get_number_of_households();
      FRED_VERBOSE(0, "Preparing to set households: %li \n", households);
      for (int i = 0; i < households; ++i)
      {
        Household* h = Global::Places.get_household_ptr(i);
        double h_lat = h.get_latitude();
        double h_lon = h.get_longitude();
        int census_tract_index = h.get_census_tract_index();
        long int h_id = Global::Places.get_census_tract_with_index(census_tract_index);
        int c = h.get_county_index();
        int h_county = Global::Places.get_fips_of_county_with_index(c);
        FRED_VERBOSE(2, "h_id: %li h_county: %i \n", h_id, h_county);
        // find the travel hub closest to this household
        double max_distance = 166.0;    // travel at most 100 miles to nearest airport
        double min_dist = 100000000.0;
        int closest = -1;
        for (int j = 0; j < num_hubs; ++j)
        {
          double dist = Geo::xy_distance(h_lat, h_lon, hubs[j].lat, hubs[j].lon);
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
          FRED_VERBOSE(1, "h_id: %li from county: %i  assigned to the airport: %i, distance:  %f\n", h_id, h_county, hubs[closest].id, min_dist);
          // add everyone in the household to the user list for this hub
          int housemates = h.get_size();
          for (int k = 0; k < housemates; ++k)
          {
            Person* person = h.get_enrollee(k);
            hubs[closest].users.push_back(person);
          }
        }
      }

      // adjustment for partial user base
      for (int i = 0; i < num_hubs; ++i)
      {
        hubs[i].pct = 0.5 + (100.0 * hubs[i].users.size()) / hubs[i].pop;
      }
      // print hubs
      for (int i = 0; i < num_hubs; ++i)
      {
        printf("Hub %d: lat = %f lon = %f users = %d pop = %d pct = %d\n",
         hubs[i].id, hubs[i].lat, hubs[i].lon, static_cast<int>(hubs[i].users.size()),
         hubs[i].pop, hubs[i].pct);
      }
      fflush(stdout);
    }

    void update_travel(int day)
    {

      if (!Global::Enable_Travel)
      {
        return;
      }

      if (Global::Verbose > 0)
      {
        fprintf(Global::Statusfp, "update_travel entered day %d\n", day);
        fflush(Global::Statusfp);
      }

      // initiate new trips
      for (int i = 0; i < num_hubs; ++i)
      {
        if (hubs[i].users.size() == 0)
        {
          continue;
        }
        for (int j = 0; j < num_hubs; ++j)
        {
          if (hubs[j].users.size() == 0)
          {
            continue;
          }
          int successful_trips = 0;
          int count = (trips_per_day[i][j] * hubs[i].pct + 0.5) / 100;
          FRED_VERBOSE(1, "TRIPCOUNT day %d i %d j %d count %d\n", day, i, j, count);
          for (int t = 0; t < count; ++t)
          {
            // select a potential traveler determined by travel_age_prob param
            Person* traveler = NULL;
            Person* host = NULL;
            int attempts = 0;
            while (traveler == NULL && attempts < 100)
            {
              // select a random member of the source hub's user group
              int v = Random::draw_random_int(0, static_cast<int>(hubs[i].users.size()) - 1);
              traveler = hubs[i].users[v];
              double prob_travel_by_age = travel_age_prob.find_value(traveler.get_real_age());
              if (prob_travel_by_age < Random::draw_random())
              {
                traveler = NULL;
              }
              attempts++;
            }
            if (traveler != NULL)
            {
              // select a potential travel host determined by travel_age_prob param
              attempts = 0;
              while (host == NULL && attempts < 100)
              {
                // select a random member of the destination hub's user group
                int v = Random::draw_random_int(0, static_cast<int>(hubs[j].users.size()) - 1);
                host = hubs[j].users[v];

                //	          double prob_travel_by_age = travel_age_prob.find_value(host.get_real_age());
                //	          if(prob_travel_by_age < Random::draw_random()) {
                //	            host = NULL;
                //	          }

                attempts++;
              }
            }
            // travel occurs only if both traveler and host are not already traveling
            if (traveler != NULL && (!traveler.get_travel_status()) &&
               host != NULL && (!host.get_travel_status()))
            {
              // put traveler in travel status
              traveler.start_traveling(host);
              if (traveler.get_travel_status())
              {
                // put traveler on list for given number of days to travel
                int duration = Random::draw_from_distribution(max_Travel_Duration, Travel_Duration_Cdf);
                int return_sim_day = day + duration;
                add_return_event(return_sim_day, traveler);
                traveler.get_activities().set_return_from_travel_sim_day(return_sim_day);
                FRED_STATUS(1, "RETURN_FROM_TRAVEL EVENT ADDED today %d duration %d returns %d id %d age %d\n",
                day, duration, return_sim_day, traveler.get_id(), traveler.get_age());
                successful_trips++;
              }
            }
          }
          FRED_VERBOSE(1, "DAY %d SRC = %d DEST = %d TRIPS = %d\n", day, hubs[i].id, hubs[j].id, successful_trips);
        }
      }

      // process travelers who are returning home
      find_returning_travelers(day);

      if (Global::Verbose > 1)
      {
        fprintf(Global::Statusfp, "update_travel finished\n");
        fflush(Global::Statusfp);
      }

      return;
    }

    void find_returning_travelers(int day)
    {
      int size = return_queue.get_size(day);
      for (int i = 0; i < size; i++)
      {
        Person* person = return_queue.get_event(day, i);
        FRED_STATUS(1, "RETURNING FROM TRAVEL today %d id %d age %d\n",
        day, person.get_id(), person.get_age());
        person.stop_traveling();
      }
      return_queue.clear_events(day);
    }

    void terminate_person(Person* person)
    {
      if (!person.get_travel_status())
      {
        return;
      }
      int return_day = person.get_activities().get_return_from_travel_sim_day();
      assert(Global::Simulation_Day <= return_day);
      delete_return_event(return_day, person);
    }

    void quality_control(char* directory)
    {
    }

    void add_return_event(int day, Person* person)
    {
      return_queue.add_event(day, person);
    }

    void delete_return_event(int day, Person* person)
    {
      return_queue.delete_event(day, person);
    }


  }
}
