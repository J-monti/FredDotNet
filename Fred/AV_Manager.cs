using System;
using System.Collections.Generic;

namespace Fred
{
  public class AV_Manager : Manager
  {
    private const int AV_POLICY_PERCENT_SYMPT = 0;
    private const int AV_POLICY_GIVE_EVERYONE = 1;
    public AV_Manager(int nav, Population _pop)
      : base(_pop)
    {
      this.Policies = new List<Policy>();
      if (nav > 0)
      {
        this.DoAntivirals = true;
        this.Antivirals = new Antivirals(nav);

        // Gather relavent Input Parameters
        //overall_start_day = 0;
        //if(does_param_exist(s))
        //  get_param(s,&overall_start_day);

        // Need to fill the AV_Manager Policies
        this.Policies.Add(new AntiviralPolicyDistributeToSymptomatics(this));
        this.Policies.Add(new AntiviralPolicyDistributeToEveryone(this));

        // Need to run through the Antivirals and give them the appropriate policy
        this.SetPolicies();
      }
      else
      {
        this.OverallStartDay = null;
      }
    }

    public bool DoAntivirals { get; }

    public bool ArePoliciesSet { get; private set; }

    public Antiviral CurrentAntiviral { get; private set; }

    public Antivirals Antivirals { get; }

    public DateTime? OverallStartDay { get; }

    public List<Policy> Policies { get; }

    public void Update(DateTime day)
    {
      if (this.DoAntivirals)
      {
        this.Antivirals.Update(day);
      }
    }

    public void Reset()
    {
      if (this.DoAntivirals)
      {
        this.Antivirals.Reset();
      }
    }

    public void Print()
    {
      if (this.DoAntivirals)
      {
        this.Antivirals.Print();
      }
    }

    public void Disseminate(DateTime day)
    {
      // There is no queue, only the whole population
      if (!this.DoAntivirals)
      {
        return;
      }
      int num_avs = 0;
      //current_day = day;
      // The av_package are in a priority based order, so lets loop over the av_package first
      foreach (var av in this.Antivirals)
      {
        if (av.CurrentStock > 0)
        {
          // Each AV has its one policy
          var p = av.Policy;
          this.CurrentAntiviral = av;
          // loop over the entire population
          for (int ip = 0; ip < this.Population.People.Count; ++ip)
          {
            if (av.CurrentStock == 0)
            {
              break;
            }
            Person current_person = this.Population.People[ip];
            if (current_person != null)
            {
              // Should the person get an av
              //int yeah_or_ney = p->choose(current_person,av->get_disease(),day);
              //if(yeah_or_ney == 0){

              if (p->choose_first_negative(current_person, av.Disease, day) == true)
              {
                Console.WriteLine("Giving Antiviral for disease {0} to {1}", av.Disease, ip);
                av.RemoveStock(1);
                current_person.Health.Take(av, day);
                num_avs++;
              }
            }
          }
        }
      }
      Global.DailyTracker.SetIndexKeyPair(day, "Av", num_avs);
    }

    private void SetPolicies()
    {
      foreach (var av in this.Antivirals)
      {
        if (av.IsProphylaxis)
        {
          av.SetPolicy(this.Policies[AV_POLICY_GIVE_EVERYONE]);
        }
        else
        {
          av.SetPolicy(this.Policies[AV_POLICY_PERCENT_SYMPT]);
        }
      }

      this.ArePoliciesSet = true;
    }
  }
}
