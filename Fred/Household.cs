using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Household : Place
  {
    private static double contacts_per_day;
    private static double same_age_bias;
    private static double[,] prob_transmission_per_contact;

    //Income Limits for classification
    private static int Cat_I_Max_Income;
    private static int Cat_II_Max_Income;
    private static int Cat_III_Max_Income;
    private static int Cat_IV_Max_Income;
    private static int Cat_V_Max_Income;
    private static int Cat_VI_Max_Income;

    private static int Min_hh_income;
    private static int Max_hh_income;
    private static int Min_hh_income_90_pct;

    private Place group_quarters_workplace;
    private bool sheltering;
    private bool primary_healthcare_available;
    private bool other_healthcare_location_that_accepts_insurance_available;
    private bool healthcare_available;
    private bool seeking_healthcare;
    private int count_seeking_hc;
    private int count_primary_hc_unav;
    private int count_hc_accept_ins_unav;

    private bool hh_schl_aged_chld_unemplyd_adlt_chng;
    private bool hh_schl_aged_chld;
    private bool hh_schl_aged_chld_unemplyd_adlt;
    private bool hh_sympt_child;
    private bool hh_working_adult_using_sick_leave;

    private char deme_id;        // deme == synthetic population id
    private int group_quarters_units;
    private int shelter_start_day;
    private int shelter_end_day;
    private int household_income;
    private int income_quartile;
    private Household_income_level_code household_income_code;

    // true iff a household member is at one of the places for an extended absence
    //std.bitset<Household_extended_absence_index.HOUSEHOLD_EXTENDED_ABSENCE> not_home_bitset;
    private BitArray not_home_bitset = new BitArray(4);

    // Places that household members may visit
    private Dictionary<Household_visitation_place_index, Place> household_visitation_places_map;

    // profile of original housemates
    private readonly List<int> ages = new List<int>();
    private readonly List<int> ids = new List<int>();

    // Sick time available to watch children for adult housemates
    private readonly Dictionary<Person, HH_Adult_Sickleave_Data> adult_childcare_sickleave_map = new Dictionary<Person, HH_Adult_Sickleave_Data>();
    private readonly static Dictionary<int, int> count_inhabitants_by_household_income_level_map = new Dictionary<int, int>();

    public readonly static Dictionary<int, int> count_children_by_household_income_level_map = new Dictionary<int, int>();
    public readonly static Dictionary<long, int> count_inhabitants_by_census_tract_map = new Dictionary<long, int>();
    public readonly static Dictionary<long, int> count_children_by_census_tract_map = new Dictionary<long, int>();
    public readonly static List<long> census_tract_set = new List<long>();

    public static string household_income_level_lookup(Household_income_level_code idx)
    {
      switch (idx)
      {
        case Household_income_level_code.CAT_I:
          return "cat_I";
        case Household_income_level_code.CAT_II:
          return "cat_II";
        case Household_income_level_code.CAT_III:
          return "cat_III";
        case Household_income_level_code.CAT_IV:
          return "cat_IV";
        case Household_income_level_code.CAT_V:
          return "cat_V";
        case Household_income_level_code.CAT_VI:
          return "cat_VI";
        case Household_income_level_code.CAT_VII:
          return "cat_VII";
        case Household_income_level_code.UNCLASSIFIED:
          return "Unclassified";
        default:
          Utils.fred_abort("Invalid Household Income Level Code", "");
          break;
      }
      return null;
    }

    public static Household_income_level_code get_household_income_level_code_from_income(int income)
    {
      if (income < 0)
      {
        return Household_income_level_code.UNCLASSIFIED;
      }
      else if (income < Household.Cat_I_Max_Income)
      {
        return Household_income_level_code.CAT_I;
      }
      else if (income < Household.Cat_II_Max_Income)
      {
        return Household_income_level_code.CAT_II;
      }
      else if (income < Household.Cat_III_Max_Income)
      {
        return Household_income_level_code.CAT_III;
      }
      else if (income < Household.Cat_IV_Max_Income)
      {
        return Household_income_level_code.CAT_IV;
      }
      else if (income < Household.Cat_V_Max_Income)
      {
        return Household_income_level_code.CAT_V;
      }
      else if (income < Household.Cat_VI_Max_Income)
      {
        return Household_income_level_code.CAT_VI;
      }
      else
      {
        return Household_income_level_code.CAT_VII;
      }
    }

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Household()
    {
      this.set_type(Place.TYPE_HOUSEHOLD);
      this.set_subtype(Place.SUBTYPE_NONE);
      this.sheltering = false;
      this.hh_schl_aged_chld_unemplyd_adlt_chng = false;
      this.hh_schl_aged_chld = false;
      this.hh_schl_aged_chld_unemplyd_adlt = false;
      this.hh_sympt_child = false;
      this.hh_working_adult_using_sick_leave = false;
      this.seeking_healthcare = false;
      this.primary_healthcare_available = true;
      this.other_healthcare_location_that_accepts_insurance_available = true;
      this.healthcare_available = true;
      this.count_seeking_hc = 0;
      this.count_primary_hc_unav = 0;
      this.count_hc_accept_ins_unav = 0;
      this.shelter_start_day = 0;
      this.shelter_end_day = 0;
      this.deme_id = ' ';
      this.group_quarters_units = 0;
      this.group_quarters_workplace = null;
      this.income_quartile = -1;
      this.household_income = -1;
      this.household_income_code = Household_income_level_code.UNCLASSIFIED;
    }

    /**
     * Constructor with necessary parameters
     */
    public Household(string label, char _subtype, FredGeo lon, FredGeo lat)
      : base(label, lon, lat)
    {
      this.set_type(Place.TYPE_HOUSEHOLD);
      this.set_subtype(_subtype);
      this.sheltering = false;
      this.hh_schl_aged_chld_unemplyd_adlt_chng = false;
      this.hh_schl_aged_chld = false;
      this.hh_schl_aged_chld_unemplyd_adlt = false;
      this.hh_sympt_child = false;
      this.hh_working_adult_using_sick_leave = false;
      this.seeking_healthcare = false;
      this.primary_healthcare_available = true;
      this.other_healthcare_location_that_accepts_insurance_available = true;
      this.healthcare_available = true;
      this.count_seeking_hc = 0;
      this.count_primary_hc_unav = 0;
      this.count_hc_accept_ins_unav = 0;
      this.shelter_start_day = 0;
      this.shelter_end_day = 0;
      this.deme_id = ' ';
      this.intimacy = 1.0;
      this.group_quarters_units = 0;
      this.group_quarters_workplace = null;
      this.income_quartile = -1;
      this.household_income = -1;
      this.household_income_code = Household_income_level_code.UNCLASSIFIED;
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("household_contacts", ref contacts_per_day);
      FredParameters.GetParameter("neighborhood_same_age_bias", ref same_age_bias);
      same_age_bias *= 0.5;
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("household_trans_per_contact");
      int n = prob_transmission_per_contact.Length;
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nHousehold contact_prob:");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.Write("{0} ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
      }

      //Get the Household Income Cutoffs
      FredParameters.GetParameter("cat_I_max_income", ref Cat_I_Max_Income);
      FredParameters.GetParameter("cat_II_max_income", ref Cat_II_Max_Income);
      FredParameters.GetParameter("cat_III_max_income", ref Cat_III_Max_Income);
      FredParameters.GetParameter("cat_IV_max_income", ref Cat_IV_Max_Income);
      FredParameters.GetParameter("cat_V_max_income", ref Cat_V_Max_Income);
      FredParameters.GetParameter("cat_VI_max_income", ref Cat_VI_Max_Income);
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
      
      return 1;
    }

    /**
     * @see Mixing_Group.get_transmission_prob(int disease_id, Person* i, Person* s)
     *
     * This method returns the value from the static array <code>Household.Household_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Household_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>household_prob[]</code>.
     */
    public override double get_transmission_prob(int disease_id, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease_id, i);
      int col = get_group(disease_id, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
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
     * @see Place.get_contacts_per_day(int disease)
     *
     * This method returns the value from the static array <code>Household.Household_contacts_per_day</code>
     * that corresponds to a particular disease.<br />
     * The static array <code>Household_contacts_per_day</code> will be filled with values from the parameter
     * file for the key <code>household_contacts[]</code>.
     */
    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    /**
     * Use to get list of all people in the household.
     * @return vector of pointers to people in household.
     */
    public List<Person> get_inhabitants()
    {
      return this.enrollees;
    }

    /**
     * Set the household income.
     */
    public void set_household_income(int x)
    {
      this.household_income = x;
      this.set_household_income_code(get_household_income_level_code_from_income(x));
      if (x != -1)
      {
        if (x >= 0 && x < Household.Min_hh_income)
        {
          Household.Min_hh_income = x;
          //Utils.fred_log("HH_INCOME: Min[%i]\n", Household.Min_hh_income);
        }
        if (x > Household.Max_hh_income)
        {
          Household.Max_hh_income = x;
          //Utils.fred_log("HH_INCOME: Max[%i]\n", Household.Max_hh_income);
        }
      }
    }

    /**
     * Get the household income.
     * @return income
     */
    public int get_household_income()
    {
      return this.household_income;
    }

    /**
     * Determine if the household should be open. It is dependent on the disease and simulation day.
     *
     * @param day the simulation day
     * @param disease an integer representation of the disease
     * @return whether or not the household is open on the given day for the given disease
     */
    public override bool should_be_open(int day, int disease)
    {
      return true;
    }

    /**
     * Record the ages and the id's of the original members of the household
     */
    public void record_profile()
    {
      // record the ages
      this.ages.Clear();
      int size = get_size();
      for (int i = 0; i < size; ++i)
      {
        this.ages.Add(this.enrollees[i].get_age());
      }

      // record the id's of the original members of the household
      this.ids.Clear();
      for (int i = 0; i < size; ++i)
      {
        this.ids.Add(this.enrollees[i].get_id());
      }
    }

    /**
     * @return the original count of agents in this Household
     */
    public override int get_orig_size()
    {
      return this.ids.Count;
    }

    /*
     * @param i the index of the agent
     * @return the id of the original household member with index i
     */
    public int get_orig_id(int i)
    {
      return this.ids[i];
    }

    public void set_deme_id(char _deme_id)
    {
      this.deme_id = _deme_id;
    }

    public char get_deme_id()
    {
      return this.deme_id;
    }

    public void set_group_quarters_units(int units)
    {
      this.group_quarters_units = units;
    }

    public int get_group_quarters_units()
    {
      return this.group_quarters_units;
    }

    public void set_shelter(bool _sheltering)
    {
      this.sheltering = _sheltering;
    }

    public bool is_sheltering()
    {
      return this.sheltering;
    }

    public bool is_sheltering_today(int day)
    {
      return (this.shelter_start_day <= day && day < this.shelter_end_day);
    }

    public void set_shelter_start_day(int start_day)
    {
      this.shelter_start_day = start_day;
    }

    public void set_shelter_end_day(int end_day)
    {
      this.shelter_end_day = end_day;
    }

    public int get_shelter_start_day()
    {
      return this.shelter_start_day;
    }

    public int get_shelter_end_day()
    {
      return this.shelter_end_day;
    }

    public void set_seeking_healthcare(bool _seeking_healthcare)
    {
      this.seeking_healthcare = _seeking_healthcare;
    }

    public bool is_seeking_healthcare()
    {
      return this.seeking_healthcare;
    }

    public void set_is_primary_healthcare_available(bool _primary_healthcare_available)
    {
      this.primary_healthcare_available = _primary_healthcare_available;
    }

    public bool is_primary_healthcare_available()
    {
      return this.primary_healthcare_available;
    }

    public void set_other_healthcare_location_that_accepts_insurance_available(bool _other_healthcare_location_that_accepts_insurance_available)
    {
      this.other_healthcare_location_that_accepts_insurance_available = _other_healthcare_location_that_accepts_insurance_available;
    }

    public bool is_other_healthcare_location_that_accepts_insurance_available()
    {
      return this.other_healthcare_location_that_accepts_insurance_available;
    }

    public void set_is_healthcare_available(bool _healthcare_available)
    {
      this.healthcare_available = _healthcare_available;
    }

    public bool is_healthcare_available()
    {
      return this.healthcare_available;
    }

    public int get_household_income_code()
    {
      return (int)this.household_income_code;
    }

    /**
     * @return a pointer to this household's visitation hospital
     */
    public Hospital get_household_visitation_hospital()
    {
      return (Hospital)(get_household_visitation_place(Household_visitation_place_index.HOSPITAL));
    }

    public void set_household_visitation_hospital(Hospital hosp)
    {
      set_household_visitation_place(Household_visitation_place_index.HOSPITAL, hosp);
    }

    public void set_household_has_hospitalized_member(bool does_have)
    {
      this.hh_schl_aged_chld_unemplyd_adlt_chng = true;
      if (does_have)
      {
        this.not_home_bitset[(int)Household_extended_absence_index.HAS_HOSPITALIZED] = true;
      }
      else
      {
        //Initially say no one is hospitalized
        this.not_home_bitset[(int)Household_extended_absence_index.HAS_HOSPITALIZED] = false;
        //iterate over all housemates  to see if anyone is still hospitalized
        foreach (var enrollee in this.enrollees)
        {
          if (enrollee.is_hospitalized())
          {
            this.not_home_bitset[(int)Household_extended_absence_index.HAS_HOSPITALIZED] = true;
            if (this.get_household_visitation_hospital() == null)
            {
              this.set_household_visitation_hospital((Hospital)(enrollee.get_activities().get_hospital()));
            }
            break;
          }
        }
      }
    }

    public bool has_hospitalized_member()
    {
      return this.not_home_bitset[(int)Household_extended_absence_index.HAS_HOSPITALIZED];
    }

    public void set_group_quarters_workplace(Place p)
    {
      this.group_quarters_workplace = p;
    }

    public Place get_group_quarters_workplace()
    {
      return this.group_quarters_workplace;
    }

    public bool has_school_aged_child()
    {
      //Household has been loaded
      Utils.assert(Global.Pop.is_load_completed());
      //If the household status hasn't changed, just return the flag
      if (!this.hh_schl_aged_chld_unemplyd_adlt_chng)
      {
        return this.hh_schl_aged_chld;
      }
      else
      {
        bool ret_val = false;
        for (int i = 0; i < this.enrollees.Count; ++i)
        {
          var per = this.enrollees[i];
          if (per.is_student() && per.is_child())
          {
            ret_val = true;
            break;
          }
        }
        this.hh_schl_aged_chld = ret_val;
        this.hh_schl_aged_chld_unemplyd_adlt_chng = false;
        return ret_val;
      }
    }

    public bool has_school_aged_child_and_unemployed_adult()
    {
      //Household has been loaded
      Utils.assert(Global.Pop.is_load_completed());
      //If the household status hasn't changed, just return the flag
      if (!this.hh_schl_aged_chld_unemplyd_adlt_chng)
      {
        return this.hh_schl_aged_chld_unemplyd_adlt;
      }
      else
      {
        bool ret_val = false;
        if (has_school_aged_child())
        {
          for (int i = 0; i < this.enrollees.Count; ++i)
          {
            var per = this.enrollees[i];
            if (per.is_child())
            {
              continue;
            }

            //Person is an adult, but is also a student
            if (per.is_adult() && per.is_student())
            {
              continue;
            }

            //Person is an adult, but isn't at home
            if (per.is_adult() &&
               (per.is_hospitalized() ||
                per.is_college_dorm_resident() ||
                per.is_military_base_resident() ||
                per.is_nursing_home_resident() ||
                per.is_prisoner()))
            {
              continue;
            }

            //Person is an adult AND is either retired or unemployed
            if (per.is_adult() &&
               (per.get_activities().get_profile() == Activities.RETIRED_PROFILE ||
                per.get_activities().get_profile() == Activities.UNEMPLOYED_PROFILE))
            {
              ret_val = true;
              break;
            }
          }
        }
        this.hh_schl_aged_chld_unemplyd_adlt = ret_val;
        this.hh_schl_aged_chld_unemplyd_adlt_chng = false;
        return ret_val;
      }
    }

    public void set_sympt_child(bool _hh_sympt_childl)
    {
      this.hh_sympt_child = _hh_sympt_childl;
    }

    public bool has_sympt_child()
    {
      return this.hh_sympt_child;
    }

    public void set_hh_schl_aged_chld_unemplyd_adlt_chng(bool _hh_status_changed)
    {
      this.hh_schl_aged_chld_unemplyd_adlt_chng = _hh_status_changed;
    }

    public void set_working_adult_using_sick_leave(bool _is_using_sl)
    {
      this.hh_working_adult_using_sick_leave = _is_using_sl;
    }

    public bool has_working_adult_using_sick_leave()
    {
      return this.hh_working_adult_using_sick_leave;
    }

    public void prepare_person_childcare_sickleave_map()
    {
      //Household has been loaded
      Utils.assert(Global.Pop.is_load_completed());

      if (Global.Report_Childhood_Presenteeism)
      {
        if (has_school_aged_child() && !has_school_aged_child_and_unemployed_adult())
        {
          for (int i = 0; i < this.enrollees.Count; ++i)
          {
            var per = this.enrollees[i];
            if (per.is_child())
            {
              continue;
            }

            //Person is an adult, but is also a student
            if (per.is_adult() && per.is_student())
            {
              continue;
            }

            //Person is an adult, but isn't at home
            if (per.is_adult() &&
               (per.is_hospitalized() ||
                per.is_college_dorm_resident() ||
                per.is_military_base_resident() ||
                per.is_nursing_home_resident() ||
                per.is_prisoner()))
            {
              continue;
            }

            //Person is an adult AND is neither retired nor unemployed
            if (per.is_adult() &&
               per.get_activities().get_profile() != Activities.RETIRED_PROFILE &&
               per.get_activities().get_profile() != Activities.UNEMPLOYED_PROFILE)
            {
              //Insert the adult into the sickleave info map
              var sickleave_info = new HH_Adult_Sickleave_Data();

              //Add any school-aged children to that person's info
              for (int j = 0; j < this.enrollees.Count; ++j)
              {
                var child_check = this.enrollees[j];
                if (child_check.is_student() && child_check.is_child())
                {
                  sickleave_info.add_child_to_maps(child_check);
                }
              }

              this.adult_childcare_sickleave_map.Add(per, sickleave_info);
            }
          }
        }
      }
      else
      {
        return;
      }
    }

    public bool have_working_adult_use_sickleave_for_child(Person adult, Person child)
    {
      if (this.adult_childcare_sickleave_map.ContainsKey(adult))
      {
        var sickleave_data = this.adult_childcare_sickleave_map[adult];
        if (!sickleave_data.stay_home_with_child(adult))
        { //didn't already stayed home with this child
          return sickleave_data.stay_home_with_child(child);
        }
      }
      return false;
    }


    public void set_income_quartile(int _income_quartile)
    {
      Utils.assert(_income_quartile >= Global.Q1 && _income_quartile <= Global.Q4);
      this.income_quartile = _income_quartile;
    }

    public int get_income_quartile()
    {
      return this.income_quartile;
    }

    public int get_count_seeking_hc()
    {
      return this.count_seeking_hc;
    }

    public void set_count_seeking_hc(int _count_seeking_hc)
    {
      this.count_seeking_hc = _count_seeking_hc;
    }

    public int get_count_primary_hc_unav()
    {
      return this.count_primary_hc_unav;
    }

    public void set_count_primary_hc_unav(int _count_primary_hc_unav)
    {
      this.count_primary_hc_unav = _count_primary_hc_unav;
    }

    public int get_count_hc_accept_ins_unav()
    {
      return this.count_hc_accept_ins_unav;
    }

    public void set_count_hc_accept_ins_unav(int _count_hc_accept_ins_unav)
    {
      this.count_hc_accept_ins_unav = _count_hc_accept_ins_unav;
    }

    public void reset_healthcare_info()
    {
      this.set_is_primary_healthcare_available(true);
      this.set_other_healthcare_location_that_accepts_insurance_available(true);
      this.set_is_healthcare_available(true);
      this.set_count_seeking_hc(0);
      this.set_count_primary_hc_unav(0);
      this.set_count_hc_accept_ins_unav(0);
    }

    public static int get_min_hh_income()
    {
      return Household.Min_hh_income;
    }

    public static int get_max_hh_income()
    {
      return Household.Max_hh_income;
    }

    public static int get_min_hh_income_90_pct()
    {
      return Household.Min_hh_income_90_pct;
    }

    public static void set_min_hh_income_90_pct(int _hh_income)
    {
      Household.Min_hh_income_90_pct = _hh_income;
    }

    private void set_household_income_code(Household_income_level_code _household_income_code)
    {
      this.household_income_code = _household_income_code;
    }

    private Place get_household_visitation_place(Household_visitation_place_index i)
    {
      if (this.household_visitation_places_map.ContainsKey(i))
      {
        return this.household_visitation_places_map[i];
      }
      else
      {
        return null;
      }
    }

    private void set_household_visitation_place(Household_visitation_place_index i, Place p)
    {
      if (p != null)
      {
        this.household_visitation_places_map[i] = p;
      }
      else if (this.household_visitation_places_map.ContainsKey(i))
      {
        this.household_visitation_places_map.Remove(i);
      }
    }
  }
}
