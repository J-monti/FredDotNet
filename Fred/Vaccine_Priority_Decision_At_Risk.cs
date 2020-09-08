using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Priority_Decision_At_Risk : Decision
  {
    public Vaccine_Priority_Decision_At_Risk() : base() { }

    public Vaccine_Priority_Decision_At_Risk(Policy p) : base(p)
    {
      this.name = "Vaccine Priority Decision to Include At_Risk";
      this.type = "Y/N";
      this.policy = p;
    }

    public override int evaluate(Person person, int disease, int day)
    {
      return person.get_health().is_at_risk(disease) ? 1 : -1;
    }
  }
}
