using System;

namespace Fred
{
  public class Decision
  {

    public Decision() { }

    public Decision(Policy policy)
    {
      this.Policy = policy;
      this.Name = "Generic Decision";
      this.Type = "Generic";
    }

    /// <summary>
    /// Gets the name of the decision.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// Gets the type of the decision.
    /// </summary>
    public string Type { get; protected set; }

    public Policy Policy { get; protected set; }

    public virtual int Evaluate (Person person, int disease, DateTime currentDay)
    {
      return 0;
    }
  }
}
