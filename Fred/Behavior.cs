using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Behavior
  {
    private Person health_decision_maker;
    private Intention[] intention;

    // run-time parameters for behaviors
    private static bool parameters_are_set;
    private static Behavior_params[] behavior_params;

    static Behavior()
    {
      if (!parameters_are_set)
      {
        var behaviorCount = (int)Behavior_index.NUM_BEHAVIORS;
        behavior_params = new Behavior_params[behaviorCount];
        for (int a = 0; a < behaviorCount; a++)
        {
          behavior_params[a] = new Behavior_params();
        }
        get_parameters();
        for (int i = 0; i < behaviorCount; ++i)
        {
          print_params(i);
        }
        parameters_are_set = true;
      }
    }

    public Behavior()
    {
    }

    public void setup(Person _self)
    {
      Utils.assert(parameters_are_set == true);
      if (!Global.Enable_Behaviors)
      {
        return;
      }

      // setup an adult
      if (_self.is_adult())
      {
        // adults do not have a separate health decision maker
        Utils.FRED_VERBOSE(1, "behavior_setup for adult {0} age {1} -- will make own health decisions",
         _self.get_id(), _self.get_age());
        this.health_decision_maker = null;
        setup_intentions(_self);
        return;
      }

      // setup a child
      int relationship = _self.get_relationship();
      var hh = (Household)(_self.get_household());

      if (hh == null)
      {
        if (Global.Enable_Hospitals && _self.is_hospitalized() && _self.get_permanent_household() != null)
        {
          hh = (Household)(_self.get_permanent_household());
        }
      }

      var person = select_adult(hh, relationship, _self);

      // child is on its own
      if (person == null)
      {
        Utils.FRED_VERBOSE(1, "behavior_setup for child {0} age {1} -- will make own health decisions",
         _self.get_id(), _self.get_age());
        // no separate health decision maker
        this.health_decision_maker = null;
        setup_intentions(_self);
        return;
      }

      // an older child is available
      if (person.is_adult() == false)
      {
        Utils.FRED_VERBOSE(0,
         "behavior_setup for child {0} age {1} -- minor person {2} age {3} will make health decisions",
         _self.get_id(), _self.get_age(), person.get_id(), person.get_age());
        this.health_decision_maker = person;
        person.become_health_decision_maker(person);
        return;
      }

      // an adult is available
      Utils.FRED_VERBOSE(0,
             "behavior_setup for child {0} age {1} -- adult person {2} age {3} will make health decisions",
             _self.get_id(), _self.get_age(), person.get_id(), person.get_age());
      this.health_decision_maker = person; // no need to setup atitudes for adults
    }

    public void update(Person self, int day)
    {
      Utils.FRED_VERBOSE(1, "behavior::update person {0} day {1}", self.get_id(), day);

      /*
        if(day == -1 && self.get_id() == 0) {
        for(int i = 0; i < Behavior_index::NUM_BEHAVIORS; ++i) {
        print_params(i);
        }
        }
      */

      if (!Global.Enable_Behaviors)
      {
        return;
      }

      if (this.health_decision_maker != null)
      {
        return;
      }

      for (int i = 0; i < (int)Behavior_index.NUM_BEHAVIORS; ++i)
      {
        var bParams = behavior_params[i];
        if (bParams.enabled)
        {
          Utils.FRED_VERBOSE(1, "behavior::update -- update intention[{0}]\n", i);
          Utils.assert(this.intention[i] != null);
          this.intention[i].update(day);
        }
      }

      Utils.FRED_VERBOSE(1, "behavior::update complete person {0} day {1}", self.get_id(), day);
    }

    public void terminate(Person self)
    {
      if (!Global.Enable_Behaviors)
      {
        return;
      }
      if (this.health_decision_maker != null)
      {
        return;
      }
      if (Global.Verbose > 1)
      {
        Console.WriteLine("terminating behavior for agent {0} age {1}", self.get_id(), self.get_age());
      }

      // see if this person is the adult decision maker for any child in the household
      var hh = (Household)(self.get_household());
      if (hh == null)
      {
        if (Global.Enable_Hospitals && self.is_hospitalized() && self.get_permanent_household() != null)
        {
          hh = (Household)(self.get_permanent_household()); ;
        }
      }
      int size = hh.get_size();
      for (int i = 0; i < size; ++i)
      {
        var child = hh.get_enrollee(i);
        if (child != self && child.get_health_decision_maker() == self)
        {
          if (Global.Verbose > 1)
          {
            Console.WriteLine("need new decision maker for agent {0} age {1}", child.get_id(), child.get_age());
          }
          child.setup_behavior();
          if (Global.Verbose > 1)
          {
            Console.WriteLine("new decision maker is {0} age {1}", child.get_health_decision_maker().get_id(),
             child.get_health_decision_maker().get_age());
          }
        }
      }
    }

    // methods to return this person's intention to take an action:
    public bool adult_is_staying_home()
    {
      return CheckBehavior((int)Behavior_index.STAY_HOME_WHEN_SICK);
    }

    public bool adult_is_taking_sick_leave()
    {
      return CheckBehavior((int)Behavior_index.TAKE_SICK_LEAVE);
    }

    public bool child_is_staying_home()
    {
      var n = (int)Behavior_index.KEEP_CHILD_HOME_WHEN_SICK;
      if (!behavior_params[n].enabled)
      {
        return false;
      }

      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.child_is_staying_home();
      }

      return CheckBehavior(n);
    }

    public bool acceptance_of_vaccine()
    {
      var n = (int)Behavior_index.ACCEPT_VACCINE;
      if (!behavior_params[n].enabled)
      {
        return false;
      }

      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.acceptance_of_vaccine();
      }

      return CheckBehavior(n);
    }

    public bool acceptance_of_another_vaccine_dose()
    {
      var n = (int)Behavior_index.ACCEPT_VACCINE_DOSE;
      if (!behavior_params[n].enabled)
      {
        return false;
      }

      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.acceptance_of_another_vaccine_dose();
      }

      return CheckBehavior(n);
    }

    public bool child_acceptance_of_vaccine()
    {
      var n = (int)Behavior_index.ACCEPT_VACCINE_FOR_CHILD;
      if (!behavior_params[n].enabled)
      {
        return false;
      }

      if (this.health_decision_maker != null)
      {
        Console.WriteLine("Asking health_decision_maker {0}", this.health_decision_maker.get_id());
        return this.health_decision_maker.child_acceptance_of_vaccine();
      }

      return CheckBehavior(n);
    }

    public bool child_acceptance_of_another_vaccine_dose()
    {
      var n = (int)Behavior_index.ACCEPT_VACCINE_DOSE_FOR_CHILD;
      if (!behavior_params[n].enabled)
      {
        return false;
      }

      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.child_acceptance_of_another_vaccine_dose();
      }

      return CheckBehavior(n);
    }


    // access functions
    public Person get_health_decision_maker()
    {
      return this.health_decision_maker;
    }

    public bool is_health_decision_maker()
    {
      return this.health_decision_maker == null;
    }

    public void set_health_decision_maker(Person p)
    {
      health_decision_maker = p;
      if (p != null)
      {
        delete_intentions();
      }
    }

    public void become_health_decision_maker(Person self)
    {
      if (this.health_decision_maker != null)
      {
        this.health_decision_maker = null;
        delete_intentions();
        setup_intentions(self);
      }
    }

    public static Behavior_params get_behavior_params(int i)
    {
      return behavior_params[i];
    }

    public static void print_params(int n)
    {
      var bParams = behavior_params[n];
      Console.WriteLine("PRINT BEHAVIOR PARAMS:\n");
      Console.WriteLine("name = {0}", bParams.name);
      Console.WriteLine("enabled = {0}", bParams.enabled);
      Console.WriteLine("frequency = {0}", bParams.frequency);
      Console.WriteLine("behavior_change_model_population = ");
      for (int i = 0; i < (int)Behavior_change_model_enum.NUM_BEHAVIOR_CHANGE_MODELS; ++i)
      {
        Console.WriteLine("{0} ", bParams.behavior_change_model_population[i]);
      }
      Console.WriteLine();
    }

    // private methods
    private void setup_intentions(Person _self)
    {
      if (!Global.Enable_Behaviors)
      {
        return;
      }

      Utils.assert(this.intention == null);
      var behaviorCount = (int)Behavior_index.NUM_BEHAVIORS;
      // create array of pointers to intentions
      this.intention = new Intention[behaviorCount];

      // initialize to null intentions
      for (int i = 0; i < behaviorCount; i++)
      {
        this.intention[i] = null;
      }

      if (behavior_params[(int)Behavior_index.STAY_HOME_WHEN_SICK].enabled)
      {
        this.intention[(int)Behavior_index.STAY_HOME_WHEN_SICK] =
          new Intention(_self, (int)Behavior_index.STAY_HOME_WHEN_SICK);
      }

      if (behavior_params[(int)Behavior_index.TAKE_SICK_LEAVE].enabled)
      {
        this.intention[(int)Behavior_index.TAKE_SICK_LEAVE] =
          new Intention(_self, (int)Behavior_index.TAKE_SICK_LEAVE);
      }

      if (behavior_params[(int)Behavior_index.KEEP_CHILD_HOME_WHEN_SICK].enabled)
      {
        this.intention[(int)Behavior_index.KEEP_CHILD_HOME_WHEN_SICK] =
          new Intention(_self, (int)Behavior_index.KEEP_CHILD_HOME_WHEN_SICK);
      }

      if (behavior_params[(int)Behavior_index.ACCEPT_VACCINE].enabled)
      {
        this.intention[(int)Behavior_index.ACCEPT_VACCINE] =
          new Intention(_self, (int)Behavior_index.ACCEPT_VACCINE);
      }

      if (behavior_params[(int)Behavior_index.ACCEPT_VACCINE_DOSE].enabled)
      {
        this.intention[(int)Behavior_index.ACCEPT_VACCINE_DOSE] =
          new Intention(_self, (int)Behavior_index.ACCEPT_VACCINE_DOSE);
      }

      if (behavior_params[(int)Behavior_index.ACCEPT_VACCINE_FOR_CHILD].enabled)
      {
        this.intention[(int)Behavior_index.ACCEPT_VACCINE_FOR_CHILD] =
          new Intention(_self, (int)Behavior_index.ACCEPT_VACCINE_FOR_CHILD);
      }

      if (behavior_params[(int)Behavior_index.ACCEPT_VACCINE_DOSE_FOR_CHILD].enabled)
      {
        this.intention[(int)Behavior_index.ACCEPT_VACCINE_DOSE_FOR_CHILD] =
          new Intention(_self, (int)Behavior_index.ACCEPT_VACCINE_DOSE_FOR_CHILD);
      }
    }

    private static void get_parameters()
    {
      if (parameters_are_set)
      {
        return;
      }

      get_parameters_for_behavior("stay_home_when_sick", (int)Behavior_index.STAY_HOME_WHEN_SICK);
      get_parameters_for_behavior("take_sick_leave", (int)Behavior_index.TAKE_SICK_LEAVE);
      get_parameters_for_behavior("keep_child_home_when_sick", (int)Behavior_index.KEEP_CHILD_HOME_WHEN_SICK);
      get_parameters_for_behavior("accept_vaccine", (int)Behavior_index.ACCEPT_VACCINE);
      get_parameters_for_behavior("accept_vaccine_dose", (int)Behavior_index.ACCEPT_VACCINE_DOSE);
      get_parameters_for_behavior("accept_vaccine_for_child", (int)Behavior_index.ACCEPT_VACCINE_FOR_CHILD);
      get_parameters_for_behavior("accept_vaccine_dose_for_child", (int)Behavior_index.ACCEPT_VACCINE_DOSE_FOR_CHILD);
      parameters_are_set = true;
    }

    private static void get_parameters_for_behavior(string behavior_name, int j)
    {
      var bParams = behavior_params[j];
      bParams.name = behavior_name;

      int temp_int = 0;
      FredParameters.GetParameter($"{behavior_name}_enabled", ref temp_int);
      bParams.enabled = temp_int != 0;
      for (int i = 0; i < (int)Behavior_change_model_enum.NUM_BEHAVIOR_CHANGE_MODELS; ++i)
      {
        bParams.behavior_change_model_population[i] = 0;
      }

      bParams.behavior_change_model_cdf =
        FredParameters.GetParameterList<double>($"{behavior_name}_behavior_change_model_distribution").ToArray();
      bParams.behavior_change_model_cdf_size = bParams.behavior_change_model_cdf.Length;

      // convert to cdf
      double stotal = 0;
      for (int i = 0; i < bParams.behavior_change_model_cdf_size; ++i)
      {
        stotal += bParams.behavior_change_model_cdf[i];
      }

      if (stotal != 100.0 && stotal != 1.0)
      {
        Utils.fred_abort("Bad distribution {behavior_name}_behavior_change_model_distribution params_str\nMust sum to 1.0 or 100.0");
      }

      double cumm = 0.0;
      for (int i = 0; i < bParams.behavior_change_model_cdf_size; ++i)
      {
        bParams.behavior_change_model_cdf[i] /= stotal;
        bParams.behavior_change_model_cdf[i] += cumm;
        cumm = bParams.behavior_change_model_cdf[i];
      }

      Console.WriteLine("BEHAVIOR {0} behavior_change_model_cdf: ", bParams.name);
      for (int i = 0; i < (int)Behavior_change_model_enum.NUM_BEHAVIOR_CHANGE_MODELS; i++)
      {
        Console.Write("{0} ", bParams.behavior_change_model_cdf[i]);
      }
      Console.WriteLine();

      FredParameters.GetParameter($"{behavior_name}_frequency", ref bParams.frequency);
      // FLIP behavior parameters
      FredParameters.GetParameter($"{behavior_name}_min_prob", ref bParams.min_prob);
      FredParameters.GetParameter($"{behavior_name}_max_prob", ref bParams.max_prob);

      // override max and min probs if prob is set
      double prob = 0;
      FredParameters.GetParameter($"{behavior_name}_prob", ref prob);
      if (0.0 <= prob)
      {
        bParams.min_prob = prob;
        bParams.max_prob = prob;
      }

      // IMITATE PREVALENCE behavior parameters
      bParams.imitate_prevalence_weight =
        FredParameters.GetParameterList<double>($"{behavior_name}_imitate_prevalence_weights").ToArray();

      bParams.imitate_prevalence_total_weight = 0.0;
      for (int i = 0; i < Behavior_params.NUM_WEIGHTS; ++i)
      {
        bParams.imitate_prevalence_total_weight += bParams.imitate_prevalence_weight[i];
      }

      FredParameters.GetParameter($"{behavior_name}_imitate_prevalence_update_rate", ref bParams.imitate_prevalence_update_rate);
      // IMITATE CONSENSUS behavior parameters
      bParams.imitate_consensus_weight =
        FredParameters.GetParameterList<double>($"{behavior_name}_imitate_consensus_weights").ToArray();

      bParams.imitate_consensus_total_weight = 0.0;
      for (int i = 0; i < Behavior_params.NUM_WEIGHTS; ++i)
      {
        bParams.imitate_consensus_total_weight += bParams.imitate_consensus_weight[i];
      }

      FredParameters.GetParameter($"{behavior_name}_imitate_consensus_update_rate", ref bParams.imitate_consensus_update_rate);
      FredParameters.GetParameter($"{behavior_name}_imitate_consensus_threshold", ref bParams.imitate_consensus_threshold);

      // IMITATE COUNT behavior parameters
      bParams.imitate_count_weight =
        FredParameters.GetParameterList<double>($"{behavior_name}_imitate_count_weights").ToArray();

      bParams.imitate_count_total_weight = 0.0;
      for (int i = 0; i < Behavior_params.NUM_WEIGHTS; ++i)
      {
        bParams.imitate_count_total_weight += bParams.imitate_count_weight[i];
      }

      FredParameters.GetParameter($"{behavior_name}_imitate_count_update_rate", ref bParams.imitate_count_update_rate);
      FredParameters.GetParameter($"{behavior_name}_imitate_count_threshold", ref bParams.imitate_count_threshold);
      bParams.imitation_enabled = 0;
      // HBM parameters
      bParams.susceptibility_threshold_distr =
        FredParameters.GetParameterList<double>($"{behavior_name}_susceptibility_threshold").ToArray();
      if (bParams.susceptibility_threshold_distr.Length != 2)
      {
        Utils.fred_abort($"bad {behavior_name}_susceptibility_threshold");
      }

      bParams.severity_threshold_distr =
        FredParameters.GetParameterList<double>($"{behavior_name}_severity_threshold").ToArray();
      if (bParams.severity_threshold_distr.Length != 2)
      {
        Utils.fred_abort($"bad {behavior_name}_severity_threshold");
      }

      bParams.benefits_threshold_distr =
        FredParameters.GetParameterList<double>($"{behavior_name}_benefits_threshold").ToArray();
      if (bParams.benefits_threshold_distr.Length != 2)
      {
        Utils.fred_abort($"bad {behavior_name}_benefits_threshold");
      }

      bParams.barriers_threshold_distr =
        FredParameters.GetParameterList<double>($"{behavior_name}_barriers_threshold").ToArray();
      if (bParams.barriers_threshold_distr.Length != 2)
      {
        Utils.fred_abort($"bad {behavior_name}_barriers_threshold");
      }

      FredParameters.GetParameter($"{behavior_name}_base_odds_ratio", ref bParams.base_odds_ratio);
      FredParameters.GetParameter($"{behavior_name}_susceptibility_odds_ratio", ref bParams.susceptibility_odds_ratio);
      FredParameters.GetParameter($"{behavior_name}_severity_odds_ratio", ref bParams.severity_odds_ratio);
      FredParameters.GetParameter($"{behavior_name}_benefits_odds_ratio", ref bParams.benefits_odds_ratio);
      FredParameters.GetParameter($"{behavior_name}_barriers_odds_ratio", ref bParams.barriers_odds_ratio);
    }

    private Person select_adult(Household h, int relationship, Person self)
    {
      int N = h.get_size();
      if (relationship == Global.GRANDCHILD)
      {

        // select first adult natural child or spouse thereof of householder parent, if any
        for (int i = 0; i < N; ++i)
        {
          var person = h.get_enrollee(i);
          if (person.is_adult() == false || person == self)
          {
            continue;
          }
          int r = person.get_relationship();
          if (r == Global.SPOUSE || r == Global.CHILD || r == Global.SIBLING || r == Global.IN_LAW)
          {
            return person;
          }
        }

        // consider adult relative of householder, if any
        for (int i = 0; i < N; ++i)
        {
          var person = h.get_enrollee(i);
          if (person.is_adult() == false || person == self)
          {
            continue;
          }
          int r = person.get_relationship();
          if (r == Global.PARENT || r == Global.OTHER_RELATIVE)
          {
            return person;
          }
        }
      }

      // select householder if an adult
      for (int i = 0; i < N; ++i)
      {
        var person = h.get_enrollee(i);
        if (person.is_adult() == false || person == self)
        {
          continue;
        }
        if (person.get_relationship() == Global.HOUSEHOLDER)
        {
          return person;
        }
      }

      // select householder's spouse if an adult
      for (int i = 0; i < N; ++i)
      {
        var person = h.get_enrollee(i);
        if (person.is_adult() == false || person == self)
        {
          continue;
        }
        if (person.get_relationship() == Global.SPOUSE)
        {
          return person;
        }
      }

      // select oldest available person
      int max_age = self.get_age();

      Person oldest = null;
      for (int i = 0; i < N; ++i)
      {
        var person = h.get_enrollee(i);
        if (person.get_age() <= max_age || person == self)
        {
          continue;
        }
        oldest = person;
        max_age = oldest.get_age();
      }

      return oldest;
    }

    private void delete_intentions()
    {
      this.intention = null;
    }

    private bool CheckBehavior(int n)
    {
      Utils.assert(Global.Enable_Behaviors == true);
      var my_intention = this.intention[n];
      if (my_intention == null)
      {
        return false;
      }

      var bParams = behavior_params[n];
      if (!bParams.enabled)
      {
        return false;
      }

      return my_intention.agree();
    }
  }
}
