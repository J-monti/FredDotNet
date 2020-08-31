using System.Collections.Generic;

namespace Fred
{
  public class TimestepMap : Dictionary<int, int>
  {
    public TimestepMap(string name)
    {
      this.Name = name;
    }

    public string Name { get; }
  }
}
