using System.Collections.Generic;

namespace Fred
{
  public class ActivitiesTrackingData
  {
    public int daily_sick_days_present;
    public int daily_sick_days_absent;
    public int daily_school_sick_days_present;
    public int daily_school_sick_days_absent;
    
    // Run-wide totals (cumulative)
    public int total_employees_days_used;
    public int total_employees_taking_day_off;
    public int total_employees_sick_leave_days_used;
    public int total_employees_taking_sick_leave;
    public int total_employees_sick_days_present;
    public int entered_school;
    public int left_school;

    public ActivitiesTrackingData()
    {
      this.employees_with_sick_leave = new List<int>();
      this.employees_without_sick_leave = new List<int>();
      this.employees_days_used = new List<int>();
      this.employees_taking_day_off = new List<int>();
      this.employees_sick_leave_days_used = new List<int>();
      this.employees_taking_sick_leave_day_off = new List<int>();
      this.employees_sick_days_present = new List<int>();
    }

    // Statistics for presenteeism study
    // These are cumulative totals by sick leave groupings (e.g. workplace size level)
    public List<int> employees_with_sick_leave { get; }
    public List<int> employees_without_sick_leave { get; }
    public List<int> employees_days_used { get; }
    public List<int> employees_taking_day_off { get; }
    public List<int> employees_sick_leave_days_used { get; }
    public List<int> employees_taking_sick_leave_day_off { get; }
    public List<int> employees_sick_days_present { get; }
  }
}
