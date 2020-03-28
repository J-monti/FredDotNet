using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Markov_NaturalHistory : NaturalHistory
  {
    void setup(Disease _disease)
    {
      Natural_History::setup(_disease);
      this.markov_model = new Markov_Model;
      markov_model.setup(this.disease.get_disease_name());
    }


    // the following are unused in this model:
    double get_probability_of_symptoms(int age)
    {
      return 0.0;
    }
    int get_latent_period(Person* host)
    {
      return -1;
    }
    int get_duration_of_infectiousness(Person* host)
    {
      return -1;
    }
    int get_duration_of_immunity(Person* host)
    {
      return -1;
    }
    int get_incubation_period(Person* host)
    {
      return -1;
    }
    int get_duration_of_symptoms(Person* host)
    {
      return -1;
    }
    bool is_fatal(double real_age, double symptoms, int days_symptomatic)
    {
      return false;
    }
    bool is_fatal(Person* per, double symptoms, int days_symptomatic)
    {
      return false;
    }
    void init_prior_immunity() { }
    bool gen_immunity_infection(double real_age)
    {
      return true;
    }

    void get_parameters()
    {

      FRED_VERBOSE(0, "get_parameters\n");

      // skip get_parameters() in base class:
      // Natural_History::get_parameters();

      markov_model.get_parameters();

      this.state_infectivity.reserve(get_number_of_states());
      this.state_symptoms.reserve(get_number_of_states());
      this.state_fatality.reserve(get_number_of_states());

      this.state_infectivity.clear();
      this.state_symptoms.clear();
      this.state_fatality.clear();

      char paramstr[256];
      int fatal;
      double inf, symp;
      for (int i = 0; i < get_number_of_states(); i++)
      {

        sprintf(paramstr, "%s[%d].infectivity", get_name(), i);
        Params::get_param(paramstr, &inf);
        this.state_infectivity.push_back(inf);

        sprintf(paramstr, "%s[%d].symptoms", get_name(), i);
        Params::get_param(paramstr, &symp);
        this.state_symptoms.push_back(symp);

        sprintf(paramstr, "%s[%d].fatality", get_name(), i);
        Params::get_param(paramstr, &fatal);
        this.state_fatality.push_back(fatal);

      }
      // enable case_fatality, as specified by each state
      this.enable_case_fatality = 1;

    }

    char* get_name()
    {
      return markov_model.get_name();
    }

    int get_number_of_states()
    {
      return markov_model.get_number_of_states();
    }

    std::string get_state_name(int i)
    {
      return markov_model.get_state_name(i);
    }

    void print()
    {
      markov_model.print();
      for (int i = 0; i < get_number_of_states(); i++)
      {
        printf("MARKOV MODEL %s[%d].infectivity = %f\n",
         get_name(), i, this.state_infectivity[i]);
        printf("MARKOV MODEL %s[%d].symptoms = %f\n",
         get_name(), i, this.state_symptoms[i]);
        printf("MARKOV MODEL %s[%d].fatality = %d\n",
         get_name(), i, this.state_fatality[i]);
      }
    }


  }
}
