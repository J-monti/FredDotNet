namespace Fred
{
  public class Vaccine_Priority_Policy_ACIP : Policy
  {
    public Vaccine_Priority_Policy_ACIP(Vaccine_Manager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - ACIP Priority";
      this.decision_list.Add(new Vaccine_Priority_Decision_Specific_Age(this));
      this.decision_list.Add(new Vaccine_Priority_Decision_Pregnant(this));
      this.decision_list.Add(new Vaccine_Priority_Decision_At_Risk(this));
    }
  }
}
