namespace Fred
{
  public class Vaccine_Priority_Policy_Specific_Age : Policy
  {
    public Vaccine_Priority_Policy_Specific_Age(Vaccine_Manager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - Sepcific Age Group";
      this.Decisions.Add(new Vaccine_Priority_Decision_Specific_Age(this));
    }
  }
}
