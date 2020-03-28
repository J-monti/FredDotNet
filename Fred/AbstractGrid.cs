namespace Fred
{
  public abstract class AbstractGrid
  {
    public int Rows { get; protected set; }

    public int Cols { get; protected set; }

    public int MinGlobalRow { get; protected set; }

    public int MinGlobalCol { get; protected set; }

    public int MaxGlobalRow { get; protected set; }

    public int MaxGlobalCol { get; protected set; }

    public double MinLat { get; protected set; }

    public double MinLon { get; protected set; }

    public double MaxLat { get; protected set; }

    public double MaxLon { get; protected set; }

    public double MinX { get; protected set; }

    public double MinY { get; protected set; }

    public double MaxX { get; protected set; }

    public double MaxY { get; protected set; }

    public double PatchSize { get; protected set; }

    public int NumberOfPatches
    {
      get { return this.Rows * this.Cols; }
    }

    public int GetRow(double y)
    {
      return (int)((y - this.MinY) / this.PatchSize);
    }

    public int GetCol(double x)
    {
      return (int)(((x - this.MinX) / this.PatchSize));
    }

    public int GetRowLat(double lat)
    {
      double y = Geo.GetY(lat);
      return (int)((y - this.MinY) / this.PatchSize);
    }

    public int GetRowLon(double lon)
    {
      double x = Geo.GetX(lon);
      return (int)((x - this.MinX) / this.PatchSize);
    }

    public int GetGlobalRow(int row)
    {
      return row + this.MinGlobalRow;
    } // ???

    public int GetGlobalCol(int col)
    {
      return col + this.MinGlobalCol;
    } // ???
  }
}
