using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Place_List
  {
    private readonly List<Place> places;
    private readonly List<Place> households;
    private readonly List<Place> neighborhoods;
    private readonly List<Place> schools;
    private readonly List<Place> workplaces;
    private readonly List<Place> hospitals;
    private readonly List<Place>[] schools_by_grade = new List<Place>[Neighborhood_Patch.GRADES];
    private int number_of_demes;
    private bool is_primary_care_assignment_initialized;

    // input files
    private string MSA_file;
    private string Counties_file;
    private string States_file;

    // list of counties
    private List<County> counties;

    // list of census_tracts
    private List<long> census_tracts;

    // mean size of "household" associated with group quarters
    private static double College_dorm_mean_size = 3.5;
    private static double Military_barracks_mean_size = 12;
    private static double Prison_cell_mean_size = 1.5;
    private static double Nursing_home_room_mean_size = 1.5;

    // non-resident staff for group quarters
    private static int College_fixed_staff;
    private static double College_resident_to_staff_ratio;
    private static int Prison_fixed_staff;
    private static double Prison_resident_to_staff_ratio;
    private static int Nursing_home_fixed_staff;
    private static double Nursing_home_resident_to_staff_ratio;
    private static int Military_fixed_staff;
    private static double Military_resident_to_staff_ratio;

    // the following support household shelter:
    private static int Enable_copy_files;
    private static int Shelter_duration_mean;
    private static int Shelter_duration_std;
    private static int Shelter_delay_mean;
    private static int Shelter_delay_std;
    private static double Pct_households_sheltering;
    private static bool High_income_households_sheltering;
    private static double Early_shelter_rate;
    private static double Shelter_decay_rate;

    // Hospital support
    private static bool Household_hospital_map_file_exists;
    private static int Hospital_fixed_staff = 1;
    private static double Hospital_worker_to_bed_ratio = 1;
    private static double Hospital_outpatients_per_day_per_employee;
    private static double Healthcare_clinic_outpatients_per_day_per_employee;
    private static int Hospital_min_bed_threshold;
    private static double Hospitalization_radius;

    private readonly static Dictionary<int, int> Hospital_ID_total_assigned_size_map = new Dictionary<int, int>();
    private readonly static Dictionary<int, int> Hospital_ID_current_assigned_size_map = new Dictionary<int, int>();
    private static int Hospital_overall_panel_size;

    private static int HAZEL_disaster_start_sim_day = -1;
    private static int HAZEL_disaster_end_sim_day = -1;
    private static int HAZEL_disaster_evac_start_offset;
    private static int HAZEL_disaster_evac_end_offset;
    private static int HAZEL_disaster_return_start_offset;
    private static int HAZEL_disaster_return_end_offset;
    private static double HAZEL_disaster_evac_prob_per_day;
    private static double HAZEL_disaster_return_prob_per_day;
    private static int HAZEL_mobile_van_max;

    // School support
    private static int School_fixed_staff;
    private static double School_student_teacher_ratio;

    private int next_place_id;
    private bool load_completed;
    private int min_household_income;
    private int max_household_income;
    private int median_household_income;
    private int first_quartile_household_income;
    private int third_quartile_household_income;
    // For hospitalization
    private FredGeo min_lat, max_lat, min_lon, max_lon;
    private readonly Dictionary<char, int> place_type_counts = new Dictionary<char, int>();
    private readonly Dictionary<string, int> place_label_map = new Dictionary<string, int>();
    private readonly Dictionary<string, int> household_hospital_map = new Dictionary<string, int>();
    private readonly Dictionary<char, string> place_type_name_lookup_map = new Dictionary<char, string>();

    public Place_List()
    {
      this.load_completed = false;
      this.is_primary_care_assignment_initialized = false;
      this.places = new List<Place>();
      this.schools = new List<Place>();
      this.hospitals = new List<Place>();
      this.workplaces = new List<Place>();
      this.households = new List<Place>();
      this.neighborhoods = new List<Place>();
      this.next_place_id = 0;
      init_place_type_name_lookup_map();
    }

    public void read_all_places(List<Utils.Tokens> Demes);
    public void read_places(string pop_dir, string pop_id, char deme_id, List<Place_Init_Data> pids);
    public void reassign_workers();
    public void prepare();
    public void print_status_of_schools(int day);
    public void update(int day);
    public void quality_control();
    public void report_school_distributions(int day);
    public void report_household_distributions();
    public void get_parameters()
    {
      // get static parameters for all place subclasses
      Household.get_parameters();
      Neighborhood.get_parameters();
      School.get_parameters();
      Classroom.get_parameters();
      Workplace.get_parameters();
      Office.get_parameters();
      Hospital.get_parameters();

      FredParameters.GetParameter("enable_copy_files", ref Enable_copy_files);

      // geography
      FredParameters.GetParameter("msa_file", ref MSA_file);
      FredParameters.GetParameter("counties_file", ref Counties_file);
      FredParameters.GetParameter("states_file", ref States_file);

      // population parameters
      FredParameters.GetParameter("synthetic_population_directory", ref Global.Synthetic_population_directory);
      FredParameters.GetParameter("synthetic_population_id", ref Global.Synthetic_population_id);
      FredParameters.GetParameter("synthetic_population_version", ref Global.Synthetic_population_version);
      FredParameters.GetParameter("city", ref Global.City);
      FredParameters.GetParameter("county", ref Global.County);
      FredParameters.GetParameter("state", ref Global.US_state);
      FredParameters.GetParameter("fips", ref Global.FIPS_code);
      FredParameters.GetParameter("msa", ref Global.MSA_code);

      if (Global.Enable_Group_Quarters)
      {
        // group quarter parameters
        FredParameters.GetParameter("college_dorm_mean_size", ref College_dorm_mean_size);
        FredParameters.GetParameter("military_barracks_mean_size", ref Military_barracks_mean_size);
        FredParameters.GetParameter("prison_cell_mean_size", ref Prison_cell_mean_size);
        FredParameters.GetParameter("nursing_home_room_mean_size", ref Nursing_home_room_mean_size);

        FredParameters.GetParameter("school_fixed_staff", ref School_fixed_staff);
        FredParameters.GetParameter("school_student_teacher_ratio", ref School_student_teacher_ratio);
        FredParameters.GetParameter("college_fixed_staff", ref College_fixed_staff);
        FredParameters.GetParameter("college_resident_to_staff_ratio", ref College_resident_to_staff_ratio);
        FredParameters.GetParameter("prison_fixed_staff", ref Prison_fixed_staff);
        FredParameters.GetParameter("prison_resident_to_staff_ratio", ref Prison_resident_to_staff_ratio);
        FredParameters.GetParameter("nursing_home_fixed_staff", ref Nursing_home_fixed_staff);
        FredParameters.GetParameter("nursing_home_resident_to_staff_ratio", ref Nursing_home_resident_to_staff_ratio);
        FredParameters.GetParameter("military_fixed_staff", ref Military_fixed_staff);
        FredParameters.GetParameter("military_resident_to_staff_ratio", ref Military_resident_to_staff_ratio);
      }

      // household shelter parameters
      if (Global.Enable_Household_Shelter)
      {
        FredParameters.GetParameter("shelter_in_place_duration_mean", ref Shelter_duration_mean);
        FredParameters.GetParameter("shelter_in_place_duration_std", ref Shelter_duration_std);
        FredParameters.GetParameter("shelter_in_place_delay_mean", ref Shelter_delay_mean);
        FredParameters.GetParameter("shelter_in_place_delay_std", ref Shelter_delay_std);
        FredParameters.GetParameter("shelter_in_place_compliance", ref Pct_households_sheltering);
        int temp_int = 0;
        FredParameters.GetParameter("shelter_in_place_by_income", ref temp_int);
        High_income_households_sheltering = temp_int != 0;
        FredParameters.GetParameter("shelter_in_place_early_rate", ref Early_shelter_rate);
        FredParameters.GetParameter("shelter_in_place_decay_rate", ref Shelter_decay_rate);
      }

      // household evacuation parameters
      if (Global.Enable_HAZEL)
      {
        FredParameters.GetParameter("HAZEL_disaster_start_sim_day", ref HAZEL_disaster_start_sim_day);
        FredParameters.GetParameter("HAZEL_disaster_end_sim_day", ref HAZEL_disaster_end_sim_day);
        FredParameters.GetParameter("HAZEL_disaster_evac_start_offset", ref HAZEL_disaster_evac_start_offset);
        FredParameters.GetParameter("HAZEL_disaster_evac_end_offset", ref HAZEL_disaster_evac_end_offset);
        FredParameters.GetParameter("HAZEL_disaster_return_start_offset", ref HAZEL_disaster_return_start_offset);
        FredParameters.GetParameter("HAZEL_disaster_return_end_offset", ref HAZEL_disaster_return_end_offset);
        FredParameters.GetParameter("HAZEL_disaster_evac_prob_per_day", ref HAZEL_disaster_evac_prob_per_day);
        FredParameters.GetParameter("HAZEL_disaster_return_prob_per_day", ref HAZEL_disaster_return_prob_per_day);
        FredParameters.GetParameter("HAZEL_mobile_van_max", ref HAZEL_mobile_van_max);
      }

      if (Global.Enable_Hospitals)
      {
        string hosp_file_dir = string.Empty;
        string hh_hosp_map_file_name = string.Empty;

        FredParameters.GetParameter("household_hospital_map_file_directory", ref hosp_file_dir);
        FredParameters.GetParameter("household_hospital_map_file", ref hh_hosp_map_file_name);
        FredParameters.GetParameter("hospital_worker_to_bed_ratio", ref Hospital_worker_to_bed_ratio);
        Hospital_worker_to_bed_ratio = Hospital_worker_to_bed_ratio == 0.0 ? 1.0 : Hospital_worker_to_bed_ratio;
        FredParameters.GetParameter("hospital_outpatients_per_day_per_employee", ref Hospital_outpatients_per_day_per_employee);
        FredParameters.GetParameter("healthcare_clinic_outpatients_per_day_per_employee", ref Healthcare_clinic_outpatients_per_day_per_employee);
        FredParameters.GetParameter("hospital_min_bed_threshold", ref Hospital_min_bed_threshold);
        FredParameters.GetParameter("hospitalization_radius", ref Hospitalization_radius);
        FredParameters.GetParameter("hospital_fixed_staff", ref Hospital_fixed_staff);
        if (hh_hosp_map_file_name == "none")
        {
          Household_hospital_map_file_exists = false;
        }
        else
        {
          //If there is a file mapping Households to Hospitals, open it
          FILE* hospital_household_map_fp = NULL;

          char filename[FRED_STRING_SIZE];

          sprintf(filename, "%s%s", hosp_file_dir, hh_hosp_map_file_name);

          hospital_household_map_fp = Utils.fred_open_file(filename);
          if (hospital_household_map_fp != NULL)
          {
            Place_List.Household_hospital_map_file_exists = true;
            //    enum column_index
            //{
            //  hh_id = 0, hospital_id = 1
            //};
            char line_str[255];
            Utils.Tokens tokens;
            for (char* line = line_str; fgets(line, 255, hospital_household_map_fp); line = line_str)
            {
              tokens = Utils.split_by_delim(line, ',', tokens, false);
              // skip header line
              if (strcmp(tokens[hh_id], "hh_id") != 0 && strcmp(tokens[hh_id], "sp_id") != 0)
              {
                char s[80];

                sprintf(s, "%s", tokens[hh_id]);
                string hh_id_str(s);
                sprintf(s, "%s", tokens[hospital_id]);
                string hosp_id_str(s);
                int hosp_id = 0;
                sscanf(hosp_id_str.c_str(), "%d", &hosp_id);
                this->household_hospital_map.insert(std.pair<string, int>(hh_id_str, hosp_id));
              }
              tokens.clear();
            }
            fclose(hospital_household_map_fp);
          }
        }
      }

      //added for cbsa
      if (strcmp(Global.MSA_code, "none") != 0)
      {
        // msa param overrides other locations, used to populate the synthetic_population_id
        // get fips(s) from msa code
        char msaline_string[FRED_STRING_SIZE];
        char pop_id[FRED_FIPS_LIST_SIZE];
        char* msaline;
        char* cbsa;
        char* msa;
        char* fips;
        int msafound = 0;
        int msaLength = strlen(Global.MSA_code);
        if (msaLength == 5)
        {
          FILE* msafp = Utils.fred_open_file(Place_List.MSA_file);
          if (msafp == NULL)
          {
            Utils.fred_abort("msa file |%s| NOT FOUND\n", Place_List.MSA_file);
          }
          while (fgets(msaline_string, FRED_STRING_SIZE - 1, msafp) != NULL)
          {
            msaline = msaline_string;
            cbsa = strsep(&msaline, "\t");
            msa = strsep(&msaline, "\n");
            if (strcmp(Global.MSA_code, cbsa) == 0)
            {
              msafound = 1;
              break;
            }
          }
          fclose(msafp);
          if (msafound)
          {
            Utils.fred_log("FOUND FIPS = |%s msa | for cbsa = |%s|\n", msa, cbsa);
            int first = 1;
            while ((fips = strsep(&msa, " ")) != NULL)
            {
              if (first == 1)
              { //first one uses strcpy to start string
                strcpy(pop_id, Global.Synthetic_population_version);
                strcat(pop_id, "_");
                strcat(pop_id, fips);
                first++;
              }
              else
              {
                strcat(pop_id, " ");
                strcat(pop_id, Global.Synthetic_population_version);
                strcat(pop_id, "_");
                strcat(pop_id, fips);
              }
            }
            sprintf(Global.Synthetic_population_id, "%s", pop_id);
          }
          else
          {
            Utils.fred_abort("Sorry, could not find fips for MSA = |%s|\n", Global.MSA_code);
          }
        }
      }
      else if (strcmp(Global.FIPS_code, "none") != 0)
      {

        // fips param overrides the synthetic_population_id

        // get population_id from fips
        char line_string[FRED_STRING_SIZE];
        char* line;
        char* city;
        char* state;
        char* county;
        char* fips;
        int found = 0;
        int fipsLength = strlen(Global.FIPS_code);
        if (fipsLength == 5)
        {
          FILE* fp = Utils.fred_open_file(Place_List.Counties_file);
          if (fp == NULL)
          {
            Utils.fred_abort("counties file |%s| NOT FOUND\n", Place_List.Counties_file);
          }
          while (fgets(line_string, FRED_STRING_SIZE - 1, fp) != NULL)
          {
            line = line_string;
            city = strsep(&line, "\t");
            state = strsep(&line, "\t");
            county = strsep(&line, "\t");
            fips = strsep(&line, "\n");
            // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
            // city,state,county,fips);
            if (strcmp(Global.FIPS_code, fips) == 0)
            {
              found = 1;
              break;
            }
          }
          fclose(fp);
          if (found)
          {
            Utils.fred_log("FOUND a county = |%s County %s| for fips = |%s|\n", county, state, fips);
            sprintf(Global.Synthetic_population_id, "%s_%s", Global.Synthetic_population_version, fips);
          }
          else
          {
            Utils.fred_abort("Sorry, could not find a county for fips = |%s|\n", Global.FIPS_code);
          }
        }
        else if (fipsLength == 2)
        {
          // get population_id from state
          char line_string[FRED_STRING_SIZE];
          char* line;
          char* abbrev;
          char* state;
          char* fips;
          int found = 0;
          FILE* fp = Utils.fred_open_file(Place_List.States_file);
          if (fp == NULL)
          {
            Utils.fred_abort("states file |%s| NOT FOUND\n", Place_List.States_file);
          }
          while (fgets(line_string, FRED_STRING_SIZE - 1, fp) != NULL)
          {
            line = line_string;
            fips = strsep(&line, "\t");
            abbrev = strsep(&line, "\t");
            state = strsep(&line, "\n");
            if (strcmp(Global.FIPS_code, fips) == 0)
            {
              found = 1;
              break;
            }
          }
          fclose(fp);
          if (found)
          {
            Utils.fred_log("FOUND state = |%s| state_abbrev = |%s| fips = |%s|\n", state, abbrev, fips);
            sprintf(Global.Synthetic_population_id, "%s_%s", Global.Synthetic_population_version, fips);
          }
          else
          {
            Utils.fred_abort("Sorry, could not find state called |%s|\n", Global.US_state);
          }
        }
        else
        {
          Utils.fred_abort(
              "FRED keyword fips only supports 2 digits (for states) and 5 digits (for counties), you specified %s",
              Global.FIPS_code);
        }
      }
      else if (strcmp(Global.City, "none") != 0)
      {

        // city param overrides the synthetic_population_id

        // delete any commas and periods
        Utils.delete_char(Global.City, ',', FRED_STRING_SIZE);
        Utils.delete_char(Global.City, '.', FRED_STRING_SIZE);

        // replace white space characters with a single space
        Utils.normalize_white_space(Global.City);

        // get population_id from city
        char city_state[FRED_STRING_SIZE];
        char line_string[FRED_STRING_SIZE];
        char* line;
        char* city;
        char* state;
        char* county;
        char* fips;
        int found = 0;
        FILE* fp = Utils.fred_open_file(Place_List.Counties_file);
        if (fp == NULL)
        {
          Utils.fred_abort("counties file |%s| NOT FOUND\n", Place_List.Counties_file);
        }
        while (fgets(line_string, FRED_STRING_SIZE - 1, fp) != NULL)
        {
          line = line_string;
          city = strsep(&line, "\t");
          state = strsep(&line, "\t");
          county = strsep(&line, "\t");
          fips = strsep(&line, "\n");
          // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
          // city,state,county,fips);
          sprintf(city_state, "%s %s", city, state);
          if (strcmp(Global.City, city_state) == 0)
          {
            found = 1;
            break;
          }
        }
        fclose(fp);
        if (found)
        {
          Utils.fred_log("FOUND a county for city = |%s| county = |%s County %s| and fips = |%s|\n", Global.City, county,
              state, fips);
          sprintf(Global.Synthetic_population_id, "%s_%s", Global.Synthetic_population_version, fips);
        }
        else
        {
          Utils.fred_abort("Sorry, could not find a county for city = |%s|\n", Global.City);
        }
      }
      else if (strcmp(Global.County, "none") != 0)
      {

        // county param overrides the synthetic_population_id

        // delete any commas and periods
        Utils.delete_char(Global.County, ',', FRED_STRING_SIZE);
        Utils.delete_char(Global.County, '.', FRED_STRING_SIZE);

        // replace white space characters with a single space
        Utils.normalize_white_space(Global.County);

        // get population_id from county
        char county_state[FRED_STRING_SIZE];
        char line_string[FRED_STRING_SIZE];
        char* line;
        char* city;
        char* state;
        char* county;
        char* fips;
        int found = 0;
        FILE* fp = Utils.fred_open_file(Place_List.Counties_file);
        if (fp == NULL)
        {
          Utils.fred_abort("counties file |%s| NOT FOUND\n", Place_List.Counties_file);
        }
        while (fgets(line_string, FRED_STRING_SIZE - 1, fp) != NULL)
        {
          line = line_string;
          city = strsep(&line, "\t");
          state = strsep(&line, "\t");
          county = strsep(&line, "\t");
          fips = strsep(&line, "\n");
          // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
          // city,state,county,fips);
          sprintf(county_state, "%s County %s", county, state);
          if (strcmp(Global.County, county_state) == 0)
          {
            found = 1;
            break;
          }
        }
        fclose(fp);
        if (found)
        {
          Utils.fred_log("FOUND county = |%s| fips = |%s|\n", county_state, fips);
          sprintf(Global.Synthetic_population_id, "%s_%s", Global.Synthetic_population_version, fips);
        }
        else
        {
          Utils.fred_abort("Sorry, could not find county called |%s|\n", Global.County);
        }
      }
      else if (strcmp(Global.US_state, "none") != 0)
      {

        // state param overrides the synthetic_population_id

        // delete any commas and periods
        Utils.delete_char(Global.US_state, ',', FRED_STRING_SIZE);
        Utils.delete_char(Global.US_state, '.', FRED_STRING_SIZE);

        // replace white space characters with a single space
        Utils.normalize_white_space(Global.US_state);

        // get population_id from state
        char line_string[FRED_STRING_SIZE];
        char* line;
        char* abbrev;
        char* state;
        char* fips;
        int found = 0;
        FILE* fp = Utils.fred_open_file(Place_List.States_file);
        if (fp == NULL)
        {
          Utils.fred_abort("states file |%s| NOT FOUND\n", Place_List.States_file);
        }
        while (fgets(line_string, FRED_STRING_SIZE - 1, fp) != NULL)
        {
          line = line_string;
          fips = strsep(&line, "\t");
          abbrev = strsep(&line, "\t");
          state = strsep(&line, "\n");
          if (strcmp(Global.US_state, abbrev) == 0 || strcmp(Global.US_state, state) == 0)
          {
            found = 1;
            break;
          }
        }
        fclose(fp);
        if (found)
        {
          Utils.fred_log("FOUND state = |%s| state_abbrev = |%s| fips = |%s|\n", state, abbrev, fips);
          sprintf(Global.Synthetic_population_id, "%s_%s", Global.Synthetic_population_version, fips);
        }
        else
        {
          Utils.fred_abort("Sorry, could not find state called |%s|\n", Global.US_state);
        }
      }
    }

    public int get_new_place_id()
    {
      int id = this.next_place_id;
      ++(this.next_place_id);
      return id;
    }

    public void setup_group_quarters();
    public void setup_households();
    public void setup_classrooms();
    public void setup_offices();
    public void setup_HAZEL_mobile_vans();
    public void setup_school_income_quartile_pop_sizes();
    public void setup_household_income_quartile_sick_days();
    public int get_min_household_income_by_percentile(int percentile);
    public Place get_place_from_label(string s);
    public Place get_random_workplace();
    public void assign_hospitals_to_households();

    /**
     * Uses a gravity model to find a random open hospital given the search parameters.
     * The location must allows overnight stays (have a subtype of NONE)
     * @param sim_day the simulation day
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_open_hospital_matching_criteria(int sim_day, Person per, bool check_insurance, bool use_search_radius_limit);

    /**
     * Uses a gravity model to find a random open healthcare location given the search parameters.
     * The search is ambivalent about the location allowing overnight stays.
     * @param sim_day the simulation day
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_open_healthcare_facility_matching_criteria(int sim_day, Person per, bool check_insurance, bool use_search_radius_limit);

    /**
     * Uses a gravity model to find a random open healthcare location given the search parameters.
     * The search is ambivalent about the location allowing overnight stays, but it must be open on sim_day 0
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_primary_care_facility_matching_criteria(Person per, bool check_insurance, bool use_search_radius_limit);
    public void print_household_size_distribution(string dir, string date_string, int run);
    public void report_shelter_stats(int day);
    public void end_of_run();

    public int get_number_of_demes()
    {
      return this.number_of_demes;
    }

    public int get_housing_data(int[] target_size, int[] current_size);
    public void get_initial_visualization_data_from_households();
    public void get_visualization_data_from_households(int day, int disease_id, int output_code);
    public void get_census_tract_data_from_households(int day, int disease_id, int output_code);
    public void swap_houses(int house_index1, int house_index2);
    public void combine_households(int house_index1, int house_index2);

    public Place select_school(int county_index, int grade);

    public int get_number_of_counties()
    {
      return (int)this.counties.Count;
    }

    public County get_county_with_index(int index)
    {
      Utils.assert(index < this.counties.Count);
      return this.counties[index];
    }

    public int get_fips_of_county_with_index(int index)
    {
      if (index < 0)
      {
        return 99999;
      }
      Utils.assert(index < this.counties.Count);
      return this.counties[index].get_fips();
    }

    public int get_population_of_county_with_index(int index)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      return this.counties[index].get_tot_current_popsize();
    }

    public int get_population_of_county_with_index(int index, int age)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      int retval = this.counties[index].get_current_popsize(age);
      return (retval < 0 ? 0 : retval);
    }

    public int get_population_of_county_with_index(int index, int age, char sex)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      int retval = this.counties[index].get_current_popsize(age, sex);
      return (retval < 0 ? 0 : retval);
    }

    public int get_population_of_county_with_index(int index, int age_min, int age_max, char sex)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      int retval = this.counties[index].get_current_popsize(age_min, age_max, sex);
      return (retval < 0 ? 0 : retval);
    }

    public void increment_population_of_county_with_index(int index, Person person)
    {
      if (index < 0)
      {
        return;
      }
      Utils.assert(index < this.counties.Count);
      int fips = this.counties[index].get_fips();
      bool test = this.counties[index].increment_popsize(person);
      Utils.assert(test);
      return;
    }

    public void decrement_population_of_county_with_index(int index, Person person)
    {
      if (index < 0)
      {
        return;
      }
      Utils.assert(index < this.counties.Count);
      bool test = this.counties[index].decrement_popsize(person);
      Utils.assert(test);
      return;
    }

    public void report_county_populations()
    {
      for (int index = 0; index < this.counties.Count; ++index)
      {
        this.counties[index].report_county_population();
      }
    }

    public int get_number_of_census_tracts()
    {
      return (int)this.census_tracts.Count;
    }

    public long get_census_tract_with_index(int index)
    {
      Utils.assert(index < this.census_tracts.Count);
      return this.census_tracts[index];
    }

    public bool is_load_completed()
    {
      return this.load_completed;
    }

    public void update_population_dynamics(int day);

    public void delete_place_label_map();

    public void print_stats(int day);

    public static int get_HAZEL_disaster_start_sim_day();
    public static int get_HAZEL_disaster_end_sim_day();
    public static void increment_hospital_ID_current_assigned_size_map(int hospital_id)
    {
      Hospital_ID_current_assigned_size_map[hospital_id]++;
    }

    // access function for places by type

    public int get_number_of_places()
    {
      return this.places.Count;
    }

    public Place get_place(int i)
    {
      if (0 <= i && i < get_number_of_places())
      {
        return this.places[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_households()
    {
      return (int)this.households.Count;
    }

    public Place get_household(int i)
    {
      if (0 <= i && i < get_number_of_households())
      {
        return this.households[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_neighborhoods()
    {
      return this.neighborhoods.Count;
    }

    public Place get_neighborhood(int i)
    {
      if (0 <= i && i < get_number_of_neighborhoods())
      {
        return this.neighborhoods[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_schools()
    {
      return this.schools.Count;
    }

    public Place get_school(int i)
    {
      if (0 <= i && i < get_number_of_schools())
      {
        return this.schools[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_workplaces()
    {
      return this.workplaces.Count;
    }

    public Place get_workplace(int i)
    {
      if (0 <= i && i < get_number_of_workplaces())
      {
        return this.workplaces[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_hospitals()
    {
      return this.hospitals.Count;
    }

    public Place get_hospital(int i)
    {
      if (0 <= i && i < get_number_of_hospitals())
      {
        return this.hospitals[i];
      }
      else
      {
        return null;
      }
    }

    // access function for when we need a Household pointer
    public Household get_household_ptr(int i)
    {
      return (Household)get_household(i);
    }

    // access function for when we need a Neighborhood pointer
    public Neighborhood get_neighborhood_ptr(int i)
    {
      return (Neighborhood)get_neighborhood(i);
    }

    // access function for when we need a School pointer
    public School get_school_ptr(int i)
    {
      return (School)get_school(i);
    }

    // access function for when we need a Workplace pointer
    public Workplace get_workplace_ptr(int i)
    {
      return (Workplace)get_workplace(i);
    }

    // access function for when we need a Hospital pointer
    public Hospital get_hospital_ptr(int i)
    {
      return (Hospital)get_hospital(i);
    }

    private double distance_between_places(Place p1, Place p2)
    {
      return Geo.xy_distance(p1.get_latitude(), p1.get_longitude(), p2.get_latitude(), p2.get_longitude());
    }

    private void read_household_file(char deme_id, string location_file, List<Place_Init_Data> pids);
    private void read_workplace_file(char deme_id, string location_file, List<Place_Init_Data> pids);
    private void read_hospital_file(char deme_id, string location_file, List<Place_Init_Data> pids);
    private void read_school_file(char deme_id, string location_file, List<Place_Init_Data> pids);
    private void read_group_quarters_file(char deme_id, string location_file, List<Place_Init_Data> pids);
    private void reassign_workers_to_places_of_type(char place_type, int fixed_staff, double resident_to_staff_ratio);
    private void reassign_workers_to_group_quarters(char subtype, int fixed_staff, double resident_to_staff_ratio);
    private void prepare_primary_care_assignment();

    /**
     * @param hh a pointer to a Household object
     *
     * If there is already a Hospital assigned to a Household int the map household_hospital_map, then just return it.
     * Otherwise, find a suitable hospital (must allow overnight stays) and assign it to a household (put it in the map for later)
     *
     * @return a pointer to the Hospital that is assigned to the Household
     */
    private Hospital get_hospital_assigned_to_household(Household hh);
    private void report_household_incomes();
    private void select_households_for_shelter();
    private void shelter_household(Household h);
    private void select_households_for_evacuation();
    private void evacuate_household(Household h);

    private void init_place_type_name_lookup_map()
    {
      place_type_name_lookup_map.Clear();
      place_type_name_lookup_map.Add(Place.TYPE_NEIGHBORHOOD, "NEIGHBORHOOD");
      place_type_name_lookup_map.Add(Place.TYPE_HOUSEHOLD, "HOUSEHOLD");
      place_type_name_lookup_map.Add(Place.TYPE_SCHOOL, "SCHOOL");
      place_type_name_lookup_map.Add(Place.TYPE_CLASSROOM, "CLASSROOM");
      place_type_name_lookup_map.Add(Place.TYPE_WORKPLACE, "WORKPLACE");
      place_type_name_lookup_map.Add(Place.TYPE_OFFICE, "OFFICE");
      place_type_name_lookup_map.Add(Place.TYPE_HOSPITAL, "HOSPITAL");
      place_type_name_lookup_map.Add(Place.TYPE_COMMUNITY, "COMMUNITY");
    }

    private bool add_place(Place p);
    private void parse_lines_from_stream(TextWriter stream, List<Place_Init_Data> pids);

    private string lookup_place_type_name(char place_type)
    {
      Utils.assert(this.place_type_name_lookup_map.ContainsKey(place_type));
      return this.place_type_name_lookup_map[place_type];
    }

    private void set_number_of_demes(int n)
    {
      this.number_of_demes = n;
    }
  }
}
