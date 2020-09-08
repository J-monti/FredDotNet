using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Priority_Decision_No_Priority : Decision
  {
    public Vaccine_Priority_Decision_No_Priority() : base() { }

    public Vaccine_Priority_Decision_No_Priority(Policy p) : base(p)
    {
      this.name = "Vaccine Priority Decision No Priority";
      this.type = "Y/N";
      this.policy = p;
    }

    public override int evaluate(Person person, int disease, int day)
    {
      return -1;
    }
  }
}
