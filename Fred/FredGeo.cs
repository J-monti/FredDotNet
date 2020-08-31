using System;

namespace Fred
{
  public struct FredGeo
  {
    public FredGeo(double value)
    {
      this.Value = value;
    }

    public double Value { get; set; }

    public override bool Equals(object obj)
    {
      return obj is FredGeo geo &&
             this.Value == geo.Value;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(this.Value);
    }

    public override string ToString()
    {
      return $"{this.Value}";
    }
  }
}
