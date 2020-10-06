using System;
using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public class Epidemic
  {
    public const char SEED_USER = 'U';
    public const char SEED_RANDOM = 'R';
    public const char SEED_EXPOSED = 'E';
    public const char SEED_INFECTIOUS = 'I';

    protected readonly Disease disease;
    protected int id;
    private int N;          // current population size
    private int N_init;     // initial population size

    private bool report_generation_time;
    private bool report_transmission_by_age;

    // event queues
    private readonly Events infectious_start_event_queue;
    private readonly Events infectious_end_event_queue;
    private readonly Events symptoms_start_event_queue;
    private readonly Events symptoms_end_event_queue;
    private readonly Events immunity_start_event_queue;
    private readonly Events immunity_end_event_queue;

    // active sets
    private readonly List<Person> infected_people;
    protected readonly List<Person> potentially_infectious_people;
    private readonly List<Place> active_places;
    private readonly List<Person> actually_infectious_people;
    private readonly List<Place> active_place_vec;

    // seeding imported cases
    private readonly List<Epidemic_TimeStepMap> imported_cases_map;
    private bool import_by_age;
    private double import_age_lower_bound;
    private double import_age_upper_bound;

    // valid seeding types are:
    // "user_specified" => SEED_USER 'U'
    // "random" => SEED_RANDOM 'R'
    // see Epidemic.advance_seed_infection"
    private string seeding_type_name;
    private char seeding_type;
    private double fraction_seeds_infectious;

    private readonly List<Person> daily_infections_list;
    private readonly List<Person> daily_symptomatic_list;

    // population health state counters
    private int susceptible_people;
    protected int exposed_people;
    private int infectious_people;
    private int removed_people;
    private int immune_people;
    //private int vaccinated_people;
    private int people_becoming_infected_today;
    private readonly Disease_Count_Info population_infection_counts = new Disease_Count_Info();

    //Values for household income based stratification
    private Dictionary<int, Disease_Count_Info> household_income_infection_counts_map;
    private Dictionary<long, Disease_Count_Info> census_tract_infection_counts_map;

    //Values for school income based stratification
    private Dictionary<int, Disease_Count_Info> school_income_infection_counts_map;

    protected int people_becoming_symptomatic_today;
    protected int people_with_current_symptoms;

    private int daily_case_fatality_count;
    private int total_case_fatality_count;

    // used for computing reproductive rate:
    private double RR;
    private int[] daily_cohort_size;
    private int[] number_infected_by_cohort;

    // attack rates
    private double attack_rate;
    private double symptomatic_attack_rate;

    // serial interval
    private double total_serial_interval;
    private int total_secondary_cases;

    // used for maintining quantities from previous day;
    private int incidence;
    private int symptomatic_incidence;
    private int prevalence_count;
    private double prevalence;
    private int case_fatality_incidence;

    // used for incidence counts by county
    private int counties;
    private int[] county_incidence;

    // used for incidence counts by census_tracts
    private int census_tracts;
    private int[] census_tract_incidence;
    private int[] census_tract_symp_incidence;

    /**
   * This static factory method is used to get an instance of a specific
   * Epidemic Model.  Depending on the model parameter, it will create a
   * specific Epidemic Model and return a pointer to it.
   *
   * @param a string containing the requested Epidemic model type
   * @return a pointer to a Epidemic model
   */
    public static Epidemic get_epidemic(Disease disease)
    {
      if (disease.get_disease_name() == "drug_use")
      {
        return new Markov_Epidemic(disease);
      }

      if (disease.get_disease_name() == "hiv")
      {
        return new HIV_Epidemic(disease);
      }

      return new Epidemic(disease);
    }

    public Epidemic(Disease disease)
    {
      this.disease = disease;
      this.id = disease.get_id();
      this.daily_cohort_size = new int[Global.Days];
      this.number_infected_by_cohort = new int[Global.Days];
      for (int i = 0; i < Global.Days; ++i)
      {
        this.daily_cohort_size[i] = 0;
        this.number_infected_by_cohort[i] = 0;
      }
      this.susceptible_people = 0;

      this.exposed_people = 0;
      this.people_becoming_infected_today = 0;

      this.infectious_people = 0;

      this.people_becoming_symptomatic_today = 0;
      this.people_with_current_symptoms = 0;
      this.removed_people = 0;

      this.immune_people = 0;
      // this.vaccinated_people = 0;

      this.report_generation_time = false;
      this.report_transmission_by_age = false;

      this.population_infection_counts.tot_ppl_evr_inf = 0;
      this.population_infection_counts.tot_ppl_evr_sympt = 0;

      if (Global.Report_Mean_Household_Stats_Per_Income_Category)
      {
        //Values for household income based stratification
        this.household_income_infection_counts_map = new Dictionary<int, Disease_Count_Info>();
        for (int i = 0; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
        {
          this.household_income_infection_counts_map.Add(i, new Disease_Count_Info());
          this.household_income_infection_counts_map[i].tot_ppl_evr_inf = 0;
          this.household_income_infection_counts_map[i].tot_ppl_evr_sympt = 0;
          this.household_income_infection_counts_map[i].tot_chldrn_evr_inf = 0;
          this.household_income_infection_counts_map[i].tot_chldrn_evr_sympt = 0;
        }
      }

      if (Global.Report_Epidemic_Data_By_Census_Tract)
      {
        this.census_tract_infection_counts_map = new Dictionary<long, Disease_Count_Info>();
        //Values for household census_tract based stratification
        for (int a = 0; a < Household.census_tract_set.Count; a++)
        {
          this.census_tract_infection_counts_map.Add(a, new Disease_Count_Info());
        }
      }

      if (Global.Report_Childhood_Presenteeism)
      {
        //    //Values for household census_tract based stratification
        //    for(std.set<long int>.iterator census_tract_itr = Household.census_tract_set.begin();
        //          census_tract_itr != Household.census_tract_set.end(); ++census_tract_itr) {
        //      this.census_tract_infection_counts_map[*census_tract_itr].tot_ppl_evr_inf = 0;
        //      this.census_tract_infection_counts_map[*census_tract_itr].tot_ppl_evr_sympt = 0;
        //      this.census_tract_infection_counts_map[*census_tract_itr].tot_chldrn_evr_inf = 0;
        //      this.census_tract_infection_counts_map[*census_tract_itr].tot_chldrn_evr_sympt = 0;
        //    }
      }

      this.attack_rate = 0.0;
      this.symptomatic_attack_rate = 0.0;

      this.total_serial_interval = 0.0;
      this.total_secondary_cases = 0;

      this.N_init = 0;
      this.N = 0;
      this.prevalence_count = 0;
      this.incidence = 0;
      this.symptomatic_incidence = 0;
      this.prevalence = 0.0;
      this.RR = 0.0;

      this.daily_case_fatality_count = 0;
      this.total_case_fatality_count = 0;

      this.daily_infections_list = new List<Person>();
      this.daily_symptomatic_list = new List<Person>();

      this.case_fatality_incidence = 0;
      this.counties = 0;
      this.county_incidence = null;
      this.census_tract_incidence = null;
      this.census_tract_symp_incidence = null;
      this.census_tracts = 0;
      this.fraction_seeds_infectious = 0.0;

      this.imported_cases_map = new List<Epidemic_TimeStepMap>();
      this.import_by_age = false;
      this.import_age_lower_bound = 0;
      this.import_age_upper_bound = Demographics.MAX_AGE;
      this.seeding_type = SEED_EXPOSED;

      this.infectious_start_event_queue = new Events();
      this.infectious_end_event_queue = new Events();
      this.symptoms_start_event_queue = new Events();
      this.symptoms_end_event_queue = new Events();
      this.immunity_start_event_queue = new Events();
      this.immunity_end_event_queue = new Events();

      this.infected_people = new List<Person>();
      this.potentially_infectious_people = new List<Person>();
      this.actually_infectious_people = new List<Person>();
    }

    public virtual void setup()
    {
      string paramstr = string.Empty;
      string map_file_name = string.Empty;
      int temp = 0;

      // read time_step_map
      FredParameters.GetParameter("primary_cases_file", ref map_file_name);
      // If this parameter is "none", then there is no map
      if (!map_file_name.StartsWith("none"))
      {
        map_file_name = Utils.get_fred_file_name(map_file_name);
        if (!File.Exists(map_file_name))
        {
          Utils.fred_abort("Help! Can't read {0} Timestep Map\n", map_file_name);
        }
        string line;
        using var ts_input = new StreamReader(map_file_name);
        while (ts_input.Peek() != -1)
        {
          line = ts_input.ReadLine().Trim();
          if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
          { // empty line or comment
            continue;
          }
          var tmap = new Epidemic_TimeStepMap();
          var data = line.Split(' ');
          var n = data.Length;
          if (n < 3)
          {
            Utils.fred_abort("Need to specify at least SimulationDayStart, SimulationDayEnd and NumSeedingAttempts for Time_Step_Map. ");
          }

          tmap.sim_day_start = Convert.ToInt32(data[0]);
          tmap.sim_day_end = Convert.ToInt32(data[1]);
          tmap.num_seeding_attempts = Convert.ToInt32(data[2]);
          tmap.disease_id = (n >= 4) ? Convert.ToInt32(data[3]) : 0;
          tmap.seeding_attempt_prob = (n >= 5) ? Convert.ToDouble(data[4]) : 1;
          tmap.min_num_successful = (n >= 6) ? Convert.ToInt32(data[5]) : 0;
          tmap.lat = (n >= 7) ? Convert.ToDouble(data[6]) : 0.0;
          tmap.lon = (n >= 8) ? Convert.ToDouble(data[7]) : 0.0;
          tmap.radius = (n >= 9) ? Convert.ToDouble(data[8]) : -1;
          this.imported_cases_map.Add(tmap);
        }
        ts_input.Dispose();
      }
      if (Global.Verbose > 1)
      {
        for (int i = 0; i < this.imported_cases_map.Count; ++i)
        {
          Console.WriteLine(this.imported_cases_map[i].ToString());
        }
      }

      FredParameters.GetParameter("seed_by_age", ref temp);
      this.import_by_age = temp != 0;
      FredParameters.GetParameter("seed_age_lower_bound", ref import_age_lower_bound);
      FredParameters.GetParameter("seed_age_upper_bound", ref import_age_upper_bound);
      FredParameters.GetParameter("report_generation_time", ref temp);
      this.report_generation_time = temp > 0;
      FredParameters.GetParameter("report_transmission_by_age", ref temp);
      this.report_transmission_by_age = temp > 0;
      FredParameters.GetParameter("advanced_seeding", ref this.seeding_type_name);
      if (this.seeding_type_name == "random")
      {
        this.seeding_type = SEED_RANDOM;
      }
      else if (this.seeding_type_name == "exposed")
      {
        this.seeding_type = SEED_EXPOSED;
      }
      else if (this.seeding_type_name == "infectious")
      {
        this.fraction_seeds_infectious = 1.0;
        this.seeding_type = SEED_INFECTIOUS;
      }
      else if (this.seeding_type_name.Contains(';'))
      {
        // format is exposed:0.0000; infectious:0.0000
        var tokens = this.seeding_type_name.Split(';');
        Utils.assert(tokens[0].Contains(':'));
        Utils.assert(tokens[1].Contains(':'));
        var t1 = tokens[0].Split(':');
        var t2 = tokens[1].Split(':');
        Utils.assert(t1[0] == "exposed");
        Utils.assert(t2[0] == "infectious");
        double fraction_exposed, fraction_infectious;
        fraction_exposed = Convert.ToDouble(t1[1]);
        fraction_infectious = Convert.ToDouble(t2[1]);
        Utils.assert(fraction_exposed + fraction_infectious <= 1.01);
        Utils.assert(fraction_exposed + fraction_infectious >= 0.99);
        this.fraction_seeds_infectious = fraction_infectious;
      }
      else
      {
        Utils.fred_abort("Invalid advance_seeding parameter: {0}!\n", this.seeding_type_name);
      }
    }

    public virtual void prepare() { }
    public void print_stats(int day)
    {
      Utils.FRED_VERBOSE(1, "epidemic print stats for disease {0} day {1}", id, day);
      // set population size, and remember original pop size
      if (day == 0)
      {
        this.N_init = this.N = Global.Pop.get_pop_size();
      }
      else
      {
        this.N = Global.Pop.get_pop_size();
      }

      // get reproductive rate for the cohort exposed RR_delay days ago
      // unless RR_delay == 0
      this.daily_cohort_size[day] = this.people_becoming_infected_today;
      this.RR = 0.0;         // reproductive rate for a fixed cohort of infectors
      if (0 < Global.RR_delay && Global.RR_delay <= day)
      {
        int cohort_day = day - Global.RR_delay;    // exposure day for cohort
        int cohort_size = this.daily_cohort_size[cohort_day];        // size of cohort
        if (cohort_size > 0)
        {
          // compute reproductive rate for this cohort
          this.RR = this.number_infected_by_cohort[cohort_day] / cohort_size;
        }
      }

      this.population_infection_counts.tot_ppl_evr_inf += this.people_becoming_infected_today;
      this.population_infection_counts.tot_ppl_evr_sympt += this.people_becoming_symptomatic_today;

      this.attack_rate = (100.0 * this.population_infection_counts.tot_ppl_evr_inf) / this.N_init;
      this.symptomatic_attack_rate = (100.0 * this.population_infection_counts.tot_ppl_evr_sympt) / this.N_init;

      // preserve these quantities for use during the next day
      this.incidence = this.people_becoming_infected_today;
      this.symptomatic_incidence = this.people_becoming_symptomatic_today;
      this.prevalence_count = this.exposed_people + this.infectious_people;
      this.prevalence = this.prevalence_count / this.N;
      this.case_fatality_incidence = this.daily_case_fatality_count;
      double case_fatality_rate = 0.0;
      if (this.population_infection_counts.tot_ppl_evr_sympt > 0)
      {
        case_fatality_rate = 100000.0 * this.total_case_fatality_count
          / this.population_infection_counts.tot_ppl_evr_sympt;
      }

      if (this.id == 0)
      {
        Global.Daily_Tracker.set_index_key_pair(day, "Date", Date.get_date_string());
        Global.Daily_Tracker.set_index_key_pair(day, "WkDay", Date.get_day_of_week_string());
        Global.Daily_Tracker.set_index_key_pair(day, "Year", Date.get_epi_year());
        Global.Daily_Tracker.set_index_key_pair(day, "Week", Date.get_epi_week());
        Global.Daily_Tracker.set_index_key_pair(day, "N", this.N);
      }

      this.susceptible_people = this.N - this.exposed_people - this.infectious_people - this.removed_people;
      Utils.track_value(day, "S", this.susceptible_people, this.id);
      Utils.track_value(day, "E", this.exposed_people, this.id);
      Utils.track_value(day, "I", this.infectious_people, this.id);
      Utils.track_value(day, "Is", this.people_with_current_symptoms, this.id);
      Utils.track_value(day, "R", this.removed_people, this.id);
      if (this.disease.get_natural_history().is_case_fatality_enabled())
      {
        Utils.track_value(day, "CF", this.daily_case_fatality_count, this.id);
        Utils.track_value(day, "TCF", this.total_case_fatality_count, this.id);
        Utils.track_value(day, "CFR", case_fatality_rate, this.id);
      }
      Utils.track_value(day, "M", this.immune_people, this.id);
      Utils.track_value(day, "P", this.prevalence_count, this.id);
      Utils.track_value(day, "C", this.incidence, this.id);
      Utils.track_value(day, "Cs", this.symptomatic_incidence, this.id);
      Utils.track_value(day, "AR", this.attack_rate, this.id);
      Utils.track_value(day, "ARs", this.symptomatic_attack_rate, this.id);
      Utils.track_value(day, "RR", this.RR, this.id);

      if (Global.Enable_Vector_Layer && Global.Report_Vector_Population)
      {
        Global.Vectors.report(day, this);
      }

      if (this.report_transmission_by_age)
      {
        report_transmission_by_age_group(day);
      }

      if (Global.Report_Presenteeism)
      {
        report_presenteeism(day);
      }

      if (Global.Report_Childhood_Presenteeism)
      {
        report_school_attack_rates_by_income_level(day);
      }

      if (Global.Report_Place_Of_Infection)
      {
        Utils.FRED_VERBOSE(0, "report place if infection\n");
        report_place_of_infection(day);
      }

      if (Global.Report_Age_Of_Infection != 0)
      {
        report_age_of_infection(day);
      }

      if (Global.Report_Distance_Of_Infection)
      {
        report_distance_of_infection(day);
      }

      if (Global.Report_Incidence_By_County)
      {
        Utils.FRED_VERBOSE(0, "report incidence by county\n");
        report_incidence_by_county(day);
      }

      if (Global.Report_Incidence_By_Census_Tract)
      {
        report_incidence_by_census_tract(day);
      }
      if (Global.Report_Symptomatic_Incidence_By_Census_Tract)
      {
        report_symptomatic_incidence_by_census_tract(day);
      }
      if (this.report_generation_time || Global.Report_Serial_Interval)
      {
        Utils.FRED_VERBOSE(0, "report serial interval\n");
        report_serial_interval(day);
      }

      //Only report AR and ARs on last day
      if (Global.Report_Mean_Household_Stats_Per_Income_Category && day == (Global.Days - 1))
      {
        report_household_income_stratified_results(day);
      }

      if (Global.Enable_Household_Shelter)
      {
        Global.Places.report_shelter_stats(day);
      }

      //Only report AR and ARs on last day
      if (Global.Report_Epidemic_Data_By_Census_Tract && day == (Global.Days - 1))
      {
        report_census_tract_stratified_results(day);
      }

      if (Global.Enable_Group_Quarters)
      {
        report_group_quarters_incidence(day);
      }

      Utils.FRED_VERBOSE(0, "report disease specific stats\n");
      report_disease_specific_stats(day);

      // prepare for next day
      this.people_becoming_infected_today = 0;
      this.people_becoming_symptomatic_today = 0;
      this.daily_case_fatality_count = 0;
      this.daily_infections_list.Clear();
      this.daily_symptomatic_list.Clear();
    }

    public void report_age_of_infection(int day)
    {
      int infants = 0;
      int toddlers = 0;
      int pre_school = 0;
      int elementary = 0;
      int high_school = 0;
      int young_adults = 0;
      int adults = 0;
      int elderly = 0;
      var age_count = new int [Demographics.MAX_AGE + 1];       // age group counts
      double mean_age = 0.0;
      int count_infections = 0;
      for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      {
        age_count[i] = 0;
      }

      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        int age = infectee.get_age();
        mean_age += age;
        count_infections++;
        int age_group = age / 5;
        if (age_group > 20)
        {
          age_group = 20;
        }
        if (Global.Report_Age_Of_Infection > 3)
        {
          age_group = age;
          if (age_group > Demographics.MAX_AGE)
          {
            age_group = Demographics.MAX_AGE;
          }
        }

        age_count[age_group]++;
        double real_age = infectee.get_real_age();
        if (Global.Report_Age_Of_Infection == 1)
        {
          if (real_age < 0.5)
          {
            infants++;
          }
          else if (real_age < 2.0)
          {
            toddlers++;
          }
          else if (real_age < 6.0)
          {
            pre_school++;
          }
          else if (real_age < 12.0)
          {
            elementary++;
          }
          else if (real_age < 18.0)
          {
            high_school++;
          }
          else if (real_age < 21.0)
          {
            young_adults++;
          }
          else if (real_age < 65.0)
          {
            adults++;
          }
          else if (65 <= real_age)
          {
            elderly++;
          }
        }
        else if (Global.Report_Age_Of_Infection == 2)
        {
          if (real_age < 19.0 / 12.0)
          {
            infants++;
          }
          else if (real_age < 3.0)
          {
            toddlers++;
          }
          else if (real_age < 5.0)
          {
            pre_school++;
          }
          else if (real_age < 12.0)
          {
            elementary++;
          }
          else if (real_age < 18.0)
          {
            high_school++;
          }
          else if (real_age < 21.0)
          {
            young_adults++;
          }
          else if (real_age < 65.0)
          {
            adults++;
          }
          else if (65 <= real_age)
          {
            elderly++;
          }
        }
        else if (Global.Report_Age_Of_Infection == 3)
        {
          if (real_age < 5.0)
          {
            pre_school++;
          }
          else if (real_age < 18.0)
          {
            high_school++;
          }
          else if (real_age < 50.0)
          {
            young_adults++;
          }
          else if (real_age < 65.0)
          {
            adults++;
          }
          else if (65 <= real_age)
          {
            elderly++;
          }
        }
      }
      if (count_infections > 0)
      {
        mean_age /= count_infections;
      }
      //Write to log file
      Utils.FRED_STATUS(0, "day {0} INF_AGE: ", day);
      Utils.FRED_STATUS(0, "Age_at_infection {0}", mean_age);

      //Store for daily output file
      Utils.track_value(day, "Age_at_infection", mean_age, this.id);

      switch(Global.Report_Age_Of_Infection) {
        case 1:
          Utils.track_value(day, "Infants", infants, this.id);
          Utils.track_value(day, "Toddlers", toddlers, this.id);
          Utils.track_value(day, "Preschool", pre_school, this.id);
          Utils.track_value(day, "Students", elementary + high_school, this.id);
          Utils.track_value(day, "Elementary", elementary, this.id);
          Utils.track_value(day, "Highschool", high_school, this.id);
          Utils.track_value(day, "Young_adults", young_adults, this.id);
          Utils.track_value(day, "Adults", adults, this.id);
          Utils.track_value(day, "Elderly", elderly, this.id);
          break;
        case 2:
          Utils.track_value(day, "Infants", infants, this.id);
          Utils.track_value(day, "Toddlers", toddlers, this.id);
          Utils.track_value(day, "Pre-k", pre_school, this.id);
          Utils.track_value(day, "Elementary", elementary, this.id);
          Utils.track_value(day, "Highschool", high_school, this.id);
          Utils.track_value(day, "Young_adults", young_adults, this.id);
          Utils.track_value(day, "Adults", adults, this.id);
          Utils.track_value(day, "Elderly", elderly, this.id);
          break;
        case 3:
          Utils.track_value(day, "0_4", pre_school, this.id);
          Utils.track_value(day, "5_17", high_school, this.id);
          Utils.track_value(day, "18_49", young_adults, this.id);
          Utils.track_value(day, "50_64", adults, this.id);
          Utils.track_value(day, "65_up", elderly, this.id);
          break;
        case 4:
          for (int i = 0; i <= Demographics.MAX_AGE; ++i)
          {
            Utils.track_value(day, $"A{i}", age_count[i], this.id);
            Utils.track_value(day, $"Age{i}", Global.Popsize_by_age[i] > 0 ?
              (100000.0 * age_count[i] / Global.Popsize_by_age[i]) : 0.0, this.id);
          }
          break;
        default:
          if (Global.Age_Of_Infection_Log_Level >= Global.LOG_LEVEL_LOW)
          {
            report_transmission_by_age_group_to_file(day);
          }
          if (Global.Age_Of_Infection_Log_Level >= Global.LOG_LEVEL_MED)
          {
            for (int i = 0; i <= 20; ++i)
            {
              //Store for daily output file
              Utils.track_value(day, $"A{i * 5}", age_count[i], this.id);
              Utils.FRED_STATUS(0, " A{0}_{1} {2}", i * 5, age_count[i], this.id);
            }
          }
          break;
      }
    }

    internal void track_value(int day, string key, int value)
    {
      Utils.track_value(day, key, value, this.id);
    }

    public void report_distance_of_infection(int day)
    {
      double tot_dist = 0.0;
      double ave_dist = 0.0;
      int n = 0;
      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        var infector = infectee.get_infector(this.id);
        if (infector == null)
        {
          continue;
        }
        var new_place = infectee.get_health().get_infected_mixing_group(this.id) as Place;
        var old_place = infector.get_health().get_infected_mixing_group(this.id) as Place;
        if (new_place == null || old_place == null)
        {
          //Only Places have lat / lon
          continue;
        }
        else
        {
          var lat1 = new_place.get_latitude();
          var lon1 = new_place.get_longitude();
          var lat2 = old_place.get_latitude();
          var lon2 = old_place.get_longitude();
          double dist = Geo.xy_distance(lat1, lon1, lat2, lon2);
          tot_dist += dist;
          n++;
        }
      }
      if (n > 0)
      {
        ave_dist = tot_dist / n;
      }

      //Write to log file
      Utils.FRED_STATUS(0, "\nDay {0} INF_DIST: ", day);
      Utils.FRED_STATUS(0, " Dist {0} ", 1000 * ave_dist);

      //Store for daily output file
      Utils.track_value(day, "Dist", 1000 * ave_dist, this.id);
    }

    public void report_transmission_by_age_group(int day)
    {
      int groups = 4;
      var age_count = new int[groups, groups];    // age group counts
      for (int i = 0; i < groups; ++i)
      {
        for (int j = 0; j < groups; ++j)
        {
          age_count[i,j] = 0;
        }
      }
      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        var infector = this.daily_infections_list[i].get_infector(id);
        if (infector == null)
        {
          continue;
        }
        int g1 = get_age_group(infector.get_age());
        int g2 = get_age_group(infectee.get_age());
        age_count[g1, g2]++;
      }

      //Store for daily output file
      Utils.track_value(day, "T_4_to_4",   age_count[0, 0], this.id);
      Utils.track_value(day, "T_4_to_18",  age_count[0, 1], this.id);
      Utils.track_value(day, "T_4_to_64",  age_count[0, 2], this.id);
      Utils.track_value(day, "T_4_to_99",  age_count[0, 3], this.id);
      Utils.track_value(day, "T_18_to_4",  age_count[1, 0], this.id);
      Utils.track_value(day, "T_18_to_18", age_count[1, 1], this.id);
      Utils.track_value(day, "T_18_to_64", age_count[1, 2], this.id);
      Utils.track_value(day, "T_18_to_99", age_count[1, 3], this.id);
      Utils.track_value(day, "T_64_to_4",  age_count[2, 0], this.id);
      Utils.track_value(day, "T_64_to_18", age_count[2, 1], this.id);
      Utils.track_value(day, "T_64_to_64", age_count[2, 2], this.id);
      Utils.track_value(day, "T_64_to_99", age_count[2, 3], this.id);
      Utils.track_value(day, "T_99_to_4",  age_count[3, 0], this.id);
      Utils.track_value(day, "T_99_to_18", age_count[3, 1], this.id);
      Utils.track_value(day, "T_99_to_64", age_count[3, 2], this.id);
      Utils.track_value(day, "T_99_to_99", age_count[3, 3], this.id);
    }

    public void report_transmission_by_age_group_to_file(int day)
    {
      var file = $"{Global.Output_directory}/AGE.{day}";
      var age_count = new int[100, 100];        // age group counts
      for (int i = 0; i < 100; ++i)
      {
        for (int j = 0; j < 100; ++j)
        {
          age_count[i,j] = 0;
        }
      }
      int group = 1;
      int groups = 100 / group;
      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        var infector = this.daily_infections_list[i].get_infector(id);
        if (infector == null)
        {
          continue;
        }
        int a1 = infector.get_age();
        int a2 = infectee.get_age();
        if (a1 > 99)
        {
          a1 = 99;
        }
        if (a2 > 99)
        {
          a2 = 99;
        }
        a1 = a1 / group;
        a2 = a2 / group;
        age_count[a1, a2]++;
      }
      using var fp = new StreamWriter(file);
      for (int i = 0; i < groups; ++i)
      {
        for (int j = 0; j < groups; ++j)
        {
          fp.Write(" {0}", age_count[j, i]);
        }
        fp.WriteLine();
      }
      fp.Flush();
      fp.Close();
    }

    public void report_incidence_by_county(int day)
    {
      if (day == 0)
      {
        // set up county counts
        this.counties = Global.Places.get_number_of_counties();
        this.county_incidence = new int[this.counties];
        for (int i = 0; i < this.counties; ++i)
        {
          this.county_incidence[i] = 0;
        }
      }
      Utils.FRED_VERBOSE(0, "county incidence day {0}\n", day);
      int infected = this.people_becoming_infected_today;
      for (int i = 0; i < infected; ++i)
      {
        var infectee = this.daily_infections_list[i];
        Utils.FRED_VERBOSE(0, "person {0} is {1} out of {2}", infectee.get_id(), i, infected);
        var hh = (Household)infectee.get_household();
        if (hh == null)
        {
          if (Global.Enable_Hospitals && infectee.is_hospitalized() && infectee.get_permanent_household() != null)
          {
            hh = (Household)infectee.get_permanent_household();
          }
        }

        int c = hh.get_county_index();
        Utils.assert(0 <= c && c < this.counties);
        this.county_incidence[c]++;
        Utils.FRED_VERBOSE(0, "county {0} incidence {1} {2} out of {3} person {4}", c, this.county_incidence[c], i, infected, infectee.get_id());
      }
      Utils.FRED_VERBOSE(1, "county incidence day {0}", day);
      for (int c = 0; c < this.counties; ++c)
      {
        Utils.track_value(day, $"County_{Global.Places.get_fips_of_county_with_index(c)}", this.county_incidence[c], this.id);
        Utils.track_value(day, $"N_{Global.Places.get_fips_of_county_with_index(c)}", Global.Places.get_population_of_county_with_index(c), this.id);
        // prepare for next day
        this.county_incidence[c] = 0;
      }
      Utils.FRED_VERBOSE(1, "county incidence day {0} done\n", day);
    }

    public void report_incidence_by_census_tract(int day)
    {
      if (day == 0)
      {
        // set up census_tract counts
        this.census_tracts = Global.Places.get_number_of_census_tracts();
        this.census_tract_incidence = new int[this.census_tracts];
        for (int i = 0; i < this.census_tracts; ++i)
        {
          this.census_tract_incidence[i] = 0;
        }
      }
      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        var hh = (Household)(infectee.get_household());
        if (hh == null)
        {
          if (Global.Enable_Hospitals && infectee.is_hospitalized() && infectee.get_permanent_household() != null)
          {
            hh = (Household)(infectee.get_permanent_household()); ;
          }
        }
        int t = hh.get_census_tract_index();
        Utils.assert(0 <= t && t < this.census_tracts);
        this.census_tract_incidence[t]++;
      }
      for (int t = 0; t < this.census_tracts; ++t)
      {
        var name = $"Tract_{Global.Places.get_census_tract_with_index(t)}";
        Utils.track_value(day, name, this.census_tract_incidence[t], this.id);
        // prepare for next day
        this.census_tract_incidence[t] = 0;
      }
    }

    public void report_symptomatic_incidence_by_census_tract(int day)
    {
      if (day == 0)
      {
        // set up census_tract counts
        this.census_tracts = Global.Places.get_number_of_census_tracts();
        this.census_tract_symp_incidence = new int[this.census_tracts];
      }
      for (int t = 0; t < this.census_tracts; t++)
      {
        this.census_tract_symp_incidence[t] = 0;
      }
      for (int i = 0; i < this.people_becoming_symptomatic_today; i++)
      {
        var infectee = this.daily_symptomatic_list[i];
        var h = (Household)infectee.get_household();
        int t = h.get_census_tract_index();
        Utils.assert(0 <= t && t < this.census_tracts);
        this.census_tract_symp_incidence[t]++;
      }
      for (int t = 0; t < this.census_tracts; t++)
      {
        var name = $"Tract_Cs_{Global.Places.get_census_tract_with_index(t)}";
        Utils.track_value(day, name, this.census_tract_symp_incidence[t], this.id);
      }
    }

    public void report_place_of_infection(int day)
    {
      // type of place of infection
      int X = 0;
      int H = 0;
      int N = 0;
      int S = 0;
      int C = 0;
      int W = 0;
      int O = 0;
      int M = 0;

      for (int i = 0; i < this.people_becoming_infected_today; i++)
      {
        var infectee = this.daily_infections_list[i];
        char c = infectee.get_infected_mixing_group_type(this.id);
        switch (c)
        {
          case 'X':
            X++;
            break;
          case 'H':
            H++;
            break;
          case 'N':
            N++;
            break;
          case 'S':
            S++;
            break;
          case 'C':
            C++;
            break;
          case 'W':
            W++;
            break;
          case 'O':
            O++;
            break;
          case 'M':
            M++;
            break;
        }
      }

      //Write to log file
      Utils.FRED_STATUS(0, "Day {0} INF_PLACE: ", day);
      Utils.FRED_STATUS(0, " X {0} H {1} Nbr {2} Sch {3}", X, H, N, S);
      Utils.FRED_STATUS(0, " Cls {0} Wrk {1} Off {2} Hosp {3}", C, W, O, M);

      //Store for daily output file
      Utils.track_value(day, "X", X, this.id);
      Utils.track_value(day, "H", H, this.id);
      Utils.track_value(day, "Nbr", N, this.id);
      Utils.track_value(day, "Sch", S, this.id);
      Utils.track_value(day, "Cls", C, this.id);
      Utils.track_value(day, "Wrk", W, this.id);
      Utils.track_value(day, "Off", O, this.id);
      Utils.track_value(day, "Hosp", M, this.id);
    }

    public void report_presenteeism(int day)
    {
      // daily totals
      int infections_in_pop = 0;
      List<int> presenteeism = new List<int>();
      List<int> presenteeism_with_sl = new List<int>();
      int infections_at_work = 0;

      for (int i = 0; i <= Workplace.get_workplace_size_group_count(); ++i)
      {
        presenteeism.Add(0);
        presenteeism_with_sl.Add(0);
      }

      int presenteeism_tot = 0;
      int presenteeism_with_sl_tot = 0;
      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        char c = infectee.get_infected_mixing_group_type(this.id);
        infections_in_pop++;

        // presenteeism requires that place of infection is work or office
        if (c != Place.TYPE_WORKPLACE && c != Place.TYPE_OFFICE)
        {
          continue;
        }
        infections_at_work++;

        // get the work place size (note we don't care about the office size)
        var work = infectee.get_workplace();
        Utils.assert(work != null);
        int size = work.get_size();

        // presenteeism requires that the infector have symptoms
        var infector = infectee.get_infector(this.id);
        Utils.assert(infector != null);
        if (infector.is_symptomatic() > 0)
        {

          // determine whether sick leave was available to infector
          bool infector_has_sick_leave = infector.is_sick_leave_available();

          for (int j = 0; j <= Workplace.get_workplace_size_group_count(); ++j)
          {
            if (size < Workplace.get_workplace_size_max_by_group_id(j))
            {
              presenteeism[j]++;
              presenteeism_tot++;
              if (infector_has_sick_leave)
              {
                presenteeism_with_sl[j]++;
                presenteeism_with_sl_tot++;
              }
            }
          }
        }
      } // end loop over infectees

      //Write to log file
      Utils.FRED_STATUS(0, "\nDay %d PRESENTEEISM: ", day);
      for (int i = 0; i < Workplace.get_workplace_size_group_count(); ++i)
      {
        if (i == 0)
        {
          Utils.FRED_STATUS(0, "wp_0_{0}_pres {1} ", Workplace.get_workplace_size_max_by_group_id(i), presenteeism[i]);
          Utils.FRED_STATUS(0, "wp_0_{0}_pres_sl {1} ", Workplace.get_workplace_size_max_by_group_id(i), presenteeism_with_sl[i]);
          Utils.FRED_STATUS(0, "wp_0_{0}_n {1} ", Workplace.get_workplace_size_max_by_group_id(i), Workplace.get_count_workers_by_workplace_size(i));
        }
        else if (i + 1 < Workplace.get_workplace_size_group_count())
        {
          Utils.FRED_STATUS(0, "wp_{0}_{1}_pres {2} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), Workplace.get_workplace_size_max_by_group_id(i), presenteeism[i]);
          Utils.FRED_STATUS(0, "wp_{0}_{1}_pres_sl {2} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), Workplace.get_workplace_size_max_by_group_id(i), presenteeism_with_sl[i]);
          Utils.FRED_STATUS(0, "wp_{0}_{1}_n {2} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), Workplace.get_workplace_size_max_by_group_id(i), Workplace.get_count_workers_by_workplace_size(i));
        }
        else
        {
          Utils.FRED_STATUS(0, "wp_{0}_up_pres {1} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), presenteeism[i]);
          Utils.FRED_STATUS(0, "wp_{0}_up_pres_sl {1} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), presenteeism_with_sl[i]);
          Utils.FRED_STATUS(0, "wp_{0}_up_n {1} ", (Workplace.get_workplace_size_max_by_group_id(i - 1) + 1), Workplace.get_count_workers_by_workplace_size(i));
        }
      }
      Utils.FRED_STATUS(0, "presenteeism_tot {0} ", presenteeism_tot);
      Utils.FRED_STATUS(0, "presenteeism_with_sl_tot {0} ", presenteeism_with_sl_tot);
      Utils.FRED_STATUS(0, "inf_at_work {0} ", infections_at_work);
      Utils.FRED_STATUS(0, "tot_emp {0} ", Workplace.get_total_workers());
      Utils.FRED_STATUS(0, "N {0}\n", this.N);

      //Store for daily output file
      for (int i = 0; i < Workplace.get_workplace_size_group_count(); ++i)
      {
        string wp_pres = string.Empty;
        string wp_pres_sl = string.Empty;
        string wp_n = string.Empty;
        if (i == 0)
        {
          wp_pres = $"wp_0_{Workplace.get_workplace_size_max_by_group_id(i)}_pres";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres, presenteeism[i]);
          wp_pres_sl = $"wp_0_{Workplace.get_workplace_size_max_by_group_id(i)}_pres_sl";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres_sl, presenteeism_with_sl[i]);
          wp_n = $"wp_0_{Workplace.get_workplace_size_max_by_group_id(i)}_n";
          Global.Daily_Tracker.set_index_key_pair(day, wp_n, Workplace.get_count_workers_by_workplace_size(i));
        }
        else if (i + 1 < Workplace.get_workplace_size_group_count())
        {
          wp_pres = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_{Workplace.get_workplace_size_max_by_group_id(i)}_pres";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres, presenteeism[i]);
          wp_pres_sl = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_{Workplace.get_workplace_size_max_by_group_id(i)}_pres_sl";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres_sl, presenteeism_with_sl[i]);
          wp_n = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_{Workplace.get_workplace_size_max_by_group_id(i)}_n";
          Global.Daily_Tracker.set_index_key_pair(day, wp_n, Workplace.get_count_workers_by_workplace_size(i));
        }
        else
        {
          wp_pres = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_up_pres";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres, presenteeism[i]);
          wp_pres_sl = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_up_pres_sl";
          Global.Daily_Tracker.set_index_key_pair(day, wp_pres_sl, presenteeism_with_sl[i]);
          wp_n = $"wp_{Workplace.get_workplace_size_max_by_group_id(i - 1) + 1}_up_n";
          Global.Daily_Tracker.set_index_key_pair(day, wp_n, Workplace.get_count_workers_by_workplace_size(i));
        }
      }
      Global.Daily_Tracker.set_index_key_pair(day, "presenteeism_tot", presenteeism_tot);
      Global.Daily_Tracker.set_index_key_pair(day, "presenteeism_with_sl_tot", presenteeism_with_sl_tot);
      Global.Daily_Tracker.set_index_key_pair(day, "inf_at_work", infections_at_work);
      Global.Daily_Tracker.set_index_key_pair(day, "tot_emp", Workplace.get_total_workers());
      Global.Daily_Tracker.set_index_key_pair(day, "N", this.N);
    }

    public void report_school_attack_rates_by_income_level(int day)
    {
      // daily totals
      int presenteeism_Q1 = 0;
      int presenteeism_Q2 = 0;
      int presenteeism_Q3 = 0;
      int presenteeism_Q4 = 0;
      int presenteeism_Q1_with_sl = 0;
      int presenteeism_Q2_with_sl = 0;
      int presenteeism_Q3_with_sl = 0;
      int presenteeism_Q4_with_sl = 0;
      int infections_at_school = 0;

      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {

        var infectee = this.daily_infections_list[i];
        Utils.assert(infectee != null);
        char c = infectee.get_infected_mixing_group_type(this.id);

        // school presenteeism requires that place of infection is school or classroom
        if (c != Place.TYPE_SCHOOL && c != Place.TYPE_CLASSROOM)
        {
          continue;
        }
        infections_at_school++;

        // get the school income quartile
        var school = (School)(infectee.get_school());
        Utils.assert(school != null);
        int income_quartile = school.get_income_quartile();

        // presenteeism requires that the infector have symptoms
        var infector = infectee.get_infector(this.id);
        Utils.assert(infector != null);
        if (infector.is_symptomatic() != 0)
        {

          // determine whether anyone was at home to watch child
          var hh = (Household)(infector.get_household());
          Utils.assert(hh != null);
          bool infector_could_stay_home = hh.has_school_aged_child_and_unemployed_adult();

          if (income_quartile == Global.Q1)
          {  // Quartile 1
            presenteeism_Q1++;
            if (infector_could_stay_home)
            {
              presenteeism_Q1_with_sl++;
            }
          }
          else if (income_quartile == Global.Q2)
          {  // Quartile 2
            presenteeism_Q2++;
            if (infector_could_stay_home)
            {
              presenteeism_Q2_with_sl++;
            }
          }
          else if (income_quartile == Global.Q3)
          {  // Quartile 3
            presenteeism_Q3++;
            if (infector_could_stay_home)
            {
              presenteeism_Q3_with_sl++;
            }
          }
          else if (income_quartile == Global.Q4)
          {  // Quartile 4
            presenteeism_Q4++;
            if (infector_could_stay_home)
            {
              presenteeism_Q4_with_sl++;
            }
          }
        }
      } // end loop over infectees

      // raw counts
      int presenteeism = presenteeism_Q1 + presenteeism_Q2 + presenteeism_Q3
        + presenteeism_Q4;
      int presenteeism_with_sl = presenteeism_Q1_with_sl + presenteeism_Q2_with_sl
        + presenteeism_Q3_with_sl + presenteeism_Q4_with_sl;

      //Store for daily output file
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres_Q1", presenteeism_Q1);
      Global.Daily_Tracker.set_index_key_pair(day, "school_pop_Q1", School.get_school_pop_income_quartile_1());
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres_Q2", presenteeism_Q2);
      Global.Daily_Tracker.set_index_key_pair(day, "school_pop_Q2", School.get_school_pop_income_quartile_2());
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres_Q3", presenteeism_Q3);
      Global.Daily_Tracker.set_index_key_pair(day, "school_pop_Q3", School.get_school_pop_income_quartile_3());
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres_Q4", presenteeism_Q4);
      Global.Daily_Tracker.set_index_key_pair(day, "school_pop_Q4", School.get_school_pop_income_quartile_4());
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres", presenteeism);
      Global.Daily_Tracker.set_index_key_pair(day, "school_pres_sl", presenteeism_with_sl);
      Global.Daily_Tracker.set_index_key_pair(day, "inf_at_school", infections_at_school);
      Global.Daily_Tracker.set_index_key_pair(day, "tot_school_pop", School.get_total_school_pop());
      Global.Daily_Tracker.set_index_key_pair(day, "N", this.N);
    }

    public void report_infections_by_workplace_size(int day)
    {
      //  //Workplace Size Delimiters (1-4, 5-9, 10-19, 20-49, 50-99, 100-499, >=500 workers).
      //
      //  // daily totals
      // STUB for now
    }
    public void report_serial_interval(int day)
    {

      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        var infector = infectee.get_infector(id);
        if (infector != null)
        {
          int serial_interval = infectee.get_exposure_date(this.id)
      - infector.get_exposure_date(this.id);
          this.total_serial_interval += serial_interval;
          this.total_secondary_cases++;
        }
      }

      double mean_serial_interval = 0.0;
      if (this.total_secondary_cases > 0)
      {
        mean_serial_interval = this.total_serial_interval / this.total_secondary_cases;
      }

      if (Global.Report_Serial_Interval)
      {
        //Write to log file
        Utils.FRED_STATUS(0, "day {0} SERIAL_INTERVAL:", day);
        Utils.FRED_STATUS(0, " ser_int {0.##}", mean_serial_interval);

        //Store for daily output file
        Global.Daily_Tracker.set_index_key_pair(day, "ser_int", mean_serial_interval);
      }

      Utils.track_value(day, "Tg", mean_serial_interval, this.id);
    }

    public void report_household_income_stratified_results(int day)
    {

      for (int i = 0; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
      {
        int temp_adult_count = 0;
        int temp_adult_inf_count = 0;
        int temp_adult_symp_count = 0;

        //AR
        if (Household.count_inhabitants_by_household_income_level_map[i] > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR",
                    (100.0 * this.household_income_infection_counts_map[i].tot_ppl_evr_inf
                     / Household.count_inhabitants_by_household_income_level_map[i]));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR", 0.0);
        }

        //AR_under_18
        if (Household.count_children_by_household_income_level_map[i] > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR_under_18",
                    (100.0 * this.household_income_infection_counts_map[i].tot_chldrn_evr_inf
                     / Household.count_children_by_household_income_level_map[i]));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR_under_18", 0.0);
        }

        //AR_adult
        temp_adult_count = Household.count_inhabitants_by_household_income_level_map[i]
          - Household.count_children_by_household_income_level_map[i];
        temp_adult_inf_count = this.household_income_infection_counts_map[i].tot_ppl_evr_inf
          - this.household_income_infection_counts_map[i].tot_chldrn_evr_inf;
        if (temp_adult_count > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR_adult",
                    (100.0 * temp_adult_inf_count / temp_adult_count));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "AR_adult", 0.0);
        }

        //ARs
        if (Household.count_inhabitants_by_household_income_level_map[i] > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs",
                    (100.0 * this.household_income_infection_counts_map[i].tot_ppl_evr_sympt
                     / Household.count_inhabitants_by_household_income_level_map[i]));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs", 0.0);
        }

        //ARs_under_18
        if (Household.count_children_by_household_income_level_map[i] > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs_under_18",
                    (100.0 * this.household_income_infection_counts_map[i].tot_chldrn_evr_sympt
                     / Household.count_children_by_household_income_level_map[i]));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs_under_18", 0.0);
        }

        //ARs_adult
        temp_adult_symp_count = this.household_income_infection_counts_map[i].tot_ppl_evr_sympt
          - this.household_income_infection_counts_map[i].tot_chldrn_evr_sympt;
        if (temp_adult_count > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs_adult",
                    (100.0 * temp_adult_symp_count / temp_adult_count));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "ARs_adult", 0.0);
        }
      }

    }

    public void report_census_tract_stratified_results(int day)
    {
      for (int census_tract_itr = 0; census_tract_itr < Household.census_tract_set.Count; census_tract_itr++)
      {
        int temp_adult_count = 0;
        int temp_adult_inf_count = 0;
        int temp_adult_symp_count = 0;

        //AR
        if (Household.count_inhabitants_by_census_tract_map[census_tract_itr] > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR",
                (100.0 * this.census_tract_infection_counts_map[census_tract_itr].tot_ppl_evr_inf
                 / Household.count_inhabitants_by_census_tract_map[census_tract_itr]));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR", 0.0);
        }

        //AR_under_18
        if (Household.count_children_by_census_tract_map[census_tract_itr] > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR_under_18",
                (100.0 * this.census_tract_infection_counts_map[census_tract_itr].tot_chldrn_evr_inf
                 / Household.count_children_by_census_tract_map[census_tract_itr]));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR_under_18", 0.0);
        }

        //AR_adult
        temp_adult_count = Household.count_inhabitants_by_census_tract_map[census_tract_itr]
          - Household.count_children_by_census_tract_map[census_tract_itr];
        temp_adult_inf_count = this.census_tract_infection_counts_map[census_tract_itr].tot_ppl_evr_inf
          - this.census_tract_infection_counts_map[census_tract_itr].tot_chldrn_evr_inf;
        if (temp_adult_count > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR_adult",
                (100.0 * temp_adult_inf_count / temp_adult_count));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "AR_adult", 0.0);
        }

        //Symptomatic AR
        if (Household.count_inhabitants_by_census_tract_map[census_tract_itr] > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs",
                (100.0 * this.census_tract_infection_counts_map[census_tract_itr].tot_ppl_evr_sympt
                 / Household.count_inhabitants_by_census_tract_map[census_tract_itr]));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs", 0.0);
        }


        //ARs_under_18
        if (Household.count_children_by_census_tract_map[census_tract_itr] > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs_under_18",
                (100.0 * this.census_tract_infection_counts_map[census_tract_itr].tot_chldrn_evr_sympt
                 / Household.count_children_by_census_tract_map[census_tract_itr]));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs_under_18", 0.0);
        }


        //ARs_adult
        temp_adult_symp_count = this.census_tract_infection_counts_map[census_tract_itr].tot_ppl_evr_sympt
          - this.census_tract_infection_counts_map[census_tract_itr].tot_chldrn_evr_sympt;
        if (temp_adult_count > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs_adult",
                (100.0 * temp_adult_symp_count / temp_adult_count));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "ARs_adult", 0.0);
        }
      }
    }

    public void report_group_quarters_incidence(int day)
    {
      // group quarters incidence counts
      int G = 0;
      int D = 0;
      int J = 0;
      int L = 0;
      int B = 0;

      for (int i = 0; i < this.people_becoming_infected_today; ++i)
      {
        var infectee = this.daily_infections_list[i];
        // record infections occurring in group quarters
        var place = infectee.get_infected_mixing_group(this.id) as Place;
        if (place == null)
        {
          //Only Places can be group quarters
          continue;
        }
        else
        {
          if (place.is_group_quarters())
          {
            G++;
            if (place.is_college())
            {
              D++;
            }
            if (place.is_prison())
            {
              J++;
            }
            if (place.is_nursing_home())
            {
              L++;
            }
            if (place.is_military_base())
            {
              B++;
            }
          }
        }
      }

      //Write to log file
      Utils.FRED_STATUS(0, "Day {0} GQ_INF: ", day);
      Utils.FRED_STATUS(0, " GQ {0} College {1} Prison {2} Nursing_Home {3} Military {4}", G, D, J, L, B);

      //Store for daily output file
      Utils.track_value(day, "GQ", G, this.id);
      Utils.track_value(day, "College", D, this.id);
      Utils.track_value(day, "Prison", J, this.id);
      Utils.track_value(day, "Nursing_Home", L, this.id);
      Utils.track_value(day, "Military", B, this.id);
    }

    public virtual void report_disease_specific_stats(int day) { }
    
    public void read_time_step_map() { }

    public virtual void get_imported_infections(int day)
    {
      this.N = Global.Pop.get_pop_size();

      for (int i = 0; i < this.imported_cases_map.Count; ++i)
      {
        var tmap = this.imported_cases_map[i];
        if (tmap.sim_day_start <= day && day <= tmap.sim_day_end)
        {
          Utils.FRED_VERBOSE(0, "IMPORT MST:\n"); // tmap.print();
          int imported_cases_requested = tmap.num_seeding_attempts;
          int imported_cases = 0;
          var lat = tmap.lat;
          var lon = tmap.lon;
          double radius = tmap.radius;
          // list of susceptible people that qualify by distance and age
          List<Person> people = new List<Person>();
          if (this.import_by_age)
          {
            Utils.FRED_VERBOSE(0, "IMPORT import by age {0.##} {0.##}", this.import_age_lower_bound, this.import_age_upper_bound);
          }

          int searches_within_given_location = 1;
          while (searches_within_given_location <= 10)
          {
            Utils.FRED_VERBOSE(0, "IMPORT search number {0} ", searches_within_given_location);
            // find households that qualify by distance
            int hsize = Global.Places.get_number_of_households();
            // printf("IMPORT: houses  %d\n", hsize); fflush(stdout);
            for (int j = 0; j < hsize; ++j)
            {
              var house = Global.Places.get_household_ptr(j);
              double dist = 0.0;
              if (radius > 0)
              {
                dist = Geo.xy_distance(lat, lon, house.get_latitude(), house.get_longitude());
                if (radius < dist)
                {
                  continue;
                }
              }
              // this household qualifies by distance.
              // find all susceptible housemates who qualify by age.
              int size = house.get_size();
              // printf("IMPORT: house %s size %d\n", house.get_label(), size); fflush(stdout);
              for (int k = 0; k < size; ++k)
              {
                var person = house.get_enrollee(k);
                if (person.get_health().is_susceptible(this.id))
                {
                  double age = person.get_real_age();
                  if (this.import_age_lower_bound <= age && age <= this.import_age_upper_bound)
                  {
                    people.Add(person);
                  }
                }
              }
            }

            int imported_cases_remaining = imported_cases_requested - imported_cases;
            Utils.FRED_VERBOSE(0, "IMPORT: seeking %d candidates, found %d\n", imported_cases_remaining, (int)people.Count);

            if (imported_cases_remaining <= people.Count)
            {
              // we have at least the minimum number of candidates.
              for (int n = 0; n < imported_cases_remaining; ++n)
              {
                Utils.FRED_VERBOSE(0, "IMPORT candidate %d people.size %d\n", n, people.Count);

                // pick a candidate without replacement
                int pos = FredRandom.Next(0, people.Count - 1);
                var infectee = people[pos];
                people[pos] = people[people.Count - 1];
                people.PopBack();

                // infect the candidate
                Utils.FRED_VERBOSE(0, "infecting candidate %d id %d\n", n, infectee.get_id());
                infectee.become_exposed(this.id, null, null, day);
                Utils.FRED_VERBOSE(0, "exposed candidate %d id %d\n", n, infectee.get_id());
                if (this.seeding_type != SEED_EXPOSED)
                {
                  advance_seed_infection(infectee);
                }
                become_exposed(infectee, day);
                imported_cases++;
              }
              Utils.FRED_VERBOSE(0, "IMPORT SUCCESS: %d imported cases\n", imported_cases);
              return; // success!
            }
            else
            {
              // infect all the candidates
              for (int n = 0; n < people.Count; ++n)
              {
                var infectee = people[n];
                infectee.become_exposed(this.id, null, null, day);
                if (this.seeding_type != SEED_EXPOSED)
                {
                  advance_seed_infection(infectee);
                }
                become_exposed(infectee, day);
                imported_cases++;
              }
            }

            if (radius > 0)
            {
              // expand the distance and try again
              radius = 2 * radius;
              Utils.FRED_VERBOSE(0, "IMPORT: increasing radius to %f\n", radius);
              searches_within_given_location++;
            }
            else
            {
              // return with a warning
              Utils.FRED_VERBOSE(0, "IMPORT FAILURE: only %d imported cases out of %d\n", imported_cases, imported_cases_requested);
              return;
            }
          } //End while(searches_within_given_location <= 10)
            // after 10 tries, return with a warning
          Utils.FRED_VERBOSE(0, "IMPORT FAILURE: only %d imported cases out of %d\n", imported_cases, imported_cases_requested);
          return;
        }
      }
    }

    public void become_exposed(Person person, int day)
    {
      this.infected_people.Insert(0, person);
      // update next event list
      int infectious_start_date = -1;
      if (this.disease.get_transmissibility() > 0.0)
      {
        infectious_start_date = person.get_infectious_start_date(this.id);
        if (0 <= infectious_start_date && infectious_start_date <= day)
        {
          Utils.FRED_VERBOSE(0, "TIME WARP day {0} inf {1}", day, infectious_start_date);
          infectious_start_date = day + 1;
        }
        this.infectious_start_event_queue.add_event(infectious_start_date, person);
      }
      else
      {
        // This disease is not transmissible, therefore, no one ever becomes
        // infectious.  Consequently, spread_infection is never called. So
        // no transmission model is even generated (see Disease.setup()).

        // This is how FRED supports non-communicable disease. Just use the parameter:
        // <disease_name>_transmissibility = 0
        //
      }

      int symptoms_start_date = person.get_symptoms_start_date(this.id);
      if (0 <= symptoms_start_date && symptoms_start_date <= day)
      {
        Utils.FRED_VERBOSE(0, "TIME WARP day {0} symp {1}", day, symptoms_start_date);
        symptoms_start_date = day + 1;
      }
      this.symptoms_start_event_queue.add_event(symptoms_start_date, person);

      // update epidemic counters
      this.exposed_people++;
      this.people_becoming_infected_today++;

      if (Global.Report_Mean_Household_Stats_Per_Income_Category)
      {
        if (person.get_household() != null)
        {
          var hh = (Household)person.get_household();
          int income_level = hh.get_household_income_code();
          if (income_level >= (int)Household_income_level_code.CAT_I &&
             income_level < (int)Household_income_level_code.UNCLASSIFIED)
          {
            this.household_income_infection_counts_map[income_level].tot_ppl_evr_inf++;
          }
        }
      }

      if (Global.Report_Epidemic_Data_By_Census_Tract)
      {
        if (person.get_household() != null)
        {
          var hh = (Household)person.get_household();
          long census_tract = Global.Places.get_census_tract_with_index(hh.get_census_tract_index());
          if (Household.census_tract_set.IndexOf(census_tract) != -1)
          {
            this.census_tract_infection_counts_map[census_tract].tot_ppl_evr_inf++;
            if (person.is_child())
            {
              this.census_tract_infection_counts_map[census_tract].tot_chldrn_evr_inf++;
            }
          }
        }
      }

      if (Global.Report_Childhood_Presenteeism)
      {
        if (person.is_student() &&
           person.get_school() != null &&
           person.get_household() != null)
        {
          var schl = (School)person.get_school();
          var hh = (Household)person.get_household();
          int income_quartile = schl.get_income_quartile();

          if (person.is_child())
          { //Already know person is student
            this.school_income_infection_counts_map[income_quartile].tot_chldrn_evr_inf++;
            this.school_income_infection_counts_map[income_quartile].tot_sch_age_chldrn_evr_inf++;
          }

          if (hh.has_school_aged_child_and_unemployed_adult())
          {
            this.school_income_infection_counts_map[income_quartile].tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf++;
          }
        }
      }

      this.daily_infections_list.Add(person);
    }

    public virtual void update(int day)
    {

      Utils.FRED_VERBOSE(0, "epidemic update for disease %d day %d\n", id, day);
      Utils.fred_start_epidemic_timer();

      // import infections from unknown sources
      get_imported_infections(day);
      // Utils::fred_print_epidemic_timer("imported infections");

      // update markov transitions
      markov_updates(day);

      // transition to infectious
      process_infectious_start_events(day);

      // transition to noninfectious
      process_infectious_end_events(day);

      // transition to symptomatic
      process_symptoms_start_events(day);

      // transition to asymptomatic
      process_symptoms_end_events(day);

      // transition to immune
      process_immunity_start_events(day);

      // transition to susceptible
      process_immunity_end_events(day);

      // Utils::fred_print_epidemic_timer("transition events");

      // update list of infected people
      foreach (var person in this.infected_people)
      {
        Utils.FRED_VERBOSE(1, "update_infection for person %d day %d\n", person.get_id(), day);
        person.update_infection(day, this.id);

        // handle case fatality
        if (person.is_case_fatality(this.id))
        {
          // update epidemic fatality counters
          this.daily_case_fatality_count++;
          this.total_case_fatality_count++;
          // record removed person
          this.removed_people++;
        }

        // note: case fatalities will be uninfected at this point
        if (person.is_infected(this.id) == false)
        {
          Utils.FRED_VERBOSE(1, "update_infection for person %d day %d - deleting from infected_people list\n", person.get_id(), day);
          // delete from infected list
          this.infected_people.Remove(person);
        }
        else
        {
          // update person's mixing group infection counters
          person.update_household_counts(day, this.id);
          person.update_school_counts(day, this.id);
          // move on the next infected person    
        }
      }

      // get list of actually infectious people
      this.actually_infectious_people.Clear();
      foreach (var person in this.potentially_infectious_people)
      {
        if (person.is_infectious(this.id))
        {
          this.actually_infectious_people.Add(person);
          Utils.FRED_VERBOSE(1, "ACTUALLY INF person %d\n", person.get_id());
        }
      }
      this.infectious_people = this.actually_infectious_people.Count;
      // Utils::fred_print_epidemic_timer("identifying actually infections people");

      // update the daily activities of infectious people
      for (int i = 0; i < this.infectious_people; ++i)
      {
        var person = this.actually_infectious_people[i];

        if (this.disease.get_transmission_mode() == "sexual")
        {
          Utils.FRED_VERBOSE(1, "ADDING_ACTUALLY INF person %d\n", person.get_id());
          // this will insert the infectious person onto the infectious list in sexual partner network
          var st_network = Global.Sexual_Partner_Network;
          st_network.add_infectious_person(this.id, person);
        }
        else
        {
          Utils.FRED_VERBOSE(1, "updating activities of infectious person %d -- %d out of %d\n", person.get_id(), i, this.infectious_people);
          person.update_activities_of_infectious_person(day);
          // note: infectious person will be added to the daily places in find_active_places_of_type()
        }
      }
      Utils.fred_print_epidemic_timer("scheduled updated");

      if (this.disease.get_transmission_mode() == "sexual")
      {
        var st_network = Global.Sexual_Partner_Network;
        this.disease.get_transmission().spread_infection(day, this.id, st_network);
        st_network.clear_infectious_people(this.id);
      }
      else
      {
        // spread infection in places attended by actually infectious people
        for (int type = 0; type < 7; ++type)
        {
          find_active_places_of_type(day, type);
          spread_infection_in_active_places(day);
          var msg = $"spread_infection for type {type}";
          Utils.fred_print_epidemic_timer(msg);
        }
      }

      Utils.FRED_VERBOSE(0, "epidemic update finished for disease %d day %d\n", id, day);
      return;
    }

    public virtual void markov_updates(int day) { }

    public void find_active_places_of_type(int day, int place_type)
    {
      Utils.FRED_VERBOSE(1, "find_active_places_of_type %d\n", place_type);
      this.active_places.Clear();
      Utils.FRED_VERBOSE(1, "find_active_places_of_type %d actual %d\n", place_type, this.infectious_people);
      for (int i = 0; i < this.infectious_people; i++)
      {
        var person = this.actually_infectious_people[i];
        Utils.assert(person != null);
        Place place = null;
        switch (place_type)
        {
          case 0:
            place = person.get_household();
            break;
          case 1:
            place = person.get_neighborhood();
            break;
          case 2:
            place = person.get_school();
            break;
          case 3:
            place = person.get_classroom();
            break;
          case 4:
            place = person.get_workplace();
            break;
          case 5:
            place = person.get_office();
            break;
          case 6:
            place = person.get_hospital();
            break;
        }
        Utils.FRED_VERBOSE(1, "find_active_places_of_type %d person %d place %s\n", place_type, person.get_id(), place != null ? place.get_label() : "NULL");
        if (place != null && person.is_present(day, place) && person.is_infectious(this.id))
        {
          Utils.FRED_VERBOSE(1, "add_infection_person %d place %s\n", person.get_id(), place.get_label());
          place.add_infectious_person(this.id, person);
          this.active_places.Insert(0, place);
        }
      }

      // vector transmission mode (for dengue and chikungunya)
      if (this.disease.get_transmission_mode() == "vector")
      {

        // add all places that have any infectious vectors
        int size = 0;
        switch (place_type)
        {
          case 0:
            // add households
            size = Global.Places.get_number_of_households();
            for (int i = 0; i < size; ++i)
            {
              var place = Global.Places.get_household(i);
              if (place.get_infectious_vectors(this.id) > 0)
              {
                this.active_places.Insert(0, place);
              }
            }
            break;
          case 2:
            // add schools
            size = Global.Places.get_number_of_schools();
            for (int i = 0; i < size; ++i)
            {
              var place = Global.Places.get_school(i);
              if (place.get_infectious_vectors(this.id) > 0)
              {
                this.active_places.Insert(0, place);
              }
            }
            break;
          case 4:
            // add workplaces
            size = Global.Places.get_number_of_workplaces();
            for (int i = 0; i < size; ++i)
            {
              var place = Global.Places.get_workplace(i);
              if (place.get_infectious_vectors(this.id) > 0)
              {
                this.active_places.Insert(0, place);
              }
            }
            break;
        }
      }

      Utils.FRED_VERBOSE(1, "find_active_places_of_type %d found %d\n", place_type, this.active_places.Count);

      // convert active set to vector
      this.active_place_vec.Clear();
      foreach (var activePlace in this.active_places)
      {
        this.active_place_vec.Add(activePlace);
      }
      Utils.FRED_VERBOSE(0, "find_active_places_of_type %d day %d found %d\n", place_type, day, this.active_place_vec.Count);

    }

    public void spread_infection_in_active_places(int day)
    {
      Utils.FRED_VERBOSE(0, "spread_infection__active_places day %d\n", day);
      for (int i = 0; i < this.active_place_vec.Count; ++i)
      {
        var place = this.active_place_vec[i];
        this.disease.get_transmission().spread_infection(day, this.id, place);
        place.clear_infectious_people(this.id);
      }
      return;
    }

    public int get_susceptible_people()
    {
      return this.susceptible_people;
    }

    public int get_exposed_people()
    {
      return this.exposed_people;
    }

    public int get_infectious_people()
    {
      return this.infectious_people;
    }

    public int get_removed_people()
    {
      return this.removed_people;
    }

    public int get_immune_people()
    {
      return this.immune_people;
    }

    public int get_people_becoming_infected_today()
    {
      return this.people_becoming_infected_today;
    }

    public int get_total_people_ever_infected()
    {
      return this.population_infection_counts.tot_ppl_evr_inf;
    }

    public int get_people_becoming_symptomatic_today()
    {
      return this.people_becoming_symptomatic_today;
    }

    public int get_people_with_current_symptoms()
    {
      return this.people_with_current_symptoms;
    }

    public int get_daily_case_fatality_count()
    {
      return this.daily_case_fatality_count;
    }

    public int get_total_case_fatality_count()
    {
      return this.total_case_fatality_count;
    }

    public double get_RR()
    {
      return this.RR;
    }

    public double get_attack_rate()
    {
      return this.attack_rate;
    }

    public double get_symptomatic_attack_rate()
    {
      return this.symptomatic_attack_rate;
    }

    public double get_symptomatic_prevalence()
    {
      return this.people_with_current_symptoms / this.N;
    }

    public int get_incidence()
    {
      return this.incidence;
    }

    public int get_symptomatic_incidence()
    {
      return this.symptomatic_incidence;
    }

    public int get_symptomatic_incidence_by_tract_index(int index_)
    {
      this.census_tracts = Global.Places.get_number_of_census_tracts();
      if (index_ >= 0 && index_ < this.census_tracts)
      {
        Utils.FRED_VERBOSE(0, "Census_tracts {0} index {1}", this.census_tracts, index_);
        return this.census_tract_symp_incidence[index_];
      }
      else
      {
        return -1;
      }
    }

    public int get_prevalence_count()
    {
      return this.prevalence_count;
    }

    public double get_prevalence()
    {
      return this.prevalence;
    }

    public int get_incident_infections()
    {
      return get_incidence();
    }

    public void increment_cohort_infectee_count(int cohort_day)
    {
      ++(this.number_infected_by_cohort[cohort_day]);
    }

    public void become_immune(Person person, bool susceptible, bool infectious, bool symptomatic)
    {
      if (!susceptible)
      {
        this.removed_people++;
      }
      if (symptomatic)
      {
        this.people_with_current_symptoms--;
      }
      this.immune_people++;
    }

    public int get_id()
    {
      return this.id;
    }

    public virtual void transition_person(Person person, int day, int state) { }

    // events processing
    public void process_infectious_start_events(int day)
    {
      int size = this.infectious_start_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "INF_START_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.infectious_start_event_queue.get_event(day, i);

        Utils.FRED_VERBOSE(1, "infectious_start_event day %d person %d\n",
         day, person.get_id());

        // update next event list
        int infectious_end_date = person.get_infectious_end_date(this.id);
        this.infectious_end_event_queue.add_event(infectious_end_date, person);

        // add to active people list
        this.potentially_infectious_people.Insert(0, person);

        // update epidemic counters
        this.exposed_people--;

        // update person's health chart
        person.become_infectious(this.disease);
      }
      this.infectious_start_event_queue.clear_events(day);
    }

    public void process_infectious_end_events(int day)
    {
      int size = this.infectious_end_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "INF_END_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.infectious_end_event_queue.get_event(day, i);
        // update person's health chart
        person.become_noninfectious(this.disease);

        // check to see if person has fully recovered:
        int symptoms_end_date = person.get_symptoms_end_date(this.id);
        if (-1 < symptoms_end_date && symptoms_end_date < day)
        {
          recover(person, day);
        }
      }
      this.infectious_end_event_queue.clear_events(day);
    }

    public void recover(Person person, int day)
    {
      Utils.FRED_VERBOSE(1, "infectious_end_event day %d person %d\n", day, person.get_id());

      // remove from active list
      this.potentially_infectious_people.Remove(person);
      this.removed_people++;

      // update person's health chart
      person.recover(day, this.disease);
    }

    public void process_symptoms_start_events(int day)
    {
      int size = this.symptoms_start_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "SYMP_START_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.symptoms_start_event_queue.get_event(day, i);

        // update next event list
        int symptoms_end_date = person.get_symptoms_end_date(this.id);
        this.symptoms_end_event_queue.add_event(symptoms_end_date, person);

        // update epidemic counters
        this.people_with_current_symptoms++;
        this.people_becoming_symptomatic_today++;

        if (Global.Report_Mean_Household_Stats_Per_Income_Category)
        {
          if (person.get_household() != null)
          {
            int income_level = ((Household)person.get_household()).get_household_income_code();
            if (income_level >= (int)Household_income_level_code.CAT_I &&
               income_level < (int)Household_income_level_code.UNCLASSIFIED)
            {
              this.household_income_infection_counts_map[income_level].tot_ppl_evr_sympt++;
            }
          }
        }

        if (Global.Report_Epidemic_Data_By_Census_Tract)
        {
          if (person.get_household() != null)
          {
            var hh = (Household)(person.get_household());
            long census_tract = Global.Places.get_census_tract_with_index(hh.get_census_tract_index());
            if (Household.census_tract_set.IndexOf(census_tract) !=  -1)
            {
              this.census_tract_infection_counts_map[census_tract].tot_ppl_evr_sympt++;
              if (person.is_child())
              {
                this.census_tract_infection_counts_map[census_tract].tot_chldrn_evr_sympt++;
              }
            }
          }
        }

        if (Global.Report_Childhood_Presenteeism)
        {
          if (person.is_student() &&
             person.get_school() != null &&
             person.get_household() != null)
          {
            var schl =(School)(person.get_school());
            var hh = (Household)(person.get_household());
            int income_quartile = schl.get_income_quartile();

            if (person.is_child())
            { //Already know person is student
              this.school_income_infection_counts_map[income_quartile].tot_chldrn_evr_sympt++;
              this.school_income_infection_counts_map[income_quartile].tot_sch_age_chldrn_ever_sympt++;
            }

            if (hh.has_school_aged_child_and_unemployed_adult())
            {
              this.school_income_infection_counts_map[income_quartile].tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt++;
            }
          }
        }

        // update person's health chart
        person.become_symptomatic(this.disease);
      }

      this.symptoms_start_event_queue.clear_events(day);
    }

    public void process_symptoms_end_events(int day)
    {
      int size = symptoms_end_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "SYMP_END_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.symptoms_end_event_queue.get_event(day, i);

        // update epidemic counters
        this.people_with_current_symptoms--;

        // update person's health chart
        person.resolve_symptoms(this.disease);

        // check to see if person has fully recovered:
        int infectious_end_date = person.get_infectious_end_date(this.id);
        if (-1 < infectious_end_date && infectious_end_date <= day)
        {
          recover(person, day);
        }
      }
      this.symptoms_end_event_queue.clear_events(day);
    }
    public void process_immunity_start_events(int day)
    {
      int size = immunity_start_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "IMMUNITY_START_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.immunity_start_event_queue.get_event(day, i);

        // update epidemic counters
        this.immune_people++;

        // update person's health chart
        // person.become_immune(this.id);
      }
      this.immunity_start_event_queue.clear_events(day);
    }

    public void process_immunity_end_events(int day)
    {
      int size = immunity_end_event_queue.get_size(day);
      Utils.FRED_VERBOSE(1, "IMMUNITY_END_EVENT_QUEUE day %d size %d\n", day, size);

      for (int i = 0; i < size; ++i)
      {
        var person = this.immunity_end_event_queue.get_event(day, i);

        // update epidemic counters
        this.immune_people--;

        // update epidemic counters
        this.removed_people++;

        // update person's health chart
        person.become_susceptible(this.id);
      }
      this.immunity_end_event_queue.clear_events(day);
    }

    public void cancel_symptoms_start(int day, Person person)
    {
      this.symptoms_start_event_queue.delete_event(day, person);
    }

    public void cancel_symptoms_end(int day, Person person)
    {
      this.symptoms_end_event_queue.delete_event(day, person);
    }

    public void cancel_infectious_start(int day, Person person)
    {
      this.infectious_start_event_queue.delete_event(day, person);
    }

    public void cancel_infectious_end(int day, Person person)
    {
      this.infectious_end_event_queue.delete_event(day, person);
    }

    public void cancel_immunity_start(int day, Person person)
    {
      this.immunity_start_event_queue.delete_event(day, person);
    }

    public void cancel_immunity_end(int day, Person person)
    {
      this.immunity_end_event_queue.delete_event(day, person);
    }

    public virtual void end_of_run() { }
    public virtual void terminate_person(Person person, int day)
    {
      Utils.FRED_VERBOSE(1, "EPIDEMIC TERMINATE person {0} day {1}\n",
             person.get_id(), day);

      // cancel any events for this person
      int date = person.get_symptoms_start_date(this.id);
      if (date > day)
      {
        Utils.FRED_VERBOSE(0, "EPIDEMIC CANCEL symptoms_start_date {0} {1}\n", date, day);
        cancel_symptoms_start(date, person);
      }
      else if (date > -1)
      {
        date = person.get_symptoms_end_date(this.id);
        if (date > day)
        {
          Utils.FRED_VERBOSE(0, "EPIDEMIC CANCEL symptoms_end_date {0} {1}\n", date, day);
          cancel_symptoms_end(date, person);
        }
      }

      date = person.get_infectious_start_date(this.id);
      if (date > day)
      {
        Utils.FRED_VERBOSE(0, "EPIDEMIC CANCEL infectious_start_date {0} {1}\n", date, day);
        cancel_infectious_start(date, person);
      }
      else if (date > -1)
      {
        date = person.get_infectious_end_date(this.id);
        if (date > day)
        {
          Utils.FRED_VERBOSE(0, "EPIDEMIC CANCEL infectious_end_date {0} {1}\n", date, day);
          cancel_infectious_end(date, person);
        }
      }

      date = person.get_immunity_end_date(this.id);
      if (date > day)
      {
        Utils.FRED_VERBOSE(0, "EPIDEMIC CANCEL immunity_end_date {0} {1}\n", date, day);
        cancel_immunity_end(date, person);
      }

      Utils.FRED_VERBOSE(1, "EPIDEMIC TERMINATE finished\n");
    }

    /// advances infection either to the first infetious day (SEED_INFECTIOUS)
    /// or to a random day in the trajectory (SEED_RANDOM)
    /// this is accomplished by moving the exposure date back as appropriate;
    /// (ultimately done in Infection.advance_infection)
    protected void advance_seed_infection(Person person)
    {
      // if advanced_seeding is infectious or random
      int d = this.disease.get_id();
      int advance = 0;
      int duration = person.get_infectious_end_date(d) - person.get_exposure_date(d);
      Utils.assert(duration > 0);
      if (this.seeding_type == SEED_RANDOM)
      {
        advance = FredRandom.Next(0, duration);
      }
      else if (FredRandom.NextDouble() < this.fraction_seeds_infectious)
      {
        advance = person.get_infectious_start_date(d) - person.get_exposure_date(d);
      }
      Utils.assert(advance <= duration);
      Utils.FRED_VERBOSE(0, "%s %s %s %d %s %d %s\n", "advanced_seeding:", seeding_type_name,
             "=> advance infection trajectory of duration", duration, "by", advance, "days");
      person.advance_seed_infection(d, advance);
    }

    protected static int get_age_group(int age)
    {
      if (age < 5)
      {
        return 0;
      }
      if (age < 19)
      {
        return 1;
      }
      if (age < 65)
      {
        return 2;
      }
      return 3;
    }
  }
}
