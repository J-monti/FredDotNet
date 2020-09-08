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
      this.name = "Vaccine Priority Decision to Include Pregnant Women";
      this.type = "Y/N";
      this.policy = p;
    }

    public override int evaluate(Person person, int disease, int day)
    {
      return person.get_demographics().is_pregnant() ? 1 :- 1;
    }
  }
}
