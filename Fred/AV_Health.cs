using System;

namespace Fred
{
  public class AV_Health
  {
    private int av_day;           // Day on which the AV treatment starts
    private int av_end_day;       // Day on which the AV treatment ends
    private Health health;       // Pointer to the health class for agent
    private int disease;           // Disease for this AV
    private Antiviral AV;        // Pointer to the AV the person took

    /**
   * Default constructor
   */
    public AV_Health() { }

    /**
     * Constructor that sets the AV start day, the Antiviral, and the Health of this AV_Health object
     *
     * @param _av_day the av start day
     * @param _AV a pointer to the Antiviral object
     * @param _health a pointer to the Health object
     */
    public AV_Health(int _av_day, Antiviral _AV, Health _health)
    {
      AV = _AV;
      disease = AV.get_disease();
      av_day = _av_day + 1;
      health = _health;
      av_end_day = -1;
      av_end_day = av_day + AV.get_course_length();
    }

    //Access Members 
    /**
     * @return the AV start day
     */
    public virtual int get_av_start_day() { return av_day; }

    /**
     * @return the AV end day
     */
    public virtual int get_av_end_day() { return av_end_day; }

    /**
     * @return a pointer to the Health object
     */
    public virtual Health get_health() { return health; }

    /**
     * @return the disease
     */
    public virtual int get_disease() { return disease; }

    /**
     * @return a pointer to the AV
     */
    public virtual Antiviral get_antiviral() { return AV; }

    /**
     * @param day the simulation day to check for
     * @return <code>true</code> if day is between the start and end days, <code>false</code> otherwise
     */
    public virtual bool is_on_av(int day)
    {
      return ((day >= av_day) && (day <= av_end_day));
    }

    /**
     * @return <code>true</code> if av_end_day is not -1, <code>false</code> otherwise
     */
    public virtual bool is_effective()
    {
      return (av_end_day != -1);
    }

    //Utility Functions
    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public virtual void update(int day)
    {
      if (day <= av_end_day)
      {
        if (health.get_infection(0) != null)
        {
          if (Global.Debug > 3)
          {
            Console.WriteLine();
            Console.WriteLine("Before");
            health.get_infection(0).print();
          }
        }
        else if (Global.Debug > 3)
        {
          Console.WriteLine();
          Console.WriteLine("Before: Suceptibility {0}", health.get_susceptibility(0));
        }
      }
      AV.effect(health, day, this);
      if (day <= av_end_day)
      {
        if (Global.Debug > 3)
        {
          if (health.get_infection(0) != null)
          {
            Console.WriteLine();
            Console.WriteLine("After");
            health.get_infection(0).print();
          }
          else if (Global.Debug > 3)
          {
            Console.WriteLine("After: Suceptibility {0}", health.get_susceptibility(0));
          }
        }
      }
    }

    /**
     * Print out information about this object
     */
    public virtual void print()
    {
      Console.WriteLine("AV Health Status...");
    }

    /**
     * Print out information about this object to the trace file
     */
    public virtual void printTrace()
    {
      Global.Tracefp.WriteLine("AV_Health - {0} {0} {0}", av_day, disease, is_effective());
      Global.Tracefp.Flush();
    }
  }
}
