using System;

namespace Fred
{
  public class Vaccine_Dose
  {
    int days_between_doses;       // Number of days until the next dose is administered
    Age_Map efficacy;            // Age specific efficacy of vaccine, does the dose provide immunity
    Age_Map efficacy_delay;      // Age specific delay to efficacy, how long does it take to develop immunity
    Age_Map efficacy_duration;  // Age specific duration of immunity

    public Vaccine_Dose(Age_Map _efficacy, Age_Map _efficacy_delay,
         Age_Map _efficacy_duration, int _days_between_doses)
    {
      efficacy = _efficacy;
      efficacy_delay = _efficacy_delay;
      efficacy_duration = _efficacy_duration;
      days_between_doses = _days_between_doses;
    }

    public void print()
    {
      //cout << "Time Between Doses:\t " << days_between_doses << "\n";
      Console.WriteLine("Time Between Doses:\t {0}", days_between_doses);
      efficacy.print();
      efficacy_delay.print();
      efficacy_duration.print();
    }

    public bool is_within_age(double real_age)
    {
      double eff = efficacy.find_value(real_age);
      // printf("age = %.1f  eff = %f\n", real_age, eff);
      if (eff != 0.0)
      {
        return true;
      }
      return false;
    }

    public double get_duration_of_immunity(double real_age)
    {
      double expected_duration = efficacy_duration.find_value(real_age);
      // select a value from an exponential distribution with mean expected_duration
      double actual_duration = 0.0;
      if (expected_duration > 0.0)
      {
        actual_duration = FredRandom.Exponential(1.0 / expected_duration);
      }
      return actual_duration;
    }

    public Age_Map get_efficacy_map() { return efficacy; }
    public Age_Map get_efficacy_delay_map() { return efficacy_delay; }

    public int get_days_between_doses() { return days_between_doses; }

    public double get_efficacy(double real_age) { return efficacy.find_value(real_age); }

    public double get_efficacy_delay(double real_age)
    {
      return efficacy_delay.find_value(real_age);
    }
  }
}