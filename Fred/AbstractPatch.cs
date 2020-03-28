using System;

namespace Fred
{
  public abstract class AbstractPatch
  {
    public int Row { get; protected set; }
    
    public int Col { get; protected set; }

    public double MinX { get; protected set; }

    public double MinY { get; protected set; }

    public double MaxX { get; protected set; }

    public double MaxY { get; protected set; }

    public double CenterX { get; protected set; }

    public double CenterY { get; protected set; }

    public virtual void setup(int i, int j, double patch_size, double grid_min_x, double grid_min_y)
    {
      this.Row = i;
      this.Col = j;
      this.MinX = grid_min_x + this.Col * patch_size;
      this.MinY = grid_min_y + this.Row * patch_size;
      this.MaxX = grid_min_x + (this.Col + 1) * patch_size;
      this.MaxY = grid_min_y + (this.Row + 1) * patch_size;
      this.CenterY = (this.MinY + this.MaxY) / 2.0;
      this.CenterX = (this.MinX + this.MaxX) / 2.0;
    }

    public virtual double distance_to_patch(AbstractPatch p2)
    {
      double x1 = this.CenterX;
      double y1 = this.CenterY;
      double x2 = p2.CenterX;
      double y2 = p2.CenterY;
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public override string ToString()
    {
      return string.Format("patch {0} {1}: {2}, {3}, {4}, {5}", this.Row, this.Col, this.MinX, this.MinY, this.MaxX, this.MaxY);
    }

    public override bool Equals(object obj)
    {
      return obj is AbstractPatch patch &&
             Row == patch.Row &&
             Col == patch.Col &&
             MinX == patch.MinX &&
             MinY == patch.MinY &&
             MaxX == patch.MaxX &&
             MaxY == patch.MaxY &&
             CenterX == patch.CenterX &&
             CenterY == patch.CenterY;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Row, Col, MinX, MinY, MaxX, MaxY, CenterX, CenterY);
    }
  }
}
