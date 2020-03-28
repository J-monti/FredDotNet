using System;

namespace Fred
{
  public class AntiviralDecisionAllowOnlyOne : Decision
  {
    public AntiviralDecisionAllowOnlyOne() { }
    public AntiviralDecisionAllowOnlyOne(Policy policy)
      : base(policy)
    {
      this.Name = "AV Decision Allow Only One AV per Person";
      this.Type = "Y/N";
    }

    public override int Evaluate(Person person, int disease, DateTime currentDay)
    {
      if (person.Health.NumberAVTaken == 0)
      {
        return 0;
      }
      
      return -1;
    }
  }
}
