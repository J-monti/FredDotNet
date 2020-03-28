namespace Fred
{
  public class Vaccine_Priority_Policy_No_Priority : Policy
  {
    public Vaccine_Priority_Policy_No_Priority(VaccineManager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - No Priority";
      this.Decisions.Add(new Vaccine_Priority_Decision_No_Priority(this));
    }
  }
}
