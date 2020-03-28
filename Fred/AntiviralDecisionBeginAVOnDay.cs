using System;

namespace Fred
{
  public class AntiviralDecisionBeginAVOnDay : Decision
  {
    public AntiviralDecisionBeginAVOnDay() { }
    public AntiviralDecisionBeginAVOnDay(Policy policy)
      : base(policy)
    {
      this.Name = "AV Decision to Begin disseminating AVs on a certain day";
      this.Type = "Y/N";
    }

    public override int Evaluate(Person person, int disease, DateTime currentDay)
    {
      var av = this.Policy.Manager.CurrentAV;
      if (currentDay >= av.StartDay)
      {
        return 0;
      }

      return -1;
    }
  }
}
