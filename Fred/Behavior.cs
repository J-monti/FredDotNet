using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Behavior
  {
    static Behavior()
    {
      if (!AreBehaviorParamsSet)
      {
        var behaviorCount = (int)BehaviorEnum.NumBehaviors;
        BehaviorParams = new List<BehaviorParameters>();
        for(int a = 0; a < behaviorCount; a++)
        {
          BehaviorParams.Add(new BehaviorParameters());
        }
        AreBehaviorParamsSet = true;
      }
    }

    public Behavior()
    {
      this.Intentions = new List<Intention>();
    }

    public static bool AreBehaviorParamsSet { get; }
    public static List<BehaviorParameters> BehaviorParams { get; }
    public Person HealthDecisionMaker { get; private set; }
    public List<Intention> Intentions { get; }
    public bool IsHealthDecisionMaker { get { return this.HealthDecisionMaker != null; } }

    public void SetHealthDecisionMaker(Person person)
    {
      this.HealthDecisionMaker = person;
      if (person != null)
      {
        this.Intentions.Clear();
      }
    }

    public void setup(Person self)
    {
      if (!Global.IsBehaviorsEnabled)
      {
        return;
      }

      // setup an adult
      if (self.IsAdult)
      {
        // adults do not have a separate health decision maker
        this.SetHealthDecisionMaker(null);
        this.SetupIntentions(self);
        return;
      }

      // setup a child
      int relationship = self.get_relationship();
      var hh = self.Household;

      if (hh == null)
      {
        if (Global::Enable_Hospitals && self.is_hospitalized() && self.get_permanent_household() != null)
        {
          hh = static_cast<Household*>(self.get_permanent_household());
        }
      }

      Person* person = select_adult(hh, relationship, self);

      // child is on its own
      if (person == null)
      {
        // no separate health decision maker
        this.HealthDecisionMaker = null;
        this.SetupIntentions(self);
        return;
      }

      // an older child is available
      if (!person.IsAdult)
      {
        this.HealthDecisionMaker = person;
        person.become_health_decision_maker(person);
        return;
      }

      // an adult is available
      this.HealthDecisionMaker = person; // no need to setup atitudes for adults
      return;
    }

    private void SetupIntentions(Person self)
    {
      if (!Global.IsBehaviorsEnabled)
      {
        return;
      }

      // create array of pointers to intentions
      this.Intentions.Clear();

      // initialize to null intentions
      for (int i = 0; i < (int)BehaviorEnum.NumBehaviors; i++)
      {
        this.Intentions.Add(null);
      }

      if (BehaviorParams[(int)BehaviorEnum.StayHomeWhenSick].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.StayHomeWhenSick] =
          new Intention(self, (int)BehaviorEnum.StayHomeWhenSick);
      }

      if (BehaviorParams[(int)BehaviorEnum.TakeSickLeave].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.TakeSickLeave] =
          new Intention(self, (int)BehaviorEnum.TakeSickLeave);
      }

      if (BehaviorParams[(int)BehaviorEnum.KeepChildHomeWhenSick].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.KeepChildHomeWhenSick] =
          new Intention(self, (int)BehaviorEnum.KeepChildHomeWhenSick);
      }

      if (BehaviorParams[(int)BehaviorEnum.AcceptVaccine].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.AcceptVaccine] =
          new Intention(self, (int)BehaviorEnum.AcceptVaccine);
      }

      if (BehaviorParams[(int)BehaviorEnum.AcceptVaccineDose].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.AcceptVaccineDose] =
          new Intention(self, (int)BehaviorEnum.AcceptVaccineDose);
      }

      if (BehaviorParams[(int)BehaviorEnum.AcceptVaccineForChild].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.AcceptVaccineForChild] =
          new Intention(self, (int)BehaviorEnum.AcceptVaccineForChild);
      }

      if (BehaviorParams[(int)BehaviorEnum.AcceptVaccineDoseForChild].IsEnabled)
      {
        this.Intentions[(int)BehaviorEnum.AcceptVaccineDoseForChild] =
          new Intention(self, (int)BehaviorEnum.AcceptVaccineDoseForChild);
      }
    }

    public void Update(Person self, DateTime day)
    {
      if (!Global.IsBehaviorsEnabled)
      {
        return;
      }

      if (this.HealthDecisionMaker != null)
      {
        return;
      }

      for (int i = 0; i < (int)BehaviorEnum.NumBehaviors; ++i)
      {
        var bParams = BehaviorParams[i];
        if (bParams.IsEnabled) {
          this.Intentions[i].Update(day);
        }
      }
    }

    public bool adult_is_staying_home()
    {
      int n = (int)BehaviorEnum.StayHomeWhenSick;
      var my_intention = this.Intentions[n];
      if (my_intention == null)
      {
        return false;
      }
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      return my_intention.agree();
    }

    public bool adult_is_taking_sick_leave()
    {
      int n = (int)BehaviorEnum.TakeSickLeave;
      var my_intention = this.Intentions[n];
      if (my_intention == null)
      {
        return false;
      }
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      return my_intention.agree();
    }

    public bool child_is_staying_home()
    {
      int n = (int)BehaviorEnum.KeepChildHomeWhenSick;
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.child_is_staying_home();
      }
      // I am the health decision maker
      var my_intention = this.Intentions[n];
      assert(my_intention != null);
      return my_intention.agree();
    }

    public bool acceptance_of_vaccine()
    {
      int n = (int)BehaviorEnum.AcceptVaccine;
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      if (this.health_decision_maker != null)
      {
        // printf("Asking health_decision_maker %d \n", health_decision_maker.get_id());
        return this.health_decision_maker.acceptance_of_vaccine();
      }
      // I am the health decision maker
      var my_intention = this.Intentions[n];
      assert(my_intention != null);
      // printf("My answer is:\n");
      return my_intention.agree();
    }

    public bool acceptance_of_another_vaccine_dose()
    {
      int n = (int)BehaviorEnum.AcceptVaccineDose;
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.acceptance_of_another_vaccine_dose();
      }
      // I am the health decision maker
      var my_intention = this.Intentions[n];
      assert(my_intention != null);
      return my_intention.agree();
    }

    public bool child_acceptance_of_vaccine()
    {
      int n = (int)BehaviorEnum.AcceptVaccineForChild;
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      if (this.health_decision_maker != null)
      {
        printf("Asking health_decision_maker %d \n", this.health_decision_maker.get_id());
        return this.health_decision_maker.child_acceptance_of_vaccine();
      }
      // I am the health decision maker
      var my_intention = this.Intentions[n];
      assert(my_intention != null);
      return my_intention.agree();
    }

    public bool child_acceptance_of_another_vaccine_dose()
    {
      int n = (int)BehaviorEnum.AcceptVaccineDoseForChild;
      var bParams = BehaviorParams[n];
      if (bParams.IsEnabled == false) {
        return false;
      }
      if (this.health_decision_maker != null)
      {
        return this.health_decision_maker.child_acceptance_of_another_vaccine_dose();
      }
      // I am the health decision maker
      var my_intention = this.Intentions[n];
      assert(my_intention != null);
      return my_intention.agree();
    }

    private Person select_adult(Household h, int relationship, Person self)
    {
      int N = h.get_size();
      if (relationship == Global.GRANDCHILD)
      {

        // select first adult natural child or spouse thereof of householder parent, if any
        for (int i = 0; i < N; ++i)
        {
          Person person = h.get_enrollee(i);
          if (person.is_adult() == false || person == self)
          {
            continue;
          }
          int r = person.get_relationship();
          if (r == Global::SPOUSE || r == Global::CHILD || r == Global::SIBLING || r == Global::IN_LAW)
          {
            return person;
          }
        }

        // consider adult relative of householder, if any
        for (int i = 0; i < N; ++i)
        {
          Person* person = h.get_enrollee(i);
          if (person.is_adult() == false || person == self)
          {
            continue;
          }
          int r = person.get_relationship();
          if (r == Global::PARENT || r == Global::OTHER_RELATIVE)
          {
            return person;
          }
        }
      }

      // select householder if an adult
      for (int i = 0; i < N; ++i)
      {
        Person* person = h.get_enrollee(i);
        if (person.is_adult() == false || person == self)
        {
          continue;
        }
        if (person.get_relationship() == Global::HOUSEHOLDER)
        {
          return person;
        }
      }

      // select householder's spouse if an adult
      for (int i = 0; i < N; ++i)
      {
        Person* person = h.get_enrollee(i);
        if (person.is_adult() == false || person == self)
        {
          continue;
        }
        if (person.get_relationship() == Global::SPOUSE)
        {
          return person;
        }
      }

      // select oldest available person
      int max_age = self.get_age();

      Person* oldest = null;
      for (int i = 0; i < N; ++i)
      {
        Person* person = h.get_enrollee(i);
        if (person.get_age() <= max_age || person == self)
        {
          continue;
        }
        oldest = person;
        max_age = oldest.get_age();
      }
      return oldest;
    }

    void terminate(Person* self)
    {
      if (!Global::Enable_Behaviors)
      {
        return;
      }
      if (this.health_decision_maker != null)
      {
        return;
      }
      if (Global::Verbose > 1)
      {
        printf("terminating behavior for agent %d age %d\n", self.get_id(), self.get_age());
      }

      // see if this person is the adult decision maker for any child in the household
      Household* hh = static_cast<Household*>(self.get_household());
      if (hh == null)
      {
        if (Global::Enable_Hospitals && self.is_hospitalized() && self.get_permanent_household() != null)
        {
          hh = static_cast<Household*>(self.get_permanent_household()); ;
        }
      }
      int size = hh.get_size();
      for (int i = 0; i < size; ++i)
      {
        Person child = hh.get_enrollee(i);
        if (child != self && child.get_health_decision_maker() == self)
        {
          if (Global::Verbose > 1)
          {
            printf("need new decision maker for agent %d age %d\n", child.get_id(), child.get_age());
          }
          child.setup_behavior();
          if (Global::Verbose > 1)
          {
            printf("new decision maker is %d age %d\n", child.get_health_decision_maker().get_id(),
             child.get_health_decision_maker().get_age());
            fflush(stdout);
          }
        }
      }
    }

    void become_health_decision_maker(Person self)
    {
      if (this.health_decision_maker != null)
      {
        this.health_decision_maker = null;
        delete_intentions();
        setup_intentions(self);
      }
    }
  }
}
