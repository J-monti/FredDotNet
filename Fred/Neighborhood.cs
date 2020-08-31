using System;

namespace Fred
{
  public class Neighborhood : Place
  {
    private static double contacts_per_day;
    private static double same_age_bias;
    private static double[,] prob_transmission_per_contact;
    private static double weekend_contact_rate;

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Neighborhood() : base() { }

    /**
     * Constructor with necessary parameters
     */
    public Neighborhood(string label, char _subtype, FredGeo lon, FredGeo lat)
      : base(label, lon, lat)
    {
      this.set_type(Place.TYPE_NEIGHBORHOOD);
      this.set_subtype(_subtype);
      this.intimacy = 0.0025;
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("neighborhood_contacts", ref contacts_per_day);
      FredParameters.GetParameter("neighborhood_same_age_bias", ref same_age_bias);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("neighborhood_trans_per_contact");
      int n = prob_transmission_per_contact.Length;
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nNeighborhood_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
        }
      }
      FredParameters.GetParameter("weekend_contact_rate", ref weekend_contact_rate);

      if (Global.Verbose > 0)
      {
        Console.WriteLine("\nprob_transmission_per_contact before normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
        }
        Console.WriteLine("\ncontact rate: {0}\n", Neighborhood.contacts_per_day);
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (Neighborhood.prob_transmission_per_contact[i, j] > max_prob)
          {
            max_prob = Neighborhood.prob_transmission_per_contact[i, j];
          }
        }
      }

      // convert max contact prob to 1.0
      if (max_prob > 0)
      {
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Neighborhood.prob_transmission_per_contact[i, j] /= max_prob;
          }
        }
        // compensate contact rate
        Neighborhood.contacts_per_day *= max_prob;
        // end normalization

        if (Global.Verbose > 0)
        {
          Console.WriteLine("\nprob_transmission_per_contact after normalization:\n");
          for (int i = 0; i < n; ++i)
          {
            for (int j = 0; j < n; ++j)
            {
              Console.WriteLine("{0} ", Neighborhood.prob_transmission_per_contact[i, j]);
            }
          }
          Console.WriteLine("\ncontact rate: {0}\n", Neighborhood.contacts_per_day);
        }
        // end normalization
      }
    }

    /**
     * @see Place.get_group(int disease, Person* per)
     */
    public override int get_group(int disease, Person per)
    {
      int age = per.get_age();
      if (age < Global.ADULT_AGE)
      {
        return 0;
      }
      else
      {
        return 1;
      }
    }

    public override double get_transmission_probability(int disease, Person i, Person s)
    {
      double age_i = i.get_real_age();
      double age_s = s.get_real_age();
      double diff = Math.Abs(age_i - age_s);
      double prob = Math.Exp(-same_age_bias * diff);
      return prob;
    }

    /**
     * @see Place.get_transmission_prob(int disease, Person* i, Person* s)
     *
     * This method returns the value from the static array <code>Neighborhood.Neighborhood_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Neighborhood_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>neighborhood_prob[]</code>.
     */
    public override double get_transmission_prob(int disease, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
    }

    /**
     * @see Place.get_contacts_per_day(int disease)
     *
     * This method returns the value from the static array <code>Neighborhood.Neighborhood_contacts_per_day</code>
     * that corresponds to a particular disease.<br />
     * The static array <code>Neighborhood_contacts_per_day</code> will be filled with values from the parameter
     * file for the key <code>neighborhood_contacts[]</code>.
     */
    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    /**
     * Determine if the neighborhood should be open. It is dependent on the disease and simulation day.
     *
     * @param day the simulation day
     * @param disease an integer representation of the disease
     * @return whether or not the neighborhood is open on the given day for the given disease
     */
    public override bool should_be_open(int day, int disease)
    {
      return true;
    }

    /**
     * Returns the rate by which to increase neighborhood contacts on weekends
     *
     * @return the rate by which to increase neighborhood contacts on weekends
     */
    public static double get_weekend_contact_rate(int disease)
    {
      return Neighborhood.weekend_contact_rate;
    }
  }
}
