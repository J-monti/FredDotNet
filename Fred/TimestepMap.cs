using System;
using System.Collections.Generic;
using System.Text;

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
