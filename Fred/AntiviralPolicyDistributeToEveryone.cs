namespace Fred
{
  public class AntiviralPolicyDistributeToEveryone : Policy
  {
    public AntiviralPolicyDistributeToEveryone(Manager manager)
      : base (manager)
    {
      this.Name = "Distribute AVs to Symptomatics";

      // Need to add the policies in the decision
      this.Decisions.Add(new AntiviralDecisionBeginAVOnDay(this));
      this.Decisions.Add(new AntiviralDecisionAllowOnlyOne(this));
    }
  }
}
