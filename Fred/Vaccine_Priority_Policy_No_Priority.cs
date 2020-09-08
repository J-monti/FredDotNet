namespace Fred
{
  public class Vaccine_Priority_Policy_No_Priority : Policy
  {
    public Vaccine_Priority_Policy_No_Priority(Vaccine_Manager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - No Priority";
      this.decision_list.Add(new Vaccine_Priority_Decision_No_Priority(this));
    }
  }
}
