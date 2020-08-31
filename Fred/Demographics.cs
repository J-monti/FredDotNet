using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fred
{
  public class Demographics
  {
    public const int MAX_AGE = 110;
    public const int MIN_PREGNANCY_AGE = 12;
    public const int MAX_PREGNANCY_AGE = 60;
    public double MEAN_PREG_DAYS = 280.0; //40 weeks
    public double STDDEV_PREG_DAYS = 7.0; //1 week

    private static Events conception_queue;
    private static Events maternity_queue;
    private static Events mortality_queue;

    private int init_age;          // Initial age of the agent
    private int age;           // Current age of the agent
    private int number_of_children;     // number of births
    private int relationship; // relationship to the householder (see Global.h)
    private int race;       // see Global.h for race codes
    private char sex;         // Male or female?
    private bool pregnant;             // is the agent pregnant?
    private bool deceased;        // Is the agent deceased

    // all sim_day values assume simulation starts on day 0
    private int birthday_sim_day;     // agent's birthday in simulation time
    private int deceased_sim_day;      // When the agent (will die) / (died)
    private int conception_sim_day;   // When the agent will become pregnant
    private int maternity_sim_day;         // When the agent will give birth

    private static int births_today;
    private static int births_ytd;
    private static int total_births;
    private static int deaths_today;
    private static int deaths_ytd;
    private static int total_deaths;

    private static List<Person>[] birthday_vecs = new List<Person>[367]; //0 won't be used | day 1 - 366
    private static Dictionary<Person, int> birthday_map = new Dictionary<Person, int>();

    private static List<int> fips_codes;

    static Demographics()
    {
      for (int i = 0; i < 367; ++i)
      {
        birthday_vecs[i] = new List<Person>();
      }
    }

    // default constructor and destructor
    public Demographics()
    {
      this.init_age = -1;
      this.age = -1;
      this.sex = 'n';
      this.birthday_sim_day = -1;
      this.deceased_sim_day = -1;
      this.conception_sim_day = -1;
      this.maternity_sim_day = -1;
      this.pregnant = false;
      this.deceased = false;
      this.relationship = -1;
      this.race = -1;
      this.number_of_children = -1;
    }

    /**
     * setup for two-phase construction; sets all of the attributes of a Demographics object
     * @param self pointer to the Person object with which this Demographics object is associated
     * @param _age
     * @param _sex (M or F)
     * @param day the simulation day
     * @param is_newborn needed to know how to set the date of birth
     */
    internal void setup(Person self, int _age, char _sex, int _race, int rel, int day, bool is_newborn = false)
    {
      //int self_index = self.get_pop_index();
      // adjust age for those over 89 (due to binning in the synthetic pop)
      if (_age > 89)
      {
        _age = 90;
        while (this.age < MAX_AGE && FredRandom.NextDouble() < 0.6)
        {
          _age++;
        }
      }

      // set demographic variables
      this.init_age = _age;
      this.age = this.init_age;
      this.sex = _sex;
      this.race = _race;
      this.relationship = rel;
      this.deceased_sim_day = -1;
      this.conception_sim_day = -1;
      this.maternity_sim_day = -1;
      this.pregnant = false;
      this.deceased = false;
      this.number_of_children = 0;

      if (is_newborn)
      {
        // today is birthday
        this.birthday_sim_day = day;
      }
      else
      {
        // set the agent's birthday relative to simulation day
        this.birthday_sim_day = day - 365 * this.age;
        // adjust for leap years:
        this.birthday_sim_day -= (this.age / 4);
        // pick a random birthday in the previous year
        this.birthday_sim_day -= FredRandom.Next(1, 365);
      }
    }

    public void initialize_demographic_dynamics(Person self)
    {

      Utils.FRED_VERBOSE(1, "demographic dynamics: id = {0} age = {1}\n", self.get_id(), this.age);

      int day = 0;

      // add self to birthday list
      add_to_birthday_list(self);

      // will this person die in the next year?
      double age_specific_probability_of_death = 0.0;
      if (MAX_AGE <= this.age)
      {
        age_specific_probability_of_death = 1.0;
        Utils.FRED_STATUS(1, "DAY {0} DEATH BY MAX_AGE RULE\n", day);
      }
      else
      {
        // look up mortality in the mortality rate tables
        int county_index_M = self.get_household().get_county_index();
        int fips = Global.Places.get_fips_of_county_with_index(county_index_M);
        age_specific_probability_of_death = Global.Places.get_county_with_index(county_index_M).get_mortality_rate(this.age, this.sex);
      }

      if (FredRandom.NextDouble() <= age_specific_probability_of_death)
      {
        //Yes, so set the death day (in simulation days)
        this.deceased_sim_day = (day + FredRandom.Next(1, 364));
        add_mortality_event(this.deceased_sim_day, self);
        Utils.FRED_STATUS(1, "MORTALITY EVENT ADDDED today {0} id {1] age {2} decease {3}\n",
          day, self.get_id(), age, deceased_sim_day);
      }

      // set pregnancy status
      int county_index = self.get_household().get_county_index();
      double pregnancy_rate = Global.Places.get_county_with_index(county_index).get_pregnancy_rate(this.age);
      if (this.sex == 'F' &&
         MIN_PREGNANCY_AGE <= this.age &&
         this.age <= MAX_PREGNANCY_AGE &&
         self.lives_in_group_quarters() == false &&
         FredRandom.NextDouble() < pregnancy_rate)
      {

        // decide if already pregnant
        int current_day_of_year = Date.get_day_of_year(day);
        int days_since_birthday = current_day_of_year - Date.get_day_of_year(this.birthday_sim_day);
        if (days_since_birthday < 0)
        {
          days_since_birthday += 365;
        }
        double fraction_of_year = days_since_birthday / 366.0;

        if (FredRandom.NextDouble() < fraction_of_year)
        {
          // already pregnant
          this.conception_sim_day = day - FredRandom.Next(0, days_since_birthday);
          int length_of_pregnancy = (int)(FredRandom.Normal(MEAN_PREG_DAYS, STDDEV_PREG_DAYS) + 0.5);
          this.maternity_sim_day = this.conception_sim_day + length_of_pregnancy;
          if (this.maternity_sim_day > 0)
          {
            add_maternity_event(maternity_sim_day, self);
            Utils.FRED_STATUS(1, "MATERNITY EVENT ADDDED today {0} id {1} age {2} due {3}\n",
                        day, self.get_id(), age, maternity_sim_day);
            this.pregnant = true;
            this.conception_sim_day = -1;
          }
          else
          {
            // already gave birth before start of sim
            this.pregnant = false;
            this.conception_sim_day = -1;
          }
        }
        else
        {
          // will conceive before next birthday
          this.conception_sim_day = day + FredRandom.Next(1, 365 - days_since_birthday);
          add_conception_event(this.conception_sim_day, self);
          Utils.FRED_STATUS(1, "CONCEPTION EVENT ADDDED today {0} id {1} age {2} conceive {3} house {4}\n",
            day, self.get_id(), age, conception_sim_day, self.get_household().get_label());
        }
      } // end test for pregnancy
    }

    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     *
     */
    public void update(int day)
    {
      // reset counts of births and deaths
      births_today = 0;
      deaths_today = 0;

      update_people_on_birthday_list(day);

      // initiate pregnancies
      // FRED_VERBOSE(0, "conception queue\n");
      int size = conception_queue.get_size(day);
      for (int i = 0; i < size; ++i)
      {
        var person = conception_queue.get_event(day, i);
        person.get_demographics().become_pregnant(day, person);
      }
      conception_queue.clear_events(day);

      // add newborns to the population
      // FRED_VERBOSE(0, "maternity queue\n");
      size = maternity_queue.get_size(day);
      for (int i = 0; i < size; ++i)
      {
        var person = maternity_queue.get_event(day, i);
        person.give_birth(day);
      }

      maternity_queue.clear_events(day);
      // remove dead from population
      // FRED_VERBOSE(0, "mortality queue\n");
      size = mortality_queue.get_size(day);
      for (int i = 0; i < size; ++i)
      {
        var person = mortality_queue.get_event(day, i);
        Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
               "HEALTH CHART: {0} person {1} age {2} DIES FROM UNKNOWN CAUSE.\n",
               Date.get_date_string(),
               person.get_id(), person.get_age());
        // queue removal from population
        Global.Pop.prepare_to_die(day, person);
      }
      mortality_queue.clear_events(day);
    }

    /**
     * @return the number of days the agent has been alive / 365.25
     */
    public double get_real_age()
    {
      return (Global.Simulation_Day - this.birthday_sim_day) / 365.25;
    }

    /**
     * @return the agent's age
     */
    public int get_age()
    {
      return this.age;
    }

    /**
     * @return the agent's sex
     */
    public char get_sex()
    {
      return this.sex;
    }

    /**
     * @return <code>true</code> if the agent is pregnant, <code>false</code> otherwise
     */
    public bool is_pregnant()
    {
      return this.pregnant;
    }

    public void set_pregnant()
    {
      this.pregnant = true;
    }

    public void unset_pregnant()
    {
      this.pregnant = false;
    }

    /**
     * @return <code>true</code> if the agent is deceased, <code>false</code> otherwise
     */
    public bool is_deceased()
    {
      return this.deceased;
    }

    /**
     * Print out information about this object
     */
    public void print() { }

    /**
     * @return the agent's init_age
     */
    public int get_init_age()
    {
      return this.init_age;
    }

    /**
     * @return the agent's race
     */
    public int get_race()
    {
      return this.race;
    }

    public void set_relationship(int rel)
    {
      this.relationship = rel;
    }

    public int get_relationship()
    {
      return this.relationship;
    }

    /**
     * @return <code>true</code> if the agent is a householder, <code>false</code> otherwise
     */
    public bool is_householder()
    {
      return this.relationship == Global.HOUSEHOLDER;
    }

    public void make_householder()
    {
      this.relationship = Global.HOUSEHOLDER;
    }

    public int get_day_of_year_for_birthday_in_nonleap_year()
    {
      int day_of_year = Date.get_day_of_year(this.birthday_sim_day);
      int year = Date.get_year(this.birthday_sim_day);
      if (Date.is_leap_year(year) && 59 < day_of_year)
      {
        day_of_year--;
      }

      return day_of_year;
    }

    /**
     * Perform the necessary changes to the demographics on an agent's birthday
     */
    public void birthday(Person self, int day)
    {
      if (!Global.Enable_Population_Dynamics)
      {
        return;
      }

      Utils.FRED_STATUS(2, "Birthday entered for person {0} with (previous) age {1}\n", self.get_id(), self.get_age());

      int county_index = self.get_household().get_county_index();
      //The count of agents at the current age is decreased by 1
      Global.Places.decrement_population_of_county_with_index(county_index, self);
      // change age
      this.age++;
      //The count of agents at the new age is increased by 1
      Global.Places.increment_population_of_county_with_index(county_index, self);

      // will this person die in the next year?
      double age_specific_probability_of_death = 0.0;
      if (MAX_AGE <= this.age)
      {
        age_specific_probability_of_death = 1.0;
        // printf("DAY %d DEATH BY MAX_AGE RULE\n", day);
        /*
          } else if (self.is_nursing_home_resident()) {
          age_specific_probability_of_death = 0.25;
          }
        */
      }
      else
      {
        // look up mortality in the mortality rate tables
        age_specific_probability_of_death = Global.Places.get_county_with_index(county_index).get_mortality_rate(this.age, this.sex);
      }
      if (this.deceased == false && this.deceased_sim_day == -1 &&
           FredRandom.NextDouble() <= age_specific_probability_of_death)
      {

        // Yes, so set the death day (in simulation days)
        this.deceased_sim_day = (day + FredRandom.Next(0, 364));
        add_mortality_event(deceased_sim_day, self);
        Utils.FRED_STATUS(1, "MORTALITY EVENT ADDDED today {0} id {1} age {2} decease {3}\n", day, self.get_id(), age, deceased_sim_day);
      }
      else
      {
        Utils.FRED_STATUS(2, "SURVIVER: AGE {0} deceased_sim_day = {1}\n", age, this.deceased_sim_day);
      }

      // Will this person conceive in the coming year year?
      double pregnancy_rate = Global.Places.get_county_with_index(county_index).get_pregnancy_rate(this.age);
      if (this.sex == 'F' &&
         MIN_PREGNANCY_AGE <= this.age &&
         this.age <= MAX_PREGNANCY_AGE &&
         this.conception_sim_day == -1 && this.maternity_sim_day == -1 &&
         self.lives_in_group_quarters() == false &&
         FredRandom.NextDouble() < pregnancy_rate)
      {

        Utils.assert(this.pregnant == false);

        // ignore small distortion due to leap years
        this.conception_sim_day = day + FredRandom.Next(1, 365);
        add_conception_event(this.conception_sim_day, self);
        Utils.FRED_STATUS(1, "CONCEPTION EVENT ADDDED today {0} id {1} age {2} conceive {3} house {4}\n",
        day, self.get_id(), age, conception_sim_day, self.get_household().get_label());
      }

      // become responsible for health decisions when reaching adulthood
      if (this.age == Global.ADULT_AGE && self.is_health_decision_maker() == false)
      {
        Utils.FRED_STATUS(2, "Become_health_decision_maker\n");
        self.become_health_decision_maker(self);
      }

      // update any state-base health conditions
      self.update_health_conditions(day);

      Utils.FRED_STATUS(2, "Birthday finished for person {0} with new age {1}\n", self.get_id(), self.get_age());
    }

    public void terminate(Person self)
    {
      int day = Global.Simulation_Day;
      Utils.FRED_STATUS(1, "Demographics.terminate day {0} person {1} age {2}\n",
            day, self.get_id(), this.age);

      // cancel any planned pregnancy
      if (day <= this.conception_sim_day)
      {
        Utils.FRED_STATUS(0, "DEATH CANCELS PLANNED CONCEPTION: today {0} person {1} age {2} conception {3}\n",
                    day, self.get_id(), this.age, this.conception_sim_day);
        cancel_conception(self);
      }

      // cancel any current pregnancy
      if (this.pregnant)
      {
        Utils.FRED_STATUS(0, "DEATH CANCELS PREGNANCY: today {0} person {1} age {2} due {3}\n",
                    day, self.get_id(), this.age, this.maternity_sim_day);
        cancel_pregnancy(self);
      }

      // remove from the birthday lists
      if (Global.Enable_Population_Dynamics)
      {
        Demographics.delete_from_birthday_list(self);
      }

      // cancel any future mortality event
      if (day < this.deceased_sim_day)
      {
        Utils.FRED_STATUS(0, "DEATH CANCELS FUTURE MORTALITY: today {0} person {1} age {2} mortality {3}\n",
                    day, self.get_id(), this.age, this.deceased_sim_day);
        cancel_mortality(self);
      }

      // update death stats
      Demographics.deaths_today++;
      Demographics.deaths_ytd++;
      Demographics.total_deaths++;

      if (Global.Deathfp != null)
      {
        // report deaths
        Global.Deathfp.WriteLine("day {0} person {1} age {2}",
          day, self.get_id(), self.get_age());
        Global.Deathfp.Flush();
      }
      // self.die();
      this.deceased = true;
    }

    public void set_number_of_children(int n)
    {
      this.number_of_children = n;
    }

    public int get_number_of_children()
    {
      return this.number_of_children;
    }

    public int get_conception_sim_day()
    {
      return this.conception_sim_day;
    }

    public void set_conception_sim_day(int day)
    {
      this.conception_sim_day = day;
    }

    public int get_maternity_sim_day()
    {
      return this.maternity_sim_day;
    }

    public void set_maternity_sim_day(int day)
    {
      this.maternity_sim_day = day;
    }

    public static int get_births_today()
    {
      return births_today;
    }

    public static int get_births_ytd()
    {
      return births_ytd;
    }

    public static int get_total_births()
    {
      return total_births;
    }

    public static int get_deaths_today()
    {
      return deaths_today;
    }

    public static int get_deaths_ytd()
    {
      return deaths_ytd;
    }

    public static int get_total_deaths()
    {
      return total_deaths;
    }

    // event handlers:
    public void cancel_conception(Person self)
    {
      Utils.assert(this.conception_sim_day > -1);
      delete_conception_event(this.conception_sim_day, self);
      Utils.FRED_STATUS(0, "CONCEPTION EVENT DELETED\n");
      this.conception_sim_day = -1;
    }

    public void become_pregnant(int day, Person self)
    {
      // No pregnancies in group quarters
      if (self.lives_in_group_quarters())
      {
        Utils.FRED_STATUS(0, "GQ PREVENTS PREGNANCY today {0} id {1} age {2}\n",
          day, self.get_id(), self.get_age());
        this.conception_sim_day = -1;
        return;
      }
      int length_of_pregnancy = (int)(FredRandom.Normal(MEAN_PREG_DAYS, STDDEV_PREG_DAYS) + 0.5);
      this.maternity_sim_day = this.conception_sim_day + length_of_pregnancy;
      add_maternity_event(maternity_sim_day, self);
      Utils.FRED_STATUS(1, "MATERNITY EVENT ADDDED today {0} id {1} age {2} due {3}\n",
                  day, self.get_id(), age, maternity_sim_day);
      this.pregnant = true;
      this.conception_sim_day = -1;
    }

    public void cancel_pregnancy(Person self)
    {
      Utils.assert(this.pregnant == true);
      delete_maternity_event(maternity_sim_day, self);
      Utils.FRED_STATUS(0, "MATERNITY EVENT DELETED\n");
      this.maternity_sim_day = -1;
      this.pregnant = false;
    }

    public void update_birth_stats(int day, Person self)
    {
      // NOTE: This is called by Person.give_birth() to update stats.
      // The baby is actually created in Person.give_birth()
      this.pregnant = false;
      this.maternity_sim_day = -1;
      this.number_of_children++;
      births_today++;
      births_ytd++;
      total_births++;

      if (Global.Report_County_Demographic_Information)
      {
        var hh = self.get_household();
        int index = -1;
        if (hh != null)
        {
          index = hh.get_county_index();
        }
        else if (Global.Enable_Hospitals && self.is_hospitalized())
        {
          hh = self.get_permanent_household();
          if (hh != null)
          {
            index = hh.get_county_index();
          }
        }
        else if (Global.Enable_Travel)
        {
          hh = self.get_permanent_household();
          if (hh != null)
          {
            index = hh.get_county_index();
          }
        }

        Utils.assert(index != -1);
      }

      if (Global.Birthfp != null)
      {
        // report births
        Global.Birthfp.WriteLine("day {0} mother {1} age {2}\n", day, self.get_id(), self.get_age());
        Global.Birthfp.Flush();
      }
    }

    public void cancel_mortality(Person self)
    {
      Utils.assert(this.deceased_sim_day > -1);
      delete_mortality_event(this.deceased_sim_day, self);
      Utils.FRED_STATUS(0, "MORTALITY EVENT DELETED\n");
      this.deceased_sim_day = -1;
    }

    // birthday lists
    public static void add_to_birthday_list(Person person)
    {
      int day_of_year = person.get_demographics().get_day_of_year_for_birthday_in_nonleap_year();
      birthday_vecs[day_of_year].Add(person);
      Utils.FRED_VERBOSE(2,
             "Adding person {0} to birthday vector for day = {1}.\n ... birthday_vecs[ {2} ].size() = {3}\n",
             person.get_id(), day_of_year, day_of_year, birthday_vecs[day_of_year].Count);
      birthday_map[person] = birthday_vecs[day_of_year].Count - 1;
    }

    public static void delete_from_birthday_list(Person person)
    {
      int day_of_year = person.get_demographics().get_day_of_year_for_birthday_in_nonleap_year();

      Utils.FRED_VERBOSE(2,
             "deleting person {0} to birthday vector for day = {1}.\n ... birthday_vecs[ {2} ].size() = {3}\n",
             person.get_id(), day_of_year, day_of_year, birthday_vecs[day_of_year].Count);

      if (!birthday_map.ContainsKey(person))
      {
        Utils.FRED_VERBOSE(0, "Help! person {0} deleted, but not in the birthday_map\n", person.get_id());
      }
      Utils.assert(birthday_map.ContainsKey(person));
      int pos = birthday_map[person];

      // copy last person in birthday list into this person's slot
      var last = birthday_vecs[day_of_year].Last();
      birthday_vecs[day_of_year][pos] = last;
      birthday_map[last] = pos;

      // delete last slot
      birthday_vecs[day_of_year].PopBack();

      // delete from birthday map
      birthday_map.Remove(person);

      Utils.FRED_VERBOSE(2,
             "deleted person {0} to birthday vector for day = {1}.\n ... birthday_vecs[ {2} ].size() = {3}\n",
             person.get_id(), day_of_year, day_of_year, birthday_vecs[day_of_year].Count);
    }

    public static void update_people_on_birthday_list(int day)
    {
      int birthday_index = Date.get_day_of_year();
      if (Date.is_leap_year())
      {
        // skip feb 29
        if (birthday_index == 60)
        {
          Utils.FRED_VERBOSE(1, "LEAP DAY day {0} index {1}\n", day, birthday_index);
          return;
        }
        // shift all days after feb 28 forward
        if (60 < birthday_index)
        {
          birthday_index--;
        }
      }

      int size = birthday_vecs[birthday_index].Count;
      Utils.FRED_VERBOSE(1, "day {0} update_people_on_birthday_list started. size = {1}\n",
             day, size);

      // make a temporary list of birthday people
      var birthday_list = new List<Person>();
      for (int p = 0; p < size; ++p)
      {
        var person = birthday_vecs[birthday_index][p];
        birthday_list.Add(person);
      }

      // update everyone on birthday list
      int birthday_count = 0;
      for (int p = 0; p < size; ++p)
      {
        var person = birthday_list[p];
        person.birthday(day);
        birthday_count++;
      }

      size = birthday_vecs[birthday_index].Count;

      Utils.FRED_VERBOSE(1, "day {0} update_people_on_birthday_list finished. size = {1}\n", day, size);
    }

    public static void add_conception_event(int day, Person person)
    {
      conception_queue.add_event(day, person);
    }

    public static void delete_conception_event(int day, Person person)
    {
      conception_queue.delete_event(day, person);
    }

    public static void add_maternity_event(int day, Person person)
    {
      maternity_queue.add_event(day, person);
    }

    public static void delete_maternity_event(int day, Person person)
    {
      maternity_queue.delete_event(day, person);
    }

    public static void add_mortality_event(int day, Person person)
    {
      mortality_queue.add_event(day, person);
    }

    public static void delete_mortality_event(int day, Person person)
    {
      mortality_queue.delete_event(day, person);
    }

    public static void report(int day)
    {
      // get the current year
      int year = Date.get_year();
      using var fp = new StreamWriter($"{Global.Simulation_directory}/ages-{year}.txt");
      int n0, n5, n18, n65;
      var count = new int[20];
      int total = 0;
      n0 = n5 = n18 = n65 = 0;
      // age distribution
      for (int c = 0; c < 20; ++c)
      {
        count[c] = 0;
      }
      int current_popsize = Global.Pop.get_pop_size();
      for (int p = 0; p < Global.Pop.get_index_size(); ++p)
      {
        var person = Global.Pop.get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        int a = person.get_age();
        if (a < 5)
        {
          n0++;
        }
        else if (a < 18)
        {
          n5++;
        }
        else if (a < 65)
        {
          n18++;
        }
        else
        {
          n65++;
        }
        int n = a / 5;
        if (n < 20)
        {
          count[n]++;
        }
        else
        {
          count[19]++;
        }
        total++;
      }
      // fprintf(fp, "\nAge distribution: %d people\n", total);
      for (int c = 0; c < 20; ++c)
      {
        fp.WriteLine("age {0.##} to {1}: {2.######} {3} {4}", 5 * c, 5 * (c + 1) - 1, count[c],
          (1.0 * count[c]) / total, total);
      }
      /*
        fprintf(fp, "AGE 0-4: %d %.2f%%\n", n0, (100.0 * n0) / total);
        fprintf(fp, "AGE 5-17: %d %.2f%%\n", n5, (100.0 * n5) / total);
        fprintf(fp, "AGE 18-64: %d %.2f%%\n", n18, (100.0 * n18) / total);
        fprintf(fp, "AGE 65-100: %d %.2f%%\n", n65, (100.0 * n65) / total);
        fprintf(fp, "\n");
      */
      fp.Flush();
      fp.Dispose();
    }

    public static int find_fips_code(int n)
    {
      int size = Demographics.fips_codes.Count;
      for (int i = 0; i < size; ++i)
      {
        if (Demographics.fips_codes[i] == n)
        {
          return i;
        }
      }
      return -1;
    }
  }
}
