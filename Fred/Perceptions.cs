using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Perceptions
  {
    static double[] memory_decay_distr = new double[2] { 0.0, 0.0 };
    static bool parameters_set = false;

    public Perceptions(Person* p)
    {
      this.self = p;

      if (Perceptions::parameters_set == false)
        get_parameters();

      // individual differences:
      this.memory_decay = Random::draw_normal(Perceptions::memory_decay_distr[0], Perceptions::memory_decay_distr[1]);
      if (this.memory_decay < 0.00001)
      {
        this.memory_decay = 0.00001;
      }

      this.perceived_susceptibility = new double[Global::Diseases.get_number_of_diseases()];
      this.perceived_severity = new double[Global::Diseases.get_number_of_diseases()];
      for (int b = 0; b < Behavior_index::NUM_BEHAVIORS; b++)
      {
        this.perceived_benefits[b] = new double[Global::Diseases.get_number_of_diseases()];
        this.perceived_barriers[b] = new double[Global::Diseases.get_number_of_diseases()];
      }

      for (int d = 0; d < Global::Diseases.get_number_of_diseases(); d++)
      {
        this.perceived_susceptibility[d] = 0.0;
        this.perceived_severity[d] = 0.0;
        for (int b = 0; b < Behavior_index::NUM_BEHAVIORS; b++)
        {
          this.perceived_benefits[b][d] = 1.0;
          this.perceived_barriers[b][d] = 0.0;
        }
      }
    }

    void get_parameters()
    {
      char param_str[FRED_STRING_SIZE];
      sprintf(param_str, "memory_decay");
      int n = Params::get_param_vector(param_str, Perceptions::memory_decay_distr);
      if (n != 2)
      {
        Utils::fred_abort("bad %s\n", param_str);
      }
      Perceptions::parameters_set = true;
    }

    void update(int day)
    {
      update_perceived_severity(day);
      update_perceived_susceptibility(day);
      update_perceived_benefits(day);
      update_perceived_barriers(day);
    }

    void update_perceived_severity(int day)
    {
      for (int d = 0; d < Global::Diseases.get_number_of_diseases(); d++)
      {
        this.perceived_severity[d] = 1.0;
      }
    }

    void update_perceived_susceptibility(int day)
    {
      for (int d = 0; d < Global::Diseases.get_number_of_diseases(); d++)
      {
        this.epidemic = Global::Diseases.get_disease(d).get_epidemic();
        this.perceived_susceptibility[d] = 100.0 * this.epidemic.get_symptomatic_prevalence();
        // printf("update_per_sus: %f\n", perceived_susceptibility[d]);
      }
    }

    void update_perceived_benefits(int day)
    {
      return;
    }

    void update_perceived_barriers(int day)
    {
      return;
    }

    public double get_perceived_severity(int disease_id)
    {
      return this.perceived_severity[disease_id];
    }

    public double get_perceived_susceptibility(int disease_id)
    {
      return this.perceived_susceptibility[disease_id];
    }

    public double get_perceived_benefits(int behavior_id, int disease_id)
    {
      return this.perceived_benefits[behavior_id][disease_id];
    }

    public double get_perceived_barriers(int behavior_id, int disease_id)
    {
      return this.perceived_barriers[behavior_id][disease_id];
    }
  }
}
