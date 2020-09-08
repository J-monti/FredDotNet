using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Classroom : Place
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;
    private static char[] Classroom_closure_policy;
    private static int Classroom_closure_day;
    private static double Classroom_closure_threshold;
    private static int Classroom_closure_period;
    private static int Classroom_closure_delay;
    private School school;
    private int age_level;

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Classroom()
    {
      this.set_type(Place.TYPE_CLASSROOM);
      this.set_subtype(Place.SUBTYPE_NONE);
      this.age_level = -1;
    }

    /**
     * Constructor with necessary parameters
     */
    public Classroom(string label, char _subtype, FredGeo lon, FredGeo lat)
       : base(label, lon, lat)
    {
      this.set_type(Place.TYPE_CLASSROOM);
      this.set_subtype(_subtype);
      this.age_level = -1;
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("classroom_contacts", ref contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("classroom_trans_per_contact");
      int n = Convert.ToInt32(Math.Sqrt(prob_transmission_per_contact.Length));
      if (Global.Verbose > 1)
      {
        Utils.FRED_STATUS(0, "\nClassroom_contact_prob:");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Utils.FRED_STATUS(0, "{0} ", prob_transmission_per_contact[i, j]);
          }
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
        Utils.FRED_STATUS(0, "\nClassroom_contact_prob after normalization:");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Utils.FRED_STATUS(0, "{0} ", prob_transmission_per_contact[i, j]);
          }
          Utils.FRED_STATUS(0, "\n");
        }
        Utils.FRED_STATUS(0, "\ncontact rate: {0}", contacts_per_day);
      }
      // end normalization
    }

    public override int enroll(Person person)
    {
      Utils.assert(person.is_teacher() == false);

      // call base class method:
      int return_value = base.enroll(person);
      int age = person.get_age();
      int grade = ((age < Neighborhood_Patch.GRADES) ? age : Neighborhood_Patch.GRADES - 1);
      Utils.assert(grade > 0);

      Utils.FRED_VERBOSE(1, "Enrolled person {0} age {1} in classroom {2} grade {3} {4}",
             person.get_id(), person.get_age(), this.get_id(), this.age_level, this.get_label());
      if (this.age_level == -1)
      {
        this.age_level = age;
      }
      Utils.assert(grade == this.age_level);

      return return_value;
    }

    public override void unenroll(int pos)
    {
      int size = this.enrollees.Count;
      Utils.assert(0 <= pos && pos < size);
      var removed = this.enrollees[pos];
      Utils.assert(removed.is_teacher() == false);
      int grade = removed.get_grade();
      Utils.FRED_VERBOSE(1, "UNENROLL removed {0} age {1} grade {2}, is_teacher {3} from school {4} {5} size = {6}",
             removed.get_id(), removed.get_age(), grade, removed.is_teacher() ? 1 : 0, this.get_id(), this.get_label(), this.get_size());

      // call base class method
      base.unenroll(pos);
    }

    /**
     * @see Place::get_group(int disease, Person* per)
     */
    public override int get_group(int disease, Person per)
    {
      return this.school.get_group(disease, per);
    }

    /**
     * @see Mixing_Group::get_transmission_prob(int disease, Person* i, Person* s)
     *
     * This method returns the value from the static array <code>Classroom::Classroom_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Classroom_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>classroom_prob[]</code>.
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

    public override bool is_open(int day)
    {
      bool open = this.school.is_open(day);
      if (!open)
      {
        Utils.FRED_VERBOSE(0, "Place {0} is closed on day {1}", this.get_label(), day);
      }
      return open;
    }

    /**
     * @see Place::should_be_open(int day, int disease)
     */
    public override bool should_be_open(int day, int disease)
    {
      return this.school.should_be_open(day, disease);
    }

    /**
     * @see Place::get_contacts_per_day(int disease)
     *
     * This method returns the value from the static array <code>Classroom::Classroom_contacts_per_day</code>
     * that corresponds to a particular disease.<br />
     * The static array <code>Classroom_contacts_per_day</code> will be filled with values from the parameter
     * file for the key <code>classroom_contacts[]</code>.
     */
    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    /**
     *  @return the age_level
     */
    public int get_age_level()
    {
      return this.age_level;
    }

    public void set_school(School _school)
    {
      this.school = _school;
    }

    public School get_school()
    {
      return this.school;
    }

    public override int get_container_size()
    {
      return this.school.get_size();
    }
  }
}
