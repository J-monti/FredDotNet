using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Person
  {
    // id: Person's unique identifier (never reused)
    private int id;
    // index: Person's location in population container; once set, will be unique at any given time,
    // but can be reused over the course of the simulation for different people (after death/removal)
    private int index;
    private int exposed_household_index;
    private readonly Health health = new Health();
    private readonly Demographics demographics = new Demographics();
    private readonly Activities activities = new Activities();
    private readonly Behavior behavior = new Behavior();

    public Person()
    {
      this.id = -1;
      this.index = -1;
      this.exposed_household_index = -1;
    }

    protected void setup(int _index, int _id, int age, char sex,
       int race, int rel, Place house, Place school, Place work,
       int day, bool today_is_birthday)
    {
      Utils.FRED_VERBOSE(1, "Person.setup() id {0} age {1} house {2} school {3} work {4}",
         _id, age, house.get_label(), school != null ? school.get_label() : "null", work != null ? work.get_label() : "null");
      int myFIPS;
      this.index = _index;
      this.id = _id;
      this.demographics.setup(this, age, sex, race, rel, day, today_is_birthday);
      this.health.setup(this);
      this.activities.setup(this, house, school, work);
      Utils.FRED_VERBOSE(1, "Person.setup() activities_setup finished\n");

      // behavior setup called externally, after entire population is available

      myFIPS = house.get_household_fips();
      if (today_is_birthday)
      {
        Utils.FRED_VERBOSE(1, "Baby index {0} id {1} age {2} born on day {3} household = {4}  new_size {5} orig_size {6}",
         _index, this.id, age, day, house.get_label(), house.get_size(), house.get_orig_size());
      }
      else
      {
        // residual immunity does NOT apply to newborns
        for (int disease = 0; disease < Global.Diseases.get_number_of_diseases(); ++disease)
        {
          var dis = Global.Diseases.get_disease(disease);

          if (Global.Residual_Immunity_by_FIPS)
          {
            var temp_map = new Age_Map();
            List<double> temp_ages = dis.get_residual_immunity().get_ages();
            List<double> temp_values = dis.get_residual_immunity_values_by_FIPS(myFIPS);
            temp_map.set_ages(temp_ages);
            temp_map.set_values(temp_values);
            double residual_immunity_by_fips_prob = temp_map.find_value(this.get_real_age());
            if (FredRandom.NextDouble() < residual_immunity_by_fips_prob)
            {
              become_immune(dis);
            }
          }
          else if (!dis.get_residual_immunity().is_empty())
          {
            double residual_immunity_prob = dis.get_residual_immunity().find_value(this.get_real_age());
            if (FredRandom.NextDouble() < residual_immunity_prob)
            {
              become_immune(dis);
            }
          }
        }
      }
    }

    /**
   * Make this agent unsusceptible to the given disease
   * @param disease_id the id of the disease to reference
   */
    public void become_unsusceptible(int disease_id)
    {
      this.health.become_unsusceptible(disease_id);
    }

    /**
     * Make this agent unsusceptible to the given disease
     * @param disease the disease to reference
     */
    public void become_unsusceptible(Disease disease)
    {
      this.health.become_unsusceptible(disease);
    }

    public void become_exposed(int disease_id, Person infector, Mixing_Group mixing_group, int day)
    {
      this.health.become_exposed(disease_id, infector, mixing_group, day);
    }
    /**
     * Make this agent immune to the given disease
     * @param disease the disease to reference
     */
    public void become_immune(Disease disease)
    {
      int disease_id = disease.get_id();
      if (this.health.is_susceptible(disease_id))
      {
        this.health.become_immune(disease);
      }
    }

    public void print(TextWriter fp, int disease)
    {
      if (fp == null)
      {
        return;
      }

      fp.Write("{0} id {1.#######}  a {2.###}  s {3} r {4} ",
              disease, id,
              this.demographics.get_age(),
              this.demographics.get_sex(),
              this.demographics.get_race());
      fp.Write("exp: {0.##} ",
              this.health.get_exposure_date(disease));
      fp.Write("infected_at{0} {1.######} ",
              this.health.get_infected_mixing_group_type(disease),
        this.health.get_infected_mixing_group_id(disease));
      fp.Write("infector {0} ", health.get_infector_id(disease));
      fp.Write("infectees {0} ", this.health.get_infectees(disease));
      /*
      fprintf(fp, "antivirals: %2d ", this.health.get_number_av_taken());
      for(int i=0; i < this.health.get_number_av_taken(); ++i) {
        fprintf(fp," %2d", this.health.get_av_start_day(i));
      }
      */
      fp.Flush();
    }

    public void infect(Person infectee, int disease_id, Mixing_Group mixing_group, int day)
    {
      this.health.infect(infectee, disease_id, mixing_group, day);
    }

    /**
     * @param day the simulation day
     * @see Demographics.update(int day)
     */
    public void update_demographics(int day)
    {
      this.demographics.update(day);
    }

    public void update_infection(int day, int disease_id)
    {
      this.health.update_infection(day, disease_id);
    }

    public void update_health_interventions(int day)
    {
      this.health.update_interventions(day);
    }

    /**
     * @param day the simulation day
     * @see Behavior.update(int day)
     */
    public void update_behavior(int day)
    {
      this.behavior.update(this, day);
    }

    /**
     * @Activities.prepare()
     */
    public void prepare_activities()
    {
      this.activities.prepare();
    }

    public bool is_present(int sim_day, Place place)
    {
      return this.activities.is_present(sim_day, place);
    }

    public void update_schedule(int sim_day)
    {
      this.activities.update_schedule(sim_day);
    }

    public void update_activities_of_infectious_person(int sim_day)
    {
      this.activities.update_activities_of_infectious_person(sim_day);
    }

    public void update_enrollee_index(Mixing_Group mixing_group, int pos)
    {
      this.activities.update_enrollee_index(mixing_group, pos);
    }

    /**
     * @Activities.update_profile()
     */
    public void update_activity_profile()
    {
      this.activities.update_profile();
    }

    public void become_susceptible(int disease_id)
    {
      this.health.become_susceptible(disease_id);
    }

    public void become_susceptible_by_vaccine_waning(int disease_id)
    {
      this.health.become_susceptible_by_vaccine_waning(disease_id);
    }

    public void update_household_counts(int day, int disease_id)
    {
      // this is only called for people with an active infection.
      var hh = this.get_household();
      if (hh == null)
      {
        if (Global.Enable_Hospitals && this.is_hospitalized() && this.get_permanent_household() != null)
        {
          hh = this.get_permanent_household();
        }
      }
      if (hh != null)
      {
        this.health.update_mixing_group_counts(day, disease_id, hh);
      }
    }

    public void update_school_counts(int day, int disease_id)
    {
      // this is only called for people with an active infection.
      var school = this.get_school();
      if (school != null)
      {
        this.health.update_mixing_group_counts(day, disease_id, school);
      }
    }

    /**
     * This agent will become infectious with the disease
     * @param disease a pointer to the Disease
     */
    public void become_infectious(Disease disease)
    {
      this.health.become_infectious(disease);
    }

    public void become_noninfectious(Disease disease)
    {
      this.health.become_noninfectious(disease);
    }

    /**
     * This agent will become symptomatic with the disease
     * @param disease a pointer to the Disease
     */
    public void become_symptomatic(Disease disease)
    {
      this.health.become_symptomatic(disease);
    }

    public void resolve_symptoms(Disease disease)
    {
      this.health.resolve_symptoms(disease);
    }

    /**
     * This agent will recover from the disease
     * @param disease a pointer to the Disease
     */
    public void recover(int day, Disease disease)
    {
      this.health.recover(disease, day);
    }

    public void become_case_fatality(int day, Disease disease)
    {
      this.health.become_case_fatality(disease.get_id(), day);
    }

    /**
     * This agent creates a new agent
     * @return a pointer to the new Person
     */
    public Person give_birth(int day)
    {
      int age = 0;
      char sex = (FredRandom.NextDouble() < 0.5 ? 'M' : 'F');
      int race = get_race();
      int rel = Global.CHILD;
      var house = this.get_household();
      if (house == null)
      {
        if (Global.Enable_Hospitals && this.is_hospitalized() && this.get_permanent_household() != null)
        {
          house = this.get_permanent_household();
        }
      }
      /*
        if (house == null) {
        printf("Mom %d has no household\n", this.get_id()); fflush(stdout);
        }
      */
      Utils.assert(house != null);
      Place school = null;
      Place work = null;
      bool today_is_birthday = true;
      var baby = Global.Pop.add_person(age, sex, race, rel,
              house, school, work, day, today_is_birthday);

      if (Global.Enable_Population_Dynamics)
      {
        baby.get_demographics().initialize_demographic_dynamics(baby);
      }

      if (Global.Enable_Behaviors)
      {
        // turn mother into an adult decision maker, if not already
        if (this.is_health_decision_maker() == false)
        {
          Utils.FRED_VERBOSE(0,
           "young mother {0} age {1} becomes baby's health decision maker on day {2}\n",
           id, age, day);
          this.become_health_decision_maker(this);
        }
        // let mother decide health behaviors for child
        baby.set_health_decision_maker(this);
      }
      this.demographics.update_birth_stats(day, this);
      Utils.FRED_VERBOSE(1, "mother {0} baby {1}\n", this.get_id(), baby.get_id());

      return baby;
    }

    /**
     * Assign the agent to a Classroom
     * @see Activities.assign_classroom()
     */
    public void assign_classroom()
    {
      if (this.activities.get_school() != null)
      {
        this.activities.assign_classroom();
      }
    }

    /**
     * Assign the agent to an Office
     * @see Activities.assign_office()
     */
    public void assign_office()
    {
      this.activities.assign_office();
    }

    /**
     * Assign the agent a primary health care facility
     * @see Activities.assign_primary_healthcare_facility()
     */
    public void assign_primary_healthcare_facility()
    {
      this.activities.assign_primary_healthcare_facility();
    }

    /**
     * Will print out a person in a format similar to that read from population file
     * (with additional run-time values inserted (denoted by *)):<br />
     * (i.e Label *ID* Age Sex Married Occupation Household School *Classroom* Workplace *Office*)
     * @return a string representation of this Person object
     */
    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.Append($"{this.id} {get_age()} {get_sex()} ");
      builder.Append($"{get_race()} ");
      builder.Append(Place.get_place_label(get_household()));
      builder.Append(Place.get_place_label(get_school()));
      builder.Append(Place.get_place_label(get_classroom()));
      builder.Append(Place.get_place_label(get_workplace()));
      builder.Append(Place.get_place_label(get_office()));
      builder.Append(Place.get_place_label(get_neighborhood()));
      builder.Append(Place.get_place_label(get_hospital()));
      builder.Append(Place.get_place_label(get_ad_hoc()));
      builder.Append(get_relationship());
      return builder.ToString();
    }

    // access functions:
    /**
     * The id is generated at runtime
     * @return the id of this Person
     */
    public int get_id()
    {
      return this.id;
    }

    /**
     * @return a pointer to this Person's Demographics
     */
    public Demographics get_demographics()
    {
      return this.demographics;
    }

    /**
     * @return the Person's age
     * @see Demographics.get_age()
     */
    public int get_age()
    {
      return this.demographics.get_age();
    }

    /**
     * @return the Person's initial age
     * @see Demographics.get_init_age()
     */
    public int get_init_age()
    {
      return this.demographics.get_init_age();
    }

    /**
     * @return the Person's age as a double value based on the number of days alive
     * @see Demographics.get_real_age()
     */
    public double get_real_age()
    {
      return this.demographics.get_real_age();
    }

    /**
     * @return the Person's sex
     */
    public char get_sex()
    {
      return this.demographics.get_sex();
    }

    /**
     * @return the Person's race
     * @see Demographics.get_race()
     */
    public int get_race()
    {
      return this.demographics.get_race();
    }

    public int get_relationship()
    {
      return this.demographics.get_relationship();
    }

    public void set_relationship(int rel)
    {
      this.demographics.set_relationship(rel);
    }

    /**
     * @return <code>true</code> if this agent is deceased, <code>false</code> otherwise
     */
    public bool is_deceased()
    {
      return this.demographics.is_deceased();
    }

    /**
     * @return <code>true</code> if this agent is an adult, <code>false</code> otherwise
     */
    public bool is_adult()
    {
      return this.demographics.get_age() >= Global.ADULT_AGE;
    }

    /**
     * @return <code>true</code> if this agent is a child, <code>false</code> otherwise
     */
    public bool is_child()
    {
      return this.demographics.get_age() < Global.ADULT_AGE;
    }

    /**
     * @return a pointer to this Person's Health
     */
    public Health get_health()
    {
      return this.health;
    }

    /**
     * @return <code>true</code> if this agent is symptomatic, <code>false</code> otherwise
     * @see Health.is_symptomatic()
     */
    public int is_symptomatic()
    {
      return this.health.is_symptomatic() ? 1 : 0;
    }

    public int is_symptomatic(int disease_id)
    {
      return this.health.is_symptomatic(disease_id) ? 1 : 0;
    }

    public int get_days_symptomatic()
    {
      return this.health.get_days_symptomatic();
    }

    public bool is_immune(int disease_id)
    {
      return this.health.is_immune(disease_id);
    }

    /**
     * @param dis the disease to check
     * @return <code>true</code> if this agent is susceptible to disease, <code>false</code> otherwise
     * @see Health.is_susceptible(int dis)
     */
    public bool is_susceptible(int dis)
    {
      return this.health.is_susceptible(dis);
    }

    /**
     * @param dis the disease to check
     * @return <code>true</code> if this agent is infectious with disease, <code>false</code> otherwise
     * @see Health.is_infectious(int disease)
     */
    public bool is_infectious(int dis)
    {
      return this.health.is_infectious(dis);
    }

    public bool is_recovered(int dis)
    {
      return this.health.is_recovered(dis);
    }

    /**
     * @param dis the disease to check
     * @return <code>true</code> if this agent is infected with disease, <code>false</code> otherwise
     * @see Health.is_infected(int disease)
     */
    public bool is_infected(int dis)
    {
      return this.health.is_infected(dis);
    }

    /**
     * @param disease the disease to check
     * @return the specific Disease's susceptibility for this Person
     * @see Health.get_susceptibility(int disease)
     */
    public double get_susceptibility(int disease)
    {
      return this.health.get_susceptibility(disease);
    }

    /**
     * @param disease the disease to check
     * @param day the simulation day
     * @return the specific Disease's infectivity for this Person
     * @see Health.get_infectivity(int disease, int day)
     */
    public double get_infectivity(int disease_id, int day)
    {
      return this.health.get_infectivity(disease_id, day);
    }

    /**
     * @param disease the disease to check
     * @param day the simulation day
     * @return the Symptoms for this Person
     * @see Health.get_symptoms(int day)
     */
    public double get_symptoms(int disease_id, int day)
    {
      return this.health.get_symptoms(disease_id, day);
    }

    /*
     * Advances the course of the infection by moving the exposure date
     * backwards
     */
    public void advance_seed_infection(int disease_id, int days_to_advance)
    {
      this.health.advance_seed_infection(disease_id, days_to_advance);
    }

    /**
     * @param disease the disease to check
     * @return the simulation day that this agent became exposed to disease
     */
    public int get_exposure_date(int disease)
    {
      return this.health.get_exposure_date(disease);
    }

    public int get_infectious_start_date(int disease)
    {
      return this.health.get_infectious_start_date(disease);
    }

    public int get_infectious_end_date(int disease)
    {
      return this.health.get_infectious_end_date(disease);
    }

    public int get_symptoms_start_date(int disease)
    {
      return this.health.get_symptoms_start_date(disease);
    }

    public int get_symptoms_end_date(int disease)
    {
      return this.health.get_symptoms_end_date(disease);
    }

    public int get_immunity_end_date(int disease)
    {
      return this.health.get_immunity_end_date(disease);
    }

    /**
     * @param disease the disease to check
     * @return the Person who infected this agent with disease
     */
    public Person get_infector(int disease)
    {
      return this.health.get_infector(disease);
    }

    /**
     * @param disease the disease to check
     * @return the id of the location where this agent became infected with disease
     */
    public int get_infected_mixing_group_id(int disease)
    {
      return this.health.get_infected_mixing_group_id(disease);
    }

    /**
     * @param disease the disease to check
     * @return the pointer to the Place where this agent became infected with disease
     */
    public Mixing_Group get_infected_mixing_group(int disease)
    {
      return this.health.get_infected_mixing_group(disease);
    }

    /**
     * @param disease the disease to check
     * @return the label of the location where this agent became infected with disease
     */
    public string get_infected_mixing_group_label(int disease)
    {
      return this.health.get_infected_mixing_group_label(disease);
    }

    /**
     * @param disease the disease to check
     * @return the type of the location where this agent became infected with disease
     */
    public char get_infected_mixing_group_type(int disease)
    {
      return this.health.get_infected_mixing_group_type(disease);
    }

    /**
     * @param disease the disease in question
     * @return the infectees
     * @see Health.get_infectees(int disease)
     */
    public int get_infectees(int disease)
    {
      return this.health.get_infectees(disease);
    }

    /**
     * @return a pointer to this Person's Activities
     */
    public Activities get_activities()
    {
      return this.activities;
    }

    /**
     * @return the a pointer to this agent's Neighborhood
     */
    public Place get_neighborhood()
    {
      return this.activities.get_neighborhood();
    }

    public void reset_neighborhood()
    {
      this.activities.reset_neighborhood();
    }

    /**
     * @return a pointer to this Person's Household
     * @see Activities.get_household()
     */
    public Place get_household()
    {
      return this.activities.get_household();
    }

    public int get_exposed_household_index()
    {
      return this.exposed_household_index;
    }

    public void set_exposed_household(int index_)
    {
      this.exposed_household_index = index_;
    }

    public Place get_permanent_household()
    {
      return this.activities.get_permanent_household();
    }

    public char get_deme_id()
    {
      return this.activities.get_deme_id();
    }

    public bool is_householder()
    {
      return this.demographics.is_householder();
    }

    public void make_householder()
    {
      this.demographics.make_householder();
    }

    /**
     * @return a pointer to this Person's School
     * @see Activities.get_school()
     */
    public Place get_school()
    {
      return this.activities.get_school();
    }

    /**
     * @return a pointer to this Person's Classroom
     * @see Activities.get_classroom()
     */
    public Place get_classroom()
    {
      return this.activities.get_classroom();
    }

    /**
     * @return a pointer to this Person's Workplace
     * @see Activities.get_workplace()
     */
    public Place get_workplace()
    {
      return this.activities.get_workplace();
    }

    /**
     * @return a pointer to this Person's Office
     * @see Activities.get_office()
     */
    public Place get_office()
    {
      return this.activities.get_office();
    }

    /**
     * @return a pointer to this Person's Hospital
     * @see Activities.get_hospital()
     */
    public Place get_hospital()
    {
      return this.activities.get_hospital();
    }

    /**
     * @return a pointer to this Person's Ad Hoc location
     * @see Activities.get_ad_hoc()
     */
    public Place get_ad_hoc()
    {
      return this.activities.get_ad_hoc();
    }

    /**
     *  @return the number of other agents in an agent's neighborhood, school, and workplace.
     *  @see Activities.get_degree()
     */
    public int get_degree()
    {
      return this.activities.get_degree();
    }

    public int get_household_size()
    {
      return this.activities.get_group_size(Activity_index.HOUSEHOLD_ACTIVITY);
    }

    public int get_neighborhood_size()
    {
      return this.activities.get_group_size(Activity_index.NEIGHBORHOOD_ACTIVITY);
    }

    public int get_school_size()
    {
      return this.activities.get_group_size(Activity_index.SCHOOL_ACTIVITY);
    }

    public int get_classroom_size()
    {
      return this.activities.get_group_size(Activity_index.CLASSROOM_ACTIVITY);
    }

    public int get_workplace_size()
    {
      return this.activities.get_group_size(Activity_index.WORKPLACE_ACTIVITY);
    }

    public int get_office_size()
    {
      return this.activities.get_group_size(Activity_index.OFFICE_ACTIVITY);
    }

    public bool is_hospitalized()
    {
      return this.activities.get_is_hospitalized();
    }

    /**
     * Have this Person begin traveling
     * @param visited the Person this agent will visit
     * @see Activities.start_traveling(Person* visited)
     */
    public void start_traveling(Person visited)
    {
      this.activities.start_traveling(visited);
    }

    /**
     * Have this Person stop traveling
     * @see Activities.stop_traveling()
     */
    public void stop_traveling()
    {
      this.activities.stop_traveling();
    }

    /**
     * @return <code>true</code> if the Person is traveling, <code>false</code> if not
     * @see Activities.get_travel_status()
     */
    public bool get_travel_status()
    {
      return this.activities.get_travel_status();
    }

    public int get_num_past_infections(int disease)
    {
      return this.health.get_num_past_infections(disease);
    }

    public Past_Infection get_past_infection(int disease, int i)
    {
      return this.health.get_past_infection(disease, i);
    }

    public void clear_past_infections(int disease)
    {
      this.health.clear_past_infections(disease);
    }

    //void add_past_infection(int d, Past_Infection *pi){ health.add_past_infection(d, pi); }  
    public void add_past_infection(int strain_id, int recovery_date, int age_at_exposure, Disease dis)
    {
      this.health.add_past_infection(strain_id, recovery_date, age_at_exposure, dis);
    }

    public void take_vaccine(Vaccine vacc, int day, Vaccine_Manager vm)
    {
      this.health.take_vaccine(vacc, day, vm);
    }

    // set up and access health behaviors
    public void setup_behavior()
    {
      this.behavior.setup(this);
    }

    public bool is_health_decision_maker()
    {
      return this.behavior.is_health_decision_maker();
    }

    public Person get_health_decision_maker()
    {
      return this.behavior.get_health_decision_maker();
    }

    public void set_health_decision_maker(Person p)
    {
      this.behavior.set_health_decision_maker(p);
    }

    public void become_health_decision_maker(Person self)
    {
      this.behavior.become_health_decision_maker(self);
    }

    public bool adult_is_staying_home()
    {
      return this.behavior.adult_is_staying_home();
    }

    public bool child_is_staying_home()
    {
      return this.behavior.child_is_staying_home();
    }

    public bool acceptance_of_vaccine()
    {
      return this.behavior.acceptance_of_vaccine();
    }

    public bool acceptance_of_another_vaccine_dose()
    {
      return this.behavior.acceptance_of_another_vaccine_dose();
    }

    public bool child_acceptance_of_vaccine()
    {
      return this.behavior.child_acceptance_of_vaccine();
    }

    public bool child_acceptance_of_another_vaccine_dose()
    {
      return this.behavior.child_acceptance_of_another_vaccine_dose();
    }

    public bool is_sick_leave_available()
    {
      return this.activities.is_sick_leave_available();
    }

    public double get_transmission_modifier_due_to_hygiene(int disease_id)
    {
      return this.health.get_transmission_modifier_due_to_hygiene(disease_id);
    }

    public double get_susceptibility_modifier_due_to_hygiene(int disease_id)
    {
      return this.health.get_susceptibility_modifier_due_to_hygiene(disease_id);
    }

    public bool is_case_fatality(int disease_id)
    {
      return this.health.is_case_fatality(disease_id);
    }

    public void terminate(int day)
    {
      Utils.FRED_VERBOSE(1, "terminating person {0}\n", id);
      this.behavior.terminate(this);
      this.activities.terminate();
      this.health.terminate(day);
      this.demographics.terminate(this);
    }

    public void set_pop_index(int idx)
    {
      this.index = idx;
    }

    public int get_pop_index()
    {
      return this.index;
    }

    public void birthday(int day)
    {
      this.demographics.birthday(this, day);
    }

    public bool become_a_teacher(Place school)
    {
      return this.activities.become_a_teacher(school);
    }

    public bool is_teacher()
    {
      return this.activities.is_teacher();
    }

    public bool is_student()
    {
      return this.activities.is_student();
    }

    public void move_to_new_house(Place house)
    {
      activities.move_to_new_house(house);
    }

    public void change_school(Place place)
    {
      this.activities.change_school(place);
    }

    public void change_workplace(Place place)
    {
      this.activities.change_workplace(place);
    }

    public void change_workplace(Place place, int include_office)
    {
      this.activities.change_workplace(place, include_office);
    }

    public int get_visiting_health_status(Place place, int day, int disease_id)
    {
      return this.activities.get_visiting_health_status(place, day, disease_id);
    }

    /**
     * @return <code>true</code> if agent is asthmatic, <code>false</code> otherwise
     * @see Health.is_asthmatic()
     */
    public bool is_asthmatic()
    {
      return this.health.is_asthmatic();
    }

    /**
     * @return <code>true</code> if agent has COPD (Chronic Obstructive Pulmonary
     * disease), <code>false</code> otherwise
     * @see Health.has_COPD()
     */
    public bool has_COPD()
    {
      return this.health.has_COPD();
    }

    /**
     * @return <code>true</code> if agent has chronic renal disease, <code>false</code> otherwise
     * @see Health.has_chronic_renal_disease()
     */
    public bool has_chronic_renal_disease()
    {
      return this.health.has_chronic_renal_disease();
    }

    /**
     * @return <code>true</code> if agent is diabetic, <code>false</code> otherwise
     * @see Health.is_diabetic()
     */
    public bool is_diabetic()
    {
      return this.health.is_diabetic();
    }

    /**
     * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
     *  @see Health.has_heart_disease()
     */
    public bool has_heart_disease()
    {
      return this.health.has_heart_disease();
    }

    /**
     * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
     *  @see Health.has_hypertension()
     */
    public bool has_hypertension()
    {
      return this.health.has_hypertension();
    }

    /**
     * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
     *  @see Health.has_hypercholestrolemia()
     */
    public bool has_hypercholestrolemia()
    {
      return this.health.has_hypercholestrolemia();
    }

    /**
     * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
     *  @see Health.has_chronic_condition()
     */
    public bool has_chronic_condition()
    {
      return this.health.has_chronic_condition();
    }

    /**
     * @return <code>true</code> if agent is alive, <code>false</code> otherwise
     * @see Health.is_alive()
     */
    public bool is_alive()
    {
      return this.health.is_alive();
    }

    /**
     * @return <code>true</code> if agent is dead, <code>false</code> otherwise
     * @see Health.is_alive()
     */
    public bool is_dead()
    {
      return this.health.is_alive() == false;
    }

    public double get_x()
    {
      var hh = this.get_household();

      if (hh == null)
      {
        if (Global.Enable_Hospitals && this.is_hospitalized() && this.get_permanent_household() != null)
        {
          hh = this.get_permanent_household();
        }
      }

      if (hh == null)
      {
        return 0.0;
      }
      else
      {
        return hh.get_x();
      }
    }

    public double get_y()
    {
      var hh = this.get_household();

      if (hh == null)
      {
        if (Global.Enable_Hospitals && this.is_hospitalized() && this.get_permanent_household() != null)
        {
          hh = this.get_permanent_household();
        }
      }

      if (hh == null)
      {
        return 0.0;
      }
      else
      {
        return hh.get_y();
      }
    }

    public bool is_prisoner()
    {
      return this.activities.is_prisoner();
    }

    public bool is_college_dorm_resident()
    {
      return this.activities.is_college_dorm_resident();
    }

    public bool is_military_base_resident()
    {
      return this.activities.is_military_base_resident();
    }

    public bool is_nursing_home_resident()
    {
      return this.activities.is_nursing_home_resident();
    }

    public bool lives_in_group_quarters()
    {
      return (is_college_dorm_resident() || is_military_base_resident()
        || is_prisoner() || is_nursing_home_resident());
    }

    public char get_profile()
    {
      return this.activities.get_profile();
    }

    public int get_number_of_children()
    {
      return this.demographics.get_number_of_children();
    }

    public int get_grade()
    {
      return this.activities.get_grade();
    }

    void set_grade(int n)
    {
      this.activities.set_grade(n);
    }

    // convenience methods for Networks

    public void create_network_link_to(Person person, Network network)
    {
      this.activities.create_network_link_to(person, network);
    }

    public void destroy_network_link_to(Person person, Network network)
    {
      this.activities.destroy_network_link_to(person, network);
    }

    public void create_network_link_from(Person person, Network network)
    {
      this.activities.create_network_link_from(person, network);
    }

    public void destroy_network_link_from(Person person, Network network)
    {
      this.activities.destroy_network_link_from(person, network);
    }

    public void add_network_link_to(Person person, Network network)
    {
      this.activities.add_network_link_to(person, network);
    }

    public void add_network_link_from(Person person, Network network)
    {
      this.activities.add_network_link_from(person, network);
    }

    public void delete_network_link_to(Person person, Network network)
    {
      this.activities.delete_network_link_to(person, network);
    }

    public void delete_network_link_from(Person person, Network network)
    {
      this.activities.delete_network_link_from(person, network);
    }

    public void join_network(Network network)
    {
      this.activities.join_network(network);
    }

    public void print_network(TextWriter fp, Network network)
    {
      this.activities.print_network(fp, network);
    }

    public bool is_connected_to(Person person, Network network)
    {
      return this.activities.is_connected_to(person, network);
    }

    public bool is_connected_from(Person person, Network network)
    {
      return this.activities.is_connected_from(person, network);
    }

    public int get_out_degree(Network network)
    {
      return this.activities.get_out_degree(network);
    }

    public int get_in_degree(Network network)
    {
      return this.activities.get_in_degree(network);
    }

    public void clear_network(Network network)
    {
      this.activities.clear_network(network);
    }

    public Person get_end_of_link(int n, Network network)
    {
      return this.activities.get_end_of_link(n, network);
    }

    public int get_health_state(int disease_id)
    {
      return this.health.get_health_state(disease_id);
    }

    public void set_health_state(int disease_id, int s, int day)
    {
      this.health.set_health_state(disease_id, s, day);
    }

    public int get_last_health_transition_day(int disease_id)
    {
      return this.health.get_last_transition_day(disease_id);
    }

    public int get_next_health_state(int disease_id)
    {
      return this.health.get_next_health_state(disease_id);
    }

    public void set_next_health_state(int disease_id, int s, int day)
    {
      this.health.set_next_health_state(disease_id, s, day);
    }

    public int get_next_health_transition_day(int disease_id)
    {
      return this.health.get_next_transition_day(disease_id);
    }

    public void update_health_conditions(int day)
    {
      this.health.update_health_conditions(day);
    }
  }
}
