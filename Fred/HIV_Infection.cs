namespace Fred
{
  public class HIV_Infection : Infection
  {
    public HIV_Infection(Disease disease, Person infector, Person host, Mixing_Group mixing_group, int day)
      : base (disease, infector, host, mixing_group, day)
    {
    }

    public override void update(int day)
    {
      Utils.FRED_VERBOSE(1, "update HIV INFECTION on day {0} for host {1}", day, host.get_id());
    }

    public override double get_infectivity(int day)
    {
      return (is_infectious(day) ? 1.0 : 0.0);
    }

    public override double get_symptoms(int day)
    {
      return (is_symptomatic(day) ? 1.0 : 0.0);
    }

    public override bool is_fatal(int day)
    {
      return false;
    }
  }
}
