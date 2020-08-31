using System;

namespace Fred
{
  public class Markov_Infection : Infection
  {
    private int state;

    public Markov_Infection(Disease _disease, Person _infector, Person _host, Mixing_Group _mixing_group, int day)
      : base(_disease, _infector, _host, _mixing_group, day)
    {
      this.infectious_start_date = -1;
      this.infectious_end_date = -1;
      this.symptoms_start_date = -1;
      this.symptoms_end_date = -1;
      this.immunity_end_date = -1;
      this.infection_is_fatal_today = false;
      this.will_develop_symptoms = false;
    }

    public override void setup()
    {
      // Infection::setup();
      Utils.FRED_VERBOSE(1, "Markov_Infection::setup entered\n");
      // initialize Markov specific-variables here:

      this.state = this.disease.get_natural_history().get_initial_state();
      Console.WriteLine("MARKOV INIT state {0}", this.state);
      if (this.get_infectivity(this.exposure_date) > 0.0)
      {
        this.infectious_start_date = this.exposure_date;
        this.infectious_end_date = 99999;
      }
      if (this.get_symptoms(this.exposure_date) > 0.0)
      {
        this.symptoms_start_date = this.exposure_date;
        this.symptoms_end_date = 99999;
      }
    }

    public override void update(int day)
    {
      Utils.FRED_VERBOSE(1, "update Markov INFECTION on day {0} for host {1}", day, host.get_id());
      // put daily update here
    }

    public override double get_infectivity(int day)
    {
      return (this.disease.get_natural_history().get_infectivity(this.state));
    }

    public override double get_symptoms(int day)
    {
      return (this.disease.get_natural_history().get_symptoms(this.state));
    }

    public override bool is_fatal(int day)
    {
      return (this.disease.get_natural_history().is_fatal(this.state));
    }

    public override int get_state()
    {
      return this.state;
    }

    public override void set_state(int _state)
    {
      this.state = _state;
    }
  }
}
