using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Health
  {
    private int vaccination_day;              // On which day did you get the vaccine
    private int vaccination_effective_day;    // On which day is the vaccine effective
    private int vaccination_immunity_loss_day;  // On which day does the vaccine lose effectiveness
    private Vaccine vaccine;                 // Which vaccine did you take
    private int current_dose;                 // Current Dose that the agent is on
    private int days_to_next_dose;            // How long between doses
    private Person person;                  // The person object this belongs to.
    private Vaccine_Manager vaccine_manager; // Which manager did the vaccine come from?
    private bool effective;

    public Vaccine_Health() { }
    public Vaccine_Health(int _vaccination_day, Vaccine _vaccine, double _age,
       Person _person, Vaccine_Manager _vaccine_manager)
    {
      vaccine = _vaccine;
      vaccination_day = _vaccination_day;
      person = _person;
      double efficacy = vaccine.get_dose(0).get_efficacy(_age);
      double efficacy_delay = vaccine.get_dose(0).get_efficacy_delay(_age);
      double efficacy_duration = vaccine.get_dose(0).get_duration_of_immunity(_age);
      vaccine_manager = _vaccine_manager;
      vaccination_immunity_loss_day = -1;
      vaccination_effective_day = -1;
      effective = false;

      // decide on efficacy
      if (FredRandom.NextDouble() < efficacy)
      {
        vaccination_effective_day = Convert.ToInt32(vaccination_day + efficacy_delay);
        vaccination_immunity_loss_day = Convert.ToInt32(vaccination_effective_day + 1 + efficacy_duration);
      }

      current_dose = 0;
      days_to_next_dose = -1;
      if (Global.Debug > 1)
      {
        Console.WriteLine($"Agent: {person.get_id()} took dose {current_dose} on day {vaccination_day}");
      }
      if (vaccine.get_number_doses() > 1)
      {
        days_to_next_dose = vaccination_day + vaccine.get_dose(0).get_days_between_doses();
      }
    }

    // Access Members
    public int get_vaccination_day() { return vaccination_day; }
    public int get_vaccination_effective_day() { return vaccination_effective_day; }
    public int is_effective() { if (vaccination_effective_day != -1) return 1; else return 0; }
    public Vaccine get_vaccine() { return vaccine; }
    public int get_current_dose() { return current_dose; }
    public int get_days_to_next_dose() { return days_to_next_dose; }
    public Vaccine_Manager get_vaccine_manager() { return vaccine_manager; }
    // Modifiers
    public void set_vaccination_day(int day)
    {
      if (vaccination_day == -1)
      {
        vaccination_day = day;
      }
      else
      {
        //This is an error, but it will not stop a run, only pring a Warning.
        Utils.FRED_STATUS(0, "WARNING! Vaccination Status, setting vaccine day of someone who has already been vaccinated\n");
      }
    }
    public bool isEffective() { return effective; }

    public void print() { Console.WriteLine("Vaccine Status"); }

    public void printTrace()
    {
      if (Global.VaccineTracefp != null)
      {
        Global.VaccineTracefp.WriteLine(" vaccday {0.#####} age {1} iseff {2.###} effday {3.#####} currentdose {0.###}",
          vaccination_day, person.get_real_age(), is_effective(), vaccination_effective_day, current_dose);
        Global.VaccineTracefp.Flush();
      }
    }

    public void update(int day, double age)
    {
      // First check for immunity 
      if (is_effective() != 0)
      {
        if (day == vaccination_effective_day)
        {
          var disease = Global.Diseases.get_disease(0);
          if (person.is_infected(disease.get_id()) == false)
          {
            person.become_immune(disease);
            effective = true;
            if (Global.Verbose > 0)
            {
              Console.WriteLine($"Agent {person.get_id()} has become immune from dose {current_dose} on day {day}");
            }
          }
          else
          {
            if (Global.Verbose > 0)
            {
              Console.WriteLine($"Agent {person.get_id()} was already infected so did not become immune from dose {current_dose} on day {day}");
            }
          }
        }
        if (day == vaccination_immunity_loss_day)
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Agent {person.get_id()} became immune on day {vaccination_effective_day} and lost immunity on day {day}");
          }
          int disease_id = 0;
          person.become_susceptible_by_vaccine_waning(disease_id);
          effective = false;
        }
      }

      // Next check on dose
      // Even immunized people get another dose
      // If they are to get another dose, then put them on the queue based on dose priority

      if (current_dose < vaccine.get_number_doses() - 1)
      {   // Are we done with the dosage?
          // Get the dosage policy from the manager
        if (day >= days_to_next_dose)
        {
          current_dose++;
          days_to_next_dose = day + vaccine.get_dose(current_dose).get_days_between_doses();
          int vaccine_dose_priority = vaccine_manager.get_vaccine_dose_priority();
          if (Global.Debug < 1)
          {
            Console.WriteLine($"Agent {person.get_id()} being put in to the queue with priority {vaccine_dose_priority} for dose {current_dose} on day {day} ");
          }

          switch (vaccine_dose_priority)
          {
            case Vaccine_Manager.VACC_DOSE_NO_PRIORITY:
              vaccine_manager.add_to_regular_queue_random(person);
              break;
            case Vaccine_Manager.VACC_DOSE_FIRST_PRIORITY:
              vaccine_manager.add_to_priority_queue_begin(person);
              break;
            case Vaccine_Manager.VACC_DOSE_RAND_PRIORITY:
              vaccine_manager.add_to_priority_queue_random(person);
              break;
            case Vaccine_Manager.VACC_DOSE_LAST_PRIORITY:
              vaccine_manager.add_to_priority_queue_end(person);
              break;

          }
        }
      }
    }

    public void update_for_next_dose(int day, double age)
    {
      vaccination_day = day;
      if (is_effective() == 0)
      {
        double efficacy = vaccine.get_dose(current_dose).get_efficacy(age);
        double efficacy_delay = vaccine.get_dose(current_dose).get_efficacy_delay(age);
        if (FredRandom.NextDouble() < efficacy)
          vaccination_effective_day = Convert.ToInt32(day + efficacy_delay);
      }
    }
  }
}
