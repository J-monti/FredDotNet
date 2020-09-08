using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Fred
{
  public class Visualization_Layer : Abstract_Grid
  {
    private Visualization_Patch[,] grid;            // Rectangular array of patches
    private int max_grid_size;        // maximum number of rows or columns
    private int gaia_mode;           // if set, collect data for GAIA
    private int household_mode;     // if set, collect data for households
    private int census_tract_mode;  // if set, collect data for census_tract
                                   // vector<point> infected_households;
                                   // vector<point> all_households;
    private List<Place> households = new List<Place>();
    private Dictionary<long, long> census_tract = new Dictionary<long, long>();
    private Dictionary<long, long> census_tract_pop = new Dictionary<long, long>();

    public Visualization_Layer()
    {
      this.rows = 0;
      this.cols = 0;

      FredParameters.GetParameter("gaia_visualization_mode", ref this.gaia_mode);
      FredParameters.GetParameter("household_visualization_mode", ref this.household_mode);
      FredParameters.GetParameter("census_tract_visualization_mode", ref this.census_tract_mode);

      if (this.gaia_mode != 0)
      {
        // create visualization grid
        var base_grid = Global.Simulation_Region;
        this.min_lat = base_grid.get_min_lat();
        this.min_lon = base_grid.get_min_lon();
        this.max_lat = base_grid.get_max_lat();
        this.max_lon = base_grid.get_max_lon();
        this.min_x = base_grid.get_min_x();
        this.min_y = base_grid.get_min_y();
        this.max_x = base_grid.get_max_x();
        this.max_y = base_grid.get_max_y();

        // determine patch size for this layer
        FredParameters.GetParameter("visualization_grid_size", ref this.max_grid_size);
        if (this.max_x - this.min_x > this.max_y - this.min_y)
        {
          this.patch_size = (this.max_x - this.min_x) / (double)this.max_grid_size;
        }
        else
        {
          this.patch_size = (this.max_y - this.min_y) / (double)this.max_grid_size;
        }
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
          Utils.FRED_STATUS(0, "Visualization_Layer min_lon = %f\n", this.min_lon);
          Utils.FRED_STATUS(0, "Visualization_Layer min_lat = %f\n", this.min_lat);
          Utils.FRED_STATUS(0, "Visualization_Layer max_lon = %f\n", this.max_lon);
          Utils.FRED_STATUS(0, "Visualization_Layer max_lat = %f\n", this.max_lat);
          Utils.FRED_STATUS(0, "Visualization_Layer rows = %d  this.cols = %d\n", this.rows, this.cols);
          Utils.FRED_STATUS(0, "Visualization_Layer min_x = %f  min_y = %f\n", this.min_x, this.min_y);
          Utils.FRED_STATUS(0, "Visualization_Layer max_x = %f  max_y = %f\n", this.max_x, this.max_y);
        }

        this.grid = new Visualization_Patch[this.rows, this.cols];
        for (int i = 0; i < this.rows; ++i)
        {
          for (int j = 0; j < this.cols; ++j)
          {
            this.grid[i, j] = new Visualization_Patch();
            this.grid[i, j].setup(i, j, this.patch_size, this.min_x, this.min_y);
          }
        }
      }
    }

    //public Visualization_Patch** get_neighbors(int row, int col);
    public Visualization_Patch get_patch(int row, int col)
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
    public Visualization_Patch get_patch(FredGeo lat, FredGeo lon)
    {
      int row = get_row(lat);
      int col = get_col(lon);
      return get_patch(row, col);
    }

    public Visualization_Patch get_patch(double x, double y)
    {
      int row = get_row(y);
      int col = get_col(x);
      return get_patch(row, col);
    }

    public Visualization_Patch select_random_patch()
    {
      int row = FredRandom.Next(0, this.rows - 1);
      int col = FredRandom.Next(0, this.cols - 1);
      return this.grid[row, col];
    }

    public void quality_control()
    {
      if (Global.Verbose != 0)
      {
        Utils.FRED_STATUS(0, "visualization grid quality control check\n");
      }

      if (this.gaia_mode != 0)
      {
        for (int row = 0; row < this.rows; ++row)
        {
          for (int col = 0; col < this.cols; ++col)
          {
            this.grid[row, col].quality_control();
          }
        }
        if (Global.Verbose > 1 && this.rows > 0)
        {
          var filename = $"{Global.Simulation_directory}/visualization_grid.dat";
          using var fp = new StreamWriter(filename);
          for (int row = 0; row < this.rows; ++row)
          {
            if (row % 2 != 0)
            {
              for (int col = this.cols - 1; col >= 0; --col)
              {
                double x = this.grid[row, col].get_center_x();
                double y = this.grid[row, col].get_center_y();
                fp.WriteLine("{0} {1}", x, y);
              }
            }
            else
            {
              for (int col = 0; col < this.cols; ++col)
              {
                double x = this.grid[row, col].get_center_x();
                double y = this.grid[row, col].get_center_y();
                fp.WriteLine("{0} {1}", x, y);
              }
            }
          }
          fp.Flush();
          fp.Dispose();
        }
      }

      if (Global.Verbose != 0)
      {
        Utils.FRED_STATUS(0, "visualization grid quality control finished\n");
      }
    }

    public void add_census_tract(long tract)
    {
      census_tract.Add(tract, 0);
      census_tract_pop.Add(tract, 0);
    }

    public void initialize()
    {
      var vis_top_dir = $"{Global.Simulation_directory}/VIS";
      create_data_directories(vis_top_dir);
      // create visualization data directory
      if (this.gaia_mode != 0)
      {
        vis_top_dir = $"{Global.Simulation_directory}/GAIA";
        create_data_directories(vis_top_dir);
        // create GAIA setup file
        var setup_file = $"{vis_top_dir}/grid.txt";
        using var fp = new StreamWriter(setup_file);
        fp.WriteLine("rows = {0}", this.rows);
        fp.WriteLine("cols = {0}", this.cols);
        fp.WriteLine("min_lat = {0}", this.min_lat);
        fp.WriteLine("min_lon = {0}", this.min_lon);
        fp.WriteLine("patch_x_size = {0}", Geo.xsize_to_degree_longitude(patch_size));
        fp.WriteLine("patch_y_size = {0}", Geo.ysize_to_degree_latitude(patch_size));
        fp.Flush();
        fp.Close();
      }
    }

    public void create_data_directories(string vis_top_dir)
    {
      string vis_run_dir = string.Empty;
      string vis_dis_dir = string.Empty;
      string vis_var_dir = string.Empty;

      // make top level data directory
      Directory.CreateDirectory(vis_top_dir);

      // make directory for this run
      vis_run_dir= string.Format("{0}/run{1}", vis_top_dir, Global.Simulation_run_number);
      Directory.CreateDirectory(vis_run_dir);

      // create sub directories for diseases and output vars
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
      {
        vis_dis_dir = string.Format("{0}/dis{1}", vis_run_dir, d);
        Directory.CreateDirectory(vis_dis_dir);

        if (d == 0)
        {
          // print out household locations
          string filename = string.Format("{0}/households.txt", vis_dis_dir);
          using var fp = new StreamWriter(filename);
          int num_households = this.households.Count;
          for (int i = 0; i < num_households; ++i)
          {
            var h = this.households[i];
            fp.WriteLine("{0} {1} {2} {3}", h.get_latitude(), h.get_longitude(), h.get_size(), h.get_label());
          }
          fp.Flush();
          fp.Close();
        }
        else
        {
          // create symbolic links
          //var cmd = string.Format("ln -s {0}/dis0/households.txt {1}/households.txt", vis_run_dir, vis_dis_dir);
          var args = string.Format("-s {0}/dis0/households.txt {1}/households.txt", vis_run_dir, vis_dis_dir);
          var cmd = new ProcessStartInfo { FileName = "ln", Arguments = args };
          var process = Process.Start(cmd);
          process.WaitForExit();
          if (process.ExitCode != 0)//system(cmd) != 0)
          {
            Utils.fred_abort("Error using system command \"%s\"\n", cmd);
          }
        }

        // create directories for specific output variables
        vis_var_dir = string.Format("{0}/I", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/Is", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/C", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/Cs", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/P", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/N", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/R", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/Vec", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/D", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/CF", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        vis_var_dir = string.Format("{0}/TCF", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);

        if (this.household_mode != 0 && Global.Enable_HAZEL)
        {
          vis_var_dir = string.Format("{0}/HH_primary_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
          vis_var_dir = string.Format("{0}/HH_accept_insr_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
          vis_var_dir = string.Format("{0}/HH_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
        }

        if (this.census_tract_mode != 0 && Global.Enable_HAZEL)
        {
          vis_var_dir = string.Format("{0}/HC_DEFICIT", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
        }
      }
    }

    public void print_visualization_data(int day)
    {
      if (this.census_tract_mode != 0)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
        {
          var dir = string.Format("{0}/VIS/run{1}", Global.Simulation_directory, Global.Simulation_run_number);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_I, "I", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_Is, "Is", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_C, "C", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_Cs, "Cs", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_P, "P", day);
          if (Global.Diseases.get_disease(disease_id).is_case_fatality_enabled())
          {
            print_census_tract_data(dir, disease_id, Global.OUTPUT_CF, "CF", day);
            print_census_tract_data(dir, disease_id, Global.OUTPUT_TCF, "TCF", day);
          }
        }

        if (Global.Enable_HAZEL)
        {
          string dir = string.Empty;
          print_census_tract_data(dir, 0, Global.OUTPUT_HC_DEFICIT, "HC_DEFICIT", day);
        }
      }

      if (this.household_mode != 0)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
        {
          var dir = string.Format("{0}/VIS/run{1}", Global.Simulation_directory, Global.Simulation_run_number);
          print_household_data(dir, disease_id, day);
        }
      }

      if (this.gaia_mode != 0)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
        {
          var dir = string.Format("{0}/GAIA/run{1}", Global.Simulation_directory, Global.Simulation_run_number);
          print_output_data(dir, disease_id, Global.OUTPUT_I, "I", day);
          print_output_data(dir, disease_id, Global.OUTPUT_Is, "Is", day);
          print_output_data(dir, disease_id, Global.OUTPUT_C, "C", day);
          print_output_data(dir, disease_id, Global.OUTPUT_Cs, "Cs", day);
          print_output_data(dir, disease_id, Global.OUTPUT_P, "P", day);
          print_population_data(dir, disease_id, day);
          if (Global.Enable_Vector_Layer)
          {
            print_vector_data(dir, disease_id, day);
          }
        }
      }
    }

    public void print_vector_data(string dir, int disease_id, int day)
    {
      var filename = string.Format("{0}/dis{1}/Vec/day-{2}.txt", dir, disease_id, day);
      var fp = new StreamWriter(filename);

      Global.Vectors.update_visualization_data(disease_id, day);
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          var patch = grid[i, j];
          int count = patch.get_count();
          if (count > 0)
          {
            fp.WriteLine("{0} {1} {2}", i, j, count);
          }
          patch.reset_counts();
        }
      }
      fp.Flush();
      fp.Dispose();
    }

    public void print_population_data(string dir, int disease_id, int day)
    {
      var filename = string.Format("{0}/dis{1}/N/day-{2}.txt", dir, disease_id, day);
      var fp = new StreamWriter(filename);

      // get the counts for an arbitrary output code;
      // we only care about the popsize here.
      Global.Places.get_visualization_data_from_households(day, disease_id, Global.OUTPUT_C);
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          var patch = grid[i, j];
          int popsize = patch.get_popsize();
          if (popsize > 0)
          {
            fp.WriteLine("{0} {1} {2}", i, j, popsize);
          }
          // zero out this patch
          patch.reset_counts();
        }
      }
      fp.Flush();
      fp.Dispose();
    }

    public void add_household(Place h)
    {
      this.households.Add(h);
    }

    public void print_household_data(string dir, int disease_id, int day)
    {
      // household with new cases
      var filename= string.Format("{0}/dis{1}/C/households-{2}.txt", dir, disease_id, day);
      var fp = new StreamWriter(filename);
      fp.WriteLine("lat long");
      int size = this.households.Count;
      for (int i = 0; i < size; ++i)
      {
        var house = this.households[i];
        if (house.get_new_infections(day, disease_id) > 0)
        {
          fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
        }
      }
      fp.Flush();
      fp.Dispose();

      // household with active infections
      filename= string.Format("{0}/dis{1}/P/households-{2}.txt", dir, disease_id, day);
      fp = new StreamWriter(filename);
      fp.WriteLine("lat long");
      for (int i = 0; i < size; ++i)
      {
        var house = this.households[i];
        //  just consider human infectious, not mosquito neither infectious places visited
        if (house.get_current_infections(day, disease_id) > 0)
        {
          fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
        }
      }
      fp.Flush();
      fp.Dispose();

      // household with infectious cases
      filename= string.Format("{0}/dis{1}/I/households-{2}.txt", dir, disease_id, day);
      fp = new StreamWriter(filename);
      fp.WriteLine("lat long");
      for (int i = 0; i < size; ++i)
      {
        var house = this.households[i];
        //  just consider human infectious, not mosquito neither infectious places visited
        if (house.is_human_infectious(disease_id))
        {
          fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
        }
      }
      fp.Flush();
      fp.Dispose();

      // household with recovered cases
      filename= string.Format("{0}/dis{1}/R/households-{2}.txt", dir, disease_id, day);
      fp = new StreamWriter(filename);
      fp.WriteLine("lat long");
      for (int i = 0; i < size; ++i)
      {
        var house = this.households[i];
        if (house.is_recovered(disease_id))
        {
          fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
        }
      }
      fp.Flush();
      fp.Dispose();

      if (Global.Diseases.get_disease(disease_id).is_case_fatality_enabled())
      {
        // household with current case fatalities
        filename= string.Format("{0}/dis{1}/CF/households-{2}.txt", dir, disease_id, day);
        fp = new StreamWriter(filename);
        fp.WriteLine("lat long");
        for (int i = 0; i < size; ++i)
        {
          var house = this.households[i];
          if (house.get_current_case_fatalities(day, disease_id) > 0)
          {
            fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
          }
        }
        fp.Flush();
        fp.Dispose();

        // households with any case_fatalities
        filename= string.Format("{0}/dis{1}/TCF/households-{2}.txt", dir, disease_id, day);
        fp = new StreamWriter(filename);
        fp.WriteLine("lat long");
        for (int i = 0; i < size; ++i)
        {
          var house = this.households[i];
          if (house.get_total_case_fatalities(disease_id) > 0)
          {
            fp.WriteLine("{0} {1}", house.get_latitude(), house.get_longitude());
          }
        }
        fp.Flush();
        fp.Dispose();

      }

      // household with Healthcare availability deficiency
      if (Global.Enable_HAZEL)
      {

        //!is_primary_healthcare_available
        filename= string.Format("{0}/dis{1}/HH_primary_hc_unav/households-{2}.txt", dir, disease_id, day);
        fp = new StreamWriter(filename);
        Utils.assert(fp != null);
        fp.WriteLine("lat long");
        for (int i = 0; i < size; ++i)
        {
          var hh = (Household)(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_primary_healthcare_available())
          {
            fp.WriteLine("{0} {1}", hh.get_latitude(), hh.get_longitude());
          }
        }
        fp.Flush();
        fp.Dispose();

        //!is_other_healthcare_location_that_accepts_insurance_available
        filename= string.Format("{0}/dis{1}/HH_accept_insr_hc_unav/households-{2}.txt", dir, disease_id, day);
        fp = new StreamWriter(filename);
        fp.WriteLine("lat long");
        for (int i = 0; i < size; ++i)
        {
          var hh = (Household)(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_other_healthcare_location_that_accepts_insurance_available())
          {
            fp.WriteLine("{0} {1}", hh.get_latitude(), hh.get_longitude());
          }
        }
        fp.Flush();fp.Dispose();

        //!is_healthcare_available
        filename= string.Format("{0}/dis{1}/HH_hc_unav/households-{2}.txt", dir, disease_id, day);
        fp = new StreamWriter(filename);
        fp.WriteLine("lat long");
        for (int i = 0; i < size; ++i)
        {
          var hh = (Household)(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_healthcare_available())
          {
            fp.WriteLine("{0} {1}", hh.get_latitude(), hh.get_longitude());
          }
        }
        fp.Flush();
        fp.Dispose();
      }
    }

    // void print_household_data(string dir, int disease_id, int output_code, string output_str, int day);
    public void print_output_data(string dir, int disease_id, int output_code, string output_str, int day)
    {
      var filename = string.Format("{0}/dis{1}/{2}/day-{3}.txt", dir, disease_id, output_str, day);
      using var fp = new StreamWriter(filename);
      // printf("print_output_data to file %s\n", filename);

      // get the counts for this output_code
      Global.Places.get_visualization_data_from_households(day, disease_id, output_code);

      // print out the non-zero patches
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          var patch = (this.grid[i, j]);
          int count = patch.get_count();
          if (count > 0)
          {
            int popsize = patch.get_popsize();
            fp.WriteLine("{0} {1} {2} {3}", i, j, count, popsize);
          }
          // zero out this patch
          patch.reset_counts();
        }
      }
      fp.Flush();
      fp.Dispose();
    }

    public void print_census_tract_data(string dir, int disease_id, int output_code, string output_str, int day)
    {
      var filename = string.Format("{0}/dis{1}/{2}/census_tracts-{3}.txt", dir, disease_id, output_str, day);

      // get the counts for this output_code
      Global.Places.get_census_tract_data_from_households(day, disease_id, output_code);
      using var fp = new StreamWriter(filename);
      fp.WriteLine("Census_tract\tCount\tPopsize");
      //for (census_tract_t::iterator itr = census_tract.begin(); itr != census_tract.end(); ++itr)
      foreach (var kvp in this.census_tract)
      {
        fp.WriteLine("{0}\t{1}\t{2}\n", kvp.Key, kvp.Value, census_tract_pop[kvp.Key]);
      }
      fp.Flush();
      fp.Dispose();

      // clear census_tract_counts
      foreach (var kvp in this.census_tract)
      {
        this.census_tract[kvp.Key] = 0;
      }
      foreach (var kvp in this.census_tract_pop)
      {
        this.census_tract_pop[kvp.Key] = 0;
      }
    }

    public void initialize_household_data(FredGeo latitude, FredGeo longitude, int count) { }

    public void update_data(FredGeo latitude, FredGeo longitude, int count, int popsize)
    {
      if (this.gaia_mode != 0)
      {
        var patch = get_patch(latitude, longitude);
        if (patch != null)
        {
          patch.update_patch_count(count, popsize);
        }
      }

      if (Global.Enable_HAZEL)
      {
        int size = this.households.Count;
        for (int i = 0; i < size; ++i)
        {
          var hh = (Household)(this.households[i]);
          hh.reset_healthcare_info();
        }
      }
      /*
        if (count > 0) {
        point p = std::make_pair(latitude, longitude);
        this.infected_households.push_back(p);
        }
      */
    }

    public void update_data(double x, double y, int count, int popsize)
    {
      var patch = get_patch(x, y);
      if (patch != null)
      {
        patch.update_patch_count(count, popsize);
      }
    }

    public void update_data(long tract, int count, int popsize)
    {
      census_tract[tract] += count;
      census_tract_pop[tract] += popsize;
    }
  }
}
