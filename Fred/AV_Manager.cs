using System;
using System.Collections.Generic;

namespace Fred
{
  public class AV_Manager : Manager
  {
    public int AV_POLICY_PERCENT_SYMPT = 0;
    public int AV_POLICY_GIVE_EVERYONE = 1;

    private bool do_av;                    //Whether or not antivirals are being disseminated
    private Antivirals av_package;         //The package of avs available to this manager
    private int overall_start_day;         //Day to start the av procedure
    private bool are_policies_set;         //Ensure that the policies for AVs have been set.
    private Antiviral current_av;          //NEED TO ELIMINATE, HIDDEN to IMPLEMENTATION

    /**
   * Default constructor. Does not set 'do_av' bool, thereby disabling antirals.
   */
    public AV_Manager()
    {
      this.overall_start_day = -1;
    }

    /**
     * Constructor that sets the Population to which this AV_Manager is tied.
     * This constructor also checks to see if the number of antivirals given in the
     * params file greater than one and, if so, sets the 'do_av' bool.
     */
    public AV_Manager(Population _pop)
      : base(_pop)
    {

      this.pop = _pop;
      this.are_policies_set = false;
      //char s[80];
      int nav = 0;
      FredParameters.GetParameter("number_antivirals", ref nav);
      this.do_av = false;
      if (nav > 0)
      {
        this.do_av = true;
        this.av_package = new Antivirals();

        // Gather relavent Input Parameters
        //overall_start_day = 0;
        //if(does_param_exist(s))
        //  get_param(s,&overall_start_day);

        // Need to fill the AV_Manager Policies
        this.policies.Add(new AV_Policy_Distribute_To_Symptomatics(this));
        this.policies.Add(new AV_Policy_Distribute_To_Everyone(this));

        // Need to run through the Antivirals and give them the appropriate policy
        set_policies();
      }
      else
      {
        this.overall_start_day = -1;
      }
    }

    //Parameters
    /**
     * @return  <code>true</code> if antivirals are being disseminated <code>false</code> otherwise
     */
    public bool do_antivirals()
    {
      return do_av;
    }

    /**
     * @return overall_start_day
     */
    public int get_overall_start_day()
    {
      return overall_start_day;
    }

    /**
     * @return a pointer to current_av
     */
    public Antiviral get_current_av()
    {
      return this.current_av;
    }

    //Paramters
    /**
     * @return a pointer to this manager's Antiviral package
     */
    public Antivirals get_antivirals()
    {
      return this.av_package;
    }

    /**
     * @return a count of this manager's antivirals
     * @see Antivirals::get_number_antivirals()
     */
    public int get_num_antivirals()
    {
      return this.av_package.get_number_antivirals();
    }

    /**
     * @return <code>true</code> if policies are set, <code>false</code> otherwise
     */
    public bool get_are_policies_set()
    {
      return this.are_policies_set;
    }

    // Manager Functions
    /**
     * Push antivirals to agents, needed for prophylaxis
     *
     * @param day the simulation day
     */
    public void disseminate(int day)
    {
      // There is no queue, only the whole population
      if (!this.do_av)
      {
        return;
      }
      int num_avs = 0;
      //current_day = day;
      // The av_package are in a priority based order, so lets loop over the av_package first
      var avs = this.av_package;
      for (int iav = 0; iav < avs.Count; iav++)
      {
        var av = avs[iav];
        if (av.get_current_stock() > 0)
        {
          // Each AV has its one policy
          var p = av.get_policy();
          this.current_av = av;
          // loop over the entire population
          for (int ip = 0; ip < this.pop.get_index_size(); ++ip)
          {
            if (av.get_current_stock() == 0)
            {
              break;
            }
            var current_person = this.pop.get_person_by_index(ip);
            if (current_person != null)
            {
              // Should the person get an av
              //int yeah_or_ney = p.choose(current_person,av.get_disease(),day);
              //if(yeah_or_ney == 0){

              if (p.choose_first_negative(current_person, av.get_disease(), day) == true)
              {
                if (Global.Debug > 3)
                {
                  Console.WriteLine($"Giving Antiviral for disease {av.get_disease()} to {ip}");
                }
                av.remove_stock(1);
                current_person.get_health().take(av, day);
                num_avs++;
              }
            }
          }
        }
      }
      Global.Daily_Tracker.set_index_key_pair(day, "Av", num_avs);
    }

    // Utility Functions
    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public override void update(int day)
    {
      if (this.do_av)
      {
        this.av_package.update(day);
        if (Global.Debug > 1)
        {
          this.av_package.print_stocks();
        }
      }
    }

    /**
     * Put this object back to its original state
     */
    public override void reset()
    {
      if (this.do_av)
      {
        this.av_package.reset();
      }
    }

    /**
     * Print out information about this object
     */
    public override void print()
    {
      if (this.do_av)
      {
        this.av_package.print();
      }
    }

    /**
     * Member to set the policy of all of the Antivirals
     */
    private void set_policies()
    {
      var avs = this.av_package;
      for (int iav = 0; iav < avs.Count; iav++)
      {
        if (avs[iav].is_prophylaxis())
        {
          avs[iav].set_policy(this.policies[AV_POLICY_GIVE_EVERYONE]);
        }
        else
        {
          avs[iav].set_policy(this.policies[AV_POLICY_PERCENT_SYMPT]);
        }
      }

      this.are_policies_set = true;
    }
  }
}
