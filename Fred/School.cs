namespace Fred
{
  public class School : Place
  {
    static double contacts_per_day;
    static double[,] prob_transmission_per_contact;
    static int school_classroom_size = 0;
    static string school_closure_policy = "undefined";
    static int school_closure_day = 0;
    static int min_school_closure_day = 0;
    static double school_closure_threshold = 0.0;
    static double individual_school_closure_threshold = 0.0;
    static int school_closure_cases = -1;
    static int school_closure_duration = 0;
    static int school_closure_delay = 0;
    static int school_summer_schedule = 0;
    static char school_summer_start[8];
    static char school_summer_end[8];
    static int summer_start_month = 0;
    static int summer_start_day = 0;
    static int summer_end_month = 0;
    static int summer_end_day = 0;
    static int total_school_pop = 0;
    static int pop_income_Q1 = 0;
    static int pop_income_Q2 = 0;
    static int pop_income_Q3 = 0;
    static int pop_income_Q4 = 0;
    static bool global_closure_is_active = false;
    static int global_close_date = 0;
    static int global_open_date = 0;

    public School()
    {
      this.set_type(Place::TYPE_SCHOOL);
      this.set_subtype(Place::SUBTYPE_NONE);
      this.intimacy = 0.025;
      for (int i = 0; i < GRADES; ++i)
      {
        this.students_in_grade[i] = 0;
        this.orig_students_in_grade[i] = 0;
        this.next_classroom[i] = 0;
        this.classrooms[i].clear();
      }
      this.closure_dates_have_been_set = false;
      this.staff_size = 0;
      this.max_grade = -1;
      this.county_index = -1;
      this.income_quartile = -1;
    }


    public School(string lab, char _subtype, fred::geo lon, fred::geo lat)
      : base (lab, lon, lat)
    {
      this.set_type(Place::TYPE_SCHOOL);
      this.set_subtype(_subtype);
      this.intimacy = 0.025;
      for (int i = 0; i < GRADES; ++i)
      {
        this.students_in_grade[i] = 0;
        this.orig_students_in_grade[i] = 0;
        this.next_classroom[i] = 0;
        this.classrooms[i].clear();
      }
      this.closure_dates_have_been_set = false;
      this.staff_size = 0;
      this.max_grade = -1;
      this.county_index = -1;
      this.income_quartile = -1;
    }

    public void prepare()
    {
      // call base class function to perform preparations common to all Places 
      base.prepare();
      // record original size in each grade
      for (int g = 0; g < GRADES; ++g)
      {
        this.orig_students_in_grade[g] = this.students_in_grade[g];
      }
    }


    void get_parameters()
    {

      Params::get_param_from_string("school_contacts", &contacts_per_day);
      int n = Params::get_param_matrix((string)"school_trans_per_contact", &prob_transmission_per_contact);
      if (Global.Verbose > 1)
      {
        printf("\nSchool_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", prob_transmission_per_contact[i][j]);
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

      if (Global.Verbose > 0)
      {
        printf("\nSchool_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", prob_transmission_per_contact[i][j]);
          }
          printf("\n");
        }
        printf("\ncontact rate: %f\n", contacts_per_day);
      }
      // end normalization

      Params::get_param_from_string("school_classroom_size", &school_classroom_size);

      // summer school parameters
      Params::get_param_from_string("school_summer_schedule", &school_summer_schedule);
      Params::get_param_from_string("school_summer_start", school_summer_start);
      Params::get_param_from_string("school_summer_end", school_summer_end);
      sscanf(school_summer_start, "%d-%d", &summer_start_month, &summer_start_day);
      sscanf(school_summer_end, "%d-%d", &summer_end_month, &summer_end_day);

      // school closure parameters
      Params::get_param_from_string("school_closure_policy", school_closure_policy);
      Params::get_param_from_string("school_closure_duration", &school_closure_duration);
      Params::get_param_from_string("school_closure_delay", &school_closure_delay);
      Params::get_param_from_string("school_closure_day", &school_closure_day);
      Params::get_param_from_string("min_school_closure_day", &min_school_closure_day);
      Params::get_param_from_string("school_closure_ar_threshold", &school_closure_threshold);
      Params::get_param_from_string("individual_school_closure_ar_threshold",
            &individual_school_closure_threshold);
      Params::get_param_from_string("school_closure_cases", &school_closure_cases);

      // aliases for parameters
      int Weeks;
      Params::get_param_from_string("Weeks", &Weeks);
      if (Weeks > -1)
      {
        school_closure_duration = 7 * Weeks;
      }

      int cases;
      Params::get_param_from_string("Cases", &cases);
      if (cases > -1)
      {
        school_closure_cases = cases;
      }
    }

    int get_group(int disease_id, Person* per)
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

    double get_transmission_prob(int disease_id, Person* i, Person* s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease_id, i);
      int col = get_group(disease_id, s);
      double tr_pr = prob_transmission_per_contact[row][col];
      return tr_pr;
    }

    void close(int day, int day_to_close, int duration)
    {
      this.close_date = day_to_close;
      this.open_date = close_date + duration;
      this.closure_dates_have_been_set = true;

      // log this school closure decision
      if (Global.Verbose > 0)
      {
        printf("SCHOOL %s CLOSURE decision day %d close_date %d duration %d open_date %d\n",
         this.get_label(), day, this.close_date, duration, this.open_date);
      }
    }


    bool is_open(int day)
    {
      bool open = (day < this.close_date || this.open_date <= day);
      if (!open)
      {
        FredUtils.Log(0, "Place %s is closed on day %d\n", this.get_label(), day);
      }
      return open;
    }

    bool should_be_open(int day, int disease_id)
    {
      // no students
      if (get_size() == 0)
      {
        return false;
      }

      // summer break
      if (school_summer_schedule > 0)
      {
        int month = Date::get_month();
        int day_of_month = Date::get_day_of_month();
        if ((month == summer_start_month && summer_start_day <= day_of_month) ||
           (month == summer_end_month && day_of_month <= summer_end_day) ||
           (summer_start_month < month && month < summer_end_month))
        {
          if (Global.Verbose > 1)
          {
            Console.WriteLine("School %s closed for summer\n", this.get_label());
            
          }
          return false;
        }
      }

      // stick to previously made decision to close
      if (this.closure_dates_have_been_set)
      {
        return is_open(day);
      }

      // global school closure policy in effect
      if (strcmp(school_closure_policy, "global") == 0)
      {
        apply_global_school_closure_policy(day, disease_id);
        return is_open(day);
      }

      // individual school closure policy in effect
      if (strcmp(school_closure_policy, "individual") == 0)
      {
        apply_individual_school_closure_policy(day, disease_id);
        return is_open(day);
      }

      // if school_closure_policy is not recognized, then open
      return true;
    }

    void apply_global_school_closure_policy(int day, int disease_id)
    {

      // Only test triggers for school closure if not global closure is not already activated
      if (global_closure_is_active == false)
      {
        // Setting school_closure_day > -1 overrides other global triggers.
        // Close schools if the closure date has arrived (after a delay)
        if (school_closure_day > -1)
        {
          if (school_closure_day <= day)
          {
            // the following only happens once
            global_close_date = day + school_closure_delay;
            global_open_date = day + school_closure_delay
              + school_closure_duration;
            global_closure_is_active = true;
          }
        }
        else
        {
          // Close schools if the global symptomatic attack rate has reached the threshold (after a delay)
          Disease* disease = Global.Diseases.get_disease(disease_id);
          if (school_closure_threshold <= disease.get_symptomatic_attack_rate())
          {
            // the following only happens once
            global_close_date = day + school_closure_delay;
            global_open_date = day + school_closure_delay
              + school_closure_duration;
            global_closure_is_active = true;
          }
        }
      }
      if (global_closure_is_active)
      {
        // set close and open dates for this school (only once)
        close(day, global_close_date, school_closure_duration);

        // log this school closure decision
        if (Global.Verbose > 0)
        {
          Disease* disease = Global.Diseases.get_disease(disease_id);
          printf("GLOBAL SCHOOL CLOSURE pop_ar %5.2f local_cases = %d / %d (%5.2f)\n",
           disease.get_symptomatic_attack_rate(), get_total_cases(disease_id),
           get_size(), get_symptomatic_attack_rate(disease_id));
        }
      }
    }

    void apply_individual_school_closure_policy(int day, int disease_id)
    {

      // don't apply any policy prior to min_school_closure_day
      if (day <= min_school_closure_day)
      {
        return;
      }

      // don't apply any policy before the epidemic reaches a noticeable threshold
      Disease* disease = Global.Diseases.get_disease(disease_id);
      if (disease.get_symptomatic_attack_rate() < school_closure_threshold)
      {
        return;
      }

      bool close_this_school = false;

      // if school_closure_cases > -1 then close if this number of cases occurs
      if (school_closure_cases != -1)
      {
        close_this_school = (school_closure_cases <= get_total_cases(disease_id));
      }
      else
      {
        // close if attack rate threshold is reached
        close_this_school = (individual_school_closure_threshold <= get_symptomatic_attack_rate(disease_id));
      }

      if (close_this_school)
      {
        // set close and open dates for this school (only once)
        close(day, day + school_closure_delay, school_closure_duration);

        // log this school closure decision
        if (Global.Verbose > 0)
        {
          Disease* disease = Global.Diseases.get_disease(disease_id);
          printf("LOCAL SCHOOL CLOSURE pop_ar %.3f local_cases = %d / %d (%.3f)\n",
           disease.get_symptomatic_attack_rate(), get_total_cases(disease_id),
           get_size(), get_symptomatic_attack_rate(disease_id));
        }
      }
    }

    double get_contacts_per_day(int disease_id)
    {
      return contacts_per_day;
    }

    int enroll(Person* person)
    {

      // call base class method:
      int return_value = Place::enroll(person);

      FredUtils.Log(1, "Enroll person %d age %d in school %d %s new size %d\n",
             person.get_id(), person.get_age(), this.get_id(), this.get_label(), this.get_size());
      if (person.is_teacher())
      {
        this.staff_size++;
      }
      else
      {
        int age = person.get_age();
        int grade = ((age < GRADES) ? age : GRADES - 1);
        assert(grade > 0);
        this.students_in_grade[grade]++;
        if (grade > this.max_grade)
        {
          this.max_grade = grade;
        }
        person.set_grade(grade);
      }

      return return_value;
    }

    void unenroll(int pos)
    {
      int size = this.enrollees.size();
      assert(0 <= pos && pos < size);
      Person* removed = this.enrollees[pos];
      int grade = removed.get_grade();
      FredUtils.Log(1, "UNENROLL person %d age %d grade %d, is_teacher %d from school %d %s Size = %d\n",
             removed.get_id(), removed.get_age(), grade, removed.is_teacher() ? 1 : 0, this.get_id(), this.get_label(), this.get_size());

      // call the base class method
      Place::unenroll(pos);

      if (removed.is_teacher() || grade == 0)
      {
        this.staff_size--;
      }
      else
      {
        assert(0 < grade && grade <= this.max_grade);
        this.students_in_grade[grade]--;
      }
      removed.set_grade(0);
      FredUtils.Log(1, "UNENROLLED from school %d %s size = %d\n", this.get_id(), this.get_label(), this.get_size());
    }

    void print(int disease_id)
    {
      Console.WriteLine("Place %d label %s type %c ", this.get_id(), this.get_label(), this.get_type());
      for (int g = 0; g < GRADES; ++g)
      {
        Console.WriteLine("%d students in grade %d | ", this.students_in_grade[g], g);
      }
      Console.WriteLine("\n");
      
    }

    int get_number_of_rooms()
    {
      int total_rooms = 0;
      if (school_classroom_size == 0)
      {
        return 0;
      }
      for (int a = 0; a < GRADES; ++a)
      {
        int n = this.students_in_grade[a];
        if (n == 0)
        {
          continue;
        }
        int rooms = n / school_classroom_size;
        if (n % school_classroom_size)
        {
          rooms++;
        }
        total_rooms += rooms;
      }
      return total_rooms;
    }

    void setup_classrooms(Allocator<Classroom> &classroom_allocator)
    {
      if (school_classroom_size == 0)
      {
        return;
      }

      for (int a = 0; a < GRADES; ++a)
      {
        int n = this.students_in_grade[a];
        if (n == 0)
        {
          continue;
        }
        int rooms = n / school_classroom_size;
        if (n % school_classroom_size)
        {
          rooms++;
        }

        FredUtils.Status(1, "School %d %s grade %d number %d rooms %d\n", this.get_id(), this.get_label(), a, n, rooms);

        for (int c = 0; c < rooms; ++c)
        {
          char new_label[128];
          sprintf(new_label, "%s-%02d-%02d", this.get_label(), a, c + 1);

          Classroom* clsrm = new (classroom_allocator.get_free()) Classroom(new_label,
                      Place::SUBTYPE_NONE,
                      this.get_longitude(),
                      this.get_latitude());
      clsrm.set_school(this);

      this.classrooms[a].push_back(clsrm);
    }
  }
}

public Place select_classroom_for_student(Person per)
{
  if (school_classroom_size == 0)
  {
    return null;
  }
  int grade = per.get_age();
  if (GRADES <= grade)
  {
    grade = GRADES - 1;
  }
  if (Global.Verbose > 1)
  {
    Console.WriteLine("assign classroom for student %d in school %d %s grade %d\n",
      per.get_id(), this.get_id(), this.get_label(), grade);
    
  }

  if (this.classrooms[grade].size() == 0)
  {
    return null;
  }

  // pick next classroom for this grade, round-robin
  int room = this.next_classroom[grade];
  if (room < (int)this.classrooms[grade].size() - 1)
  {
    this.next_classroom[grade]++;
  }
  else
  {
    this.next_classroom[grade] = 0;
  }

  // pick next classroom for this grade at random
  // int room = Random::draw_random_int(0,(classrooms[grade].size()-1));

  if (Global.Verbose > 1)
  {
    Console.WriteLine("room = %d %s %d\n", room, this.classrooms[grade][room].get_label(),
      this.classrooms[grade][room].get_id());
    
  }
  return this.classrooms[grade][room];
}

void print_size_distribution()
{
  for (int g = 1; g < GRADES; ++g)
  {
    if (this.orig_students_in_grade[g] > 0)
    {
      printf("SCHOOL %s grade %d orig %d current %d\n",
       this.get_label(), g, this.orig_students_in_grade[g], this.students_in_grade[g]);
    }
  }
  fflush(stdout);
}


int get_orig_students_in_grade(int grade)
    {
      if (grade < 0 || this.max_grade < grade)
      {
        return 0;
      }
      return this.orig_students_in_grade[grade];
    }

    int get_students_in_grade(int grade)
    {
      if (grade < 0 || this.max_grade < grade)
      {
        return 0;
      }
      return this.students_in_grade[grade];
    }

    int get_classrooms_in_grade(int grade)
    {
      if (grade < 0 || GRADES <= grade)
      {
        return 0;
      }
      return static_cast<int>(this.classrooms[grade].size());
    }
    int get_number_of_students()
    {
      int n = 0;
      for (int grade = 1; grade < GRADES; ++grade)
      {
        n += this.students_in_grade[grade];
      }
      return n;
    }

    int get_orig_number_of_students() const {
    int n = 0;
    for(int grade = 1; grade<GRADES; ++grade) {
      n += this.orig_students_in_grade[grade];
    }
    return n;
  }

static int get_max_classroom_size()
{
  return school_classroom_size;
}

void set_county(int _county_index)
{
  this.county_index = _county_index;
}

int get_county()
{
  return this.county_index;
}

void set_income_quartile(int _income_quartile)
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

int get_income_quartile() const {
    return this.income_quartile;
  }

void prepare_income_quartile_pop_size()
{
  if (Global.Report_Childhood_Presenteeism)
  {
    int size = get_size();
    // update population stats based on income quartile of this school
    if (this.income_quartile == Global.Q1)
    {
      pop_income_Q1 += size;
    }
    else if (this.income_quartile == Global.Q2)
    {
      pop_income_Q2 += size;
    }
    else if (this.income_quartile == Global.Q3)
    {
      pop_income_Q3 += size;
    }
    else if (this.income_quartile == Global.Q4)
    {
      pop_income_Q4 += size;
    }
    total_school_pop += size;
  }
}

static int get_total_school_pop()
{
  return total_school_pop;
}

static int get_school_pop_income_quartile_1()
{
  return pop_income_Q1;
}

static int get_school_pop_income_quartile_2()
{
  return pop_income_Q2;
}

static int get_school_pop_income_quartile_3()
{
  return pop_income_Q3;
}

static int get_school_pop_income_quartile_4()
{
  return pop_income_Q4;
}

static string get_school_closure_policy()
{
  return school_closure_policy;
}

//for access from Classroom:
static double get_school_contacts_per_day(int disease_id)
{
  return contacts_per_day;
}
  }
}