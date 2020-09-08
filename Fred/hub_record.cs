using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public struct hub_record
  {
    public int id;
    public int pop;
    public int pct;
    public double lat;
    public double lon;
    public List<Person> users;
  }
}
