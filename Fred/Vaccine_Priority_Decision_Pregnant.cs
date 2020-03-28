using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Priority_Decision_Pregnant : Decision
  {
    public Vaccine_Priority_Decision_Pregnant() : base() { }

    public Vaccine_Priority_Decision_Pregnant(Policy p) : base(p)
    {
      this.Name = "Vaccine Priority Decision to Include Pregnant Women";
      this.Type = "Y/N";
      this.Policy = p;
    }

    public override int Evaluate(Person person, int disease, DateTime day)
    {
      return person.Demographics.IsPregnant ? 1 :- 1;
    }
  }
}
