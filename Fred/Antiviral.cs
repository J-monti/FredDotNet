using System;

namespace Fred
{
  public class Antiviral
  {
    int disease;
    int course_length;               // How many days mush one take the AV
    double reduce_infectivity;       // What percentage does it reduce infectivity
    double reduce_susceptibility;    // What percentage does it reduce susceptability
    double reduce_infectious_period; // What percentage does AV reduce infectious period
    double percent_symptomatics;     // Percentage of symptomatics receiving this drug
    double reduce_asymp_period;      // What percentage does it reduce the asymptomatic period
    double reduce_symp_period;       // What percentage does it reduce the symptomatic period
    double prob_symptoms;            // What is the probability of being symptomatic
    double efficacy;                 // The effectiveness of the AV (resistance)
    int max_av_course_start_day;     // Maximum start day
    double[] av_course_start_day;     // Probabilistic AV start

    //Logistics
    int initial_stock;               // Amount of AV at day 0
    int stock;                       // Amount of AV currently available
    int additional_per_day;          // Amount of AV added to the system per day
    int total_avail;                 // The total amount available to the simulation
    int reserve;                     // Amount of AV left unused
    int start_day;                   // Day that AV is available to the system


    //Policy variables
    bool prophylaxis;                // Is this AV allowed for prophylaxis
    int percent_of_symptomatics;     // Percent of symptomatics that get this AV as treatment

    // For Statistics
    int given_out;
    int ineff_given_out;

    //Policy
    Policy policy;

    protected Antiviral() { }

    public Antiviral(int _disease, int _course_length, double _reduce_infectivity,
            double _reduce_susceptibility, double _reduce_asymp_period,
            double _reduce_symp_period, double _prob_symptoms,
            int _initial_stock, int _total_avail, int _additional_per_day,
            double _efficacy, double[] _av_course_start_day,
            int _max_av_course_start_day, int _start_day, bool _prophylaxis,
            double _percent_symptomatics)
    {
      this.disease = _disease;
      this.course_length = _course_length;
      this.reduce_infectivity = _reduce_infectivity;
      this.reduce_susceptibility = _reduce_susceptibility;
      this.reduce_asymp_period = _reduce_asymp_period;
      this.reduce_symp_period = _reduce_symp_period;
      this.prob_symptoms = _prob_symptoms;
      this.stock = _initial_stock;
      this.initial_stock = _initial_stock;
      this.reserve = _total_avail - _initial_stock;
      this.total_avail = _total_avail;
      this.additional_per_day = _additional_per_day;
      this.efficacy = _efficacy;
      this.av_course_start_day = _av_course_start_day;
      this.max_av_course_start_day = _max_av_course_start_day;
      this.start_day = _start_day;
      this.prophylaxis = _prophylaxis;
      this.percent_symptomatics = _percent_symptomatics;
    }

    //Parameter Access Members
    /**
     * @return this Antiviral's disease
     */
    public int get_disease()
    {
      return disease;
    }

    /**
     * @return this Antiviral's reduce_infectivity
     */
    public double get_reduce_infectivity()
    {
      return reduce_infectivity;
    }

    /**
     * @return this Antiviral's reduce_susceptibility
     */
    public double get_reduce_susceptibility()
    {
      return reduce_susceptibility;
    }

    /**
     * @return this Antiviral's reduce_asymp_period
     */
    public double get_reduce_asymp_period()
    {
      return reduce_asymp_period;
    }

    /**
     * @return this Antiviral's reduce_symp_period
     */
    public double get_reduce_symp_period()
    {
      return reduce_symp_period;
    }

    /**
     * @return this Antiviral's prob_symptoms
     */
    public double get_prob_symptoms()
    {
      return prob_symptoms;
    }

    /**
     * @return this Antiviral's course_length
     */
    public int get_course_length()
    {
      return course_length;
    }

    /**
     * @return this Antiviral's percent_symptomatics
     */
    public double get_percent_symptomatics()
    {
      return percent_symptomatics;
    }

    /**
     * @return this Antiviral's efficacy
     */
    public double get_efficacy()
    {
      return efficacy;
    }

    /**
     * @return this Antiviral's start_day
     */
    public int get_start_day()
    {
      return start_day;
    }

    /**
     * @return <code>true</code> if this Antiviral's is prophylaxis, <code>false</code> otherwise
     */
    public bool is_prophylaxis()
    {
      return prophylaxis;
    }

    // Roll operators
    /**
     * Randomly determine if will be symptomatic (determined by prob_symptoms)
     *
     * @return 1 if roll is successful, 0 if false
     */
    public int roll_will_have_symp()
    {
      return FredRandom.NextDouble() < prob_symptoms ? 1 : 0;
    }

    /**
     * Randomly determine if will be effective (determined by efficacy)
     *
     * @return 1 if roll is successful, 0 if false
     */
    public int roll_efficacy()
    {
      return FredRandom.NextDouble() < efficacy ? 1 : 0;
    }

    /**
     * Randomly determine the day to start (<code>draw_from_distribution(max_av_course_start_day, av_course_start_day)</code>)
     *
     * @return the number of days drawn
     */
    public int roll_course_start_day()
    {
      return FredRandom.DrawFromDistribution(max_av_course_start_day, av_course_start_day);
    }

    // Logistics Functions
    /**
     * @return the initial_stock
     */
    public int get_initial_stock()
    {
      return initial_stock;
    }

    /**
     * @return the total_avail
     */
    public int get_total_avail()
    {
      return total_avail;
    }

    /**
     * @return the reserve
     */
    public int get_current_reserve()
    {
      return reserve;
    }

    /**
     * @return the stock
     */
    public int get_current_stock()
    {
      return stock;
    }

    /**
     * @return the additional_per_day
     */
    public int get_additional_per_day()
    {
      return additional_per_day;
    }

    /**
     * @param amount how much to add to stock
     */
    public void add_stock(int amount)
    {
      if (amount < reserve)
      {
        stock += amount;
        reserve -= amount;
      }
      else
      {
        stock += reserve;
        reserve = 0;
      }
    }

    /**
     * @param remove how much to remove from stock
     */
    public void remove_stock(int remove)
    {
      stock -= remove;

      if (stock < 0) stock = 0;
    }

    // Utility Functions
    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public void update(int day)
    {
      if (day >= start_day)
      {
        add_stock(additional_per_day);
      }
    }

    /**
     * Print out information about this object
     */
    public void print()
    {
      Console.WriteLine("\tEffective for Disease \t\t{0}",disease);
      Console.WriteLine("\tCurrent Stock:\t\t\t{0} out of {1}", stock, total_avail);
      Console.WriteLine("\tWhat is left:\t\t\t{0}", reserve);
      Console.WriteLine("\tAdditional Per Day: \t\t{0}", additional_per_day);
      Console.WriteLine("\tReduces by:");
      Console.WriteLine("\t\tInfectivity:\t\t{0}%", reduce_infectivity * 100.0);
      Console.WriteLine("\t\tSusceptibility:\t\t{0}%", reduce_susceptibility * 100.0);
      Console.WriteLine("\t\tAymptomatic Period:\t{0}%", reduce_asymp_period * 100.0);
      Console.WriteLine("\t\tSymptomatic Period:\t{0}%", reduce_symp_period * 100.0);
      Console.WriteLine("\tNew Probability of symptoms:\t{0}", prob_symptoms);

      if (prophylaxis)
      {
        Console.WriteLine("\tCan be given as prophylaxis");
      }

      if (percent_symptomatics != 0)
      {
        Console.WriteLine("\tGiven to percent symptomatics:\t{0}", percent_symptomatics * 100.0);
      }

      Console.WriteLine();
      Console.Write("\tAV Course start day (max {0}):", max_av_course_start_day);
      for (int i = 0; i <= max_av_course_start_day; i++)
      {
        if ((i % 5) == 0)
        {
          Console.WriteLine();
          Console.Write("\t\t");
        }

        Console.Write("{0.######,10:D} ", av_course_start_day[i]);
      }

      Console.WriteLine();
    }

    /**
     * Put this object back to its original state
     */
    public void reset()
    {
      stock = initial_stock;
      reserve = total_avail - initial_stock;
    }

    /**
     * Print out a daily report
     *
     * @param day the simulation day
     */
    public void report(int day)
    {
      Console.WriteLine("No report just yet...");
    }

    /**
     * Print out current stock information
     */
    public void print_stocks()
    {
      Console.WriteLine($"Current: {stock} Reserve: {reserve} TAvail: {total_avail}");
    }

    /**
     * Used during debugging to verify that code is functioning properly. <br />
     * Currently, this checks the parsing of the AVs, and it returns 1 if there is a problem
     *
     * @param ndiseases the bumber of diseases
     * @return 1 if there is a problem, 0 otherwise
     */
    public int quality_control(int ndiseases)
    {
      // Currently, this checks the parsing of the AVs, and it returns 1 if there is a problem
      if (disease < 0 || disease > ndiseases)
      {
        Console.WriteLine();
        Console.WriteLine($"AV disease invalid, cannot be higher than {ndiseases}");
        return 1;
      }

      if (initial_stock < 0)
      {
        Console.WriteLine();
        Console.WriteLine("AV initial_stock invalid, cannot be lower than 0");
        return 1;
      }

      if (efficacy > 100 || efficacy < 0)
      {
        Console.WriteLine();
        Console.WriteLine("AV Percent_Resistance invalid, must be between 0 and 100");
        return 1;
      }

      if (course_length < 0)
      {
        Console.WriteLine();
        Console.WriteLine("AV Course Length invalid, must be higher than 0");
        return 1;
      }

      if (reduce_infectivity < 0 || reduce_infectivity > 1.00)
      {
        Console.WriteLine();
        Console.WriteLine("AV reduce_infectivity invalid, must be between 0 and 1.0");
        return 1;
      }

      if (reduce_susceptibility < 0 || reduce_susceptibility > 1.00)
      {
        Console.WriteLine();
        Console.WriteLine("AV reduce_susceptibility invalid, must be between 0 and 1.0");
        return 1;
      }

      if (reduce_infectious_period < 0 || reduce_infectious_period > 1.00)
      {
        Console.WriteLine();
        Console.WriteLine($"AV reduce_infectious_period invalid, must be between 0 and 1.0; is equal to: {reduce_infectious_period}");
        //return 1;
        //  TODO: Help!!!  This is never set - just contains whatever garbage present at the address.
      }

      return 0;
    }

    //Effect the Health of Person
    /**
     * Used to alter the Health of an agent
     *
     * @param h pointer to a Health object
     * @param cur_day the simulation day
     * @param av_health pointer to a specific AV_Health object
     */
    public void effect(Health health, int cur_day, AV_Health av_health)
    {
      // We need to calculate the effect of the AV on all diseases it is applicable to
      for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); disease_id++)
      {
        if (disease_id == this.disease)
        { //Is this antiviral applicable to this disease
          var dis = Global.Diseases.get_disease(disease_id);
          avEffect(health, disease, cur_day, av_health);
        }
      }
    }

    /**
     * Modify the susceptibility of an agent (through that agent's Health)
     *
     * @param health pointer to a Health object
     * @param disease which disease
     */
    public void modify_susceptiblilty(Health health, int disease)
    {
      health.modify_susceptibility(disease, 1.0 - reduce_susceptibility);
    }

    /**
     * Modify the infectivity of an agent (through that agent's Health)
     *
     * @param health pointer to a Health object
     * @param disease which disease
     */
    public void modify_infectivity(Health health, int disease)
    {
      health.modify_infectivity(disease, 1.0 - reduce_infectivity);
    }

    /**
     * Modify the infectivity of an agent (through that agent's Health)
     *
     * @param health pointer to a Health object
     * @param disease which disease
     * @param strain which strain of the disease
     */
    public void modify_infectivity_strain(Health health, int disease, int strain)
    {
      throw new NotImplementedException("modify_infectivity_strain is not implemented!");
    }

    /**
     * Modify the symptomaticity of an agent (through that agent's Health)
     *
     * @param health pointer to a Health object
     * @param disease which disease
     * @param cur_day the simulation day
     */
    public void modify_symptomaticity(Health health, int disease, int cur_day)
    {
      if (!health.is_symptomatic() && cur_day < health.get_symptoms_start_date(disease))
      {
        // Can only have these effects if the agent is not symptomatic yet
        health.modify_develops_symptoms(disease, roll_will_have_symp() != 0, cur_day);
      }

      if (!health.is_symptomatic() && cur_day < health.get_symptoms_start_date(disease))
      {
        health.modify_asymptomatic_period(disease, 1.0 - reduce_asymp_period, cur_day);
      }

      if (health.is_symptomatic() && cur_day < health.get_infectious_end_date(disease))
      {
        health.modify_symptomatic_period(disease, 1.0 - reduce_symp_period, cur_day);
      }
    }

    public void avEffect(Health health, int disease, int cur_day, AV_Health av_health)
    {
      // If this is the first day of AV Course
      if (cur_day == av_health.get_av_start_day())
      {
        modify_susceptiblilty(health, disease);
        // If you are already exposed, we need to modify your infection
        if ((health.get_exposure_date(disease) > -1) && (cur_day > health.get_exposure_date(disease)))
        {
          if (Global.Debug > 3)
          {
            Console.WriteLine("reducing an already exposed person");
          }

          modify_infectivity(health, disease);
          //modify_symptomaticity(health, disease, cur_day);
        }
      }

      // If today is the day you got exposed, prophilaxis
      if (cur_day == health.get_exposure_date(disease))
      {
        if (Global.Debug > 3)
        {
          Console.WriteLine("reducing agent on the day they are exposed");
        }

        modify_infectivity(health, disease);
        modify_symptomaticity(health, disease, cur_day);
      }

      // If this is the last day of the course
      if (cur_day == av_health.get_av_end_day())
      {
        if (Global.Debug > 3)
        {
          Console.WriteLine("resetting agent to original state");
        }

        modify_susceptiblilty(health, disease);

        if (cur_day >= health.get_exposure_date(disease))
        {
          modify_infectivity(health, disease);
        }
      }
    }

    // Policies members
    // Antivirals need a policy associated with them to determine who gets them.
    /**
     * Set the distribution policy for this Antiviral
     *
     * @param p pointer to the new Policy
     */
    public void set_policy(Policy p)
    {
      policy = p;
    }

    /**
     * @return this Antiviral's distribution policy
     */
    public Policy get_policy()
    {
      return policy;
    }

    // To Be depricated
    [Obsolete]
    public void add_given_out(int amount) { given_out += amount; }
    [Obsolete]
    public void add_ineff_given_out(int amount) { ineff_given_out += amount; }
  }
}
