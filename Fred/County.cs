using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fred
{
  public class County
  {
    private int fips;
    private int tot_current_popsize;
    private int[] male_popsize = new int[Demographics.MAX_AGE + 1];
    private int tot_male_popsize;
    private int[] female_popsize = new int[Demographics.MAX_AGE + 1];
    private int tot_female_popsize;
    private int target_popsize;
    private double mortality_rate_adjustment_weight;
    private double[] male_mortality_rate = new double[Demographics.MAX_AGE + 1];
    private double[] female_mortality_rate = new double[Demographics.MAX_AGE + 1];
    private double[] adjusted_male_mortality_rate = new double[Demographics.MAX_AGE + 1];
    private double[] adjusted_female_mortality_rate = new double[Demographics.MAX_AGE + 1];
    private double[] birth_rate = new double[Demographics.MAX_AGE + 1];
    private double[] adjusted_birth_rate = new double[Demographics.MAX_AGE + 1];
    private double[] pregnancy_rate = new double[Demographics.MAX_AGE + 1];
    private bool is_initialized;
    private double population_growth_rate;
    private double college_departure_rate;
    private double military_departure_rate;
    private double prison_departure_rate;
    private double youth_home_departure_rate;
    private double adult_home_departure_rate;
    private int[] beds;
    private int[] occupants;
    private int max_beds;
    private int max_occupants;
    private List<Tuple<Person, int>> ready_to_move;

    // pointers to households
    private List<Household> households;
    private int houses;

    public County(int _fips)
    {
      this.is_initialized = false;
      this.fips = _fips;
      this.tot_current_popsize = 0;
      this.tot_female_popsize = 0;
      this.tot_male_popsize = 0;
      this.target_popsize = 0;
      this.population_growth_rate = 0.0;
      this.college_departure_rate = 0.0;
      this.military_departure_rate = 0.0;
      this.prison_departure_rate = 0.0;
      this.youth_home_departure_rate = 0.0;
      this.adult_home_departure_rate = 0.0;
      this.houses = 0;
      this.beds = null;
      this.occupants = null;
      this.max_beds = -1;
      this.max_occupants = -1;
      this.ready_to_move = new List<Tuple<Person, int>>();
      this.households = new List<Household>();
      this.mortality_rate_adjustment_weight = 0.0;

      for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      {
        this.female_popsize[i] = 0;
        this.male_popsize[i] = 0;
      }

      if (Global.Enable_Population_Dynamics == false)
      {
        return;
      }

      string mortality_rate_file = string.Empty;
      string birth_rate_file = string.Empty;
      double birth_rate_multiplier = 0.0;
      double mortality_rate_multiplier = 0.0;

      FredParameters.GetParameter("population_growth_rate", ref this.population_growth_rate);
      FredParameters.GetParameter("birth_rate_file", ref birth_rate_file);
      FredParameters.GetParameter("mortality_rate_file", ref mortality_rate_file);
      FredParameters.GetParameter("birth_rate_multiplier", ref birth_rate_multiplier);
      FredParameters.GetParameter("mortality_rate_multiplier", ref mortality_rate_multiplier);
      FredParameters.GetParameter("mortality_rate_adjustment_weight", ref this.mortality_rate_adjustment_weight);
      FredParameters.GetParameter("college_departure_rate", ref this.college_departure_rate);
      FredParameters.GetParameter("military_departure_rate", ref this.military_departure_rate);
      FredParameters.GetParameter("prison_departure_rate", ref this.prison_departure_rate);
      FredParameters.GetParameter("youth_home_departure_rate", ref this.youth_home_departure_rate);
      FredParameters.GetParameter("adult_home_departure_rate", ref this.adult_home_departure_rate);

      // initialize the birth rate array
      for (int j = 0; j <= Demographics.MAX_AGE; ++j)
      {
        this.birth_rate[j] = 0.0;
      }

      // read the birth rates
      if (!File.Exists(birth_rate_file))
      {
        Utils.FRED_STATUS(0, "County birth_rate_file {0} not found.", birth_rate_file);
        Utils.fred_abort("County birth_rate_file {0} not found.", birth_rate_file);
      }

      using var fp = new StreamReader(birth_rate_file);
      int age;
      double rate;
      while (fp.Peek() != -1)
      {
        string line = fp.ReadLine();
        var data = line.Split(' ');
        if (data.Length == 2)
        {
          age = Convert.ToInt32(data[0]);
          rate = Convert.ToDouble(data[1]);
          if (age >= 0 && age <= Demographics.MAX_AGE)
          {
            this.birth_rate[age] = birth_rate_multiplier * rate;
          }
        }
      }
      fp.Dispose();

      // set up pregnancy rates
      for (int i = 0; i < Demographics.MAX_AGE; ++i)
      {
        // approx 3/4 of pregnancies deliver at the next age of the mother
        this.pregnancy_rate[i] = 0.25 * this.birth_rate[i] + 0.75 * this.birth_rate[i + 1];
      }
      this.pregnancy_rate[Demographics.MAX_AGE] = 0.0;

      if (Global.Verbose > 0)
      {
        for (int i = 0; i <= Demographics.MAX_AGE; ++i)
        {
          Utils.FRED_STATUS(0, "BIRTH RATE     for age {0} {1}", i, this.birth_rate[i]);
          Utils.FRED_STATUS(0, "PREGNANCY RATE for age {0} {1}", i, this.pregnancy_rate[i]);
        }
      }

      // read mortality rate file
      if (!File.Exists(mortality_rate_file))
      {
        Utils.FRED_STATUS(0, "County mortality_rate %s not found", mortality_rate_file);
        Utils.fred_abort("County mortality_rate %s not found", mortality_rate_file);
      }
      using var fpMortality = new StreamReader(mortality_rate_file);
      //for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      //{
      double female_rate;
      double male_rate;
      while (fpMortality.Peek() != -1)
      {
        string line = fpMortality.ReadLine();
        var data = line.Split(' ');
        if (data.Length != 3)
        {
          Utils.fred_abort("Help! Read failure for mortality rate");
        }

        age = Convert.ToInt32(data[0]);
        female_rate = Convert.ToDouble(data[1]);
        male_rate = Convert.ToDouble(data[2]);
        if (Global.Verbose > 0)
        {
          Utils.FRED_STATUS(0, "MORTALITY RATE for age {0}: female: {1} male: {2}", age, female_rate, male_rate);
        }
        this.female_mortality_rate[age] = mortality_rate_multiplier * female_rate;
        this.male_mortality_rate[age] = mortality_rate_multiplier * male_rate;
      }
      fpMortality.Dispose();

      for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      {
        this.adjusted_female_mortality_rate[i] = this.female_mortality_rate[i];
        this.adjusted_male_mortality_rate[i] = this.male_mortality_rate[i];
      }
    }

    public int get_fips()
    {
      return this.fips;
    }

    public int get_tot_current_popsize()
    {
      return this.tot_current_popsize;
    }

    public int get_tot_female_popsize()
    {
      return this.tot_female_popsize;
    }

    public int get_tot_male_popsize()
    {
      return this.tot_male_popsize;
    }

    public int get_current_popsize(int age)
    {
      if (age > Demographics.MAX_AGE)
      {
        age = Demographics.MAX_AGE;
      }
      if (age >= 0)
      {
        return this.female_popsize[age] + this.male_popsize[age];
      }
      return -1;
    }

    public int get_current_popsize(int age, char sex)
    {
      if (age > Demographics.MAX_AGE)
      {
        age = Demographics.MAX_AGE;
      }
      if (age >= 0)
      {
        if (sex == 'F')
        {
          return this.female_popsize[age];
        }
        else if (sex == 'M')
        {
          return this.male_popsize[age];
        }
      }
      return -1;
    }

    public int get_current_popsize(int age_min, int age_max, char sex)
    {
      if (age_min < 0)
      {
        age_min = 0;
      }
      if (age_max > Demographics.MAX_AGE)
      {
        age_max = Demographics.MAX_AGE;
      }
      if (age_min > age_max)
      {
        age_min = 0;
      }
      if (age_min >= 0 && age_max >= 0 && age_min <= age_max)
      {
        if (sex == 'F' || sex == 'M')
        {
          int temp_count = 0;
          for (int i = age_min; i <= age_max; ++i)
          {
            temp_count += (sex == 'F' ? this.female_popsize[i] : this.male_popsize[i]);
          }
          return temp_count;
        }
      }
      return -1;
    }

    public bool increment_popsize(Person person)
    {
      int age = person.get_age();
      char sex = person.get_sex();
      if (age > Demographics.MAX_AGE)
      {
        age = Demographics.MAX_AGE;
      }
      if (age >= 0)
      {
        if (sex == 'F')
        {
          this.female_popsize[age]++;
          this.tot_female_popsize++;
          this.tot_current_popsize++;
          return true;
        }
        else if (sex == 'M')
        {
          this.male_popsize[age]++;
          this.tot_male_popsize++;
          this.tot_current_popsize++;
          return true;
        }
      }
      return false;
    }

    public bool decrement_popsize(Person person)
    {
      int age = person.get_age();
      char sex = person.get_sex();
      if (age > Demographics.MAX_AGE)
      {
        age = Demographics.MAX_AGE;
      }
      if (age >= 0)
      {
        if (sex == 'F')
        {
          this.female_popsize[age]--;
          this.tot_female_popsize--;
          this.tot_current_popsize--;
          return true;
        }
        else if (sex == 'M')
        {
          this.male_popsize[age]--;
          this.tot_male_popsize--;
          this.tot_current_popsize--;
          return true;
        }
      }
      return false;
    }

    public void add_household(Household h)
    {
      this.households.Add(h);
    }

    public void update(int day)
    {
      // TODO: test and enable
      return;

      // update housing periodically
      /*if (day == 0 || (Date.get_month() == 6 && Date.get_day_of_month() == 30))
      {
        Utils.FRED_VERBOSE(0, "County.update_housing = {0}", day);
        update_housing(day);
        Utils.FRED_VERBOSE(0, "County.update_housing = {0}", day);
      }

      // update birth and death rates on July 1
      if (Date.get_month() == 7 && Date.get_day_of_month() == 1)
      {
        update_population_dynamics(day);
      }*/
    }

    public void set_initial_popsize(int popsize)
    {
      this.target_popsize = popsize;
    }

    public void update_population_dynamics(int day)
    {
      double total_adj = 1.0;

      // no adjustment for first year, to avoid overreacting to low birth rate
      if (day < 1)
      {
        return;
      }

      // get the current year
      int year = Date.get_year();

      // get the target population size for the end of the coming year
      this.target_popsize = Convert.ToInt32((1.0 + 0.01 * this.population_growth_rate) * this.target_popsize);

      double error = (this.tot_current_popsize - this.target_popsize) / (1.0 * this.target_popsize);

      double mortality_rate_adjustment = 1.0 + this.mortality_rate_adjustment_weight * error;

      total_adj *= mortality_rate_adjustment;

      // adjust mortality rates
      for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      {
        this.adjusted_female_mortality_rate[i] = mortality_rate_adjustment * this.adjusted_female_mortality_rate[i];
        this.adjusted_male_mortality_rate[i] = mortality_rate_adjustment * this.adjusted_male_mortality_rate[i];
      }

      // message to LOG file
      Utils.FRED_VERBOSE(0, "COUNTY {0} POP_DYN: year {1}  popsize = {2} target = {3} pct_error = {4:P} death_adj = {5:P}  total_adj = {6:P}",
                   this.fips, year, this.tot_current_popsize, this.target_popsize, 100.0 * error, mortality_rate_adjustment, total_adj);
    }

    public void get_housing_imbalance(int day)
    {
      get_housing_data(this.beds, this.occupants);
      int imbalance = 0;
      int houses = this.households.Count;
      for (int i = 0; i < houses; ++i)
      {
        // skip group quarters
        if (this.households[i].is_group_quarters())
        {
          continue;
        }
        imbalance += Math.Abs(this.beds[i] - this.occupants[i]);
      }

      Utils.FRED_VERBOSE(1, "DAY {0} HOUSING: houses = {1}, imbalance = {2}", day, houses, imbalance);
    }

    public int fill_vacancies(int day)
    {
      // move ready_to_moves into underfilled units
      int moved = 0;
      if (this.ready_to_move.Count > 0)
      {
        // first focus on the empty units
        int houses = this.households.Count;
        for (int newhouse = 0; newhouse < houses; ++newhouse)
        {
          if (this.occupants[newhouse] > 0)
          {
            continue;
          }
          int vacancies = this.beds[newhouse] - this.occupants[newhouse];
          if (vacancies > 0)
          {
            var houseptr = this.households[newhouse];
            // skip group quarters
            if (houseptr.is_group_quarters())
            {
              continue;
            }
            for (int j = 0; (j < vacancies) && (this.ready_to_move.Count > 0); ++j)
            {
              var person = this.ready_to_move.Last().Item1;
              int oldhouse = this.ready_to_move.Last().Item2;
              var ohouseptr = this.households[oldhouse];
              this.ready_to_move.PopBack();
              if (ohouseptr.is_group_quarters() || (this.occupants[oldhouse] - this.beds[oldhouse] > 0))
              {
                // move person to new home
                person.move_to_new_house(houseptr);
                this.occupants[oldhouse]--;
                this.occupants[newhouse]++;
                moved++;
              }
            }
          }
        }

        // now consider any vacancy
        for (int newhouse = 0; newhouse < houses; ++newhouse)
        {
          int vacancies = beds[newhouse] - this.occupants[newhouse];
          if (vacancies > 0)
          {
            var houseptr = this.households[newhouse];
            // skip group quarters
            if (houseptr.is_group_quarters())
            {
              continue;
            }
            for (int j = 0; (j < vacancies) && (this.ready_to_move.Count > 0); ++j)
            {
              var person = this.ready_to_move.Last().Item1;
              int oldhouse = this.ready_to_move.Last().Item2;
              var ohouseptr = this.households[oldhouse];
              this.ready_to_move.PopBack();
              if (ohouseptr.is_group_quarters() || (this.occupants[oldhouse] - this.beds[oldhouse] > 0))
              {
                // move person to new home
                person.move_to_new_house(houseptr);
                this.occupants[oldhouse]--;
                this.occupants[newhouse]++;
                moved++;
              }
            }
          }
        }
      }
      return moved;
    }

    public void update_housing(int day)
    {

      this.houses = this.households.Count;
      Utils.FRED_VERBOSE(0, "houses = {0}", houses);

      if (day == 0)
      {
        // initialize house data structures
        this.beds = new int[houses];
        this.occupants = new int[houses];
        this.target_popsize = tot_current_popsize;
      }

      get_housing_data(this.beds, this.occupants);
      this.max_beds = -1;
      this.max_occupants = -1;
      for (int i = 0; i < this.houses; ++i)
      {
        if (this.beds[i] > this.max_beds)
        {
          this.max_beds = this.beds[i];
        }
        if (this.occupants[i] > this.max_occupants)
        {
          this.max_occupants = this.occupants[i];
        }
      }

      Utils.FRED_VERBOSE(1, "DAY {0} HOUSING: houses = {1}, max_beds = {2} max_occupants = {3}",
       day, this.houses, this.max_beds, this.max_occupants);
      Utils.FRED_VERBOSE(1, "BEFORE ");
      get_housing_imbalance(day);

      move_college_students_out_of_dorms(day);
      get_housing_imbalance(day);

      move_college_students_into_dorms(day);
      get_housing_imbalance(day);

      move_military_personnel_out_of_barracks(day);
      get_housing_imbalance(day);

      move_military_personnel_into_barracks(day);
      get_housing_imbalance(day);

      move_inmates_out_of_prisons(day);
      get_housing_imbalance(day);

      move_inmates_into_prisons(day);
      get_housing_imbalance(day);

      move_patients_into_nursing_homes(day);
      get_housing_imbalance(day);

      move_young_adults(day);
      get_housing_imbalance(day);

      move_older_adults(day);
      get_housing_imbalance(day);

      swap_houses(day);
      get_housing_imbalance(day);

      report_household_distributions();
      // Global.Places.report_school_distributions(day);
    }

    public void move_college_students_out_of_dorms(int day)
    {
      // printf("MOVE FORMER COLLEGE RESIDENTS =======================\n");
      this.ready_to_move.Clear();
      int college = 0;
      // find students ready to move off campus
      int houses = this.households.Count;
      for (int i = 0; i < houses; ++i)
      {
        var house = this.households[i];
        if (house.is_college())
        {
          int hsize = house.get_size();
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            // printf("PERSON %d LIVES IN COLLEGE DORM %s\n", person.get_id(), house.get_label());
            Utils.assert(person.is_college_dorm_resident());
            college++;
            // some college students leave each year
            if (FredRandom.NextDouble() < this.college_departure_rate)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }

      // printf("DAY %d READY TO MOVE %d COLLEGE\n", day, (int) ready_to_move.Count);
      int moved = fill_vacancies(day);
      // printf("DAY %d MOVED %d COLLEGE STUDENTS\n",day,moved);
      // printf("DAY %d COLLEGE COUNT AFTER DEPARTURES %d\n", day, college-moved); fflush(stdout);
    }

    public void move_college_students_into_dorms(int day)
    {
      {
        // printf("GENERATE NEW COLLEGE RESIDENTS =======================\n");
        this.ready_to_move.Clear();
        int moved = 0;
        int college = 0;

        // find vacant doom rooms
        var dorm_rooms = new List<int>();
        for (int i = 0; i < this.houses; ++i)
        {
          var house = this.households[i];
          if (house.is_college())
          {
            int vacancies = house.get_orig_size() - house.get_size();
            for (int j = 0; j < vacancies; ++j)
            {
              dorm_rooms.Add(i);
            }
            college += house.get_size();
          }
        }
        int dorm_vacancies = (int)dorm_rooms.Count;
        // printf("COLLEGE COUNT %d VACANCIES %d\n", college, dorm_vacancies);
        if (dorm_vacancies == 0)
        {
          Utils.FRED_VERBOSE(1, "NO COLLEGE VACANCIES FOUND\n");
          return;
        }

        // find students to fill the dorms
        for (int i = 0; i < this.houses; ++i)
        {
          var house = this.households[i];
          if (house.is_group_quarters() == false)
          {
            int hsize = house.get_size();
            if (hsize <= house.get_orig_size())
            {
              continue;
            }
            for (int j = 0; j < hsize; ++j)
            {
              var person = house.get_enrollee(j);
              int age = person.get_age();
              if (18 < age && age < 40 && person.get_number_of_children() == 0)
              {
                this.ready_to_move.Add(new Tuple<Person, int>(person, i));
              }
            }
          }
        }
        // printf("COLLEGE APPLICANTS %d\n", (int)ready_to_move.Count);

        if (this.ready_to_move.Count == 0)
        {
          Utils.FRED_VERBOSE(1, "NO COLLEGE APPLICANTS FOUND\n");
          return;
        }

        // shuffle the applicants
        this.ready_to_move.Shuffle();

        // pick the top of the list to move into dorms
        for (int i = 0; i < dorm_vacancies && this.ready_to_move.Count > 0; ++i)
        {
          int newhouse = dorm_rooms[i];
          var houseptr = Global.Places.get_household(newhouse);
          //      printf("VACANT DORM %s ORIG %d SIZE %d\n", houseptr.get_label(),
          //      houseptr.get_orig_size(),houseptr.get_size());
          var person = this.ready_to_move.Last().Item1;
          int oldhouse = this.ready_to_move.Last().Item2;
          var ohouseptr = Global.Places.get_household(oldhouse);
          this.ready_to_move.PopBack();
          // move person to new home
          //      printf("APPLICANT %d SEX %c AGE %d HOUSE %s SIZE %d ORIG %d PROFILE %c\n",
          //      person.get_id(),person.get_sex(),person.get_age(),ohouseptr.get_label(),
          //      ohouseptr.get_size(),ohouseptr.get_orig_size(),person.get_profile());
          person.move_to_new_house(houseptr);
          this.occupants[oldhouse]--;
          this.occupants[newhouse]++;
          moved++;
        }
        Utils.FRED_VERBOSE(1, "DAY {0} ACCEPTED {1} COLLEGE STUDENTS, CURRENT = {2}  MAX = {3}", day, moved, college + moved, college + dorm_vacancies);
      }
    }

    public void move_military_personnel_out_of_barracks(int day)
    {
      // printf("MOVE FORMER MILITARY =======================\n");
      this.ready_to_move.Clear();
      int military = 0;
      // find military personnel to discharge
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_military_base())
        {
          int hsize = house.get_size();
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            // printf("PERSON %d LIVES IN MILITARY BARRACKS %s\n", person.get_id(), house.get_label());
            Utils.assert(person.is_military_base_resident());
            military++;
            // some military leave each each year
            if (FredRandom.NextDouble() < this.military_departure_rate)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("DAY %d READY TO MOVE %d FORMER MILITARY\n", day, (int) ready_to_move.Count);
      int moved = fill_vacancies(day);
      // printf("DAY %d RELEASED %d MILITARY, TOTAL NOW %d\n",day,moved,military-moved);
    }

    public void move_military_personnel_into_barracks(int day)
    {
      // printf("GENERATE NEW MILITARY BASE RESIDENTS =======================\n");
      this.ready_to_move.Clear();
      int moved = 0;
      int military = 0;

      // find unfilled barracks units
      var barracks_units = new List<int>();
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_military_base())
        {
          int vacancies = house.get_orig_size() - house.get_size();
          for (int j = 0; j < vacancies; ++j)
          {
            barracks_units.Add(i);
          }
          military += house.get_size();
        }
      }
      int barracks_vacancies = (int)barracks_units.Count;
      // printf("MILITARY VACANCIES %d\n", barracks_vacancies);
      if (barracks_vacancies == 0)
      {
        Utils.FRED_VERBOSE(1, "NO MILITARY VACANCIES FOUND\n");
        return;
      }

      // find recruits to fill the dorms
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_group_quarters() == false)
        {
          int hsize = house.get_size();
          if (hsize <= house.get_orig_size()) continue;
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            int age = person.get_age();
            if (18 < age && age < 40 && person.get_number_of_children() == 0)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("MILITARY RECRUITS %d\n", (int)ready_to_move.Count);

      if (this.ready_to_move.Count == 0)
      {
        Utils.FRED_VERBOSE(1, "NO MILITARY RECRUITS FOUND\n");
        return;
      }

      // shuffle the recruits
      this.ready_to_move.Shuffle();

      // pick the top of the list to move into dorms
      for (int i = 0; i < barracks_vacancies && ready_to_move.Count > 0; ++i)
      {
        int newhouse = barracks_units[i];
        var houseptr = Global.Places.get_household(newhouse);
        // printf("UNFILLED BARRACKS %s ORIG %d SIZE %d\n", houseptr.get_label(),
        // houseptr.get_orig_size(),houseptr.get_size());
        var person = this.ready_to_move.Last().Item1;
        int oldhouse = this.ready_to_move.Last().Item2;
        var ohouseptr = Global.Places.get_household(oldhouse);
        this.ready_to_move.PopBack();
        // move person to new home
        // printf("RECRUIT %d SEX %c AGE %d HOUSE %s SIZE %d ORIG %d PROFILE %c\n",
        // person.get_id(),person.get_sex(),person.get_age(),ohouseptr.get_label(),
        // ohouseptr.get_size(),ohouseptr.get_orig_size(),person.get_profile());
        person.move_to_new_house(houseptr);
        this.occupants[oldhouse]--;
        this.occupants[newhouse]++;
        moved++;
      }
      Utils.FRED_VERBOSE(1, "DAY {0} ADDED {1} MILITARY, CURRENT = {2}  MAX = {3}",
        day, moved, military + moved, military + barracks_vacancies);
    }

    public void move_inmates_out_of_prisons(int day)
    {
      // printf("RELEASE PRISONERS =======================\n");
      this.ready_to_move.Clear();
      int prisoners = 0;
      // find former prisoners still in jail
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_prison())
        {
          int hsize = house.get_size();
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            // printf("PERSON %d LIVES IN PRISON %s\n", person.get_id(), house.get_label());
            Utils.assert(person.is_prisoner());
            prisoners++;
            // some prisoners get out each year
            if (FredRandom.NextDouble() < this.prison_departure_rate)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("DAY %d READY TO MOVE %d FORMER PRISONERS\n", day, (int) ready_to_move.Count);
      int moved = fill_vacancies(day);
      Utils.FRED_VERBOSE(1, "DAY {0} RELEASED {1} PRISONERS, TOTAL NOW {2}", day, moved, prisoners - moved);
    }

    public void move_inmates_into_prisons(int day)
    {
      // printf("GENERATE NEW PRISON RESIDENTS =======================\n");
      this.ready_to_move.Clear();
      int moved = 0;
      int prisoners = 0;

      // find unfilled jail_cell units
      var jail_cell_units = new List<int>();
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_prison())
        {
          int vacancies = house.get_orig_size() - house.get_size();
          for (int j = 0; j < vacancies; ++j)
          {
            jail_cell_units.Add(i);
          }
          prisoners += house.get_size();
        }
      }
      int jail_cell_vacancies = (int)jail_cell_units.Count;
      // printf("PRISON VACANCIES %d\n", jail_cell_vacancies);
      if (jail_cell_vacancies == 0)
      {
        Utils.FRED_VERBOSE(1, "NO PRISON VACANCIES FOUND\n");
        return;
      }

      // find inmates to fill the jail_cells
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_group_quarters() == false)
        {
          int hsize = house.get_size();
          if (hsize <= house.get_orig_size()) continue;
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            int age = person.get_age();
            if ((18 < age && person.get_number_of_children() == 0) || (age < 50))
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("PRISON POSSIBLE INMATES %d\n", (int)ready_to_move.Count);

      if (this.ready_to_move.Count == 0)
      {
        Utils.FRED_VERBOSE(1, "NO INMATES FOUND\n");
        return;
      }

      // shuffle the inmates
      this.ready_to_move.Shuffle();

      // pick the top of the list to move into dorms
      for (int i = 0; i < jail_cell_vacancies && this.ready_to_move.Count > 0; ++i)
      {
        int newhouse = jail_cell_units[i];
        var houseptr = Global.Places.get_household(newhouse);
        // printf("UNFILLED JAIL_CELL %s ORIG %d SIZE %d\n", houseptr.get_label(),
        // houseptr.get_orig_size(),houseptr.get_size());
        var person = this.ready_to_move.Last().Item1;
        int oldhouse = this.ready_to_move.Last().Item2;
        var ohouseptr = Global.Places.get_household(oldhouse);
        this.ready_to_move.PopBack();
        // move person to new home
        // printf("INMATE %d SEX %c AGE %d HOUSE %s SIZE %d ORIG %d PROFILE %c\n",
        // person.get_id(),person.get_sex(),person.get_age(),ohouseptr.get_label(),
        // ohouseptr.get_size(),ohouseptr.get_orig_size(),person.get_profile());
        person.move_to_new_house(houseptr);
        this.occupants[oldhouse]--;
        this.occupants[newhouse]++;
        moved++;
      }
      Utils.FRED_VERBOSE(1, "DAY {0} ADDED {1} PRISONERS, CURRENT = {2}  MAX = {3}",
        day, moved, prisoners + moved, prisoners + jail_cell_vacancies);
    }

    public void move_patients_into_nursing_homes(int day)
    {
      // printf("NEW NURSING HOME RESIDENTS =======================\n");
      this.ready_to_move.Clear();
      int moved = 0;
      int nursing_home_residents = 0;
      int beds = 0;

      // find unfilled nursing_home units
      var nursing_home_units = new List<int>();
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_nursing_home())
        {
          int vacancies = house.get_orig_size() - house.get_size();
          for (int j = 0; j < vacancies; ++j) { nursing_home_units.Add(i); }
          nursing_home_residents += house.get_size();
          beds += house.get_orig_size();
        }
      }
      int nursing_home_vacancies = (int)nursing_home_units.Count;
      // printf("NURSING HOME VACANCIES %d\n", nursing_home_vacancies);
      if (nursing_home_vacancies == 0)
      {
        Utils.FRED_VERBOSE(1, "DAY {0} ADDED {1} NURSING HOME PATIENTS, TOTAL NOW {2} BEDS = {3}", day, 0, nursing_home_residents, beds);
        return;
      }

      // find patients to fill the nursing_homes
      for (int i = 0; i < this.houses; ++i)
      {
        var house = this.households[i];
        if (house.is_group_quarters() == false)
        {
          int hsize = house.get_size();
          if (hsize <= house.get_orig_size())
          {
            continue;
          }
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            int age = person.get_age();
            if (60 <= age)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("NURSING HOME POSSIBLE PATIENTS %d\n", (int)ready_to_move.Count);

      // shuffle the patients
      this.ready_to_move.Shuffle();

      // pick the top of the list to move into nursing_home
      for (int i = 0; i < nursing_home_vacancies && this.ready_to_move.Count > 0; ++i)
      {
        int newhouse = nursing_home_units[i];
        var houseptr = Global.Places.get_household(newhouse);
        // printf("UNFILLED NURSING_HOME UNIT %s ORIG %d SIZE %d\n", houseptr.get_label(),houseptr.get_orig_size(),houseptr.get_size());
        var person = this.ready_to_move.Last().Item1;
        int oldhouse = this.ready_to_move.Last().Item2;
        var ohouseptr = Global.Places.get_household(oldhouse);
        this.ready_to_move.PopBack();
        // move person to new home
        /*
          printf("PATIENT %d SEX %c AGE %d HOUSE %s SIZE %d ORIG %d PROFILE %c\n",
          person.get_id(),person.get_sex(),person.get_age(),ohouseptr.get_label(),
          ohouseptr.get_size(),ohouseptr.get_orig_size(),person.get_profile());
        */
        person.move_to_new_house(houseptr);
        this.occupants[oldhouse]--;
        this.occupants[newhouse]++;
        moved++;
      }
      Utils.FRED_VERBOSE(1, "DAY {0} ADDED {1} NURSING HOME PATIENTS, CURRENT = {2}  MAX = {3}", day, moved, nursing_home_residents + moved, beds);
    }

    public void move_young_adults(int day)
    {
      // printf("MOVE YOUNG ADULTS =======================\n");
      this.ready_to_move.Clear();
      // find young adults in overfilled units who are ready to move out
      for (int i = 0; i < this.houses; ++i)
      {
        if (this.occupants[i] > this.beds[i])
        {
          var house = this.households[i];
          int hsize = house.get_size();
          for (int j = 0; j < hsize; ++j)
          {
            var person = house.get_enrollee(j);
            int age = person.get_age();
            if (18 <= age && age < 30)
            {
              if (FredRandom.NextDouble() < this.youth_home_departure_rate)
              {
                this.ready_to_move.Add(new Tuple<Person, int>(person, i));
              }
            }
          }
        }
      }
      // printf("DAY %d READY TO MOVE young adults = %d\n", day, (int) ready_to_move.size());
      int moved = fill_vacancies(day);
      Utils.FRED_VERBOSE(1, "MOVED {0} YOUNG ADULTS =======================\n", moved);
    }

    public void move_older_adults(int day)
    {
      // printf("MOVE OLDER ADULTS =======================\n");
      this.ready_to_move.Clear();
      // find older adults in overfilled units
      for (int i = 0; i < this.houses; ++i)
      {
        int excess = this.occupants[i] - this.beds[i];
        if (excess > 0)
        {
          var house = this.households[i];
          // find the oldest person in the house
          int hsize = house.get_size();
          int max_age = -1;
          int pos = -1;
          int adults = 0;
          for (int j = 0; j < hsize; ++j)
          {
            int age = house.get_enrollee(j).get_age();
            if (age > max_age)
            {
              max_age = age; pos = j;
            }
            if (age > 20)
            {
              adults++;
            }
          }
          if (adults > 1)
          {
            var person = house.get_enrollee(pos);
            if (FredRandom.NextDouble() < this.adult_home_departure_rate)
            {
              this.ready_to_move.Add(new Tuple<Person, int>(person, i));
            }
          }
        }
      }
      // printf("DAY %d READY TO MOVE older adults = %d\n", day, (int) ready_to_move.size());
      int moved = fill_vacancies(day);
      Utils.FRED_VERBOSE(1, "MOVED {0} OLDER ADULTS =======================\n", moved);
    }

    public void report_ages(int day, int house_id)
    {
      var house = this.households[house_id];
      Utils.FRED_VERBOSE(1, "HOUSE {0} BEDS {1} OCC {2} AGES ", house.get_id(), this.beds[house_id], this.occupants[house_id]);
      int hsize = house.get_size();
      for (int j = 0; j < hsize; ++j)
      {
        int age = house.get_enrollee(j).get_age();
        Utils.FRED_VERBOSE(1, "{0} ", age);
      }
    }

    public void swap_houses(int day)
    {
      Utils.FRED_VERBOSE(1, "SWAP HOUSES =======================\n");
      // two-dim array of vectors of imbalanced houses
      var houselist = new List<int>[13, 13];
      for (int i = 0; i < 13; ++i)
      {
        for (int j = 0; j < 13; ++j)
        {
          houselist[i, j] = new List<int>();
        }
      }
      for (int i = 0; i < this.houses; ++i)
      {
        // skip group quarters
        if (Global.Places.get_household(i).is_group_quarters())
        {
          continue;
        }
        int b = this.beds[i];
        if (b > 12)
        {
          b = 12;
        }
        int occ = this.occupants[i];
        if (occ > 12)
        {
          occ = 12;
        }
        if (b != occ)
        {
          houselist[b,occ].Add(i);
        }
      }

      // find complementary pairs (beds=i,occ=j) and (beds=j,occ=i)
      for (int i = 1; i < 10; ++i)
      {
        for (int j = i + 1; j < 10; ++j)
        {
          while (houselist[i,j].Count > 0 && houselist[j,i].Count > 0)
          {
            int hi = houselist[i,j].Last();
            houselist[i,j].PopBack();
            int hj = houselist[j,i].Last();
            houselist[j,i].PopBack();
            // swap houses hi and hj
            // printf("SWAPPING: "); report_ages(day,hi); report_ages(day,hj); printf("\n");
            Global.Places.swap_houses(hi, hj);
            this.occupants[hi] = i;
            this.occupants[hj] = j;
            // printf("AFTER:\n"); report_ages(day,hi); report_ages(day,hj);
          }
        }
      }

      /*
      return;
      // refill-vectors
      for (int i = 0; i < 10; ++i)
      {
        for (int j = 0; j < 10; ++j)
        {
          houselist[i][j].clear();
        }
      }
      for (int i = 0; i < this.houses; ++i)
      {
        int b = this.beds[i];
        if (b > 9)
        {
          b = 9;
        }
        int occ = this.occupants[i];
        if (occ > 9)
        {
          occ = 9;
        }
        if (b > 0 && b != occ)
        {
          houselist[b][occ].push_back(i);
        }
      }

      //// find complementary pairs (beds=B,occ=o) and (beds=b,occ=O) where o<=b and O <= B
      //for (int bd = 0; bd < 10; bd++) {
      //for (int oc = bd+1; oc < 10; oc++) {
      //// this household needs more beds
      //while (houselist[bd][oc].size() > 0) {
      //int h1 = houselist[bd][oc].back();
      //houselist[bd][oc].pop_back();
      //int swapper = 0;
      //for (int Bd = oc; (swapper == 0) && Bd < 10; Bd++) {
      //for (int Oc = bd; (swapper == 0) && 0 <= Oc; Oc--) {
      //// this house has enough beds for the first house
      //// and needs at most as many beds as are in the first house
      //if (houselist[Bd][Oc].size() > 0) {
      //int h2 = houselist[Bd][Oc].back();
      //houselist[Bd][Oc].pop_back();
      //// swap houses h1 and h2
      //printf("SWAPPING II: "); report_ages(day,h1); report_ages(day,h2); printf("\n");
      //Global.Places.swap_houses(h1, h2);
      //occupants[h1] = Oc;
      //occupants[h2] = oc;
      //// printf("AFTER:\n"); report_ages(day,h1); report_ages(day,h1);
      //swapper = 1;
      //}
      //}
      //}
      //}
      //}
      //}
      // get_housing_imbalance(day);

      // NOTE: This can be simplified using swap houses matrix

      for (int i = 0; i < this.houses; ++i)
      {
        if (this.beds[i] == 0)
        {
          continue;
        }
        int diff = this.occupants[i] - this.beds[i];
        if (diff < -1 || diff > 1)
        {
          // take a look at this house
          Household* house = this.households[i];
          FRED_DEBUG(1, "DAY %d PROBLEM HOUSE %d BEDS %d OCC %d AGES ",
            day, house.get_id(), beds[i], occupants[i]);
          int hsize = house.get_size();
          for (int j = 0; j < hsize; ++j)
          {
            int age = house.get_enrollee(j).get_age();
            FRED_DEBUG(1, "%d ", age);
          }
          FRED_DEBUG(1, "\n");
        }
      }

      // make lists of overfilled houses
      vector<int>* overfilled;
      overfilled = new vector<int>[this.max_beds + 1];
      for (int i = 0; i <= this.max_beds; ++i)
      {
        overfilled[i].clear();
      }

      // make lists of underfilled houses
      vector<int>* underfilled;
      underfilled = new vector<int>[this.max_beds + 1];
      for (int i = 0; i <= this.max_beds; ++i)
      {
        underfilled[i].clear();
      }

      for (int i = 0; i < this.houses; ++i)
      {
        if (this.beds[i] == 0)
        {
          continue;
        }
        int diff = this.occupants[i] - this.beds[i];
        if (diff > 0)
        {
          overfilled[this.beds[i]].push_back(i);
        }
        if (diff < 0)
        {
          underfilled[this.beds[i]].push_back(i);
        }
      }

      int count[100];
      for (int i = 0; i <= this.max_beds; ++i)
      {
        for (int j = 0; j <= this.max_beds + 1; ++j)
        {
          count[j] = 0;
        }
        for (int k = 0; k < (int)overfilled[i].size(); ++k)
        {
          int kk = overfilled[i][k];
          int occ = this.occupants[kk];
          if (occ <= this.max_beds + 1)
          {
            count[occ]++;
          }
          else
          {
            count[this.max_beds + 1]++;
          }
        }
        for (int k = 0; k < (int)underfilled[i].size(); ++k)
        {
          int kk = underfilled[i][k];
          int occ = this.occupants[kk];
          if (occ <= this.max_beds + 1)
          {
            count[occ]++;
          }
          else
          {
            count[this.max_beds + 1]++;
          }
        }
        FRED_DEBUG(1, "DAY %4d BEDS %2d ", day, i);
        for (int j = 0; j <= this.max_beds + 1; ++j)
        {
          FRED_DEBUG(1, "%3d ", count[j]);
        }
        FRED_DEBUG(1, "\n");
        fflush(stdout);
      }*/
    }

    // public int find_fips_code(int n);

    public int get_housing_data(int[] target_size, int[] current_size)
    {
      int num_households = this.households.Count;
      for (int i = 0; i < num_households; ++i)
      {
        var h = this.households[i];
        current_size[i] = h.get_size();
        target_size[i] = h.get_orig_size();
      }
      return num_households;
    }

    public void report_household_distributions()
    {
      int houses = this.households.Count;

      if (Global.Verbose > 0)
      {
        var count = new int[20];
        int total = 0;
        // size distribution of households
        for (int c = 0; c <= 10; ++c)
        {
          count[c] = 0;
        }
        for (int p = 0; p < houses; ++p)
        {
          int n = this.households[p].get_size();
          if (n <= 10)
          {
            count[n]++;
          }
          else
          {
            count[10]++;
          }
          total++;
        }
        Utils.FRED_STATUS(0, "Household size distribution: N = {0} ", total);
        for (int c = 0; c <= 10; ++c)
        {
          Utils.FRED_STATUS(0, "{0.###}: {1.######} ({2:P}) ", c, count[c], (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "\n");

        // original size distribution
        var hsize = new int[20];
        total = 0;
        // size distribution of households
        for (int c = 0; c <= 10; ++c)
        {
          count[c] = 0;
          hsize[c] = 0;
        }
        for (int p = 0; p < houses; ++p)
        {
          int n = this.households[p].get_orig_size();
          int hs = this.households[p].get_size();
          if (n <= 10)
          {
            count[n]++;
            hsize[n] += hs;
          }
          else
          {
            count[10]++;
            hsize[10] += hs;
          }
          total++;
        }
        Utils.FRED_STATUS(0, "Household orig distribution: N = {0} ", total);
        for (int c = 0; c <= 10; c++)
        {
          Utils.FRED_STATUS(0, "{0.###}: {1.######} ({2:P}) {3:P} ", c, count[c],
                 (100.0 * count[c]) / total, count[c] > 0 ? ((double)hsize[c] / (double)count[c]) : 0.0);
        }
        Utils.FRED_STATUS(0, "\n");
      }
      return;
    }

    public void report_county_population()
    {
      if (Global.Report_County_Demographic_Information)
      {
        Utils.fred_report("County_Demographic_Information,fips[{0}],date[{1}]\n", this.get_fips(), Date.get_date_string());
        Utils.fred_report("County_Demographic_Information,Total,Males,Females\n");
        Utils.fred_report("County_Demographic_Information,{0},{2},{3}\n", this.tot_current_popsize, this.tot_male_popsize, this.tot_female_popsize);
        Utils.fred_report("County_Demographic_Information,By Age Groups:\n");
        Utils.fred_report("County_Demographic_Information,Ages,Total,Males,Females\n");
        for (int i = 0; i <= Demographics.MAX_AGE; i += 5)
        {
          if (i == 5)
          { //want 0 - 5, then 6 - 10, 11 - 15, 16 - 20, etc.)
            i++;
          }
          int max = (i == 0 ? i + 5 : (i + 4 > Demographics.MAX_AGE ? Demographics.MAX_AGE : i + 4));
          int males = this.get_current_popsize(i, max, 'M');
          int females = this.get_current_popsize(i, max, 'F');
          Utils.fred_report("County_Demographic_Information,({0} - {1}),{2},{3},{4}\n", i, max, males + females, males, females);
        }
        Utils.fred_report("County_Demographic_Information\n");
      }
    }

    public double get_pregnancy_rate(int age)
    {
      return this.pregnancy_rate[age];
    }

    public double get_mortality_rate(int age, char sex)
    {
      if (sex == 'F')
      {
        if (age > Demographics.MAX_AGE)
        {
          return this.adjusted_female_mortality_rate[Demographics.MAX_AGE];
        }
        else
        {
          return this.adjusted_female_mortality_rate[age];
        }
      }
      else
      {
        if (age > Demographics.MAX_AGE)
        {
          return this.adjusted_male_mortality_rate[Demographics.MAX_AGE];
        }
        else
        {
          return this.adjusted_male_mortality_rate[age];
        }
      }
    }
  }
}
