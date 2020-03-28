using System;
using System.Collections.Generic;

namespace Fred
{
  public class Policy
  {
    public Policy ()
    {
      this.Decisions = new List<Decision>();
    }

    public Policy (Manager manager)
    {
      this.Name = "Generic";
      this.Manager = manager;
      this.Decisions = new List<Decision>();
    }

    public Manager Manager { get; }

    public List<Decision> Decisions { get; }

    public string Name { get; protected set; }

    public virtual bool ChooseFirstPositive(Person person, int disease, DateTime currentDay)
    {
      for (var i = 0; i < this.Decisions.Count; i++)
      {
        if (this.Decisions[i].Evaluate(person, disease, currentDay) == 1)
        {
          return true;
        }
      }

      return false;
    }

    public virtual bool ChooseFirstNegative(Person person, int disease, DateTime currentDay)
    {
      for (var i = 0; i < this.Decisions.Count; i++)
      {
        if (this.Decisions[i].Evaluate(person, disease, currentDay) == -1)
        {
          return false;
        }
      }

      return true;
    }

    public virtual int Choose(Person person, int disease, DateTime current_day)
    {
      int result = -1;
      for (var i = 0; i < this.Decisions.Count; i++)
      {
        int new_result = this.Decisions[i].Evaluate(person, disease, current_day);
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

    public void Print()
    {
      Console.WriteLine("Policy List for Decision {0}", Name);
      Console.WriteLine("------------------------------------------------------------------");
      Console.WriteLine("Policy\t\tType");
      foreach (var decision in this.Decisions)
      {
        Console.WriteLine("{0}\t\t{1}", decision.Name, decision.Type);
      }
    }
  }
}