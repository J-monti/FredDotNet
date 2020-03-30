using System;

namespace Fred
{
  public class VaccineDose
  {
    int days_between_doses;       // Number of days until the next dose is administered
    AgeMap efficacy;            // Age specific efficacy of vaccine, does the dose provide immunity
    AgeMap efficacy_delay;      // Age specific delay to efficacy, how long does it take to develop immunity
    AgeMap efficacy_duration;  // Age specific duration of immunity

    public VaccineDose(AgeMap _efficacy, AgeMap _efficacy_delay,
         AgeMap _efficacy_duration, int _days_between_doses)
    {
      efficacy = _efficacy;
      efficacy_delay = _efficacy_delay;
      efficacy_duration = _efficacy_duration;
      days_between_doses = _days_between_doses;
    }

    public void print() {
      //cout << "Time Between Doses:\t " << days_between_doses << "\n";
      Console.WriteLine("Time Between Doses:\t {0}", days_between_doses);
      efficacy.Print();
      efficacy_delay.Print();
      efficacy_duration.Print();
    }

    public bool is_within_age(double real_age) {
      double eff = efficacy.find_value(real_age);
      // printf("age = %.1f  eff = %f\n", real_age, eff);
      if(eff != 0.0){
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
  }
}