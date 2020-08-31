namespace Fred
{
  public class HIV_Natural_History : Natural_History
  {
    public HIV_Natural_History() { }

    public HIV_Natural_History(Disease disease)
    {
      base.setup(disease);
    }

    public override double get_probability_of_symptoms(int age)
    {
      return 1.0;
    }

    public override int get_latent_period(Person host)
    {
      return 14;
    }

    public override int get_duration_of_infectiousness(Person host)
    {
      // infectious forever
      return -1;
    }

    public override int get_duration_of_immunity(Person host)
    {
      // immune forever
      return -1;
    }

    public override int get_incubation_period(Person host)
    {
      return -1;
    }

    public override int get_duration_of_symptoms(Person host)
    {
      // symptoms last forever
      return -1;
    }


    public override bool is_fatal(double real_age, double symptoms, int days_symptomatic)
    {
      return false;
    }

    public override bool is_fatal(Person per, double symptoms, int days_symptomatic)
    {
      return false;
    }


    public override void update_infection(int day, Person host, Infection infection)
    {
      // put daily updates to host here.
    }
  }
}
