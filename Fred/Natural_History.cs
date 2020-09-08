using System;

namespace Fred
{
  public class Natural_History
  {
    public const int NEVER = -1;
    public const int LOGNORMAL = 0;
    public const int OFFSET_FROM_START_OF_SYMPTOMS = 1;
    public const int OFFSET_FROM_SYMPTOMS = 2;
    public const int CDF = 3;

    protected Disease disease;
    // prob of getting symptoms
    protected double probability_of_symptoms;
    // relative infectivity if asymptomatic
    protected double asymptomatic_infectivity;

    // distributions for symptoms and infectiousness
    protected string symptoms_distributions;
    protected string infectious_distributions;
    protected int symptoms_distribution_type;
    protected int infectious_distribution_type;

    // CDFs
    protected int max_days_incubating;
    protected int max_days_symptomatic;
    protected double[] days_incubating;
    protected double[] days_symptomatic;
    protected int max_days_latent;
    protected int max_days_infectious;
    protected double[] days_latent;
    protected double[] days_infectious;
    protected Age_Map age_specific_prob_symptoms;
    protected double immunity_loss_rate;

    // parameters for incubation and infectious periods and offsets
    protected double incubation_period_median;
    protected double incubation_period_dispersion;
    protected double incubation_period_upper_bound;
    protected double symptoms_duration_median;
    protected double symptoms_duration_dispersion;
    protected double symptoms_duration_upper_bound;
    protected double latent_period_median;
    protected double latent_period_dispersion;
    protected double latent_period_upper_bound;
    protected double infectious_duration_median;
    protected double infectious_duration_dispersion;
    protected double infectious_duration_upper_bound;

    protected double infectious_start_offset;
    protected double infectious_end_offset;

    // thresholds used in Infection class to determine if an agent is
    // infectious/symptomatic at a given time point
    protected double infectivity_threshold;
    protected double symptomaticity_threshold;

    // fraction of periods with full symptoms or infectivity
    protected double full_symptoms_start;
    protected double full_symptoms_end;
    protected double full_infectivity_start;
    protected double full_infectivity_end;

    // case fatality parameters
    protected int enable_case_fatality;
    protected double min_symptoms_for_case_fatality;
    protected Age_Map age_specific_prob_case_fatality;
    protected double[] case_fatality_prob_by_day;
    protected int max_days_case_fatality_prob;

    protected Age_Map age_specific_prob_infection_immunity;
    protected Evolution evol;

    /**
   * This static factory method is used to get an instance of a specific Natural_History Model.
   * Depending on the model parameter, it will create a specific Natural_History Model and return
   * a pointer to it.
   *
   * @param a string containing the requested Natural_History model type
   * @return a pointer to a specific Natural_History model
   */

    public static Natural_History get_new_natural_history(string natural_history_model)
    {

      if (natural_history_model == "basic")
      {
        return new Natural_History();
      }

      if (natural_history_model == "markov")
      {
        return new Markov_Natural_History();
      }

      if (natural_history_model == "hiv")
      {
        return new HIV_Natural_History();
      }

      Utils.FRED_STATUS(0, "Unknown natural_history_model ({0})-- using basic Natural_History.\n", natural_history_model);
      return new Natural_History();
    }

    /*
     * The Natural History base class implements an SEIR(S) model.
     * For other models, define a derived class and redefine the following
     * virtual methods as needed.
     */

    public virtual void setup(Disease _disease)
    {
      Utils.FRED_VERBOSE(0, "Natural_History.setup\n");
      this.disease = _disease;

      // set defaults 
      this.probability_of_symptoms = 0;
      this.asymptomatic_infectivity = 0;
      symptoms_distributions = "none";
      infectious_distributions = "none";
      this.symptoms_distribution_type = 0;
      this.infectious_distribution_type = 0;
      this.max_days_incubating = 0;
      this.max_days_symptomatic = 0;
      this.days_incubating = null;
      this.days_symptomatic = null;
      this.max_days_latent = 0;
      this.max_days_infectious = 0;
      this.days_latent = null;
      this.days_infectious = null;
      this.age_specific_prob_symptoms = null;
      this.immunity_loss_rate = 0;
      this.incubation_period_median = 0;
      this.incubation_period_dispersion = 0;
      this.incubation_period_upper_bound = 0;
      this.symptoms_duration_median = 0;
      this.symptoms_duration_dispersion = 0;
      this.symptoms_duration_upper_bound = 0;
      this.latent_period_median = 0;
      this.latent_period_dispersion = 0;
      this.latent_period_upper_bound = 0;
      this.infectious_duration_median = 0;
      this.infectious_duration_dispersion = 0;
      this.infectious_duration_upper_bound = 0;
      this.infectious_start_offset = 0;
      this.infectious_end_offset = 0;
      this.infectivity_threshold = 0;
      this.symptomaticity_threshold = 0;
      this.full_symptoms_start = 0;
      this.full_symptoms_end = 1;
      this.full_infectivity_start = 0;
      this.full_infectivity_end = 1;
      this.enable_case_fatality = 0;
      this.min_symptoms_for_case_fatality = -1;
      this.age_specific_prob_case_fatality = null;
      this.case_fatality_prob_by_day = null;
      this.max_days_case_fatality_prob = -1;
      this.age_specific_prob_infection_immunity = null;
      this.evol = null;
      Utils.FRED_VERBOSE(0, "Natural_History.setup finished\n");
    }

    public virtual void get_parameters()
    {

      Utils.FRED_VERBOSE(0, "Natural_History.get_parameters\n");
      // read in the disease-specific parameters
      string disease_name = disease.get_disease_name();
      int n = 0;

      // get required natural history parameters

      FredParameters.GetParameter($"{disease_name}_symptoms_distributions", ref this.symptoms_distributions);

      if (this.symptoms_distributions == "lognormal")
      {
        FredParameters.GetParameter($"{disease_name}_incubation_period_median", ref this.incubation_period_median);
        FredParameters.GetParameter($"{disease_name}_incubation_period_dispersion", ref this.incubation_period_dispersion);
        FredParameters.GetParameter($"{disease_name}_symptoms_duration_median", ref this.symptoms_duration_median);
        FredParameters.GetParameter($"{disease_name}_symptoms_duration_dispersion", ref this.symptoms_duration_dispersion);

        FredParameters.GetParameter($"{disease_name}_incubation_period_upper_bound", ref this.incubation_period_upper_bound);
        FredParameters.GetParameter($"{disease_name}_symptoms_duration_upper_bound", ref this.symptoms_duration_upper_bound);
        this.symptoms_distribution_type = LOGNORMAL;
      }
      else if (this.symptoms_distributions == "cdf")
      {
        FredParameters.GetParameter($"{disease_name}_days_incubating", ref n);
        this.days_incubating = new double[n];
        this.days_incubating = FredParameters.GetParameterList<double>($"{disease_name}_days_incubating").ToArray();
        this.max_days_incubating = days_incubating.Length - 1;

        FredParameters.GetParameter($"{disease_name}_days_symptomatic", ref n);
        this.days_symptomatic = new double[n];
        this.days_symptomatic = FredParameters.GetParameterList<double>($"{disease_name}_days_symptomatic").ToArray();
        this.max_days_symptomatic = days_symptomatic.Length - 1;
        this.symptoms_distribution_type = CDF;
      }
      else
      {
        Utils.fred_abort("Natural_History: unrecognized symptoms_distributions type: %s\n", this.symptoms_distributions);
      }

      if (this.disease.get_transmissibility() > 0.0)
      {

        FredParameters.GetParameter($"{disease_name}_infectious_distributions", ref this.infectious_distributions);

        if (this.infectious_distributions == "offset_from_symptoms")
        {
          FredParameters.GetParameter($"{disease_name}_infectious_start_offset", ref this.infectious_start_offset);
          FredParameters.GetParameter($"{disease_name}_infectious_end_offset", ref this.infectious_end_offset);
          this.infectious_distribution_type = OFFSET_FROM_SYMPTOMS;
        }
        else if (this.infectious_distributions == "offset_from_start_of_symptoms")
        {
          FredParameters.GetParameter($"{disease_name}_infectious_start_offset", ref this.infectious_start_offset);
          FredParameters.GetParameter($"{disease_name}_infectious_end_offset", ref this.infectious_end_offset);
          this.infectious_distribution_type = OFFSET_FROM_START_OF_SYMPTOMS;
        }
        else if (this.infectious_distributions == "lognormal")
        {
          FredParameters.GetParameter($"{disease_name}_latent_period_median", ref this.latent_period_median);
          FredParameters.GetParameter($"{disease_name}_latent_period_dispersion", ref this.latent_period_dispersion);
          FredParameters.GetParameter($"{disease_name}_infectious_duration_median", ref this.infectious_duration_median);
          FredParameters.GetParameter($"{disease_name}_infectious_duration_dispersion", ref this.infectious_duration_dispersion);
          FredParameters.GetParameter($"{disease_name}_latent_period_upper_bound", ref this.latent_period_upper_bound);
          FredParameters.GetParameter($"{disease_name}_infectious_duration_upper_bound", ref this.infectious_duration_upper_bound);

          this.infectious_distribution_type = LOGNORMAL;
        }
        else if (this.infectious_distributions == "cdf")
        {
          FredParameters.GetParameter($"{disease_name}_days_latent", ref n);
          this.days_latent = new double[n];
          this.days_latent = FredParameters.GetParameterList<double>($"{disease_name}_days_latent").ToArray();
          this.max_days_latent = days_latent.Length - 1;

          FredParameters.GetParameter($"{disease_name}_days_infectious", ref n);
          this.days_infectious = new double[n];
          this.days_infectious = FredParameters.GetParameterList<double>($"{disease_name}_days_infectious").ToArray();
          this.max_days_infectious = days_infectious.Length - 1;
          this.infectious_distribution_type = CDF;
        }
        else
        {
          Utils.fred_abort("Natural_History: unrecognized infectious_distributions type: %s\n", this.infectious_distributions);
        }
        FredParameters.GetParameter($"{disease_name}_asymp_infectivity", ref this.asymptomatic_infectivity);
      }

      // set required parameters
      FredParameters.GetParameter($"{disease_name}_probability_of_symptoms", ref this.probability_of_symptoms);

      // get fractions corresponding to full symptoms or infectivity
      FredParameters.GetParameter($"{disease_name}_full_symptoms_start", ref this.full_symptoms_start);
      FredParameters.GetParameter($"{disease_name}_full_symptoms_end", ref this.full_symptoms_end);
      FredParameters.GetParameter($"{disease_name}_full_infectivity_start", ref this.full_infectivity_start);
      FredParameters.GetParameter($"{disease_name}_full_infectivity_end", ref this.full_infectivity_end);
      FredParameters.GetParameter($"{disease_name}_immunity_loss_rate", ref this.immunity_loss_rate);
      FredParameters.GetParameter($"{disease_name}_infectivity_threshold", ref this.infectivity_threshold);
      FredParameters.GetParameter($"{disease_name}_symptomaticity_threshold", ref this.symptomaticity_threshold);

      // age specific probablility of symptoms
      this.age_specific_prob_symptoms = new Age_Map("Symptoms");
      this.age_specific_prob_symptoms.read_from_input($"{disease_name}_prob_symptoms");

      // probability of developing an immune response by past infections
      this.age_specific_prob_infection_immunity = new Age_Map("Infection Immunity");
      this.age_specific_prob_infection_immunity.read_from_input($"{disease_name}_infection_immunity");

      //case fatality parameters
      FredParameters.GetParameter($"{disease_name}_enable_case_fatality", ref this.enable_case_fatality);
      if (this.enable_case_fatality != 0)
      {
        FredParameters.GetParameter($"{disease_name}_min_symptoms_for_case_fatality", ref this.min_symptoms_for_case_fatality);
        this.age_specific_prob_case_fatality = new Age_Map("Case Fatality");
        this.age_specific_prob_case_fatality.read_from_input($"{disease_name}_case_fatality");
        FredParameters.GetParameter($"{disease_name}_case_fatality_prob_by_day", ref this.max_days_case_fatality_prob);
        this.case_fatality_prob_by_day = FredParameters.GetParameterList<double>($"{disease_name}_case_fatality_prob_by_day").ToArray();
      }

      if (Global.Enable_Viral_Evolution)
      {
        int evolType = 0;
        FredParameters.GetParameter($"{disease_name}_evolution", ref evolType);
        this.evol = EvolutionFactory.newEvolution(evolType);
        this.evol.setup(this.disease);
      }

      Utils.FRED_VERBOSE(0, "Natural_History.get_parameters finished\n");
    }

    public virtual void prepare() { }

    // called from Infection

    public virtual void update_infection(int day, Person host, Infection infection) { }

    public virtual bool do_symptoms_coincide_with_infectiousness()
    {
      return true;
    }

    public virtual double get_probability_of_symptoms(int age)
    {
      if (this.age_specific_prob_symptoms.is_empty())
      {
        return this.probability_of_symptoms;
      }
      else
      {
        return this.age_specific_prob_symptoms.find_value(age);
      }
    }

    public virtual int get_latent_period(Person host)
    {
      return FredRandom.DrawFromDistribution(max_days_latent, days_latent);
    }

    public virtual int get_duration_of_infectiousness(Person host)
    {
      return FredRandom.DrawFromDistribution(max_days_infectious, days_infectious);
    }

    public virtual int get_duration_of_immunity(Person host)
    {
      int days;
      if (this.immunity_loss_rate > 0.0)
      {
        // draw from exponential distribution
        days = Convert.ToInt32(Math.Floor(0.5 + FredRandom.Exponential(this.immunity_loss_rate)));
        // printf("DAYS RECOVERED = %d\n", days);
      }
      else
      {
        days = -1;
      }
      return days;
    }

    public virtual double get_real_incubation_period(Person host)
    {
      double location = Math.Log(this.incubation_period_median);
      double scale = 0.5 * Math.Log(this.incubation_period_dispersion);
      double incubation_period = FredRandom.LogNormal(location, scale);
      if (this.incubation_period_upper_bound > 0 && incubation_period > this.incubation_period_upper_bound)
      {
        incubation_period = FredRandom.NextDouble(0.0, this.incubation_period_upper_bound);
      }
      return incubation_period;
    }

    public virtual double get_symptoms_duration(Person host)
    {
      double location = Math.Log(this.symptoms_duration_median);
      double scale = Math.Log(this.symptoms_duration_dispersion);
      double symptoms_duration = FredRandom.LogNormal(location, scale);
      if (this.symptoms_duration_upper_bound > 0 && symptoms_duration > this.symptoms_duration_upper_bound)
      {
        symptoms_duration = FredRandom.NextDouble(0.0, this.symptoms_duration_upper_bound);
      }
      return symptoms_duration;
    }

    public virtual double get_real_latent_period(Person host)
    {
      double location = Math.Log(this.latent_period_median);
      double scale = 0.5 * Math.Log(this.latent_period_dispersion);
      double latent_period = FredRandom.LogNormal(location, scale);
      if (this.latent_period_upper_bound > 0 && latent_period > this.latent_period_upper_bound)
      {
        latent_period = FredRandom.NextDouble(0.0, this.latent_period_upper_bound);
      }
      return latent_period;
    }

    public virtual double get_infectious_duration(Person host)
    {
      double location = Math.Log(this.infectious_duration_median);
      double scale = Math.Log(this.infectious_duration_dispersion);
      double infectious_duration = FredRandom.LogNormal(location, scale);
      if (this.infectious_duration_upper_bound > 0 && infectious_duration > this.infectious_duration_upper_bound)
      {
        infectious_duration = FredRandom.NextDouble(0.0, this.infectious_duration_upper_bound);
      }
      return infectious_duration;
    }

    public virtual double get_infectious_start_offset(Person host)
    {
      return this.infectious_start_offset;
    }

    public virtual double get_infectious_end_offset(Person host)
    {
      return this.infectious_end_offset;
    }

    public virtual int get_incubation_period(Person host)
    {
      return FredRandom.DrawFromDistribution(max_days_incubating, days_incubating);
    }

    public virtual int get_duration_of_symptoms(Person host)
    {
      return FredRandom.DrawFromDistribution(max_days_symptomatic, days_symptomatic);
    }

    public virtual double get_asymptomatic_infectivity()
    {
      return this.asymptomatic_infectivity;
    }

    public virtual Evolution get_evolution()
    {
      return this.evol;
    }

    public virtual double get_infectivity_threshold()
    {
      return this.infectivity_threshold;
    }

    public virtual double get_symptomaticity_threshold()
    {
      return this.symptomaticity_threshold;
    }

    public virtual void init_prior_immunity()
    {
      this.evol.init_prior_immunity(this.disease);
    }

    // case fatality

    public virtual bool is_case_fatality_enabled()
    {
      return this.enable_case_fatality != 0;
    }

    public virtual bool is_fatal(double real_age, double symptoms, int days_symptomatic)
    {
      if (this.enable_case_fatality != 0 && symptoms >= this.min_symptoms_for_case_fatality)
      {
        double age_prob = this.age_specific_prob_case_fatality.find_value(real_age);
        double day_prob = this.case_fatality_prob_by_day[days_symptomatic];
        return (FredRandom.NextDouble() < age_prob * day_prob);
      }
      return false;
    }

    public virtual bool is_fatal(Person per, double symptoms, int days_symptomatic)
    {
      if (Global.Enable_Chronic_Condition && this.enable_case_fatality != 0)
      {
        if (per.has_chronic_condition())
        {
          double age_prob = this.age_specific_prob_case_fatality.find_value(per.get_real_age());
          double day_prob = this.case_fatality_prob_by_day[days_symptomatic];
          if (per.is_asthmatic())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.ASTHMA);
          }
          if (per.has_COPD())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.COPD);
          }
          if (per.has_chronic_renal_disease())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.CHRONIC_RENAL_DISEASE);
          }
          if (per.is_diabetic())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.DIABETES);
          }
          if (per.has_heart_disease())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.HEART_DISEASE);
          }
          if (per.has_hypertension())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.HYPERTENSION);
          }
          if (per.has_hypercholestrolemia())
          {
            age_prob *= Health.get_chronic_condition_case_fatality_prob_mult(per.get_age(), Chronic_condition_index.HYPERCHOLESTROLEMIA);
          }
          if (per.get_demographics().is_pregnant())
          {
            age_prob *= Health.get_pregnancy_case_fatality_prob_mult(per.get_age());
          }
          return (FredRandom.NextDouble() < age_prob * day_prob);
        }
        else
        {
          return is_fatal(per.get_age(), symptoms, days_symptomatic);
        }
      }
      else
      {
        return is_fatal(per.get_age(), symptoms, days_symptomatic);
      }
    }

    // immunity after infection

    public virtual bool gen_immunity_infection(double real_age)
    {
      if (this.age_specific_prob_infection_immunity == null)
      {
        // no age_specific_prob_infection_immunity was specified in the params files,
        // so we assume that INFECTION PRODUCES LIFE-LONG IMMUNITY.
        // if this is not true, an age map must be specified in the params file.
        return true;
      }
      else
      {
        double prob = this.age_specific_prob_infection_immunity.find_value(real_age);
        return (FredRandom.NextDouble() <= prob);
      }
    }

    // support for viral evolution

    public virtual void initialize_evolution_reporting_grid(Regional_Layer grid)
    {
      this.evol.initialize_reporting_grid(grid);
    }

    public virtual void end_of_run() { }

    /*
    public virtual int get_use_incubation_offset() {
      return 0;
    }
    */

    public virtual int get_number_of_states()
    {
      return 1;
    }

    public virtual double get_transition_probability(int i, int j)
    {
      return 0.0;
    }

    public virtual string get_state_name(int i)
    {
      return string.Empty;
    }

    public virtual int get_initial_state()
    {
      return 0;
    }

    public virtual double get_infectivity(int state)
    {
      return 0.0;
    }

    public virtual double get_symptoms(int state)
    {
      return 0.0;
    }

    public virtual bool is_fatal(int state)
    {
      return false;
    }

    public int get_symptoms_distribution_type()
    {
      return this.symptoms_distribution_type;
    }

    public int get_infectious_distribution_type()
    {
      return this.infectious_distribution_type;
    }

    public double get_full_symptoms_start()
    {
      return this.full_symptoms_start;
    }

    public double get_full_symptoms_end()
    {
      return this.full_symptoms_end;
    }

    public double get_full_infectivity_start()
    {
      return this.full_infectivity_start;
    }

    public double get_full_infectivity_end()
    {
      return this.full_infectivity_end;
    }
  }
}
