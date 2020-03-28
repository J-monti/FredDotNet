using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Priority_Decision_Specific_Age : Decision
  {
    public Vaccine_Priority_Decision_Specific_Age() : base () { }

    public Vaccine_Priority_Decision_Specific_Age(Policy p) : base (p)
    {
      this.Name = "Vaccine Priority Decision Specific Age";
      this.Type = "Y/N";
      this.Policy = p;
    }

    public override int Evaluate(Person person, int disease, DateTime day)
    {
      var vcm = (VaccineManager)this.Policy.Manager;
      int low_age = vcm.get_vaccine_priority_age_low();
      int high_age = vcm.get_vaccine_priority_age_high();

      if (person.Age >= low_age && person.Age <= high_age)
      {
        return 1;
      }
      return -1;
    }
  }
}
