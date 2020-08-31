using System;

namespace Fred
{
  public class Decision
  {
    protected string name;
    protected string type;
    protected Policy policy;

    public Decision()
    {
      this.name = string.Empty;
      this.type = string.Empty;
    }

    public Decision(Policy policy)
    {
      this.policy = policy;
      this.name = "Generic Decision";
      this.type = "Generic";
    }
    /**
     * @return the name of this Decision
     */
    public string get_name() { return name; }

    /**
     * @return the type of this Decision
     */
    public string get_type() { return type; }

    public virtual int evaluate(Person person, int disease, int current_day)
    {
      return 0;
    }
  }
}
