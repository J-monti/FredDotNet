using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Intention
  {
    private Person self;
    private Behavior_params bParams;
    private int index;
    private int behavior_change_model;
    private double probability;
    private int frequency;
    private int expiration;
    private bool intention;

    // Health Belief Model

    // where the agent computes perceived values
    private Perceptions perceptions;

    // Thresholds for dichotomous variables
    private double susceptibility_threshold;
    private double severity_threshold;
    private double benefits_threshold;
    private double barriers_threshold;

    /**
   * Default constructor
   */
    public Intention(Person _self, int _index)
    {
      this.index = _index;
      this.self = _self;
      this.expiration = 0;
      this.bParams = Behavior.get_behavior_params(this.index);

      this.perceptions = null;

      // pick a behavior_change_model for this individual based on the population market shares
      this.behavior_change_model = FredRandom.DrawFromDistribution(this.bParams.behavior_change_model_cdf_size,
                         this.bParams.behavior_change_model_cdf);

      // set the other intention parameters based on the behavior_change_model
      switch (this.behavior_change_model)
      {
        case (int)Behavior_change_model_enum.REFUSE:
          this.intention = false;
          this.probability = 0.0;
          this.frequency = 0;
          break;

        case (int)Behavior_change_model_enum.ACCEPT:
          this.intention = true;
          this.probability = 1.0;
          this.frequency = 0;
          break;

        case (int)Behavior_change_model_enum.FLIP:
          this.probability = FredRandom.NextDouble(this.bParams.min_prob, this.bParams.max_prob);
          this.intention = (FredRandom.NextDouble() <= this.probability);
          this.frequency = this.bParams.frequency;
          break;

        case (int)Behavior_change_model_enum.HBM:
          this.probability = FredRandom.NextDouble(this.bParams.min_prob, this.bParams.max_prob);
          this.intention = (FredRandom.NextDouble() <= this.probability);
          this.frequency = this.bParams.frequency;
          setup_hbm();
          break;

        default: // REFUSE
          this.intention = false;
          this.probability = 0.0;
          this.frequency = 0;
          break;
      }

      Utils.FRED_VERBOSE(1,
             "created INTENTION %d name %d behavior_change_model %d freq %d expir %d probability %f\n",
             this.index, this.bParams.name, this.behavior_change_model, this.frequency,
             this.expiration, this.probability);
    }

    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public void update(int day)
    {
      Utils.FRED_VERBOSE(1,
             "update INTENTION %s day %d behavior_change_model %d freq %d expir %d probability %f\n",
             this.bParams.name, day, this.behavior_change_model, this.frequency,
             this.expiration, this.probability);

      if (this.frequency > 0 && this.expiration <= day)
      {
        if (this.behavior_change_model == (int)Behavior_change_model_enum.HBM && day > 0)
        {
          this.probability = update_hbm(day);
        }
        double r = FredRandom.NextDouble();
        this.intention = (r < this.probability);
        this.expiration = day + this.frequency;
      }

      Utils.FRED_VERBOSE(1, "updated INTENTION %s = %d  expir = %d\n",
        this.bParams.name,
        (this.intention ? 1 : 0),
        this.expiration);

    }

    // access functions
    public void set_behavior_change_model(int bcm)
    {
      this.behavior_change_model = bcm;
    }

    public void set_probability(double prob)
    {
      this.probability = prob;
    }

    public void set_frequency(int freq)
    {
      this.frequency = freq;
    }

    public void set_intention(bool decision)
    {
      this.intention = decision;
    }

    public int get_behavior_change_model()
    {
      return this.behavior_change_model;
    }

    public double get_probability()
    {
      return this.probability;
    }

    public double get_frequency()
    {
      return this.frequency;
    }

    public bool agree()
    {
      return this.intention;
    }

    // HBM methods
    private void setup_hbm()
    {
      this.perceptions = new Perceptions(self);
      this.susceptibility_threshold = FredRandom.NextDouble(this.bParams.susceptibility_threshold_distr[0],
                       this.bParams.susceptibility_threshold_distr[1]);
      this.severity_threshold = FredRandom.NextDouble(this.bParams.severity_threshold_distr[0],
                 this.bParams.severity_threshold_distr[1]);
      this.benefits_threshold = FredRandom.NextDouble(this.bParams.benefits_threshold_distr[0],
                 this.bParams.benefits_threshold_distr[1]);
      this.barriers_threshold = FredRandom.NextDouble(this.bParams.barriers_threshold_distr[0],
                 this.bParams.barriers_threshold_distr[1]);
      Utils.FRED_VERBOSE(1, "setup_hbm: thresholds: sus= %f sev= %f  ben= %f bar = %f\n",
             this.susceptibility_threshold, this.severity_threshold,
             this.benefits_threshold, this.barriers_threshold);
    }

    private double update_hbm(int day)
    {
      int disease_id = 0;

      Utils.FRED_VERBOSE(1, "update_hbm entered: thresholds: sus= %f sev= %f  ben= %f bar = %f\n",
             this.susceptibility_threshold, this.severity_threshold,
             this.benefits_threshold, this.barriers_threshold);

      // update perceptions.
      this.perceptions.update(day);

      // each update is specific to current behavior
      bool perceived_severity = (this.perceptions.get_perceived_severity(disease_id) > this.severity_threshold);

      bool perceived_susceptibility = (this.perceptions.get_perceived_susceptibility(disease_id)
                 > this.susceptibility_threshold);

      bool perceived_benefits = true;
      bool perceived_barriers = false;

      // decide whether to act or not
      double odds;
      odds = this.bParams.base_odds_ratio;

      if (perceived_susceptibility)
        odds *= this.bParams.susceptibility_odds_ratio;

      if (perceived_severity)
        odds *= this.bParams.severity_odds_ratio;

      if (perceived_benefits)
        odds *= this.bParams.benefits_odds_ratio;

      if (perceived_barriers)
        odds *= this.bParams.barriers_odds_ratio;


      return odds > 1.0 ? 1.0 : 0.0;
    }
  }
}
