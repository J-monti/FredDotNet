namespace Fred
{
  public class Vaccine_Priority_Policy_Specific_Age : Policy
  {
    public Vaccine_Priority_Policy_Specific_Age(Vaccine_Manager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - Specific Age Group";
      this.decision_list.Add(new Vaccine_Priority_Decision_Specific_Age(this));
    }
  }
}
