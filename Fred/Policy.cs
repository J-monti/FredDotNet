using System;
using System.Collections.Generic;

namespace Fred
{
  public class Policy
  {
    protected List<Decision> decision_list;
    protected string Name;
    protected Manager manager;

    public Policy ()
    {
      this.decision_list = new List<Decision>();
    }

    public Policy (Manager manager)
    {
      this.Name = "Generic";
      this.manager = manager;
      this.decision_list = new List<Decision>();
    }

    public virtual bool choose_first_positive(Person person, int disease, int current_day)
    {
      for (var i = 0; i < this.decision_list.Count; i++)
      {
        if (this.decision_list[i].evaluate(person, disease, current_day) == 1)
        {
          return true;
        }
      }

      return false;
    }

    public virtual bool choose_first_negative(Person person, int disease, int current_day)
    {
      for (var i = 0; i < this.decision_list.Count; i++)
      {
        if (this.decision_list[i].evaluate(person, disease, current_day) == -1)
        {
          return false;
        }
      }

      return true;
    }

    public virtual int choose(Person person, int disease, int current_day)
    {
      int result = -1;
      for (var i = 0; i < this.decision_list.Count; i++)
      {
        int new_result = this.decision_list[i].evaluate(person, disease, current_day);
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

    public void print()
    {
      Console.WriteLine("Policy List for Decision {0}", Name);
      Console.WriteLine("------------------------------------------------------------------");
      Console.WriteLine("Policy\t\tType");
      foreach (var decision in this.decision_list)
      {
        Console.WriteLine("{0}\t\t{1}", decision.get_name(), decision.get_type());
      }
    }

    public void reset()
    {
      Console.WriteLine("Reset is not implemented for a base policy.");
    }
  }
}