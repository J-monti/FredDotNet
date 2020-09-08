using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Epidemic_TimeStepMap
  {
    public int sim_day_start;
    public int sim_day_end;
    public int num_seeding_attempts;
    public int disease_id;
    public double seeding_attempt_prob;
    public int min_num_successful;
    public double lat;
    public double lon;
    public double radius;

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("Time Step Map ");
      builder.AppendLine($" sim_day_start {sim_day_start}");
      builder.AppendLine($" sim_day_end {sim_day_end}");
      builder.AppendLine($" num_seeding_attempts {num_seeding_attempts}");
      builder.AppendLine($" disease_id {disease_id}");
      builder.AppendLine($" seeding_attempt_prob {seeding_attempt_prob}");
      builder.AppendLine($" min_num_successful {min_num_successful}");
      builder.AppendLine($" lat {lat}");
      builder.AppendLine($" lon {lon}");
      builder.AppendLine($" radius {radius}");
      return builder.ToString();
    }
  }
}
