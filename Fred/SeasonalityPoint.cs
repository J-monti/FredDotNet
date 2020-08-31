using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class SeasonalityPoint
  {
    public SeasonalityPoint(int x, int y, double value)
    {
      this.X = x;
      this.Y = y;
      this.Value = value;
    }

    public int X { get; }

    public int Y { get; }

    public double Value { get; }
  }
}
