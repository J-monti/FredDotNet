using System;

namespace Fred
{
  public class Perceptions
  {
    public static double[] memory_decay_distr = new double[2] { 0.0, 0.0 };
    public static bool parameters_set;

    private Person self;
    private Epidemic epidemic;
    private double[] perceived_severity;
    private double[] perceived_susceptibility;
    private double[,] perceived_benefits;
    private double[,] perceived_barriers;
    private double memory_decay;

    protected Perceptions() { }
    public Perceptions(Person p)
    {
      this.self = p;
      if (Perceptions.parameters_set == false)
        get_parameters();

      // individual differences:
      this.memory_decay = FredRandom.Normal(memory_decay_distr[0], memory_decay_distr[1]);
      if (this.memory_decay < 0.00001)
      {
        this.memory_decay = 0.00001;
      }

      this.perceived_susceptibility = new double[Global.Diseases.get_number_of_diseases()];
      this.perceived_severity = new double[Global.Diseases.get_number_of_diseases()];
      this.perceived_benefits = new double[(int)Behavior_index.NUM_BEHAVIORS, Global.Diseases.get_number_of_diseases()];
      this.perceived_barriers = new double[(int)Behavior_index.NUM_BEHAVIORS, Global.Diseases.get_number_of_diseases()];
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
      {
        this.perceived_susceptibility[d] = 0.0;
        this.perceived_severity[d] = 0.0;
        for (int b = 0; b < (int)Behavior_index.NUM_BEHAVIORS; b++)
        {
          this.perceived_benefits[b, d] = 1.0;
          this.perceived_barriers[b, d] = 0.0;
        }
      }
    }

    public void get_parameters()
    {
      memory_decay_distr = FredParameters.GetParameterList<double>("memory_decay").ToArray();
      int n = memory_decay_distr.Length;
      if (memory_decay_distr.Length != 2)
      {
        Utils.fred_abort("bad memory_decay");
      }

      parameters_set = true;
    }

    public void update(int day)
    {
      update_perceived_severity(day);
      update_perceived_susceptibility(day);
      update_perceived_benefits(day);
      update_perceived_barriers(day);
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
      return this.perceived_benefits[behavior_id, disease_id];
    }

    public double get_perceived_barriers(int behavior_id, int disease_id)
    {
      return this.perceived_barriers[behavior_id, disease_id];
    }

    // update methods
    private void update_perceived_severity(int day)
    {
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
      {
        this.perceived_severity[d] = 1.0;
      }
    }

    private void update_perceived_susceptibility(int day)
    {
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
      {
        this.epidemic = Global.Diseases.get_disease(d).get_epidemic();
        this.perceived_susceptibility[d] = 100.0 * this.epidemic.get_symptomatic_prevalence();
        // printf("update_per_sus: %f\n", perceived_susceptibility[d]);
      }
    }

    private void update_perceived_benefits(int day) { }
    private void update_perceived_barriers(int day) { }
  }
}
