using System;
using System.Collections.Generic;

namespace Fred
{
  public class Markov_Natural_History : Natural_History
  {
    private Markov_Model markov_model;
    private List<double> state_infectivity;
    private List<double> state_symptoms;
    private List<int> state_fatality;

    public override void setup(Disease _disease)
    {
      base.setup(_disease);
      this.markov_model = new Markov_Model();
      markov_model.setup(this.disease.get_disease_name());
    }

    public override void get_parameters()
    {
      Utils.FRED_VERBOSE(0, "Markov_Natural_History::get_parameters\n");
      this.state_infectivity = new List<double>();
      this.state_symptoms = new List<double>();
      this.state_fatality = new List<int>();
      // skip get_parameters() in base class:
      // Natural_History::get_parameters();

      markov_model.get_parameters();
      int fatal = 0;
      double inf = 0, symp = 0;
      for (int i = 0; i < get_number_of_states(); i++)
      {
        FredParameters.GetParameter($"{get_name()}[{i}].infectivity", ref inf);
        this.state_infectivity.Add(inf);
        FredParameters.GetParameter($"{get_name()}[{i}].symptoms", ref symp);
        this.state_symptoms.Add(symp);
        FredParameters.GetParameter($"{get_name()}[{i}].fatality", ref fatal);
        this.state_fatality.Add(fatal);
      }

      // enable case_fatality, as specified by each state
      this.enable_case_fatality = 1;

    }

    public void print()
    {
      markov_model.print();
      for (int i = 0; i < get_number_of_states(); i++)
      {
        Console.WriteLine("MARKOV MODEL {0}[{1}].infectivity = {2}", get_name(), i, this.state_infectivity[i]);
        Console.WriteLine("MARKOV MODEL {0}[{1}].symptoms = {2}", get_name(), i, this.state_symptoms[i]);
        Console.WriteLine("MARKOV MODEL {0}[{1}].fatality = {2}", get_name(), i, this.state_fatality[i]);
      }
    }

    public override double get_infectivity(int s)
    {
      return state_infectivity[s];
    }

    public override double get_symptoms(int s)
    {
      return state_symptoms[s];
    }

    public override bool is_fatal(int s)
    {
      return (state_fatality[s] == 1);
    }

    public Markov_Model get_markov_model()
    {
      return markov_model;
    }

    public string get_name()
    {
      return markov_model.get_name();
    }

    public override int get_number_of_states()
    {
      return markov_model.get_number_of_states();
    }

    public override string get_state_name(int i)
    {
      return markov_model.get_state_name(i);
    }

    // the following are unused in this model:
    public override double get_probability_of_symptoms(int age)
    {
      return 0.0;
    }

    public override int get_latent_period(Person host)
    {
      return -1;
    }

    public override int get_duration_of_infectiousness(Person host)
    {
      return -1;
    }

    public override int get_duration_of_immunity(Person host)
    {
      return -1;
    }

    public override int get_incubation_period(Person host)
    {
      return -1;
    }

    public override int get_duration_of_symptoms(Person host)
    {
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

    public override void init_prior_immunity() { }

    public override bool gen_immunity_infection(double real_age)
    {
      return true;
    }
  }
}
