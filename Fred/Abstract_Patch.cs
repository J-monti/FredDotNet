using System;

namespace Fred
{
  public abstract class Abstract_Patch
  {
    protected int row;
    protected int col;
    protected double min_x;
    protected double max_x;
    protected double min_y;
    protected double max_y;
    protected double center_x;
    protected double center_y;

    public int get_row()
    {
      return this.row;
    }

    public int get_col()
    {
      return this.col;
    }

    public double get_min_x()
    {
      return this.min_x;
    }

    public double get_max_x()
    {
      return this.max_x;
    }

    public double get_min_y()
    {
      return this.min_y;
    }

    public double get_max_y()
    {
      return this.max_y;
    }

    public double get_center_y()
    {
      return this.center_y;
    }

    public double get_center_x()
    {
      return this.center_x;
    }

    public void print()
    {
      Console.WriteLine("patch {0} {1}: {2}, {3}, {4}, {5}", this.row, this.col, this.min_x, this.min_y, this.max_x, this.max_y);
    }
  }
}
