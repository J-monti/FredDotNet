using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Workplace : Place
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;
    private static int Office_size = 0;
    private static int total_workers = 0;
    private static List<int> workplace_size_max = new List<int>(); // vector to hold the upper limit for each workplace size group
    private static List<int> workers_by_workplace_size = new List<int>(); // vector to hold the counts of workers in each group (plus, the "greater than" group)
    private static int workplace_size_group_count = 0;
    private List<Place> offices;
    private int next_office;

    public Workplace() : base()
    {
      this.set_type(Place.TYPE_WORKPLACE);
      this.set_subtype(SUBTYPE_NONE);
      this.intimacy = 0.01;
      this.offices = new List<Place>();
      this.next_office = 0;
    }

    public Workplace(string label, char _subtype, double lon, double lat)
      : base(label, lon, lat)
    {
      this.set_type(Place.TYPE_WORKPLACE);
      this.set_subtype(_subtype);
      this.intimacy = 0.01;
      this.offices = new List<Place>();
      this.next_office = 0;
    }

    public static void get_parameters()
    {
      // people per office
      FredParameters.GetParameter("office_size", ref Office_size);

      // workplace size limits
      workplace_size_max = FredParameters.GetParameterList<int>("workplace_size_max");
      workplace_size_group_count = workplace_size_max.Count;
      //Add the last column so that it goes to intmax
      workplace_size_max.Add(int.MaxValue);
      workplace_size_group_count++;
      //Set all of the workplace counts to 0
      for (int i = 0; i < workplace_size_group_count; ++i)
      {
        workers_by_workplace_size.Add(0);
      }

      FredParameters.GetParameter("workplace_contacts", ref contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("workplace_trans_per_contact");
      int n = Convert.ToInt32(Math.Sqrt(prob_transmission_per_contact.Length));
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nWorkplace_contact_prob:");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("%f ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine("\n");
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
        Console.WriteLine("\nWorkplace_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
        Console.WriteLine("\ncontact rate: {0}", contacts_per_day);
      }
      // end normalization
    }

    // this method is called after all workers are assigned to workplaces
    public override void prepare()
    {
      total_workers += get_size();
      // update employment stats based on size of workplace
      for (int i = 0; i < workplace_size_group_count; ++i)
      {
        if (get_size() < workplace_size_max[i])
        {
          workers_by_workplace_size[i] += get_size();
          break;
        }
      }

      int wp_size_min = 0;
      for (int i = 0; i < workplace_size_group_count; ++i)
      {
        wp_size_min = workplace_size_max[i] + 1;
      }

      // now call base class function to perform preparations common to all Places 
      base.prepare();
    }

    public override double get_transmission_prob(int disease, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
    }

    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    public int get_number_of_rooms()
    {
      if (Office_size == 0)
      {
        return 0;
      }
      int rooms = get_size() / Office_size;
      this.next_office = 0;
      if (get_size() % Office_size != 0)
      {
        rooms++;
      }
      if (rooms == 0)
      {
        ++rooms;
      }
      return rooms;
    }

    public void setup_offices()
    {
      int rooms = get_number_of_rooms();
      //FredUtils.Status(1, "workplace %d %s number %d rooms %d\n", this.get_id(), this.get_label(), this.get_size(), rooms);
      for (int i = 0; i < rooms; ++i)
      {
        string new_label = string.Format("{0}-{0:0.000}", this.get_label(), i);
        var office = new Office(new_label,
                              SUBTYPE_NONE,
                              this.get_longitude(),
                              this.get_latitude());

        office.set_workplace(this);
        this.offices.Add(office);
        //FredUtils.Status(1, "workplace %d %s added office %d %s %d\n", this.get_id(), this.get_label(), i,
        //            office.get_label(), office.get_id());
      }
    }

    public Place assign_office(Person per)
    {
      if (Office_size == 0)
      {
        return null;
      }

      //FredUtils.Status(1, "assign office for person %d at workplace %d %s size %d == ", per.get_id(),
      //            this.get_id(), this.get_label(), this.get_size());
      // pick next office, round-robin
      int i = this.next_office;
      //FredUtils.Status(1, "office = %d %s %d\n",
      //      i, offices[i].get_label(), offices[i].get_id());

      // update next pick
      if (this.next_office < this.offices.Count - 1)
      {
        this.next_office++;
      }
      else
      {
        this.next_office = 0;
      }
      return this.offices[i];
    }

    public override int get_group(int disease, Person per)
    {
      return 0;
    }

    public int get_workplace_size_group_id()
    {
      for (int i = 0; i < get_workplace_size_group_count(); ++i)
      {
        if (i <= get_workplace_size_max_by_group_id(i))
        {
          return i;
        }
      }
      return -1;
    }

    /**
     * Determine if the Workplace should be open. It is dependent on the disease and simulation day.
     *
     * @param day the simulation day
     * @param disease an integer representation of the disease
     * @return whether or not the workplace is open on the given day for the given disease
     */
    public override bool should_be_open(int day, int disease)
    {
      return true;
    }

    public static int get_max_office_size()
    {
      return Office_size;
    }

    public static int get_total_workers()
    {
      return total_workers;
    }

    // for access from Office
    public static double get_workplace_contacts_per_day(int disease_id)
    {
      return contacts_per_day;
    }

    public static int get_workplace_size_group_count()
    {
      return workplace_size_group_count;
    }

    public static int get_workplace_size_max_by_group_id(int group_id)
    {
      if (group_id < 0 || group_id > get_workplace_size_group_count())
      {
        return -1;
      }
      else
      {
        return workplace_size_max[group_id];
      }
    }

    public static int get_count_workers_by_workplace_size(int group_id)
    {
      if (group_id < 0 || group_id > workplace_size_group_count)
      {
        return -1;
      }
      else
      {
        return workers_by_workplace_size[group_id];
      }
    }
  }
}
