using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class HealthCondition
  {
    public HealthCondition()
    {
      this.State = -1;
      this.NextState = -1;
      this.LastTransitionDay = null;
      this.NextTransitionDay = null;
    }

    public int State { get; set; }

    public DateTime? LastTransitionDay { get; set; }

    public int NextState { get; set; }

    public DateTime? NextTransitionDay { get; set; }
  }
}
