using System;

namespace Fred
{
  public class Visualization_Patch : Abstract_Patch
  {
    protected int count;
    protected int popsize;

    public void setup(int i, int j, double patch_size, double grid_min_x, double grid_min_y)
    {
      this.row = i;
      this.col = j;
      this.min_x = grid_min_x + (this.col) * patch_size;
      this.min_y = grid_min_y + (this.row) * patch_size;
      this.max_x = grid_min_x + (this.col + 1) * patch_size;
      this.max_y = grid_min_y + (this.row + 1) * patch_size;
      this.center_y = (this.min_y + this.max_y) / 2.0;
      this.center_x = (this.min_x + this.max_x) / 2.0;
      reset_counts();
    }

    public void quality_control() { return; }
    public double distance_to_patch(Visualization_Patch patch2)
    {
      double x1 = this.center_x;
      double y1 = this.center_y;
      double x2 = patch2.get_center_x();
      double y2 = patch2.get_center_y();
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public new void print()
    {
      Utils.FRED_VERBOSE(0, "visualization_patch: %d %d %d %d\n", row, col, count, popsize);
    }

    public void reset_counts()
    {
      this.count = 0;
      this.popsize = 0;
    }

    public void update_patch_count(int n, int total)
    {
      this.count += n;
      this.popsize += total;
    }

    public int get_count()
    {
      return this.count;
    }

    public int get_popsize()
    {
      return this.popsize;
    }
  }
}
