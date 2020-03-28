using System;

namespace Fred
{
  public class VectorPatch : AbstractPatch
  {
    public const int DISEASE_TYPES = 4;
    protected double suitability;
    // Vectors per host
    protected double temperature;
    protected double house_index;
    protected double vectors_per_host;
    protected double pupae_per_host;
    protected double life_span;
    protected double sucess_rate;
    protected double female_ratio;
    protected double development_time;
    // proportion of imported or born infectious
    protected double[] seeds = new double[DISEASE_TYPES];
    // day on and day off of seeding mosquitoes in the patch
    protected int[] day_start_seed = new int[DISEASE_TYPES];
    protected int[] day_end_seed = new int[DISEASE_TYPES];
    protected int census_tract_index;
    protected bool eligible_for_vector_control;
    protected bool registered_for_vector_control;

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
    public void set_census_tract_index(int in_) { census_tract_index = in_; }
    public void make_eligible_for_vector_control() { eligible_for_vector_control = true; }
    public void register_for_vector_control() { registered_for_vector_control = true; }
    public bool is_eligible_for_vector_control() { return this.eligible_for_vector_control; }
    public bool is_registered_for_vector_control() { return this.registered_for_vector_control; }
    public int get_census_tract_index() { return this.census_tract_index; }

    public override void setup(int i, int j, double patch_size, double grid_min_x, double grid_min_y)
    {
      base.setup(i, j, patch_size, grid_min_x, grid_min_x);
      for (int a = 0; a < DISEASE_TYPES; a++)
      {
        seeds[a] = 0.0;
        day_start_seed[a] = 0;
        day_end_seed[a] = 0;
      }

      temperature = 0;
      house_index = 0;
    }

    public void set_temperature(double patch_temperature)
    {
      //temperatures vs development times..FOCKS2000: DENGUE TRANSMISSION THRESHOLDS
      //var temps = new double[8] { 8.49, 3.11, 4.06, 3.3, 2.66, 2.04, 1.46, 0.92 }; //temperatures
      //var dev_times = new double[8] { 15.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0, 32.0 }; //development times
      temperature = patch_temperature;
      if (temperature > 32)
      {
        temperature = 32;
      }
      //FRED_VERBOSE(1, "SET TEMP: Patch %d %d temp %f\n", row, col, patch_temperature);
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

    public void set_vector_seeds(int dis, int day_on, int day_off, double seeds_)
    {
      seeds[dis] = seeds_;
      day_start_seed[dis] = day_on;
      day_end_seed[dis] = day_off;
      //FRED_VERBOSE(1, "SET_VECTOR_SEEDS: Patch %d %d proportion of susceptible for disease [%d]: %f. start: %d end: %d\n", row, col, dis, seeds[dis], day_on, day_off);
    }
    public void print()
    {
      //FRED_VERBOSE(0, "Vector_patch: %d %d\n",
      //       row, col);
    }

    public void set_mosquito_index(double index_)
    {
      if (index_ == 0)
      {
        index_ = 14.18; // average Colombia
      }
      house_index = (double)index_ / 100.00;
    }
  }
}
