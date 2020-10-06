using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly List<County> counties = new List<County>();

    // list of census_tracts
    private readonly List<long> census_tracts = new List<long>();

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
      MSA_file = Utils.get_fred_file_name(MSA_file);
      FredParameters.GetParameter("counties_file", ref Counties_file);
      Counties_file = Utils.get_fred_file_name(Counties_file);
      FredParameters.GetParameter("states_file", ref States_file);
      States_file = Utils.get_fred_file_name(States_file);

      // population parameters
      FredParameters.GetParameter("synthetic_population_directory", ref Global.Synthetic_population_directory);
      Global.Synthetic_population_directory = Utils.get_fred_file_name(Global.Synthetic_population_directory);
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
        hosp_file_dir = Utils.get_fred_file_name(hosp_file_dir);
        FredParameters.GetParameter("household_hospital_map_file", ref hh_hosp_map_file_name);
        hh_hosp_map_file_name = Utils.get_fred_file_name(hh_hosp_map_file_name);
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
          string fileName = Path.Combine(hosp_file_dir, hh_hosp_map_file_name);
          if (File.Exists(fileName))
          {
            var hospital_household_map_fp = new StreamReader(fileName);
            Household_hospital_map_file_exists = true;
            string line_str;
            while (hospital_household_map_fp.Peek() != -1)
            {
              line_str = hospital_household_map_fp.ReadLine();
              var tokens = line_str.Split(',');
              if (tokens[0] == "hh_id" || tokens[0] == "sp_id")
              {
                continue;
              }

              this.household_hospital_map.Add(tokens[0], Convert.ToInt32(tokens[1]));
            }

            hospital_household_map_fp.Dispose();
          }
        }
      }

      //added for cbsa
      if (Global.MSA_code != "none")
      {
        // msa param overrides other locations, used to populate the synthetic_population_id
        // get fips(s) from msa code
        string msaline;
        string pop_id = string.Empty;
        string cbsa = string.Empty;
        string msa = string.Empty;
        bool msafound = false;
        int msaLength = Global.MSA_code.Length;
        if (msaLength == 5)
        {
          if (!File.Exists(MSA_file))
          {
            Utils.fred_abort("msa file |{0}| NOT FOUND\n", MSA_file);
          }

          var msafp = new StreamReader(MSA_file);
          while (msafp.Peek() != -1)
          {
            msaline = msafp.ReadLine();
            var tokens = msaline.Split('\t');
            cbsa = tokens[0];
            msa = tokens[1];
            if (Global.MSA_code == cbsa)
            {
              msafound = true;
              break;
            }
          }

          msafp.Dispose();
          if (msafound)
          {
            Utils.FRED_STATUS(0, "FOUND FIPS = |{0} msa | for cbsa = |{1}|\n", msa, cbsa);
            int first = 1;
            var tokens = msa.Split(' ');
            //while ((fips = strsep(&msa, " ")) != null)
            foreach (var fips in tokens)
            {
              if (first == 1)
              { //first one uses strcpy to start string
                pop_id = $"{Global.Synthetic_population_version}_{fips}";
                first++;
              }
              else
              {
                pop_id += $" {Global.Synthetic_population_version}_{fips}";
              }
            }
            Console.WriteLine(Global.Synthetic_population_id, "{0}", pop_id);
          }
          else
          {
            Utils.fred_abort("Sorry, could not find fips for MSA = |%s|\n", Global.MSA_code);
          }
        }
      }
      else if (Global.FIPS_code != "none")
      {
        // fips param overrides the synthetic_population_id
        // get population_id from fips
        string line;
        string city = string.Empty;
        string state = string.Empty;
        string county = string.Empty;
        string fips = string.Empty;
        bool found = false;
        int fipsLength = Global.FIPS_code.Length;
        if (fipsLength == 5)
        {
          if (!File.Exists(Counties_file))
          {
            Utils.fred_abort("counties file |%s| NOT FOUND\n", Counties_file);
          }

          using var fp = new StreamReader(Counties_file);
          while (fp.Peek() != -1)
          {
            line = fp.ReadLine();
            var tokens = line.Split('\t');
            city = tokens[0];
            state = tokens[1];
            county = tokens[2];
            fips = tokens[3];
            // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
            // city,state,county,fips);
            if (Global.FIPS_code == fips)
            {
              found = true;
              break;
            }
          }

          fp.Dispose();
          if (found)
          {
            Utils.FRED_STATUS(0, "FOUND a county = |{0} County {1}| for fips = |{2}|\n", county, state, fips);
            Global.Synthetic_population_id = $"{Global.Synthetic_population_version}_{fips}";
          }
          else
          {
            Utils.fred_abort("Sorry, could not find a county for fips = |%s|\n", Global.FIPS_code);
          }
        }
        else if (fipsLength == 2)
        {
          // get population_id from state
          found = false;
          string abbrev = string.Empty;
          if (!File.Exists(States_file))
          {
            Utils.fred_abort("states file |{0}| NOT FOUND\n", States_file);
          }
          using var fp = new StreamReader(States_file);
          while (fp.Peek() != -1)
          {
            line = fp.ReadLine();
            var tokens = line.Split('\t');
            fips = tokens[0];
            abbrev = tokens[1];
            state = tokens[2];
            if (Global.FIPS_code == fips)
            {
              found = true;
              break;
            }
          }
          fp.Dispose();
          if (found)
          {
            Utils.FRED_STATUS(0, "FOUND state = |%s| state_abbrev = |%s| fips = |%s|\n", state, abbrev, fips);
            Global.Synthetic_population_id = $"{Global.Synthetic_population_version}_{fips}";
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
      else if (Global.City != "none")
      {
        // city param overrides the synthetic_population_id
        // delete any commas and periods
        Global.City = Global.City.Replace(",", string.Empty).Replace(".", string.Empty);
        // replace white space characters with a single space
        Global.City = Utils.NormalizeWhiteSpace(Global.City);

        // get population_id from city
        string city_state = string.Empty;
        string line = string.Empty;
        string city = string.Empty;
        string state = string.Empty;
        string county = string.Empty;
        string fips = string.Empty;
        bool found = false;
        if (!File.Exists(Counties_file))
        {
          Utils.fred_abort("counties file |{0}| NOT FOUND\n", Counties_file);
        }

        using var fp = new StreamReader(Counties_file);
        while (fp.Peek() != -1)
        {
          line = fp.ReadLine();
          var tokens = line.Split('\t');
          city = tokens[0];
          state = tokens[1];
          county = tokens[2];
          fips = tokens[3];
          // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
          // city,state,county,fips);
          city_state = $"{city} {state}";
          if (Global.City == city_state)
          {
            found = true;
            break;
          }
        }

        fp.Dispose();
        if (found)
        {
          Utils.FRED_STATUS(0, "FOUND a county for city = |{0}| county = |{1} County {2]| and fips = |{3}|\n", Global.City, county,
              state, fips);
          Global.Synthetic_population_id = $"{Global.Synthetic_population_version}_{fips}";
        }
        else
        {
          Utils.fred_abort("Sorry, could not find a county for city = |%s|\n", Global.City);
        }
      }
      else if (Global.County != "none")
      {
        // county param overrides the synthetic_population_id
        // delete any commas and periods
        Global.County = Global.County.Replace(",", string.Empty).Replace(".", string.Empty);
        // replace white space characters with a single space
        Global.County = Utils.NormalizeWhiteSpace(Global.County);

        // get population_id from city
        string county_state = string.Empty;
        string line = string.Empty;
        string city = string.Empty;
        string state = string.Empty;
        string county = string.Empty;
        string fips = string.Empty;
        bool found = false;
        if (!File.Exists(Counties_file))
        {
          Utils.fred_abort("counties file |{0}| NOT FOUND\n", Counties_file);
        }

        using var fp = new StreamReader(Counties_file);
        while (fp.Peek() != -1)
        {
          line = fp.ReadLine();
          var tokens = line.Split('\t');
          city = tokens[0];
          state = tokens[1];
          county = tokens[2];
          fips = tokens[3];
          // printf("city = |%s| state = |%s| county = |%s| fips = |%s|\n",
          // city,state,county,fips);
          county_state = $"{city} County {state}";
          if (Global.County == county_state)
          {
            found = true;
            break;
          }
        }

        fp.Dispose();
        if (found)
        {
          Utils.FRED_STATUS(0, "FOUND county = |{0}| fips = |{1}|\n", county_state, fips);
          Global.Synthetic_population_id = $"{Global.Synthetic_population_version}_{fips}";
        }
        else
        {
          Utils.fred_abort("Sorry, could not find county called |%s|\n", Global.County);
        }
      }
      else if (Global.US_state != "none")
      {
        // state param overrides the synthetic_population_id
        // delete any commas and periods
        Global.US_state = Global.US_state.Replace(",", string.Empty).Replace(".", string.Empty);
        // replace white space characters with a single space
        Global.US_state = Utils.NormalizeWhiteSpace(Global.US_state);

        // get population_id from city
        string county_state = string.Empty;
        string line = string.Empty;
        string abbrev = string.Empty;
        string state = string.Empty;
        string fips = string.Empty;
        bool found = false;
        if (!File.Exists(States_file))
        {
          Utils.fred_abort("states file |{0}| NOT FOUND\n", States_file);
        }

        using var fp = new StreamReader(States_file);
        while (fp.Peek() != -1)
        {
          line = fp.ReadLine();
          var tokens = line.Split('\t');
          fips = tokens[0];
          abbrev = tokens[1];
          state = tokens[2];
          if (Global.US_state == abbrev || Global.US_state == state)
          {
            found = true;
            break;
          }
        }
        fp.Dispose();
        if (found)
        {
          Utils.FRED_STATUS(0, "FOUND state = |{0}| state_abbrev = |{1}| fips = |{2}|", state, abbrev, fips);
          Global.Synthetic_population_id = $"{Global.Synthetic_population_version}_{fips}";
        }
        else
        {
          Utils.fred_abort("Sorry, could not find state called |{0}|\n", Global.US_state);
        }
      }
    }

    public void read_all_places(List<string[]> Demes)
    {
      // clear the vectors
      this.workplaces.Clear();
      this.neighborhoods.Clear();

      this.households.Clear();
      this.schools.Clear();
      this.hospitals.Clear();
      this.counties.Clear();
      this.census_tracts.Clear();

      // store the number of demes as member variable
      set_number_of_demes(Demes.Count);

      // to compute the region's bounding box
      this.min_lat = this.min_lon = 999;
      this.max_lat = this.max_lon = -999;

      // initialize counts to zero
      this.place_type_counts[Place.TYPE_HOUSEHOLD] = 0;       // 'H'
      this.place_type_counts[Place.TYPE_SCHOOL] = 0;          // 'S'
      this.place_type_counts[Place.TYPE_WORKPLACE] = 0;       // 'W'
      this.place_type_counts[Place.TYPE_HOSPITAL] = 0;        // 'M'
      this.place_type_counts[Place.TYPE_NEIGHBORHOOD] = 0;    // 'N'
      this.place_type_counts[Place.TYPE_CLASSROOM] = 0;       // 'C'
      this.place_type_counts[Place.TYPE_OFFICE] = 0;          // 'O'
      this.place_type_counts[Place.TYPE_COMMUNITY] = 0;       // 'X'

      // vector to hold init data
      var pids = new List<Place_Init_Data>();

      // only one population directory allowed
      string pop_dir = Global.Synthetic_population_directory;

      // need to have at least one deme
      Utils.assert(Demes.Count > 0);
      Utils.assert(Demes.Count <= char.MaxValue);

      // and each deme must contain at least one synthetic population id
      for (int d = 0; d < Demes.Count; ++d)
      {
        Utils.FRED_STATUS(0, "Reading Places for Deme %d:\n", d);
        Utils.assert(Demes[d].Length > 0);
        for (int i = 0; i < Demes[d].Length; ++i)
        {
          // o---------------------------------------- Call read_places to actually
          // |                                         read the population files
          // V
          read_places(pop_dir, Demes[d][i], char.ConvertFromUtf32(d)[0], pids);
        }
      }

      for (int i = 0; i < this.counties.Count; ++i)
      {
        int fips = this.counties[i].get_fips();
        Utils.FRED_VERBOSE(0, "COUNTIES[%d] = %d\n", i, fips);
      }
      for (int i = 0; i < this.census_tracts.Count; ++i)
      {
        Utils.FRED_VERBOSE(1, "CENSUS_TRACTS[%d] = %ld\n", i, this.census_tracts[i]);
      }
      //// HOUSEHOLD in-place allocator
      //Place.Allocator<Household> household_allocator;
      //household_allocator.reserve(this.place_type_counts[Place.TYPE_HOUSEHOLD]);
      //// SCHOOL in-place allocator
      //Place.Allocator<School> school_allocator;
      //school_allocator.reserve(this.place_type_counts[Place.TYPE_SCHOOL]);
      //// WORKPLACE in-place allocator
      //Place.Allocator<Workplace> workplace_allocator;
      //workplace_allocator.reserve(this.place_type_counts[Place.TYPE_WORKPLACE]);
      //// HOSPITAL in-place allocator
      //Place.Allocator<Hospital> hospital_allocator;
      //hospital_allocator.reserve(this.place_type_counts[Place.TYPE_HOSPITAL]);

      // fred-specific place types initialized elsewhere (setup_offices, setup_classrooms)

      // more temporaries
      Place place = null;

      // loop through sorted init data and create objects using Place_Allocator
      int a = 0;
      foreach (var pid in pids)
      {
        var s = pid.s;
        var place_type = pid.place_type;
        var place_subtype = pid.place_subtype;
        var lon = pid.lon;
        var lat = pid.lat;

        if (place_type == Place.TYPE_HOUSEHOLD && lat != 0.0)
        {
          if (lat < this.min_lat)
          {
            this.min_lat = lat;
          }
          if (this.max_lat < lat)
          {
            this.max_lat = lat;
          }
        }
        if (place_type == Place.TYPE_HOUSEHOLD && lon != 0.0)
        {
          if (lon < this.min_lon)
          {
            this.min_lon = lon;
          }
          if (this.max_lon < lon)
          {
            this.max_lon = lon;
          }
        }
        if (place_type == Place.TYPE_HOUSEHOLD)
        {
          var h = new Household(s, place_subtype, lon, lat);
          place = h;
          place.set_household_fips(this.counties[pid.county].get_fips());  //resid_imm
          // ensure that household income is non-negative
          h.set_household_income(pid.income > 0 ? pid.income : 0);
          h.set_deme_id(pid.deme_id);
          if (pid.is_group_quarters)
          {
            h.set_group_quarters_units(pid.group_quarters_units);
            h.set_group_quarters_workplace(get_place_from_label(pid.gq_workplace));
          }
          h.set_county_index(pid.county);
          h.set_census_tract_index(pid.census_tract_index);
          h.set_shelter(false);
          this.households.Add(h);
          //FRED_VERBOSE(9, "pushing household %s\n", s);
          this.counties[pid.county].add_household(h);
          if (Global.Enable_Visualization_Layer)
          {
            long census_tract = this.get_census_tract_with_index(pid.census_tract_index);
            Global.Visualization.add_census_tract(census_tract);
          }
        }
        else if (place_type == Place.TYPE_SCHOOL)
        {
          place = new School(s, place_subtype, lon, lat);
          ((School)place).set_county_index(pid.county);
        }
        else if (place_type == Place.TYPE_WORKPLACE)
        {
          place = new Workplace(s, place_subtype, lon, lat);
        }
        else if (place_type == Place.TYPE_HOSPITAL)
        {
          var hosp = new Hospital(s, place_subtype, lon, lat);
          place = hosp;
          int bed_count = Convert.ToInt32((pid.num_workers_assigned
              / Hospital_worker_to_bed_ratio) + 1.0);
          hosp.set_bed_count(bed_count);
          if (hosp.get_daily_patient_capacity(0) == -1)
          {
            int capacity = Convert.ToInt32(pid.num_workers_assigned * Hospital_outpatients_per_day_per_employee);
            hosp.set_daily_patient_capacity(capacity);
          }
          if (hosp.get_subtype() != Place.SUBTYPE_MOBILE_HEALTHCARE_CLINIC)
          {
            if (bed_count < Hospital_min_bed_threshold)
            { // This place is not have enough "bed" to be considered for overnight
              hosp.set_subtype(Place.SUBTYPE_HEALTHCARE_CLINIC);
            }
            else
            { // This place is a hospital that allows overnight stays, so add in bed count for capacity
              int capacity = hosp.get_daily_patient_capacity(0);
              capacity += hosp.get_bed_count(0);
              hosp.set_daily_patient_capacity(capacity);
            }
            Hospital_overall_panel_size += hosp.get_daily_patient_capacity(0);
          }
        }
        else
        {
          Utils.fred_abort("Help! bad place_type %c\n", place_type);
        }

        if (place == null)
        {
          Utils.fred_abort("Help! allocation failure for the %dth entry in location file (s=%s, type=%c)\n", a, s, place_type);
        }
        place = null;
        a++;
      }

      // since everything was allocated in contiguous blocks, we can use pointer arithmetic
      // call to add_preallocated_places also ensures that all allocations were used for
      // successful additions to the place list
      //add_preallocated_places<Household>(Place.TYPE_HOUSEHOLD, household_allocator);
      //add_preallocated_places<School>(Place.TYPE_SCHOOL, school_allocator);
      //add_preallocated_places<Workplace>(Place.TYPE_WORKPLACE, workplace_allocator);
      //add_preallocated_places<Hospital>(Place.TYPE_HOSPITAL, hospital_allocator);

      Utils.FRED_STATUS(0, "finished reading %d locations, now creating additional FRED locations\n", next_place_id);

      if (Global.Use_Mean_Latitude)
      {
        // Make projection based on the location file.
        var mean_lat = (min_lat + max_lat) / 2.0;
        Geo.set_km_per_degree(mean_lat);
        Utils.FRED_STATUS(0, "min_lat: %f  max_lat: %f  mean_lat: %f\n", min_lat, max_lat, mean_lat);
      }
      else
      {
        // DEFAULT: Use mean US latitude (see Geo.cc)
        Utils.FRED_STATUS(0, "min_lat: %f  max_lat: %f\n", min_lat, max_lat);
      }

      // create geographical grids
      Global.Simulation_Region = new Regional_Layer(min_lon, min_lat, max_lon, max_lat);

      // Initialize global seasonality object
      if (Global.Enable_Seasonality)
      {
        Global.Clim = new Seasonality(Global.Simulation_Region);
      }

      // layer containing neighborhoods
      Global.Neighborhoods = new Neighborhood_Layer();

      // add households to the Neighborhoods Layer
      for (int i = 0; i < this.households.Count; ++i)
      {
        var h = this.get_household_ptr(i);
        int row = Global.Neighborhoods.get_row(h.get_latitude());
        int col = Global.Neighborhoods.get_col(h.get_longitude());
        var patch = Global.Neighborhoods.get_patch(row, col);
        Utils.FRED_CONDITIONAL_VERBOSE(0, patch == null, "Help: household %d has bad patch,  lat = %f  lon = %f\n", h.get_id(),
            h.get_latitude(), h.get_longitude());
        Utils.assert(patch != null);
        patch.add_household(h);
        h.set_patch(patch);
      }

      //int number_of_neighborhoods = Global.Neighborhoods.get_number_of_neighborhoods();
      //// create allocator for neighborhoods
      //Place.Allocator<Neighborhood> neighborhood_allocator;
      //// reserve enough space for all neighborhoods
      //neighborhood_allocator.reserve(number_of_neighborhoods);
      //Utils.FRED_STATUS(0, "Allocated space for %7d neighborhoods\n", number_of_neighborhoods);
      //// pass allocator to Neighborhood_Layer.setup (which then passes to Neighborhood_Patch.make_neighborhood)
      //Global.Neighborhoods.setup(neighborhood_allocator);
      //// add Neighborhoods in one contiguous block
      //add_preallocated_places<Neighborhood>(Place.TYPE_NEIGHBORHOOD, neighborhood_allocator);

      int number_places = places.Count;
      for (int p = 0; p < number_places; ++p)
      {
        // add workplaces to the regional layer (needed for teacher assignments to schools)
        if (places[p].get_type() == Place.TYPE_WORKPLACE)
        {
          Global.Simulation_Region.add_workplace(places[p]);
        }
      }

      this.load_completed = true;
      Utils.FRED_STATUS(0, "read places finished: Places = {0}", places.Count);
    }

    public void read_places(string pop_dir, string pop_id, char deme_id, List<Place_Init_Data> pids)
    {
      Utils.FRED_STATUS(0, "read places entered");
      string temp_file;
      var scratchRamDisk = Environment.GetEnvironmentVariable("SCRATCH_RAMDISK");
      if (!string.IsNullOrWhiteSpace(scratchRamDisk))
      {
        temp_file = $"{scratchRamDisk}/temp_file-{Process.GetCurrentProcess().Id}-{Global.Simulation_run_number}";
      }
      else
      {
        temp_file = $"./temp_file-{Process.GetCurrentProcess().Id}-{Global.Simulation_run_number}";
      }

      // record the actual synthetic population in the log file
      Utils.FRED_STATUS(0, "POPULATION_FILE: %s/%s\n", pop_dir, pop_id);

      // read household locations
      string location_file = $"{pop_dir}/{pop_id}/{pop_id}_synth_households.txt";
      if (Enable_copy_files != 0)
      {
        if (File.Exists(temp_file))
        {
          File.Delete(temp_file);
        }

        File.Copy(location_file, temp_file);
        location_file = temp_file;
      }
      read_household_file(deme_id, location_file, pids);
      //Utils.fred_print_lap_time("Places.read_household_file");

      // log county info
      Utils.FRED_STATUS(0, "COUNTIES AFTER READING HOUSEHOLDS\n");
      for (int i = 0; i < this.counties.Count; ++i)
      {
        Utils.FRED_STATUS(0, "COUNTIES[%d] = %d\n", i, this.counties[i].get_fips());
      }

      // read workplace locations
      location_file = $"{pop_dir}/{pop_id}/{pop_id}_workplaces.txt";
      read_workplace_file(deme_id, location_file, pids);

      // read school locations
      location_file = $"{pop_dir}/{pop_id}/{pop_id}_schools.txt";
      read_school_file(deme_id, location_file, pids);

      // log county info
      Utils.FRED_STATUS(0, "COUNTIES AFTER READING SCHOOLS\n");
      for (int i = 0; i < this.counties.Count; i++)
      {
        Utils.FRED_STATUS(0, "COUNTIES[%d] = %d\n", i, this.counties[i].get_fips());
      }

      // read hospital locations
      if (Global.Enable_Hospitals)
      {
        location_file = $"{pop_dir}/{pop_id}/{pop_id}_hospitals.txt";
        read_hospital_file(deme_id, location_file, pids);
      }

      if (Global.Enable_Group_Quarters)
      {
        // read group quarters locations (a new workplace and household is created 
        // for each group quarters)
        location_file = $"{pop_dir}/{pop_id}/{pop_id}_synth_gq.txt";
        read_group_quarters_file(deme_id, location_file, pids);
      }
      //Utils.fred_print_lap_time("Places.read_group_quarters_file");

      // log county info
      Utils.FRED_STATUS(0, "COUNTIES AFTER READING GQ\n");
      for (int i = 0; i < this.counties.Count; ++i)
      {
        Utils.FRED_STATUS(0, "COUNTIES[%d] = %d\n", i, this.counties[i].get_fips());
      }
    }

    public void reassign_workers()
    {
      if (Global.Assign_Teachers)
      {
        //from: http://www.statemaster.com/graph/edu_ele_sec_pup_rat-elementary-secondary-pupil-teacher-ratio
        reassign_workers_to_places_of_type(Place.TYPE_SCHOOL, School_fixed_staff,
            School_student_teacher_ratio);
      }

      if (Global.Enable_Hospitals)
      {
        reassign_workers_to_places_of_type(Place.TYPE_HOSPITAL, Hospital_fixed_staff,
            1.0 / Hospital_worker_to_bed_ratio);
      }

      if (Global.Enable_Group_Quarters)
      {
        reassign_workers_to_group_quarters(Place.SUBTYPE_COLLEGE, College_fixed_staff,
            College_resident_to_staff_ratio);
        reassign_workers_to_group_quarters(Place.SUBTYPE_PRISON, Prison_fixed_staff,
            Prison_resident_to_staff_ratio);
        reassign_workers_to_group_quarters(Place.SUBTYPE_MILITARY_BASE, Military_fixed_staff,
            Military_resident_to_staff_ratio);
        reassign_workers_to_group_quarters(Place.SUBTYPE_NURSING_HOME, Nursing_home_fixed_staff,
            Nursing_home_resident_to_staff_ratio);
      }
    }

    public void prepare()
    {
      Utils.FRED_STATUS(0, "prepare places entered\n", "");
      int number_places = places.Count;
      for (int p = 0; p < number_places; ++p)
      {
        this.places[p].prepare();
      }
      Global.Neighborhoods.prepare();

      int number_of_schools = this.schools.Count;
      for (int p = 0; p < number_of_schools; ++p)
      {
        var school = get_school_ptr(p);

        // add school to lists of school by grade
        for (int grade = 0; grade < Neighborhood_Patch.GRADES; ++grade)
        {
          if (school.get_orig_students_in_grade(grade) > 0)
          {
            this.schools_by_grade[grade].Add(get_school(p));
          }
        }
      }


      if (Global.Verbose > 1)
      {
        // check the schools by grade lists
        Console.WriteLine();
        for (int grade = 0; grade < Neighborhood_Patch.GRADES; ++grade)
        {
          int size = this.schools_by_grade[grade].Count;
          Console.Write("GRADE = {0} SCHOOLS = {1}: ", grade, size);
          for (int i = 0; i < size; ++i)
          {
            Console.Write("{1} ", this.schools_by_grade[grade][i].get_label());
          }
          Console.WriteLine();
        }
        Console.WriteLine();
      }
      if (Global.Verbose > 0)
      {
        print_status_of_schools(0);
      }
    }

    public void print_status_of_schools(int day)
    {
      var students_per_grade = new int[Neighborhood_Patch.GRADES];
      for (int i = 0; i < Neighborhood_Patch.GRADES; ++i)
      {
        students_per_grade[i] = 0;
      }

      int number_of_schools = this.schools.Count;
      for (int p = 0; p < number_of_schools; ++p)
      {
        var school = get_school_ptr(p);
        for (int grade = 0; grade < Neighborhood_Patch.GRADES; ++grade)
        {
          int total = school.get_orig_number_of_students();
          int orig = school.get_orig_students_in_grade(grade);
          int now = school.get_students_in_grade(grade);
          students_per_grade[grade] += now;
          if (false && total > 1500 && orig > 0)
          {
            Console.WriteLine("{0} GRADE {1} ORIG {2} NOW {3} DIFF {4}", school.get_label(), grade,
                   school.get_orig_students_in_grade(grade),
                   school.get_students_in_grade(grade),
                   school.get_students_in_grade(grade)
                     - school.get_orig_students_in_grade(grade));
          }
        }
      }

      int year = day / 365;
      // char filename[256];
      // sprintf(filename, "students.%d", year);
      // FILE *fp = fopen(filename,"w");
      int total_students = 0;
      for (int i = 0; i < Neighborhood_Patch.GRADES; ++i)
      {
        // fprintf(fp, "%d %d\n", i,students_per_grade[i]);
        Console.WriteLine("YEAR {0{ GRADE {1} STUDENTS {2}", year, i, students_per_grade[i]);
        total_students += students_per_grade[i];
      }
      // fclose(fp);
      Console.WriteLine("YEAR {0} TOTAL_STUDENTS {1}", year, total_students);
    }

    public void update(int day)
    {
      Utils.FRED_STATUS(1, "update places entered\n", "");

      if (Global.Enable_Seasonality)
      {
        Global.Clim.update(day);
      }

      if (Global.Enable_Vector_Transmission)
      {
        int number_places = this.places.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var place = this.places[p];
          place.update_vector_population(day);
        }
      }

      if (Global.Enable_HAZEL)
      {
        int number_places = this.places.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var place = this.places[p];
          if (place.is_hospital())
          {
            var temp_hosp = (Hospital)place;
            temp_hosp.reset_current_daily_patient_count();
          }

          if (place.is_household())
          {
            var temp_hh = (Household)place;
            temp_hh.reset_healthcare_info();
          }
        }
      }

      Utils.FRED_STATUS(1, "update places finished\n", "");
    }

    public void quality_control()
    {
      //Can't do the quality control until all of the population files have been read
      Utils.assert(Global.Pop.is_load_completed());
      int number_places = this.places.Count;
      Utils.FRED_STATUS(0, "places quality control check for %d places\n", number_places);
      if (Global.Verbose != 0)
      {
        int hn = 0;
        int nn = 0;
        int sn = 0;
        int wn = 0;
        double hsize = 0.0;
        double nsize = 0.0;
        double ssize = 0.0;
        double wsize = 0.0;
        // mean size by place type
        for (int p = 0; p < number_places; ++p)
        {
          int n = this.places[p].get_size();
          if (this.places[p].is_household())
          {
            hn++;
            hsize += n;
          }
          if (this.places[p].get_type() == Place.TYPE_NEIGHBORHOOD)
          {
            nn++;
            nsize += n;
          }
          if (this.places[p].get_type() == Place.TYPE_SCHOOL)
          {
            sn++;
            ssize += n;
          }
          if (this.places[p].get_type() == Place.TYPE_WORKPLACE)
          {
            wn++;
            wsize += n;
          }
        }
        if (hn != 0)
        {
          hsize /= hn;
        }
        if (nn != 0)
        {
          nsize /= nn;
        }
        if (sn != 0)
        {
          ssize /= sn;
        }
        if (wn != 0)
        {
          wsize /= wn;
        }
        Utils.FRED_STATUS(0, "\nMEAN PLACE SIZE: H %.2f N %.2f S %.2f W %.2f\n",
          hsize, nsize, ssize, wsize);
      }

      if (Global.Verbose > 1)
      {
        using var fp = new StreamWriter($"{Global.Simulation_directory}/houses.dat");
        for (int p = 0; p < number_places; p++)
        {
          if (this.places[p].is_household())
          {
            var h = this.places[p];
            double x = Geo.get_x(h.get_longitude());
            double y = Geo.get_y(h.get_latitude());
            fp.WriteLine("{0} {1}", x, y);
          }
        }
        fp.Flush();
        fp.Dispose();
      }

      if (Global.Verbose != 0)
      {
        var count = new int[20];
        int total = 0;
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            int n = this.places[p].get_size();
            if (n < 15)
            {
              count[n]++;
            }
            else
            {
              count[14]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nHousehold size distribution: %d households\n", total);
        for (int c = 0; c < 15; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[20];
        int total = 0;
        // adult distribution of households
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            int n = h.get_adults();
            if (n < 15)
            {
              count[n]++;
            }
            else
            {
              count[14]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nHousehold adult size distribution: %d households\n", total);
        for (int c = 0; c < 15; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[20];
        int total = 0;
        // children distribution of households
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            int n = h.get_children();
            if (n < 15)
            {
              count[n]++;
            }
            else
            {
              count[14]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nHousehold children size distribution: %d households\n", total);
        for (int c = 0; c < 15; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[20];
        int total = 0;
        // adult distribution of households with children
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            if (h.get_children() == 0)
            {
              continue;
            }
            int n = h.get_adults();
            if (n < 15)
            {
              count[n]++;
            }
            else
            {
              count[14]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nHousehold w/ children, adult size distribution: %d households\n", total);
        for (int c = 0; c < 15; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      // relationship between children and decision maker
      if (Global.Verbose > 1 && Global.Enable_Behaviors)
      {
        // find adult decision maker for each child
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            if (h.get_children() == 0)
            {
              continue;
            }
            int size = h.get_size();
            for (int i = 0; i < size; ++i)
            {
              var child = h.get_enrollee(i);
              int ch_age = child.get_age();
              if (ch_age < 18)
              {
                int ch_rel = child.get_relationship();
                var dm = child.get_health_decision_maker();
                if (dm == null)
                {
                  Console.WriteLine("DECISION_MAKER: household {0} {1}  child: {2} {3} is making own health decisions",
                   h.get_id(), h.get_label(), ch_age, ch_rel);
                }
                else
                {
                  int dm_age = dm.get_age();
                  int dm_rel = dm.get_relationship();
                  if (dm_rel != 1 || ch_rel != 3)
                  {
                    Console.WriteLine("DECISION_MAKER: household {0} {1}  decision_maker: {2} {3} child: {4} {5}",
                    h.get_id(), h.get_label(), dm_age, dm_rel, ch_age, ch_rel);
                  }
                }
              }
            }
          }
        }
      }

      if (Global.Verbose != 0)
      {
        var count = new int[100];
        int total = 0;
        // age distribution of heads of households
        for (int p = 0; p < number_places; ++p)
        {
          Person per = null;
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            for (int i = 0; i < h.get_size(); ++i)
            {
              if (h.get_enrollee(i).is_householder())
              {
                per = h.get_enrollee(i);
              }
            }
            if (per == null)
            {
              Utils.FRED_STATUS(0, "Help! No head of household found for household id %d label %s size %d groupquarters: %d\n",
               h.get_id(), h.get_label(), h.get_size(), h.is_group_quarters() ? 1 : 0);
              count[0]++;
            }
            else
            {
              int a = per.get_age();
              if (a < 100)
              {
                count[a]++;
              }
              else
              {
                count[99]++;
              }
              total++;
            }
          }
        }
        Utils.FRED_STATUS(0, "\nAge distribution of heads of households: %d households\n", total);
        for (int c = 0; c < 100; ++c)
        {
          Utils.FRED_STATUS(0, "age %2d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[100];
        int total = 0;
        int children = 0;
        // age distribution of heads of households with children
        for (int p = 0; p < number_places; ++p)
        {
          Person per = null;
          if (this.places[p].is_household())
          {
            var h = (Household)(this.places[p]);
            if (h.get_children() == 0)
            {
              continue;
            }
            children += h.get_children();
            for (int i = 0; i < h.get_size(); ++i)
            {
              if (h.get_enrollee(i).is_householder())
              {
                per = h.get_enrollee(i);
              }
            }
            if (per == null)
            {
              Utils.FRED_STATUS(0, "Help! No head of household found for household id %d label %s groupquarters: %d\n",
               h.get_id(), h.get_label(), h.is_group_quarters() ? 1 : 0);
              count[0]++;
            }
            else
            {
              int a = per.get_age();
              if (a < 100)
              {
                count[a]++;
              }
              else
              {
                count[99]++;
              }
              total++;
            }
          }
        }
        Utils.FRED_STATUS(0, "\nAge distribution of heads of households with children: %d households\n", total);
        for (int c = 0; c < 100; ++c)
        {
          Utils.FRED_STATUS(0, "age %2d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "children = %d\n", children);
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        int count_has_school_age = 0;
        int count_has_school_age_and_unemployed_adult = 0;
        int total_hh = 0;

        //Households with school-age children and at least one unemployed adult
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            total_hh++;
            var h = (Household)(this.places[p]);
            if (h.get_children() == 0)
            {
              continue;
            }
            if (h.has_school_aged_child())
            {
              count_has_school_age++;
            }
            if (h.has_school_aged_child_and_unemployed_adult())
            {
              count_has_school_age_and_unemployed_adult++;
            }
          }
        }
        Utils.FRED_STATUS(0, "\nHouseholds with school-aged children and at least one unemployed adult\n");
        Utils.FRED_STATUS(0, "Total Households: %d\n", total_hh);
        Utils.FRED_STATUS(0, "Total Households with school-age children: %d\n", count_has_school_age);
        Utils.FRED_STATUS(0, "Total Households with school-age children and at least one unemployed adult: %d\n", count_has_school_age_and_unemployed_adult);
      }

      if (Global.Verbose != 0)
      {
        var count = new int[100];
        int total = 0;
        // size distribution of schools
        for (int c = 0; c < 20; ++c)
        {
          count[c] = 0;
        }
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].get_type() == Place.TYPE_SCHOOL)
          {
            int s = this.places[p].get_size();
            int n = s / 50;
            if (n < 20)
            {
              count[n]++;
            }
            else
            {
              count[19]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nSchool size distribution: %d schools\n", total);
        for (int c = 0; c < 20; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            (c + 1) * 50, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        // age distribution in schools
        Utils.FRED_STATUS(0, "\nSchool age distribution:\n");
        var count = new int[100];
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].get_type() == Place.TYPE_SCHOOL)
          {
            // places[p].print(0);
            for (int c = 0; c < 100; ++c)
            {
              count[c] += ((School)(this.places[p])).get_students_in_grade(c);
            }
          }
        }
        for (int c = 0; c < 100; ++c)
        {
          Utils.FRED_STATUS(0, "age = %2d  students = %6d\n",
            c, count[c]); ;
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[50];
        int total = 0;
        // size distribution of classrooms
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].get_type() == Place.TYPE_CLASSROOM)
          {
            int s = this.places[p].get_size();
            int n = s;
            if (n < 50)
            {
              count[n]++;
            }
            else
            {
              count[50 - 1]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nClassroom size distribution: %d classrooms\n", total);
        for (int c = 0; c < 50; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      if (Global.Verbose != 0)
      {
        var count = new int[101];
        int small_employees = 0;
        int med_employees = 0;
        int large_employees = 0;
        int xlarge_employees = 0;
        int total_employees = 0;
        int total = 0;
        // size distribution of workplaces
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].get_type() == Place.TYPE_WORKPLACE || places[p].get_type() == Place.TYPE_SCHOOL)
          {
            int s = this.places[p].get_size();
            if (this.places[p].get_type() == Place.TYPE_SCHOOL)
            {
              var school = (School)(this.places[p]);
              s = school.get_staff_size();
            }
            int n = s;
            if (n <= 100)
            {
              count[n]++;
            }
            else
            {
              count[100]++;
            }
            total++;
            if (s < 50)
            {
              small_employees += s;
            }
            else if (s < 100)
            {
              med_employees += s;
            }
            else if (s < 500)
            {
              large_employees += s;
            }
            else
            {
              xlarge_employees += s;
            }
            total_employees += s;
          }
        }
        Utils.FRED_STATUS(0, "\nWorkplace size distribution: %d workplaces\n", total);
        for (int c = 0; c <= 100; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            (c + 1) * 1, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n\n");

        Utils.FRED_STATUS(0, "employees at small workplaces (1-49): ");
        Utils.FRED_STATUS(0, "%d\n", small_employees);

        Utils.FRED_STATUS(0, "employees at medium workplaces (50-99): ");
        Utils.FRED_STATUS(0, "%d\n", med_employees);

        Utils.FRED_STATUS(0, "employees at large workplaces (100-499): ");
        Utils.FRED_STATUS(0, "%d\n", large_employees);

        Utils.FRED_STATUS(0, "employees at xlarge workplaces (500-up): ");
        Utils.FRED_STATUS(0, "%d\n", xlarge_employees);

        Utils.FRED_STATUS(0, "total employees: %d\n\n", total_employees);
      }

      /*
        if(Global.Verbose) {
        int covered[4];
        int all[4];
        // distribution of sick leave in workplaces
        for(int c = 0; c < 4; ++c) { all[c] = covered[c] = 0; }
        for(int p = 0; p < number_places; ++p) {
        if(this.places[p].get_type() == WORKPLACE) {
        Workplace* work = (Workplace)(this.places[p]);
        char s = work.get_size_code();
        bool sl = work.is_sick_leave_available();
        switch(s) {
        case 'S':
        all[0] += s;
        if(sl) {
        covered[0] += s;
        }
        break;
        case 'M':
        all[1] += s;
        if(sl) {
        covered[1] += s;
        }
        break;
        case 'L':
        all[2] += s;
        if(sl) {
        covered[2] += s;
        }
        break;
        case 'X':
        all[3] += s;
        if(sl) {
        covered[3] += s;
        }
        break;
        }
        }
        }
        Utils.FRED_STATUS(0, "\nWorkplace sick leave coverage: ");
        for(int c = 0; c < 4; ++c) {
        Utils.FRED_STATUS(0, "%3d: %d/%d %5.2f | ",
        c, covered[c], all[c], (all[c]? (1.0*covered[c])/all[c] : 0));
        }
        Utils.FRED_STATUS(0, "\n");
        }
      */

      if (Global.Verbose != 0)
      {
        var count = new int[60];
        int total = 0;
        // size distribution of offices
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].get_type() == Place.TYPE_OFFICE)
          {
            int s = this.places[p].get_size();
            int n = s;
            if (n < 60)
            {
              count[n]++;
            }
            else
            {
              count[60 - 1]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "\nOffice size distribution: %d offices\n", total);
        for (int c = 0; c < 60; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");
      }
      if (Global.Verbose != 0)
      {
        Utils.FRED_STATUS(0, "places quality control finished\n");
      }
    }

    public void report_school_distributions(int day)
    {
      int number_places = this.places.Count;
      // original size distribution
      var count = new int[21];
      var osize = new int[21];
      var nsize = new int[21];
      // size distribution of schools
      for (int c = 0; c <= 20; ++c)
      {
        count[c] = 0;
        osize[c] = 0;
        nsize[c] = 0;
      }
      for (int p = 0; p < number_places; ++p)
      {
        if (this.places[p].get_type() == Place.TYPE_SCHOOL)
        {
          int os = this.places[p].get_orig_size();
          int ns = this.places[p].get_size();
          int n = os / 50;
          if (n > 20)
          {
            n = 20;
          }
          count[n]++;
          osize[n] += os;
          nsize[n] += ns;
        }
      }
      Utils.FRED_STATUS(0,"SCHOOL SIZE distribution: ");
      for (int c = 0; c <= 20; ++c)
      {
        Utils.FRED_STATUS(0,"%d %d %0.2f %0.2f | ",
          c, count[c], count[c] != 0 ? (1.0 * osize[c]) / (1.0 * count[c]) : 0, count[c] != 0 ? (1.0 * nsize[c]) / (1.0 * count[c]) : 0);
      }
      Utils.FRED_STATUS(0,"\n");

      //return;

      //int year = day / 365;
      //char filename[FRED_STRING_SIZE];
      //FILE* fp = NULL;

      //sprintf(filename, "%s/school-%d.txt", Global.Simulation_directory, year);
      //fp = fopen(filename, "w");
      //for (int p = 0; p < number_places; ++p)
      //{
      //  if (this.places[p].get_type() == Place.TYPE_SCHOOL)
      //  {
      //    Place* h = this.places[p];
      //    fprintf(fp, "%s orig_size %d current_size %d\n", h.get_label(), h.get_orig_size() - h.get_staff_size(), h.get_size() - h.get_staff_size());
      //    /*
      //School * s = static_cast<School*>(h);
      //for(int a = 1; a <=20; ++a) {
      //if(s.get_orig_students_in_grade(a) > 0) {
      //fprintf(fp, "SCHOOL %s age %d orig %d current %d\n",
      //s.get_label(), a, s.get_orig_students_in_grade(a), s.get_students_in_grade(a));
      //}
      //}
      //fprintf(fp,"\n");
      //    */
      //  }
      //}
      //fclose(fp);

      //sprintf(filename, "%s/work-%d.txt", Global.Simulation_directory, year);
      //fp = fopen(filename, "w");
      //for (int p = 0; p < number_places; ++p)
      //{
      //  if (this.places[p].get_type() == Place.TYPE_WORKPLACE)
      //  {
      //    Place* h = this.places[p];
      //    fprintf(fp, "%s orig_size %d current_size %d\n", h.get_label(), h.get_orig_size(), h.get_size());
      //  }
      //}
      //fclose(fp);

      //sprintf(filename, "%s/house-%d.txt", Global.Simulation_directory, year);
      //fp = fopen(filename, "w");
      //for (int p = 0; p < number_places; ++p)
      //{
      //  if (this.places[p].is_household())
      //  {
      //    Place* h = this.places[p];
      //    fprintf(fp, "%s orig_size %d current_size %d\n", h.get_label(), h.get_orig_size(), h.get_size());
      //  }
      //}
      //fclose(fp);
    }

    public void report_household_distributions()
    {
      int number_places = this.places.Count;
      if (Global.Verbose != 0)
      {
        var count = new int[20];
        int total = 0;
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            int n = this.places[p].get_size();
            if (n <= 10)
            {
              count[n]++;
            }
            else
            {
              count[10]++;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "Household size distribution: N = %d ", total);
        for (int c = 0; c <= 10; ++c)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%) ",
            c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");

        // original size distribution
        var hsize = new int[20];
        total = 0;
        // size distribution of households
        for (int c = 0; c <= 10; ++c)
        {
          count[c] = 0;
          hsize[c] = 0;
        }
        for (int p = 0; p < number_places; ++p)
        {
          if (this.places[p].is_household())
          {
            int n = this.places[p].get_orig_size();
            int hs = this.places[p].get_size();
            if (n <= 10)
            {
              count[n]++;
              hsize[n] += hs;
            }
            else
            {
              count[10]++;
              hsize[10] += hs;
            }
            total++;
          }
        }
        Utils.FRED_STATUS(0, "Household orig distribution: N = %d ", total);
        for (int c = 0; c <= 10; c++)
        {
          Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%) %0.2f ",
            c, count[c], (100.0 * count[c]) / total, count[c] != 0 ? ((double)hsize[c] / (double)count[c]) : 0.0);
        }
        Utils.FRED_STATUS(0, "\n");
      }

      //return;

      //if (Global.Verbose)
      //{
      //  int count[100];
      //  int total = 0;
      //  // age distribution of heads of households
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    Person* per = NULL;
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      for (int i = 0; i < h.get_size(); ++i)
      //      {
      //        if (h.get_enrollee(i).is_householder())
      //        {
      //          per = h.get_enrollee(i);
      //        }
      //      }
      //      if (per == NULL)
      //      {
      //        FRED_WARNING("Help! No head of household found for household id %d label %s groupquarters: %d\n",
      //         h.get_id(), h.get_label(), h.is_group_quarters() ? 1 : 0);
      //        count[0]++;
      //      }
      //      else
      //      {
      //        int a = per.get_age();
      //        if (a < 100)
      //        {
      //          count[a]++;
      //        }
      //        else
      //        {
      //          count[99]++;
      //        }
      //        total++;
      //      }
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nAge distribution of heads of households: %d households\n", total);
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    Utils.FRED_STATUS(0, "age %2d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //if (Global.Verbose)
      //{
      //  int count[20];
      //  int total = 0;
      //  // adult distribution of households
      //  for (int c = 0; c < 15; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(places[p]);
      //      int n = h.get_adults();
      //      if (n < 15)
      //      {
      //        count[n]++;
      //      }
      //      else
      //      {
      //        count[14]++;
      //      }
      //      total++;
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nHousehold adult size distribution: %d households\n", total);
      //  for (int c = 0; c < 15; ++c)
      //  {
      //    Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //if (Global.Verbose)
      //{
      //  int count[20];
      //  int total = 0;
      //  // children distribution of households
      //  for (int c = 0; c < 15; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      int n = h.get_children();
      //      if (n < 15)
      //      {
      //        count[n]++;
      //      }
      //      else
      //      {
      //        count[14]++;
      //      }
      //      total++;
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nHousehold children size distribution: %d households\n", total);
      //  for (int c = 0; c < 15; c++)
      //  {
      //    Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //if (Global.Verbose)
      //{
      //  int count[20];
      //  int total = 0;
      //  // adult distribution of households with children
      //  for (int c = 0; c < 15; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(places[p]);
      //      if (h.get_children() == 0)
      //      {
      //        continue;
      //      }
      //      int n = h.get_adults();
      //      if (n < 15)
      //      {
      //        count[n]++;
      //      }
      //      else
      //      {
      //        count[14]++;
      //      }
      //      total++;
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nHousehold w/ children, adult size distribution: %d households\n", total);
      //  for (int c = 0; c < 15; ++c)
      //  {
      //    Utils.FRED_STATUS(0, "%3d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //// relationship between children and decision maker
      //if (Global.Verbose > 1 && Global.Enable_Behaviors)
      //{
      //  // find adult decision maker for each child
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      if (h.get_children() == 0)
      //      {
      //        continue;
      //      }
      //      int size = h.get_size();
      //      for (int i = 0; i < size; ++i)
      //      {
      //        Person* child = h.get_enrollee(i);
      //        int ch_age = child.get_age();
      //        if (ch_age < 18)
      //        {
      //          int ch_rel = child.get_relationship();
      //          Person* dm = child.get_health_decision_maker();
      //          if (dm == NULL)
      //          {
      //            printf("DECISION_MAKER: household %d %s  child: %d %d is making own health decisions\n",
      //             h.get_id(), h.get_label(), ch_age, ch_rel);
      //          }
      //          else
      //          {
      //            int dm_age = dm.get_age();
      //            int dm_rel = dm.get_relationship();
      //            if (dm_rel != 1 || ch_rel != 3)
      //            {
      //              printf("DECISION_MAKER: household %d %s  decision_maker: %d %d child: %d %d\n",
      //                     h.get_id(), h.get_label(), dm_age, dm_rel, ch_age, ch_rel);
      //            }
      //          }
      //        }
      //      }
      //    }
      //  }
      //}

      //if (Global.Verbose)
      //{
      //  int count[100];
      //  int total = 0;
      //  // age distribution of heads of households
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      Person* per = NULL;
      //      for (int i = 0; i < h.get_size(); ++i)
      //      {
      //        if (h.get_enrollee(i).is_householder())
      //        {
      //          per = h.get_enrollee(i);
      //        }
      //      }
      //      if (per == NULL)
      //      {
      //        FRED_WARNING("Help! No head of household found for household id %d label %s groupquarters: %d\n",
      //         h.get_id(), h.get_label(), h.is_group_quarters() ? 1 : 0);
      //        count[0]++;
      //      }
      //      else
      //      {
      //        int a = per.get_age();
      //        if (a < 100)
      //        {
      //          count[a]++;
      //        }
      //        else
      //        {
      //          count[99]++;
      //        }
      //        total++;
      //      }
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nAge distribution of heads of households: %d households\n", total);
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    Utils.FRED_STATUS(0, "age %2d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //if (Global.Verbose)
      //{
      //  int count[100];
      //  int total = 0;
      //  int children = 0;
      //  // age distribution of heads of households with children
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    count[c] = 0;
      //  }
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    Person* per = NULL;
      //    if (this.places[p].is_household())
      //    {
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      if (h.get_children() == 0)
      //      {
      //        continue;
      //      }
      //      children += h.get_children();
      //      for (int i = 0; i < h.get_size(); ++i)
      //      {
      //        if (h.get_enrollee(i).is_householder())
      //        {
      //          per = h.get_enrollee(i);
      //        }
      //      }
      //      if (per == NULL)
      //      {
      //        FRED_WARNING("Help! No head of household found for household id %d label %s groupquarters: %d\n",
      //         h.get_id(), h.get_label(), h.is_group_quarters() ? 1 : 0);
      //        count[0]++;
      //      }
      //      else
      //      {
      //        int a = per.get_age();
      //        if (a < 100)
      //        {
      //          count[a]++;
      //        }
      //        else
      //        {
      //          count[99]++;
      //        }
      //        total++;
      //      }
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nAge distribution of heads of households with children: %d households\n", total);
      //  for (int c = 0; c < 100; ++c)
      //  {
      //    Utils.FRED_STATUS(0, "age %2d: %6d (%.2f%%)\n",
      //      c, count[c], (100.0 * count[c]) / total);
      //  }
      //  Utils.FRED_STATUS(0, "children = %d\n", children);
      //  Utils.FRED_STATUS(0, "\n");
      //}

      //if (Global.Verbose)
      //{
      //  int count_has_school_age = 0;
      //  int count_has_school_age_and_unemployed_adult = 0;
      //  int total_hh = 0;

      //  //Households with school-age children and at least one unemployed adult
      //  for (int p = 0; p < number_places; ++p)
      //  {
      //    Person* per = NULL;
      //    if (this.places[p].is_household())
      //    {
      //      total_hh++;
      //      Household* h = static_cast<Household*>(this.places[p]);
      //      if (h.get_children() == 0)
      //      {
      //        continue;
      //      }
      //      if (h.has_school_aged_child())
      //      {
      //        count_has_school_age++;
      //      }
      //      if (h.has_school_aged_child_and_unemployed_adult())
      //      {
      //        count_has_school_age_and_unemployed_adult++;
      //      }
      //    }
      //  }
      //  Utils.FRED_STATUS(0, "\nHouseholds with school-aged children and at least one unemployed adult\n");
      //  Utils.FRED_STATUS(0, "Total Households: %d\n", total_hh);
      //  Utils.FRED_STATUS(0, "Total Households with school-age children: %d\n", count_has_school_age);
      //  Utils.FRED_STATUS(0, "Total Households with school-age children and at least one unemployed adult: %d\n", count_has_school_age_and_unemployed_adult);
      //}
    }


    public int get_new_place_id()
    {
      int id = this.next_place_id;
      ++this.next_place_id;
      return id;
    }

    public void setup_group_quarters()
    {
      Utils.FRED_STATUS(0, "setup group quarters entered\n", "");

      // reset household indexes
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        this.get_household_ptr(i).set_index(i);
      }

      int p = 0;
      while (p < num_households)
      {
        var house = this.get_household_ptr(p++);
        Household new_house;
        if (house.is_group_quarters())
        {
          int gq_size = house.get_size();
          int gq_units = house.get_group_quarters_units();
          Utils.FRED_VERBOSE(0, "GQ_setup: house %d label %s subtype %c initial size %d units %d\n", p, house.get_label(),
              house.get_subtype(), gq_size, gq_units);
          if (gq_units > 1)
          {
            List<Person> housemates = new List<Person>();
            for (int i = 0; i < gq_size; ++i)
            {
              var person = house.get_enrollee(i);
              housemates.Add(person);
            }
            int units_filled = 1;
            int min_per_unit = gq_size / gq_units;
            int larger_units = gq_size - min_per_unit * gq_units;
            int smaller_units = gq_units - larger_units;
            Utils.FRED_VERBOSE(1, "GQ min_per_unit %d smaller = %d  larger = %d total = %d  orig = %d\n", min_per_unit,
                smaller_units, larger_units, smaller_units * min_per_unit + larger_units * (min_per_unit + 1), gq_size);
            int next_person = min_per_unit;
            for (int i = 1; i < smaller_units; ++i)
            {
              // assert(units_filled < gq_units);
              new_house = this.get_household_ptr(p++);
              // printf("GQ smaller new_house %s\n", new_house.get_label()); fflush(stdout);
              for (int j = 0; j < min_per_unit; ++j)
              {
                var person = housemates[next_person++];
                person.move_to_new_house(new_house);
              }
              units_filled++;
              // printf("GQ size of smaller unit %s = %d remaining in main house %d\n",
              // new_house.get_label(), new_house.get_size(), house.get_size());
            }
            for (int i = 0; i < larger_units; ++i)
            {
              new_house = this.get_household_ptr(p++);
              // printf("GQ larger new_house %s\n", new_house.get_label()); fflush(stdout);
              for (int j = 0; j < min_per_unit + 1; ++j)
              {
                var person = housemates[next_person++];
                person.move_to_new_house(new_house);
              }
              // printf("GQ size of larger unit %s = %d -- remaining in main house %d\n",
              // new_house.get_label(), new_house.get_size(), house.get_size());
            }
          }
        }
      }
    }

    public void setup_households()
    {
      Utils.FRED_STATUS(0, "setup households entered\n", "");

      // ensure that each household has an identified householder
      int num_households = this.households.Count;
      for (int p = 0; p < num_households; ++p)
      {
        var house = this.get_household_ptr(p);
        house.set_index(p);
        if (house.get_size() == 0)
        {
          Utils.FRED_VERBOSE(0, "Warning: house %d label %s has zero size.\n", house.get_id(), house.get_label());
          continue;
        }
        Person person_with_max_age = null;
        Person head_of_household = null;
        int max_age = -99;
        for (int j = 0; j < house.get_size() && head_of_household == null; ++j)
        {
          var person = house.get_enrollee(j);
          Utils.assert(person != null);
          if (person.is_householder())
          {
            head_of_household = person;
            continue;
          }
          else
          {
            int age = person.get_age();
            if (age > max_age)
            {
              max_age = age;
              person_with_max_age = person;
            }
          }
        }
        if (head_of_household == null)
        {
          Utils.assert(person_with_max_age != null);
          person_with_max_age.make_householder();
          head_of_household = person_with_max_age;
        }
        Utils.assert(head_of_household != null);
        // make sure everyone know who's the head
        for (int j = 0; j < house.get_size(); j++)
        {
          var person = house.get_enrollee(j);
          if (person != head_of_household && person.is_householder())
          {
            person.set_relationship(Global.HOUSEMATE);
          }
        }
        Utils.assert(head_of_household != null);
        Utils.FRED_VERBOSE(1, "HOLDER: house %d label %s is_group_quarters %d householder %d age %d\n", house.get_id(),
            house.get_label(), house.is_group_quarters() ? 1 : 0, head_of_household.get_id(), head_of_household.get_age());
      }

      // NOTE: the following sorts households from lowest income to highest
      this.households.Sort(new HouseholdIncomeComparer());

      // reset household indexes
      for (int i = 0; i < num_households; ++i)
      {
        this.get_household_ptr(i).set_index(i);
      }

      report_household_incomes();

      if (Global.Enable_Household_Shelter)
      {
        select_households_for_shelter();
      }
      else if (Global.Enable_HAZEL)
      {
        select_households_for_evacuation();
      }

      // add household list to visualization layer if needed
      if (Global.Enable_Visualization_Layer)
      {
        for (int i = 0; i < num_households; ++i)
        {
          var h = this.get_household_ptr(i);
          Global.Visualization.add_household(h);
        }
      }

      Utils.FRED_STATUS(0, "setup households finished\n", "");
    }

    public void setup_classrooms()
    {
      Utils.FRED_STATUS(0, "setup classrooms entered\n", "");

      int number_classrooms = 0;
      int number_schools = this.schools.Count;

      for (int p = 0; p < number_schools; ++p)
      {
        var school = get_school_ptr(p);
        number_classrooms += school.get_number_of_rooms();
      }

      Utils.FRED_STATUS(0, "Allocating space for %d classrooms in %d schools (out of %d total places)\n",
            number_classrooms, number_schools, get_number_of_places());

      for (int p = 0; p < number_schools; ++p)
      {
        var school = get_school_ptr(p);
        school.setup_classrooms();
      }

      Utils.FRED_STATUS(0, "setup classrooms finished\n", "");
    }

    public void setup_offices()
    {
      Utils.FRED_STATUS(0, "setup offices entered\n", "");

      int number_offices = 0;
      int number_places = this.places.Count;

      for (int p = 0; p < number_places; ++p)
      {
        if (this.places[p].get_type() == Place.TYPE_WORKPLACE)
        {
          var workplace = (Workplace)places[p];
          number_offices += workplace.get_number_of_rooms();
        }
      }

      for (int p = 0; p < number_places; ++p)
      {
        if (this.places[p].get_type() == Place.TYPE_WORKPLACE)
        {
          var workplace = (Workplace)this.places[p];
          workplace.setup_offices();
        }
      }

      Utils.FRED_STATUS(0, "setup offices finished\n", "");
    }

    public void setup_HAZEL_mobile_vans()
    {
      int num_hospitals = this.hospitals.Count;
      List<Hospital> temp_hosp_vec = new List<Hospital>();
      int count = 0;
      for (int i = 0; i < num_hospitals; ++i)
      {
        var tmp_hosp = this.get_hospital_ptr(i);
        if (tmp_hosp.is_mobile_healthcare_clinic())
        {
          temp_hosp_vec.Add(tmp_hosp);
          count++;
        }
      }

      //If the max number of Mobile vans allowed is >= the total mobile vans in the system, then activate all of them
      if (HAZEL_mobile_van_max >= temp_hosp_vec.Count)
      {
        for (int i = 0; i < temp_hosp_vec.Count; ++i)
        {
          //The Mobile Healthcare Clinics close after days
          temp_hosp_vec[i].set_close_date(HAZEL_disaster_end_sim_day + Hospital.get_HAZEL_mobile_van_open_delay()
            + Hospital.get_HAZEL_mobile_van_closure_day());
          temp_hosp_vec[i].set_open_date(Global.Days);
          temp_hosp_vec[i].have_HAZEL_closure_dates_been_set(true);
        }
      }
      else
      {
        //shuffle the vector
        temp_hosp_vec.Shuffle();
        for (int i = 0; i < HAZEL_mobile_van_max; ++i)
        {
          //The Mobile Healthcare Clinics close after days
          temp_hosp_vec[i].set_close_date(HAZEL_disaster_end_sim_day + Hospital.get_HAZEL_mobile_van_open_delay()
            + Hospital.get_HAZEL_mobile_van_closure_day());
          temp_hosp_vec[i].set_open_date(Global.Days);
          temp_hosp_vec[i].have_HAZEL_closure_dates_been_set(true);
        }
        for (int i = HAZEL_mobile_van_max; i < temp_hosp_vec.Count; ++i)
        {
          //These Mobile Healthcare Clinic will never open
          temp_hosp_vec[i].set_close_date(0);
          temp_hosp_vec[i].set_open_date(Global.Days);
          temp_hosp_vec[i].have_HAZEL_closure_dates_been_set(true);
        }
      }
    }

    public void setup_school_income_quartile_pop_sizes()
    {
      Utils.assert(this.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      if (Global.Report_Childhood_Presenteeism)
      {
        int number_places = this.schools.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var school = get_school_ptr(p);
          school.prepare_income_quartile_pop_size();
        }
      }
    }

    public void setup_household_income_quartile_sick_days()
    {
      Utils.assert(this.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      if (Global.Report_Childhood_Presenteeism)
      {
        var household_income_hh_mm = new List<Tuple<double, Household>>();
        int number_places = this.places.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var place = this.places[p];
          if (this.places[p].get_type() == Place.TYPE_HOUSEHOLD)
          {
            var hh = (Household)places[p];
            double hh_income = hh.get_household_income();
            household_income_hh_mm.Add(new Tuple<double, Household>(hh_income, hh));
          }
        }

        int total = household_income_hh_mm.Count;
        int q1 = total / 4;
        int q2 = q1 * 2;
        int q3 = q1 * 3;

        Utils.FRED_STATUS(0, "\nPROBABILITY WORKERS HAVE PAID SICK DAYS BY HOUSEHOLD INCOME QUARTILE:\n");
        double q1_sick_leave = 0.0;
        double q1_count = 0.0;
        double q2_sick_leave = 0.0;
        double q2_count = 0.0;
        double q3_sick_leave = 0.0;
        double q3_count = 0.0;
        double q4_sick_leave = 0.0;
        double q4_count = 0.0;
        int counter = 0;
        foreach (var househole_income in household_income_hh_mm)
        {
          double hh_sick_leave_total = 0.0;
          double hh_employee_total = 0.0;

          foreach (var per in househole_income.Item2.enrollees)
          {
            if (per.is_adult() && !per.is_student()
                && (per.get_activities().is_teacher() || per.get_activities().get_profile() == Activities.WORKER_PROFILE
                    || per.get_activities().get_profile() == Activities.WEEKEND_WORKER_PROFILE))
            {
              hh_sick_leave_total += per.get_activities().is_sick_leave_available() ? 1.0 : 0.0;
              hh_employee_total += 1.0;
            }
          }

          if (counter < q1)
          {
            househole_income.Item2.set_income_quartile(Global.Q1);
            q1_sick_leave += hh_sick_leave_total;
            q1_count += hh_employee_total;
          }
          else if (counter < q2)
          {
            househole_income.Item2.set_income_quartile(Global.Q2);
            q2_sick_leave += hh_sick_leave_total;
            q2_count += hh_employee_total;
          }
          else if (counter < q3)
          {
            househole_income.Item2.set_income_quartile(Global.Q3);
            q3_sick_leave += hh_sick_leave_total;
            q3_count += hh_employee_total;
          }
          else
          {
            househole_income.Item2.set_income_quartile(Global.Q4);
            q4_sick_leave += hh_sick_leave_total;
            q4_count += hh_employee_total;
          }

          counter++;
        }

        Utils.FRED_STATUS(0, "HOUSEHOLD INCOME QUARITLE[%d]: %.2f\n", Global.Q1,
            q1_count == 0.0 ? 0.0 : (q1_sick_leave / q1_count));
        Utils.FRED_STATUS(0, "HOUSEHOLD INCOME QUARITLE[%d]: %.2f\n", Global.Q2,
            q2_count == 0.0 ? 0.0 : (q2_sick_leave / q2_count));
        Utils.FRED_STATUS(0, "HOUSEHOLD INCOME QUARITLE[%d]: %.2f\n", Global.Q3,
            q3_count == 0.0 ? 0.0 : (q3_sick_leave / q3_count));
        Utils.FRED_STATUS(0, "HOUSEHOLD INCOME QUARITLE[%d]: %.2f\n", Global.Q4,
            q4_count == 0.0 ? 0.0 : (q4_sick_leave / q4_count));
      }
    }

    public int get_min_household_income_by_percentile(int percentile)
    {
      Utils.assert(this.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      Utils.assert(percentile > 0);
      Utils.assert(percentile <= 100);
      if (Global.Enable_hh_income_based_susc_mod)
      {
        var household_income_hh_mm = new List<Tuple<double, Household>>();
        int number_places = this.places.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var place = this.places[p];
          if (this.places[p].get_type() == Place.TYPE_HOUSEHOLD)
          {
            var hh = (Household)this.places[p];
            double hh_income = hh.get_household_income();
            household_income_hh_mm.Add(new Tuple<double, Household>(hh_income, hh));
          }
        }

        int total = household_income_hh_mm.Count;
        int percentile_goal = Convert.ToInt32(percentile / 100 * total);
        int ret_value = 0;
        int counter = 1;
        //for (HouseholdMultiMapT.iterator itr = household_income_hh_mm.begin(); itr != household_income_hh_mm.end(); ++itr)
        foreach (var household_income in household_income_hh_mm)
        {
          //double hh_sick_leave_total = 0.0;
          if (counter == percentile_goal)
          {
            ret_value = household_income.Item2.get_household_income();
            break;
          }
          counter++;
        }
        return ret_value;
      }

      return -1;
    }

    public Place get_place_from_label(string s)
    {
      Utils.assert(this.place_label_map != null);

      if (s == "-1" || string.IsNullOrWhiteSpace(s))
      {
        return null;
      }

      if (this.place_label_map.ContainsKey(s))
      {
        return this.places[this.place_label_map[s]];
      }

      Utils.FRED_VERBOSE(1, "Help! can't find place with label = {0}", s);
      return null;
    }

    public Place get_random_workplace()
    {
      int size = this.workplaces.Count;
      if (size > 0)
      {
        return this.workplaces[FredRandom.Next(0, size - 1)];
      }

      return null;
    }

    public void assign_hospitals_to_households()
    {
      if (Global.Enable_Hospitals)
      {
        int number_hh = (int)this.households.Count;
        for (int i = 0; i < number_hh; ++i)
        {
          var hh = (Household)this.households[i];
          var hosp = (Hospital)this.get_hospital_assigned_to_household(hh);
          Utils.assert(hosp != null);
          if (hosp != null)
          {
            hh.set_household_visitation_hospital(hosp);
            string hh_id_str = hh.get_label();
            this.household_hospital_map.Add(hh_id_str, hosp.get_id());
          }
        }

        //Write the mapping file if it did not already exist (or if it was incomplete)
        if (!Household_hospital_map_file_exists)
        {

          string map_file_dir = string.Empty;
          string map_file_name = string.Empty;
          FredParameters.GetParameter("household_hospital_map_file_directory", ref map_file_dir);
          FredParameters.GetParameter("household_hospital_map_file", ref map_file_name);

          if (map_file_name == "none")
          {
            this.household_hospital_map.Clear();
            return;
          }

          var filename = Utils.get_fred_file_name($"{map_file_dir}{map_file_name}");
          var hospital_household_map_fp = new StreamWriter(filename);
          if (hospital_household_map_fp == null)
          {
            Utils.fred_abort("Can't open %s\n", filename);
          }

          foreach (var house_hospital in this.household_hospital_map)
          //for (std.map < std.string, int >.iterator itr = this.household_hospital_map.begin();
          //    itr != this.household_hospital_map.end(); ++itr)
          {
            hospital_household_map_fp.WriteLine("{0},{1}", house_hospital.Key, house_hospital.Value);
          }

          hospital_household_map_fp.Flush();
          hospital_household_map_fp.Dispose();
        }
      }
    }

    /**
     * Uses a gravity model to find a random open hospital given the search parameters.
     * The location must allows overnight stays (have a subtype of NONE)
     * @param sim_day the simulation day
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_open_hospital_matching_criteria(int sim_day, Person per, bool check_insurance, bool use_search_radius_limit)
    {
      if (!Global.Enable_Hospitals)
      {
        return null;
      }

      if (check_insurance)
      {
        Utils.assert(Global.Enable_Health_Insurance);
      }

      Utils.assert(per != null);
      int overnight_cap = 0;
      //Hospital assigned_hospital = null;
      int number_hospitals = this.hospitals.Count;
      if (number_hospitals == 0)
      {
        Utils.fred_abort("No Hospitals in simulation that has Enabled Hospitalization", "");
      }
      int number_possible_hospitals = 0;
      var hh = (Household)per.get_household();
      Utils.assert(hh != null);
      //First, only try Hospitals within a certain radius (* that accept insurance)
      List<double> hosp_probs = new List<double>();
      double probability_total = 0.0;
      for (int i = 0; i < number_hospitals; ++i)
      {
        var hospital = (Hospital)this.hospitals[i];
        double distance = distance_between_places(hh, hospital);
        double cur_prob = 0.0;
        int increment = 0;
        overnight_cap = hospital.get_bed_count(sim_day);
        //Need to make sure place is not a healthcare clinic && there are beds available
        if (distance > 0.0 && !hospital.is_healthcare_clinic() && !hospital.is_mobile_healthcare_clinic()
             && hospital.should_be_open(sim_day)
             && (hospital.get_occupied_bed_count() < overnight_cap))
        {
          if (use_search_radius_limit)
          {
            if (distance <= Hospitalization_radius)
            {
              if (check_insurance)
              {
                Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
                if (hospital.accepts_insurance(per_insur))
                {
                  //Hospital accepts the insurance so we are good
                  cur_prob = overnight_cap / (distance * distance);
                  increment = 1;
                }
                else
                {
                  //Not possible (Doesn't accept insurance)
                  cur_prob = 0.0;
                  increment = 0;
                }
              }
              else
              {
                //We don't care about insurance so good to go
                cur_prob = overnight_cap / (distance * distance);
                increment = 1;
              }
            }
            else
            {
              //Not possible (not within the radius)
              cur_prob = 0.0;
              increment = 0;
            }
          }
          else
          { //Don't car about search radius
            if (check_insurance)
            {
              Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
              if (hospital.accepts_insurance(per_insur))
              {
                //Hospital accepts the insurance so we are good
                cur_prob = overnight_cap / (distance * distance);
                increment = 1;
              }
              else
              {
                //Not possible (Doesn't accept insurance)
                cur_prob = 0.0;
                increment = 0;
              }
            }
            else
            {
              //We don't care about insurance so good to go
              cur_prob = overnight_cap / (distance * distance);
              increment = 1;
            }
          }
        }
        else
        {
          //Not possible
          cur_prob = 0.0;
          increment = 0;
        }
        hosp_probs.Add(cur_prob);
        probability_total += cur_prob;
        number_possible_hospitals += increment;
      }
      Utils.assert(hosp_probs.Count == number_hospitals);
      if (number_possible_hospitals > 0)
      {
        if (probability_total > 0.0)
        {
          for (int j = 0; j < number_hospitals; ++j)
          {
            hosp_probs[j] /= probability_total;
          }
        }

        double rand = FredRandom.NextDouble();
        double cum_prob = 0.0;
        int i = 0;
        while (i < number_hospitals)
        {
          cum_prob += hosp_probs[i];
          if (rand < cum_prob)
          {
            return (Hospital)this.hospitals[i];
          }
          ++i;
        }

        return (Hospital)this.hospitals[number_hospitals - 1];
      }
      else
      {
        //No hospitals in the simulation match search criteria
        return null;
      }
    }

    /**
     * Uses a gravity model to find a random open healthcare location given the search parameters.
     * The search is ambivalent about the location allowing overnight stays.
     * @param sim_day the simulation day
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_open_healthcare_facility_matching_criteria(int sim_day, Person per, bool check_insurance, bool use_search_radius_limit)
    {
      if (!Global.Enable_Hospitals)
      {
        return null;
      }

      if (check_insurance)
      {
        Utils.assert(Global.Enable_Health_Insurance);
      }
      Utils.assert(per != null);
      int number_hospitals = this.hospitals.Count;
      if (number_hospitals == 0)
      {
        Utils.fred_abort("No Hospitals in simulation that has Enabled Hospitalization", "");
      }
      int number_possible_hospitals = 0;
      var hh = (Household)per.get_household();
      Utils.assert(hh != null);
      //First, only try Hospitals within a certain radius (* that accept insurance)
      List<double> hosp_probs = new List<double>();
      double probability_total = 0.0;
      for (int i = 0; i < number_hospitals; ++i)
      {
        var hospital = (Hospital)this.hospitals[i];
        int daily_hosp_cap = hospital.get_daily_patient_capacity(sim_day);
        double distance = distance_between_places(hh, hospital);
        int increment;
        double cur_prob;
        //Need to make sure place is open and not over capacity
        if (distance > 0.0 && hospital.should_be_open(sim_day)
           && hospital.get_current_daily_patient_count() < daily_hosp_cap)
        {
          if (use_search_radius_limit)
          {
            if (distance <= Hospitalization_radius)
            {
              if (check_insurance)
              {
                Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
                if (hospital.accepts_insurance(per_insur))
                {
                  //Hospital accepts the insurance so we are good
                  cur_prob = daily_hosp_cap / (distance * distance);
                  increment = 1;
                }
                else
                {
                  //Not possible (Doesn't accept insurance)
                  cur_prob = 0.0;
                  increment = 0;
                }
              }
              else
              {
                //We don't care about insurance so good to go
                cur_prob = daily_hosp_cap / (distance * distance);
                increment = 1;
              }
            }
            else
            {
              //Not possible (not within the radius)
              cur_prob = 0.0;
              increment = 0;
            }
          }
          else
          { //Don't car about search radius
            if (check_insurance)
            {
              Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
              if (hospital.accepts_insurance(per_insur))
              {
                //Hospital accepts the insurance so we are good
                cur_prob = daily_hosp_cap / (distance * distance);
                increment = 1;
              }
              else
              {
                //Not possible (Doesn't accept insurance)
                cur_prob = 0.0;
                increment = 0;
              }
            }
            else
            {
              //We don't care about insurance so good to go
              cur_prob = daily_hosp_cap / (distance * distance);
              increment = 1;
            }
          }
        }
        else
        {
          //Not possible
          cur_prob = 0.0;
          increment = 0;
        }
        hosp_probs.Add(cur_prob);
        probability_total += cur_prob;
        number_possible_hospitals += increment;
      } // end for loop

      Utils.assert(hosp_probs.Count == number_hospitals);
      if (number_possible_hospitals > 0)
      {
        if (probability_total > 0.0)
        {
          for (int j = 0; j < number_hospitals; ++j)
          {
            hosp_probs[j] /= probability_total;
          }
        }

        double rand = FredRandom.NextDouble();
        double cum_prob = 0.0;
        int i = 0;
        while (i < number_hospitals)
        {
          cum_prob += hosp_probs[i];
          if (rand < cum_prob)
          {
            return (Hospital)this.hospitals[i];
          }
          ++i;
        }
        return (Hospital)this.hospitals[number_hospitals - 1];
      }
      else
      {
        //No hospitals in the simulation match search criteria
        return null;
      }
    }

    /**
     * Uses a gravity model to find a random open healthcare location given the search parameters.
     * The search is ambivalent about the location allowing overnight stays, but it must be open on sim_day 0
     * @param per the person we are trying to match (need the agent's household for distance and possibly need the agent's insurance)
     * @param check_insurance whether or not to use the agent's insurance in the matching
     * @param use_search_radius_limit whether or not to cap the search radius
     */
    public Hospital get_random_primary_care_facility_matching_criteria(Person per, bool check_insurance, bool use_search_radius_limit)
    {
      if (!Global.Enable_Hospitals)
      {
        return null;
      }

      if (check_insurance)
      {
        Utils.assert(Global.Enable_Health_Insurance);
      }
      Utils.assert(per != null);

      //This is the initial primary care assignment
      if (!this.is_primary_care_assignment_initialized)
      {
        this.prepare_primary_care_assignment();
      }

      int daily_hosp_cap = 0;
      Hospital assigned_hospital = null;
      int number_hospitals = this.hospitals.Count;
      if (number_hospitals == 0)
      {
        Utils.fred_abort("No Hospitals in simulation that has Enabled Hospitalization", "");
      }
      int number_possible_hospitals = 0;
      var hh = (Household)per.get_household();
      Utils.assert(hh != null);
      //First, only try Hospitals within a certain radius (* that accept insurance)
      List<double> hosp_probs = new List<double>();
      double probability_total = 0.0;
      for (int i = 0; i < number_hospitals; ++i)
      {
        var hospital = (Hospital)this.hospitals[i];
        daily_hosp_cap = hospital.get_daily_patient_capacity(0);
        double distance = distance_between_places(hh, hospital);
        double cur_prob = 0.0;
        int increment = 0;

        //Need to make sure place is open and not over capacity
        if (distance > 0.0 && hospital.should_be_open(0))
        {
          if (use_search_radius_limit)
          {
            if (distance <= Hospitalization_radius)
            {
              if (check_insurance)
              {
                Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
                if (hospital.accepts_insurance(per_insur))
                {
                  //Hospital accepts the insurance so can check further
                  if (Hospital_ID_current_assigned_size_map[hospital.get_id()]
                      < Hospital_ID_total_assigned_size_map[hospital.get_id()])
                  {
                    //Hospital accepts the insurance and it hasn't been filled so we are good
                    cur_prob = daily_hosp_cap / (distance * distance);
                    increment = 1;
                  }
                  else
                  {
                    //Not possible
                    cur_prob = 0.0;
                    increment = 0;
                  }
                }
                else
                {
                  //Not possible (Doesn't accept insurance)
                  cur_prob = 0.0;
                  increment = 0;
                }
              }
              else
              {
                //We don't care about insurance so can check further
                if (Hospital_ID_current_assigned_size_map[hospital.get_id()]
                    < Hospital_ID_total_assigned_size_map[hospital.get_id()])
                {
                  //Hospital accepts the insurance and it hasn't been filled so we are good
                  cur_prob = daily_hosp_cap / (distance * distance);
                  increment = 1;
                }
                else
                {
                  //Not possible
                  cur_prob = 0.0;
                  increment = 0;
                }
              }
            }
            else
            {
              //Not possible (not within the radius)
              cur_prob = 0.0;
              increment = 0;
            }
          }
          else
          { //Don't car about search radius
            if (check_insurance)
            {
              Insurance_assignment_index per_insur = per.get_health().get_insurance_type();
              if (hospital.accepts_insurance(per_insur))
              {
                //Hospital accepts the insurance so can check further
                if (Hospital_ID_current_assigned_size_map[hospital.get_id()]
                    < Hospital_ID_total_assigned_size_map[hospital.get_id()])
                {
                  //Hospital accepts the insurance and it hasn't been filled so we are good
                  cur_prob = daily_hosp_cap / (distance * distance);
                  increment = 1;
                }
                else
                {
                  //Not possible
                  cur_prob = 0.0;
                  increment = 0;
                }
              }
              else
              {
                //Not possible (Doesn't accept insurance)
                cur_prob = 0.0;
                increment = 0;
              }
            }
            else
            {
              //We don't care about insurance so can check further
              if (Hospital_ID_current_assigned_size_map[hospital.get_id()]
                  < Hospital_ID_total_assigned_size_map[hospital.get_id()])
              {
                //Hospital accepts the insurance and it hasn't been filled so we are good
                cur_prob = daily_hosp_cap / (distance * distance);
                increment = 1;
              }
              else
              {
                //Not possible
                cur_prob = 0.0;
                increment = 0;
              }
            }
          }
        }
        else
        {
          //Not possible
          cur_prob = 0.0;
          increment = 0;
        }
        hosp_probs.Add(cur_prob);
        probability_total += cur_prob;
        number_possible_hospitals += increment;
      }  // end for loop

      Utils.assert(hosp_probs.Count == number_hospitals);
      if (number_possible_hospitals > 0)
      {
        if (probability_total > 0.0)
        {
          for (int j = 0; j < number_hospitals; ++j)
          {
            hosp_probs[j] /= probability_total;
          }
        }

        double rand = FredRandom.NextDouble();
        double cum_prob = 0.0;
        int i = 0;
        while (i < number_hospitals)
        {
          cum_prob += hosp_probs[i];
          if (rand < cum_prob)
          {
            return (Hospital)this.hospitals[i];
          }
          ++i;
        }
        return (Hospital)this.hospitals[number_hospitals - 1];
      }
      else
      {
        //No hospitals in the simulation match search criteria
        return null;
      }
    }

    public void print_household_size_distribution(string dir, string date_string, int run)
    {
      var count = new int[11];
      var pct = new double[11];
      string filename = $"{dir}/household_size_dist_{date_string}.{run:D2}";
      Utils.FRED_STATUS(0, "print_household_size_dist entered, filename = {0}", filename);
      int total = 0;
      int number_households = (int)households.Count;
      for (int p = 0; p < number_households; ++p)
      {
        int n = this.households[p].get_size();
        if (n < 11)
        {
          count[n]++;
        }
        else
        {
          count[10]++;
        }
        total++;
      }

      using var fp = new StreamWriter(filename);
      for (int i = 0; i < 11; i++)
      {
        pct[i] = 100.0 * count[i] / number_households;
        fp.WriteLine("size {0} count {1} pct {2}",  i * 5, count[i], pct[i]);
      }

      fp.Flush();
      fp.Dispose();
    }

    public void report_shelter_stats(int day)
    {
      int sheltering_households = 0;
      int sheltering_pop = 0;
      int sheltering_total_pop = 0;
      int sheltering_new_infections = 0;
      int sheltering_total_infections = 0;
      int non_sheltering_total_infections = 0;
      int non_sheltering_pop = 0;
      int non_sheltering_new_infections = 0;
      int num_households = this.households.Count;
      double sheltering_ar = 0.0;
      double non_sheltering_ar = 0.0;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.get_household_ptr(i);
        if (h.is_sheltering())
        {
          sheltering_new_infections += h.get_new_infections(day, 0);
          sheltering_total_infections += h.get_total_infections(0);
          sheltering_total_pop += h.get_size();
        }
        else
        {
          non_sheltering_pop += h.get_size();
          non_sheltering_new_infections += h.get_new_infections(day, 0);
          non_sheltering_total_infections += h.get_total_infections(0);
        }
        if (h.is_sheltering_today(day))
        {
          sheltering_households++;
          sheltering_pop += h.get_size();
        }
      }
      if (sheltering_total_pop > 0)
      {
        sheltering_ar = 100.0 * sheltering_total_infections / sheltering_total_pop;
      }
      if (non_sheltering_pop > 0)
      {
        non_sheltering_ar = 100.0 * non_sheltering_total_infections / non_sheltering_pop;
      }
      Global.Daily_Tracker.set_index_key_pair(day, "H_sheltering", sheltering_households);
      Global.Daily_Tracker.set_index_key_pair(day, "N_sheltering", sheltering_pop);
      Global.Daily_Tracker.set_index_key_pair(day, "C_sheltering", sheltering_new_infections);
      Global.Daily_Tracker.set_index_key_pair(day, "AR_sheltering", sheltering_ar);
      Global.Daily_Tracker.set_index_key_pair(day, "N_noniso", non_sheltering_pop);
      Global.Daily_Tracker.set_index_key_pair(day, "C_noniso", non_sheltering_new_infections);
      Global.Daily_Tracker.set_index_key_pair(day, "AR_noniso", non_sheltering_ar);
    }

    public void end_of_run()
    {
      if (Global.Verbose > 1)
      {
        int number_places = this.places.Count;
        for (int p = 0; p < number_places; ++p)
        {
          var place = this.places[p];
          Utils.FRED_STATUS(0,
            "PLACE REPORT: id %d type %c size %d inf %d attack_rate %5.2f first_day %d last_day %d\n",
            place.get_id(), place.get_type(), place.get_size(),
            place.get_total_infections(0),
            100.0 * place.get_attack_rate(0),
            place.get_first_day_infectious(),
            place.get_last_day_infectious());
        }
      }
      if (Global.Enable_Household_Shelter)
      {
        int households_sheltering = 0;
        int households_not_sheltering = 0;
        int pop_sheltering = 0;
        int pop_not_sheltering = 0;
        int infections_sheltering = 0;
        int infections_not_sheltering = 0;
        double ar_sheltering = 0.0;
        double ar_not_sheltering = 0.0;
        int num_households = this.households.Count;
        for (int i = 0; i < num_households; ++i)
        {
          var h = this.get_household_ptr(i);
          if (h.is_sheltering())
          {
            pop_sheltering += h.get_size();
            infections_sheltering += h.get_total_infections(0);
            households_sheltering++;
          }
          else
          {
            pop_not_sheltering += h.get_size();
            infections_not_sheltering += h.get_total_infections(0);
            households_not_sheltering++;
          }
        }

        if (pop_sheltering > 0)
        {
          ar_sheltering = (double)infections_sheltering / (double)pop_sheltering;
        }

        if (pop_not_sheltering > 0)
        {
          ar_not_sheltering = (double)infections_not_sheltering / (double)pop_not_sheltering;
        }

        Utils.FRED_STATUS(0,
          "ISOLATION REPORT: households_sheltering %d pop_sheltering %d infections_sheltering %d ar_sheltering %f ",
            households_sheltering, pop_sheltering, infections_sheltering, ar_sheltering);
        Utils.FRED_STATUS(0,
          "households_not_sheltering %d pop_not_sheltering %d infections_not_sheltering %d ar_not_sheltering %f\n",
            households_not_sheltering, pop_not_sheltering, infections_not_sheltering, ar_not_sheltering);
      }
    }

    public int get_number_of_demes()
    {
      return this.number_of_demes;
    }

    public int get_housing_data(int[] target_size, int[] current_size)
    {
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.get_household_ptr(i);
        current_size[i] = h.get_size();
        target_size[i] = h.get_orig_size();
      }
      return num_households;
    }

    public void get_initial_visualization_data_from_households()
    {
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.get_household_ptr(i);
        Global.Visualization.initialize_household_data(h.get_latitude(), h.get_longitude(), h.get_size());
        // printf("%f %f %3d %s\n", h.get_latitude(), h.get_longitude(), h.get_size(), h.get_label());
      }
    }

    public void get_visualization_data_from_households(int day, int disease_id, int output_code)
    {
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.get_household_ptr(i);
        int count = h.get_visualization_counter(day, disease_id, output_code);
        int popsize = h.get_size();
        // update appropriate visualization patch
        Global.Visualization.update_data(h.get_latitude(), h.get_longitude(), count, popsize);
      }
    }

    public void get_census_tract_data_from_households(int day, int disease_id, int output_code)
    {
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.get_household_ptr(i);
        int count = h.get_visualization_counter(day, disease_id, output_code);
        int popsize = h.get_size();
        int census_tract_index = h.get_census_tract_index();
        long  census_tract = this.get_census_tract_with_index(census_tract_index);
        Global.Visualization.update_data(census_tract, count, popsize);
      }

    }
    public void swap_houses(int house_index1, int house_index2)
    {

      var h1 = this.get_household_ptr(house_index1);
      var h2 = this.get_household_ptr(house_index2);
      if (h1 == null || h2 == null)
        return;

      Utils.FRED_VERBOSE(1, "HOUSING: swapping house %s with %d beds and %d occupants with %s with %d beds and %d occupants\n",
          h1.get_label(), h1.get_orig_size(), h1.get_size(), h2.get_label(), h2.get_orig_size(), h2.get_size());

      // get pointers to residents of house h1
      var housemates1 = h1.get_inhabitants();
      List<Person> temp1 = new List<Person>(housemates1);

      // get pointers to residents of house h2
      var housemates2 = h2.get_inhabitants();
      List<Person> temp2 = new List<Person>(housemates2);

      foreach (var person in temp1)
      {
        person.move_to_new_house(h2);
      }

      foreach (var person in temp2)
      {
        person.move_to_new_house(h1);
      }

      Utils.FRED_VERBOSE(1, "HOUSING: swapped house %s with %d beds and %d occupants with %s with %d beds and %d occupants\n",
          h1.get_label(), h1.get_orig_size(), h1.get_size(), h2.get_label(), h2.get_orig_size(), h2.get_size());
    }

    public void combine_households(int house_index1, int house_index2)
    {
      var h1 = this.get_household_ptr(house_index1);
      var h2 = this.get_household_ptr(house_index2);
      if (h1 == null || h2 == null)
        return;

      Utils.FRED_VERBOSE(1, "HOUSING: combining house %s with %d beds and %d occupants with %s with %d beds and %d occupants\n",
          h1.get_label(), h1.get_orig_size(), h1.get_size(), h2.get_label(), h2.get_orig_size(), h2.get_size());

      // get pointers to residents of house h2
      var temp2 = new List<Person>(h2.get_inhabitants());
      // move into house h1
      foreach (var person in temp2)
      {
        person.move_to_new_house(h1);
      }

      Console.WriteLine("HOUSING: combined house {0} with {1} beds and {2} occupants with {3} with {4} beds and {5} occupants",
          h1.get_label(), h1.get_orig_size(), h1.get_size(), h2.get_label(), h2.get_orig_size(), h2.get_size());

    }

    public Place select_school(int county_index, int grade)
    {
      // find school with this grade with greatest vacancy, and one with smallest overcapacity
      School school_with_vacancy = null;
      School school_with_overcrowding = null;
      double vacancy = -1.0;
      // limit capacity to 150% of original size:
      double overcap = 50.0;
      int size = this.schools_by_grade[grade].Count;
      for (int i = 0; i < size; ++i)
      {
        var school = (School)this.schools_by_grade[grade][i];
        int orig = school.get_orig_students_in_grade(grade);
        // the following treats schools with fewer than 20 original
        // students as an anomaly due to incomplete representation of
        // the student body, perhaps from outside the simulation region
        if (orig < 20)
        {
          continue;
        }
        // the following avoids initially empty schools
        // if (orig == 0) continue;
        int now = school.get_students_in_grade(grade);
        if (now <= orig)
        {
          // school has vacancy
          double vac_pct = (orig - now) / orig;
          if (vac_pct > vacancy)
          {
            vacancy = vac_pct;
            school_with_vacancy = school;
          }
        }
        else
        {
          // school is at or over capacity
          double over_pct = (now - orig) / orig;
          if (over_pct < overcap)
          {
            overcap = over_pct;
            school_with_overcrowding = school;
          }
        }
      }

      // if there is a school with a vacancy, return one with the most vacancy
      if (school_with_vacancy != null)
      {
        int orig = school_with_vacancy.get_orig_students_in_grade(grade);
        int now = school_with_vacancy.get_students_in_grade(grade);
        Utils.FRED_VERBOSE(1, "select_school_by_grade: GRADE %d closest school WITH VACANCY %s ORIG %d NOW %d\n", grade,
            school_with_vacancy.get_label(), orig, now);
        return school_with_vacancy;
      }

      // otherwise, return school with minimal overcrowding, if there is one
      if (school_with_overcrowding != null)
      {
        int orig = school_with_overcrowding.get_orig_students_in_grade(grade);
        int now = school_with_overcrowding.get_students_in_grade(grade);
        Utils.FRED_VERBOSE(1, "select_school_by_grade: GRADE %d school with smallest OVERCROWDING %s ORIG %d NOW %d\n", grade,
            school_with_overcrowding.get_label(), orig, now);
        return school_with_overcrowding;
      }

      // ERROR: no grade appropriate school found
      Utils.fred_abort("select_school_by_grade: null -- no grade-appropriate school found\n");
      return null;
    }

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
      return retval < 0 ? 0 : retval;
    }

    public int get_population_of_county_with_index(int index, int age, char sex)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      int retval = this.counties[index].get_current_popsize(age, sex);
      return retval < 0 ? 0 : retval;
    }

    public int get_population_of_county_with_index(int index, int age_min, int age_max, char sex)
    {
      if (index < 0)
      {
        return 0;
      }
      Utils.assert(index < this.counties.Count);
      int retval = this.counties[index].get_current_popsize(age_min, age_max, sex);
      return retval < 0 ? 0 : retval;
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

    public void update_population_dynamics(int day)
    {
      int number_counties = this.counties.Count;
      for (int i = 0; i < number_counties; ++i)
      {
        this.counties[i].update(day);
      }
    }

    public void delete_place_label_map()
    {
      this.place_label_map.Clear();
    }

    public void print_stats(int day)
    {
      if (Global.Enable_HAZEL)
      {
        int num_open_hosp = 0;
        int open_hosp_cap = 0;
        int tot_hosp_cap = 0;
        int num_hospitals = this.hospitals.Count;
        for (int i = 0; i < num_hospitals; ++i)
        {
          var tmp_hosp = this.get_hospital_ptr(i);
          int hosp_cap = tmp_hosp.get_daily_patient_capacity(day);
          if (tmp_hosp.should_be_open(day))
          {
            num_open_hosp++;
            open_hosp_cap += hosp_cap;
            tot_hosp_cap += hosp_cap;
          }
          else
          {
            tot_hosp_cap += hosp_cap;
          }
        }

        int num_households = this.households.Count;
        int tot_res_stayed = 0;
        int tot_res_evac = 0;

        for (int i = 0; i < num_households; ++i)
        {
          var hh = this.get_household_ptr(i);
          if (hh.is_sheltering_today(day))
          {
            tot_res_evac += hh.get_size();
          }
          else
          {
            tot_res_stayed += hh.get_size();
          }
        }

        Utils.FRED_VERBOSE(1, "Place_List print stats for day %d\n", day);
        Global.Daily_Tracker.set_index_key_pair(day, "Tot_hosp_cap", tot_hosp_cap);
        Global.Daily_Tracker.set_index_key_pair(day, "Open_hosp_cap", open_hosp_cap);
        Global.Daily_Tracker.set_index_key_pair(day, "Open_hosp", num_open_hosp);
        Global.Daily_Tracker.set_index_key_pair(day, "Closed_hosp", num_hospitals - num_open_hosp);
        Global.Daily_Tracker.set_index_key_pair(day, "Tot_res_stayed", tot_res_stayed);
        Global.Daily_Tracker.set_index_key_pair(day, "Tot_res_evac", tot_res_evac);
      }
    }


    public static int get_HAZEL_disaster_start_sim_day()
    {
      return HAZEL_disaster_start_sim_day;
    }

    public static int get_HAZEL_disaster_end_sim_day()
    {
      return HAZEL_disaster_end_sim_day;
    }

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

    private void read_household_file(char deme_id, string location_file, List<Place_Init_Data> pids)
    {
      var hh_id = 0;
      var serialno = 1;
      var stcotrbg = 2;
      var hh_race = 3;
      var hh_income = 4;
      var hh_size = 5;
      var hh_age = 6;
      var latitude = 7;
      var longitude = 8;

      string line;
      using var fp = new StreamReader(location_file);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        if (line.Contains("\""))
        {
          line = line.Replace("\"", string.Empty);
        }
        var tokens = line.Split(',');

        // skip header line
        if (tokens[hh_id] != "hh_id" && tokens[hh_id] != "sp_id")
        {
          char place_type = Place.TYPE_HOUSEHOLD;
          char place_subtype = Mixing_Group.SUBTYPE_NONE;
          string s = $"{place_type}{tokens[hh_id]}";
          string fipstr = tokens[stcotrbg].Length > 5 ? tokens[stcotrbg].Substring(0, 5) : tokens[stcotrbg];
          string census_tract_str;
          long census_tract = 0;
          int fips = Convert.ToInt32(fipstr);
          int county = 0;
          int tract_index = 0;

          // Grab the first eleven (state and county + six) digits of stcotrbg to get the census tract
          // e.g 090091846001 StateCo = 09009, 184600 is the census tract, throw away the 1
          if (Global.Enable_Vector_Transmission)
          {
            // Colombian census tracks are just 8 digits
            census_tract_str = tokens[stcotrbg].Length > 8 ? tokens[stcotrbg].Substring(0, 8) : tokens[stcotrbg];
          }
          else
          {
            census_tract_str = tokens[stcotrbg].Length > 11 ? tokens[stcotrbg].Substring(0, 11) : tokens[stcotrbg];
          }

          census_tract = Convert.ToInt64(census_tract_str);
          // find the index for this census tract
          int n_census_tracts = this.census_tracts.Count;
          for (tract_index = 0; tract_index < n_census_tracts; ++tract_index)
          {
            if (this.census_tracts[tract_index] == census_tract)
            {
              break;
            }
          }
          if (tract_index == n_census_tracts)
          {
            this.census_tracts.Add(census_tract);
          }

          // find the county index for this fips code
          int n_counties = this.counties.Count;
          for (county = 0; county < n_counties; county++)
          {
            if (this.counties[county].get_fips() == fips)
            {
              break;
            }
          }
          if (county == n_counties)
          {
            var new_county = new County(fips);
            this.counties.Add(new_county);
          }

          // TODO: SetInsertResultT result = pids.Add(new 
          pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, county,
                  tract_index, tokens[hh_income]));
          ++this.place_type_counts[place_type];
          //if (result.second)
          //{
          //}
        }
      }

      fp.Dispose();
    }

    private void read_workplace_file(char deme_id, string location_file, List<Place_Init_Data> pids)
    {
      int workplace_id = 0, num_workers_assigned = 1, latitude = 2, longitude = 3;
      string line;
      using var fp = new StreamReader(location_file);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        if (line.Contains("\""))
        {
          line = line.Replace("\"", string.Empty);
        }
        var tokens = line.Split(',');
        // skip header line
        if (tokens[workplace_id] != "workplace_id" && tokens[workplace_id] != "sp_id")
        {
          char place_type = Place.TYPE_WORKPLACE;
          char place_subtype = Mixing_Group.SUBTYPE_NONE;
          var s = $"{place_type}{tokens[workplace_id]}";

          // TODO: SetInsertResultT result = pids.Add(new 
          pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id));

          ++this.place_type_counts[place_type];
          //if (result.second)
          //{
          //}
        }
      }
      fp.Dispose();
    }

    private void read_hospital_file(char deme_id, string location_file, List<Place_Init_Data> pids)
    {
      if (!Global.Enable_Hospitals)
      {
        return;
      }

      int workers = 0;
      int workplace_id = 0, num_workers_assigned = 1, latitude = 2, longitude = 3;
      string line;
      using var fp = new StreamReader(location_file);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        if (line.Contains("\""))
        {
          line = line.Replace("\"", string.Empty);
        }
        var tokens = line.Split(',');
        // skip header line
        if (tokens[workplace_id] != "workplace_id" && tokens[workplace_id] != "sp_id")
        {
          char place_type = Place.TYPE_WORKPLACE;
          char place_subtype = Mixing_Group.SUBTYPE_NONE;
          var s = $"{place_type}{tokens[workplace_id]}";
          workers = Convert.ToInt32(tokens[num_workers_assigned]);
          // TODO: SetInsertResultT result = pids.Add(new 
          pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, 0, 0, "0", false, workers));

          ++this.place_type_counts[place_type];
          //if (result.second)
          //{
          //}
        }
      }
      fp.Dispose();
    }
    private void read_school_file(char deme_id, string location_file, List<Place_Init_Data> pids)
    {
      int school_id = 0,
          name = 1,
          stabbr = 2,
          address = 3,
          city = 4,
          countyIndex = 5,
          zip = 6,
          zip4 = 7,
          nces_id = 8,
          total = 9,
          prek = 10,
          kinder = 11,
          gr01_gr12 = 12,
          ungraded = 13,
          latitude = 14,
          longitude = 15,
          source = 16,
          stco = 17;

      string line;
      using var fp = new StreamReader(location_file);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        if (line.Contains("\""))
        {
          line = line.Replace("\"", string.Empty);
        }
        var tokens = line.Split(',');
        // skip header line
        if (tokens[school_id] != "school_id" && tokens[school_id] != "sp_id")
        {
          char place_type = Place.TYPE_SCHOOL;
          char place_subtype = Mixing_Group.SUBTYPE_NONE;
          // printf("|%s| |%s| |%s| |%s|\n", tokens[latitude], tokens[longitude], tokens[source], tokens[stco]); exit(0);

          // get county index for this school
          int county = 0;
          if (tokens[stco] != "-1")
          {
            // grab the first five digits of stcotrbg to get the county fips code
            string fipstr = tokens[stco];
            int fips = Convert.ToInt32(fipstr);
            // find the county index for this fips code
            int n_counties = counties.Count;
            for (county = 0; county < n_counties; ++county)
            {
              if (counties[county].get_fips() == fips)
              {
                break;
              }
            }
            if (county == n_counties)
            {
              // this school is outside the simulation region
              county = -1;
            }
          }

          string s = $"{place_type}{tokens[school_id]}";

          //SetInsertResultT result = pids.insert(
          pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, county));

          ++this.place_type_counts[place_type];
          Utils.FRED_VERBOSE(1, "READ_SCHOOL: %s %c %f %f name |%s| county %d\n", s, place_type, tokens[latitude],
              tokens[longitude], tokens[name], get_fips_of_county_with_index(county));
          //if (result.second)
          //{
          //}
        }
      }
      fp.Dispose();
    }

    private void read_group_quarters_file(char deme_id, string location_file, List<Place_Init_Data> pids)
    {
      int gq_id = 0, gq_type = 1, gq_size = 2, stcotrbg_a = 3, stcotrbg_b = 4, latitude = 5, longitude = 6;
      string fipstr;
      string census_tract_str;
      long census_tract = 0;
      int fips = 0;
      int county = 0;
      int tract_index = 0;
      int capacity = 0;
      bool format_2010_ver1 = false;

      string line;
      using var fp = new StreamReader(location_file);
      while (fp.Peek() != -1)
      {
        line = fp.ReadLine();
        if (line.Contains("\""))
        {
          line = line.Replace("\"", string.Empty);
        }
        var tokens = line.Split(',');

        // check for 2010_ver1 format
        if (tokens[gq_id] == "sp_id")
        {
          format_2010_ver1 = true;
        }

        // skip header line
        if (tokens[gq_id] != "gq_id" && tokens[gq_id] != "sp_id")
        {
          char place_type;
          char place_subtype = Mixing_Group.SUBTYPE_NONE;

          if (format_2010_ver1)
          {
            // the 2010_ver1 format omits the stcotrbg_b field
            // add the additional field
            Array.Resize(ref tokens, tokens.Length + 1);
            // shift last three fields back one position
            //throw new NotImplementedException("PLACE_LIST 2010 ver1 is not implemented!");
            tokens[tokens.Length - 1] = tokens[tokens.Length - 2];
            tokens[tokens.Length - 2] = tokens[tokens.Length - 3];
            tokens[tokens.Length - 3] = tokens[tokens.Length - 4];
            tokens[tokens.Length - 4] = string.Empty;
            //tokens.assign(longitude, latitude);
            //tokens.assign(latitude, stcotrbg_b);
            //tokens.assign(stcotrbg_b, stcotrbg_a);
            // for (int i = 0; i < 7; i++) { printf("token %d: |%s|\n", i, tokens[i]); } printf("\n");
          }

          capacity = Convert.ToInt32(tokens[gq_size]);
          // grab the first five digits of stcotrbg to get the county fips code
          fipstr = tokens[stcotrbg_b].Length > 5 ? tokens[stcotrbg_b].Substring(0, 5) : tokens[stcotrbg_b];
          fips = Convert.ToInt32(fipstr);
          // Grab the first eleven (state and county + six) digits of stcotrbg to get the census tract
          // e.g 090091846001 StateCo = 09009, 184600 is the census tract, throw away the 1
          census_tract_str = tokens[stcotrbg_b].Length > 11 ? tokens[stcotrbg_b].Substring(0, 11) : tokens[stcotrbg_b];
          census_tract = Convert.ToInt64(census_tract_str);

          // find the index for this census tract
          int n_census_tracts = census_tracts.Count;
          for (tract_index = 0; tract_index < n_census_tracts; tract_index++)
          {
            if (census_tracts[tract_index] == census_tract)
            {
              break;
            }
          }
          if (tract_index == n_census_tracts)
          {
            census_tracts.Add(census_tract);
          }

          // find the county index for this fips code
          int n_counties = counties.Count;
          for (county = 0; county < n_counties; county++)
          {
            if (counties[county].get_fips() == fips)
            {
              break;
            }
          }
          if (county == n_counties)
          {
            var new_county = new County(fips);
            this.counties.Add(new_county);
          }

          // set number of units and subtype for this group quarters
          int number_of_units = 0;
          if (tokens[gq_type] == "C")
          {
            number_of_units = Convert.ToInt32(capacity / College_dorm_mean_size);
            place_subtype = Place.SUBTYPE_COLLEGE;
          }
          if (tokens[gq_type] == "M")
          {
            number_of_units = Convert.ToInt32(capacity / Military_barracks_mean_size);
            place_subtype = Place.SUBTYPE_MILITARY_BASE;
          }
          if (tokens[gq_type] == "P")
          {
            number_of_units = Convert.ToInt32(capacity / Prison_cell_mean_size);
            place_subtype = Place.SUBTYPE_PRISON;
          }
          if (tokens[gq_type] == "N")
          {
            number_of_units = Convert.ToInt32(capacity / Nursing_home_room_mean_size);
            place_subtype = Place.SUBTYPE_NURSING_HOME;
          }
          if (number_of_units == 0)
          {
            number_of_units = 1;
          }

          // add a workplace for this group quarters
          place_type = Place.TYPE_WORKPLACE;
          var wp = $"{place_type}{tokens[gq_id]}";

          pids.Add(new Place_Init_Data(wp, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, county,
                  tract_index, "0", true));

          ++this.place_type_counts[place_type];
          //if (result.second)
          //{
          //  ++(this.place_type_counts[place_type]);
          //}

          // add as household
          place_type = Place.TYPE_HOUSEHOLD;
          var s = $"{place_type}{tokens[gq_id]}";
          pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, county,
                  tract_index, "0", true, 0, number_of_units, tokens[gq_type], wp));
          ++this.place_type_counts[place_type];
          Utils.FRED_VERBOSE(1, "READ_GROUP_QUARTERS: %s type %c size %d lat %f lon %f\n", s, place_type, capacity,
              tokens[latitude], tokens[longitude]);
          //if (result.second)
          //{
          //  ++(this.place_type_counts[place_type]);
          //  FRED_VERBOSE(1, "READ_GROUP_QUARTERS: %s type %c size %d lat %f lon %f\n", s, place_type, capacity,
          //      result.first.lat, result.first.lon);
          //}

          // generate additional household units associated with this group quarters
          for (int i = 1; i < number_of_units; ++i)
          {
            s = $"{place_type}{tokens[gq_id]}-{i:D3}";
            pids.Add(new Place_Init_Data(s, place_type, place_subtype, tokens[latitude], tokens[longitude], deme_id, county,
                    tract_index, "0", true, 0, 0, tokens[gq_type], wp));
            ++this.place_type_counts[place_type];
            //if (result.second)
            //{
            //  ++(this.place_type_counts[place_type]);
            //}
            Utils.FRED_VERBOSE(1, "Adding GQ Household %s out of %d units\n", s, number_of_units);
          }
        }
      }
      fp.Dispose();
    }

    private void reassign_workers_to_places_of_type(char place_type, int fixed_staff, double staff_ratio)
    {
      int number_places = this.places.Count;
      Utils.FRED_STATUS(0, "reassign workers to place of type %c entered. places = %d\n", place_type, number_places);
      for (int p = 0; p < number_places; p++)
      {
        var place = this.places[p];
        if (place.get_type() == place_type)
        {
          var lat = place.get_latitude();
          var lon = place.get_longitude();
          double x = Geo.get_x(lon);
          double y = Geo.get_y(lat);
          if (place_type == Place.TYPE_SCHOOL)
          {
            Utils.FRED_VERBOSE(0, "Reassign teachers to school %s at (%f,%f) \n", place.get_label(), x, y);
          }
          else
          {
            Utils.FRED_VERBOSE(0, "Reassign workers to place %s at (%f,%f) \n", place.get_label(), x, y);
          }

          // ignore place if it is outside the region
          var regional_patch = Global.Simulation_Region.get_patch(lat, lon);
          if (regional_patch == null)
          {
            Utils.FRED_VERBOSE(0, "place OUTSIDE_REGION lat %f lon %f \n", lat, lon);
            continue;
          }

          // target staff size
          int n = place.get_size();
          if (place_type == Place.TYPE_SCHOOL)
          {
            var s = (School)place;
            n = s.get_orig_number_of_students();
          }
          Utils.FRED_VERBOSE(1, "Size %d\n", n);
          int staff = fixed_staff;
          if (staff_ratio > 0.0)
          {
            staff += Convert.ToInt32(0.5 + n / staff_ratio);
          }

          var nearby_workplace = regional_patch.get_nearby_workplace(place, staff);
          if (nearby_workplace != null)
          {
            if (place_type == Place.TYPE_SCHOOL)
            {
              // make all the workers in selected workplace teachers at the nearby school
              nearby_workplace.turn_workers_into_teachers(place);
            }
            else
            {
              // make all the workers in selected workplace as workers in the target place
              nearby_workplace.reassign_workers(place);
            }
            return;
          }
          else
          {
            Utils.FRED_VERBOSE(0, "NO NEARBY_WORKPLACE FOUND for place at lat %f lon %f \n", lat, lon);
          }
        }
      }
    }

    private void reassign_workers_to_group_quarters(char subtype, int fixed_staff, double resident_to_staff_ratio)
    {
      int number_places = this.places.Count;
      Utils.FRED_STATUS(0, "reassign workers to group quarters subtype %c entered. places = %d\n", subtype, number_places);
      for (int p = 0; p < number_places; ++p)
      {
        var place = this.places[p];
        if (place.is_workplace() && place.get_subtype() == subtype)
        {
          var lat = place.get_latitude();
          var lon = place.get_longitude();
          double x = Geo.get_x(lon);
          double y = Geo.get_y(lat);
          Utils.FRED_VERBOSE(1, "Reassign workers to place %s at (%f,%f) \n", place.get_label(), x, y);

          // ignore place if it is outside the region
          var regional_patch = Global.Simulation_Region.get_patch(lat, lon);
          if (regional_patch == null)
          {
            Utils.FRED_VERBOSE(0, "place OUTSIDE_REGION lat %f lon %f \n", lat, lon);
            continue;
          }

          // target staff size
          Utils.FRED_VERBOSE(1, "Size %d ", place.get_size());
          int staff = fixed_staff;
          if (resident_to_staff_ratio > 0.0)
          {
            staff += Convert.ToInt32(0.5 + (double)place.get_size() / resident_to_staff_ratio);
          }

          var nearby_workplace = regional_patch.get_nearby_workplace(place, staff);
          if (nearby_workplace != null)
          {
            // make all the workers in selected workplace as workers in the target place
            nearby_workplace.reassign_workers(place);
            return;
          }
          else
          {
            Utils.FRED_VERBOSE(0, "NO NEARBY_WORKPLACE FOUND for place at lat %f lon %f \n", lat, lon);
          }
        }
      }
    }

    private void prepare_primary_care_assignment()
    {
      if (this.is_primary_care_assignment_initialized)
      {
        return;
      }

      if (Global.Enable_Hospitals && this.is_load_completed() && Global.Pop.is_load_completed())
      {
        int tot_pop_size = Global.Pop.get_pop_size();
        Utils.assert(Hospital_overall_panel_size > 0);
        //Determine the distribution of population that should be assigned to each hospital location
        for (int i = 0; i < this.hospitals.Count; ++i)
        {
          var hosp = this.get_hospital_ptr(i);
          double proprtn_of_total_panel = 0;
          if (hosp.get_subtype() != Place.SUBTYPE_MOBILE_HEALTHCARE_CLINIC)
          {
            proprtn_of_total_panel = hosp.get_daily_patient_capacity(0)
                / Hospital_overall_panel_size;
          }
          Hospital_ID_total_assigned_size_map.Add(hosp.get_id(), Convert.ToInt32(Math.Ceiling(proprtn_of_total_panel * tot_pop_size)));
          Hospital_ID_current_assigned_size_map.Add(hosp.get_id(), 0);
        }
        this.is_primary_care_assignment_initialized = true;
      }
    }

    /**
     * @param hh a pointer to a Household object
     *
     * If there is already a Hospital assigned to a Household int the map household_hospital_map, then just return it.
     * Otherwise, find a suitable hospital (must allow overnight stays) and assign it to a household (put it in the map for later)
     *
     * @return a pointer to the Hospital that is assigned to the Household
     */
    private Hospital get_hospital_assigned_to_household(Household hh)
    {
      Utils.assert(this.is_load_completed());
      if (this.household_hospital_map.ContainsKey(hh.get_label()))
      {
        return this.get_hospital_ptr(this.household_hospital_map[hh.get_label()]);
      }
      else
      {
        if (Household_hospital_map_file_exists)
        {
          //List is incomplete so set this so we can print out a new file
          Household_hospital_map_file_exists = false;
        }

        Hospital hosp = null;
        if (hh.get_size() > 0)
        {
          var per = hh.get_enrollee(0);
          Utils.assert(per != null);
          if (Global.Enable_Health_Insurance)
          {
            hosp = this.get_random_open_hospital_matching_criteria(0, per, true, true);
          }
          else
          {
            hosp = this.get_random_open_hospital_matching_criteria(0, per, false, true);
          }
          //If it came back with nothing, expand the search radius
          if (hosp == null)
          {
            if (Global.Enable_Health_Insurance)
            {
              hosp = this.get_random_open_hospital_matching_criteria(0, per, true, false);
            }
            else
            {
              hosp = this.get_random_open_hospital_matching_criteria(0, per, false, false);
            }
          }
          //If it still came back with nothing, ignore health insurance
          if (hosp == null)
          {
            hosp = this.get_random_open_hospital_matching_criteria(0, per, false, false);
          }
        }
        Utils.assert(hosp != null);
        return hosp;
      }
    }

    private void report_household_incomes()
    {

      // initialize household income stats
      this.min_household_income = 0;
      this.max_household_income = 0;
      this.median_household_income = 0;
      this.first_quartile_household_income = 0;
      this.third_quartile_household_income = 0;

      int num_households = this.households.Count;
      if (num_households > 0)
      {
        this.min_household_income = this.get_household_ptr(0).get_household_income();
        this.max_household_income = this.get_household_ptr(num_households - 1).get_household_income();
        this.first_quartile_household_income = this.get_household_ptr(num_households / 4).get_household_income();
        this.median_household_income = this.get_household_ptr(num_households / 2).get_household_income();
        this.third_quartile_household_income = this.get_household_ptr(3 * num_households / 4).get_household_income();
      }

      // print household incomes to LOG file
      if (Global.Verbose > 1)
      {
        for (int i = 0; i < num_households; ++i)
        {
          var h = this.get_household_ptr(i);
          int c = h.get_county_index();
          int h_county = Global.Places.get_fips_of_county_with_index(c);
          Utils.FRED_VERBOSE(0, "INCOME: %s %c %f %f %d %d\n", h.get_label(), h.get_type(), h.get_latitude(),
              h.get_longitude(), h.get_household_income(), h_county);
        }
      }

      Utils.FRED_VERBOSE(0, "INCOME_STATS: households: %d  min %d  first_quartile %d  median %d  third_quartile %d  max %d\n",
          num_households, min_household_income, first_quartile_household_income, median_household_income,
          third_quartile_household_income, max_household_income);
    }

    private void select_households_for_shelter()
    {
      Utils.FRED_VERBOSE(0, "select_households_for_shelter entered.\n");
      Utils.FRED_VERBOSE(0, "pct_households_sheltering = %f\n", Pct_households_sheltering);
      Utils.FRED_VERBOSE(0, "num_households = %d\n", this.households.Count);
      int num_sheltering = Convert.ToInt32(0.5 + Pct_households_sheltering * this.households.Count);
      Utils.FRED_VERBOSE(0, "num_sheltering = %d\n", num_sheltering);
      Utils.FRED_VERBOSE(0, "high_income = %d\n", High_income_households_sheltering ? 1 : 0);

      int num_households = this.households.Count;

      if (High_income_households_sheltering)
      {
        // this assumes that household have been sorted in increasing income
        // in setup_households()
        for (int i = 0; i < num_sheltering; ++i)
        {
          int j = num_households - 1 - i;
          var h = get_household_ptr(j);
          shelter_household(h);
        }
      }
      else
      {
        // select households randomly
        var tmp = new List<Household>();
        for (int i = 0; i < this.households.Count; ++i)
        {
          tmp.Add(this.get_household_ptr(i));
        }
        // randomly shuffle households
        tmp.Shuffle();
        for (int i = 0; i < num_sheltering; ++i)
        {
          this.shelter_household(tmp[i]);
        }
      }
      Utils.FRED_VERBOSE(0, "select_households_for_shelter finished.\n");
    }

    private void shelter_household(Household h)
    {
      h.set_shelter(true);

      // set shelter delay
      int shelter_start_day = Convert.ToInt32(0.4999999 + FredRandom.Normal(Shelter_delay_mean, Shelter_delay_std));
      if (Early_shelter_rate > 0.0)
      {
        double r = FredRandom.NextDouble();
        while (shelter_start_day > 0 && r < Early_shelter_rate)
        {
          shelter_start_day--;
          r = FredRandom.NextDouble();
        }
      }
      if (shelter_start_day < 0)
      {
        shelter_start_day = 0;
      }
      h.set_shelter_start_day(shelter_start_day);

      // set shelter duration
      int shelter_duration = Convert.ToInt32(0.4999999 + FredRandom.Normal(Shelter_duration_mean, Shelter_duration_std));
      if (shelter_duration < 1)
      {
        shelter_duration = 1;
      }

      if (Shelter_decay_rate > 0.0)
      {
        double r = FredRandom.NextDouble();
        if (r < 0.5)
        {
          shelter_duration = 1;
          r = FredRandom.NextDouble();
          while (shelter_duration < Shelter_duration_mean && Shelter_decay_rate < r)
          {
            shelter_duration++;
            r = FredRandom.NextDouble();
          }
        }
      }
      h.set_shelter_end_day(shelter_start_day + shelter_duration);

      Utils.FRED_VERBOSE(1, "ISOLATE household %s size %d income %d ", h.get_label(), h.get_size(), h.get_household_income());
      Utils.FRED_VERBOSE(1, "start_day %d end_day %d duration %d ", h.get_shelter_start_day(), h.get_shelter_end_day(),
          h.get_shelter_end_day() - h.get_shelter_start_day());
    }

    private void select_households_for_evacuation()
    {
      if (!Global.Enable_HAZEL)
      {
        return;
      }

      Utils.FRED_VERBOSE(0, "HAZEL: select_households_for_evacuation entered.\n");
      int num_households = this.households.Count;
      int evac_start_sim_day = HAZEL_disaster_start_sim_day + HAZEL_disaster_evac_start_offset;
      int evac_end_sim_day = HAZEL_disaster_end_sim_day + HAZEL_disaster_evac_end_offset;
      int return_start_sim_day = HAZEL_disaster_end_sim_day + HAZEL_disaster_return_start_offset;
      int return_end_sim_day = HAZEL_disaster_end_sim_day + HAZEL_disaster_return_end_offset;
      int count_hh_evacuating = 0;

      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_start_sim_day = {0}", HAZEL_disaster_start_sim_day);
      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_evac_start_offset = {0}", HAZEL_disaster_evac_start_offset);
      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_end_sim_day = {0}", HAZEL_disaster_end_sim_day);
      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_evac_end_offset = {0}", HAZEL_disaster_evac_end_offset);
      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_return_start_offset = {0}", HAZEL_disaster_return_start_offset);
      Utils.FRED_VERBOSE(0, "HAZEL: HAZEL_disaster_return_end_offset = {0}", HAZEL_disaster_return_end_offset);
      Utils.FRED_VERBOSE(0, "HAZEL: evac_start_sim_day = {0}", evac_start_sim_day);
      Utils.FRED_VERBOSE(0, "HAZEL: evac_end_sim_day = {0}", evac_end_sim_day);
      Utils.FRED_VERBOSE(0, "HAZEL: return_start_sim_day = {0}", return_start_sim_day);
      Utils.FRED_VERBOSE(0, "HAZEL: return_end_sim_day = {0}", return_end_sim_day);
      if (evac_start_sim_day < 0 || evac_end_sim_day < evac_start_sim_day)
      {
        return;
      }

      for (int i = 0; i < num_households; ++i)
      {
        var tmp_hh = this.get_household_ptr(i);
        bool return_date_set = false;
        for (int j = evac_start_sim_day; j <= evac_end_sim_day; ++j)
        {
          if (FredRandom.NextDouble() < HAZEL_disaster_evac_prob_per_day)
          {
            tmp_hh.set_shelter_start_day(j);
            bool evac_date_set = true;
            count_hh_evacuating++;
            for (int k = return_start_sim_day; k <= return_end_sim_day; ++k)
            {
              if (FredRandom.NextDouble() < HAZEL_disaster_evac_prob_per_day || k == return_end_sim_day)
              {
                if (k > j)
                { //Can't return before you leave
                  tmp_hh.set_shelter_end_day(k);
                  return_date_set = true;
                }
              }
              if (return_date_set)
              {
                break;
              }
            }
            if (evac_date_set)
            {
              Utils.assert(return_date_set);
              break;
            }
          }
        }
      }

      Utils.FRED_VERBOSE(0, "HAZEL: num_households = {0}", num_households);
      Utils.FRED_VERBOSE(0, "HAZEL: num_evacuating = {0}", count_hh_evacuating);
      Utils.FRED_VERBOSE(0, "HAZEL: pct_households_evacuating = %f\n", count_hh_evacuating / num_households);
      Utils.FRED_VERBOSE(0, "HAZEL: select_households_for_evacuation finished.\n");
    }

    //private void evacuate_household(Household h);

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

    private bool add_place(Place p)
    {
      Utils.FRED_CONDITIONAL_VERBOSE(0, p.get_id() != -1, "Place id (%d) was overwritten!", p.get_id());
      Utils.assert(p.get_id() == -1);

      string str = p.get_label();

      if (!this.place_label_map.ContainsKey(str))
      {
        p.set_id(get_new_place_id());
        this.places.Add(p);
        this.place_label_map.Add(str, this.places.Count - 1);
        // printf("places now = %d\n", (int)(places.Count)); fflush(stdout);

        if (p.is_neighborhood())
        {
          this.neighborhoods.Add(p);
        }

        if (p.is_workplace())
        {
          this.workplaces.Add(p);
        }

        if (p.is_hospital())
        {
          this.hospitals.Add(p);
        }

        if (p.is_school())
        {
          this.schools.Add(p);
        }
        return true;
      }

      Console.WriteLine("WARNING: duplicate place label found: ");
      p.print(0);
      return false;
    }

    //private void parse_lines_from_stream(TextWriter stream, List<Place_Init_Data> pids);

    private string lookup_place_type_name(char place_type)
    {
      Utils.assert(this.place_type_name_lookup_map.ContainsKey(place_type));
      return this.place_type_name_lookup_map[place_type];
    }

    private void set_number_of_demes(int n)
    {
      this.number_of_demes = n;
    }

    private class HouseholdIncomeComparer : IComparer<Place>
    {
      public int Compare([AllowNull] Place x, [AllowNull] Place y)
      {
        int inc1 = ((Household)x).get_household_income();
        int inc2 = ((Household)y).get_household_income();
        return (inc1 == inc2) ? x.get_id().CompareTo(y.get_id()) : inc1.CompareTo(inc2);
      }
    }
  }
}
