using System;
using System.Collections.Generic;
using System.Text;

namespace Fred.Tests
{
  public class County
  {
    public County(int id, string name, int stateId, string fips)
    {
      Id = id;
      Name = name;
      StateId = stateId;
      Fips = fips;
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public int StateId { get; set; }

    public string Fips { get; set; }
  }
}
