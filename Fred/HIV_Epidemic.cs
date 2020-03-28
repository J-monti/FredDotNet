using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class HIV_Epidemic : Epidemic
  {
    public HIV_Epidemic(Disease disease)
      : base (disease)
    {
    }

    protected override void report_disease_specific_stats(DateTime day)
    {
      base.report_disease_specific_stats(day);
    }
  }
}
