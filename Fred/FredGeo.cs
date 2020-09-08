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

    public static implicit operator FredGeo(double value) => new FredGeo(value);

    public static implicit operator double(FredGeo geo) => geo.Value;

    public static FredGeo operator +(FredGeo a, FredGeo b) => new FredGeo(a.Value + b.Value);

    public static FredGeo operator -(FredGeo a) => new FredGeo(-a.Value);

    public static FredGeo operator -(FredGeo a, FredGeo b) => new FredGeo(a.Value - b.Value);
  }
}
