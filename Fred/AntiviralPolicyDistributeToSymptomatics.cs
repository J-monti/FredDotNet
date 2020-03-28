namespace Fred
{
  public class AntiviralPolicyDistributeToSymptomatics : Policy
  {
    public AntiviralPolicyDistributeToSymptomatics(Manager manager)
      : base(manager)
    {
      this.Name = "Distribute AVs to Symptomatics";
      // Need to add the policies in the decisions
      this.Decisions.Add(new AntiviralDecisionBeginAVOnDay(this));
      this.Decisions.Add(new AntiviralDecisionGiveToSymptomatic(this));
      this.Decisions.Add(new AntiviralDecisionAllowOnlyOne(this));
    }
  }
}
