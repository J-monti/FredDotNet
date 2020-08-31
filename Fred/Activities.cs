using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Fred
{
  public class Activities
  {
    public const int MAX_MOBILITY_AGE = 100;

    // Activity Profiles
    public const char INFANT_PROFILE = 'I';
    public const char PRESCHOOL_PROFILE = 'P';
    public const char STUDENT_PROFILE = 'S';
    public const char TEACHER_PROFILE = 'T';
    public const char WORKER_PROFILE = 'W';
    public const char WEEKEND_WORKER_PROFILE = 'Y';
    public const char UNEMPLOYED_PROFILE = 'U';
    public const char RETIRED_PROFILE = 'R';
    public const char UNDEFINED_PROFILE = 'X';

    public const char PRISONER_PROFILE = 'J';
    public const char COLLEGE_STUDENT_PROFILE = 'C';
    public const char MILITARY_PROFILE = 'M';
    public const char NURSING_HOME_RESIDENT_PROFILE = 'L';

    public const string SEEK_HC = "Seek_hc";
    public const string PRIMARY_HC_UNAV = "Primary_hc_unav";
    public const string HC_ACCEP_INS_UNAV = "Hc_accep_ins_unav";
    public const string HC_UNAV = "Hc_unav";
    public const string ER_VISIT = "ER_visit";
    public const string DIABETES_HC_UNAV = "Diabetes_hc_unav";
    public const string ASTHMA_HC_UNAV = "Asthma_hc_unav";
    public const string HTN_HC_UNAV = "HTN_hc_unav";
    public const string MEDICAID_UNAV = "Medicaid_unav";
    public const string MEDICARE_UNAV = "Medicare_unav";
    public const string PRIVATE_UNAV = "Private_unav";
    public const string UNINSURED_UNAV = "Uninsured_unav";

    private Person myself;

    // links to daily activity locations
    private Person_Place_Link link;

    // links to networks of people
    private List<Person_Network_Link> networks = new List<Person_Network_Link>();

    private BitArray on_schedule = new BitArray((int)Activity_index.DAILY_ACTIVITY_LOCATIONS);

    // list of daily activity locations, stored while traveling
    private Place[] stored_daily_activity_locations;

    //Primary Care Location
    private Hospital primary_healthcare_facility;

    private Place home_neighborhood;

    // daily activity schedule:
    private int schedule_updated;                       // date of last schedule update
    private bool is_traveling;                         // true if traveling
    private bool is_traveling_outside;                 // true if traveling outside modeled area

    private char profile;                              // activities profile type
    private bool is_hospitalized;
    private bool is_isolated;
    private int return_from_travel_sim_day;
    private int sim_day_hospitalization_ends;
    private int grade;

    // Do I have paid sick leave
    private bool sick_leave_available;
    // Only matters if have sick_leave_available
    private double sick_days_remaining;

    // individual sick day variables
    private int my_sick_days_absent;
    private int my_sick_days_present;
    private int my_unpaid_sick_days_absent;

    private bool my_sick_leave_decision_has_been_made;
    private bool my_sick_leave_decision;

    // static variables
    private static bool is_initialized; // true if static arrays have been initialized
    private static bool is_weekday;     // true if current day is Monday .. Friday
    private static int day_of_week;     // day of week index, where Sun = 0, ... Sat = 6

    // run-time parameters
    private static bool Enable_default_sick_behavior;
    private static double Default_sick_day_prob;
    //  // mean number of sick days taken if sick leave is available
    //  static double SLA_mean_sick_days_absent;
    //  // mean number of sick days taken if sick leave is unavailable
    //  static double SLU_mean_sick_days_absent;
    // prob of staying home if sick leave is available
    private static double SLA_absent_prob;
    // prob of staying home if sick leave is unavailable
    private static double SLU_absent_prob;
    // extra sick days for flu
    private static double Flu_days;

    private static int HAZEL_seek_hc_ramp_up_days;
    private static double HAZEL_seek_hc_ramp_up_mult = 1.0;

    private static Age_Map Hospitalization_prob;
    private static Age_Map Outpatient_healthcare_prob;
    private static double Hospitalization_visit_housemate_prob;

    private static readonly Activities_Tracking_Data Tracking_data = new Activities_Tracking_Data();

    // sick days statistics
    private static double Standard_sicktime_allocated_per_child;

    private const int WP_SIZE_DIST = 1;
    private const int HH_INCOME_QTILE_DIST = 2;
    private static int Sick_leave_dist_method;
    private static List<double> WP_size_sl_prob_vec;
    private static List<double> HH_income_qtile_sl_prob_vec;
    private static double WP_small_mean_sl_days_available;
    private static double WP_large_mean_sl_days_available;
    private static int WP_size_cutoff_sl_exception;
    private static double HH_income_qtile_mean_sl_days_available;

    // Statistics for childhood presenteeism study
    private static double Sim_based_prob_stay_home_not_needed;
    private static double Census_based_prob_stay_home_not_needed;

    // school change statistics
    private static int entered_school;
    private static int left_school;

    public Activities()
    {
      this.my_sick_days_absent = -1;
      this.my_sick_days_present = -1;
      this.my_unpaid_sick_days_absent = -1;
      this.profile = UNDEFINED_PROFILE;
      this.schedule_updated = -1;
      this.sim_day_hospitalization_ends = -1;
      this.grade = 0;
      this.return_from_travel_sim_day = -1;
      this.link = new Person_Place_Link[(int)Activity_index.DAILY_ACTIVITY_LOCATIONS];
    }
    /**
   * Setup activities at start of run
   */
    public void prepare()
    {
      this.initialize_sick_leave();
    }

    public static string activity_lookup(Activity_index idx)
    {
      Utils.assert(idx >= 0);
      Utils.assert(idx<Activity_index.DAILY_ACTIVITY_LOCATIONS);
      switch(idx) {
        case Activity_index.HOUSEHOLD_ACTIVITY:
          return "Household";
        case Activity_index.NEIGHBORHOOD_ACTIVITY:
          return "Neighborhood";
        case Activity_index.SCHOOL_ACTIVITY:
          return "School";
        case Activity_index.CLASSROOM_ACTIVITY:
          return "Classroom";
        case Activity_index.WORKPLACE_ACTIVITY:
          return "Workplace";
        case Activity_index.OFFICE_ACTIVITY:
          return "Office";
        case Activity_index.HOSPITAL_ACTIVITY:
          return "Hospital";
        case Activity_index.AD_HOC_ACTIVITY:
          return "Ad_Hoc";
        default:
          Utils.fred_abort("Invalid Activity Type", "");
          break;
      }
      return null;
    }

    /**
     * Setup sick leave depending on size of workplace
     */
    public void initialize_sick_leave()
    {
      Utils.FRED_VERBOSE(1, "initialize_sick_leave entered");
      this.my_sick_days_absent = 0;
      this.my_sick_days_present = 0;
      this.my_sick_leave_decision_has_been_made = false;
      this.my_sick_leave_decision = false;
      this.sick_days_remaining = 0.0;
      this.sick_leave_available = false;

      if (!Enable_default_sick_behavior)
      {
        int index = get_index_of_sick_leave_dist(this.myself);
        if (index >= 0 && Sick_leave_dist_method == WP_SIZE_DIST)
        {
          this.sick_leave_available = FredRandom.NextDouble() < WP_size_sl_prob_vec[index];
          if (this.sick_leave_available)
          {
            Tracking_data.employees_with_sick_leave[index]++;
            // compute sick days available
            int workplace_size = 0;
            if (get_workplace() != null)
            {
              workplace_size = get_workplace().get_size();
            }
            else
            {
              if (is_teacher())
              {
                if (get_school() != null)
                {
                  workplace_size = get_school().get_staff_size();
                }
              }
            }
            if (workplace_size <= WP_size_cutoff_sl_exception)
            {
              this.sick_days_remaining = WP_small_mean_sl_days_available + Flu_days;
            }
            else
            {
              this.sick_days_remaining = WP_large_mean_sl_days_available + Flu_days;
            }
          }
          else
          {
            Tracking_data.employees_without_sick_leave[index]++;
          }
        }
        else if (index >= 0 && Sick_leave_dist_method == HH_INCOME_QTILE_DIST)
        {
          this.sick_leave_available = FredRandom.NextDouble() < HH_income_qtile_sl_prob_vec[index];
          // compute sick days available
          this.sick_days_remaining = HH_income_qtile_mean_sl_days_available + Flu_days;
        }

        Utils.FRED_VERBOSE(0, "initialize_sick_leave size_leave_avaliable = {0}",
                     this.sick_leave_available ? 1 : 0);
      }
      Utils.FRED_VERBOSE(0, "initialize_sick_leave sick_days_remaining = {0}", this.sick_days_remaining);
    }

    /**
     * Assigns an activity profile to the agent
     */
    public void assign_initial_profile()
    {
      int age = this.myself.get_age();
      if (age == 0)
      {
        this.profile = PRESCHOOL_PROFILE;
      }
      else if (get_school() != null)
      {
        this.profile = STUDENT_PROFILE;
      }
      else if (age < Global.SCHOOL_AGE)
      {
        this.profile = PRESCHOOL_PROFILE;    // child at home
      }
      else if (get_workplace() != null)
      {
        this.profile = WORKER_PROFILE;// worker
      }
      else if (Global.RETIREMENT_AGE <= age)
      {
        this.profile = RETIRED_PROFILE;      // retired
      }
      else
      {
        this.profile = UNEMPLOYED_PROFILE;
      }

      // weekend worker
      if (this.profile == WORKER_PROFILE && FredRandom.NextDouble() < 0.2)
      {  // 20% weekend worker
        this.profile = WEEKEND_WORKER_PROFILE;
      }

      // profiles for group quarters residents
      if (get_household().is_college())
      {
        this.profile = COLLEGE_STUDENT_PROFILE;
      }
      if (get_household().is_military_base())
      {
        this.profile = MILITARY_PROFILE;
      }
      if (get_household().is_prison())
      {
        this.profile = PRISONER_PROFILE;
        Utils.FRED_VERBOSE(2, "INITIAL PROFILE AS PRISONER ID {0} AGE {1} SEX {2} HOUSEHOLD {3}",
         this.myself.get_id(), age, this.myself.get_sex(), get_household().get_label());
      }
      if (get_household().is_nursing_home())
      {
        this.profile = NURSING_HOME_RESIDENT_PROFILE;
      }
    }

    public bool get_is_hospitalized()
    {
      return this.is_hospitalized;
    }

    /**
     * Perform the daily update for an infectious agent
     *
     * @param day the simulation day
     */
    public void update_activities_of_infectious_person(int sim_day)
    {
      Utils.FRED_VERBOSE(1, "update_activities for person {0} day {1}", this.myself.get_id(), sim_day);

      // skip all scheduled activities if traveling abroad
      if (this.is_traveling_outside)
      {
        return;
      }

      if (Global.Enable_Isolation)
      {
        if (this.is_isolated)
        {
          // once isolated, remain isolated
          update_schedule(sim_day);
          return;
        }
        else
        {
          // enter isolation if symptomatic, with a given probability
          if (this.myself.is_symptomatic() > 0)
          {
            // are we passed the isolation delay period?
            if (Global.Isolation_Delay <= this.myself.get_days_symptomatic())
            {
              //decide whether to enter isolation
              if (FredRandom.NextDouble() < Global.Isolation_Rate)
              {
                this.is_isolated = true;
                update_schedule(sim_day);
                return;
              }
            }
          }
        }
      }

      if (sim_day > this.schedule_updated)
      {
        // get list of places to visit today
        update_schedule(sim_day);

        // decide which neighborhood to visit today
        if (this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY])
        {
          var destination_neighborhood = Global.Neighborhoods.select_destination_neighborhood(this.home_neighborhood);
          set_neighborhood(destination_neighborhood);
        }

        // if symptomatic, decide whether or not to stay home
        if (this.myself.is_symptomatic() > 0 && !this.myself.is_hospitalized())
        {
          decide_whether_to_stay_home(sim_day);
          //For Symptomatics - background will be in update_schedule()
          if (Global.Enable_Hospitals)
          {
            decide_whether_to_seek_healthcare(sim_day);
          }
        }
      }
    }

    /**
     * Perform the daily update to the schedule
     *
     * @param day the simulation day
     */
    public void update_schedule(int sim_day)
    {
      // update this schedule only once per day
      if (sim_day <= this.schedule_updated)
      {
        return;
      }

      this.schedule_updated = sim_day;
      this.on_schedule.SetAll(false);

      // if isolated, visit nowhere today
      if (this.is_isolated)
      {
        return;
      }

      if (Global.Enable_Hospitals && this.is_hospitalized && !(this.sim_day_hospitalization_ends == sim_day))
      {
        // only visit the hospital
        this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
        if (Global.Enable_HAZEL)
        {
          Global.Daily_Tracker.increment_index_key_pair(sim_day, SEEK_HC, 1);
          var hh = (Household)this.myself.get_permanent_household();
          Utils.assert(hh != null);
          hh.set_count_seeking_hc(hh.get_count_seeking_hc() + 1);
        }
      }
      else
      {
        //If the hospital stay should end today, go back to normal
        if (Global.Enable_Hospitals && this.is_hospitalized && (this.sim_day_hospitalization_ends == sim_day))
        {
          end_hospitalization();
        }

        // always visit the household
        this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = true;

        // decide if my household is sheltering
        if (Global.Enable_Household_Shelter || Global.Enable_HAZEL)
        {
          var h = (Household)this.myself.get_household();
          if (h.is_sheltering_today(sim_day))
          {
            Utils.FRED_STATUS(1, "update_schedule on day {0}\n{1}", sim_day, schedule_to_string(sim_day));
            return;
          }
        }

        // decide whether to visit the neighborhood
        if (this.profile == PRISONER_PROFILE || this.profile == NURSING_HOME_RESIDENT_PROFILE)
        {
          // prisoners and nursing home residents stay indoors
          this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = false;
        }
        else
        {
          // provisionally visit the neighborhood
          this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = true;
        }

        // weekday vs weekend provisional activity
        if (is_weekday)
        {
          if (get_school() != null)
          {
            this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = true;
          }
          if (get_classroom() != null)
          {
            this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = true;
          }
          if (get_workplace() != null)
          {
            this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = true;
          }
          if (get_office() != null)
          {
            this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = true;
          }
        }
        else
        {
          if (this.profile == WEEKEND_WORKER_PROFILE || this.profile == STUDENT_PROFILE)
          {
            if (get_workplace() != null)
            {
              this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = true;
            }
            if (get_office() != null)
            {
              this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = true;
            }
          }
          else if (this.is_hospital_staff())
          {
            if (FredRandom.NextDouble() < 0.4)
            {
              if (get_workplace() != null)
              {
                this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = true;
              }
              if (get_office() != null)
              {
                this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = true;
              }
            }
          }
        }

        //Decide whether to visit healthcare if ASYMPTOMATIC (Background)
        if (Global.Enable_Hospitals && !(this.myself.is_symptomatic() > 0) && !this.myself.is_hospitalized())
        {
          decide_whether_to_seek_healthcare(sim_day);
        }

        //Decide whether to visit hospitalized housemates
        if (Global.Enable_Hospitals && !this.myself.is_hospitalized() && (Household)this.myself.get_household().has_hospitalized_member())
        {

          var hh = (Household)this.myself.get_household();
          if (hh == null)
          {
            if (Global.Enable_Hospitals && this.myself.is_hospitalized() && this.myself.get_permanent_household() != null)
            {
              hh = (Household)this.myself.get_permanent_household();
            }
          }

          if (hh != null)
          {
            if (this.profile != PRISONER_PROFILE
              && this.profile != NURSING_HOME_RESIDENT_PROFILE
              && FredRandom.NextDouble() < Hospitalization_visit_housemate_prob)
            {
              set_ad_hoc(hh.get_household_visitation_hospital());
              this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = true;
            }
          }
        }

        // skip work at background absenteeism rate
        if (Global.Work_absenteeism > 0.0 && this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY])
        {
          if (FredRandom.NextDouble() < Global.Work_absenteeism)
          {
            this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
          }
        }

        // skip school at background school absenteeism rate
        if (Global.School_absenteeism > 0.0 && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
        {
          if (FredRandom.NextDouble() < Global.School_absenteeism)
          {
            this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
          }
        }

        if (Global.Report_Childhood_Presenteeism)
        {
          if (this.myself.is_adult() && this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY])
          {
            var my_hh = (Household)this.myself.get_household();
            //my_hh.have_working_adult_use_sickleave_for_child()
            if (my_hh.has_sympt_child() &&
               my_hh.has_school_aged_child() &&
               !my_hh.has_school_aged_child_and_unemployed_adult() &&
               !my_hh.has_working_adult_using_sick_leave())
            {
              for (int i = 0; i < my_hh.get_size(); ++i)
              {
                var child_check = my_hh.get_enrollee(i);
                if (child_check.is_child() &&
                   child_check.is_student() &&
                   child_check.is_symptomatic() &&
                   child_check.get_activities().on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
                {
                  // Do I have enough sick time?
                  if (this.sick_days_remaining >= Standard_sicktime_allocated_per_child)
                  {
                    if (my_hh.have_working_adult_use_sickleave_for_child(this.myself, child_check))
                    {
                      //Stay Home from work
                      this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
                      this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
                      my_hh.set_working_adult_using_sick_leave(true);
                      this.sick_days_remaining -= Standard_sicktime_allocated_per_child;
                    }
                  }
                }
              }
            }
          }
        }
      }
      Utils.FRED_STATUS(1, "update_schedule on day {0}\n{1}", sim_day, schedule_to_string(sim_day));
    }

    /**
     * Decide whether to stay home if symptomatic.
     * May depend on availability of sick leave at work.
     *
     * @param sim_day the simulation day
     */
    public void decide_whether_to_stay_home(int sim_day)
    {
      Utils.assert(this.myself.is_symptomatic() > 0);
      bool stay_home = false;
      bool it_is_a_workday = this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY]
            || (is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY]);

      if (this.myself.is_adult())
      {
        if (Enable_default_sick_behavior)
        {
          stay_home = default_sick_leave_behavior();
        }
        else
        {
          if (it_is_a_workday)
          {
            int index = get_index_of_sick_leave_dist(this.myself);
            if (index >= 0 && this.my_sick_leave_decision_has_been_made)
            {
              if (this.is_sick_leave_available() && this.sick_days_remaining > 0.0)
              {
                if (this.sick_days_remaining < 1.0)
                { //I have sick leave, but my days are about to run out
                  this.sick_days_remaining = this.sick_days_remaining < 0.0 ? 0.0 : this.sick_days_remaining;
                  if (FredRandom.NextDouble() < this.sick_days_remaining)
                  {
                    stay_home = this.my_sick_leave_decision;
                    //Want to make sure that tomorrow I roll against the SLU_absent_prob
                    this.my_sick_leave_decision_has_been_made = false;
                    if (stay_home)
                    {
                      this.my_sick_days_absent++;
                      Tracking_data.employees_days_used[index]++;
                      Tracking_data.employees_sick_leave_days_used[index]++;
                    }
                    else
                    {
                      this.my_sick_days_present++;
                      Tracking_data.employees_sick_days_present[index]++;
                    }
                  }
                  else
                  {
                    stay_home = FredRandom.NextDouble() < SLU_absent_prob;
                    if (stay_home)
                    {
                      this.my_unpaid_sick_days_absent++;
                      Tracking_data.employees_days_used[index]++;
                    }
                    else
                    {
                      this.my_sick_days_present++;
                      Tracking_data.daily_sick_days_present++;
                      Tracking_data.total_employees_sick_days_present++;
                      Tracking_data.employees_sick_days_present[index]++;
                    }
                  }
                  this.my_sick_leave_decision = stay_home;
                  this.sick_days_remaining = 0.0;
                }
                else
                {
                  stay_home = this.my_sick_leave_decision;
                  if (stay_home)
                  {
                    this.my_sick_days_absent++;
                    Tracking_data.daily_sick_days_absent++;
                    Tracking_data.total_employees_days_used++;
                    Tracking_data.total_employees_sick_leave_days_used++;
                    Tracking_data.employees_days_used[index]++;
                    Tracking_data.employees_sick_leave_days_used[index]++;
                  }
                  else
                  {
                    this.my_sick_days_present++;
                    Tracking_data.daily_sick_days_present++;
                    Tracking_data.total_employees_sick_days_present++;
                    Tracking_data.employees_sick_days_present[index]++;
                  }
                  this.sick_days_remaining -= 1.0;
                }
              }
              else
              { //Sick leave decision has been made, but I don't get paid sick days
                stay_home = this.my_sick_leave_decision;
                if (stay_home)
                {
                  this.my_unpaid_sick_days_absent++;
                  Tracking_data.daily_sick_days_absent++;
                  Tracking_data.total_employees_days_used++;
                  Tracking_data.employees_days_used[index]++;
                }
                else
                {
                  this.my_sick_days_present++;
                  Tracking_data.daily_sick_days_present++;
                  Tracking_data.total_employees_sick_days_present++;
                  Tracking_data.employees_sick_days_present[index]++;
                }
              }
            }
            else if (index >= 0)
            { //Sick Leave decision hasn't been made
              if (this.is_sick_leave_available() && this.sick_days_remaining > 0.0)
              {
                if (this.sick_days_remaining < 1.0)
                { //I have sick leave but I am running out of days, so need to decide what to do
                  this.sick_days_remaining = this.sick_days_remaining < 0.0 ? 0.0 : this.sick_days_remaining;
                  if (FredRandom.NextDouble() < this.sick_days_remaining)
                  {
                    stay_home = FredRandom.NextDouble() < SLA_absent_prob;
                    if (stay_home)
                    {
                      //Want to make sure that tomorrow I roll against the SLU_absent_prob
                      this.my_sick_leave_decision_has_been_made = false;
                      this.my_sick_days_absent++;
                      Tracking_data.daily_sick_days_absent++;
                      Tracking_data.total_employees_days_used++;
                      Tracking_data.total_employees_taking_day_off++;
                      Tracking_data.total_employees_sick_leave_days_used++;
                      Tracking_data.total_employees_taking_sick_leave++;
                      Tracking_data.employees_days_used[index]++;
                      Tracking_data.employees_taking_day_off[index]++;
                      Tracking_data.employees_sick_leave_days_used[index]++;
                      Tracking_data.employees_taking_sick_leave_day_off[index]++;
                    }
                    else
                    {
                      this.my_sick_leave_decision_has_been_made = true;
                      this.my_sick_days_present++;
                      Tracking_data.daily_sick_days_present++;
                      Tracking_data.total_employees_sick_days_present++;
                      Tracking_data.employees_sick_days_present[index]++;
                    }
                  }
                  else
                  {
                    stay_home = FredRandom.NextDouble() < SLU_absent_prob;
                    this.my_sick_leave_decision_has_been_made = true;
                    if (stay_home)
                    {
                      this.my_unpaid_sick_days_absent++;
                      Tracking_data.total_employees_days_used++;
                      Tracking_data.total_employees_taking_day_off++;
                      Tracking_data.employees_days_used[index]++;
                      Tracking_data.employees_taking_day_off[index]++;
                    }
                    else
                    {
                      this.my_sick_days_present++;
                      Tracking_data.daily_sick_days_present++;
                      Tracking_data.total_employees_sick_days_present++;
                      Tracking_data.employees_sick_days_present[index]++;
                    }
                  }
                  this.my_sick_leave_decision = stay_home;
                  this.sick_days_remaining = 0.0;
                }
                else
                { //I have sick leave and I am not running out of days, so stay home or not versus SLA probability
                  stay_home = FredRandom.NextDouble() < SLA_absent_prob;
                  this.my_sick_leave_decision = stay_home;
                  this.my_sick_leave_decision_has_been_made = true;
                  if (stay_home)
                  {
                    this.my_sick_days_absent++;
                    Tracking_data.daily_sick_days_absent++;
                    Tracking_data.total_employees_days_used++;
                    Tracking_data.total_employees_taking_day_off++;
                    Tracking_data.total_employees_sick_leave_days_used++;
                    Tracking_data.total_employees_taking_sick_leave++;
                    Tracking_data.employees_days_used[index]++;
                    Tracking_data.employees_taking_day_off[index]++;
                    Tracking_data.employees_sick_leave_days_used[index]++;
                    Tracking_data.employees_taking_sick_leave_day_off[index]++;
                  }
                  else
                  {
                    this.my_sick_days_present++;
                    Tracking_data.daily_sick_days_present++;
                    Tracking_data.total_employees_sick_days_present++;
                    Tracking_data.employees_sick_days_present[index]++;
                  }
                  this.sick_days_remaining -= 1.0;
                }
              }
              else
              { //I have no sick leave available, so roll against SLU probability only
                stay_home = FredRandom.NextDouble() < SLU_absent_prob;
                this.my_sick_leave_decision = stay_home;
                this.my_sick_leave_decision_has_been_made = true;
                if (stay_home)
                {
                  this.my_unpaid_sick_days_absent++;
                  Tracking_data.total_employees_days_used++;
                  Tracking_data.total_employees_taking_day_off++;
                  Tracking_data.employees_days_used[index]++;
                  Tracking_data.employees_taking_day_off[index]++;
                }
                else
                {
                  this.my_sick_days_present++;
                  Tracking_data.daily_sick_days_present++;
                  Tracking_data.total_employees_sick_days_present++;
                  Tracking_data.employees_sick_days_present[index]++;
                }
              }
            }
          }
          else
          {
            // it is a not work day
            stay_home = FredRandom.NextDouble() < Default_sick_day_prob;
          }
        }
      }
      else
      {
        // sick child
        if (Global.Report_Childhood_Presenteeism)
        {
          bool it_is_a_schoolday = this.myself.is_student() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY];
          if (it_is_a_schoolday)
          {
            var my_hh = (Household)this.myself.get_household();
            Utils.assert(my_hh != null);
            if (this.my_sick_leave_decision_has_been_made)
            {
              //Child has already made decision to stay home
              stay_home = this.my_sick_leave_decision;
            }
            else if (my_hh.has_working_adult_using_sick_leave())
            {
              //An adult has already decided to stay home
              stay_home = true;
            }
            else if (my_hh.has_school_aged_child_and_unemployed_adult())
            {
              //The agent will stay home because someone is there to watch him/her
              double prob_diff = Sim_based_prob_stay_home_not_needed - Census_based_prob_stay_home_not_needed;
              if (prob_diff > 0)
              {
                //There is a prob_diff chance that the agent will NOT stay home
                stay_home = FredRandom.NextDouble() < (1 - prob_diff);
              }
              else
              {
                //The agent will stay home because someone is there to watch him/her
                stay_home = true;
              }
            }
            else
            {
              //No one would be home so we need to force an adult to stay home, if s/he has sick time
              //First find an adult in the house
              if (my_hh.get_adults() > 0)
              {
                var inhab_vec = my_hh.get_inhabitants();
                foreach (var inhabitant in inhab_vec)
                {
                  if (inhabitant.is_child())
                  {
                    continue;
                  }

                  //Person is an adult, but is also a student
                  if (inhabitant.is_adult() && inhabitant.is_student())
                  {
                    continue;
                  }

                  //Person is an adult, but isn't at home
                  if (my_hh.have_working_adult_use_sickleave_for_child(inhabitant, this.myself))
                  {
                    stay_home = true;
                    inhabitant.get_activities().sick_days_remaining -= Standard_sicktime_allocated_per_child;
                    my_hh.set_working_adult_using_sick_leave(true);
                    break;
                  }
                }
              }
            }
            if (!this.my_sick_leave_decision_has_been_made)
            {
              // Kids will stick to that decision even after the parent goes back to work
              // i.e. some kind of external daycare is setup
              this.my_sick_leave_decision = stay_home;
              this.my_sick_leave_decision_has_been_made = true;
            }
          }
          else
          {
            //Preschool or no school today, so we use default sick behavior, for now
            stay_home = default_sick_leave_behavior();
          }
        }
        else
        {
          //use default sick behavior, for now.
          stay_home = default_sick_leave_behavior();
        }
      }

      //Update the counters for how many sick days used

      // record work absent/present decision if it is a workday
      if (it_is_a_workday)
      {
        Tracking_data.total_employees_days_used++;
        if (stay_home)
        {
          Tracking_data.daily_sick_days_absent++;
          this.my_sick_days_absent++;
        }
        else
        {
          Tracking_data.daily_sick_days_present++;
          this.my_sick_days_present++;
        }
      }

      // record school absent/present decision if it is a school day
      if (!is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
      {
        if (stay_home)
        {
          Tracking_data.daily_school_sick_days_absent++;
          this.my_sick_days_absent++;
        }
        else
        {
          Tracking_data.daily_school_sick_days_present++;
          this.my_sick_days_present++;
        }
      }

      if (stay_home)
      {
        // withdraw to household
        this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = false;
        this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;
      }
    }

    /**
     * Decide whether to seek healthcare if symptomatic.
     *
     * @param sim_day the simulation day
     */
    public void decide_whether_to_seek_healthcare(int sim_day)
    {
      if (Global.Enable_Hospitals)
      {
        bool is_a_workday = this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY]
           || (is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY]);

        if (!this.is_hospitalized)
        {

          double rand = FredRandom.NextDouble();
          double hospitalization_prob = Hospitalization_prob.find_value(this.myself.get_real_age()); //Background probability
          double seek_healthcare_prob = Outpatient_healthcare_prob.find_value(this.myself.get_real_age()); //Background probability

          //First check to see if agent will seek health care for any active symptomatic infection
          if (this.myself.is_symptomatic() > 0)
          {
            //Get specific symptomatic diseases for multiplier
            for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
            {
              if (this.myself.get_health().is_infected(disease_id))
              {
                var disease = Global.Diseases.get_disease(disease_id);
                if (this.myself.get_health().get_symptoms(disease_id, sim_day) > disease.get_min_symptoms_for_seek_healthcare())
                {
                  hospitalization_prob += disease.get_hospitalization_prob(this.myself);
                  seek_healthcare_prob += disease.get_outpatient_healthcare_prob(this.myself);
                }
              }
            }
          }

          // If the agent has chronic conditions, multiply the probability by the appropriate modifiers
          if (Global.Enable_Chronic_Condition)
          {
            if (this.myself.has_chronic_condition())
            {
              double mult = 1.0;
              if (this.myself.is_asthmatic())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.ASTHMA);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.has_COPD())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.COPD);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.has_chronic_renal_disease())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.CHRONIC_RENAL_DISEASE);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.is_diabetic())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.DIABETES);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.has_heart_disease())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.HEART_DISEASE);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.has_hypertension())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.HYPERTENSION);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.has_hypercholestrolemia())
              {
                mult = Health.get_chronic_condition_hospitalization_prob_mult(this.myself.get_age(), Chronic_condition_index.HYPERCHOLESTROLEMIA);
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
              if (this.myself.get_demographics().is_pregnant())
              {
                mult = Health.get_pregnancy_hospitalization_prob_mult(this.myself.get_age());
                hospitalization_prob *= mult;
                seek_healthcare_prob *= mult;
              }
            }
          }

          if (Global.Enable_HAZEL)
          {
            double mult = 1.0;
            //If we are within a week after the disaster
            //Ramp up visits immediately after a disaster
            if (sim_day > Place_List.get_HAZEL_disaster_end_sim_day() && sim_day <= (Place_List.get_HAZEL_disaster_end_sim_day() + 7))
            {
              int days_since_storm_end = sim_day - Place_List.get_HAZEL_disaster_end_sim_day();
              mult = 1.0 + (1.0 / days_since_storm_end * 0.25);
              hospitalization_prob *= mult;
              seek_healthcare_prob *= mult;
            }

            //Multiplier by insurance type
            mult = 1.0;
            switch (this.myself.get_health().get_insurance_type())
            {
              case Insurance_assignment_index.PRIVATE:
                mult = 1.0;
                break;
              case Insurance_assignment_index.MEDICARE:
                mult = 1.037;
                break;
              case Insurance_assignment_index.MEDICAID:
                mult = .909;
                break;
              case Insurance_assignment_index.HIGHMARK:
                mult = 1.0;
                break;
              case Insurance_assignment_index.UPMC:
                mult = 1.0;
                break;
              case Insurance_assignment_index.UNINSURED:
                {
                  double age = this.myself.get_real_age();
                  if (age < 5.0)
                  { //These values are hard coded for HAZEL
                    mult = 1.0;
                  }
                  else if (age < 18.0)
                  {
                    mult = 0.59;
                  }
                  else if (age < 25.0)
                  {
                    mult = 0.33;
                  }
                  else if (age < 45.0)
                  {
                    mult = 0.43;
                  }
                  else if (age < 65.0)
                  {
                    mult = 0.5;
                  }
                  else
                  {
                    mult = 0.56;
                  }
                }
                break;
              case Insurance_assignment_index.UNSET:
                mult = 1.0;
                break;
            }
            hospitalization_prob *= mult;
            seek_healthcare_prob *= mult;
          }

          //First check to see if agent will visit a Hospital for an overnight stay, then check for an outpatient visit
          if (rand < hospitalization_prob)
          {
            double draw = FredRandom.Normal(3.0, 0.5);
            int length = draw > 0.0 ? Convert.ToInt32(draw) + 1 : 1;
            start_hospitalization(sim_day, length);
          }
          else if (rand < seek_healthcare_prob)
          {
            var hh = (Household)this.myself.get_household();
            Utils.assert(hh != null);

            if (Global.Enable_HAZEL)
            {
              Global.Daily_Tracker.increment_index_key_pair(sim_day, SEEK_HC, 1);
              hh.set_count_seeking_hc(hh.get_count_seeking_hc() + 1);
              if (!hh.is_seeking_healthcare())
              {
                hh.set_seeking_healthcare(true);
              }

              var hosp = this.myself.get_activities().get_primary_healthcare_facility();
              Utils.assert(hosp != null);

              if (!hosp.should_be_open(sim_day) || (hosp.get_current_daily_patient_count() >= hosp.get_daily_patient_capacity(sim_day)))
              {
                //Update all of the statistics to reflect that primary care is not available
                hh.set_is_primary_healthcare_available(false);
                if (this.myself.is_asthmatic())
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, ASTHMA_HC_UNAV, 1);
                }
                if (this.myself.is_diabetic())
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, DIABETES_HC_UNAV, 1);
                }
                if (this.myself.has_hypertension())
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, HTN_HC_UNAV, 1);
                }
                if (this.myself.get_health().get_insurance_type() == Insurance_assignment_index.MEDICAID)
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, MEDICAID_UNAV, 1);
                }
                else if (this.myself.get_health().get_insurance_type() == Insurance_assignment_index.MEDICARE)
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, MEDICARE_UNAV, 1);
                }
                else if (this.myself.get_health().get_insurance_type() == Insurance_assignment_index.PRIVATE)
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, PRIVATE_UNAV, 1);
                }
                else if (this.myself.get_health().get_insurance_type() == Insurance_assignment_index.UNINSURED)
                {
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, UNINSURED_UNAV, 1);
                }

                Global.Daily_Tracker.increment_index_key_pair(sim_day, PRIMARY_HC_UNAV, 1);
                hh.set_count_primary_hc_unav(hh.get_count_primary_hc_unav() + 1);

                //Now, try to Find an open health care provider that accepts agent's insurance
                hosp = Global.Places.get_random_open_healthcare_facility_matching_criteria(sim_day, this.myself, true, false);
                if (hosp == null)
                {
                  hh.set_other_healthcare_location_that_accepts_insurance_available(false);
                  hh.set_count_hc_accept_ins_unav(hh.get_count_hc_accept_ins_unav() + 1);
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, HC_ACCEP_INS_UNAV, 1);

                  hosp = Global.Places.get_random_open_healthcare_facility_matching_criteria(sim_day, this.myself, false, false);
                  if (hosp == null)
                  {
                    hh.set_is_healthcare_available(false);
                    Global.Daily_Tracker.increment_index_key_pair(sim_day, HC_UNAV, 1);
                  }
                }

                if (hosp != null)
                {
                  assign_hospital(hosp);
                  if (hosp.get_subtype() == Place.SUBTYPE_NONE)
                  {
                    //then it is an emergency room visit
                    Global.Daily_Tracker.increment_index_key_pair(sim_day, ER_VISIT, 1);
                  }

                  this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = true;
                  this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
                  this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
                  this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
                  this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
                  this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = true;
                  this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
                  this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;

                  hosp.increment_current_daily_patient_count();

                  // record work absent/present decision if it is a work day
                  if (is_a_workday)
                  {
                    Tracking_data.daily_sick_days_absent++;
                    this.my_sick_days_absent++;
                  }

                  // record school absent/present decision if it is a school day
                  if (!is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
                  {
                    Tracking_data.daily_school_sick_days_absent++;
                    this.my_sick_days_absent++;
                  }
                }
              }
              else
              {
                assign_hospital(hosp);
                if (hosp.get_subtype() == Place.SUBTYPE_NONE)
                {
                  //then it is an emergency room visit
                  Global.Daily_Tracker.increment_index_key_pair(sim_day, ER_VISIT, 1);
                }

                this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = true;
                this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
                this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
                this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
                this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
                this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = true;
                this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
                this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;

                hosp.increment_current_daily_patient_count();

                // record work absent/present decision if it is a work day
                if (is_a_workday)
                {
                  Tracking_data.daily_sick_days_absent++;
                  this.my_sick_days_absent++;
                }

                // record school absent/present decision if it is a school day
                if (!is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
                {
                  Tracking_data.daily_school_sick_days_absent++;
                  this.my_sick_days_absent++;
                }
              }
            }
            else
            { //Not HAZEL so don't need to track deficits
              var hosp = (Hospital)this.myself.get_hospital();
              if (hosp == null)
              {
                hosp = hh.get_household_visitation_hospital();
              }
              Utils.assert(hosp != null);
              assign_hospital(hosp);
              this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = true;
              this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
              this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
              this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
              this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
              this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = true;
              this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
              this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;

              // record work absent/present decision if it is a workday
              if (is_a_workday)
              {
                Tracking_data.daily_sick_days_absent++;
                this.my_sick_days_absent++;
              }

              // record school absent/present decision if it is a school day
              if (!is_teacher() && this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY])
              {
                Tracking_data.daily_school_sick_days_absent++;
                this.my_sick_days_absent++;
              }
            }
          }
        }
      }
    }

    /**
     * Have agent begin stay in a hospital
     *
     * @param day the simulation day
     * @param length_of_stay how many days to remain hospitalized
     */
    public void start_hospitalization(int sim_day, int length_of_stay)
    {
      Utils.assert(length_of_stay > 0);
      if (Global.Enable_Hospitals)
      {
        Utils.assert(!this.myself.is_hospitalized());
        //If agent is traveling, return home first
        if (this.is_traveling || this.is_traveling_outside)
        {
          stop_traveling();
        }

        //First see if this agent has a preferred hospital
        var hosp = (Hospital)get_daily_activity_location(Activity_index.HOSPITAL_ACTIVITY);
        var hh = (Household)this.myself.get_household();
        Utils.assert(hh != null);

        //If not, then use the household's hospital
        if (hosp == null)
        {
          hosp = hh.get_household_visitation_hospital();
          Utils.assert(hosp != null);
        }
        else if (hosp.is_healthcare_clinic() ||
                hosp.is_mobile_healthcare_clinic())
        {
          hosp = hh.get_household_visitation_hospital();
          Utils.assert(hosp != null);
        }

        if (hosp != hh.get_household_visitation_hospital())
        {
          //Change the household visitation hospital so that visitors go to the right place
          hh.set_household_visitation_hospital(hosp);
        }

        if (Global.Enable_HAZEL)
        {
          Global.Daily_Tracker.increment_index_key_pair(sim_day, SEEK_HC, 1);
          hh.set_count_seeking_hc(hh.get_count_seeking_hc() + 1);
          if (!hosp.should_be_open(sim_day) || (hosp.get_occupied_bed_count() >= hosp.get_bed_count(sim_day)))
          {
            hh.set_is_primary_healthcare_available(false);
            hh.set_count_primary_hc_unav(hh.get_count_primary_hc_unav() + 1);
            Global.Daily_Tracker.increment_index_key_pair(sim_day, PRIMARY_HC_UNAV, 1);

            //Find an open healthcare provider
            hosp = Global.Places.get_random_open_hospital_matching_criteria(sim_day, this.myself, true, false);
            if (hosp == null)
            {
              hh.set_other_healthcare_location_that_accepts_insurance_available(false);
              hh.set_count_hc_accept_ins_unav(hh.get_count_hc_accept_ins_unav() + 1);
              Global.Daily_Tracker.increment_index_key_pair(sim_day, HC_ACCEP_INS_UNAV, 1);
              hosp = Global.Places.get_random_open_hospital_matching_criteria(sim_day, this.myself, false, false);
              if (hosp == null)
              {
                hh.set_is_healthcare_available(false);
                Global.Daily_Tracker.increment_index_key_pair(sim_day, HC_UNAV, 1);
              }
            }
          }

          if (hosp != null)
          {
            store_daily_activity_locations();
            clear_daily_activity_locations();
            this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
            this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;
            assign_hospital(hosp);
            this.is_hospitalized = true;
            this.sim_day_hospitalization_ends = sim_day + length_of_stay;
            hosp.increment_occupied_bed_count();
            Global.Daily_Tracker.increment_index_key_pair(sim_day, ER_VISIT, 1);

            //Set the flag for the household
            hh.set_household_has_hospitalized_member(true);
          }
        }
        else
        {
          if (hosp.get_occupied_bed_count() < hosp.get_bed_count(sim_day))
          {
            store_daily_activity_locations();
            clear_daily_activity_locations();
            this.on_schedule[(int)Activity_index.HOUSEHOLD_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.WORKPLACE_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.OFFICE_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.SCHOOL_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.CLASSROOM_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.NEIGHBORHOOD_ACTIVITY] = false;
            this.on_schedule[(int)Activity_index.HOSPITAL_ACTIVITY] = true;
            this.on_schedule[(int)Activity_index.AD_HOC_ACTIVITY] = false;
            assign_hospital(hosp);

            this.is_hospitalized = true;
            this.sim_day_hospitalization_ends = sim_day + length_of_stay;
            hosp.increment_occupied_bed_count();

            //Set the flag for the household
            hh.set_household_has_hospitalized_member(true);
          }
          else
          {
            //No room in the hospital
            //TODO: should we do something else?
          }
        }
      }
    }

    /**
     * Have agent end stay in a hospital
     *
     * @param self this agent
     */
    public void end_hospitalization()
    {
      if (Global.Enable_Hospitals)
      {
        this.is_hospitalized = false;
        this.sim_day_hospitalization_ends = -1;
        var tmp_hosp = (Hospital)this.myself.get_hospital();
        Utils.assert(tmp_hosp != null);
        tmp_hosp.decrement_occupied_bed_count();
        restore_daily_activity_locations();

        //Set the flag for the household
        ((Household)this.myself.get_household()).set_household_has_hospitalized_member(false);
      }
    }

    /**
     * Decide whether to stay home if symptomatic.
     * If Enable_default_sick_leave_behavior is set, the decision is made only once,
     * and the agent stays home for the entire symptomatic period, or never stays home.
     */
    public bool default_sick_leave_behavior()
    {
      bool stay_home = false;
      if (this.my_sick_leave_decision_has_been_made)
      {
        stay_home = this.my_sick_leave_decision;
      }
      else
      {
        stay_home = FredRandom.NextDouble() < Default_sick_day_prob;
        this.my_sick_leave_decision = stay_home;
        this.my_sick_leave_decision_has_been_made = true;
      }
      return stay_home;
    }

    /// returns string containing Activities schedule; does
    /// not include trailing newline
    public string schedule_to_string(int sim_day)
    {
      var builder = new StringBuilder();
      for (int p = 0; p < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++p)
      {
        var ai = (Activity_index)p;
        if (get_daily_activity_location(ai) != null)
        {
          builder.AppendLine(activity_lookup(ai) + ": ");
          builder.AppendLine(this.on_schedule[p] ? "+" : "-");
          builder.AppendLine(get_daily_activity_location_id(ai) + " ");
        }
      }
      return builder.ToString();
    }

    /**
     * Print the Activity schedule
     */
    public void print_schedule(int sim_day)
    {
      Utils.FRED_STATUS(0, this.schedule_to_string(sim_day));
    }

    /**
     * Print out information about this object
     */
    public void print()
    {
      Utils.FRED_STATUS(0, this.ToString());
    }

    public char get_deme_id()
    {
      Place p;
      if (this.is_traveling_outside)
      {
        p = get_stored_household();
      }
      else
      {
        p = get_household();
      }
      Utils. assert(p.is_household());
      return ((Household)p).get_deme_id();
    }

    public Place get_daily_activity_location(Activity_index i)
    {
      return this.link[i].get_place();
    }

    public List<Place> get_daily_activity_locations()
    {
      var faves = new List<Place>();
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        var place = get_daily_activity_location((Activity_index)i);
        if (place != null)
        {
          faves.Add(place);
        }
      }
      return faves;
    }

    public void set_daily_activity_location(Activity_index i, Place place)
    {
      if (place != null)
      {
        Utils.FRED_VERBOSE(1, "SET FAVORITE PLACE {0} to place {1} {2}", i, place.get_id(), place.get_label());
      }
      else
      {
        Utils.FRED_VERBOSE(1, "SET FAVORITE PLACE {0} to NULL", i);
      }
      // update link if necessary
      var old_place = get_daily_activity_location(i);
      Utils.FRED_VERBOSE(1, "old place {0}", old_place != null ? old_place.get_label() : "NULL");
      if (place != old_place)
      {
        if (old_place != null)
        {
          // remove old link
          // printf("remove old link\n");
          this.link[i].unenroll(this.myself);
        }
        if (place != null)
        {
          this.link[i].enroll(this.myself, place);
        }
      }
      Utils.FRED_VERBOSE(1, "set daily activity location finished");
    }

    public void set_household(Place p)
    {
      set_daily_activity_location(Activity_index.HOUSEHOLD_ACTIVITY, p);
    }

    public void set_neighborhood(Place p)
    {
      set_daily_activity_location(Activity_index.NEIGHBORHOOD_ACTIVITY, p);
    }

    public void reset_neighborhood()
    {
      set_daily_activity_location(Activity_index.NEIGHBORHOOD_ACTIVITY, this.home_neighborhood);
    }

    public void set_school(Place p)
    {
      set_daily_activity_location(Activity_index.SCHOOL_ACTIVITY, p);
    }

    public void set_classroom(Place p)
    {
      set_daily_activity_location(Activity_index.CLASSROOM_ACTIVITY, p);
    }

    public void set_workplace(Place p)
    {
      set_daily_activity_location(Activity_index.WORKPLACE_ACTIVITY, p);
    }

    public void set_office(Place p)
    {
      set_daily_activity_location(Activity_index.OFFICE_ACTIVITY, p);
    }

    public void set_hospital(Place p)
    {
      set_daily_activity_location(Activity_index.HOSPITAL_ACTIVITY, p);
    }

    public void set_ad_hoc(Place p)
    {
      set_daily_activity_location(Activity_index.AD_HOC_ACTIVITY, p);
    }

    public void move_to_new_house(Place house)
    {
      Utils.FRED_VERBOSE(1, "move_to_new_house person {0} house {1}", this.myself.get_id(), house.get_label());

      // everyone must have a household
      Utils.assert(house != null);

      bool is_former_group_quarters_resident = get_household().is_group_quarters();
      if (is_former_group_quarters_resident || house.is_group_quarters())
      {
        Utils.FRED_VERBOSE(1, "MOVE STARTED GROUP QUARTERS: person {0} profile {1} old-house {2} new-house {3}",
                     this.myself.get_id(), this.myself.get_profile(), get_household().get_label(), house.get_label());
      }
      // re-assign school and work activities
      change_household(house);

      if (is_former_group_quarters_resident || house.is_group_quarters())
      {
        // this will re-assign school and work activities
        this.update_profile();
        Utils.FRED_VERBOSE(1, "MOVE FINISHED GROUP QUARTERS: person {0} profile {1] old-house {2} new-house {3}",
                     this.myself.get_id(), this.myself.get_profile(), get_household().get_label(), house.get_label());
      }
    }

    public void change_household(Place place)
    {
      Utils.assert(place != null);
      set_household(place);
      set_neighborhood(place.get_patch().get_neighborhood());
    }

    public void change_school(Place place)
    {
      Utils.FRED_VERBOSE(1, "person {0} set school {1}", myself.get_id(), place != null ? place.get_label() : "NULL");
      set_school(place);
      Utils.FRED_VERBOSE(1, "set classroom to NULL");
      set_classroom(null);
      if (place != null)
      {
        Utils.FRED_VERBOSE(1, "assign classroom");
        assign_classroom();
      }
    }

    public void change_workplace(Place place, int include_office = 1)
    {
      Utils.FRED_VERBOSE(1, "person {0} set workplace {1}", this.myself.get_id(), place != null ? place.get_label() : "NULL");
      set_workplace(place);
      set_office(null);
      if (place != null)
      {
        if (include_office != 0)
        {
          assign_office();
        }
      }
    }

    public Place get_stored_household()
    {
      return this.stored_daily_activity_locations[(int)Activity_index.HOUSEHOLD_ACTIVITY];
    }

    /**
     * @return a pointer to this agent's permanent Household
     *
     * If traveling, this is the Person's permanent residence,
     * NOT the household being visited
     */
    public Place get_permanent_household()
    {
      if (this.is_traveling && this.is_traveling_outside)
      {
        return get_stored_household();
      }
      else if (Global.Enable_Hospitals && this.is_hospitalized)
      {
        return get_stored_household();
      }
      else
      {
        return get_household();
      }
    }

    /**
     * @return a pointer to this agent's Household
     */
    public Place get_household()
    {
      return get_daily_activity_location(Activity_index.HOUSEHOLD_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Neighborhood
     */
    public Place get_neighborhood()
    {
      return get_daily_activity_location(Activity_index.NEIGHBORHOOD_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's School
     */
    public Place get_school()
    {
      return get_daily_activity_location(Activity_index.SCHOOL_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Classroom
     */
    public Place get_classroom()
    {
      return get_daily_activity_location(Activity_index.CLASSROOM_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Workplace
     */
    public Place get_workplace()
    {
      return get_daily_activity_location(Activity_index.WORKPLACE_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Office
     */
    public Place get_office()
    {
      return get_daily_activity_location(Activity_index.OFFICE_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Hospital
     */
    public Place get_hospital()
    {
      return get_daily_activity_location(Activity_index.HOSPITAL_ACTIVITY);
    }

    /**
     * @return a pointer to this agent's Ad Hoc location
     */
    public Place get_ad_hoc()
    {
      return get_daily_activity_location(Activity_index.AD_HOC_ACTIVITY);
    }

    /**
     * Assign the agent to a School
     */
    public void assign_school()
    {
      int grade = this.myself.get_age();
      Utils.FRED_VERBOSE(1, "assign_school entered for person {0} age {1} grade {2}",
             this.myself.get_id(), this.myself.get_age(), grade);

      var hh = (Household)this.myself.get_household();

      if (hh == null)
      {
        if (Global.Enable_Hospitals && this.myself.is_hospitalized() && this.myself.get_permanent_household() != null)
        {
          hh = (Household)this.myself.get_permanent_household();
        }
      }
      Utils.assert(hh != null);
      var school = Global.Places.select_school(hh.get_county_index(), grade);
      Utils.assert(school != null);
      Utils.FRED_VERBOSE(1, "assign_school {0} selected for person {1} age {2}",
             school.get_label(), this.myself.get_id(), this.myself.get_age());
      set_school(school);
      set_classroom(null);
      assign_classroom();
      Utils.FRED_VERBOSE(1, "assign_school finished for person {0} age {1} school {2} classroom {3}",
                   this.myself.get_id(), this.myself.get_age(),
                   get_school().get_label(), get_classroom().get_label());
    }

    /**
     * Assign the agent to a Classroom
     */
    public void assign_classroom()
    {
      if (School.get_max_classroom_size() == 0)
      {
        return;
      }
      Utils.assert(get_school() != null && get_classroom() == null);
      Utils.FRED_VERBOSE(1, "assign classroom entered");

      var school = (School)get_school();
      var place = school.select_classroom_for_student(this.myself);
      if (place == null)
      {
        Utils.FRED_VERBOSE(0, "CLASSROOM_WARNING: assign classroom returns null: person {0} age {1} school {2}",
                    this.myself.get_id(), this.myself.get_age(), school.get_label());
      }
      set_classroom(place);
      Utils.FRED_VERBOSE(1, "assign classroom finished");
    }

    /**
     * Assign the agent to a Workplace
     */
    public void assign_workplace()
    {
      Neighborhood_Patch patch = null;
      if (Global.Enable_Hospitals && this.is_hospitalized)
      {
        patch = get_permanent_household().get_patch();
      }
      else
      {
        patch = get_household().get_patch();
      }
      Utils.assert(patch != null);
      var p = patch.select_workplace();
      change_workplace(p);
    }

    /**
     * Assign the agent to an Office
     */
    public void assign_office()
    {
      if (get_workplace() != null && get_office() == null && get_workplace().is_workplace()
     && Workplace.get_max_office_size() > 0)
      {
        var place = (Workplace)(get_workplace()).assign_office(this.myself);
        if (place == null)
        {
          Utils.FRED_VERBOSE(0, "OFFICE WARNING: No office assigned for person {0} workplace {1}", this.myself.get_id(),
                       get_workplace().get_id());
        }
        set_office(place);
      }
    }

    /**
     * Assign the agent to a Hospital
     */
    public void assign_hospital(Place place)
    {
      if (place == null)
      {
        Utils.FRED_VERBOSE(1, "Warning! No Hospital Place assigned for person %d\n", this.myself.get_id());
      }

      set_hospital(place);
    }

    /**
     * Assign the agent to an Ad Hoc Location
     * Note: this is meant to be assigned at will
     */
    public void assign_ad_hoc_place(Place place)
    {
      if (place == null)
      {
        Utils.FRED_VERBOSE(1, "Warning! No Ad Hoc Place assigned for person {0}", this.myself.get_id());
      }

      set_ad_hoc(place);
    }

    public void unassign_ad_hoc_place()
    {
      set_ad_hoc(null);
    }

    /**
     * Find a Primary Healthcare Facility and assign it to the agent
     * @param self the agent who needs to find a Primary care facility
     */
    public void assign_primary_healthcare_facility()
    {
      var tmp_hosp = Global.Places.get_random_primary_care_facility_matching_criteria(this.myself, Global.Enable_Health_Insurance && true, true);
      if (tmp_hosp != null)
      {
        this.primary_healthcare_facility = tmp_hosp;
        Place_List.increment_hospital_ID_current_assigned_size_map(tmp_hosp.get_id());
      }
      else
      {
        //Expand search radius
        tmp_hosp = Global.Places.get_random_primary_care_facility_matching_criteria(this.myself, Global.Enable_Health_Insurance, false);
        if (tmp_hosp != null)
        {
          this.primary_healthcare_facility = tmp_hosp;
          Place_List.increment_hospital_ID_current_assigned_size_map(tmp_hosp.get_id());
        }
        else
        {
          //Don't use health insurance even if it is enabled
          tmp_hosp = Global.Places.get_random_primary_care_facility_matching_criteria(this.myself, false, false);
          if (tmp_hosp != null)
          {
            this.primary_healthcare_facility = tmp_hosp;
            Place_List.increment_hospital_ID_current_assigned_size_map(tmp_hosp.get_id());
          }
        }
      }
    }

    public Hospital get_primary_healthcare_facility()
    {
      return this.primary_healthcare_facility;
    }

    /**
     * Update the agent's profile
     */
    public void update_profile()
    {
      int age = this.myself.get_age();

      // profiles for group quarters residents
      if (get_household().is_college())
      {
        if (this.profile != COLLEGE_STUDENT_PROFILE)
        {
          Utils.FRED_VERBOSE(1, "CHANGING PROFILE TO COLLEGE_STUDENT FOR PERSON {0} AGE {1}", this.myself.get_id(), age);
          this.profile = COLLEGE_STUDENT_PROFILE;
          change_school(null);
          change_workplace(Global.Places.get_household_ptr(get_household().get_index()).get_group_quarters_workplace());
        }
        return;
      }
      if (get_household().is_military_base())
      {
        if (this.profile != MILITARY_PROFILE)
        {
          Utils.FRED_VERBOSE(1, "CHANGING PROFILE TO MILITARY FOR PERSON {0} AGE {1} barracks {2}", this.myself.get_id(), age, get_household().get_label());
          this.profile = MILITARY_PROFILE;
          change_school(null);
          change_workplace(Global.Places.get_household_ptr(get_household().get_index()).get_group_quarters_workplace());
        }
        return;
      }
      if (get_household().is_prison())
      {
        if (this.profile != PRISONER_PROFILE)
        {
          Utils.FRED_VERBOSE(1, "CHANGING PROFILE TO PRISONER FOR PERSON {0} AGE {1} prison {2}", this.myself.get_id(), age, get_household().get_label());
          this.profile = PRISONER_PROFILE;
          change_school(null);
          change_workplace(Global.Places.get_household_ptr(get_household().get_index()).get_group_quarters_workplace());
        }
        return;
      }
      if (get_household().is_nursing_home())
      {
        if (this.profile != NURSING_HOME_RESIDENT_PROFILE)
        {
          Utils.FRED_VERBOSE(1, "CHANGING PROFILE TO NURSING HOME FOR PERSON {0} AGE {1} nursing_home {2}", this.myself.get_id(), age, get_household().get_label());
          this.profile = NURSING_HOME_RESIDENT_PROFILE;
          change_school(null);
          change_workplace(Global.Places.get_household_ptr(get_household().get_index()).get_group_quarters_workplace());
        }
        return;
      }

      // updates for students finishing college
      if (this.profile == COLLEGE_STUDENT_PROFILE && get_household().is_college() == false)
      {
        if (FredRandom.NextDouble() < 0.25)
        {
          // time to leave college for good
          change_school(null);
          change_workplace(null);
          // get a job
          this.profile = WORKER_PROFILE;
          assign_workplace();
          initialize_sick_leave();
          Utils.FRED_VERBOSE(1, "CHANGING PROFILE FROM COLLEGE STUDENT TO WORKER: id {0} age {1} sex {2} HOUSE {3} WORKPLACE {4} OFFICE {5}",
                       this.myself.get_id(), age, this.myself.get_sex(), get_household().get_label(), get_workplace().get_label(), get_office().get_label());
        }
        return;
      }

      // update based on age

      if (this.profile == PRESCHOOL_PROFILE && Global.SCHOOL_AGE <= age && age < Global.ADULT_AGE)
      {
        Utils.FRED_VERBOSE(1, "CHANGING PROFILE TO STUDENT FOR PERSON {0} AGE {1}", this.myself.get_id(), age);
        this.profile = STUDENT_PROFILE;
        change_school(null);
        change_workplace(null);
        assign_school();
        Utils.assert(get_school() != null && get_classroom() != null);
        Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} ENTERING SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                     this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                     get_school().get_orig_size(), get_classroom().get_label());
        Tracking_data.entered_school++;
        return;
      }

      // rules for current students
      if (this.profile == STUDENT_PROFILE)
      {
        if (get_school() != null)
        {
          var s = (School)get_school();
          // check if too old for current school
          if (s.get_max_grade() < age)
          {
            Utils.FRED_VERBOSE(1, "PERSON {0} AGE {1} TOO OLD FOR SCHOOL {2}", this.myself.get_id(), age, s.get_label());
            if (age < Global.ADULT_AGE)
            {
              // find another school
              change_school(null);
              assign_school();
              Utils.assert(get_school() != null && get_classroom() != null);
              Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} CHANGING TO SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                           this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                           get_school().get_orig_size(), get_classroom().get_label());
            }
            else
            {
              // time to leave school
              Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} LEAVING SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                           this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                           get_school().get_orig_size(), get_classroom().get_label());
              change_school(null);
              Tracking_data.left_school++;
              // get a job
              this.profile = WORKER_PROFILE;
              assign_workplace();
              initialize_sick_leave();
              Utils.FRED_VERBOSE(1, "CHANGING PROFILE FROM STUDENT TO WORKER: id {0} age {1} sex {2} WORKPLACE {3} OFFICE {4}",
                           this.myself.get_id(), age, this.myself.get_sex(), get_workplace().get_label(), get_office().get_label());
            }
            return;
          }

          // not too old for current school.
          // make sure we're in an appropriate classroom
          var c = (Classroom)get_classroom();
          Utils.assert(c != null);

          // check if too old for current classroom
          if (c.get_age_level() != age)
          {
            // stay in this school if (1) the school offers this grade and (2) the grade is not too overcrowded (<150%)
            if (s.get_students_in_grade(age) < 1.5 * s.get_orig_students_in_grade(age))
            {
              Utils.FRED_VERBOSE(1, "CHANGE_GRADES: PERSON {0} AGE {1} IN SCHOOL {2}",
                           this.myself.get_id(), age, s.get_label());
              // re-enroll in current school -- this will assign an appropriate grade and classroom.
              change_school(s);
              Utils.assert(get_school() != null && get_classroom() != null);
              Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} MOVE TO NEXT GRADE IN SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                           this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                           get_school().get_orig_size(), get_classroom().get_label());
            }
            else
            {
              Utils.FRED_VERBOSE(1, "CHANGE_SCHOOLS: PERSON {0} AGE {1} NO ROOM in GRADE IN SCHOOL {2}",
                           this.myself.get_id(), age, s.get_label());
              // find another school
              change_school(null);
              assign_school();
              Utils.assert(get_school() != null && get_classroom() != null);
              Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} CHANGE TO NEW SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                           this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                           get_school().get_orig_size(), get_classroom().get_label());
            }
            return;
          }

          // current school and classroom are ok
          Utils.assert(get_school() != null && get_classroom() != null);
          Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} STAYING IN SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                       this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                       get_school().get_orig_size(), get_classroom().get_label());
        }
        else
        {
          // no current school
          if (age < Global.ADULT_AGE)
          {
            Utils.FRED_VERBOSE(1, "ADD_A_SCHOOL: PERSON {0} AGE {1} HAS NO SCHOOL", this.myself.get_id(), age);
            change_school(null);
            assign_school();
            Utils.assert(get_school() != null && get_classroom() != null);
            Tracking_data.entered_school++;
            Utils.FRED_VERBOSE(1, "STUDENT_UPDATE PERSON {0} AGE {1} ADDING SCHOOL {2} SIZE {3} ORIG {4} CLASSROOM {5}",
                         this.myself.get_id(), age, get_school().get_label(), get_school().get_size(),
                         get_school().get_orig_size(), get_classroom().get_label());
          }
          else
          {
            // time to leave school
            Utils.FRED_VERBOSE(1, "LEAVING_SCHOOL: PERSON {0} AGE {1} NO FORMER SCHOOL", this.myself.get_id(), age);
            change_school(null);
            // get a job
            this.profile = WORKER_PROFILE;
            assign_workplace();
            initialize_sick_leave();
            Utils.FRED_VERBOSE(1, "CHANGING PROFILE FROM STUDENT TO WORKER: id {0} age {1} sex {2} WORKPLACE {3} OFFICE {4}",
                         this.myself.get_id(), age, this.myself.get_sex(), get_workplace().get_label(), get_office().get_label());
          }
        }
        return;
      }

      // conversion to civilian life
      if (this.profile == PRISONER_PROFILE)
      {
        change_school(null);
        change_workplace(null);
        this.profile = WORKER_PROFILE;
        assign_workplace();
        initialize_sick_leave();
        Utils.FRED_VERBOSE(1, "CHANGING PROFILE FROM PRISONER TO WORKER: id {0} age {1} sex {2} WORKPLACE {3} OFFICE {4}",
                     this.myself.get_id(), age, this.myself.get_sex(), get_workplace().get_label(), get_office().get_label());
        return;
      }

      // worker updates
      if (this.profile == WORKER_PROFILE)
      {
        if (get_workplace() == null)
        {
          assign_workplace();
          initialize_sick_leave();
          Utils.FRED_STATUS(1, "UPDATED  WORKER: id {0} age {1} sex {2}\n{3}", this.myself.get_id(),
                      age, this.myself.get_sex(), this.ToString());
        }
      }

      if (this.profile != RETIRED_PROFILE && Global.RETIREMENT_AGE <= age)
      {
        if (FredRandom.NextDouble() < 0.5)
        {
          Utils.FRED_STATUS(1, "CHANGING PROFILE TO RETIRED: id {0} age {1} sex {2}", this.myself.get_id(), age, this.myself.get_sex());
          Utils.FRED_STATUS(1, "to_string: {0}", this.ToString());
          // quit working
          if (is_teacher())
          {
            change_school(null);
          }
          change_workplace(null);
          this.profile = RETIRED_PROFILE;
          //initialize_sick_leave(); // no sick leave available if retired
          Utils.FRED_STATUS(1, "CHANGED BEHAVIOR PROFILE TO RETIRED: id {0} age {1} sex {2}\n{3}\n",
                      this.myself.get_id(), age, this.myself.get_sex(), this.ToString());
        }
        return;
      }
    }

    /**
     * withdraw from all activities
     */
    public void terminate()
    {
      if (this.get_travel_status())
      {
        if (this.is_traveling && !this.is_traveling_outside)
        {
          restore_daily_activity_locations();
        }
        Travel.terminate_person(this.myself);
      }

      //If the agent was hospitalized, restore original daily activity locations
      if (this.is_hospitalized)
      {
        restore_daily_activity_locations();
      }

      // decrease the population in county of residence
      int index = get_household().get_county_index();
      Global.Places.decrement_population_of_county_with_index(index, this.myself);

      // withdraw from society
      unenroll_from_daily_activity_locations();
    }

    /**
     * The agent begins traveling.  The daily activity locations for this agent are stored, and it gets a new schedule
     * based on the agent it is visiting.
     *
     * @param visited a pointer to the Person object being visited
     * @see store_daily_activity_locations()
     */
    public void start_traveling(Person visited)
    {
      //Can't travel if hospitalized
      if (Global.Enable_Hospitals && this.is_hospitalized)
      {
        return;
      }

      //Notify the household that someone is not going to be there
      if (Global.Report_Childhood_Presenteeism)
      {
        var my_hh = (Household)this.myself.get_household();
        if (my_hh != null)
        {
          my_hh.set_hh_schl_aged_chld_unemplyd_adlt_chng(true);
        }
      }

      if (visited == null)
      {
        this.is_traveling_outside = true;
      }
      else
      {
        store_daily_activity_locations();
        clear_daily_activity_locations();
        set_household(visited.get_household());
        set_neighborhood(visited.get_neighborhood());
        if (this.profile == WORKER_PROFILE)
        {
          set_workplace(visited.get_workplace());
          set_office(visited.get_office());
        }
      }
      this.is_traveling = true;
      Utils.FRED_STATUS(1, "start traveling: id = {0}", this.myself.get_id());
    }

    /**
     * The agent stops traveling and returns to its original daily activity locations
     * @see restore_daily_activity_locations()
     */
    public void stop_traveling()
    {
      if (!this.is_traveling_outside)
      {
        restore_daily_activity_locations();
      }
      this.is_traveling = false;
      this.is_traveling_outside = false;
      this.return_from_travel_sim_day = -1;
      if (Global.Report_Childhood_Presenteeism)
      {
        var my_hh = (Household)this.myself.get_household();
        if (my_hh != null)
        {
          my_hh.set_hh_schl_aged_chld_unemplyd_adlt_chng(true);
        }
      }
      Utils.FRED_STATUS(1, "stop traveling: id = {0}", this.myself.get_id());
    }

    /**
     * @return <code>true</code> if the agent is traveling, <code>false</code> otherwise
     */
    public bool get_travel_status()
    {
      return this.is_traveling;
    }

    public bool become_a_teacher(Place school)
    {
      bool success = false;
      Utils.FRED_VERBOSE(0, "become_a_teacher: person {0} age {1}", this.myself.get_id(), this.myself.get_age());
      // print(self);
      if (get_school() != null)
      {
        if (Global.Verbose > 0)
        {
          Utils.fred_abort("become_a_teacher: person {0} age {1} ineligible -- already goes to school {2} {3}",
           this.myself.get_id(), this.myself.get_age(), get_school().get_id(), get_school().get_label());
        }
        this.profile = STUDENT_PROFILE;
      }
      else
      {
        // set profile
        this.profile = TEACHER_PROFILE;
        // join the school
        Utils.FRED_VERBOSE(0, "set school to {0}", school.get_label());
        set_school(school);
        set_classroom(null);
        success = true;
      }

      // withdraw from this workplace and any associated office
      var workplace = this.myself.get_workplace();
      Utils.FRED_VERBOSE(0, "leaving workplace {0} {1}", workplace.get_id(), workplace.get_label());
      change_workplace(null);
      Utils.FRED_VERBOSE(0, "become_a_teacher finished for person {0} age {1}", this.myself.get_id(),
            this.myself.get_age());
      // print(self);
      return success;
    }

    /**
     * Return the number of other agents in an agent's neighborhood, school,
     * and workplace.
     */
    public int get_degree()
    {
      int degree;
      int n;
      degree = 0;
      n = get_group_size(Activity_index.NEIGHBORHOOD_ACTIVITY);
      if (n > 0)
      {
        degree += n - 1;
      }
      n = get_group_size(Activity_index.SCHOOL_ACTIVITY);
      if (n > 0)
      {
        degree += n - 1;
      }
      n = get_group_size(Activity_index.WORKPLACE_ACTIVITY);
      if (n > 0)
      {
        degree += n - 1;
      }
      n = get_group_size(Activity_index.HOSPITAL_ACTIVITY);
      if (n > 0)
      {
        degree += n - 1;
      }
      n = get_group_size(Activity_index.AD_HOC_ACTIVITY);
      if (n > 0)
      {
        degree += n - 1;
      }
      return degree;
    }

    public int get_group_size(Activity_index index)
    {
      int size = 0;
      if (get_daily_activity_location(index) != null)
      {
        size = get_daily_activity_location(index).get_size();
      }
      return size;
    }

    public bool is_sick_leave_available()
    {
      return this.sick_leave_available;
    }

    public int get_sick_days_absent()
    {
      return this.my_sick_days_absent;
    }

    int get_sick_days_present()
    {
      return this.my_sick_days_present;
    }

    public static void update(int sim_day)
    {
      Utils.FRED_STATUS(1, "Activities update entered");

      // decide if this is a weekday:
      is_weekday = Date.is_weekday();

      if (Global.Enable_HAZEL)
      {
        Global.Daily_Tracker.set_index_key_pair(sim_day, SEEK_HC, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, PRIMARY_HC_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, HC_ACCEP_INS_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, HC_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, MEDICARE_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, ASTHMA_HC_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, DIABETES_HC_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, HTN_HC_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, MEDICAID_UNAV, 0);
        Global.Daily_Tracker.set_index_key_pair(sim_day, MEDICARE_UNAV, 0);
        Global.Daily_Tracker.increment_index_key_pair(sim_day, PRIVATE_UNAV, 1);
        Global.Daily_Tracker.increment_index_key_pair(sim_day, UNINSURED_UNAV, 1);
      }

      // print out absenteeism/presenteeism counts
      Utils.FRED_CONDITIONAL_VERBOSE(1, sim_day > 0,
             "DAY {0} ABSENTEEISM: work absent {1} present {2} {3}  school absent {4} present {5} {6}",
             sim_day - 1, Tracking_data.daily_sick_days_absent, Tracking_data.daily_sick_days_present,
             Tracking_data.daily_sick_days_absent / (1 + Tracking_data.daily_sick_days_absent +
                                 Tracking_data.daily_sick_days_present),
             Tracking_data.daily_school_sick_days_absent, Tracking_data.daily_school_sick_days_present,
             Tracking_data.daily_school_sick_days_absent /
             (1 + Tracking_data.daily_school_sick_days_absent + Tracking_data.daily_school_sick_days_present));

      // keep track of global activity counts
      Tracking_data.daily_sick_days_present = 0;
      Tracking_data.daily_sick_days_absent = 0;
      Tracking_data.daily_school_sick_days_present = 0;
      Tracking_data.daily_school_sick_days_absent = 0;

      // print school change activities
      if (Tracking_data.entered_school + Tracking_data.left_school > 0)
      {
        Console.WriteLine("DAY {0} ENTERED_SCHOOL {1} LEFT_SCHOOL {2}",
               sim_day, Tracking_data.entered_school, Tracking_data.left_school);
        Tracking_data.entered_school = 0;
        Tracking_data.left_school = 0;
      }

      Utils.FRED_STATUS(1, "Activities update completed");
    }

    public static void end_of_run()
    {
      if (Global.Report_Presenteeism || Global.Report_Childhood_Presenteeism)
      {
        double mean_sick_days_used = Tracking_data.total_employees_taking_sick_leave == 0
          ? 0.0
          : (Tracking_data.total_employees_days_used / Tracking_data.total_employees_taking_sick_leave);
        Utils.FRED_STATUS(0, "Sick Leave Report: {0} Employees Used Sick Leave for an average of {1} days each",
          Tracking_data.total_employees_taking_sick_leave, mean_sick_days_used);
      }
    }

    public static void before_run()
    {
      if (Global.Report_Presenteeism)
      {
        if (!Enable_default_sick_behavior)
        {
          if (Sick_leave_dist_method == WP_SIZE_DIST)
          {
            for (int i = 0; i < Workplace.get_workplace_size_group_count(); ++i)
            {
              if (i == 0)
              {
                Utils.FRED_STATUS(0, "Employees in Workplace[0 - {0}] with sick leave: {1}",
                            Workplace.get_workplace_size_max_by_group_id(i),
                            Tracking_data.employees_with_sick_leave[i]);
                Utils.FRED_STATUS(0, "Employees in Workplace[0 - {0}] without sick leave: {1}",
                            Workplace.get_workplace_size_max_by_group_id(i),
                            Tracking_data.employees_without_sick_leave[i]);
              }
              else
              {
                Utils.FRED_STATUS(0, "Employees in Workplace[{0} - {1}] with sick leave: {2}",
                            Workplace.get_workplace_size_max_by_group_id(i - 1) + 1,
                            Workplace.get_workplace_size_max_by_group_id(i),
                            Tracking_data.employees_with_sick_leave[i]);
                Utils.FRED_STATUS(0, "Employees in Workplace[{0} - {1}] without sick leave: {2}",
                            Workplace.get_workplace_size_max_by_group_id(i - 1) + 1,
                            Workplace.get_workplace_size_max_by_group_id(i),
                            Tracking_data.employees_without_sick_leave[i]);
              }
            }
          }
        }
      }

      if (Global.Report_Childhood_Presenteeism)
      {
        Global.Places.setup_household_income_quartile_sick_days();
      }
    }

    public void set_profile(char _profile)
    {
      this.profile = _profile;
    }

    public bool is_teacher()
    {
      return this.profile == TEACHER_PROFILE;
    }

    public bool is_student()
    {
      return this.profile == STUDENT_PROFILE;
    }

    public bool is_college_student()
    {
      return this.profile == COLLEGE_STUDENT_PROFILE;
    }

    public bool is_prisoner()
    {
      return this.profile == PRISONER_PROFILE;
    }

    public bool is_college_dorm_resident()
    {
      return this.profile == COLLEGE_STUDENT_PROFILE;
    }

    public bool is_military_base_resident()
    {
      return this.profile == MILITARY_PROFILE;
    }

    public bool is_nursing_home_resident()
    {
      return this.profile == NURSING_HOME_RESIDENT_PROFILE;
    }

    public bool is_hospital_staff()
    {
      bool ret_val = false;
      if (this.profile == WORKER_PROFILE || this.profile == WEEKEND_WORKER_PROFILE)
      {
        if (get_workplace() != null && get_household() != null)
        {
          if (get_workplace().is_hospital() &&
             !get_household().is_hospital())
          {
            ret_val = true;
          }
        }
      }

      return ret_val;
    }

    public bool is_prison_staff()
    {
      bool ret_val = false;
      if (this.profile == WORKER_PROFILE || this.profile == WEEKEND_WORKER_PROFILE)
      {
        if (get_workplace() != null && get_household() != null)
        {
          if (get_workplace().is_prison() &&
             !get_household().is_prison())
          {
            ret_val = true;
          }
        }
      }

      return ret_val;
    }

    public bool is_college_dorm_staff()
    {
      bool ret_val = false;

      if (this.profile == WORKER_PROFILE || this.profile == WEEKEND_WORKER_PROFILE)
      {
        if (get_workplace() != null && get_household() != null)
        {
          if (get_workplace().is_college() &&
             !get_household().is_college())
          {
            ret_val = true;
          }
        }
      }

      return ret_val;
    }

    public bool is_military_base_staff()
    {
      bool ret_val = false;

      if (this.profile == WORKER_PROFILE || this.profile == WEEKEND_WORKER_PROFILE)
      {
        if (get_workplace() != null && get_household() != null)
        {
          if (get_workplace().is_military_base() &&
             !get_household().is_military_base())
          {
            ret_val = true;
          }
        }
      }

      return ret_val;
    }

    public bool is_nursing_home_staff()
    {
      bool ret_val = false;

      if (this.profile == WORKER_PROFILE || this.profile == WEEKEND_WORKER_PROFILE)
      {
        if (get_workplace() != null && get_household() != null)
        {
          if (get_workplace().is_nursing_home() &&
             !get_household().is_nursing_home())
          {
            ret_val = true;
          }
        }
      }

      return ret_val;
    }

    public static void initialize_static_variables()
    {
      if (is_initialized)
      {
        return;
      }

      Utils.FRED_STATUS(0, "initialize() entered");

      int temp_int = 0;
      FredParameters.GetParameter("enable_default_sick_behavior", ref temp_int);
      Enable_default_sick_behavior = temp_int != 0;
      FredParameters.GetParameter("sick_day_prob", ref Default_sick_day_prob);
      FredParameters.GetParameter("SLA_absent_prob", ref SLA_absent_prob);
      FredParameters.GetParameter("SLU_absent_prob", ref SLU_absent_prob);
      FredParameters.GetParameter("wp_small_mean_sl_days_available", ref WP_small_mean_sl_days_available);
      FredParameters.GetParameter("wp_large_mean_sl_days_available", ref WP_large_mean_sl_days_available);
      FredParameters.GetParameter("wp_size_cutoff_sl_exception", ref WP_size_cutoff_sl_exception);

      FredParameters.GetParameter("flu_days", ref Flu_days);
      FredParameters.GetParameter("prob_of_visiting_hospitalized_housemate", ref Hospitalization_visit_housemate_prob);

      FredParameters.GetParameter("sick_leave_dist_method", ref Sick_leave_dist_method);

      if (!Enable_default_sick_behavior)
      {
        if (Sick_leave_dist_method == WP_SIZE_DIST)
        {
          WP_size_sl_prob_vec = FredParameters.GetParameterList<double>("wp_size_sl_prob_vec");
          Utils.assert(WP_size_sl_prob_vec.Count == Workplace.get_workplace_size_group_count());
          for (int i = 0; i < Workplace.get_workplace_size_group_count(); ++i)
          {
            Tracking_data.employees_with_sick_leave.Add(0);
            Tracking_data.employees_without_sick_leave.Add(0);
            Tracking_data.employees_days_used.Add(0);
            Tracking_data.employees_taking_day_off.Add(0);
            Tracking_data.employees_sick_leave_days_used.Add(0);
            Tracking_data.employees_taking_sick_leave_day_off.Add(0);
            Tracking_data.employees_sick_days_present.Add(0);
          }
        }
        else if (Sick_leave_dist_method == HH_INCOME_QTILE_DIST)
        {
          HH_income_qtile_sl_prob_vec = FredParameters.GetParameterList<double>("hh_income_qtile_sl_prob_vec");
          Utils.assert(HH_income_qtile_sl_prob_vec.Count == 4);
          for (int i = 0; i < 4; ++i)
          {
            Tracking_data.employees_with_sick_leave.Add(0);
            Tracking_data.employees_without_sick_leave.Add(0);
            Tracking_data.employees_days_used.Add(0);
            Tracking_data.employees_taking_day_off.Add(0);
            Tracking_data.employees_sick_leave_days_used.Add(0);
            Tracking_data.employees_taking_sick_leave_day_off.Add(0);
            Tracking_data.employees_sick_days_present.Add(0);
          }
        }
        else
        {
          Utils.fred_abort("Invalid sick_leave_dist_method: {0}", Sick_leave_dist_method);
        }
      }

      if (Global.Enable_Hospitals)
      {
        Hospitalization_prob = new Age_Map("Hospitalization Probability");
        Hospitalization_prob.read_from_input("hospitalization_prob");
        Outpatient_healthcare_prob = new Age_Map("Outpatient Healthcare Probability");
        Outpatient_healthcare_prob.read_from_input("outpatient_healthcare_prob");
      }

      if (Global.Enable_HAZEL)
      {
        FredParameters.GetParameter("HAZEL_seek_hc_ramp_up_days", ref HAZEL_seek_hc_ramp_up_days);
        FredParameters.GetParameter("HAZEL_seek_hc_ramp_up_mult", ref HAZEL_seek_hc_ramp_up_mult);
      }

      if (Global.Report_Childhood_Presenteeism)
      {

        FredParameters.GetParameter("standard_sicktime_allocated_per_child", ref Standard_sicktime_allocated_per_child);

        int count_has_school_age = 0;
        int count_has_school_age_and_unemployed_adult = 0;

        //Households with school-age children and at least one unemployed adult
        int number_places = Global.Places.get_number_of_households();
        for (int p = 0; p < number_places; ++p)
        {
          var h = Global.Places.get_household_ptr(p);
          if (h.get_children() == 0)
          {
            continue;
          }
          if (h.has_school_aged_child())
          {
            count_has_school_age++;
          }
          if (h.has_school_aged_child_and_unemployed_adult())
          {
            count_has_school_age_and_unemployed_adult++;
          }
        }

        Sim_based_prob_stay_home_not_needed = count_has_school_age_and_unemployed_adult / count_has_school_age;
      }
      is_initialized = true;
    }

    public char get_profile()
    {
      return this.profile;
    }

    public int get_grade()
    {
      return this.grade;
    }

    public void set_grade(int n)
    {
      this.grade = n;
    }

    public int get_visiting_health_status(Place place, int sim_day, int disease_id)
    {
      // assume we are not visiting this place today
      int status = 0;

      // traveling abroad?
      if (this.is_traveling_outside)
      {
        return status;
      }

      if (sim_day > this.schedule_updated)
      {
        // get list of places to visit today
        update_schedule(sim_day);

        // noninfectious people stay in neighborhood
        set_neighborhood(this.home_neighborhood);
      }

      // see if the given place is on my schedule today
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        if (this.on_schedule[i] && get_daily_activity_location((Activity_index)i) == place)
        {
          if (this.myself.is_susceptible(disease_id))
          {
            status = 1;
            break;
          }
          else if (this.myself.is_infectious(disease_id))
          {
            status = 2;
            break;
          }
          else
          {
            status = 3;
            break;
          }
        }
      }
      return status;
    }

    public void set_return_from_travel_sim_day(int sim_day)
    {
      this.return_from_travel_sim_day = sim_day;
    }

    public int get_return_from_travel_sim_day()
    {
      return this.return_from_travel_sim_day;
    }

    public void create_network_link_to(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].create_link_to(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void create_network_link_from(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].create_link_from(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void destroy_network_link_to(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].destroy_link_to(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void destroy_network_link_from(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].destroy_link_from(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void add_network_link_to(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].add_link_to(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void add_network_link_from(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].add_link_from(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void delete_network_link_to(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].delete_link_to(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void delete_network_link_from(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].delete_link_from(person);
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public void join_network(Network network)
    {
      Utils.FRED_VERBOSE(0, "JOINING NETWORK: id = {0}", this.myself.get_id());
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return;
        }
      }
      var network_link = new Person_Network_Link(this.myself, network);
      this.networks.Add(network_link);
    }

    public bool is_enrolled_in_network(Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return true;
        }
      }
      return false;
    }

    public void print_network(TextWriter fp, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].print(fp);
        }
      }
    }

    public bool is_connected_to(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return this.networks[i].is_connected_to(person);
        }
      }
      Utils.fred_abort("network not found");
      return false;
    }

    public bool is_connected_from(Person person, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return this.networks[i].is_connected_from(person);
        }
      }
      Utils.fred_abort("network not found");
      return false;
    }

    public int get_out_degree(Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return this.networks[i].get_out_degree();
        }
      }
      Utils.fred_abort("network not found");
      return 0;
    }

    public int get_in_degree(Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return this.networks[i].get_in_degree();
        }
      }
      Utils.fred_abort("network not found");
      return 0;
    }

    public void clear_network(Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          this.networks[i].clear();
          return;
        }
      }
      Utils.fred_abort("network not found");
    }

    public Person get_end_of_link(int n, Network network)
    {
      int size = this.networks.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.networks[i].get_network() == network)
        {
          return this.networks[i].get_end_of_link(n);
        }
      }
      Utils.fred_abort("network not found");
      return null;
    }

    public void setup(Person self, Place house, Place school, Place work)
    {
      Utils.FRED_VERBOSE(1, "ACTIVITIES_SETUP: person {0} age {1} household {2}",
         this.myself.get_id(), this.myself.get_age(), house.get_label());

      this.myself = self;
      clear_daily_activity_locations();

      Utils.FRED_VERBOSE(1, "set household {0}", get_label_for_place(house));
      set_household(house);

      Utils.FRED_VERBOSE(1, "set school {0}", get_label_for_place(school));
      set_school(school);

      Utils.FRED_VERBOSE(1, "set workplace {0}", get_label_for_place(work));
      set_workplace(work);
      Utils.FRED_VERBOSE(1, "set workplace {0} ok", get_label_for_place(work));

      // increase the population in county of residence
      int index = get_household().get_county_index();
      Global.Places.increment_population_of_county_with_index(index, this.myself);

      // get the neighborhood from the household
      set_neighborhood(get_household().get_patch().get_neighborhood());
      Utils.FRED_VERBOSE(1, "ACTIVITIES_SETUP: person {0} neighborhood {1} {2}", this.myself.get_id(),
             get_neighborhood().get_id(), get_neighborhood().get_label());
      Utils.FRED_CONDITIONAL_VERBOSE(0, get_neighborhood() == null,
             "Help! NO NEIGHBORHOOD for person {0} house {1}", this.myself.get_id(), get_household().get_id());
      this.home_neighborhood = get_neighborhood();

      // assign profile
      assign_initial_profile();
      Utils.FRED_VERBOSE(1, "set profile ok");

      // need to set the daily schedule
      this.schedule_updated = -1;
      this.is_traveling = false;
      this.is_traveling_outside = false;

      // sick leave variables
      this.my_sick_days_absent = 0;
      this.my_sick_days_present = 0;
      this.my_sick_leave_decision_has_been_made = false;
      this.my_sick_leave_decision = false;
      this.sick_days_remaining = 0.0;
      this.sick_leave_available = false;

      if (self.lives_in_group_quarters())
      {
        int day = Global.Simulation_Day;
        // no pregnancies in group_quarters
        if (self.get_demographics().is_pregnant())
        {
          Console.WriteLine("GQ CANCELS PREGNANCY: today {0} person {1} age {2} due {3}",
           day, self.get_id(), self.get_age(), self.get_demographics().get_maternity_sim_day());
          self.get_demographics().cancel_pregnancy(self);
        }
        // cancel any planned pregnancy
        if (day <= self.get_demographics().get_conception_sim_day())
        {
          Console.WriteLine("GQ CANCELS PLANNED CONCEPTION: today {0} person {1} age {2} conception {3}",
           day, self.get_id(), self.get_age(), self.get_demographics().get_conception_sim_day());
          self.get_demographics().cancel_conception(self);
        }
      }
      Utils.FRED_VERBOSE(1, "Activity::setup finished for person {0}", self.get_id());
    }

    private void clear_daily_activity_locations()
    {
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        if (this.link[i].is_enrolled())
        {
          this.link[i].unenroll(this.myself);
        }
        Utils.assert(this.link[i].get_place() == null);
      }
    }

    private void enroll_in_daily_activity_location(Activity_index i)
    {
      var place = get_daily_activity_location(i);
      if (place != null)
      {
        this.link[i].enroll(this.myself, place);
      }
    }

    private void enroll_in_daily_activity_locations()
    {
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        enroll_in_daily_activity_location((Activity_index)i);
      }
    }

    internal void update_enrollee_index(Mixing_Group mixing_group, int new_index)
    {
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        if (mixing_group == get_daily_activity_location((Activity_index)i))
        {
          Utils.FRED_VERBOSE(1, "update_enrollee_index for person {0} i {1} new_index {2}", this.myself.get_id(), i, new_index);
          this.link[i].update_enrollee_index(new_index);
          return;
        }
      }

      Utils.FRED_VERBOSE(0, "update_enrollee_index: person {0} place {1} {2} not found in daily activity locations: ",
                   this.myself.get_id(), mixing_group.get_id(), mixing_group.get_label());

      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        var place = get_daily_activity_location((Activity_index)i);
        Console.Write("{0} ", place != null ? place.get_label() : "NULL");
      }
      Console.WriteLine();
    }

    private void unenroll_from_daily_activity_location(int i)
    {
      var place = get_daily_activity_location((Activity_index)i);
      if (place != null)
      {
        this.link[i].unenroll(this.myself);
      }
    }

    private void unenroll_from_daily_activity_locations()
    {
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        unenroll_from_daily_activity_location(i);
      }
      clear_daily_activity_locations();
    }

    private void store_daily_activity_locations()
    {
      this.stored_daily_activity_locations = new Place[(int)Activity_index.DAILY_ACTIVITY_LOCATIONS];
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        this.stored_daily_activity_locations[i] = get_daily_activity_location((Activity_index)i);
      }
    }

    private void restore_daily_activity_locations()
    {
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        set_daily_activity_location((Activity_index)i, this.stored_daily_activity_locations[i]);
      }
      this.stored_daily_activity_locations = null;
    }

    private int get_daily_activity_location_id(Activity_index i)
    {
      return get_daily_activity_location(i) == null ? -1 : get_daily_activity_location(i).get_id();
    }

    private string get_daily_activity_location_label(Activity_index i)
    {
      return get_daily_activity_location(i) == null
        ? "NULL"
        : get_daily_activity_location(i).get_label();
    }

    internal bool is_present(int sim_day, Place place)
    {
      // not here if traveling abroad
      if (this.is_traveling_outside)
      {
        return false;
      }

      // update list of places to visit today if not already done
      if (sim_day > this.schedule_updated)
      {
        update_schedule(sim_day);
      }

      // see if this place is on the list
      for (int i = 0; i < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++i)
      {
        if (get_daily_activity_location((Activity_index)i) == place && this.on_schedule[i])
        {
          return true;
        }
      }
      return false;
    }

    private static int get_index_of_sick_leave_dist(Person per)
    {
      if (!Enable_default_sick_behavior)
      {
        if (Sick_leave_dist_method == WP_SIZE_DIST)
        {
          int workplace_size = 0;
          if (per.get_workplace() != null)
          {
            workplace_size = per.get_workplace().get_size();
          }
          else
          {
            if (per.is_teacher())
            {
              workplace_size = per.get_school().get_staff_size();
            }
          }

          // is sick leave available?
          if (workplace_size > 0)
          {
            for (int i = 0; i < Workplace.get_workplace_size_group_count(); ++i)
            {
              if (workplace_size <= Workplace.get_workplace_size_max_by_group_id(i))
              {
                return i;
              }
            }
          }
        }
        else if (Sick_leave_dist_method == HH_INCOME_QTILE_DIST)
        {
          var hh = (Household)per.get_household();
          switch (hh.get_income_quartile())
          {
            case Global.Q1:
              return 0;
            case Global.Q2:
              return 1;
            case Global.Q3:
              return 2;
            case Global.Q4:
              return 3;
          }
        }
      }
      return -1;
    }

    private string get_label_for_place(Place place)
    {
      return place == null ? "null" : place.get_label();
    }

    public override bool Equals(object obj)
    {
      if (typeof(object) != typeof(Activities))
      {
        return false;
      }

      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      var ss = new StringBuilder();
      ss.AppendFormat("Activities for person {0}:", this.myself.get_id());
      for (int p = 0; p < (int)Activity_index.DAILY_ACTIVITY_LOCATIONS; ++p)
      {
        if (get_daily_activity_location((Activity_index)p) != null)
        {
          ss.AppendFormat("{0}:", activity_lookup((Activity_index)p));
          ss.AppendFormat("{0} ", get_daily_activity_location_id((Activity_index)p));
        }
      }
      return ss.ToString();
    }
  }
}
