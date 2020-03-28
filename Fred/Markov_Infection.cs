using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Markov_Infection : Infection
  {
    public Markov_Infection(Disease _disease, Person _infector, Person _host, MixingGroup _mixing_group, DateTime day)
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

    void setup()
    {

      // Infection::setup();

      FRED_VERBOSE(1, "setup entered\n");

      // initialize Markov specific-variables here:

      this.state = this.disease.get_natural_history().get_initial_state();
      printf("MARKOV INIT state %d\n", this.state);
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

    double get_infectivity(int day)
    {
      return (this.disease.get_natural_history().get_infectivity(this.state));
    }

    double get_symptoms(int day)
    {
      return (this.disease.get_natural_history().get_symptoms(this.state));
    }

    bool is_fatal(int day)
    {
      return (this.disease.get_natural_history().is_fatal(this.state));
    }

    void update(int day)
    {

      FRED_VERBOSE(1, "update Markov INFECTION on day %d for host %d\n", day, host.get_id());

      // put daily update here
    }
  }
}
