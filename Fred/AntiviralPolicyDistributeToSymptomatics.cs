namespace Fred
{
  public class AV_Policy_Distribute_To_Symptomatics : Policy
  {
    public AV_Policy_Distribute_To_Symptomatics(AV_Manager manager)
      : base(manager)
    {
      this.Name = "Distribute AVs to Symptomatics";
      // Need to add the policies in the decisions
      this.decision_list.Add(new AV_Decision_Begin_AV_On_Day(this));
      this.decision_list.Add(new AV_Decision_Give_to_Sympt(this));
      this.decision_list.Add(new AV_Decision_Allow_Only_One(this));
    }

    public override int choose(Person person, int disease, int day)
    {
      int result = -1;
      for (int i = 0; i < decision_list.Count; i++)
      {
        int new_result = decision_list[i].evaluate(person, disease, day);
        if (new_result == -1)
        {
          return -1;
        }
        
        if (new_result > result)
        {
          result = new_result;
        }
      }

      return result;
    }
  }
}
