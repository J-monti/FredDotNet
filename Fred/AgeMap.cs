using System.Collections.Generic;

namespace Fred
{
  public class AgeMap
  {
    public AgeMap()
    {
      this.Ages = new Dictionary<int, double>();
    }
    public AgeMap(string name)
    {
      this.Name = name;
      this.Ages = new Dictionary<int, double>();
    }

    public string Name { get; }

    public Dictionary<int, double> Ages { get; }

    public void SetAll(Dictionary<int, double> ages)
    {
      this.Ages.Clear();
      foreach (var age in ages)
      {
        this.Ages.Add(age.Key, age.Value);
      }
    }

    public void SetAllAges(double value)
    {
      this.Ages.Clear();
      this.Ages.Add(Demographics.MAX_AGE, value);
    }

    public double FindValue(int age)
    {
      if (this.Ages.ContainsKey(age))
      {
        return this.Ages[age];
      }

      return 0.0;
    }
  }
}
