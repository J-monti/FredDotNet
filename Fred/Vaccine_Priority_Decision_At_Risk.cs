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
      this.Name = "Vaccine Priority Decision to Include At_Risk";
      this.Type = "Y/N";
      this.Policy = p;
    }

    public override int Evaluate(Person person, int disease, DateTime day)
    {
      return person.Demographics.IsAtRisk(disease) ? 1 : -1;
    }
  }
}
