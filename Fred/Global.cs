using System;
using System.Diagnostics;
using System.IO;

namespace Fred
{
  internal static class Global
  {
    public const int OUTPUT_S = 0;
    public const int OUTPUT_E = 1;
    public const int OUTPUT_I = 2;
    public const int OUTPUT_Is = 3;
    public const int OUTPUT_R = 4;
    public const int OUTPUT_C = 5;
    public const int OUTPUT_Cs = 6;
    public const int OUTPUT_D = 7;
    public const int OUTPUT_AR = 8;
    public const int OUTPUT_ARs = 9;
    public const int OUTPUT_P = 10;
    public const int OUTPUT_N = 11;
    public const int OUTPUT_HC_DEFICIT = 12;
    public const int OUTPUT_CF = 13;
    public const int OUTPUT_TCF = 13;

    // global constants
    public const int DAYS_PER_WEEK = 7;
    public const int ADULT_AGE = 18;
    public const int SCHOOL_AGE = 5;
    public const int RETIREMENT_AGE = 67;
    // MAX_NUM_DISEASES sets the size of stl::bitsets and static arrays used throughout FRED
    // to store disease-specific flags and pointers; set to the visualizationest possible value 
    // for optimal performance and memory usage
    public const int MAX_NUM_DISEASES = 4;
    // Change this constant and recompile to allow more threads.  For efficiency should be
    // equal to OMP_NUM_THREADS value that will be used.  If OMP_NUM_THREADS greater than
    // MAX_NUM_THREADS is used, FRED will abort the run.
    // public const int MAX_NUM_THREADS = NCPU;

    // race codes (ver 2)
    public const int WHITE = 1;
    public const int AFRICAN_AMERICAN = 2;
    public const int AMERICAN_INDIAN = 3;
    public const int ALASKA_NATIVE = 4;
    public const int TRIBAL = 5;
    public const int ASIAN = 6;
    public const int HAWAIIN_NATIVE = 7;
    public const int OTHER_RACE = 8;
    public const int MULTIPLE_RACE = 9;

    // household relationship codes (ver 2)
    public const int HOUSEHOLDER = 0;
    public const int SPOUSE = 1;
    public const int CHILD = 2;
    public const int SIBLING = 3;
    public const int PARENT = 4;
    public const int GRANDCHILD = 5;
    public const int IN_LAW = 6;
    public const int OTHER_RELATIVE = 7;
    public const int BOARDER = 8;
    public const int HOUSEMATE = 9;
    public const int PARTNER = 10;
    public const int FOSTER_CHILD = 11;
    public const int OTHER_NON_RELATIVE = 13;
    public const int INSTITUTIONALIZED_GROUP_QUARTERS_POP = 13;
    public const int NONINSTITUTIONALIZED_GROUP_QUARTERS_POP = 14;

    public const int LOG_LEVEL_MIN = 0;
    public const int LOG_LEVEL_LOW = 1;
    public const int LOG_LEVEL_MED = 2;
    public const int LOG_LEVEL_HIGH = 3;
    public const int LOG_LEVEL_MAX = 4;

    //Income quartile
    public const int Q1 = 1;
    public const int Q2 = 2;
    public const int Q3 = 3;
    public const int Q4 = 4;

    public static string Simulation_directory;
    public static int Simulation_run_number = 1;
    public static int Simulation_seed = 1;
    public static Stopwatch Simulation_Stopwatch = new Stopwatch();
    public static int Simulation_Day;

    // global runtime parameters
    public static string Synthetic_population_directory;
    public static string Synthetic_population_id;
    public static string Synthetic_population_version;
    public static string Population_directory;
    public static string Output_directory;
    public static string Tracefilebase;
    public static string VaccineTracefilebase;
    public static string Prevfilebase;
    public static string Incfilebase;
    public static string Immunityfilebase;
    public static string City;
    public static string County;
    public static string US_state;
    public static string FIPS_code;
    public static int Trace_Headers;
    public static int Rotate_start_date;
    public static int Quality_control;
    public static int RR_delay;
    //added for cbsa
    public static string MSA_code;

    public static string ErrorLogbase;
    public static bool Enable_Behaviors;
    public static int Track_infection_events;
    public static bool Track_age_distribution;
    public static bool Track_household_distribution;
    public static bool Track_network_stats;
    public static bool Track_Residual_Immunity;
    public static bool Report_Mean_Household_Income_Per_School;
    public static bool Report_Mean_Household_Size_Per_School;
    public static bool Report_Mean_Household_Distance_From_School;
    public static bool Report_Mean_Household_Stats_Per_Income_Category;
    public static bool Report_Epidemic_Data_By_Census_Tract;
    public static int Verbose;
    public static int Debug;
    public static int Test;
    public static int Days;
    public static int Reseed_day;
    public static int Seed;
    public static string Start_date;
    public static int Epidemic_offset;
    public static int Vaccine_offset;
    public static string Seasonality_Timestep;
    public static double Work_absenteeism;
    public static double School_absenteeism;

    //Boolean flags
    public static bool Enable_Health_Charts;
    public static bool Enable_Transmission_Network;
    public static bool Enable_Sexual_Partner_Network;
    public static bool Enable_Transmission_Bias;
    public static bool Enable_New_Transmission_Model;
    public static bool Enable_Hospitals;
    public static bool Enable_Health_Insurance;
    public static bool Enable_Group_Quarters;
    public static bool Enable_Visualization_Layer;
    public static bool Enable_Vector_Layer;
    public static bool Report_Vector_Population;
    public static bool Enable_Vector_Transmission;
    public static bool Enable_Population_Dynamics;
    public static bool Enable_Travel;
    public static bool Enable_Local_Workplace_Assignment;
    public static bool Enable_Seasonality;
    public static bool Enable_Climate;
    public static bool Enable_Chronic_Condition;
    public static bool Report_Immunity;
    public static bool Enable_Vaccination;
    public static bool Enable_Antivirals;
    public static bool Enable_Viral_Evolution;
    public static bool Enable_HAZEL;
    public static bool Enable_hh_income_based_susc_mod;
    public static bool Use_Mean_Latitude;
    public static bool Print_Household_Locations;
    public static int Report_Age_Of_Infection;
    public static int Age_Of_Infection_Log_Level;
    public static bool Report_Place_Of_Infection;
    public static bool Report_Distance_Of_Infection;
    public static bool Report_Presenteeism;
    public static bool Report_Childhood_Presenteeism;
    public static bool Report_Serial_Interval;
    public static bool Report_Incidence_By_County;
    public static bool Report_Incidence_By_Census_Tract;
    public static bool Report_Symptomatic_Incidence_By_Census_Tract;
    public static bool Report_County_Demographic_Information;
    public static bool Assign_Teachers;
    public static bool Enable_Household_Shelter;
    public static bool Enable_Isolation;
    public static int Isolation_Delay;
    public static double Isolation_Rate;
    public static string PSA_Method;
    public static string PSA_List_File;
    public static int PSA_Sample_Size;
    public static int PSA_Sample;
    // for residual immunity by FIPS
    public static bool Residual_Immunity_by_FIPS;
    public static string Residual_Immunity_File;

    // global singleton objects
    public static readonly Population Pop = new Population();
    public static readonly Disease_List Diseases = new Disease_List();
    public static readonly Place_List Places = new Place_List();
    public static Neighborhood_Layer Neighborhoods;
    public static Regional_Layer Simulation_Region;
    public static Visualization_Layer Visualization;
    public static Vector_Layer Vectors;
    public static readonly Evolution Evol = new Evolution();
    public static Seasonality Clim;
    public static Tracker<int> Daily_Tracker = new Tracker<int>();
    public static Tracker<long> Tract_Tracker = new Tracker<long>();
    public static Tracker<int> Income_Category_Tracker = new Tracker<int>();
    public static int[] Popsize_by_age = new int[Demographics.MAX_AGE + 1];
    public static Network Transmission_Network;
    public static Sexual_Transmission_Network Sexual_Partner_Network;

    // global file pointers
    public static TextWriter Statusfp;
    public static TextWriter Outfp;
    public static TextWriter Tracefp;
    public static TextWriter Infectionfp;
    public static TextWriter VaccineTracefp;
    public static TextWriter Birthfp;
    public static TextWriter Deathfp;
    public static TextWriter Prevfp;
    public static TextWriter Incfp;
    public static TextWriter ErrorLogfp;
    public static TextWriter Immunityfp;
    public static TextWriter Householdfp;
    public static TextWriter Tractfp;
    public static TextWriter IncomeCatfp;

    /**
     * Fills the static variables with values from the parameter file.
     */
    public static void get_global_parameters()
    {
      FredParameters.GetParameter("verbose", ref Verbose);
      FredParameters.GetParameter("debug", ref Debug);
      FredParameters.GetParameter("test", ref Test);
      FredParameters.GetParameter("quality_control", ref Quality_control);
      FredParameters.GetParameter("rr_delay", ref RR_delay);
      FredParameters.GetParameter("days", ref Days);
      FredParameters.GetParameter("seed", ref Seed);
      FredParameters.GetParameter("epidemic_offset", ref Epidemic_offset);
      FredParameters.GetParameter("vaccine_offset", ref Vaccine_offset);
      FredParameters.GetParameter("start_date", ref Start_date);
      FredParameters.GetParameter("rotate_start_date", ref Rotate_start_date);
      FredParameters.GetParameter("reseed_day", ref Reseed_day);
      FredParameters.GetParameter("outdir", ref Output_directory);
      FredParameters.GetParameter("tracefile", ref Tracefilebase);
      FredParameters.GetParameter("track_infection_events", ref Track_infection_events);

      FredParameters.GetParameter("vaccine_tracefile", ref VaccineTracefilebase);
      FredParameters.GetParameter("trace_headers", ref Trace_Headers);
      FredParameters.GetParameter("immunity_file", ref Immunityfilebase);
      FredParameters.GetParameter("seasonality_timestep_file", ref Seasonality_Timestep);
      FredParameters.GetParameter("work_absenteeism", ref Work_absenteeism);
      FredParameters.GetParameter("school_absenteeism", ref School_absenteeism);

      //Set all of the boolean flags
      int temp_int = 0;
      FredParameters.GetParameter("enable_behaviors", ref temp_int);
      Enable_Behaviors = temp_int != 0;
      FredParameters.GetParameter("track_age_distribution", ref temp_int);
      Track_age_distribution = temp_int != 0;
      FredParameters.GetParameter("track_household_distribution", ref temp_int);
      Track_household_distribution = temp_int != 0;
      FredParameters.GetParameter("track_network_stats", ref temp_int);
      Track_network_stats = temp_int != 0;
      FredParameters.GetParameter("track_residual_immunity", ref temp_int);
      Track_Residual_Immunity = temp_int != 0;
      FredParameters.GetParameter("report_mean_household_income_per_school", ref temp_int);
      Report_Mean_Household_Income_Per_School = temp_int != 0;
      FredParameters.GetParameter("report_mean_household_size_per_school", ref temp_int);
      Report_Mean_Household_Size_Per_School = temp_int != 0;
      FredParameters.GetParameter("report_mean_household_distance_from_school", ref temp_int);
      Report_Mean_Household_Distance_From_School = temp_int != 0;
      FredParameters.GetParameter("enable_health_charts", ref temp_int);
      Enable_Health_Charts = temp_int != 0;
      FredParameters.GetParameter("enable_transmission_network", ref temp_int);
      Enable_Transmission_Network = temp_int != 0;
      FredParameters.GetParameter("enable_sexual_partner_network", ref temp_int);
      Enable_Sexual_Partner_Network = temp_int != 0;
      FredParameters.GetParameter("enable_transmission_bias", ref temp_int);
      Enable_Transmission_Bias = temp_int != 0;
      FredParameters.GetParameter("enable_new_transmission_model", ref temp_int);
      Enable_New_Transmission_Model = temp_int != 0;
      FredParameters.GetParameter("report_mean_household_stats_per_income_category", ref temp_int);
      Report_Mean_Household_Stats_Per_Income_Category = temp_int != 0;
      FredParameters.GetParameter("report_epidemic_data_by_census_tract", ref temp_int);
      Report_Epidemic_Data_By_Census_Tract = temp_int != 0;
      FredParameters.GetParameter("enable_hospitals", ref temp_int);
      Enable_Hospitals = temp_int != 0;
      FredParameters.GetParameter("enable_health_insurance", ref temp_int);
      Enable_Health_Insurance = temp_int != 0;
      FredParameters.GetParameter("enable_group_quarters", ref temp_int);
      Enable_Group_Quarters = temp_int != 0;
      FredParameters.GetParameter("enable_visualization_layer", ref temp_int);
      Enable_Visualization_Layer = temp_int != 0;
      FredParameters.GetParameter("enable_vector_layer", ref temp_int);
      Enable_Vector_Layer = temp_int != 0;
      FredParameters.GetParameter("enable_vector_transmission", ref temp_int);
      Enable_Vector_Transmission = temp_int != 0;
      FredParameters.GetParameter("report_vector_population", ref temp_int);
      Report_Vector_Population = temp_int != 0;
      FredParameters.GetParameter("enable_population_dynamics", ref temp_int);
      Enable_Population_Dynamics = temp_int != 0;
      FredParameters.GetParameter("enable_travel", ref temp_int);
      Enable_Travel = temp_int != 0;
      FredParameters.GetParameter("enable_local_workplace_assignment", ref temp_int);
      Enable_Local_Workplace_Assignment = temp_int != 0;
      FredParameters.GetParameter("enable_seasonality", ref temp_int);
      Enable_Seasonality = temp_int != 0;
      FredParameters.GetParameter("enable_climate", ref temp_int);
      Enable_Climate = temp_int != 0;
      FredParameters.GetParameter("enable_chronic_condition", ref temp_int);
      Enable_Chronic_Condition = temp_int != 0;
      FredParameters.GetParameter("enable_vaccination", ref temp_int);
      Enable_Vaccination = temp_int != 0;
      FredParameters.GetParameter("enable_antivirals", ref temp_int);
      Enable_Antivirals = temp_int != 0;
      FredParameters.GetParameter("enable_viral_evolution", ref temp_int);
      Enable_Viral_Evolution = temp_int != 0;
      FredParameters.GetParameter("enable_HAZEL", ref temp_int);
      Enable_HAZEL = temp_int != 0;
      FredParameters.GetParameter("enable_hh_income_based_susc_mod", ref temp_int);
      Enable_hh_income_based_susc_mod = temp_int != 0;
      FredParameters.GetParameter("use_mean_latitude", ref temp_int);
      Use_Mean_Latitude = temp_int != 0;
      FredParameters.GetParameter("print_household_locations", ref temp_int);
      Print_Household_Locations = temp_int != 0;
      FredParameters.GetParameter("assign_teachers", ref temp_int);
      Assign_Teachers = temp_int != 0;

      FredParameters.GetParameter("report_age_of_infection", ref Report_Age_Of_Infection);
      FredParameters.GetParameter("age_of_infection_log_level", ref Age_Of_Infection_Log_Level);
      FredParameters.GetParameter("report_place_of_infection", ref temp_int);
      Report_Place_Of_Infection = temp_int != 0;
      FredParameters.GetParameter("report_distance_of_infection", ref temp_int);
      Report_Distance_Of_Infection = temp_int != 0;
      FredParameters.GetParameter("report_presenteeism", ref temp_int);
      Report_Presenteeism = temp_int != 0;
      FredParameters.GetParameter("report_childhood_presenteeism", ref temp_int);
      Report_Childhood_Presenteeism = temp_int != 0;
      FredParameters.GetParameter("report_serial_interval", ref temp_int);
      Report_Serial_Interval = temp_int != 0;
      FredParameters.GetParameter("report_incidence_by_county", ref temp_int);
      Report_Incidence_By_County = temp_int != 0;
      FredParameters.GetParameter("report_incidence_by_census_tract", ref temp_int);
      Report_Incidence_By_Census_Tract = temp_int != 0;
      FredParameters.GetParameter("report_symptomatic_incidence_by_census_tract", ref temp_int);
      Report_Symptomatic_Incidence_By_Census_Tract = temp_int != 0;
      FredParameters.GetParameter("report_county_demographic_information", ref temp_int);
      Report_County_Demographic_Information = temp_int != 0;
      FredParameters.GetParameter("enable_shelter_in_place", ref temp_int);
      Enable_Household_Shelter = temp_int != 0;
      FredParameters.GetParameter("enable_isolation", ref temp_int);
      Enable_Isolation = temp_int != 0;
      FredParameters.GetParameter("isolation_delay", ref Isolation_Delay);
      FredParameters.GetParameter("isolation_rate", ref Isolation_Rate);
      // added for residual_immunity_by_FIPS
      FredParameters.GetParameter("enable_residual_immunity_by_FIPS", ref temp_int);
      Residual_Immunity_by_FIPS = temp_int != 0;
      if (Residual_Immunity_by_FIPS)
      {
        FredParameters.GetParameter("residual_immunity_by_FIPS_file", ref Residual_Immunity_File);
      }
    }
  }
}
