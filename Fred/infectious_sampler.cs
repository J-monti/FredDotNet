using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public struct infectious_sampler
  {
    public double prob;
    public List<Person>[] samples;
    //public void operator() (Person &p);
  }
}
