using System;
using System.Collections.Generic;

namespace Fred
{
  public class School : Place
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;
    private static string school_closure_policy = "undefined";
    private static int school_closure_day;
    private static int min_school_closure_day;
    private static double school_closure_threshold;
    private static double individual_school_closure_threshold;
    private static int school_closure_cases = -1;
    private static int school_closure_duration;
    private static int school_closure_delay;
    private static int school_summer_schedule;
    private static string school_summer_start;
    private static string school_summer_end;
    private static int summer_start_month;
    private static int summer_start_day;
    private static int summer_end_month;
    private static int summer_end_day;
    private static int school_classroom_size;
    private static bool global_closure_is_active;
    private static int global_close_date;
    private static int global_open_date;

    private static int total_school_pop;
    private static int pop_income_Q1;
    private static int pop_income_Q2;
    private static int pop_income_Q3;
    private static int pop_income_Q4;

    private int[] students_in_grade;
    private int[] orig_students_in_grade;
    private int[] next_classroom;
    private List<Classroom>[] classrooms;
    private bool closure_dates_have_been_set;
    private int max_grade;
    private int income_quartile;

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public School()
    {
      this.set_type(TYPE_SCHOOL);
      this.set_subtype(SUBTYPE_NONE);
      this.intimacy = 0.025;
      this.students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.orig_students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.classrooms = new List<Classroom>[Neighborhood_Patch.GRADES];
      this.students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.next_classroom = new int[Neighborhood_Patch.GRADES];
      for (int i = 0; i < Neighborhood_Patch.GRADES; ++i)
      {
        this.students_in_grade[i] = 0;
        this.orig_students_in_grade[i] = 0;
        this.next_classroom[i] = 0;
        this.classrooms[i] = new List<Classroom>();
      }
      this.closure_dates_have_been_set = false;
      this.staff_size = 0;
      this.max_grade = -1;
      this.county_index = -1;
      this.income_quartile = -1;
    }

    /**
     * Constructor with necessary parameters
     */
    public School(string label, char _subtype, FredGeo lon, FredGeo lat)
      : base(label, lon, lat)
    {
      this.set_type(TYPE_SCHOOL);
      this.set_subtype(_subtype);
      this.intimacy = 0.025;
      this.students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.orig_students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.classrooms = new List<Classroom>[Neighborhood_Patch.GRADES];
      this.students_in_grade = new int[Neighborhood_Patch.GRADES];
      this.next_classroom = new int[Neighborhood_Patch.GRADES];
      for (int i = 0; i < Neighborhood_Patch.GRADES; ++i)
      {
        this.students_in_grade[i] = 0;
        this.orig_students_in_grade[i] = 0;
        this.next_classroom[i] = 0;
        this.classrooms[i] = new List<Classroom>();
      }
      this.closure_dates_have_been_set = false;
      this.staff_size = 0;
      this.max_grade = -1;
      this.county_index = -1;
      this.income_quartile = -1;
    }

    public override void prepare()
    {
      Utils.assert(Global.Pop.is_load_completed());
      // call base class function to perform preparations common to all Places 
      base.prepare();

      // record original size in each grade
      for (int g = 0; g < Neighborhood_Patch.GRADES; ++g)
      {
        this.orig_students_in_grade[g] = this.students_in_grade[g];
      }
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("school_contacts", ref contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("school_trans_per_contact");
      int n = Convert.ToInt32(Math.Sqrt(prob_transmission_per_contact.Length));
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nSchool_contact_prob:");
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
        Console.WriteLine("\nSchool_contact_prob after normalization:");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", School.prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
        Console.WriteLine("\ncontact rate: {0}", School.contacts_per_day);
      }
      // end normalization

      FredParameters.GetParameter("school_classroom_size", ref school_classroom_size);

      // summer school parameters
      FredParameters.GetParameter("school_summer_schedule", ref school_summer_schedule);
      FredParameters.GetParameter("school_summer_start", ref school_summer_start);
      FredParameters.GetParameter("school_summer_end", ref school_summer_end);
      school_summer_start = $"{summer_start_month}-{summer_start_day}";
      school_summer_end = $"{summer_end_month}-{summer_end_day}";

      // school closure parameters
      FredParameters.GetParameter("school_closure_policy", ref school_closure_policy);
      FredParameters.GetParameter("school_closure_duration", ref school_closure_duration);
      FredParameters.GetParameter("school_closure_delay", ref school_closure_delay);
      FredParameters.GetParameter("school_closure_day", ref school_closure_day);
      FredParameters.GetParameter("min_school_closure_day", ref min_school_closure_day);
      FredParameters.GetParameter("school_closure_ar_threshold", ref school_closure_threshold);
      FredParameters.GetParameter("individual_school_closure_ar_threshold", ref individual_school_closure_threshold);
      FredParameters.GetParameter("school_closure_cases", ref school_closure_cases);

      // aliases for parameters
      int Weeks = 0;
      FredParameters.GetParameter("Weeks", ref Weeks);
      if (Weeks > -1)
      {
        School.school_closure_duration = 7 * Weeks;
      }

      int cases = 0;
      FredParameters.GetParameter("Cases", ref cases);
      if (cases > -1)
      {
        School.school_closure_cases = cases;
      }
    }

    public override int get_group(int disease_id, Person per)
    {
      int age = per.get_age();
      if (age < 12)
      {
        return 0;
      }
      else if (age < 16)
      {
        return 1;
      }
      else if (per.is_student())
      {
        return 2;
      }
      else
      {
        return 3;
      }
    }

    public override double get_transmission_prob(int disease_id, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease_id, i);
      int col = get_group(disease_id, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
    }

    public void close(int day, int day_to_close, int duration)
    {
      this.close_date = day_to_close;
      this.open_date = close_date + duration;
      this.closure_dates_have_been_set = true;

      // log this school closure decision
      Utils.FRED_VERBOSE(1, "SCHOOL %s CLOSURE decision day %d close_date %d duration %d open_date %d\n",
        this.get_label(), day, this.close_date, duration, this.open_date);
    }

    public override bool is_open(int day)
    {
      bool open = (day < this.close_date || this.open_date <= day);
      if (!open)
      {
        Utils.FRED_VERBOSE(0, "Place {0} is closed on day {1}", this.get_label(), day);
      }
      return open;
    }

    public override bool should_be_open(int day, int disease_id)
    {
      // no students
      if (get_size() == 0)
      {
        return false;
      }

      // summer break
      if (school_summer_schedule > 0)
      {
        int month = Date.get_month();
        int day_of_month = Date.get_day_of_month();
        if ((month == School.summer_start_month && School.summer_start_day <= day_of_month) ||
           (month == School.summer_end_month && day_of_month <= School.summer_end_day) ||
           (School.summer_start_month < month && month < School.summer_end_month))
        {
          Utils.FRED_STATUS(1, "School %s closed for summer\n", this.get_label());
          return false;
        }
      }

      // stick to previously made decision to close
      if (this.closure_dates_have_been_set)
      {
        return is_open(day);
      }

      // global school closure policy in effect
      if (school_closure_policy == "global")
      {
        apply_global_school_closure_policy(day, disease_id);
        return is_open(day);
      }

      // individual school closure policy in effect
      if (school_closure_policy == "individual")
      {
        apply_individual_school_closure_policy(day, disease_id);
        return is_open(day);
      }

      // if school_closure_policy is not recognized, then open
      return true;
    }

    public void apply_global_school_closure_policy(int day, int disease_id)
    {

      // Only test triggers for school closure if not global closure is not already activated
      if (School.global_closure_is_active == false)
      {
        // Setting school_closure_day > -1 overrides other global triggers.
        // Close schools if the closure date has arrived (after a delay)
        if (School.school_closure_day > -1)
        {
          if (School.school_closure_day <= day)
          {
            // the following only happens once
            School.global_close_date = day + School.school_closure_delay;
            School.global_open_date = day + School.school_closure_delay
              + School.school_closure_duration;
            School.global_closure_is_active = true;
          }
        }
        else
        {
          // Close schools if the global symptomatic attack rate has reached the threshold (after a delay)
          var disease = Global.Diseases.get_disease(disease_id);
          if (School.school_closure_threshold <= disease.get_symptomatic_attack_rate())
          {
            // the following only happens once
            School.global_close_date = day + School.school_closure_delay;
            School.global_open_date = day + School.school_closure_delay
              + School.school_closure_duration;
            School.global_closure_is_active = true;
          }
        }
      }
      if (School.global_closure_is_active)
      {
        // set close and open dates for this school (only once)
        close(day, School.global_close_date, School.school_closure_duration);

        // log this school closure decision
        if (Global.Verbose > 0)
        {
          var disease = Global.Diseases.get_disease(disease_id);
          Utils.FRED_VERBOSE(0, "GLOBAL SCHOOL CLOSURE pop_ar %5.2f local_cases = %d / %d (%5.2f)\n",
           disease.get_symptomatic_attack_rate(), get_total_cases(disease_id),
           get_size(), get_symptomatic_attack_rate(disease_id));
        }
      }
    }
    public void apply_individual_school_closure_policy(int day, int disease_id)
    {

      // don't apply any policy prior to School.min_school_closure_day
      if (day <= School.min_school_closure_day)
      {
        return;
      }

      // don't apply any policy before the epidemic reaches a noticeable threshold
      var disease = Global.Diseases.get_disease(disease_id);
      if (disease.get_symptomatic_attack_rate() < School.school_closure_threshold)
      {
        return;
      }

      bool close_this_school;
      // if school_closure_cases > -1 then close if this number of cases occurs
      if (School.school_closure_cases != -1)
      {
        close_this_school = (School.school_closure_cases <= get_total_cases(disease_id));
      }
      else
      {
        // close if attack rate threshold is reached
        close_this_school = (School.individual_school_closure_threshold <= get_symptomatic_attack_rate(disease_id));
      }

      if (close_this_school)
      {
        // set close and open dates for this school (only once)
        close(day, day + School.school_closure_delay, School.school_closure_duration);

        // log this school closure decision
        if (Global.Verbose > 0)
        {
          Utils.FRED_VERBOSE(0, "LOCAL SCHOOL CLOSURE pop_ar %.3f local_cases = %d / %d (%.3f)\n",
           disease.get_symptomatic_attack_rate(), get_total_cases(disease_id),
           get_size(), get_symptomatic_attack_rate(disease_id));
        }
      }
    }

    public override double get_contacts_per_day(int disease_id)
    {
      return School.contacts_per_day;
    }

    public override int enroll(Person person)
    {
      // call base class method:
      int return_value = base.enroll(person);

      Utils.FRED_VERBOSE(1, "Enroll person %d age %d in school %d %s new size %d\n",
             person.get_id(), person.get_age(), this.get_id(), this.get_label(), this.get_size());
      if (person.is_teacher())
      {
        this.staff_size++;
      }
      else
      {
        int age = person.get_age();
        int grade = ((age < Neighborhood_Patch.GRADES) ? age : Neighborhood_Patch.GRADES - 1);
        Utils.assert(grade > 0);
        this.students_in_grade[grade]++;
        if (grade > this.max_grade)
        {
          this.max_grade = grade;
        }
        person.set_grade(grade);
      }

      return return_value;
    }

    public override void unenroll(int pos)
    {
      int size = this.enrollees.Count;
      Utils.assert(0 <= pos && pos < size);
      var removed = this.enrollees[pos];
      int grade = removed.get_grade();
      Utils.FRED_VERBOSE(1, "UNENROLL person %d age %d grade %d, is_teacher %d from school %d %s Size = %d\n",
             removed.get_id(), removed.get_age(), grade, removed.is_teacher() ? 1 : 0, this.get_id(), this.get_label(), this.get_size());

      // call the base class method
      base.unenroll(pos);

      if (removed.is_teacher() || grade == 0)
      {
        this.staff_size--;
      }
      else
      {
        Utils.assert(0 < grade && grade <= this.max_grade);
        this.students_in_grade[grade]--;
      }
      removed.set_grade(0);
      Utils.FRED_VERBOSE(1, "UNENROLLED from school %d %s size = %d\n", this.get_id(), this.get_label(), this.get_size());

    }
    public int get_max_grade()
    {
      return this.max_grade;
    }

    public int get_orig_students_in_grade(int grade)
    {
      if (grade < 0 || this.max_grade < grade)
      {
        return 0;
      }
      return this.orig_students_in_grade[grade];
    }

    public int get_students_in_grade(int grade)
    {
      if (grade < 0 || this.max_grade < grade)
      {
        return 0;
      }
      return this.students_in_grade[grade];
    }

    public int get_classrooms_in_grade(int grade)
    {
      if (grade < 0 || Neighborhood_Patch.GRADES <= grade)
      {
        return 0;
      }
      return this.classrooms[grade].Count;
    }

    public void print_size_distribution()
    {
      for (int g = 1; g < Neighborhood_Patch.GRADES; ++g)
      {
        if (this.orig_students_in_grade[g] > 0)
        {
          Console.WriteLine("SCHOOL {0} grade {1} orig {2} current {3}",
           this.get_label(), g, this.orig_students_in_grade[g], this.students_in_grade[g]);
        }
      }
    }

    public override void print(int disease)
    {
      Utils.FRED_STATUS(0, "Place %d label %s type %c ", this.get_id(), this.get_label(), this.get_type());
      for (int g = 0; g < Neighborhood_Patch.GRADES; ++g)
      {
        Utils.FRED_STATUS(0, "%d students in grade %d | ", this.students_in_grade[g], g);
      }
      Utils.FRED_STATUS(0, "\n");
    }

    public int get_number_of_rooms()
    {
      int total_rooms = 0;
      if (School.school_classroom_size == 0)
      {
        return 0;
      }
      for (int a = 0; a < Neighborhood_Patch.GRADES; ++a)
      {
        int n = this.students_in_grade[a];
        if (n == 0)
        {
          continue;
        }
        int rooms = n / School.school_classroom_size;
        if (n % School.school_classroom_size != 0)
        {
          rooms++;
        }
        total_rooms += rooms;
      }
      return total_rooms;
    }

    // int get_number_of_classrooms() { return (int) classrooms.size(); }
    public void setup_classrooms()
    {
      if (School.school_classroom_size == 0)
      {
        return;
      }

      for (int a = 0; a < Neighborhood_Patch.GRADES; ++a)
      {
        int n = this.students_in_grade[a];
        if (n == 0)
        {
          continue;
        }
        int rooms = n / School.school_classroom_size;
        if (n % School.school_classroom_size != 0)
        {
          rooms++;
        }

        Utils.FRED_STATUS(1, "School %d %s grade %d number %d rooms %d\n", this.get_id(), this.get_label(), a, n, rooms);

        for (int c = 0; c < rooms; ++c)
        {
          var new_label = $"{this.get_label()}-{a:D2}-{c + 1:D2}";
          var clsrm = new Classroom(new_label,
                     Place.SUBTYPE_NONE,
                     this.get_longitude(),
                     this.get_latitude());
          clsrm.set_school(this);
          this.classrooms[a].Add(clsrm);
        }
      }
    }

    public Place select_classroom_for_student(Person per)
    {
      if (School.school_classroom_size == 0)
      {
        return null;
      }
      int grade = per.get_age();
      if (Neighborhood_Patch.GRADES <= grade)
      {
        grade = Neighborhood_Patch.GRADES - 1;
      }
      if (Global.Verbose > 1)
      {
        Utils.FRED_STATUS(0, "assign classroom for student %d in school %d %s grade %d\n",
          per.get_id(), this.get_id(), this.get_label(), grade);
      }

      if (this.classrooms[grade].Count == 0)
      {
        return null;
      }

      // pick next classroom for this grade, round-robin
      int room = this.next_classroom[grade];
      if (room < (int)this.classrooms[grade].Count - 1)
      {
        this.next_classroom[grade]++;
      }
      else
      {
        this.next_classroom[grade] = 0;
      }

      // pick next classroom for this grade at random
      // int room = Random.draw_random_int(0,(classrooms[grade].size()-1));

      if (Global.Verbose > 1)
      {
        Utils.FRED_STATUS(0, "room = %d %s %d\n", room, this.classrooms[grade][room].get_label(),
          this.classrooms[grade][room].get_id());
      }
      return this.classrooms[grade][room];
    }

    public int get_number_of_students()
    {
      int n = 0;
      for (int grade = 1; grade < Neighborhood_Patch.GRADES; ++grade)
      {
        n += this.students_in_grade[grade];
      }
      return n;
    }

    public int get_orig_number_of_students()
    {
      int n = 0;
      for (int grade = 1; grade < Neighborhood_Patch.GRADES; ++grade)
      {
        n += this.orig_students_in_grade[grade];
      }
      return n;
    }

    public static int get_max_classroom_size()
    {
      return School.school_classroom_size;
    }

    public void set_county(int _county_index)
    {
      this.county_index = _county_index;
    }

    public int get_county()
    {
      return this.county_index;
    }

    public void set_income_quartile(int _income_quartile)
    {
      if (_income_quartile < Global.Q1 || _income_quartile > Global.Q4)
      {
        this.income_quartile = -1;
      }
      else
      {
        this.income_quartile = _income_quartile;
      }
    }

    public int get_income_quartile()
    {
      return this.income_quartile;
    }

    public void prepare_income_quartile_pop_size()
    {
      if (Global.Report_Childhood_Presenteeism)
      {
        int size = get_size();
        // update population stats based on income quartile of this school
        if (this.income_quartile == Global.Q1)
        {
          School.pop_income_Q1 += size;
        }
        else if (this.income_quartile == Global.Q2)
        {
          School.pop_income_Q2 += size;
        }
        else if (this.income_quartile == Global.Q3)
        {
          School.pop_income_Q3 += size;
        }
        else if (this.income_quartile == Global.Q4)
        {
          School.pop_income_Q4 += size;
        }
        School.total_school_pop += size;
      }
    }

    public static int get_total_school_pop()
    {
      return School.total_school_pop;
    }

    public static int get_school_pop_income_quartile_1()
    {
      return School.pop_income_Q1;
    }

    public static int get_school_pop_income_quartile_2()
    {
      return School.pop_income_Q2;
    }

    public static int get_school_pop_income_quartile_3()
    {
      return School.pop_income_Q3;
    }

    public static int get_school_pop_income_quartile_4()
    {
      return School.pop_income_Q4;
    }

    public static string get_school_closure_policy()
    {
      return School.school_closure_policy;
    }

    //for access from Classroom:
    public static double get_school_contacts_per_day(int disease_id)
    {
      return School.contacts_per_day;
    }
  }
}