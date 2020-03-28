using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Priority_Decision_No_Priority : Decision
  {
    public Vaccine_Priority_Decision_No_Priority() : base() { }

    public Vaccine_Priority_Decision_No_Priority(Policy p) : base(p)
    {
      this.Name = "Vaccine Priority Decision No Priority";
      this.Type = "Y/N";
      this.Policy = p;
    }

    public override int Evaluate(Person person, int disease, DateTime day)
    {
      return -1;
    }
  }
}
