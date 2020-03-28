namespace Fred
{
  public class EpidemicTimeStepMap
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
      return string.Format("Time Step Map - SimStartDay: {0} | SimEndDay: {1} | num_seeding_attempts: {2} | disease_id: {3} | seeding_attempt_prob: {4} | min_num_successful: {5} | lat: {6} | lon: {7} | radius: {8} ",
        sim_day_start,
        sim_day_end,
        num_seeding_attempts,
        disease_id,
        seeding_attempt_prob,
        min_num_successful,
        lat,
        lon,
        radius);
    }
  }
}
