using System;

namespace Fred
{
  public class HIV_Epidemic : Epidemic
  {
    public HIV_Epidemic(Disease disease)
      : base (disease)
    {
    }

    public override void report_disease_specific_stats(int day)
    {
      int hiv_count = day;
      track_value(day, "HIV", hiv_count);
    }

    public override void end_of_run()
    {
      Console.WriteLine("HIV Epidemic finished");
    }
  }
}
