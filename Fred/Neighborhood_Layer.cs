using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Fred
{
  public class Neighborhood_Layer : Abstract_Grid
  {
    private Neighborhood_Patch[,] grid;     // Rectangular array of patches
    private int popsize;
    private int households;
    private int[] age_distrib = new int[100];

    // data used by neighborhood gravity model
    private List<int>[,] offset;
    private List<double>[,] gravity_cdf;
    private int max_offset;
    private List<Tuple<double, int>> sort_pair;

    // runtime parameters for neighborhood gravity model
    private bool Enable_neighborhood_gravity_model;
    private double max_distance;
    private double min_distance;
    private int max_destinations;
    private double pop_exponent;
    private double dist_exponent;

    // runtime parameters for old neighborhood model (deprecated)
    private double Community_distance;      // deprecated
    private double Community_prob;      // deprecated
    private double Home_neighborhood_prob;		// deprecated

    public Neighborhood_Layer()
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

      // determine patch size for this layer
      FredParameters.GetParameter("neighborhood_patch_size", ref this.patch_size);

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
        Utils.FRED_STATUS(0, "Neighborhood_Layer min_lon = {0}", this.min_lon);
        Utils.FRED_STATUS(0, "Neighborhood_Layer min_lat = {0}", this.min_lat);
        Utils.FRED_STATUS(0, "Neighborhood_Layer max_lon = {0}", this.max_lon);
        Utils.FRED_STATUS(0, "Neighborhood_Layer max_lat = {0}", this.max_lat);
        Utils.FRED_STATUS(0, "Neighborhood_Layer rows = {0}  cols = {1}", this.rows, this.cols);
        Utils.FRED_STATUS(0, "Neighborhood_Layer min_x = {0}  min_y = {1}", this.min_x, this.min_y);
        Utils.FRED_STATUS(0, "Neighborhood_Layer max_x = {0}  max_y = {1}", this.max_x, this.max_y);
      }

      // setup patches
      this.grid = new Neighborhood_Patch[this.rows, this.cols];
      for (int i = 0; i < this.rows; i++)
      {
        for (int j = 0; j < this.cols; j++)
        {
          this.grid[i, j] = new Neighborhood_Patch();
          this.grid[i, j].setup(this, i, j);
        }
      }

      // params to determine neighborhood visitation patterns
      int temp_int = 0;
      FredParameters.GetParameter("enable_neighborhood_gravity_model", ref temp_int);
      this.Enable_neighborhood_gravity_model = (temp_int != 0);

      FredParameters.GetParameter("neighborhood_max_distance", ref this.max_distance);
      FredParameters.GetParameter("neighborhood_max_destinations", ref this.max_destinations);
      FredParameters.GetParameter("neighborhood_min_distance", ref this.min_distance);
      FredParameters.GetParameter("neighborhood_distance_exponent", ref this.dist_exponent);
      FredParameters.GetParameter("neighborhood_population_exponent", ref this.pop_exponent);

      // params for old neighborhood model (deprecated)
      FredParameters.GetParameter("community_distance", ref this.Community_distance);
      FredParameters.GetParameter("community_prob", ref this.Community_prob);
      FredParameters.GetParameter("home_neighborhood_prob", ref this.Home_neighborhood_prob);
    }

    public void setup()
    {
      // create one neighborhood per patch
      for (int i = 0; i < this.rows; i++)
      {
        for (int j = 0; j < this.cols; j++)
        {
          if (this.grid[i,j].get_houses() > 0)
          {
            this.grid[i,j].make_neighborhood();
          }
        }
      }

      if (Global.Verbose > 1)
      {
        for (int i = 0; i < this.rows; i++)
        {
          for (int j = 0; j < this.cols; j++)
          {
            Console.WriteLine("print grid[{0},{1}]:", i, j);
            this.grid[i, j].print();
          }
        }
      }
    }

    /**
     * @param row the row where the Patch is located
     * @param col the column where the Patch is located
     * @return a pointer to the Patch at the row and column requested
     */
    public void prepare()
    {
      record_daily_activity_locations();
      if (Enable_neighborhood_gravity_model)
      {
        Utils.FRED_VERBOSE(0, "setup gravity model ...\n");
        setup_gravity_model();
        Utils.FRED_VERBOSE(0, "setup gravity model complete\n");
      }
    }

    public Neighborhood_Patch get_patch(int row, int col)
    {
      if (row >= 0 && col >= 0 && row < this.rows && col < this.cols)
        return grid[row, col];
      else
        return null;
    }

    public Neighborhood_Patch get_patch(FredGeo lat, FredGeo lon)
    {
      int row = get_row(lat);
      int col = get_col(lon);
      return get_patch(row, col);
    }

    public Neighborhood_Patch select_random_patch(double x0, double y0, double dist)
    {
      // select a random patch within given distance.
      // if no luck after 20 attempts, return NULL
      for (int i = 0; i < 20; i++)
      {
        double r = FredRandom.NextDouble() * dist;      // random distance
        double ang = Geo.DEG_TO_RAD * FredRandom.NextDouble(0, 360);// random angle
        double x = x0 + r * Math.Cos(ang);// corresponding x coord
        double y = y0 + r * Math.Sin(ang);// corresponding y coord
        int row = get_row(y);
        int col = get_col(x);
        var patch = get_patch(row, col);
        if (patch != null)
        {
          return patch;
        }
      }
      return null;
    }

    public Neighborhood_Patch select_random_neighbor(int row, int col)
    {
      int n = FredRandom.Next(0, 7);
      if (n > 3)
      {
        n++;        // excludes local patch
      }
      int r = row - 1 + (n / 3);
      int c = col - 1 + (n % 3);
      return get_patch(r, c);
    }

    public Place select_school_in_area(int age, int row, int col)
    {
      Utils.FRED_VERBOSE(1, "SELECT_SCHOOL_IN_AREA for age %d row %d col %d\n", age, row, col);
      // make a list of all schools within 50 kms that have grades for this age
      var schools = new List<Place>();
      Neighborhood_Patch patch;
      int max_dist = 60;
      for (int c = col - max_dist; c <= col + max_dist; c++)
      {
        for (int r = row - max_dist; r <= row + max_dist; r++)
        {
          patch = get_patch(r, c);
          if (patch != null)
          {
            // find all age-appropriate schools in this patch
            patch.find_schools_for_age(age, schools);
          }
        }
      }
      Utils.assert(schools.Count > 0);
      Utils.FRED_VERBOSE(1, "SELECT_SCHOOL_IN_AREA found %d possible schools\n", schools.Count);
      // sort schools by vacancies
      schools.Sort(new MoreRoomComparer());
      // pick the school with largest vacancy or smallest crowding 
      var place = schools.First();
      var s = (School)place;
      Utils.FRED_VERBOSE(1, "SELECT_SCHOOL_IN_AREA found school %s orig %d current %d vacancies %d\n",
             s.get_label(), s.get_orig_number_of_students(), s.get_number_of_students(),
             s.get_orig_number_of_students() - s.get_number_of_students());
      // s.print_size_distribution();
      return place;
    }

    public Place select_workplace_in_area(int row, int col)
    {

      Utils.FRED_VERBOSE(1, "SELECT_WORKPLACE_IN_AREA for row %d col %d\n", row, col);

      // look for workplaces in increasingly expanding adjacent neighborhood
      List<Neighborhood_Patch> patches = new List<Neighborhood_Patch>();
      Neighborhood_Patch patch;
      for (int level = 0; level < 100; level++)
      {
        Utils.FRED_VERBOSE(2, "level %d\n", level);
        if (level == 0)
        {
          patch = get_patch(row, col);
          if (patch != null)
          {
            Utils.FRED_VERBOSE(2, "adding (%d,%d)\n", row, col);
            patches.Add(patch);
          }
        }
        else
        {
          for (int c = col - level; c <= col + level; c++)
          {
            patch = get_patch(row - level, c);
            if (patch != null)
            {
              Utils.FRED_VERBOSE(2, "adding (%d,%d)\n", row - level, c);
              patches.Add(patch);
            }
            patch = get_patch(row + level, c);
            if (patch != null)
            {
              Utils.FRED_VERBOSE(2, "adding (%d,%d)\n", row + level, c);
              patches.Add(patch);
            }
          }
          for (int r = row - level + 1; r <= row + level - 1; r++)
          {
            patch = get_patch(r, col - level);
            if (patch != null)
            {
              Utils.FRED_VERBOSE(2, "adding (%d,%d)\n", r, col - level);
              patches.Add(patch);
            }
            patch = get_patch(r, col + level);
            if (patch != null)
            {
              Utils.FRED_VERBOSE(2, "adding (%d,%d)\n", r, col + level);
              patches.Add(patch);
            }
          }
        }

        // shuffle the patches
        patches.Shuffle();
        Utils.FRED_VERBOSE(1, "Level %d include %d patches\n", level, patches.Count);

        // look for a suitable workplace in each patch
        for (int i = 0; i < patches.Count; i++)
        {
          var pat = patches[i];
          if (pat == null)
            continue;
          var p = pat.select_workplace_in_neighborhood();
          if (p != null)
          {
            Utils.FRED_VERBOSE(1, "SELECT_WORKPLACE_IN_AREA found workplace %s at level %d\n",
                   p.get_label(), level);
            return p; // success
          }
        }
      }
      return null;
    }

    /**
     * @return a pointer to a random patch in this layer
     */
    public Neighborhood_Patch select_random_patch()
    {
      int row = FredRandom.Next(0, this.rows - 1);
      int col = FredRandom.Next(0, this.cols - 1);
      return grid[row, col];
    }

    /**
     * Used during debugging to verify that code is functioning properly.
     */
    public void quality_control()
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "grid quality control check\n");
      }

      int popsize = 0;
      int tot_occ_patches = 0;
      for (int row = 0; row < this.rows; row++)
      {
        int min_occ_col = this.cols + 1;
        int max_occ_col = -1;
        for (int col = 0; col < this.cols; col++)
        {
          this.grid[row, col].quality_control();
          int patch_pop = this.grid[row, col].get_popsize();
          if (patch_pop > 0)
          {
            if (col > max_occ_col)
            {
              max_occ_col = col;
            }
            if (col < min_occ_col)
            {
              min_occ_col = col;
            }
            popsize += patch_pop;
          }
        }
        if (min_occ_col < this.cols)
        {
          int patches_occ = max_occ_col - min_occ_col + 1;
          tot_occ_patches += patches_occ;
        }
      }

      if (Global.Verbose > 1)
      {
        using var fp = new StreamWriter($"{Global.Simulation_directory}/grid.dat");
        for (int row = 0; row < rows; row++)
        {
          if (row % 2 != 0)
          {
            for (int col = this.cols - 1; col >= 0; col--)
            {
              double x = this.grid[row, col].get_center_x();
              double y = this.grid[row, col].get_center_y();
              fp.WriteLine("{0} {1}", x, y);
            }
          }
          else
          {
            for (int col = 0; col < this.cols; col++)
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

      if (true || Global.Verbose > 0)
      {
        int total_area = this.rows * this.cols;
        int convex_area = tot_occ_patches;
        Utils.FRED_STATUS(0, "Density: pop size = %d total region = %d total_density = %f\n",
          popsize, total_area, (total_area > 0) ? (double)popsize / (double)total_area : 0.0);
        Utils.FRED_STATUS(0, "Density: pop size = %d convex region = %d convex_density = %f\n",
          popsize, convex_area, (convex_area > 0) ? (double)popsize / (double)convex_area : 0.0);
        Utils.FRED_STATUS(0, "grid quality control finished\n");
      }
    }

    public void quality_control(double min_x, double min_y)
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "grid quality control check\n");
      }

      for (int row = 0; row < this.rows; row++)
      {
        for (int col = 0; col < this.cols; col++)
        {
          this.grid[row,col].quality_control();
        }
      }

      if (Global.Verbose > 1)
      {
        using var fp = new StreamWriter($"{Global.Simulation_directory}/grid.dat");
        for (int row = 0; row < rows; row++)
        {
          if (row % 2 != 0)
          {
            for (int col = this.cols - 1; col >= 0; col--)
            {
              double x = this.grid[row,col].get_center_x();
              double y = this.grid[row,col].get_center_y();
              fp.WriteLine("{0} {1}", x, y);
            }
          }
          else
          {
            for (int col = 0; col < this.cols; col++)
            {
              double x = this.grid[row,col].get_center_x();
              double y = this.grid[row,col].get_center_y();
              fp.WriteLine("{0} {1}", x, y);
            }
          }
        }
        fp.Flush();
        fp.Dispose();
      }

      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "grid quality control finished\n");
      }
    }

    /**
     * @brief Get all people living within a specified radius of a point.
     *
     * Finds patches overlapping radius_in_km from point, then finds all households in those patches radius_in_km distance from point.
     * Returns list of people in those households radius_in_km from point.
     * Used in Epidemic.update for geographical seeding.
     *
     * @author Jay DePasse
     * @param lat the latitude of the center of the circle
     * @param lon the longitude of the center of the circle
     * @param radius_in_km the distance from the center of the circle
     * @return a Vector of places
     */
    public List<Place> get_households_by_distance(FredGeo lat, FredGeo lon, double radius_in_km)
    {
      double px = Geo.get_x(lon);
      double py = Geo.get_y(lat);
      //  get patches around the point, make sure their rows & cols are in bounds
      int r1 = this.rows - 1 - (int)((py + radius_in_km) / this.patch_size);
      r1 = (r1 >= 0) ? r1 : 0;
      int r2 = this.rows - 1 - (int)((py - radius_in_km) / this.patch_size);
      r2 = (r2 <= this.rows - 1) ? r2 : this.rows - 1;

      int c1 = (int)((px - radius_in_km) / this.patch_size);
      c1 = (c1 >= 0) ? c1 : 0;
      int c2 = (int)((px + radius_in_km) / this.patch_size);
      c2 = (c2 <= this.cols - 1) ? c2 : this.cols - 1;

      var households = new List<Place>(); // store all households in patches overlapping the radius

      for (int r = r1; r <= r2; r++)
      {
        for (int c = c1; c <= c2; c++)
        {
          var p = get_patch(r, c);
          int number_of_households = p.get_number_of_households();
          for (int i = 0; i < number_of_households; i++)
          {
            var house = p.get_household(i);
            var hlat = house.get_latitude();
            var hlon = house.get_longitude();
            double hx = Geo.get_x(hlon);
            double hy = Geo.get_y(hlat);
            if (Math.Sqrt((px - hx) * (px - hx) + (py - hy) * (py - hy)) <= radius_in_km)
            {
              households.Add(house);
            }
          }
        }
      }
      return households;
    }

    /**
     * Make each patch in this layer store its daily activity locations, and then set pop size and households
     */
    public void record_daily_activity_locations()
    {
      int partial_popsize = 0;
      int partial_households = 0;
      for (int row = 0; row < this.rows; row++)
      {
        for (int col = 0; col < this.cols; col++)
        {
          var patch = this.grid[row, col];
          if (patch.get_houses() > 0)
          {
            patch.record_daily_activity_locations();
            partial_popsize += patch.get_neighborhood().get_size();
            partial_households += patch.get_houses();
          }
        }
      }
      this.popsize += partial_popsize;
      this.households += partial_households;
    }

    /**
     * @return the pop size
     */
    public int get_popsize()
    {
      return this.popsize;
    }

    /**
     * @return the households
     */
    public int get_households()
    {
      return this.households;
    }

    /**
     * Write the household distributions to a file
     *
     * @param dir the directory where the file will go
     * @param date_string the date of the write (used for filename)
     * @param run the run id (used for filename)
     */
    public int get_number_of_neighborhoods()
    {
      int n = 0;
      for (int row = 0; row < this.rows; row++)
      {
        for (int col = 0; col < this.cols; col++)
        {
          if (this.grid[row, col].get_houses() > 0)
            n++;
        }
      }
      return n;
    }

    public void setup_gravity_model()
    {
      var tmp_offset = new int[256 * 256];
      var tmp_prob = new double[256 * 256];
      int count;

      // print_distances();  // DEBUGGING

      offset = new List<int>[rows, cols];
      gravity_cdf = new List<double>[rows, cols];
      if (max_distance < 0)
      {
        setup_null_gravity_model();
        return;
      }

      max_offset = Convert.ToInt32(max_distance / this.patch_size);
      Utils.assert(max_offset < 128);

      for (int i = 0; i < rows; i++)
      {
        for (int j = 0; j < cols; j++)
        {
          // set up gravity model for grid[i][j];
          var patch = grid[i, j];
          Utils.assert(patch != null);
          double x_src = patch.get_center_x();
          double y_src = patch.get_center_y();
          int pop_src = patch.get_popsize();
          double mean_household_income_src = patch.get_mean_household_income();
          if (pop_src == 0) continue;
          count = 0;
          for (int ii = i - max_offset; ii < rows && ii <= i + max_offset; ii++)
          {
            if (ii < 0) continue;
            for (int jj = j - max_offset; jj < cols && jj <= j + max_offset; jj++)
            {
              if (jj < 0) continue;
              var dest_patch = grid[ii, jj];
              Utils.assert(dest_patch != null);
              int pop_dest = dest_patch.get_popsize();
              if (pop_dest == 0) continue;
              double x_dest = dest_patch.get_center_x();
              double y_dest = dest_patch.get_center_y();
              double dist = Math.Sqrt((x_src - x_dest) * (x_src - x_dest) + (y_src - y_dest) * (y_src - y_dest));
              if (max_distance < dist) continue;
              double gravity = Math.Pow(pop_dest, pop_exponent) / (1.0 + Math.Pow(dist / min_distance, dist_exponent));
              int off = 256 * (i - ii + max_offset) + (j - jj + max_offset);

              // consider income similarity in gravity model
              // double mean_household_income_dest = dest_patch.get_mean_household_income();
              // double income_similarity = mean_household_income_dest / mean_household_income_src;
              // if (income_similarity > 1.0) { income_similarity = 1.0 / income_similarity; }
              // gravity = ...;

              tmp_offset[count] = off;
              tmp_prob[count++] = gravity;
              // printf("SETUP row %3d col %3d pop %5d count %4d ", i,j,pop_src,count);
              // printf("offset %5d off_i %3d off_j %3d ii %3d jj %3d ",off,i+max_offset-(off/256),j+max_offset-(off%256),ii,jj);
              // printf("dist %.2f pop_dest %5d gravity %7.3f\n",dist,pop_dest,gravity);
            }
          }
          // printf("\n");

          // sort by gravity value
          sort_pair = new List<Tuple<double, int>>();
          for (int k = 0; k < count; k++)
          {
            sort_pair.Add(new Tuple<double, int>(tmp_prob[k], tmp_offset[k]));
          }
          sort_pair.Sort(new PairComparer());

          // keep at most largest max_destinations
          if (count > max_destinations) count = max_destinations;
          for (int k = 0; k < count; k++)
          {
            tmp_prob[k] = sort_pair[k].Item1;
            tmp_offset[k] = sort_pair[k].Item2;
          }
          sort_pair.Clear();

          // transform gravity values into a prob distribution
          double total = 0.0;
          for (int k = 0; k < count; k++)
          {
            total += tmp_prob[k];
          }
          for (int k = 0; k < count; k++)
          {
            tmp_prob[k] /= total;
          }

          // convert to cdf
          for (int k = 1; k < count; k++)
          {
            tmp_prob[k] += tmp_prob[k - 1];
          }

          // store gravity prob and offsets for this patch
          gravity_cdf[i, j] = new List<double>();
          offset[i, j] = new List<int>();
          for (int k = 0; k < count; k++)
          {
            gravity_cdf[i, j].Add(tmp_prob[k]);
            offset[i, j].Add(tmp_offset[k]);
          }
        }
      }
      // this.print_gravity_model();
    }

    public void setup_null_gravity_model()
    {
      var tmp_offset = new int[256 * 256];
      var tmp_prob = new double[256 * 256];
      int count = 0;

      // print_distances();  // DEBUGGING

      offset = new List<int>[rows, cols];
      gravity_cdf = new List<double>[rows, cols];

      max_offset = Convert.ToInt32(rows * this.patch_size);
      Utils.assert(max_offset < 128);

      for (int i_dest = 0; i_dest < rows; i_dest++)
      {
        for (int j_dest = 0; j_dest < cols; j_dest++)
        {
          var dest_patch = this.get_patch(i_dest, j_dest);
          int pop_dest = dest_patch.get_popsize();
          if (pop_dest == 0) continue;
          // double gravity = pow(pop_dest,pop_exponent);
          double gravity = pop_dest;
          int off = 256 * (0 - i_dest + max_offset) + (0 - j_dest + max_offset);
          tmp_offset[count] = off;
          tmp_prob[count++] = gravity;
        }
      }

      // transform gravity values into a prob distribution
      double total = 0.0;
      for (int k = 0; k < count; k++)
      {
        total += tmp_prob[k];
      }
      for (int k = 0; k < count; k++)
      {
        tmp_prob[k] /= total;
      }

      // convert to cdf
      for (int k = 1; k < count; k++)
      {
        tmp_prob[k] += tmp_prob[k - 1];
      }

      // store gravity prob and offsets for this patch
      gravity_cdf[0, 0] = new List<double>();
      offset[0, 0] = new List<int>();
      for (int k = 0; k < count; k++)
      {
        gravity_cdf[0, 0].Add(tmp_prob[k]);
        offset[0, 0].Add(tmp_offset[k]);
      }
    }

    public void print_gravity_model()
    {
      Console.WriteLine();
      Console.WriteLine("=== GRAVITY MODEL ========================================================");
      for (int i_src = 0; i_src < rows; i_src++)
      {
        for (int j_src = 0; j_src < cols; j_src++)
        {
          var src_patch = grid[i_src, j_src];
          double x_src = src_patch.get_center_x();
          double y_src = src_patch.get_center_y();
          int pop_src = src_patch.get_popsize();
          if (pop_src == 0) continue;
          int count = (int)offset[i_src, j_src].Count;
          for (int k = 0; k < count; k++)
          {
            int off = offset[i_src, j_src][k];
            Console.WriteLine("GRAVITY_MODEL row {0.###} col {1.###} pop {2.#####} count {3.####} k {4.####} offset {5} ", i_src, j_src, pop_src, count, k, off);
            int i_dest = i_src + max_offset - (off / 256);
            int j_dest = j_src + max_offset - (off % 256);
            Console.WriteLine("row {0.###} col {0.###} ", i_dest, j_dest);
            var dest_patch = this.get_patch(i_dest, j_dest);
            Utils.assert(dest_patch != null);
            double x_dest = dest_patch.get_center_x();
            double y_dest = dest_patch.get_center_y();
            double dist = Math.Sqrt((x_src - x_dest) * (x_src - x_dest) + (y_src - y_dest) * (y_src - y_dest));
            int pop_dest = dest_patch.get_popsize();
            double gravity_prob = gravity_cdf[i_src, j_src][k];
            if (k > 0)
            {
              gravity_prob -= gravity_cdf[i_src, j_src][k - 1];
            }
            Console.WriteLine("pop {0} dist {1} prob {2}", pop_dest, dist, gravity_prob);
          }
        }
      }
      // exit(0);
    }

    public void print_distances()
    {
      using var fp = new StreamWriter($"{Global.Simulation_directory}/all_distances.dat");
      for (int i_src = 0; i_src < rows; i_src++)
      {
        for (int j_src = 0; j_src < cols; j_src++)
        {
          var src_patch = grid[i_src, j_src];
          double x_src = src_patch.get_center_x();
          double y_src = src_patch.get_center_y();
          int pop_src = src_patch.get_popsize();
          if (pop_src == 0) continue;

          for (int i_dest = 0; i_dest < rows; i_dest++)
          {
            for (int j_dest = 0; j_dest < cols; j_dest++)
            {
              if (i_dest < i_src)
              {
                continue;
              }
              if (i_dest == i_src && j_dest < j_src)
              {
                continue;
              }

              fp.Write("row {0} col {1} pop {2} ", i_src, j_src, pop_src);
              fp.Write("row {0} col {1} ", i_dest, j_dest);
              var dest_patch = this.get_patch(i_dest, j_dest);
              Utils.assert(dest_patch != null);
              double x_dest = dest_patch.get_center_x();
              double y_dest = dest_patch.get_center_y();
              double dist = Math.Sqrt((x_src - x_dest) * (x_src - x_dest) + (y_src - y_dest) * (y_src - y_dest));
              int pop_dest = dest_patch.get_popsize();

              fp.WriteLine("pop {0} dist {1}", pop_dest, dist);
            }
          }
        }
      }
      fp.Flush();
      fp.Dispose();
    }

    public Place select_destination_neighborhood(Place src_neighborhood)
    {
      if (Enable_neighborhood_gravity_model)
      {
        return select_destination_neighborhood_by_gravity_model(src_neighborhood);
      }
      else
      {
        // original FRED neighborhood model (deprecated)
        return select_destination_neighborhood_by_old_model(src_neighborhood);
      }
    }

    public Place select_destination_neighborhood_by_gravity_model(Place src_neighborhood)
    {
      var src_patch = this.get_patch(src_neighborhood.get_latitude(), src_neighborhood.get_longitude());
      int i_src = src_patch.get_row();
      int j_src = src_patch.get_col();
      if (max_distance < 0)
      {
        // use null gravity model
        i_src = j_src = 0;
      }
      int offset_index = FredRandom.DrawFromCdfVector(gravity_cdf[i_src, j_src]);
      int off = offset[i_src, j_src][offset_index];
      int i_dest = i_src + max_offset - (off / 256);
      int j_dest = j_src + max_offset - (off % 256);

      var dest_patch = this.get_patch(i_dest, j_dest);
      Utils.assert(dest_patch != null);

      // int pop_src = src_patch.get_popsize(); int pop_dest = dest_patch.get_popsize();
      // printf("SELECT_DEST src (%3d, %3d) pop %d dest (%3d, %3d) pop %5d\n", i_src,j_src,pop_src,i_dest,j_dest,pop_dest);

      return dest_patch.get_neighborhood();
    }

    public Place select_destination_neighborhood_by_old_model(Place src_neighborhood)
    {
      var src_patch = get_patch(src_neighborhood.get_latitude(), src_neighborhood.get_longitude());
      Neighborhood_Patch dest_patch;
      //int i_src = src_patch.get_row();
      //int j_src = src_patch.get_col();
      double x_src = src_patch.get_center_x();
      double y_src = src_patch.get_center_y();
      double r = FredRandom.NextDouble();

      if (r < this.Home_neighborhood_prob)
      {
        dest_patch = src_patch;
      }
      else
      {
        if (r < this.Community_prob + this.Home_neighborhood_prob)
        {
          // select a random patch with community_prob
          dest_patch = select_random_patch(x_src, y_src, this.Community_distance);
        }
        else
        {
          // select randomly from among immediate neighbors
          dest_patch = select_random_neighbor(src_patch.get_row(), src_patch.get_col());
        }
        if (dest_patch == null || dest_patch.get_houses() == 0) dest_patch = src_patch; // fall back to src patch
      }
      return dest_patch.get_neighborhood();
    }

    public void register_place(Place place)
    {
      var patch = get_patch(place.get_latitude(), place.get_longitude());
      if (patch != null)
      {
        patch.register_place(place);
        //    place.set_patch(patch);  
      }
      else
      {
        Utils.FRED_VERBOSE(0, "register place:can't find patch for place %s county = %d\n",
         place.get_label(), place.get_county_index());
      }
    }

    private class PairComparer : IComparer<Tuple<double, int>>
    {
      public int Compare([AllowNull] Tuple<double, int> x, [AllowNull] Tuple<double, int> y)
      {
        if (x == null)
        {
          return -1;
        }

        if (y == null)
        {
          return 1;
        }

        return x.Item1 == y.Item1
                ? x.Item2.CompareTo(y.Item2)
                : x.Item1.CompareTo(y.Item1);
      }
    }

    private class MoreRoomComparer : IComparer<Place>
    {
      public int Compare([AllowNull] Place x, [AllowNull] Place y)
      {

        var s1 = (School)x;
        var s2 = (School)y;
        int vac1 = s1.get_orig_number_of_students() - s1.get_number_of_students();
        int vac2 = s2.get_orig_number_of_students() - s2.get_number_of_students();
        // return vac1 > vac2;
        double relvac1 = (vac1 + 0.000001) / (s1.get_orig_number_of_students() + 1.0);
        double relvac2 = (vac2 + 0.000001) / (s2.get_orig_number_of_students() + 1.0);
        // resolve ties by place id
        return (relvac1 == relvac2)
          ? s1.get_id().CompareTo(s2.get_id())
          : relvac1.CompareTo(relvac2);
      }
    }
  }
}
