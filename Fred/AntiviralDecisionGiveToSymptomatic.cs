using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class AntiviralDecisionGiveToSymptomatic : Decision
  {
    public AntiviralDecisionGiveToSymptomatic() { }
    public AntiviralDecisionGiveToSymptomatic(Policy policy)
      : base(policy)
    {
      this.Name = "AV Decision to give to a percentage of symptomatics";
      this.Type = "Y/N";
    }

    public override int Evaluate(Person person, int disease, DateTime currentDay)
    {
      var av = this.Policy.Manager.CurrentAV;
      double percentage = av.PercentSymptomatics;
      if (person.Health.IsSymptomatic(disease))
      {
        person.Health.FlipCheckedForAv(disease);
        double r = FredRandom.NextDouble(); // This is now a probability <=1.0;
        if (r < percentage)
        {
          return 0;
        }
      }
      return -1;
    }
  }
}
