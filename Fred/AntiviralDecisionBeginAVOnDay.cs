namespace Fred
{
  public class AV_Decision_Begin_AV_On_Day : Decision
  {
    public AV_Decision_Begin_AV_On_Day() { }
    public AV_Decision_Begin_AV_On_Day(Policy policy)
      : base(policy)
    {
      this.name = "AV Decision to Begin disseminating AVs on a certain day";
      this.type = "Y/N";
    }

    public override int evaluate(Person person, int disease, int current_day)
    {
      var avm = (AV_Manager)policy.get_manager();
      var av = avm.get_current_av();
      int start_day = av.get_start_day();
      if (current_day >= start_day) { return 0; }
      return -1;
    }
  }
}
