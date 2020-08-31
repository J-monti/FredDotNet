using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class AV_Decision_Give_to_Sympt : Decision
  {
    public AV_Decision_Give_to_Sympt() { }
    public AV_Decision_Give_to_Sympt(Policy policy)
      : base(policy)
    {
      this.name = "AV Decision to give to a percentage of symptomatics";
      this.type = "Y/N";
    }

    public override int evaluate(Person person, int disease, int current_day)
    {
      var avm = (AV_Manager)policy.get_manager();
      var av = avm.get_current_av();
      double percentage = av.get_percent_symptomatics();
      if (person.get_health().is_symptomatic(disease))
      {
        person.get_health().flip_checked_for_av(disease);
        double r = FredRandom.NextDouble(); // This is now a probability <=1.0;
        if (r < percentage) return 0;
      }
      return -1;
    }
  }
}
