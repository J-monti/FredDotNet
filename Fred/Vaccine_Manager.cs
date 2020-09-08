using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine_Manager : Manager
  {
    public const int VACC_NO_PRIORITY = 0;
    public const int VACC_AGE_PRIORITY = 1;
    public const int VACC_ACIP_PRIORITY = 2;
    public const int VACC_DOSE_NO_PRIORITY = 0;
    public const int VACC_DOSE_FIRST_PRIORITY = 1;
    public const int VACC_DOSE_RAND_PRIORITY = 2;
    public const int VACC_DOSE_LAST_PRIORITY = 3;

    private Vaccines vaccine_package;             //Pointer to the vaccines that this manager oversees
    private List<Person> priority_queue;         //Queue for the priority agents
    private List<Person> queue;                  //Queue for everyone else

    //Parameters from Input 
    private bool do_vacc;                           //Is Vaccination being performed
    private bool vaccine_priority_only;             //True - Vaccinate only the priority
    private bool vaccinate_symptomatics;            //True - Include people that have had symptoms in the vaccination
    private bool refresh_vaccine_queues_daily;      //True - people queue up in random order each day

    private int vaccine_priority_age_low;           //Age specific priority
    private int vaccine_priority_age_high;
    private int vaccine_dose_priority;              //Defines where people getting multiple doses fit in the queue
                                                    // See defines above for values

    private Timestep_Map vaccination_capacity_map; // How many people can be vaccinated now,
                                                   // gets its value from the capacity change list
    private int current_vaccine_capacity;           // variable to keep track of how many persons this 
                                                    // can vaccinate each timestep.

    public Vaccine_Manager()
    {
      this.vaccine_package = null;
      this.vaccine_priority_age_low = -1;
      this.vaccine_priority_age_high = -1;
      this.current_vaccine_capacity = -1;
      this.vaccine_priority_only = false;
      this.vaccination_capacity_map = null;
      this.do_vacc = false;
      this.vaccine_dose_priority = -1;
      this.refresh_vaccine_queues_daily = false;
      this.vaccinate_symptomatics = false;
    }

    public Vaccine_Manager(Population _pop)
      : base(_pop)
    {
      this.vaccine_package = new Vaccines();
      int num_vaccs = 0;
      FredParameters.GetParameter("number_of_vaccines", ref num_vaccs);
      if (num_vaccs > 0)
      {
        this.vaccine_package.setup();
        this.vaccine_package.print();
        this.do_vacc = true;
      }
      else
      {    // No vaccination specified.
        this.vaccine_priority_age_low = -1;
        this.vaccine_priority_age_high = -1;
        this.vaccination_capacity_map = null;
        this.current_vaccine_capacity = -1;
        this.vaccine_dose_priority = -1;
        this.vaccine_priority_only = false;
        this.do_vacc = false;
        return;
      }
      // ACIP Priority takes precidence
      int do_acip_priority = 0;
      current_policy = VACC_NO_PRIORITY;
      FredParameters.GetParameter("vaccine_prioritize_acip", ref do_acip_priority);
      if (do_acip_priority == 1)
      {
        Console.WriteLine("Vaccination Priority using ACIP recommendations");
        Console.WriteLine("   Includes: ");
        Console.WriteLine("        Ages 0 to 24");
        Console.WriteLine("        Pregnant Women");
        Console.WriteLine("        Persons at risk for complications");
        this.current_policy = VACC_ACIP_PRIORITY;
        this.vaccine_priority_age_low = 0;
        this.vaccine_priority_age_high = 24;
      }
      else
      {
        int do_age_priority = 0;
        FredParameters.GetParameter("vaccine_prioritize_by_age", ref do_age_priority);
        if (do_age_priority != 0)
        {
          Console.WriteLine("Vaccination Priority by Age");
          this.current_policy = VACC_AGE_PRIORITY;
          FredParameters.GetParameter("vaccine_priority_age_low", ref this.vaccine_priority_age_low);
          FredParameters.GetParameter("vaccine_priority_age_high", ref this.vaccine_priority_age_high);
          Console.WriteLine($"      Between Ages {this.vaccine_priority_age_low} and {this.vaccine_priority_age_high}");
        }
        else
        {
          this.vaccine_priority_age_low = 0;
          this.vaccine_priority_age_high = Demographics.MAX_AGE;
        }
      }

      // should we vaccinate anyone outside of the priority class
      int vacc_pri_only = 0;
      this.vaccine_priority_only = false;
      FredParameters.GetParameter("vaccine_priority_only", ref vacc_pri_only);
      if (vacc_pri_only != 0)
      {
        this.vaccine_priority_only = true;
        Console.WriteLine("      Vaccinating only the priority groups\n");
      }

      // should we exclude people that have had symptomatic infections?
      int vacc_sympt_exclude = 0;
      this.vaccinate_symptomatics = false;
      FredParameters.GetParameter("vaccinate_symptomatics", ref vacc_sympt_exclude);
      if (vacc_sympt_exclude != 0)
      {
        this.vaccinate_symptomatics = true;
        Console.WriteLine("      Vaccinating symptomatics\n");
      }

      // get vaccine_dose_priority
      FredParameters.GetParameter("vaccine_dose_priority", ref this.vaccine_dose_priority);
      Utils.assert(this.vaccine_dose_priority < 4);
      //get_param((char*)"vaccination_capacity",&vaccination_capacity);
      this.vaccination_capacity_map = new Timestep_Map("vaccination_capacity");
      this.vaccination_capacity_map.read_map();
      if (Global.Verbose > 1)
      {
        this.vaccination_capacity_map.print();
      }

      int refresh = 0;
      FredParameters.GetParameter("refresh_vaccine_queues_daily", ref refresh);
      this.refresh_vaccine_queues_daily = (refresh > 0);

      // Need to fill the Vaccine_Manager Policies
      this.policies.Add(new Vaccine_Priority_Policy_No_Priority(this));
      this.policies.Add(new Vaccine_Priority_Policy_Specific_Age(this));
      this.policies.Add(new Vaccine_Priority_Policy_ACIP(this));
    }

    //Parameters Access
    public bool do_vaccination()
    {
      return this.do_vacc;
    }

    public Vaccines get_vaccines()
    {
      return this.vaccine_package;
    }

    public List<Person> get_priority_queue()
    {
      return this.priority_queue;
    }

    public List<Person> get_queue()
    {
      return this.queue;
    }

    public int get_number_in_priority_queue()
    {
      return this.priority_queue.Count;
    }

    public int get_number_in_reg_queue()
    {
      return this.queue.Count;
    }

    public int get_current_vaccine_capacity()
    {
      return this.current_vaccine_capacity;
    }

    // Vaccination Specific Procedures
    public void fill_queues()
    {
      if (!this.do_vacc)
      {
        return;
      }
      // We need to loop over the entire population that the Manager oversees to put them in a queue.
      for (int ip = 0; ip < pop.get_index_size(); ip++)
      {
        var current_person = this.pop.get_person_by_index(ip);
        if (current_person != null)
        {
          if (this.policies[current_policy].choose_first_positive(current_person, 0, 0) == true)
          {
            priority_queue.Add(current_person);
          }
          else
          {
            if (this.vaccine_priority_only == false)
              this.queue.Add(current_person);
          }
        }
      }

      var random_queue = new List<Person>(this.queue);
      random_queue.Shuffle();
      this.queue = new List<Person>(random_queue);

      var random_priority_queue = new List<Person>(this.priority_queue);
      random_priority_queue.Shuffle();
      this.priority_queue = new List<Person>(random_priority_queue);

      if (Global.Verbose > 0)
      {
        Console.WriteLine("Vaccine Queue Stats ");
        Console.WriteLine($"   Number in Priority Queue      = {this.priority_queue.Count}");
        Console.WriteLine($"   Number in Regular Queue       = {this.queue.Count}");
        Console.WriteLine($"   Total Agents in Vaccine Queue = {this.priority_queue.Count + this.queue.Count}");
      }
    }

    public void vaccinate(int day)
    {
      if (this.do_vacc)
      {
        Console.WriteLine("Vaccinating!");
      }
      else
      {
        Console.WriteLine("Not vaccinating!");
        return;
      }

      int number_vaccinated = 0;
      int n_p_vaccinated = 0;
      int n_r_vaccinated = 0;
      int accept_count = 0;
      int reject_count = 0;
      int reject_state_count = 0;

      // Figure out the total number of vaccines we can hand out today
      int total_vaccines_avail = this.vaccine_package.get_total_vaccines_avail_today();

      if (Global.Debug > 0)
      {
        Console.WriteLine($"Vaccine Capacity on Day {day} = {current_vaccine_capacity}");
        Console.WriteLine($"Queues at beginning of vaccination:  priority ({priority_queue.Count})    Regular ({this.queue.Count})");
      }
      if (total_vaccines_avail == 0 || current_vaccine_capacity == 0)
      {
        if (Global.Debug > 1)
        {
          Console.WriteLine($"No Vaccine Available on Day {day}");
        }
        Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
        Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
        Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
        Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
        return;
      }

      // Start vaccinating Priority
      //int accept_count = 0;
      //int reject_count = 0;
      //int reject_state_count = 0;
      // Run through the priority queue first 
      var toRemove = new List<Person>();
      foreach (var current_person in this.priority_queue)
      {
        int vacc_app = this.vaccine_package.pick_from_applicable_vaccines((double)(current_person.get_age()));
        // printf("person = %d age = %.1f vacc_app = %d\n", current_person.get_id(), current_person.get_real_age(), vacc_app);
        if (vacc_app > -1)
        {
          bool accept_vaccine = false;
          // STB need to refactor to work with multiple diseases
          if ((this.vaccinate_symptomatics == false)
             && (current_person.get_health().get_symptoms_start_date(0) != -1)
             && (day >= current_person.get_health().get_symptoms_start_date(0)))
          {
            accept_vaccine = false;
            reject_state_count++;
          }
          else
          {
            if (current_person.get_health().is_vaccinated())
            {
              accept_vaccine = current_person.acceptance_of_another_vaccine_dose();
            }
            else
            {
              accept_vaccine = current_person.acceptance_of_vaccine();
            }
          }
          if (accept_vaccine == true)
          {
            accept_count++;
            number_vaccinated++;
            this.current_vaccine_capacity--;
            n_p_vaccinated++;
            var vacc = this.vaccine_package.get_vaccine(vacc_app);
            vacc.remove_stock(1);
            total_vaccines_avail--;
            current_person.take_vaccine(vacc, day, this);
            toRemove.Add(current_person);  // remove a vaccinated person
          }
          else
          {
            reject_count++;
            // TODO: HBM FIX THIS!
            // printf("vaccine rejected by person %d age %.1f\n", current_person.get_id(), current_person.get_real_age());
            // skip non-compliant person under HBM
            // if(strcmp(Global.Behavior_model_type,"HBM") == 0) ++ip;
            if (false)
            {
              //++ip;
            }
            else
            {
              // remove non-compliant person if not HBM
              //ip = this.priority_queue.erase(ip);
              toRemove.Add(current_person);
            }
          }
        }
        else
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Vaccine not applicable for agent {current_person.get_id()} {current_person.get_real_age()}");
          }
        }

        if (total_vaccines_avail == 0)
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Vaccinated priority to stock out {n_p_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
            Console.WriteLine($"Left in queues:  Priority ({priority_queue.Count - number_vaccinated})    Regular ({queue.Count})");
            Console.WriteLine($"Number of acceptances: {accept_count}, Number of rejections: {reject_count}");
          }
          Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
          Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
          return;
        }
        if (current_vaccine_capacity == 0)
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Vaccinated priority to capacity {n_p_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
            Console.WriteLine($"Left in queues:  Priority ({this.priority_queue.Count - number_vaccinated})    Regular ({queue.Count})");
            Console.WriteLine($"Number of acceptances: {accept_count}, Number of rejections: {reject_count}");
          }
          Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
          Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
          return;
        }
      }

      foreach (var p in toRemove)
      {
        this.priority_queue.Remove(p);
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine($"Vaccinated priority to population {n_p_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
      }

      toRemove.Clear();
      // Run now through the regular queue
      foreach (var current_person in this.queue)
      {
        int vacc_app = this.vaccine_package.pick_from_applicable_vaccines(current_person.get_real_age());
        if (vacc_app > -1)
        {
          bool accept_vaccine = true;
          if ((this.vaccinate_symptomatics == false)
             && (current_person.get_health().get_symptoms_start_date(0) != -1)
             && (day >= current_person.get_health().get_symptoms_start_date(0)))
          {
            accept_vaccine = false;
            reject_state_count++;
            // printf("vaccine rejected by person %d age %0.1f -- ALREADY SYMPTOMATIC\n", current_person.get_id(), current_person.get_real_age());
          }
          else
          {
            if (current_person.get_health().is_vaccinated())
            {
              // printf("vaccine rejected by person %d age %0.1f -- ALREADY VACCINATED\n", current_person.get_id(), current_person.get_real_age());
              accept_vaccine = current_person.acceptance_of_another_vaccine_dose();
            }
            else
            {
              accept_vaccine = current_person.acceptance_of_vaccine();
            }
          }
          if (accept_vaccine == true)
          {
            // printf("vaccine accepted by person %d age %0.1f\n", current_person.get_id(), current_person.get_real_age());
            accept_count++;
            number_vaccinated++;
            this.current_vaccine_capacity--;
            n_r_vaccinated++;
            var vacc = this.vaccine_package.get_vaccine(vacc_app);
            vacc.remove_stock(1);
            total_vaccines_avail--;
            current_person.take_vaccine(vacc, day, this);
            toRemove.Add(current_person);
            //ip = this.queue.erase(ip);  // remove a vaccinated person
          }
          else
          {
            // printf("vaccine rejected by person %d age %0.1f\n", current_person.get_id(), current_person.get_real_age());
            reject_count++;
            // skip non-compliant person under HBM
            // if(strcmp(Global.Behavior_model_type,"HBM") == 0) ip++;
            if (false)
            {
              //ip++;
            }
            // remove non-compliant person if not HBM
            else
            {
              //ip = this.queue.erase(ip);
              toRemove.Add(current_person);
            }
          }
        }
        if (total_vaccines_avail == 0)
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Vaccinated regular to stock_out {n_r_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
            Console.WriteLine($"Left in queues:  priority ({priority_queue.Count})    Regular ({queue.Count})");
            Console.WriteLine($"Number of acceptances: {accept_count}, Number of rejections: {reject_count}");
          }
          Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
          Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
          return;
        }
        if (this.current_vaccine_capacity == 0)
        {
          if (Global.Verbose > 0)
          {
            Console.WriteLine($"Vaccinated regular to capacity {n_r_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
            Console.WriteLine($"Left in queues:  priority ({this.priority_queue.Count})    Regular ({queue.Count})");
            Console.WriteLine($"Number of acceptances: {accept_count}, Number of rejections: {reject_count}");
          }
          Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
          Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
          Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
          return;
        }
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine($"Vaccinated regular to population {n_r_vaccinated} agents, for a total of {number_vaccinated} on day {day}");
        Console.WriteLine($"Left in queues:  priority ({this.priority_queue.Count})    Regular ({queue.Count})");
        Console.WriteLine($"Number of acceptances: {accept_count}, Number of rejections: {reject_count}");
      }
      Global.Daily_Tracker.set_index_key_pair(day, "V", number_vaccinated);
      Global.Daily_Tracker.set_index_key_pair(day, "Va", accept_count);
      Global.Daily_Tracker.set_index_key_pair(day, "Vr", reject_count);
      Global.Daily_Tracker.set_index_key_pair(day, "Vs", reject_state_count);
    }

    public void add_to_queue(Person person)                 //Adds person to queue based on current policies
    {
      if (this.policies[this.current_policy].choose_first_positive(person, 0, 0) == true)
      {
        add_to_priority_queue_random(person);
      }
      else
      {
        if (this.vaccine_priority_only == false)
        {
          add_to_regular_queue_random(person);
        }
      }
    }

    public void remove_from_queue(Person person)            //Remove person from the vaccine queue
    {
      // remove the person from the queue if they are in there
      if (this.priority_queue.Remove(person))
      {
        return;
      }

      this.queue.Remove(person);
    }
    public void add_to_priority_queue_random(Person person) //Adds person to the priority queue in a random spot
    {
      // Find a position to put the person in
      int size = this.priority_queue.Count;
      int position = FredRandom.Next(0, size);
      this.priority_queue.Insert(position, person);
    }

    public void add_to_regular_queue_random(Person person)  //Adds person to the regular queue in a random spot
    {
      // Find a position to put the person in
      int size = this.queue.Count;
      int position = FredRandom.Next(0, size);
      this.queue.Insert(position, person);
    }

    public void add_to_priority_queue_begin(Person person)  //Adds person to the beginning of the priority queue
    {
      // Find a position to put the person in
      this.priority_queue.Insert(0, person);
    }

    public void add_to_priority_queue_end(Person person)    //Adds person to the end of the priority queue
    {
      this.priority_queue.Add(person);
    }

    //Paramters Access Members
    public int get_vaccine_priority_age_low()
    {
      return vaccine_priority_age_low;
    }

    public int get_vaccine_priority_age_high()
    {
      return this.vaccine_priority_age_high;
    }

    public int get_vaccine_dose_priority()
    {
      return this.vaccine_dose_priority;
    }

    public string get_vaccine_dose_priority_string()
    {
      switch (this.vaccine_dose_priority)
      {
        case VACC_DOSE_NO_PRIORITY:
          return "No Priority";
        case VACC_DOSE_FIRST_PRIORITY:
          return "Priority, Place at Beginning of Queue";
        case VACC_DOSE_RAND_PRIORITY:
          return "Priority, Place with other Priority";
        case VACC_DOSE_LAST_PRIORITY:
          return "Priority, Place at End of Queue";
        default:
          Utils.FRED_VERBOSE(0, "Unrecognized Vaccine Dose Priority\n");
          return string.Empty;
      }
    }

    // Utility Members
    public override void update(int day)
    {
      if (this.do_vacc)
      {
        this.vaccine_package.update(day);
        // Update the current vaccination capacity
        this.current_vaccine_capacity = this.vaccination_capacity_map.get_value_for_timestep(day, Global.Vaccine_offset);
        Console.WriteLine($"Current Vaccine Stock = {this.vaccine_package.get_vaccine(0).get_current_stock()}");

        if (this.refresh_vaccine_queues_daily)
        {
          // update queues
          this.priority_queue.Clear();
          this.queue.Clear();
          fill_queues();
        }

        // vaccinate people in the queues:
        vaccinate(day);
      }
    }

    public override void reset()
    {
      this.priority_queue.Clear();
      this.queue.Clear();
      if (this.do_vacc)
      {
        fill_queues();
        this.vaccine_package.reset();
      }
    }

    public override void print()
    {
      this.vaccine_package.print();
    }
  }
}
