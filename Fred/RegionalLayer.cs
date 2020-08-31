using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class RegionalLayer : AbstractGrid
  {
    public RegionalLayer()
    {
    }

    public RegionalPatch[,] Grid { get; private set; }

    public void Setup(double minlon, double minlat, double maxlon, double maxlat, double regionalPatchSize)
    {
      this.MinLon = minlon;
      this.MinLat = minlat;
      this.MaxLon = maxlon;
      this.MaxLat = maxlat;
      this.PatchSize = regionalPatchSize;

      // find the global x,y coordinates of SW corner of grid
      this.MinX = Geo.GetX(this.MinLon);
      this.MinY = Geo.GetY(this.MinLat);

      // find the global row and col in which SW corner occurs
      this.MinGlobalRow = (int)(this.MinY / this.PatchSize);
      this.MinGlobalCol = (int)(this.MinX / this.PatchSize);

      // align coords to global grid
      this.MinX = this.MinGlobalCol * this.PatchSize;
      this.MinY = this.MinGlobalRow * this.PatchSize;

      // compute lat,lon of SW corner of aligned grid
      this.MinLat = Geo.GetLatitude(this.MinY);
      this.MinLon = Geo.GetLongitude(this.MinX);

      // find x,y coords of NE corner of bounding box
      this.MaxX = Geo.GetX(this.MaxLon);
      this.MaxY = Geo.GetY(this.MaxLat);

      // find the global row and col in which NE corner occurs
      this.MaxGlobalRow = (int)(this.MaxY / this.PatchSize);
      this.MaxGlobalCol = (int)(this.MaxX / this.PatchSize);

      // align coords_y to global grid
      this.MaxX = (this.MaxGlobalCol + 1) * this.PatchSize;
      this.MaxY = (this.MaxGlobalRow + 1) * this.PatchSize;

      // compute lat,lon of NE corner of aligned grid
      this.MaxLat = Geo.GetLatitude(this.MaxY);
      this.MaxLon = Geo.GetLongitude(this.MaxX);

      // number of rows and columns needed
      this.Rows = this.MaxGlobalRow - this.MinGlobalRow + 1;
      this.Cols = this.MaxGlobalCol - this.MinGlobalCol + 1;

      if (Global.Verbose > 0)
      {
        Console.WriteLine("Regional_Layer new min_lon = {0}", this.MinLon);
        Console.WriteLine("Regional_Layer new min_lat = {0}", this.MinLat);
        Console.WriteLine("Regional_Layer new max_lon = {0}", this.MaxLon);
        Console.WriteLine("Regional_Layer new max_lat = {0}", this.MaxLat);
        Console.WriteLine("Regional_Layer rows = {0}  cols = {1}", this.Rows, this.Cols);
        Console.WriteLine("Regional_Layer min_x = {0}  min_y = {1}", this.MinX, this.MinY);
        Console.WriteLine("Regional_Layer max_x = {0}  max_y = {1}", this.MaxX, this.MaxY);
        Console.WriteLine("Regional_Layer global_col_min = {0}  global_row_min = {1}", this.MinGlobalCol, this.MinGlobalRow);
      }

      this.Grid = new RegionalPatch[this.Rows, this.Cols];
      for (int i = 0; i < this.Rows; ++i)
      {
        for (int j = 0; j < this.Cols; ++j)
        {
          this.Grid[i, j] = new RegionalPatch();
          this.Grid[i,j].Setup(this, i, j);
          if (Global.Verbose > 1)
          {
            Console.WriteLine("print Grid[{0},{1}]:\n", i, j);
            Console.WriteLine(this.Grid[i, j].ToString());
          }
        }
      }
    }

    public RegionalPatch get_patch(int row, int col)
    {
      if (row >= 0 && col >= 0 && row < this.Rows && col < this.Cols)
      {
        return this.Grid[row, col];
      }

      return null;
    }

    public RegionalPatch get_patch(double lat, double lon)
    {
      int row = GetRow(lat);
      int col = GetCol(lon);
      if (row >= 0 && col >= 0 && row < this.Rows && col < this.Cols)
      {
        return this.Grid[row, col];
      }

      return null;
    }

    public RegionalPatch get_patch(Place place)
    {
      return get_patch(place.get_latitude(), place.get_longitude());
    }

    public RegionalPatch get_patch_with_global_coords(int row, int col)
    {
      return get_patch(row - this.MinGlobalRow, col - this.MinGlobalCol);
    }

    public RegionalPatch get_patch_from_id(int id)
    {
      int row = id / this.Cols;
      int col = id % this.Cols;
      if (Global.Verbose > 4)
      {
        Console.WriteLine("patch lookup for id = {0} ... calculated row = {1}, col = {2}, rows = {3}, cols = {4}",
          id, row, col, this.Rows, this.Cols);
      }

      return this.Grid[row, col];
    }

    public RegionalPatch select_random_patch()
    {
      int row = FredRandom.Next(0, this.Rows - 1);
      int col = FredRandom.Next(0, this.Cols - 1);
      return this.Grid[row, col];
    }

    public void quality_control()
    {
      if (Global.Verbose > 0)
      {
        Console.WriteLine("grid quality control check");
      }

      for (int row = 0; row < this.Rows; ++row)
      {
        for (int col = 0; col < this.Cols; ++col)
        {
          this.Grid[row, col].quality_control();
        }
      }

      if (Global.Verbose > 1)
      {
        var filePath = Path.Combine(Global.SimulationDirectory, "large_grid.dat");
        using (var writer = new StreamWriter(filePath))
        {
          for (int row = 0; row < this.Rows; ++row)
          {
            if (row % 2 != 0)
            {
              for (int col = this.Cols - 1; col >= 0; --col)
              {
                double x = this.Grid[row, col].CenterX;
                double y = this.Grid[row, col].CenterY;
                writer.WriteLine("{0} {1}", x, y);
              }
            }
            else
            {
              for (int col = 0; col < this.Cols; ++col)
              {
                double x = this.Grid[row, col].CenterX;
                double y = this.Grid[row, col].CenterY;
                writer.WriteLine("{0} {1}", x, y);
              }
            }
          }
        }
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine("grid quality control finished");
      }
    }

    // Specific to Regional_Patch Regional_Layer:

    public void set_population_size()
    {
      for (int p = 0; p < Global.Population.get_index_size(); ++p)
      {
        var person = Global.Population.get_person_by_index(p);
        if (person != null)
        {
          Place hh = person.get_household();
          if (hh == null)
          {
            if (Global.IsHospitalsEnabled && person.is_hospitalized() && person.get_permanent_household() != null)
            {
              hh = person.get_permanent_household();
            }
          }
          int row = this.GetRow(hh.get_latitude());
          int col = this.GetCol(hh.get_longitude());
          var patch = get_patch(row, col);
          patch.AddPerson(person);
        }
      }
    }

    public void read_max_popsize()
    {
      throw new NotImplementedException();
      /*int r, c, n;
      char filename[FRED_STRING_SIZE];
      if (Global.IsTravelEnabled)
      {
        Params::get_param_from_string("regional_patch_popfile", filename);
        FILE* fp = Utils::fred_open_file(filename);
        if (fp == null)
        {
          FredUtils.Abort("Help! Can't open patch_pop_file %s\n", filename);
        }
        printf("reading %s\n", filename);
        while (fscanf(fp, "%d %d %d ", &c, &r, &n) == 3)
        {
          Regional_Patch* patch = get_patch_with_global_coords(r, c);
          if (patch != null)
          {
            patch.set_max_popsize(n);
          }
        }
        fclose(fp);
        printf("finished reading %s\n", filename);
      }*/
    }

    public void AddWorkplace(Place place)
    {
      RegionalPatch patch = this.get_patch(place);
      if (patch != null)
      {
        patch.AddWorkplace(place);
      }
    }


    public Place GetNearbyWorkplace(int row, int col, double x, double y, int min_staff, int max_staff, ref double min_dist)
    {
      //find nearest workplace that has right number of employees
      Place workPlace = null;
      min_dist = 1e99;
      for (int i = row - 1; i <= row + 1; ++i)
      {
        for (int j = col - 1; j <= col + 1; ++j)
        {
          RegionalPatch patch = this.get_patch(i, j);
          if (patch != null)
          {
            Console.WriteLine("Looking for nearby workplace in row {0} col {1}", i, j);
            var closest_workplace = patch.GetClosestWorkplace(x, y, min_staff, max_staff, ref min_dist);
            if (closest_workplace != null)
            {
              workPlace = closest_workplace;
            }
            else
            {
              Console.WriteLine("No nearby workplace in row {0} col {1}", i, j);
            }
          }
        }
      }

      return workPlace;
    }

    void Unenroll(double lat, double lon, Person person)
    {
      var patch = this.get_patch(lat, lon);
      if (patch != null)
      {
        patch.Unenroll(person);
      }
    }

    public bool IsInRegion(double lat, double lon)
    {
      return this.get_patch(lat, lon) != null;
    }
  }
}
