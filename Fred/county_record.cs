using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class county_record
  {
    public int id;
    public int pop;
    public int people_immunized;
    public readonly int[] people_by_age = new int[102];
    public readonly double[] immunity_by_age = new double[102];
    public readonly List<Person> habitants = new List<Person>();
  }
}
