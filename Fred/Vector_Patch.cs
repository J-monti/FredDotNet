using System;

namespace Fred
{
  public class Vector_Patch : Abstract_Patch
  {
    public const int DISEASE_TYPES = 4;

    private double suitability;

    // Vectors per host
    private double temperature;
    private double house_index;
    private double vectors_per_host;
    private double pupae_per_host;
    private double life_span;
    private double sucess_rate;
    private double female_ratio;
    private double development_time;

    // proportion of imported or born infectious
    private double[] seeds = new double[DISEASE_TYPES];

    // day on and day off of seeding mosquitoes in the patch
    private int[] day_start_seed = new int[DISEASE_TYPES];
    private int[] day_end_seed = new int[DISEASE_TYPES];

    private int census_tract_index;
    private bool eligible_for_vector_control;
    private bool registered_for_vector_control;

    public Vector_Patch() { }

    public void setup(int i, int j, double patch_size, double grid_min_x, double grid_min_y)
    {
      row = i;
      col = j;
      min_x = grid_min_x + (col) * patch_size;
      min_y = grid_min_y + (row) * patch_size;
      max_x = grid_min_x + (col + 1) * patch_size;
      max_y = grid_min_y + (row + 1) * patch_size;
      center_y = (min_y + max_y) / 2.0;
      center_x = (min_x + max_x) / 2.0;

      for (int k = 0; k < DISEASE_TYPES; k++)
      {
        seeds[k] = 0.0;
        day_start_seed[k] = 0;
        day_end_seed[k] = 0;
      }
      temperature = 0;
      house_index = 0;
    }

    public double distance_to_patch(Vector_Patch patch2)
    {
      double x1 = center_x;
      double y1 = center_y;
      double x2 = patch2.get_center_x();
      double y2 = patch2.get_center_y();
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public new void print()
    {
      Utils.FRED_VERBOSE(0, "Vector_patch: %d %d\n",
             row, col);
    }

    public void set_temperature(double patch_temperature)
    {
      //temperatures vs development times..FOCKS2000: DENGUE TRANSMISSION THRESHOLDS
      //double[] temps = new []{ 8.49, 3.11, 4.06, 3.3, 2.66, 2.04, 1.46, 0.92 }; //temperatures
      //double[] dev_times = new []{ 15.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0, 32.0 }; //development times
      temperature = patch_temperature;
      if (temperature > 32) temperature = 32;
      Utils.FRED_VERBOSE(1, "SET TEMP: Patch %d %d temp %f\n", row, col, patch_temperature);
    }

    public void set_mosquito_index(double index_)
    {
      if (index_ == 0)
      {
        index_ = 14.18; // average Colombia
      }
      house_index = (double)index_ / 100.00;
    }

    public void set_vector_seeds(int dis, int day_on, int day_off, double seeds_)
    {
      seeds[dis] = seeds_;
      day_start_seed[dis] = day_on;
      day_end_seed[dis] = day_off;
      Utils.FRED_VERBOSE(1, "SET_VECTOR_SEEDS: Patch %d %d proportion of susceptible for disease [%d]: %f. start: %d end: %d\n", row, col, dis, seeds[dis], day_on, day_off);
    }

    public double get_seeds(int dis, int day)
    {
      if ((day < day_start_seed[dis]) || (day > day_end_seed[dis]))
      {
        return 0.0;
      }
      else
      {
        return seeds[dis];
      }
    }

    public void quality_control() { return; }
    public double get_temperature() { return temperature; }
    public double get_mosquito_index() { return house_index; }
    public double get_seeds(int dis) { return seeds[dis]; }
    public int get_day_start_seed(int dis) { return day_start_seed[dis]; }
    public int get_day_end_seed(int dis) { return day_end_seed[dis]; }
    public int select_places_for_vector_control(double coverage_, double efficacy_, int day_on, int day_off) { return 0; }
    public int get_vector_population() { return 0; }
    public int get_school_vector_population() { return 0; }
    public int get_workplace_vector_population() { return 0; }
    public int get_household_vector_population() { return 0; }
    public int get_neighborhood_vector_population() { return 0; }
    public int get_school_infected_vectors() { return 0; }
    public int get_workplace_infected_vectors() { return 0; }
    public int get_household_infected_vectors() { return 0; }
    public int get_neighborhood_infected_vectors() { return 0; }
    public int get_susceptible_vectors() { return 0; }
    public int get_infectious_hosts() { return 0; }
    public int get_infected_vectors() { return 0; }
    public int get_schools_in_vector_control() { return 0; }
    public int get_households_in_vector_control() { return 0; }
    public int get_workplaces_in_vector_control() { return 0; }
    public int get_neighborhoods_in_vector_control() { return 0; }

    /**
     *  Vector control functions
     */
    public void set_census_tract_index(int in_) { census_tract_index = in_; }

    public void make_eligible_for_vector_control() { eligible_for_vector_control = true; }

    public void register_for_vector_control() { registered_for_vector_control = true; }

    public bool is_eligible_for_vector_control() { return this.eligible_for_vector_control; }

    public bool is_registered_for_vector_control() { return this.registered_for_vector_control; }

    public int get_census_tract_index() { return this.census_tract_index; }
  }
}
