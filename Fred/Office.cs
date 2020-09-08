using System;

namespace Fred
{
  public class Office : Place
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;
    private Workplace workplace;

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Office()
    {
      this.set_type(Place.TYPE_OFFICE);
      this.set_subtype(Place.SUBTYPE_NONE);
      this.workplace = null;
    }

    /**
     * Constructor with necessary parameters
     */
    public Office(string label, char _subtype, FredGeo lon, FredGeo lat)
      : base (label, lon, lat)
    {
      this.set_type(Place.TYPE_OFFICE);
      this.workplace = null;
      this.set_subtype(_subtype);
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("office_contacts", ref contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("office_trans_per_contact");
      int n = Convert.ToInt32(Math.Sqrt(prob_transmission_per_contact.Length));
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nOffice_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (prob_transmission_per_contact[i, j] > max_prob)
          {
            max_prob = prob_transmission_per_contact[i, j];
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
            prob_transmission_per_contact[i, j] /= max_prob;
          }
        }
        // compensate contact rate
        contacts_per_day *= max_prob;
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine("\nOffice_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
        Console.WriteLine("\ncontact rate: {0}\n", Office.contacts_per_day);
      }
      // end normalization
    }

    /**
     * @see Place.get_group(int disease, Person* per)
     */
    public override int get_group(int disease, Person per)
    {
      return 0;
    }

    public override int get_container_size()
    {
      return this.workplace.get_size();
    }

    /**
     * @see Place.get_transmission_prob(int disease, Person* i, Person  s)
     *
     * This method returns the value from the static array <code>Office.Office_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Office_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>office_prob[]</code>.
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
     * This method returns the value from the static array <code>Office.Office_contacts_per_day</code>
     * that corresponds to a particular disease.<br />
     * The static array <code>Office_contacts_per_day</code> will be filled with values from the parameter
     * file for the key <code>office_contacts[]</code>.
     */
    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    /**
     * Determine if the office should be open. It is dependent on the disease and simulation day.
     *
     * @param day the simulation day
     * @param disease an integer representation of the disease
     * @return whether or not the office is open on the given day for the given disease
     */
    public override bool should_be_open(int day, int disease)
    {
      return true;
    }

    public void set_workplace(Workplace _workplace)
    {
      this.workplace = _workplace;
    }

    public Workplace get_workplace()
    {
      return this.workplace;
    }
  }
}
