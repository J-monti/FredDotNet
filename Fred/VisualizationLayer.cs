using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class VisualizationLayer : AbstractGrid
  {
    private bool gaia_mode;
    private bool household_mode;
    private bool census_tract_mode;
    public VisualizationLayer()
    {
      this.Rows = 0;
      this.Cols = 0;
      this.households = new List<Household>();
      this.gaia_mode = FredParameters.GaiaMode;
      this.household_mode = FredParameters.HouseholdMode;
      this.census_tract_mode = FredParameters.CensusTractMode;

      if (this.gaia_mode)
      {
        // create visualization grid
        var base_grid = Global.SimulationRegion;
        this.MinLat = base_grid.MinLat;
        this.MinLon = base_grid.MinLon;
        this.MaxLat = base_grid.MaxLat;
        this.MaxLon = base_grid.MaxLon;
        this.MinX = base_grid.MinX;
        this.MinY = base_grid.MinY;
        this.MaxX = base_grid.MaxX;
        this.MaxY = base_grid.MaxY;

        // determine patch size for this layer
        var maxGridSize = FredParameters.VisualizationGridSize;
        if (this.MaxX - this.MinX > this.MaxY - this.MinY)
        {
          this.PatchSize = (this.MaxX - this.MinX) / (double)maxGridSize;
        }
        else
        {
          this.PatchSize = (this.MaxY - this.MinY) / (double)maxGridSize;
        }
        this.Rows = (int)((this.MaxY - this.MinY) / this.PatchSize);
        if (this.MinY + this.Rows * this.PatchSize < this.MaxY)
        {
          this.Rows++;
        }
        this.Cols = (int)((this.MaxX - this.MinX) / this.PatchSize);
        if (this.MinX + this.Cols * this.PatchSize < this.MaxX)
        {
          this.Cols++;
        }

        if (Global.Verbose > 0)
        {
          Console.WriteLine("Visualization_Layer min_lon = {0}", this.MinLon);
          Console.WriteLine("Visualization_Layer min_lat = {0}", this.MinLat);
          Console.WriteLine("Visualization_Layer max_lon = {0}", this.MaxLon);
          Console.WriteLine("Visualization_Layer max_lat = {0}", this.MaxLat);
          Console.WriteLine("Visualization_Layer rows  = {0}  cols  = {1}", this.Rows, this.Cols);
          Console.WriteLine("Visualization_Layer min_x = {0}  min_y = {1}", this.MinX, this.MinY);
          Console.WriteLine("Visualization_Layer max_x = {0}  max_y = {1}", this.MaxX, this.MaxY);
        }

        this.grid = new VisualizationPatch[this.Rows, this.Cols];
        for (int i = 0; i < this.Rows; ++i)
        {
          for (int j = 0; j < this.Cols; ++j)
          {
            this.grid[i,j].setup(i, j, this.PatchSize, this.MinX, this.MinY);
          }
        }
      }
    }

    public List<Household> households { get; }

    public VisualizationPatch[,] grid { get; private set; }

    public VisualizationPatch get_patch(int row, int col)
    {
      if (row >= 0 && col >= 0 && row < this.Rows && col < this.Cols)
      {
        return this.grid[row, col];
      }

      return null;
    }

    public VisualizationPatch get_patch(double lat, double lon)
    {
      int row = this.GetRow(lat);
      int col = this.GetCol(lon);
      return get_patch(row, col);
    }

    public VisualizationPatch get_patch_geo(double x, double y)
    {
      int row = this.GetRowGeo(y);
      int col = this.GetColGeo(x);
      return get_patch(row, col);
    }

    public VisualizationPatch select_random_patch()
    {
      int row = FredRandom.Next(0, this.Rows - 1);
      int col = FredRandom.Next(0, this.Cols - 1);
      return this.grid[row, col];
    }

    private void quality_control()
    {
      if (Global.Verbose > 0)
      {
        Console.WriteLine("visualization grid quality control check");
      }

      if (this.gaia_mode)
      {
        for (int row = 0; row < this.Rows; ++row)
        {
          for (int col = 0; col < this.Cols; ++col)
          {
            this.grid[row,col].quality_control();
          }
        }
        if (Global.Verbose > 1 && this.Rows > 0) {
          using (var writer = new StreamWriter(Path.Combine(Global.SimulationDirectory, "visualization_grid.dat")))
          {
            for (int row = 0; row < this.Rows; ++row)
            {
              if (row % 2 != 0)
              {
                for (int col = this.Cols - 1; col >= 0; --col)
                {
                  double x = this.grid[row, col].CenterX;
                  double y = this.grid[row, col].CenterY;
                  writer.WriteLine("{0} {1}", x, y);
                }
              }
              else
              {
                for (int col = 0; col < this.Cols; ++col)
                {
                  double x = this.grid[row, col].CenterX;
                  double y = this.grid[row, col].CenterY;
                  writer.WriteLine("{0} {1}", x, y);
                }
              }
            }
          }
        }
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine("visualization grid quality control finished\n");
        
      }
    }

    public void initialize()
    {
      string vis_top_dir = Path.Combine(Global.SimulationDirectory, "VIS");
      create_data_directories(vis_top_dir);

      // create visualization data directory
      if (this.gaia_mode)
      {
        vis_top_dir = Path.Combine(Global.SimulationDirectory, "GAIA");
        create_data_directories(vis_top_dir);
        // create GAIA setup file
        var setup_file = Path.Combine(vis_top_dir, "grid.txt");
        using (var writer = new StreamWriter(setup_file))
        {
          writer.WriteLine("rows = {0}", this.Rows);
          writer.WriteLine("cols = {0}", this.Cols);
          writer.WriteLine("min_lat = {0}", this.MinLat);
          writer.WriteLine("min_lon = {0}", this.MinLon);
          writer.WriteLine("patch_x_size = {0}", Geo.XSizeToDegreeLongitude(this.PatchSize));
          writer.WriteLine("patch_y_size = {0}", Geo.YSizeToDegreeLatitude(this.PatchSize));
        }
      }
    }

    private void create_data_directories(string vis_top_dir)
    {
      // make top level data directory
      Directory.CreateDirectory(vis_top_dir);
      // make directory for this run
      var vis_run_dir = Path.Combine(vis_top_dir, $"run{Global.SimulationRunNumber}");
      Directory.CreateDirectory(vis_run_dir);
      var vis_dis_dir = string.Empty;
      var vis_var_dir = string.Empty;

      // create sub directories for diseases and output vars
      for (int d = 0; d < Global.Diseases.Count; ++d)
      {
        vis_dis_dir = $"{vis_run_dir}/dis{d}";
        Directory.CreateDirectory(vis_dis_dir);

        if (d == 0)
        {
          // print out household locations
          var filename = $"{vis_dis_dir}/households.txt";
          using (var writer = new StreamWriter(filename))
          {
            int num_households = this.households.Count;
            for (int i = 0; i < num_households; ++i)
            {
              var h = this.households[i];
              writer.WriteLine("{0} {1} {2} {3}", h.get_latitude(), h.get_longitude(), h.get_size(), h.Label);
            }
          }
        }
        else
        {
          // create symbolic links
          char cmd[FRED_STRING_SIZE];
          sprintf(cmd, "ln -s %s/dis0/households.txt %s/households.txt", vis_run_dir, vis_dis_dir);
          if (system(cmd) != 0)
          {
            FredUtils.Abort("Error using system command \"%s\"\n", cmd);
          }
        }

        // create directories for specific output variables
        sprintf(vis_var_dir, "%s/I", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/Is", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/C", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/Cs", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/P", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/N", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/R", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/Vec", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/D", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/CF", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);
        sprintf(vis_var_dir, "%s/TCF", vis_dis_dir);
        Directory.CreateDirectory(vis_var_dir);

        if (this.household_mode && Global.IsHazelEnabled)
        {
          sprintf(vis_var_dir, "%s/HH_primary_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
          sprintf(vis_var_dir, "%s/HH_accept_insr_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
          sprintf(vis_var_dir, "%s/HH_hc_unav", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
        }

        if (this.census_tract_mode && Global.IsHazelEnabled)
        {
          sprintf(vis_var_dir, "%s/HC_DEFICIT", vis_dis_dir);
          Directory.CreateDirectory(vis_var_dir);
        }
      }
    }


    void print_visualization_data(int day)
    {
      if (this.census_tract_mode)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.Count; ++disease_id)
        {
          char dir[FRED_STRING_SIZE];
          sprintf(dir, "%s/VIS/run%d", Global.SimulationDirectory, Global.Simulation_run_number);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_I, (string)"I", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_Is, (string)"Is", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_C, (string)"C", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_Cs, (string)"Cs", day);
          print_census_tract_data(dir, disease_id, Global.OUTPUT_P, (string)"P", day);
          if (Global.Diseases.get_disease(disease_id).is_case_fatality_enabled())
          {
            print_census_tract_data(dir, disease_id, Global.OUTPUT_CF, (string)"CF", day);
            print_census_tract_data(dir, disease_id, Global.OUTPUT_TCF, (string)"TCF", day);
          }
        }

        if (Global.IsHazelEnabled)
        {
          char dir[FRED_STRING_SIZE];
          print_census_tract_data(dir, 0, Global.OUTPUT_HC_DEFICIT, (string)"HC_DEFICIT", day);
        }
      }

      if (this.household_mode)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.Count; ++disease_id)
        {
          char dir[FRED_STRING_SIZE];
          sprintf(dir, "%s/VIS/run%d", Global.SimulationDirectory, Global.Simulation_run_number);
          print_household_data(dir, disease_id, day);
        }
      }

      if (this.gaia_mode)
      {
        for (int disease_id = 0; disease_id < Global.Diseases.Count; ++disease_id)
        {
          char dir[FRED_STRING_SIZE];
          sprintf(dir, "%s/GAIA/run%d", Global.SimulationDirectory, Global.Simulation_run_number);
          print_output_data(dir, disease_id, Global.OUTPUT_I, (string)"I", day);
          print_output_data(dir, disease_id, Global.OUTPUT_Is, (string)"Is", day);
          print_output_data(dir, disease_id, Global.OUTPUT_C, (string)"C", day);
          print_output_data(dir, disease_id, Global.OUTPUT_Cs, (string)"Cs", day);
          print_output_data(dir, disease_id, Global.OUTPUT_P, (string)"P", day);
          print_population_data(dir, disease_id, day);
          if (Global.Enable_Vector_Layer)
          {
            print_vector_data(dir, disease_id, day);
          }
        }
      }
    }

    void print_household_data(string dir, int disease_id, int day)
    {

      // household with new cases
      char filename[FRED_STRING_SIZE];
      sprintf(filename, "%s/dis%d/C/households-%d.txt", dir, disease_id, day);
      FILE* fp = fopen(filename, "w");
      fprintf(fp, "lat long\n");
      int size = this.households.size();
      for (int i = 0; i < size; ++i)
      {
        Place* house = this.households[i];
        if (house.get_new_infections(day, disease_id) > 0)
        {
          fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
        }
      }
      fclose(fp);

      // household with active infections
      sprintf(filename, "%s/dis%d/P/households-%d.txt", dir, disease_id, day);
      fp = fopen(filename, "w");
      fprintf(fp, "lat long\n");
      for (int i = 0; i < size; ++i)
      {
        Place* house = this.households[i];
        //  just consider human infectious, not mosquito neither infectious places visited
        if (house.get_current_infections(day, disease_id) > 0)
        {
          fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
        }
      }
      fclose(fp);

      // household with infectious cases
      sprintf(filename, "%s/dis%d/I/households-%d.txt", dir, disease_id, day);
      fp = fopen(filename, "w");
      fprintf(fp, "lat long\n");
      for (int i = 0; i < size; ++i)
      {
        Place* house = this.households[i];
        //  just consider human infectious, not mosquito neither infectious places visited
        if (house.is_human_infectious(disease_id))
        {
          fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
        }
      }
      fclose(fp);

      // household with recovered cases
      sprintf(filename, "%s/dis%d/R/households-%d.txt", dir, disease_id, day);
      fp = fopen(filename, "w");
      fprintf(fp, "lat long\n");
      for (int i = 0; i < size; ++i)
      {
        Place* house = this.households[i];
        if (house.is_recovered(disease_id))
        {
          fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
        }
      }
      fclose(fp);

      if (Global.Diseases.get_disease(disease_id).is_case_fatality_enabled())
      {
        // household with current case fatalities
        sprintf(filename, "%s/dis%d/CF/households-%d.txt", dir, disease_id, day);
        fp = fopen(filename, "w");
        fprintf(fp, "lat long\n");
        for (int i = 0; i < size; ++i)
        {
          Place* house = this.households[i];
          if (house.get_current_case_fatalities(day, disease_id) > 0)
          {
            fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
          }
        }
        fclose(fp);

        // households with any case_fatalities
        sprintf(filename, "%s/dis%d/TCF/households-%d.txt", dir, disease_id, day);
        fp = fopen(filename, "w");
        fprintf(fp, "lat long\n");
        for (int i = 0; i < size; ++i)
        {
          Place* house = this.households[i];
          if (house.get_total_case_fatalities(disease_id) > 0)
          {
            fprintf(fp, "%f %f\n", house.get_latitude(), house.get_longitude());
          }
        }
        fclose(fp);

      }

      // household with Healthcare availability deficiency
      if (Global.IsHazelEnabled)
      {

        //!is_primary_healthcare_available
        sprintf(filename, "%s/dis%d/HH_primary_hc_unav/households-%d.txt", dir, disease_id, day);
        fp = fopen(filename, "w");
        assert(fp != NULL);
        fprintf(fp, "lat long\n");
        for (int i = 0; i < size; ++i)
        {
          Household* hh = static_cast<Household*>(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_primary_healthcare_available())
          {
            fprintf(fp, "%f %f\n", hh.get_latitude(), hh.get_longitude());
          }
        }
        fclose(fp);

        //!is_other_healthcare_location_that_accepts_insurance_available
        sprintf(filename, "%s/dis%d/HH_accept_insr_hc_unav/households-%d.txt", dir, disease_id, day);
        fp = fopen(filename, "w");
        fprintf(fp, "lat long\n");
        for (int i = 0; i < size; ++i)
        {
          Household* hh = static_cast<Household*>(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_other_healthcare_location_that_accepts_insurance_available())
          {
            fprintf(fp, "%f %f\n", hh.get_latitude(), hh.get_longitude());
          }
        }
        fclose(fp);

        //!is_healthcare_available
        sprintf(filename, "%s/dis%d/HH_hc_unav/households-%d.txt", dir, disease_id, day);
        fp = fopen(filename, "w");
        fprintf(fp, "lat long\n");
        for (int i = 0; i < size; ++i)
        {
          Household* hh = static_cast<Household*>(this.households[i]);
          if (hh.is_seeking_healthcare() && !hh.is_healthcare_available())
          {
            fprintf(fp, "%f %f\n", hh.get_latitude(), hh.get_longitude());
          }
        }
        fclose(fp);
      }
    }

    /*
      void print_household_data(string dir, int disease_id, int output_code, string output_str, int day) {
      char filename[FRED_STRING_SIZE];
      sprintf(filename, "%s/dis%d/%s/households-%d.txt", dir, disease_id, output_str, day);
      FILE* fp = fopen(filename, "w");
      fprintf(fp, "lat long\n");
      this.infected_households.clear();
      // get the counts for this output_code
      Global.Places.get_visualization_data_from_households(disease_id, output_code);
      // print out the lat long of all infected households
      int houses = (int)(this.infected_households.size());
      for(int i = 0; i < houses; ++i) {
      fred::geo lat = infected_households[i].first;
      fred::geo lon = infected_households[i].second;
      fprintf(fp, "%lf %lf\n", lat, lon);
      }
      fclose(fp);
      this.infected_households.clear();
      }
    */

    void print_output_data(string dir, int disease_id, int output_code, string output_str, int day)
    {
      char filename[FRED_STRING_SIZE];
      sprintf(filename, "%s/dis%d/%s/day-%d.txt", dir, disease_id, output_str, day);
      FILE* fp = fopen(filename, "w");
      // printf("print_output_data to file %s\n", filename);

      // get the counts for this output_code
      Global.Places.get_visualization_data_from_households(day, disease_id, output_code);

      // print out the non-zero patches
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          Visualization_Patch* patch = (Visualization_Patch*)&(this.grid[i,j]);
          int count = patch.get_count();
          if (count > 0)
          {
            int popsize = patch.get_popsize();
            fprintf(fp, "%d %d %d %d\n", i, j, count, popsize);
          }
          // zero out this patch
          patch.reset_counts();
        }
      }
      fclose(fp);
    }

    void print_census_tract_data(string dir, int disease_id, int output_code, string output_str, int day)
    {
      char filename[FRED_STRING_SIZE];
      sprintf(filename, "%s/dis%d/%s/census_tracts-%d.txt", dir, disease_id, output_str, day);

      // get the counts for this output_code
      Global.Places.get_census_tract_data_from_households(day, disease_id, output_code);

      FILE* fp = fopen(filename, "w");
      fprintf(fp, "Census_tract\tCount\tPopsize\n");
      for (census_tract_t::iterator itr = census_tract.begin(); itr != census_tract.end(); ++itr)
      {
        unsigned long long tract = itr.first;
        fprintf(fp, "%011lld\t%lu\t%lu\n", tract, itr.second, census_tract_pop[tract]);
      }
      fclose(fp);

      // clear census_tract_counts
      for (census_tract_t::iterator itr = census_tract.begin(); itr != census_tract.end(); ++itr)
      {
        itr.second = 0;
      }
      for (census_tract_t::iterator itr = census_tract_pop.begin(); itr != census_tract_pop.end(); ++itr)
      {
        itr.second = 0;
      }
    }

    void print_population_data(string dir, int disease_id, int day)
    {
      char filename[FRED_STRING_SIZE];
      // printf("Printing population size for GAIA\n");
      sprintf(filename, "%s/dis%d/N/day-%d.txt", dir, disease_id, day);
      FILE* fp = fopen(filename, "w");

      // get the counts for an arbitrary output code;
      // we only care about the popsize here.
      Global.Places.get_visualization_data_from_households(day, disease_id, Global.OUTPUT_C);
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          Visualization_Patch* patch = (Visualization_Patch*)&grid[i,j];
          int popsize = patch.get_popsize();
          if (popsize > 0)
          {
            fprintf(fp, "%d %d %d\n", i, j, popsize);
          }
          // zero out this patch
          patch.reset_counts();
        }
      }
      fclose(fp);
    }

    void print_vector_data(string dir, int disease_id, int day)
    {
      // printf("Printing population size for GAIA\n");
      var filename = $"{dir}/dis{disease_id}/Vec/day-{day}.txt";
      Global.Vectors.update_visualization_data(disease_id, day);
      using (var writer = new StreamWriter(filename))
      {
        for (int i = 0; i < this.Rows; ++i)
        {
          for (int j = 0; j < this.Cols; ++j)
          {
            var patch = grid[i, j];
            int count = patch.get_count();
            if (count > 0)
            {
              writer.WriteLine("{0} {1} {2}", i, j, count);
            }
            patch.reset_counts();
          }
        }
      }
    }


    public void initialize_household_data(double latitude, double longitude, int count)
    {
      /*
        if(count > 0) {
        point p = std::make_pair(latitude, longitude);
        this.all_households.push_back(p);
        }
      */
    }

    public void update_data_geo(double latitude, double longitude, int count, int popsize)
    {
      if (this.gaia_mode)
      {
        var patch = get_patch_geo(latitude, longitude);
        if (patch != null)
        {
          patch.update_patch_count(count, popsize);
        }
      }

      if (Global.IsHazelEnabled)
      {
        int size = this.households.Count;
        for (int i = 0; i < size; ++i)
        {
          var hh = this.households[i];
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
      this.census_tract[tract] += count;
      this.census_tract_pop[tract] += popsize;
    }
  }
}
