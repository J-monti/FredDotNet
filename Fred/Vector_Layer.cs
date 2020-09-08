using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fred
{
  public class Vector_Layer : Abstract_Grid
  {
    private Vector_Patch[,] grid;       // Rectangular array of patches

    // fixed parameters for this disease vector
    private double infection_efficiency;
    private double transmission_efficiency;
    private double place_seeding_probability;
    private double mosquito_seeds;
    private double death_rate;
    private double birth_rate;
    private double bite_rate;
    private double incubation_rate;
    private double suitability;
    private double pupae_per_host;
    private double life_span;
    private double sucess_rate;
    private double female_ratio;

    private int vector_pop;
    private int total_infected_vectors;
    private int school_infected_vectors;
    private int workplace_infected_vectors;
    private int household_infected_vectors;
    private int neighborhood_infected_vectors;
    private int total_susceptible_vectors;
    private int total_infected_hosts;
    private int total_infectious_hosts;
    private int school_vectors;
    private int workplace_vectors;
    private int household_vectors;
    private int neighborhood_vectors;

    // vector control parameters
    private int total_places_in_vector_control;
    private int schools_in_vector_control;
    private int households_in_vector_control;
    private int workplaces_in_vector_control;
    private int neighborhoods_in_vector_control;
    private int vector_control_day_on;
    private int vector_control_day_off;
    private int vector_control_max_places;
    private int vector_control_places_enrolled;
    private int vector_control_random;
    private double vector_control_threshold;
    private double vector_control_coverage;
    private double vector_control_efficacy;
    private double vector_control_neighborhoods_rate;

    private static bool Enable_Vector_Control;
    private static bool School_Vector_Control;
    private static bool Workplace_Vector_Control;
    private static bool Household_Vector_Control;
    private static bool Limit_Vector_Control;
    private static bool Neighborhood_Vector_Control;

    private readonly List<int> census_tracts_with_vector_control = new List<int>();
    private readonly List<county_record> county_set = new List<county_record>();
    private readonly List<census_tract_record> census_tract_set = new List<census_tract_record>();

    public Vector_Layer()
    {
      var base_grid = Global.Simulation_Region;
      this.min_lat = base_grid.get_min_lat();
      this.min_lon = base_grid.get_min_lon();
      this.max_lat = base_grid.get_max_lat();
      this.max_lon = base_grid.get_max_lon();
      this.min_x = base_grid.get_min_x();
      this.min_y = base_grid.get_min_y();
      this.max_x = base_grid.get_max_x();
      this.max_y = base_grid.get_max_y();
      this.total_infected_vectors = 0;
      this.total_infectious_hosts = 0;
      this.total_infected_hosts = 0;

      this.death_rate = 1.0 / 18.0;
      this.birth_rate = 1.0 / 18.0;
      this.bite_rate = 0.76;
      this.incubation_rate = 1.0 / 11.0;
      this.suitability = 1.0;
      FredParameters.GetParameter("pupae_per_host", ref this.pupae_per_host);
      this.life_span = 18.0; // From Chao and longini
      this.sucess_rate = 0.83; // Focks 2000
      this.female_ratio = 0.5; // Focks 2000

      // get vector_control parameters
      int temp_int = 0;
      FredParameters.GetParameter("enable_vector_control", ref temp_int);
      Vector_Layer.Enable_Vector_Control = (temp_int == 0 ? false : true);
      if (Vector_Layer.Enable_Vector_Control)
      {
        FredParameters.GetParameter("school_vector_control", ref temp_int);
        Vector_Layer.School_Vector_Control = (temp_int == 0 ? false : true);
        FredParameters.GetParameter("workplace_vector_control", ref temp_int);
        Vector_Layer.Workplace_Vector_Control = (temp_int == 0 ? false : true);
        FredParameters.GetParameter("household_vector_control", ref temp_int);
        Vector_Layer.Household_Vector_Control = (temp_int == 0 ? false : true);
        FredParameters.GetParameter("neighborhood_vector_control", ref temp_int);
        Vector_Layer.Neighborhood_Vector_Control = (temp_int == 0 ? false : true);
        FredParameters.GetParameter("limit_vector_control", ref temp_int);
        Vector_Layer.Limit_Vector_Control = (temp_int == 0 ? false : true);
        FredParameters.GetParameter("vector_control_threshold", ref vector_control_threshold);
        FredParameters.GetParameter("vector_control_day_on", ref vector_control_day_on);
        FredParameters.GetParameter("vector_control_day_off", ref vector_control_day_off);
        FredParameters.GetParameter("vector_control_coverage", ref vector_control_coverage);
        FredParameters.GetParameter("vector_control_efficacy", ref vector_control_efficacy);
        FredParameters.GetParameter("vector_control_neighborhoods_rate", ref vector_control_neighborhoods_rate);
        FredParameters.GetParameter("vector_control_max_places", ref vector_control_max_places);
        FredParameters.GetParameter("vector_control_random", ref vector_control_random);
      }

      // determine patch size for this layer
      FredParameters.GetParameter("vector_patch_size", ref this.patch_size);
      // Get probabilities of transmission 
      FredParameters.GetParameter("vector_infection_efficiency", ref this.infection_efficiency);
      FredParameters.GetParameter("vector_transmission_efficiency", ref this.transmission_efficiency);
      FredParameters.GetParameter("place_seeding_probability", ref this.place_seeding_probability);
      FredParameters.GetParameter("mosquito_seeds", ref this.mosquito_seeds);
      // determine number of rows and cols
      this.rows = Convert.ToInt32((this.max_y - this.min_y) / this.patch_size);
      if (this.min_y + this.rows * this.patch_size < this.max_y)
      {
        this.rows++;
      }

      this.cols = Convert.ToInt32((this.max_x - this.min_x) / this.patch_size);
      if (this.min_x + this.cols * this.patch_size < this.max_x)
      {
        this.cols++;
      }

      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "Vector_Layer min_lon = {0}", this.min_lon);
        Utils.FRED_STATUS(0, "Vector_Layer min_lat = {0}", this.min_lat);
        Utils.FRED_STATUS(0, "Vector_Layer max_lon = {0}", this.max_lon);
        Utils.FRED_STATUS(0, "Vector_Layer max_lat = {0}", this.max_lat);
        Utils.FRED_STATUS(0, "Vector_Layer rows =  {0}  cols =  {1}", this.rows, this.cols);
        Utils.FRED_STATUS(0, "Vector_Layer min_x = {0}  min_y = {1}", this.min_x, this.min_y);
        Utils.FRED_STATUS(0, "Vector_Layer max_x = {0}  max_y = {1}", this.max_x, this.max_y);
      }

      this.grid = new Vector_Patch[this.rows, this.cols];
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          this.grid[i, j] = new Vector_Patch();
          this.grid[i, j].setup(i, j, this.patch_size, this.min_x, this.min_y);
        }
      }
      // To read the temperature grid
      this.read_temperature();
      // Read location where a proportion of mosquitoes susceptibles are infected externaly (birth or migration)
      this.read_vector_seeds();
    }

    public void setup()
    {
      int num_households = Global.Places.get_number_of_households();
      for (int i = 0; i < num_households; ++i)
      {
        var house = Global.Places.get_household_ptr(i);
        add_hosts(house);
      }
      if (Vector_Layer.Enable_Vector_Control)
      {
        this.setup_vector_control_by_census_tract();
      }
    }

    //public Vector_Patch get_neighbors(int row, int col);

    public Vector_Patch get_patch(int row, int col)
    {
      if (row >= 0 && col >= 0 && row < this.rows && col < this.cols)
      {
        return this.grid[row, col];
      }
      else
      {
        return null;
      }

    }
    public Vector_Patch get_patch(FredGeo lat, FredGeo lon)
    {
      int row = get_row(lat);
      int col = get_col(lon);
      return get_patch(row, col);
    }

    public Vector_Patch select_random_patch()
    {
      int row = FredRandom.Next(0, this.rows - 1);
      int col = FredRandom.Next(0, this.cols - 1);
      return this.grid[row, col];

    }
    public void quality_control()
    {
      if (Global.Verbose != 0)
      {
        Utils.FRED_STATUS(0, "vector grid quality control check");
      }

      for (int row = 0; row < this.rows; ++row)
      {
        for (int col = 0; col < this.cols; ++col)
        {
          this.grid[row, col].quality_control();
        }
      }

      if (Global.Verbose != 0)
      {
        Utils.FRED_STATUS(0, "vector grid quality control finished");
      }
    }

    public void update(int day)
    {
      this.total_infected_vectors = 0;
      this.total_infected_hosts = 0;
      this.total_infectious_hosts = 0;
      if (Vector_Layer.Enable_Vector_Control)
      {
        this.update_vector_control_by_census_tract(day);
      }
      Utils.FRED_VERBOSE(1, "Vector_Layer.update() entered on day %d\n", day);
      // Global.Daily_Tracker.log_key_value("Vec_I", total_infected_vectors);
      // Global.Daily_Tracker.log_key_value("Vec_H", total_infectious_hosts);
    }

    public void update_visualization_data(int disease_id, int day)
    {
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          var patch = this.grid[i, j];
          int count = 0; // patch.get_infected_vectors();
          if (count > 0)
          {
            double x = patch.get_center_x();
            double y = patch.get_center_y();
            Global.Visualization.update_data(x, y, count, 0);
          }
        }
      }
    }

    public void add_hosts(Place p)
    {
      /*
      fred.geo lat = p.get_latitude();
      fred.geo lon = p.get_longitude();
      int hosts = p.get_size();
      Vector_Patch* patch = get_patch(lat,lon);
      if(patch != null) {
        patch.add_hosts(hosts);
      }
      */
    }

    public double get_vectors_per_host(Place place)
    {

      double development_time = 1.0;

      //temperatures vs development times..FOCKS2000: DENGUE TRANSMISSION THRESHOLDS
      var temps = new[] { 15.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0, 32.0 };  //temperatures
      var dev_times = new []{ 8.49, 3.11, 4.06, 3.3, 2.66, 2.04, 1.46, 0.92 };//development times

      double temperature = -999.9;
      var lat = place.get_latitude();
      var lon = place.get_longitude();
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        temperature = patch.get_temperature();
      }
      if (temperature > 32)
      {
        temperature = 32;
      }
      double vectors_per_host;
      if (temperature <= 18)
      {
        vectors_per_host = 0;
      }
      else
      {
        for (int i = 0; i < 8; i++)
        {
          if (temperature <= temps[i])
          {
            //obtain the development time using linear interpolation
            development_time = dev_times[i - 1] + (dev_times[i] - dev_times[i - 1]) / (temps[i] - temps[i - 1]) * (temperature - temps[i - 1]);
            break;
          }
        }
        vectors_per_host = pupae_per_host * female_ratio * sucess_rate * life_span / development_time;
      }
      Utils.FRED_VERBOSE(1, "SET TEMP: place %s lat %lg lon %lg temp %f devtime %f vectors_per_host %f N_orig %d\n",
             place.get_label(), place.get_latitude(), place.get_longitude(), temperature, development_time, vectors_per_host, place.get_orig_size());
      return vectors_per_host;
    }

    public double get_seeds(Place p, int dis, int day)
    {
      FredGeo lat = p.get_latitude();
      FredGeo lon = p.get_longitude();
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        return patch.get_seeds(dis, day);
      }
      else
      {
        return 0.0;
      }
    }

    public void add_host(Person person, Place place)
    {
      /*
      if(place.is_neighborhood()) {
        return;
      }
      fred.geo lat = place.get_latitude();
      fred.geo lon = place.get_longitude();
      Vector_Patch* patch = get_patch(lat,lon);
      if(patch != null) {
        patch.add_host(person);
      }
      */
    }

    public void read_temperature()
    {
      double patch_temperature;
      FredGeo lat;
      FredGeo lon;
      var filename = string.Empty;
      FredParameters.GetParameter("temperature_grid_file", ref filename);
      //fp = Utils.fred_open_file(filename);
      //Obtain temperature values for each patch...lat,lon,oC
      double house_;
      double reservoir_;
      double breteau_;
      if (File.Exists(filename))
      {
        using var fp = new StreamReader(filename);
        while (fp.Peek() != -1)
        {
          var line = fp.ReadLine();
          var tokens = line.Split(',');
          lat = Convert.ToDouble(tokens[0]);
          lon = Convert.ToDouble(tokens[1]);
          patch_temperature = Convert.ToDouble(tokens[2]);
          house_ = Convert.ToDouble(tokens[3]);
          reservoir_ = Convert.ToDouble(tokens[4]);
          breteau_ = Convert.ToDouble(tokens[5]);
          var patch = get_patch(lat, lon);
          if (patch != null)
          {
            patch.set_temperature(patch_temperature);
          }
        }
        fp.Dispose();
      }
      else
      {
        Utils.fred_abort("Cannot  open %s to read the average temperature grid \n", filename);
      }
    }

    public void read_vector_seeds()
    {
      // first I will try with all seeded 
      var filename = string.Empty;
      FredParameters.GetParameter("vector_seeds_file", ref filename);
      //  printf("seeds filename: %s\n", filename);
      if (File.Exists(filename))
      {
        using var fp = new StreamReader(filename);
        while (fp.Peek() != -1)
        {
          var linestring = fp.ReadLine();
          var tokens = linestring.Split(' ');
          int day_on = Convert.ToInt32(tokens[0]);
          int day_off = Convert.ToInt32(tokens[1]);
          int dis = Convert.ToInt32(tokens[2]);
          double lat_ = Convert.ToDouble(tokens[3]);
          double lon_ = Convert.ToDouble(tokens[4]);
          double radius_ = Convert.ToDouble(tokens[5]); //in kilometers
          if (radius_ > 0)
          {
            Utils.FRED_VERBOSE(0, "Attempting to seed infectious vectors %lg proportion in %lg proportion of houses, day_on %d day_off %d disease %d lat %lg lon %lg radius %lg \n", mosquito_seeds, place_seeding_probability, day_on, day_off, dis, lat_, lon_, radius_);
            //make a list of houses in the radius
            FredGeo lat = lat_;
            FredGeo lon = lon_;
            if (this.mosquito_seeds < 0)
            {
              this.mosquito_seeds = 0;
            }
            if ((dis >= 0) && (dis < Global.MAX_NUM_DISEASES) && (day_on >= 0) && (day_off >= 0))
            {
              this.seed_patches_by_distance_in_km(lat, lon, radius_, dis, day_on, day_off, this.mosquito_seeds);
            }
          }
          else
          {
            Utils.FRED_VERBOSE(0, "Attempting to seed infectious vectors %lg proportion in %lg proportion of houses, day_on %d day_off %d disease %d in all houses \n", mosquito_seeds, place_seeding_probability, day_on, day_off, dis);
            if (this.mosquito_seeds < 0)
            {
              this.mosquito_seeds = 0;
            }
            if ((dis >= 0) && (dis < Global.MAX_NUM_DISEASES) && (day_on >= 0) && (day_off >= 0))
            {
              for (int i = 0; i < this.rows; ++i)
              {
                for (int j = 0; j < this.cols; ++j)
                {
                  this.grid[i, j].set_vector_seeds(dis, day_on, day_off, this.mosquito_seeds);
                }
              }
            }
          }
        }
        fp.Dispose();
      }
    }

    public void swap_county_people()
    {
      int cols = Global.Simulation_Region.get_cols();
      int rows = Global.Simulation_Region.get_rows();
      for (int i = 0; i < rows; ++i)
      {
        for (int j = 0; j < cols; ++j)
        {
          var region_patch = Global.Simulation_Region.get_patch(i, j);
          int pop_size = region_patch.get_popsize();
          if (pop_size > 0)
          {
            region_patch.swap_county_people();
          }
        }
      }
    }

    public int get_total_infected_vectors() { return total_infected_vectors; }
    public int get_total_infected_hosts() { return total_infected_hosts; }
    public int get_total_infectious_hosts() { return total_infectious_hosts; }
    public void init_prior_immunity_by_county()
    {
      this.get_county_ids();
      this.get_immunity_from_file();
      this.get_people_size_by_age();
      this.immunize_total_by_age();
      Utils.FRED_VERBOSE(0, "Number of counties %d\n", county_set.Count);
      for (int i = 0; i < county_set.Count; ++i)
      {
        if (county_set[i].habitants.Count > 0)
        {
          Utils.FRED_VERBOSE(0, "County id:  %d Population %d People Immunized: %d\n", county_set[i].id, county_set[i].habitants.Count, county_set[i].people_immunized);
          //      for (int j = 0; j < 102; ++j) {
          //	      FRED_VERBOSE(0,"AGE.  %d \t immune prob %lg\t people_by_age %d\n", j,county_set[i].immunity_by_age[j], county_set[i].people_by_age[j]);
          //	    }
        }
      }
      //  Utils.fred_abort("Running test finishes here\n");
    }

    public void init_prior_immunity_by_county(int d)
    {
      this.get_county_ids();
      this.get_immunity_from_file(d);
      Utils.FRED_VERBOSE(2, "Number of counties %d\n", county_set.Count);
      this.get_people_size_by_age();
      this.immunize_by_age(d);
      for (int i = 0; i < county_set.Count; ++i)
      {
        if (county_set[i].habitants.Count > 0)
        {
          Utils.FRED_VERBOSE(0, "County id:  %d Population %d People Immunized: %d Strain %d\n", county_set[i].id, county_set[i].habitants.Count, county_set[i].people_immunized, d);
          //      for (int j = 0; j < 102; ++j){
          //	      FRED_VERBOSE(0,"AGE.  %d \t immune prob %lg\t people_by_age %d\n", j,county_set[i].immunity_by_age[j], county_set[i].people_by_age[j]);
          //	    }
        }
      }
    }

    public double get_infection_efficiency() { return infection_efficiency; }
    public double get_transmission_efficiency() { return transmission_efficiency; }

    public double get_seeds(Place p, int dis)
    {
      var lat = p.get_latitude();
      var lon = p.get_longitude();
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        double seeds_ = patch.get_seeds(dis);
        if (seeds_ > 0)
        {
          if (FredRandom.NextDouble() < this.place_seeding_probability)
          {
            return seeds_;
          }
          else
          {
            return 0.0;
          }
        }
        else
        {
          return 0.0;
        }
      }
      else
      {
        return 0.0;
      }
    }

    public double get_day_start_seed(Place p, int dis)
    {
      var lat = p.get_latitude();
      var lon = p.get_longitude();
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        return patch.get_day_start_seed(dis);
      }
      else
      {
        return 0;
      }
    }

    public double get_day_end_seed(Place p, int dis)
    {
      var lat = p.get_latitude();
      var lon = p.get_longitude();
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        return patch.get_day_end_seed(dis);
      }
      else
      {
        return 0;
      }
    }

    public void report(int day, Epidemic epidemic)
    {
      get_vector_population(epidemic.get_id());
      epidemic.track_value(day, "Nv", vector_pop);
      epidemic.track_value(day, "Nvs", school_vectors);
      epidemic.track_value(day, "Nvw", workplace_vectors);
      epidemic.track_value(day, "Nvh", household_vectors);
      epidemic.track_value(day, "Nvn", neighborhood_vectors);
      epidemic.track_value(day, "Iv", total_infected_vectors);
      epidemic.track_value(day, "Ivs", school_infected_vectors);
      epidemic.track_value(day, "Ivw", workplace_infected_vectors);
      epidemic.track_value(day, "Ivh", household_infected_vectors);
      epidemic.track_value(day, "Ivn", neighborhood_infected_vectors);
      epidemic.track_value(day, "Sv", total_susceptible_vectors);
      if (Enable_Vector_Control)
      {
        epidemic.track_value(day, "Pvc", total_places_in_vector_control);
        epidemic.track_value(day, "Svc", schools_in_vector_control);
        epidemic.track_value(day, "Hvc", households_in_vector_control);
        epidemic.track_value(day, "Wvc", workplaces_in_vector_control);
        epidemic.track_value(day, "Nvc", neighborhoods_in_vector_control);
      }
    }

    public vector_disease_data_t update_vector_population(int day, Place place)
    {

      place.mark_vectors_as_not_infected_today();

      vector_disease_data_t v = place.get_vector_disease_data();

      if (day > vector_control_day_off)
      {
        place.stop_vector_control();
      }

      if (place.is_neighborhood())
      {
        return v;
      }

      if (v.N_vectors <= 0)
      {
        return v;
      }

      if (place.get_vector_control_status())
      {
        int N_host_orig = place.get_orig_size();
        v.N_vectors = Convert.ToInt32(N_host_orig * this.get_vectors_per_host(place) * (1 - vector_control_efficacy));
        if (v.N_vectors < 0)
        {
          v.N_vectors = 0;
        }
        Utils.FRED_VERBOSE(1, "update vector pop.Vector_control day %d place %s  N_vectors: %d efficacy: %f\n", day, place.get_label(), v.N_vectors, vector_control_efficacy);
      }
      /*else{
        int N_host_orig = place.get_orig_size();
        v.N_vectors = N_host_orig * this.get_vectors_per_host(place);
        }*/
      Utils.FRED_VERBOSE(1, "update vector pop: day %d place %s initially: S %d, N: %d\n",
             day, place.get_label(), v.S_vectors, v.N_vectors);

      var born_infectious = new int [Vector_Patch.DISEASE_TYPES];
      int total_born_infectious = 0;
      int lifespan_ = Convert.ToInt32(1 / this.death_rate);

      // new vectors are born susceptible
      if (v.N_vectors < lifespan_)
      {
        for (int k = 0; k < v.N_vectors; k++)
        {
          double r = FredRandom.NextDouble();
          if (r < this.birth_rate)
          {
            v.S_vectors++;
          }
        }
        for (int k = 0; k < v.S_vectors; k++)
        {
          double r = FredRandom.NextDouble();
          if (r < this.death_rate)
          {
            v.S_vectors--;
          }
        }
      }
      else
      {
        v.S_vectors += Convert.ToInt32(Math.Floor(this.birth_rate * v.N_vectors - this.death_rate * v.S_vectors));
      }
      Utils.FRED_VERBOSE(1, "vector_update_population. S_vector: %d, N_vectors: %d\n", v.S_vectors, v.N_vectors);

      // but some are infected
      for (int d = 0; d < Vector_Patch.DISEASE_TYPES; d++)
      {
        double seeds = place.get_seeds(d, day);
        born_infectious[d] = Convert.ToInt32(Math.Ceiling(v.S_vectors * seeds));
        total_born_infectious += born_infectious[d];
        if (born_infectious[d] > 0)
        {
          Utils.FRED_VERBOSE(1, "vector_update_population. Vector born infectious disease[%d] = %d \n", d, born_infectious[d]);
          Utils.FRED_VERBOSE(1, "Total Vector born infectious: %d \n", total_born_infectious);
        }
      }

      v.S_vectors -= total_born_infectious;
      Utils.FRED_VERBOSE(1, "vector_update_population - seeds. S_vector: %d, N_vectors: %d\n", v.S_vectors, v.N_vectors);
      // print this 
      if (total_born_infectious > 0)
      {
        Utils.FRED_VERBOSE(1, "Total Vector born infectious: %d \n", total_born_infectious);// 
      }
      if (v.S_vectors < 0)
      {
        v.S_vectors = 0;
      }

      // accumulate total number of vectors
      v.N_vectors = v.S_vectors;
      // we assume vectors can have at most one infection, if not susceptible
      for (int i = 0; i < Vector_Patch.DISEASE_TYPES; ++i)
      {
        // some die
        Utils.FRED_VERBOSE(1, "vector_update_population. E_vectors[%d] = %d \n", i, v.E_vectors[i]);
        if (v.E_vectors[i] < lifespan_ && v.E_vectors[i] > 0)
        {
          for (int k = 0; k < v.E_vectors[i]; ++k)
          {
            double r = FredRandom.NextDouble();
            if (r < this.death_rate)
            {
              v.E_vectors[i]--;
            }
          }
        }
        else
        {
          v.E_vectors[i] -= Convert.ToInt32(Math.Floor(this.death_rate * v.E_vectors[i]));
        }

        // some become infectious
        int become_infectious = 0;
        if (v.E_vectors[i] < lifespan_)
        {
          for (int k = 0; k < v.E_vectors[i]; ++k)
          {
            double r = FredRandom.NextDouble();
            if (r < this.incubation_rate)
            {
              become_infectious++;
            }
          }
        }
        else
        {
          become_infectious = Convert.ToInt32(Math.Floor(this.incubation_rate * v.E_vectors[i]));
        }

        // int become_infectious = floor(incubation_rate * E_vectors[i]);
        Utils.FRED_VERBOSE(1, "vector_update_population. become infectious [%d] = %d, incubation rate: %f,E_vectors[%d] %d \n", i,
         become_infectious, this.incubation_rate, i, v.E_vectors[i]);
        v.E_vectors[i] -= become_infectious;
        if (v.E_vectors[i] < 0) v.E_vectors[i] = 0;
        // some die
        Utils.FRED_VERBOSE(1, "vector_update_population. I_Vectors[%d] = %d \n", i, v.I_vectors[i]);
        if (v.I_vectors[i] < lifespan_ && v.I_vectors[i] > 0)
        {
          for (int k = 0; k < v.I_vectors[i]; k++)
          {
            double r = FredRandom.NextDouble();
            if (r < this.death_rate)
            {
              v.I_vectors[i]--;
            }
          }
        }
        else
        {
          v.I_vectors[i] -= Convert.ToInt32(Math.Floor(this.death_rate * v.I_vectors[i]));
        }
        Utils.FRED_VERBOSE(1, "vector_update_population. I_Vectors[%d] = %d \n", i, v.I_vectors[i]);
        // some become infectious
        v.I_vectors[i] += become_infectious;
        Utils.FRED_VERBOSE(1, "vector_update_population. I_Vectors[%d] = %d \n", i, v.I_vectors[i]);
        // some were born infectious
        v.I_vectors[i] += born_infectious[i];
        Utils.FRED_VERBOSE(1, "vector_update_population.+= born infectious I_Vectors[%d] = %d,born infectious[%d] = %d \n", i,
         v.I_vectors[i], i, born_infectious[i]);
        if (v.I_vectors[i] < 0)
        {
          v.I_vectors[i] = 0;
        }
        // add to the total
        v.N_vectors += (v.E_vectors[i] + v.I_vectors[i]);
        Utils.FRED_VERBOSE(1, "update_vector_population entered S_vectors %d E_Vectors[%d] %d  I_Vectors[%d] %d N_Vectors %d\n",
         v.S_vectors, i, v.E_vectors[i], i, v.I_vectors[i], v.N_vectors);

      }
      return v;
    }

    public double get_bite_rate() { return this.bite_rate; }
    public void get_vector_population(int disease_id)
    {
      vector_pop = 0;
      total_infected_vectors = 0;
      total_susceptible_vectors = 0;
      school_vectors = 0;
      workplace_vectors = 0;
      household_vectors = 0;
      neighborhood_vectors = 0;
      school_infected_vectors = 0;
      workplace_infected_vectors = 0;
      household_infected_vectors = 0;
      neighborhood_infected_vectors = 0;
      schools_in_vector_control = 0;
      households_in_vector_control = 0;
      workplaces_in_vector_control = 0;
      neighborhoods_in_vector_control = 0;
      total_places_in_vector_control = 0;

      int places = Global.Places.get_number_of_households();
      for (int i = 0; i < places; i++)
      {
        var place = Global.Places.get_household(i);
        household_vectors += place.get_vector_population_size();
        household_infected_vectors += place.get_infected_vectors(disease_id);
        if (place.get_vector_control_status())
        {
          households_in_vector_control++;
        }
      }

      // skip neighborhoods?
      /*
      places = Global.Places.get_number_of_neighborhoods();
      for (int i = 0; i < places; i++) {
        Place *place = Global.Places.get_neighborhood(i);
        neighborhood_vectors += place.get_vector_population_size();
        neighborhood_infected_vectors += place.get_infected_vectors(disease_id);
        if(place.get_vector_control_status()){
          neighborhoods_in_vector_control++;
        }
      }
      */

      places = Global.Places.get_number_of_schools();
      for (int i = 0; i < places; i++)
      {
        var place = Global.Places.get_school(i);
        school_vectors += place.get_vector_population_size();
        school_infected_vectors += place.get_infected_vectors(disease_id);
        if (place.get_vector_control_status())
        {
          schools_in_vector_control++;
        }
      }

      places = Global.Places.get_number_of_workplaces();
      for (int i = 0; i < places; i++)
      {
        var place = Global.Places.get_workplace(i);
        workplace_vectors += place.get_vector_population_size();
        workplace_infected_vectors += place.get_infected_vectors(disease_id);
        if (place.get_vector_control_status())
        {
          workplaces_in_vector_control++;
        }
      }

      places = Global.Places.get_number_of_places();
      for (int i = 0; i < places; i++)
      {
        var place = Global.Places.get_place(i);
        total_susceptible_vectors += place.get_susceptible_vectors();
      }

      vector_pop = school_vectors + workplace_vectors + household_vectors + neighborhood_vectors;
      total_infected_vectors = school_infected_vectors + workplace_infected_vectors + household_infected_vectors + neighborhood_infected_vectors;

      total_places_in_vector_control = schools_in_vector_control + households_in_vector_control + workplaces_in_vector_control + neighborhoods_in_vector_control;

      Utils.assert(vector_pop == total_infected_vectors + total_susceptible_vectors);
    }

    public List<int> read_vector_control_tracts(string filename)
    {
      var temp_tracts = new List<int>();
      filename = Utils.get_fred_file_name(filename);
      if (File.Exists(filename))
      {
        using var fp = new StreamReader(filename);
        while (fp.Peek() != -1)
        {
          var line = fp.ReadLine();
          int temp_census_tract_ = Convert.ToInt32(line);
          if (temp_census_tract_ > 100)
          {
            temp_tracts.Add(temp_census_tract_);
          }
        }
        fp.Dispose();
      }
      else
      {
        Utils.fred_abort("Cannot  open %s to read the vector control tracts\n", filename);
      }

      return temp_tracts;
    }

    public void setup_vector_control_by_census_tract()
    {
      Utils.FRED_VERBOSE(0, "setup_vector_control_by_census_tract entered\n");
      string filename = string.Empty;
      int cols_n = Global.Neighborhoods.get_cols();
      int rows_n = Global.Neighborhoods.get_rows();
      FredParameters.GetParameter("vector_control_census_tracts_file", ref filename);
      census_tracts_with_vector_control.Clear();
      vector_control_places_enrolled = 0;

      //read census tracts to implement vector control
      census_tracts_with_vector_control.AddRange(this.read_vector_control_tracts(filename));
      int total_census_tracts = Global.Places.get_number_of_census_tracts();

      //Load census tracts in simulation and assign eligibility for vector control

      for (int i = 0; i < total_census_tracts; i++)
      {
        census_tract_record census_temp = new census_tract_record();
        census_temp.ind = Global.Places.get_census_tract_with_index(i);
        census_temp.total_neighborhoods = 0;
        census_temp.first_day_infectious = -1;
        census_temp.popsize = 0;
        census_temp.threshold = 1000.00;
        census_temp.eligible_for_vector_control = false;
        census_temp.exceeded_threshold = false;
        for (int j = 0; j < census_tracts_with_vector_control.Count; j++)
        {
          if (census_temp.ind == census_tracts_with_vector_control[j])
          {
            census_temp.eligible_for_vector_control = true;
            census_temp.threshold = this.vector_control_threshold;
          }
        }
        census_tract_set.Add(census_temp);
      }

      // For each neighborhood that implements vector control, allocate the neighborhoods in the census tract set

      for (int i = 0; i < rows_n; i++)
      {
        for (int j = 0; j < cols_n; j++)
        {
          var neighborhood_temp = Global.Neighborhoods.get_patch(i, j);
          int pop_size = neighborhood_temp.get_popsize();
          if (pop_size > 0)
          {
            var h = (Household)neighborhood_temp.select_random_household();
            int t = h.get_census_tract_index();
            neighborhood_temp.get_neighborhood().set_census_tract_index(t);
            census_tract_set[t].popsize += pop_size;
            if (census_tract_set[t].eligible_for_vector_control == true)
            {
              census_tract_set[t].total_neighborhoods++;
              census_tract_set[t].neighborhoods.Add(neighborhood_temp);
              census_tract_set[t].non_infectious_neighborhoods.Add(neighborhood_temp);
            }
          }
        }
      }
      for (int i = 0; i < total_census_tracts; i++)
      {
        if (census_tract_set[i].eligible_for_vector_control)
        {
          Utils.FRED_VERBOSE(0, "setup_vector_control Census_tract: %d is eligible for vector control with %d neighborhoods threshold %f cols %d rows %d pop %d\n", census_tract_set[i].ind, census_tract_set[i].total_neighborhoods, census_tract_set[i].threshold, cols_n, rows_n, census_tract_set[i].popsize);
        }
      }
      Utils.FRED_VERBOSE(0, "setup_vector_control_by_census_tract finished\n");

    }
    public int select_places_for_vector_control(Neighborhood_Patch patch_n, int day)
    {
      int places_enrolled = 0;
      if (patch_n != null)
      {
        Utils.FRED_VERBOSE(1, "select_places_for_vector_control entered\n");
        patch_n.set_vector_control_status(2);
        int places = 0;
        if (Household_Vector_Control)
        {
          places = patch_n.get_number_of_households();
          for (int i = 0; i < places; i++)
          {
            double r = FredRandom.NextDouble();
            if (r < vector_control_coverage)
            {
              var place = patch_n.get_household(i);
              place.set_vector_control();
              places_enrolled++;
            }
          }
        }

        if (School_Vector_Control)
        {
          places = patch_n.get_number_of_schools();
          for (int i = 0; i < places; i++)
          {
            double r = FredRandom.NextDouble();
            if (r < vector_control_coverage)
            {
              var place = patch_n.get_school(i);
              place.set_vector_control();
              places_enrolled++;
            }
          }
        }

        if (Workplace_Vector_Control)
        {
          places = patch_n.get_number_of_workplaces();
          for (int i = 0; i < places; i++)
          {
            double r = FredRandom.NextDouble();
            if (r < vector_control_coverage)
            {
              var place = patch_n.get_workplace(i);
              place.set_vector_control();
              places_enrolled++;
            }
          }
        }
        // skip neighborhoods?
        /*
          places = Global.Places.get_number_of_neighborhoods();
          for (int i = 0; i < places; i++) {
          Place *place = Global.Places.get_neighborhood(i);
          neighborhood_vectors += place.get_vector_population_size();
          neighborhood_infected_vectors += place.get_infected_vectors(disease_id);
          if(place.get_vector_control_status()){
          neighborhoods_in_vector_control++;
          }
          }
        */

      }
      else
      {
        Utils.FRED_VERBOSE(1, "select_places_for_vector_control patch null\n");
      }
      return places_enrolled;
    }

    public void update_vector_control_by_census_tract(int day)
    {
      Utils.FRED_VERBOSE(1, "update_vector_control_by_census_tract entered day %d\n", day);
      if (vector_control_places_enrolled >= vector_control_max_places && Limit_Vector_Control == true)
      {
        return;
      }
      int total_census_tracts = Global.Places.get_number_of_census_tracts();
      for (int i = 0; i < total_census_tracts; i++)
      {
        if (census_tract_set[i].eligible_for_vector_control && (census_tract_set[i].first_day_infectious >= 0))
        {
          int symp_incidence_by_tract = 0;
          for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
          {
            var disease = Global.Diseases.get_disease(d);
            var epidemic = disease.get_epidemic();
            symp_incidence_by_tract += epidemic.get_symptomatic_incidence_by_tract_index(i);
          }
          //calculate the weekly incidence rate by 100.000 inhabitants
          double symp_incidence_ = (double)symp_incidence_by_tract / (double)census_tract_set[i].popsize * 100000.0 * 7.0;
          Utils.FRED_VERBOSE(1, "Census_tract: %d, symp_incidence %lg population %d symp_incidence_tract %d\n", census_tract_set[i].ind, symp_incidence_, census_tract_set[i].popsize, symp_incidence_by_tract);
          if (symp_incidence_ >= census_tract_set[i].threshold && census_tract_set[i].exceeded_threshold == false)
          {
            census_tract_set[i].exceeded_threshold = true;
            // if it is the capital of the county, then all the census tracts of the county are chosen for vector control
            int census_end = Convert.ToInt32(census_tract_set[i].ind % 1000);
            Utils.FRED_VERBOSE(1, "Census_tract: %d threshold exceeded\n", census_tract_set[i].ind);
            if (census_end == 1)
            {
              Utils.FRED_VERBOSE(1, "Capital : Census_tract: %d\n", census_tract_set[i].ind);
              int county_capital = Convert.ToInt32(Math.Floor((double)(census_tract_set[i].ind / 1000)));
              for (int k = 0; k < total_census_tracts; k++)
              {
                int county_temp = Convert.ToInt32(Math.Floor((double)(census_tract_set[k].ind / 1000)));
                if (county_temp == county_capital)
                {
                  Utils.FRED_VERBOSE(1, "Census_tract: %d and %d same county, threshold exceeded\n", census_tract_set[i].ind, census_tract_set[k].ind);
                  census_tract_set[k].exceeded_threshold = true;
                }
              }
            }
          }
          if (census_tract_set[i].exceeded_threshold == true)
          {
            int total_neighborhoods_enrolled = 0;
            int max_n = 0;
            int neighborhoods_enrolled_today = Convert.ToInt32(Math.Floor(vector_control_neighborhoods_rate * census_tract_set[i].total_neighborhoods));
            Utils.FRED_VERBOSE(1, "update_vector_control_by_census_tract.Census_tract: %d, %d infectious neighborhoods, neighborhoods to enroll %d\n", census_tract_set[i].ind, census_tract_set[i].infectious_neighborhoods.Count, neighborhoods_enrolled_today);
            if (census_tract_set[i].infectious_neighborhoods.Count > 0)
            {
              max_n = (neighborhoods_enrolled_today <= census_tract_set[i].infectious_neighborhoods.Count ? neighborhoods_enrolled_today : census_tract_set[i].infectious_neighborhoods.Count);
              for (int j = 0; j < max_n; j++)
              {
                var p_n = census_tract_set[i].infectious_neighborhoods.First();
                vector_control_places_enrolled += this.select_places_for_vector_control(p_n, day);
                census_tract_set[i].infectious_neighborhoods.RemoveAt(0);
                census_tract_set[i].vector_control_neighborhoods.Add(p_n);
                total_neighborhoods_enrolled++;
                if (vector_control_places_enrolled >= vector_control_max_places && Limit_Vector_Control == true)
                {
                  break;
                }
              }
            }
            if (census_tract_set[i].non_infectious_neighborhoods.Count > 0)
            {
              var non_inf_neighborhoods = new List<Neighborhood_Patch>();
              census_tract_set[i].non_infectious_neighborhoods.Shuffle();
              for (int k = 0; k < census_tract_set[i].non_infectious_neighborhoods.Count; k++)
              {
                if (census_tract_set[i].non_infectious_neighborhoods[k].get_vector_control_status() == 0)
                {
                  non_inf_neighborhoods.Add(census_tract_set[i].non_infectious_neighborhoods[k]);
                }
              }
              census_tract_set[i].non_infectious_neighborhoods.Clear();
              census_tract_set[i].non_infectious_neighborhoods.AddRange(non_inf_neighborhoods);
              non_inf_neighborhoods.Clear();
              int neighborhoods_to_enroll = neighborhoods_enrolled_today - max_n;
              if (neighborhoods_to_enroll > census_tract_set[i].non_infectious_neighborhoods.Count)
              {
                neighborhoods_to_enroll = census_tract_set[i].non_infectious_neighborhoods.Count;
              }
              if (neighborhoods_to_enroll > 0)
              {
                for (int j = 0; j < neighborhoods_to_enroll; j++)
                {
                  var p_n = census_tract_set[i].non_infectious_neighborhoods.First();
                  vector_control_places_enrolled += this.select_places_for_vector_control(p_n, day);
                  census_tract_set[i].vector_control_neighborhoods.Add(p_n);
                  census_tract_set[i].non_infectious_neighborhoods.RemoveAt(0);
                  total_neighborhoods_enrolled++;
                  if (vector_control_places_enrolled >= vector_control_max_places && Limit_Vector_Control == true)
                  {
                    break;
                  }
                }
              }
            }
            Utils.FRED_VERBOSE(1, "update_vector_control_by_census_tract.Census_tract: %d,total neighborhoods enrolled %d, neighborhoods to enroll %d, neighborhoods enrolled %d places enrolled %d\n", census_tract_set[i].ind, census_tract_set[i].vector_control_neighborhoods.Count, neighborhoods_enrolled_today, total_neighborhoods_enrolled, vector_control_places_enrolled);
          }
        }
      }

      Utils.FRED_VERBOSE(1, "update_vector_control_by_census_tract_finished\n");
      // this.make_eligible_for_vector_control(neighborhood_temp);
    }

    public void add_infectious_patch(Place p, int day)
    {
      int col_ = Global.Neighborhoods.get_col(p.get_longitude());
      int row_ = Global.Neighborhoods.get_row(p.get_latitude());
      var patch_n = Global.Neighborhoods.get_patch(row_, col_);
      var n_ = patch_n.get_neighborhood();
      if (n_ == null)
      {
        Utils.FRED_VERBOSE(1, "add_infectious_patch neighborhood is null\n");
        return;
      }
      int tract_index_ = n_.get_census_tract_index();
      if (tract_index_ < 0)
      {
        Utils.FRED_VERBOSE(1, "add_infectious_patch tract_index is < 0\n");
        return;
      }
      Utils.FRED_VERBOSE(1, "add_infectious_patch entered day %d tract %d . %d\n", day, tract_index_, census_tract_set[tract_index_].ind);
      if (census_tract_set[tract_index_].eligible_for_vector_control)
      {
        if (census_tract_set[tract_index_].first_day_infectious == -1)
        {
          census_tract_set[tract_index_].first_day_infectious = day;
          Utils.FRED_VERBOSE(1, "add_infectious_patch.Census_tract: %d First day of symptomatics %d \n", census_tract_set[tract_index_].ind, day);
        }
        if (vector_control_random == 0 && patch_n.get_vector_control_status() == 0)
        {
          census_tract_set[tract_index_].infectious_neighborhoods.Add(patch_n);
          //vector control status: 0 . non_infectious, 1 . infectious, 2. in vector control
          patch_n.set_vector_control_status(1);
          Utils.FRED_VERBOSE(1, "add_infectious_patch finished day %d tract %d . %d\n", day, tract_index_, census_tract_set[tract_index_].ind);
        }
      }
    }

    public bool get_vector_control_status() { return Enable_Vector_Control; }

    private void get_county_ids()
    {
      // get the county ids from external file
      var county_codes_file = string.Empty;
      int num_households = Global.Places.get_number_of_households();
      county_record county_temp = new county_record();
      FredParameters.GetParameter("county_codes_file", ref county_codes_file);
      if (!File.Exists(county_codes_file))
      {
        Utils.fred_abort("Can't open county_codes_file %s\n", county_codes_file);
      }
      county_set.Clear();
      county_temp.habitants.Clear();
      using var fp = new StreamReader(county_codes_file);
      var fileData = Utils.NormalizeWhiteSpace(fp.ReadToEnd());
      fp.Dispose();
      var codes = fileData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      foreach (var code in codes)
      {
        county_set.Add(new county_record { id = Convert.ToInt32(code) });
      }

      for (int i = 0; i < county_set.Count; ++i)
      {
        Utils.FRED_VERBOSE(2, "County record set for county id:  {0}", county_set[i].id);
      }

      for (int i = 0; i < num_households; ++i)
      {
        int household_county = -1;
        var h = Global.Places.get_household_ptr(i);
        int c = h.get_county_index();
        int h_county = Global.Places.get_fips_of_county_with_index(c);
        for (int j = 0; j < county_set.Count; j++)
        {
          if (h_county == county_set[j].id)
          {
            household_county = j;
          }
        }
        //find the county for each household
        if (household_county == -1)
        {
          Utils.fred_abort("No county code found for house in county %d\n", h_county);
        }
        int house_mates = h.get_size();
        // load every person in the house in a  county
        for (int j = 0; j < house_mates; ++j)
        {
          var p = h.get_enrollee(j);
          county_set[household_county].habitants.Add(p);
        }
      }
      Utils.FRED_VERBOSE(0, "get_county_ids finished\n");
    }

    private void get_immunity_from_file()
    {
      // get the prior immune proportion by age  from external file for each county for total immunity
      var prior_immune_file = string.Empty;
      FredParameters.GetParameter("prior_immune_file", ref prior_immune_file);
      if (!File.Exists(prior_immune_file))
      {
        Utils.fred_abort("Can't open prior_immune_file %s\n", prior_immune_file);
      }

      ReadImmunity(prior_immune_file);
    }

    private void ReadImmunity(string prior_immune_file)
    {
      var fp = new StreamReader(prior_immune_file);
      var fileData = Utils.NormalizeWhiteSpace(fp.ReadToEnd());
      fp.Dispose();
      var tokens = fileData.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      for (int a = 0; a < tokens.Length; a++)
      {
        int temp_county = Convert.ToInt32(tokens[a]);
        int index_county = -1;
        double temp_immune;
        for (int i = 0; i < county_set.Count; ++i)
        {
          if (county_set[i].id == temp_county)
          {
            index_county = i;
          }
        }
        if (index_county == -1)
        {
          Utils.fred_abort("No county found %d\n", temp_county);
        }
        Utils.FRED_VERBOSE(2, "County code  %d\n", temp_county);
        for (int i = 0; i < 102; ++a, ++i)
        {
          temp_immune = Convert.ToInt32(tokens[a]); //fscanf(fp, "%lg ", &temp_immune);
          county_set[index_county].immunity_by_age[i] = temp_immune;
          Utils.FRED_VERBOSE(2, "Age: {0} Immunity  {1}", i, temp_immune);
        }
      }
    }

    private void get_immunity_from_file(int d)
    {
      // get the prior immune proportion by age  from external file for each county for total immunity
      var prior_immune_file = string.Empty;
      var immune_param_string = $"prior_immune_file[{d}]";
      FredParameters.GetParameter(immune_param_string, ref prior_immune_file);
      if (!File.Exists(prior_immune_file))
      {
        Utils.fred_abort("Can't open prior_immune_file %s for disease %d \n", prior_immune_file, d);
      }

      ReadImmunity(prior_immune_file);
    }

    private void get_people_size_by_age()
    {
      //calculate number of people by age
      for (int i = 0; i < county_set.Count; ++i)
      {
        if (county_set[i].habitants.Count > 0)
        {
          for (int k = 0; k < 102; ++k)
          {
            county_set[i].people_by_age[k] = 0;
          }
          for (int j = 0; j < county_set[i].habitants.Count; ++j)
          {
            var per = county_set[i].habitants[j];
            int temp_age = per.get_age();
            if (temp_age > 101)
            {
              temp_age = 101;
            }
            county_set[i].people_by_age[temp_age]++;
          }
        }
      }
    }

    private void immunize_total_by_age()
    {
      for (int i = 0; i < county_set.Count; ++i)
      {
        county_set[i].people_immunized = 0;
        if (county_set[i].habitants.Count > 0)
        {
          for (int j = 0; j < county_set[i].habitants.Count; ++j)
          {
            var per = county_set[i].habitants[j];
            double prob_immune_ = FredRandom.NextDouble();
            double prob_immune = prob_immune_ * 100;
            int temp_age = per.get_age();
            if (temp_age > 101)
            {
              temp_age = 101;
            }
            double prob_by_age = county_set[i].immunity_by_age[temp_age];
            if (prob_by_age > prob_immune)
            {
              for (int d = 0; d < Vector_Patch.DISEASE_TYPES; ++d)
              {
                if (per.is_susceptible(d))
                {
                  per.become_unsusceptible(Global.Diseases.get_disease(d));
                }
              }
              county_set[i].people_immunized++;
            }
          }
        }
      }
    }

    private void immunize_by_age(int d)
    {
      for (int i = 0; i < county_set.Count; ++i)
      {
        county_set[i].people_immunized = 0;
        if (county_set[i].habitants.Count > 0)
        {
          for (int j = 0; j < county_set[i].habitants.Count; ++j)
          {
            var per = county_set[i].habitants[j];
            double prob_immune_ = FredRandom.NextDouble();
            double prob_immune = prob_immune_ * 100;
            int temp_age = per.get_age();
            if (temp_age > 101)
            {
              temp_age = 101;
            }
            double prob_by_age = county_set[i].immunity_by_age[temp_age];
            if (prob_by_age > prob_immune)
            {
              if (per.is_susceptible(d))
              {
                per.become_unsusceptible(Global.Diseases.get_disease(d));
                county_set[i].people_immunized++;
              }
            }
          }
        }
      }
    }

    private void seed_patches_by_distance_in_km(FredGeo lat, FredGeo lon, double radius_in_km, int dis, int day_on, int day_off, double seeds_)
    {
      //ASSUMMING WE ARE IN THE TROPIC 1/120 degree ~ 1 km
      //  printf("SEED_PATCHES_BY_DISTANCE entered\n");
      //int kilometers_per_degree = 120;
      int number_of_patches = Convert.ToInt32(Convert.ToInt32(radius_in_km) / this.patch_size);
      //find the patch of the middle point
      var patch = get_patch(lat, lon);
      if (patch != null)
      {
        //printf("SEED_PATCHES_BY_DISTANCE Patch found in the grid lat %lg lon %lg\n",lat,lon);
        int r1 = patch.get_row() - number_of_patches;
        r1 = (r1 >= 0) ? r1 : 0;
        int r2 = patch.get_row() + number_of_patches;
        r2 = (r2 <= this.rows - 1) ? r2 : this.rows - 1;

        int c1 = patch.get_col() - number_of_patches;
        c1 = (c1 >= 0) ? c1 : 0;
        int c2 = patch.get_col() + number_of_patches;
        c2 = (c2 <= this.cols - 1) ? c2 : this.cols - 1;

        //    printf("SEED_PATCHES_BY_DISTANCE number of patches %d r1 %d r2 %d c1 %d c2 %d\n",number_of_patches,r1,r2,c1,c2);

        for (int r = r1; r <= r2; ++r)
        {
          for (int c = c1; c <= c2; ++c)
          {
            var p = get_patch(r, c);
            double hx = (r - patch.get_row()) / this.patch_size;
            double hy = (c - patch.get_col()) / this.patch_size;
            if (Math.Sqrt((hx) * (hx) + (hy) * (hy)) <= radius_in_km)
            {
              p.set_vector_seeds(dis, day_on, day_off, seeds_);
            }
          }
        }
      }
    }

  }
}
