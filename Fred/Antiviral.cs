using System;

namespace Fred
{
  public class Antiviral
  {
    double reduce_infectivity;       // What percentage does it reduce infectivity
    double reduce_susceptibility;    // What percentage does it reduce susceptability
    double reduce_infectious_period; // What percentage does AV reduce infectious period
    double percent_symptomatics;     // Percentage of symptomatics receiving this drug
    double reduce_asymp_period;      // What percentage does it reduce the asymptomatic period
    double reduce_symp_period;       // What percentage does it reduce the symptomatic period
    double prob_symptoms;            // What is the probability of being symptomatic
    int max_av_course_start_day;     // Maximum start day
    double[] av_course_start_day;     // Probabilistic AV start

    //Logistics
    int m_InitialStock;               // Amount of AV at day 0
    int m_AdditionalPerDay;          // Amount of AV added to the system per day


    //Policy variables
    bool prophylaxis;                // Is this AV allowed for prophylaxis
    int percent_of_symptomatics;     // Percent of symptomatics that get this AV as treatment

    // For Statistics
    int given_out;
    int ineff_given_out;

    //Policy
    Policy policy;

    public Antiviral(int disease, int courseLength, double _reduce_infectivity,
                     double _reduce_susceptibility, double _reduce_asymp_period,
                     double _reduce_symp_period, double _prob_symptoms,
                     int initialStock, int totalAvail, int additionalPerDay,
                     double efficacy, double[] _av_course_start_day,
                     int _max_av_course_start_day, DateTime startDay, bool isProphylaxis,
                     double _percent_symptomatics)
    {
      this.Disease = disease;
      this.CourseLength = TimeSpan.FromDays(Math.Max(1, courseLength));
      reduce_infectivity = _reduce_infectivity;
      reduce_susceptibility = _reduce_susceptibility;
      reduce_asymp_period = _reduce_asymp_period;
      reduce_symp_period = _reduce_symp_period;
      prob_symptoms = _prob_symptoms;
      CurrentStock = initialStock;
      this.m_InitialStock = initialStock;
      this.Reserve = totalAvail - initialStock;
      this.TotalAvailable = totalAvail;
      this.m_AdditionalPerDay = additionalPerDay;
      this.Efficacy = efficacy;
      av_course_start_day = _av_course_start_day;
      max_av_course_start_day = _max_av_course_start_day;
      this.StartDay = startDay;
      this.IsProphylaxis = isProphylaxis;
      percent_symptomatics = _percent_symptomatics;
    }

    public int CurrentStock { get; private set; }

    public int TotalAvailable { get; private set; }

    public int Reserve { get; private set; }

    public TimeSpan CourseLength { get; }

    public bool IsProphylaxis { get; }

    public DateTime StartDay { get; }

    public int Disease { get; }

    public double Efficacy { get; }

    /**
     * @param amount how much to add to stock
     */
    public void AddStock(int amount)
    {
      if (amount < this.Reserve)
      {
        this.CurrentStock += amount;
        this.Reserve -= amount;
      }
      else
      {
        this.CurrentStock += this.Reserve;
        this.Reserve = 0;
      }
    }

    /**
     * @param remove how much to remove from stock
     */
    public void RemoveStock(int remove)
    {
      this.CurrentStock -= remove;
      if (this.CurrentStock < 0)
      {
        this.CurrentStock = 0;
      }
    }

    public bool RollWillHaveSymp()
    {
      return FredRandom.NextDouble() < prob_symptoms;
    }

    public bool RollEfficacy()
    {
      return FredRandom.NextDouble() < this.Efficacy;
    }

    public int RollCourseStartDay()
    {
      return FredRandom.DrawFromDistribution(max_av_course_start_day, av_course_start_day);
    }

    public void Update(DateTime day)
    {
        if (day >= this.StartDay)
        {
          this.AddStock(this.m_AdditionalPerDay);
        }
    }

    public void Print() {
      Console.WriteLine("\tEffective for Disease \t\t{0}",this.Disease);
      Console.WriteLine("\tCurrent Stock:\t\t\t{0} out of {1}", this.CurrentStock, this.TotalAvailable);
      Console.WriteLine("\tWhat is left:\t\t\t{0}", this.Reserve);
      Console.WriteLine("\tAdditional Per Day: \t\t{0}", this.m_AdditionalPerDay);
      Console.WriteLine("\tPercent Resistance\t\t{0}", this.Efficacy);
      Console.WriteLine("\tReduces by:");
      Console.WriteLine("\t\tInfectivity:\t\t{0}%", reduce_infectivity*100.0);
      Console.WriteLine("\t\tSusceptibility:\t\t{0}%", reduce_susceptibility*100.0);
      Console.WriteLine("\t\tAymptomatic Period:\t{0}%", reduce_asymp_period*100.0);
      Console.WriteLine("\t\tSymptomatic Period:\t{0}%", reduce_symp_period*100.0);
      Console.WriteLine("\tNew Probability of symptoms:\t{0}", prob_symptoms);

      if (this.IsProphylaxis)
      {
        Console.WriteLine("\tCan be given as prophylaxis");
      }

      if (percent_symptomatics != 0)
      {
        Console.WriteLine("\tGiven to percent symptomatics:\t{0}%", percent_symptomatics * 100.0);
      }

      Console.WriteLine("\n\tAV Course start day (max {0}):", max_av_course_start_day);

      for (int i = 0; i <= max_av_course_start_day; i++)
      {
        if ((i % 5) == 0)
        {
          Console.WriteLine("\n\t\t");
        }

        //cout << setw(10) << setprecision(6) <<av_course_start_day[i] << " ";
        Console.Write("{0}{1:0.000000}", string.Empty.PadLeft(10), av_course_start_day[i]);
      }
    }

    public void Reset()
    {
      this.CurrentStock = this.m_InitialStock;
      this.Reserve = this.TotalAvailable - this.m_InitialStock;
    }

    public void PrintStocks()
    {
      Console.WriteLine("Current: {0}, Reserve: {1}, Total Available: {2}", this.CurrentStock, this.Reserve, this.TotalAvailable);
    }


    public bool QualityControl(int ndiseases) {
      // Currently, this checks the parsing of the AVs, and it returns 1 if there is a problem
      if (this.Disease < 0 || this.Disease > ndiseases ) {
        Console.Error.WriteLine("AV disease invalid,cannot be higher than ", ndiseases);
        return true;
      }

      if (this.m_InitialStock < 0) {
        Console.Error.WriteLine("AV initial_stock invalid, cannot be lower than 0");
        return true;
      }

      if(this.Efficacy > 100 || this.Efficacy < 0) {
        Console.Error.WriteLine("AV Percent_Resistance invalid, must be between 0 and 100");
        return true;
      }

      if(this.CourseLength.Days < 0) {
        Console.Error.WriteLine("AV Course Length invalid, must be higher than 0");
        return true;
      }

      if(reduce_infectivity < 0 || reduce_infectivity > 1.00) {
        Console.Error.WriteLine("AV reduce_infectivity invalid, must be between 0 and 1.0");
        return true;
      }

      if(reduce_susceptibility < 0 || reduce_susceptibility > 1.00) {
        Console.Error.WriteLine("AV reduce_susceptibility invalid, must be between 0 and 1.0");
        return true;
      }

      if(reduce_infectious_period< 0 || reduce_infectious_period > 1.00) {
        Console.Error.WriteLine("AV reduce_infectious_period invalid, must be between 0 and 1.0; is equal to: {0}", reduce_infectious_period);
        //return 1;
        //  TODO: Help!!!  This is never set - just contains whatever garbage present at the address.
      }

      return false;
    }

    public void Effect(Health health, DateTime cur_day, AV_Health av_health)
    {
      avEffect(health, this.Disease, cur_day, av_health);
    }

    private void avEffect(Health health, int disease, DateTime cur_day, AV_Health av_health)
    {
      // If this is the first day of AV Course
      if (cur_day == av_health.AVStartDay)
      {
        ModifySusceptiblilty(health, disease);

        // If you are already exposed, we need to modify your infection
        if ((health.GetExposureDate(disease) > -1) && (cur_day > health.GetExposureDate(disease)))
        {
          if (Global.Debug > 3)
          {
            Console.WriteLine("reducing an already exposed person");
          }

          ModifyInfectivity(health, disease);
          //modify_symptomaticity(health, disease, cur_day);
        }
      }

      // If today is the day you got exposed, prophilaxis
      if (cur_day == health.GetExposureDate(disease))
      {
        if (Global.Debug > 3)
        {
          Console.WriteLine("reducing agent on the day they are exposed");
        }

        ModifyInfectivity(health, disease);
        ModifySymptomaticity(health, disease, cur_day);
      }

      // If this is the last day of the course
      if (cur_day == av_health.AVEndDay)
      {
        if (Global.Debug > 3)
        {
          Console.WriteLine("resetting agent to original state");
        }

        ModifySusceptiblilty(health, disease);

        if (cur_day >= health.GetExposureDate(disease))
        {
          ModifyInfectivity(health, disease);
        }
      }
    }

    private void ModifySusceptiblilty(Health health, int disease)
    {
      health.ModifySusceptiblilty(disease, 1.0 - reduce_susceptibility);
    }

    private void ModifyInfectivity(Health health, int disease)
    {
      health.ModifyInfectivity(disease, 1.0 - reduce_infectivity);
    }

    private void ModifySymptomaticity(Health health, int disease, DateTime cur_day)
    {
      if (!health.IsSymptomatic && cur_day < health.GetSymptomsStartDate(disease))
      {
        // Can only have these effects if the agent is not symptomatic yet
        health.ModifyDevelopsSymptoms(disease, this.RollWillHaveSymp(), cur_day);
      }

      if (!health.IsSymptomatic && cur_day < health.GetSymptomsStartDate(disease))
      {
        health.ModifyAsymptomaticPeriod(disease, 1.0 - reduce_asymp_period, cur_day);
      }

      if (health.IsSymptomatic && cur_day < health.GetSymptomsStartDate(disease))
      {
        health.ModifySymptomaticPeriod(disease, 1.0 - reduce_symp_period, cur_day);
      }
    }
  }
}
