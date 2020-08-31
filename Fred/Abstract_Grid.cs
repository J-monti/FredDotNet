using System;

namespace Fred
{
  public abstract class Abstract_Grid
  {
    protected int rows;         // number of rows
    protected int cols;         // number of columns
    protected double patch_size;        // km per side
    protected FredGeo min_lat;        // lat of SW corner
    protected FredGeo min_lon;        // lon of SW corner
    protected FredGeo max_lat;        // lat of NE corner
    protected FredGeo max_lon;        // lon of NE corner
    protected double min_x;         // global x of SW corner
    protected double min_y;         // global y of SW corner
    protected double max_x;         // global x of NE corner
    protected double max_y;         // global y of NE corner
    protected int global_row_min;       // global row coord of S row
    protected int global_col_min;       // global col coord of W col
    protected int global_row_max;       // global row coord of N row
    protected int global_col_max;				// global col coord of E col

    public int get_rows()
    {
      return this.rows;
    }

    public int get_cols()
    {
      return this.cols;
    }

    public int get_number_of_patches()
    {
      return this.rows * this.cols;
    }

    public FredGeo get_min_lat()
    {
      return this.min_lat;
    }

    public FredGeo get_min_lon()
    {
      return this.min_lon;
    }

    public FredGeo get_max_lat()
    {
      return this.max_lat;
    }

    public FredGeo get_max_lon()
    {
      return this.max_lon;
    }

    public double get_min_x()
    {
      return this.min_x;
    }

    public double get_min_y()
    {
      return this.min_y;
    }

    public double get_max_x()
    {
      return this.max_x;
    }

    public double get_max_y()
    {
      return this.max_y;
    }

    public double get_patch_size()
    {
      return this.patch_size;
    }

    public int get_row(double y)
    {
      return Convert.ToInt32((y - this.min_y) / this.patch_size);
    }

    public int get_col(double x)
    {
      return Convert.ToInt32((x - this.min_x) / this.patch_size);
    }

    public int get_row(FredGeo lat)
    {
      double y = Geo.get_y(lat);
      return (int)((y - min_y) / patch_size);
    }

    public int get_col(FredGeo lon)
    {
      double x = Geo.get_x(lon);
      return Convert.ToInt32((x - this.min_x) / this.patch_size);
    }

    public int getGlobalRow(int row)
    {
      return row + this.global_row_min;
    } // ???

    public int getGlobalCol(int col)
    {
      return col + this.global_col_min;
    } // ???

    public int get_global_row_min()
    {
      return this.global_row_min;        // global row coord of S row
    }

    public int get_global_col_min()
    {
      return this.global_col_min;        // global col coord of W col
    }

    public int get_global_row_max()
    {
      return this.global_row_max;        // global row coord of N row
    }

    public int get_global_col_max()
    {
      return this.global_col_max;        // global col coord of E col
    }
  }
}
