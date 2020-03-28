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
    private static List<int> workplace_size_max; // vector to hold the upper limit for each workplace size group
    private static List<int> workers_by_workplace_size; // vector to hold the counts of workers in each group (plus, the "greater than" group)
    private static int workplace_size_group_count = 0;
    private List<Place> offices;
    private int next_office;

    public Workplace() : base()
    {
      this.set_type(PlaceType.Workplace);
      this.set_subtype(PlaceSubType.None);
      this.intimacy = 0.01;
      this.offices = new List<Place>();
      this.next_office = 0;
    }

    public Workplace(string lab, PlaceSubType _subtype, double lon, double lat)
      : base(lab, lon, lat)
    {
      this.set_type(PlaceType.Workplace);
      this.set_subtype(_subtype);
      this.intimacy = 0.01;
      this.offices = new List<Place>();
      this.next_office = 0;
    }

    public void get_parameters()
    {
      // people per office
      Params::get_param_from_string("office_size", Office_size);

      // workplace size limits
      Workplace::workplace_size_group_count = Params::get_param_vector((char*)"workplace_size_max", Workplace::workplace_size_max);
      //Add the last column so that it goes to intmax
      Workplace::workplace_size_max.push_back(INT_MAX);
      Workplace::workplace_size_group_count++;
      //Set all of the workplace counts to 0
      for (int i = 0; i < Workplace::workplace_size_group_count; ++i)
      {
        Workplace::workers_by_workplace_size.push_back(0);
      }

      Params::get_param_from_string("workplace_contacts", &Workplace::contacts_per_day);
      int n = Params::get_param_matrix((char*)"workplace_trans_per_contact", &Workplace::prob_transmission_per_contact);
      if (Global::Verbose > 1)
      {
        printf("\nWorkplace_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", Workplace::prob_transmission_per_contact[i][j]);
          }
          printf("\n");
        }
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (Workplace::prob_transmission_per_contact[i][j] > max_prob)
          {
            max_prob = Workplace::prob_transmission_per_contact[i][j];
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
            Workplace::prob_transmission_per_contact[i][j] /= max_prob;
          }
        }
        // compensate contact rate
        Workplace::contacts_per_day *= max_prob;
      }

      if (Global::Verbose > 0)
      {
        printf("\nWorkplace_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", Workplace::prob_transmission_per_contact[i][j]);
          }
          printf("\n");
        }
        printf("\ncontact rate: %f\n", Workplace::contacts_per_day);
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
      if (get_size() % Office_size)
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
      //FRED_STATUS(1, "workplace %d %s number %d rooms %d\n", this.get_id(), this.get_label(), this.get_size(), rooms);
      for (int i = 0; i < rooms; ++i)
      {
        string new_label = string.Format("{0}-{0:0.000}", this.Label, i);
        var office = new Office(new_label,
                              PlaceSubType.None,
                              this.get_longitude(),
                              this.get_latitude());

        office.set_workplace(this);
        this.offices.Add(office);
        //FRED_STATUS(1, "workplace %d %s added office %d %s %d\n", this.get_id(), this.get_label(), i,
        //            office.get_label(), office.get_id());
      }
    }

    public Place assign_office(Person per)
    {
      if (Office_size == 0)
      {
        return null;
      }

      //FRED_STATUS(1, "assign office for person %d at workplace %d %s size %d == ", per.get_id(),
      //            this.get_id(), this.get_label(), this.get_size());
      // pick next office, round-robin
      int i = this.next_office;
      //FRED_STATUS(1, "office = %d %s %d\n",
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
