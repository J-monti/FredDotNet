using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fred
{
  public class FredMain
  {
    public int run()
    {
      fred_setup();
      for (Global.Simulation_Day = 0; Global.Simulation_Day < Global.Days; Global.Simulation_Day++)
      {
        fred_step(Global.Simulation_Day);
      }
      fred_finish();
      return 0;
    }


    private void fred_setup()
    {
      var argv = Environment.GetCommandLineArgs();
      int argc = argv.Length;
      string paramfile;

      Global.Simulation_Day = 0;
      Global.Statusfp = Console.Out;
      Utils.fred_print_wall_time("FRED started");
      //Utils.fred_start_initialization_timer();
      Utils.fred_start_timer();

      // read optional param file name from command line
      int index = Array.IndexOf(argv, "-params");
      if (index != -1 && argc > index)
      {
        paramfile = argv[index + 1];
      }
      else
      {
        paramfile = Utils.get_fred_file_name("$FRED_HOME/input_files/defaults");
      }
      Utils.FRED_STATUS(0,"param file = %s\n", paramfile);

      // read optional run number from command line (must be 2nd arg)
      index = Array.IndexOf(argv, "-run");
      if (index != -1 && argc > index)
      {
        Global.Simulation_run_number = Convert.ToInt32(argv[index + 1]);
      }
      else
      {
        Global.Simulation_run_number = 1;
      }

      // read optional working directory from command line (must be 3rd arg)
      index = Array.IndexOf(argv, "-sim_dir");
      if (index != -1 && argc > index)
      {
        Global.Simulation_directory = argv[index + 1];
      }
      else
      {
        Global.Simulation_directory = "";
      }

      // get runtime parameters
      FredParameters.read_parameters(paramfile);
      Global.get_global_parameters();
      Date.setup_dates(Global.Start_date);

      // create diseases and read parameters
      Global.Diseases.get_parameters();
      Transmission.get_parameters();

      Global.Pop.get_parameters();
      Utils.fred_print_lap_time("get_parameters");

      if (string.IsNullOrWhiteSpace(Global.Simulation_directory))
      {
        // use the directory in the params file
        Global.Simulation_directory = Global.Output_directory;
        //printf("Global.Simulation_directory = %s\n",Global.Simulation_directory);
      }
      else
      {
        // change the Output_directory
        Global.Output_directory = Global.Simulation_directory;
        Utils.FRED_STATUS(0, "Overridden from command line: Output_directory = {0}", Global.Output_directory);
      }

      // create the output directory, if necessary
      Directory.CreateDirectory(Global.Simulation_directory);

      // open output files with global file pointers
      Utils.fred_open_output_files();

      // set random number seed based on run number
      if (Global.Simulation_run_number > 1 && Global.Reseed_day == -1)
      {
        Global.Simulation_seed = Global.Seed * 100 + (Global.Simulation_run_number - 1);
      }
      else
      {
        Global.Simulation_seed = Global.Seed;
      }
      Utils.FRED_STATUS(0,"seed = %lu\n", Global.Simulation_seed);
      FredRandom.SetSeed(Global.Simulation_seed);

      Utils.fred_print_lap_time("RNG setup");
      Utils.fred_print_wall_time("\nFRED run %d started", Global.Simulation_run_number);

      // initializations

      // Initializes Synthetic Population parameters, determines the synthetic
      // population id if the city or county was specified as a parameter
      // Must be called BEFORE Pop.split_synthetic_populations_by_deme() because
      // city/county population lookup may overwrite Global.Synthetic_population_id
      Global.Places.get_parameters();

      // split the population id parameter string ( that was initialized in 
      // Places.get_parameters ) on whitespace; each population id is processed as a
      // separate deme, and stored in the Population object.
      Global.Pop.split_synthetic_populations_by_deme();

      // Loop over all Demes and read in the household, schools and workplaces
      // and setup geographical layers
      Utils.fred_print_wall_time("\nFRED read_places started");
      Global.Places.read_all_places(Global.Pop.get_demes());
      Utils.fred_print_lap_time("Places.read_places");
      Utils.fred_print_wall_time("FRED read_places finished");

      // create visualization layer, if requested
      if (Global.Enable_Visualization_Layer)
      {
        Global.Visualization = new Visualization_Layer();
      }

      // create vector layer, if requested
      if (Global.Enable_Vector_Layer)
      {
        Global.Vectors = new Vector_Layer();
      }

      // initialize parameters and other static variables
      //Demographics.initialize_static_variables();
      //Activities.initialize_static_variables();
      //Behavior.initialize_static_variables();
      //Health.initialize_static_variables();
      Utils.fred_print_lap_time("initialize_static_variables");

      // finished setting up Diseases
      Global.Diseases.setup();
      Utils.fred_print_lap_time("Diseases.setup");

      // read in the population and have each person enroll
      // in each daily activity location identified in the population file
      Utils.fred_print_wall_time("\nFRED Pop.setup started");
      Global.Pop.setup();
      Utils.fred_print_wall_time("FRED Pop.setup finished");
      Utils.fred_print_lap_time("Pop.setup");
      Global.Places.setup_group_quarters();
      Utils.fred_print_lap_time("Places.setup_group_quarters");
      Global.Places.setup_households();
      Utils.fred_print_lap_time("Places.setup_households");


      // define FRED-specific places and have each person enroll as needed

      // classrooms
      Global.Places.setup_classrooms();
      Global.Pop.assign_classrooms();
      Utils.fred_print_lap_time("assign classrooms");

      // reassign workers (to schools, hospitals, groups quarters, etc)
      Global.Places.reassign_workers();
      Utils.fred_print_lap_time("reassign workers");

      // offices
      Global.Places.setup_offices();
      Utils.fred_print_lap_time("setup_offices");
      Global.Pop.assign_offices();
      Utils.fred_print_lap_time("assign offices");

      // after all enrollments, prepare to receive visitors
      Global.Places.prepare();
      Utils.fred_print_lap_time("place preparation");

      if (Global.Enable_Hospitals)
      {
        Global.Places.assign_hospitals_to_households();
        Utils.fred_print_lap_time("assign hospitals to households");
        if (Global.Enable_HAZEL)
        {
          Global.Pop.assign_primary_healthcare_facilities();
          Utils.fred_print_lap_time("assign primary healthcare to agents");
          Global.Places.setup_HAZEL_mobile_vans();
        }
      }

      Utils.FRED_STATUS(0, "deleting place_label_map\n", "");
      Global.Places.delete_place_label_map();
      Utils.FRED_STATUS(0, "prepare places finished\n", "");

      if (Global.Enable_Vector_Layer)
      {
        Global.Vectors.setup();
        Utils.fred_print_lap_time("Vectors.setup");
      }

      // create networks if needed
      if (Global.Enable_Transmission_Network)
      {
        Global.Transmission_Network = new Network("Transmission_Network");
        //Global.Transmission_Network.test();
      }

      if (Global.Enable_Sexual_Partner_Network)
      {
        Global.Sexual_Partner_Network = new Sexual_Transmission_Network("Sexual_Partner_Network");
        Sexual_Transmission_Network.get_parameters();
        //Global.Sexual_Partner_Network.test();
      }

      if (Global.Enable_Travel)
      {
        Utils.fred_print_wall_time("\nFRED Travel setup started");
        Global.Simulation_Region.set_population_size();
        Travel.setup(Global.Simulation_directory);
        Utils.fred_print_lap_time("Travel setup");
        Utils.fred_print_wall_time("FRED Travel setup finished");
      }

      if (Global.Quality_control != 0)
      {
        Global.Pop.quality_control();
        Global.Places.quality_control();
        Global.Simulation_Region.quality_control();
        Global.Neighborhoods.quality_control();
        if (Global.Enable_Visualization_Layer)
        {
          Global.Visualization.quality_control();
        }
        if (Global.Enable_Vector_Layer)
        {
          Global.Vectors.quality_control();
          Global.Simulation_Region.set_population_size();
          Global.Vectors.swap_county_people();
        }
        if (Global.Track_network_stats)
        {
          Global.Pop.get_network_stats(Global.Simulation_directory);
        }
        Utils.fred_print_lap_time("quality control");
      }

      if (Global.Track_age_distribution)
      {
        /*
          Global.Pop.print_age_distribution(Global.Simulation_directory,
          (char *) Global.Sim_Start_Date.get_YYYYMMDD().c_str(),
          Global.Simulation_run_number);
        */
      }

      if (Global.Enable_Seasonality)
      {
        Global.Clim.print_summary();
      }

      if (Global.Report_Mean_Household_Income_Per_School)
      {
        Global.Pop.report_mean_hh_income_per_school();
      }

      if (Global.Report_Mean_Household_Size_Per_School)
      {
        Global.Pop.report_mean_hh_size_per_school();
      }

      if (Global.Report_Mean_Household_Distance_From_School)
      {
        Global.Pop.report_mean_hh_distance_from_school();
      }

      if (Global.Report_Childhood_Presenteeism)
      {
        Global.Pop.set_school_income_levels();
        Global.Places.setup_school_income_quartile_pop_sizes();
        //Global.Places.setup_household_income_quartile_sick_days();
      }

      if (Global.Enable_hh_income_based_susc_mod)
      {
        int pct_90_min = Global.Places.get_min_household_income_by_percentile(90);
        Household.set_min_hh_income_90_pct(pct_90_min);
      }

      if (Global.Report_Mean_Household_Stats_Per_Income_Category &&
         Global.Report_Epidemic_Data_By_Census_Tract)
      {
        Global.Income_Category_Tracker = new Tracker<int>("Income Category Tracker", "income_cat");
        Global.Tract_Tracker = new Tracker<long>("Census Tract Tracker", "Tract");
        Global.Pop.report_mean_hh_stats_per_income_category_per_census_tract();
      }
      else if (Global.Report_Mean_Household_Stats_Per_Income_Category)
      {
        Global.Income_Category_Tracker = new Tracker<int>("Income Category Tracker", "income_cat");
        Global.Pop.report_mean_hh_stats_per_income_category();
      }
      else if (Global.Report_Epidemic_Data_By_Census_Tract)
      {
        Global.Tract_Tracker = new Tracker<long>("Census Tract Tracker", "Tract");
        Global.Pop.report_mean_hh_stats_per_census_tract();
      }

      //  //TODO - remove this
      //  if(Global.Enable_HAZEL) {
      //    Global.Pop.print_HAZEL_data();
      //  }

      // Global tracker allows us to have as many variables we
      // want from wherever in the output file
      Global.Daily_Tracker = new Tracker<int>("Main Daily Tracker", "Day");

      // prepare diseases after population is all set up
      Utils.FRED_VERBOSE(0, "prepare diseases\n");
      Global.Diseases.prepare_diseases();
      Utils.fred_print_lap_time("prepare_diseases");

      if (Global.Enable_Vector_Layer)
      {
        Global.Vectors.init_prior_immunity_by_county();
        for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
        {
          Global.Vectors.init_prior_immunity_by_county(d);
        }
        Utils.fred_print_lap_time("vector_layer_initialization");
      }

      // initialize visualization data if desired
      if (Global.Enable_Visualization_Layer)
      {
        Global.Visualization.initialize();
      }

      // initialize generic activities
      Activities.before_run();

      // initialize sexual transmission network if needed
      if (Global.Enable_Sexual_Partner_Network)
      {
        Global.Sexual_Partner_Network.setup();
      }

      Utils.fred_print_wall_time("FRED initialization complete");

      Utils.fred_start_sim_timer();
      Utils.fred_print_initialization_timer();
    }

    private void fred_step(int day)
    {
      Utils.fred_start_day_timer();

      // optional: reseed the random number generator to create alternative
      // simulation form a given initial point
      if (day == Global.Reseed_day)
      {
        Utils.FRED_STATUS(0,"************** reseed day = %d\n", day);
        FredRandom.SetSeed(Global.Simulation_seed + Global.Simulation_run_number - 1);
      }

      // optional: periodically output distributions of the population demographics
      if (Global.Track_age_distribution)
      {
        if (Date.get_month() == 1 && Date.get_day_of_month() == 1)
        {
          /*
      char date_string[80];
      strcpy(date_string, (char *) Global.Sim_Current_Date.get_YYYYMMDD().c_str());
      Global.Pop.print_age_distribution(Global.Simulation_directory, date_string, Global.Simulation_run_number);
      Global.Places.print_household_size_distribution(Global.Simulation_directory, date_string, Global.Simulation_run_number);
          */
        }
      }

      // reset lists of infectious, susceptibles; update vector population, if any
      Global.Places.update(day);
      Utils.fred_print_lap_time("day %d update places", day);

      // optional: update population dynamics 
      if (Global.Enable_Population_Dynamics)
      {
        Demographics.update(day);
        Utils.fred_print_lap_time("day %d update demographics", day);
        Global.Places.update_population_dynamics(day);
        Utils.fred_print_lap_time("day %d update population dynamics", day);
      }

      // update everyone's health intervention status
      if (Global.Enable_Vaccination || Global.Enable_Antivirals)
      {
        Global.Pop.update_health_interventions(day);
      }

      // remove dead from population
      Global.Pop.remove_dead_from_population(day);

      // update activity profiles on July 1
      if (Global.Enable_Population_Dynamics && Date.get_month() == 7 && Date.get_day_of_month() == 1)
      {
      }

      // Update vector dynamics
      if (Global.Enable_Vector_Layer)
      {
        Global.Vectors.update(day);
      }

      // update travel decisions
      Travel.update_travel(day);

      if (Global.Enable_Behaviors)
      {
        // update decisions about behaviors
      }

      // distribute vaccines
      Global.Pop.vacc_manager.update(day);

      // distribute AVs
      Global.Pop.av_manager.update(day);

      // update generic activities (individual activities updated only if
      // needed -- see below)
      Activities.update(day);

      // shuffle the order of diseases to reduce systematic bias
      List<int> order = new List<int>();
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
      {
        order.Add(d);
      }
      if (Global.Diseases.get_number_of_diseases() > 1)
      {
        order.Shuffle();
      }

      // transmit each disease in turn
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
      {
        int disease_id = order[d];
        var disease = Global.Diseases.get_disease(disease_id);
        disease.update(day);
        Utils.fred_print_lap_time("day %d update epidemic for disease %d", day, disease_id);
      }

      // print daily report
      Global.Pop.report(day);
      Utils.fred_print_lap_time("day %d report population", day);

      if (Global.Enable_HAZEL)
      {
        //Activities.print_stats(day);
        Global.Places.print_stats(day);
      }

      // print visualization data if desired
      if (Global.Enable_Visualization_Layer)
      {
        Global.Visualization.print_visualization_data(day);
        Utils.fred_print_lap_time("day %d print_visualization_data", day);
      }

      // optional: report change in demographics at end of each year
      if (Global.Enable_Population_Dynamics && Global.Verbose != 0
         && Date.get_month() == 12 && Date.get_day_of_month() == 31)
      {
        Global.Pop.quality_control();
      }

      // optional: report County demographics at end of each year
      if (Global.Report_County_Demographic_Information && Date.get_month() == 12 && Date.get_day_of_month() == 31)
      {
        Global.Places.report_county_populations();
      }

      Global.Infectionfp.Flush();

      // print daily reports
      //Utils.fred_print_resource_usage(day);
      Utils.fred_print_wall_time("day %d finished", day);
      Utils.fred_print_day_timer(day);
      Global.Daily_Tracker.output_inline_report_format_for_index(day, Global.Outfp);

      // advance date counter
      Date.update();
    }

    private void fred_finish()
    {
      //Global.Daily_Tracker.create_full_log(10,cout);
      Global.Infectionfp.Flush();

      // final reports
      if (Global.Report_Mean_Household_Stats_Per_Income_Category && Global.Report_Epidemic_Data_By_Census_Tract)
      {
        Global.Tract_Tracker.output_csv_report_format(Global.Tractfp);
        Global.Income_Category_Tracker.output_csv_report_format(Global.IncomeCatfp);
      }
      else if (Global.Report_Mean_Household_Stats_Per_Income_Category)
      {
        Global.Income_Category_Tracker.output_csv_report_format(Global.IncomeCatfp);
      }
      else if (Global.Report_Epidemic_Data_By_Census_Tract)
      {
        Global.Tract_Tracker.output_csv_report_format(Global.Tractfp);
      }
      Activities.end_of_run();

      // report timing info
      Utils.fred_print_sim_time();
      Utils.fred_print_wall_time("FRED finished");
      //Utils.fred_print_finish_timer();

      Global.Pop.end_of_run();
      Global.Places.end_of_run();
      Global.Diseases.end_of_run();

      if (Global.Enable_Transmission_Network)
      {
        // Global.Transmission_Network.print();
      }

      if (Global.Enable_Sexual_Partner_Network)
      {
        // Global.Sexual_Partner_Network.print();
      }

      // close all open output files with global file pointers
      Utils.fred_end();
    }
  }
}
