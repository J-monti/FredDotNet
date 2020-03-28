namespace Fred
{
  public class VisualizationPatch : AbstractPatch
  {
    protected int count;
    protected int popsize;

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
    public override void setup(int i, int j, double patch_size, double grid_min_x, double grid_min_y)
    {
      base.setup(i, j, patch_size, grid_min_x, grid_min_x);
      reset_counts();
    }

    public void quality_control()
    {
      return;
    }

    public void print()
    {
      //FRED_VERBOSE(0, "visualization_patch: %d %d %d %d\n", row, col, count, popsize);
    }
  }
}
