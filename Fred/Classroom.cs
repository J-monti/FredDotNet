using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Classroom : Place
  {
    //Private static variables that will be set by parameter lookups
    public static double contacts_per_day;
    public static double** prob_transmission_per_contact;
    public static char Classroom_closure_policy[80];
    public static int Classroom_closure_day = 0;
    public static double Classroom_closure_threshold = 0.0;
    public static int Classroom_closure_period = 0;
    public static int Classroom_closure_delay = 0;
    private School school;
    private int age_level;

    public Classroom()
    {
      this.set_type(Place::TYPE_CLASSROOM);
      this.set_subtype(Place::SUBTYPE_NONE);
      this.age_level = -1;
      this.school = NULL;
    }

    public Classroom(string lab, char _subtype, double lon, double lat)
      : base (lab, lon, lat)
    {
      this.set_type(Place::TYPE_CLASSROOM);
      this.set_subtype(_subtype);
      this.age_level = -1;
      this.school = NULL;
    }

    void get_parameters()
    {

      Params::get_param_from_string("classroom_contacts", &contacts_per_day);
      int n = Params::get_param_matrix((char*)"classroom_trans_per_contact", &prob_transmission_per_contact);
      if (Global::Verbose > 1)
      {
        FRED_STATUS(0, "\nClassroom_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            FRED_STATUS(0, "%f ", prob_transmission_per_contact[i][j]);
          }
          FRED_STATUS(0, "\n");
        }
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (prob_transmission_per_contact[i][j] > max_prob)
          {
            max_prob = prob_transmission_per_contact[i][j];
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
            prob_transmission_per_contact[i][j] /= max_prob;
          }
        }
        // compensate contact rate
        contacts_per_day *= max_prob;
      }

      if (Global::Verbose > 0)
      {
        FRED_STATUS(0, "\nClassroom_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            FRED_STATUS(0, "%f ", prob_transmission_per_contact[i][j]);
          }
          FRED_STATUS(0, "\n");
        }
        FRED_STATUS(0, "\ncontact rate: %f\n", contacts_per_day);
      }
      // end normalization
    }

    double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    int get_group(int disease, Person* per)
    {
      return this.school.get_group(disease, per);
    }

    double get_transmission_prob(int disease, Person* i, Person* s)
    {

      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = prob_transmission_per_contact[row][col];
      return tr_pr;
    }

    bool is_open(int day)
    {
      bool open = this.school.is_open(day);
      if (!open)
      {
        FRED_VERBOSE(0, "Place %s is closed on day %d\n", this.get_label(), day);
      }
      return open;
    }

    bool should_be_open(int day, int disease)
    {
      return this.school.should_be_open(day, disease);
    }

    int get_container_size()
    {
      return this.school.get_size();
    }

    int enroll(Person* person)
    {
      assert(person.is_teacher() == false);

      // call base class method:
      int return_value = Mixing_Group::enroll(person);

      int age = person.get_age();
      int grade = ((age < GRADES) ? age : GRADES - 1);
      assert(grade > 0);

      FRED_VERBOSE(1, "Enrolled person %d age %d in classroom %d grade %d %s\n",
             person.get_id(), person.get_age(), this.get_id(), this.age_level, this.get_label());
      if (this.age_level == -1)
      {
        this.age_level = age;
      }
      assert(grade == this.age_level);

      return return_value;
    }

    void unenroll(int pos)
    {
      int size = this.enrollees.size();
      assert(0 <= pos && pos < size);
      Person* removed = this.enrollees[pos];
      assert(removed.is_teacher() == false);
      int grade = removed.get_grade();
      FRED_VERBOSE(1, "UNENROLL removed %d age %d grade %d, is_teacher %d from school %d %s size = %d\n",
             removed.get_id(), removed.get_age(), grade, removed.is_teacher() ? 1 : 0, this.get_id(), this.get_label(), this.get_size());

      // call base class method
      Mixing_Group::unenroll(pos);
    }
  }
}
