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
      this.name = "Vaccine Priority Decision Specific Age";
      this.type = "Y/N";
      this.policy = p;
    }

    public override int evaluate(Person person, int disease, int day)
    {
      var vcm = (Vaccine_Manager)this.policy.manager;
      int low_age = vcm.get_vaccine_priority_age_low();
      int high_age = vcm.get_vaccine_priority_age_high();

      if (person.get_age() >= low_age && person.get_age() <= high_age)
      {
        return 1;
      }
      return -1;
    }
  }
}
