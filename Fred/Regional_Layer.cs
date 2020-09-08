using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Regional_Layer : Abstract_Grid
  {
    private Regional_Patch[,] grid;

    public Regional_Layer(FredGeo minlon, FredGeo minlat, FredGeo maxlon, FredGeo maxlat)
    {
      this.min_lon = minlon;
      this.min_lat = minlat;
      this.max_lon = maxlon;
      this.max_lat = maxlat;
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "Regional_Layer min_lon = {0}", this.min_lon);
        Utils.FRED_STATUS(0, "Regional_Layer min_lat = {0}", this.min_lat);
        Utils.FRED_STATUS(0, "Regional_Layer max_lon = {0}", this.max_lon);
        Utils.FRED_STATUS(0, "Regional_Layer max_lat = {0}", this.max_lat);
      }

      // read in the patch size for this layer
      FredParameters.GetParameter("regional_patch_size", ref this.patch_size);

      // find the global x,y coordinates of SW corner of grid
      this.min_x = Geo.get_x(this.min_lon);
      this.min_y = Geo.get_y(this.min_lat);

      // find the global row and col in which SW corner occurs
      this.global_row_min = (int)(this.min_y / this.patch_size);
      this.global_col_min = (int)(this.min_x / this.patch_size);

      // align coords to global grid
      this.min_x = this.global_col_min * this.patch_size;
      this.min_y = this.global_row_min * this.patch_size;

      // compute lat,lon of SW corner of aligned grid
      this.min_lat = Geo.get_latitude(this.min_y);
      this.min_lon = Geo.get_longitude(this.min_x);

      // find x,y coords of NE corner of bounding box
      this.max_x = Geo.get_x(this.max_lon);
      this.max_y = Geo.get_y(this.max_lat);

      // find the global row and col in which NE corner occurs
      this.global_row_max = (int)(this.max_y / this.patch_size);
      this.global_col_max = (int)(this.max_x / this.patch_size);

      // align coords_y to global grid
      this.max_x = (this.global_col_max + 1) * this.patch_size;
      this.max_y = (this.global_row_max + 1) * this.patch_size;

      // compute lat,lon of NE corner of aligned grid
      this.max_lat = Geo.get_latitude(this.max_y);
      this.max_lon = Geo.get_longitude(this.max_x);

      // number of rows and columns needed
      this.rows = this.global_row_max - this.global_row_min + 1;
      this.cols = this.global_col_max - this.global_col_min + 1;

      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "Regional_Layer new min_lon = {0}", this.min_lon);
        Utils.FRED_STATUS(0, "Regional_Layer new min_lat = {0}", this.min_lat);
        Utils.FRED_STATUS(0, "Regional_Layer new max_lon = {0}", this.max_lon);
        Utils.FRED_STATUS(0, "Regional_Layer new max_lat = {0}", this.max_lat);
        Utils.FRED_STATUS(0, "Regional_Layer rows = {0}  cols = {1}", this.rows, this.cols);
        Utils.FRED_STATUS(0, "Regional_Layer min_x = {0}  min_y = {1}", this.min_x, this.min_y);
        Utils.FRED_STATUS(0, "Regional_Layer max_x = {0}  max_y = {1}", this.max_x, this.max_y);
        Utils.FRED_STATUS(0, "Regional_Layer global_col_min = {0}  global_row_min = {1}",
          this.global_col_min, this.global_row_min);
      }

      this.grid = new Regional_Patch[this.rows, this.cols];
      for (int i = 0; i < this.rows; ++i)
      {
        for (int j = 0; j < this.cols; ++j)
        {
          this.grid[i, j] = new Regional_Patch(this, i, j);
          if (Global.Verbose > 1)
          {
            Console.Write("print grid[{0}][{1}]:\n", i, j);
            this.grid[i, j].print();
          }
          //printf( "row = %d col = %d id = %d\n", i, j, grid[i][j].get_id() );
        }
      }
    }

    //public Regional_Patch[,] get_neighbors(int row, int col);

    public Regional_Patch get_patch(int row, int col)
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

    public Regional_Patch get_patch(FredGeo lat, FredGeo lon)
    {
      int row = get_row(lat);
      int col = get_col(lon);
      if (row >= 0 && col >= 0 && row < this.rows && col < this.cols)
      {
        return this.grid[row, col];
      }
      else
      {
        return null;
      }
    }

    public Regional_Patch get_patch(Place place)
    {
      return get_patch(place.get_latitude(), place.get_longitude());
    }

    public Regional_Patch get_patch_with_global_coords(int row, int col)
    {
      return get_patch(row - this.global_row_min, col - this.global_col_min);
    }

    public Regional_Patch get_patch_from_id(int id)
    {
      int row = id / this.cols;
      int col = id % this.cols;
      Utils.FRED_VERBOSE(4,
             "patch lookup for id = %d ... calculated row = %d, col = %d, rows = %d, cols = %d\n", id,
             row, col, rows, cols);
      Utils.assert(this.grid[row, col].get_id() == id);
      return this.grid[row, col];
    }

    public Regional_Patch select_random_patch()
    {
      int row = FredRandom.Next(0, this.rows - 1);
      int col = FredRandom.Next(0, this.cols - 1);
      return this.grid[row, col];
    }

    public void add_workplace(Place place)
    {
      var patch = this.get_patch(place);
      if (patch != null)
      {
        patch.add_workplace(place);
      }

    }
    public Place get_nearby_workplace(int row, int col, double x, double y, int min_staff, int max_staff, ref double min_dist)
    {
      //find nearest workplace that has right number of employees
      Place nearby_workplace = null;
      min_dist = 1e99;
      for (int i = row - 1; i <= row + 1; ++i)
      {
        for (int j = col - 1; j <= col + 1; ++j)
        {
          var patch = get_patch(i, j);
          if (patch != null)
          {
            // printf("Looking for nearby workplace in row %d col %d\n", i, j); fflush(stdout);
            var closest_workplace = patch.get_closest_workplace(x, y, min_staff, max_staff, ref min_dist);
            if (closest_workplace != null)
            {
              nearby_workplace = closest_workplace;
            }
            else
            {
              // printf("No nearby workplace in row %d col %d\n", i, j); fflush(stdout);
            }
          }
        }
      }
      return nearby_workplace;
    }

    public void set_population_size()
    {
      for (int p = 0; p < Global.Pop.get_index_size(); ++p)
      {
        var person = Global.Pop.get_person_by_index(p);
        if (person != null)
        {
          var hh = person.get_household();
          if (hh == null)
          {
            if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
            {
              hh = person.get_permanent_household();
            }
          }

          Utils.assert(hh != null);
          int row = get_row(hh.get_latitude());
          int col = get_col(hh.get_longitude());
          var patch = get_patch(row, col);
          patch.add_person(person);
        }
      }
    }

    public void quality_control()
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "grid quality control check\n");
      }

      for (int row = 0; row < this.rows; ++row)
      {
        for (int col = 0; col < this.cols; ++col)
        {
          this.grid[row, col].quality_control();
        }
      }

      if (Global.Verbose > 1)
      {
        var filename = $"{Global.Simulation_directory}/large_grid.dat";
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

      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "grid quality control finished\n");
      }
    }

    public void read_max_popsize()
    {
      int r, c, n;
      string filename = string.Empty;
      if (Global.Enable_Travel)
      {
        FredParameters.GetParameter("regional_patch_popfile", ref filename);
        if (!File.Exists(filename))
        {
          Utils.fred_abort("Help! Can't open patch_pop_file %s\n", filename);
        }

        using var fp = new StreamReader(filename);
        Console.WriteLine("reading {0}", filename);
        while (fp.Peek() != -1)
        //while (fscanf(fp, "%d %d %d ", &c, &r, &n) == 3)
        {
          var line = fp.ReadLine();
          var tokens = line.Split(' ');
          c = Convert.ToInt32(tokens[0]);
          r = Convert.ToInt32(tokens[1]);
          n = Convert.ToInt32(tokens[2]);
          var patch = get_patch_with_global_coords(r, c);
          if (patch != null)
          {
            patch.set_max_popsize(n);
          }
        }
        fp.Dispose();
        Console.WriteLine("finished reading {0}", filename);
      }
    }

    public void unenroll(FredGeo lat, FredGeo lon, Person person)
    {
      var regional_patch = this.get_patch(lat, lon);
      if (regional_patch != null)
      {
        regional_patch.unenroll(person);
      }
    }

    public bool is_in_region(FredGeo lat, FredGeo lon)
    {
      return this.get_patch(lat, lon) != null;
    }
  }
}
