using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Person
  {
    private Place house;
    private Place school;
    private Place work;

    public Person(int age, Gender sex, int race, int relationship, Place house, Place school, Place work, DateTime day, bool todayIsBirthday)
    {
      this.house = house;
      this.school = school;
      this.work = work;
      this.Demographics = new Demographics(this, age, sex, race, relationship, day, todayIsBirthday);
      this.Health = new Health(this);
      this->activities.setup(this, house, school, work);

      // behavior setup called externally, after entire population is available

      myFIPS = house->get_household_fips();
      if (todayIsBirthday)
      {
      }
      else
      {
        // residual immunity does NOT apply to newborns
        for (int disease = 0; disease < Global::Diseases.get_number_of_diseases(); ++disease)
        {
          Disease* dis = Global::Diseases.get_disease(disease);

          if (Global::Residual_Immunity_by_FIPS)
          {
            Age_Map* temp_map = new Age_Map();
            vector<double> temp_ages = dis->get_residual_immunity()->get_ages();
            vector<double> temp_values = dis->get_residual_immunity_values_by_FIPS(myFIPS);
            temp_map->set_ages(temp_ages);
            temp_map->set_values(temp_values);
            double residual_immunity_by_fips_prob = temp_map->find_value(this->get_real_age());
            if (Random::draw_random() < residual_immunity_by_fips_prob)
            {
              become_immune(dis);
            }
          }
          else if (!dis->get_residual_immunity()->is_empty())
          {
            double residual_immunity_prob = dis->get_residual_immunity()->find_value(this->get_real_age());
            if (Random::draw_random() < residual_immunity_prob)
            {
              become_immune(dis);
            }
          }
        }
      }
    }

    public Health Health { get; }

    public Demographics Demographics { get; }



    void Person::print(FILE* fp, int disease)
    {
      if (fp == NULL)
      {
        return;
      }
      fprintf(fp, "%d id %7d  a %3d  s %c r %d ",
              disease, id,
              this->demographics.get_age(),
              this->demographics.get_sex(),
              this->demographics.get_race());
      fprintf(fp, "exp: %2d ",
              this->health.get_exposure_date(disease));
      fprintf(fp, "infected_at %c %6d ",
              this->health.get_infected_mixing_group_type(disease),
        this->health.get_infected_mixing_group_id(disease));
      fprintf(fp, "infector %d ", health.get_infector_id(disease));
      fprintf(fp, "infectees %d ", this->health.get_infectees(disease));
      /*
      fprintf(fp, "antivirals: %2d ", this->health.get_number_av_taken());
      for(int i=0; i < this->health.get_number_av_taken(); ++i) {
        fprintf(fp," %2d", this->health.get_av_start_day(i));
      }
      */
      fprintf(fp, "\n");
      fflush(fp);
    }

    void Person::update_household_counts(int day, int disease_id)
    {
      // this is only called for people with an active infection.
      Mixing_Group* hh = this->get_household();
      if (hh == NULL)
      {
        if (Global::Enable_Hospitals && this->is_hospitalized() && this->get_permanent_household() != NULL)
        {
          hh = this->get_permanent_household();
        }
      }
      if (hh != NULL)
      {
        this->health.update_mixing_group_counts(day, disease_id, hh);
      }
    }

    void Person::update_school_counts(int day, int disease_id)
    {
      // this is only called for people with an active infection.
      Mixing_Group* school = this->get_school();
      if (school != NULL)
      {
        this->health.update_mixing_group_counts(day, disease_id, school);
      }
    }

    void Person::become_immune(Disease* disease)
    {
      int disease_id = disease->get_id();
      if (this->health.is_susceptible(disease_id))
      {
        this->health.become_immune(disease);
      }
    }

    Person* Person::give_birth(int day)
    {
      int age = 0;
      char sex = (Random::draw_random(0.0, 1.0) < 0.5 ? 'M' : 'F');
      int race = get_race();
      int rel = Global::CHILD;
      Place* house = this->get_household();
      if (house == NULL)
      {
        if (Global::Enable_Hospitals && this->is_hospitalized() && this->get_permanent_household() != NULL)
        {
          house = this->get_permanent_household();
        }
      }
      /*
        if (house == NULL) {
        printf("Mom %d has no household\n", this->get_id()); fflush(stdout);
        }
      */
      assert(house != NULL);
      Place* school = NULL;
      Place* work = NULL;
      bool today_is_birthday = true;
      Person* baby = Global::Pop.add_person(age, sex, race, rel,
              house, school, work, day, today_is_birthday);

      if (Global::Enable_Population_Dynamics)
      {
        baby->get_demographics()->initialize_demographic_dynamics(baby);
      }

      if (Global::Enable_Behaviors)
      {
        // turn mother into an adult decision maker, if not already
        if (this->is_health_decision_maker() == false)
        {
          FRED_VERBOSE(0,
           "young mother %d age %d becomes baby's health decision maker on day %d\n",
           id, age, day);
          this->become_health_decision_maker(this);
        }
        // let mother decide health behaviors for child
        baby->set_health_decision_maker(this);
      }
      this->demographics.update_birth_stats(day, this);
      FRED_VERBOSE(1, "mother %d baby %d\n", this->get_id(), baby->get_id());

      return baby;
    }

    string Person::to_string()
    {

      stringstream tmp_string_stream;
      // (i.e *ID* Age Sex Race Household School Classroom Workplace Office Neighborhood Hospital Ad_Hoc Relationship)
      tmp_string_stream << this->id << " " << get_age() << " " << get_sex() << " ";
      tmp_string_stream << get_race() << " ";
      tmp_string_stream << Place::get_place_label(get_household()) << " ";
      tmp_string_stream << Place::get_place_label(get_school()) << " ";
      tmp_string_stream << Place::get_place_label(get_classroom()) << " ";
      tmp_string_stream << Place::get_place_label(get_workplace()) << " ";
      tmp_string_stream << Place::get_place_label(get_office()) << " ";
      tmp_string_stream << Place::get_place_label(get_neighborhood()) << " ";
      tmp_string_stream << Place::get_place_label(get_hospital()) << " ";
      tmp_string_stream << Place::get_place_label(get_ad_hoc()) << " ";
      tmp_string_stream << get_relationship();

      return tmp_string_stream.str();
    }

    void Person::terminate(int day)
    {
      FRED_VERBOSE(1, "terminating person %d\n", id);
      this->behavior.terminate(this);
      this->activities.terminate();
      this->health.terminate(day);
      this->demographics.terminate(this);
    }

    double Person::get_x()
    {
      Place* hh = this->get_household();

      if (hh == NULL)
      {
        if (Global::Enable_Hospitals && this->is_hospitalized() && this->get_permanent_household() != NULL)
        {
          hh = this->get_permanent_household();
        }
      }

      if (hh == NULL)
      {
        return 0.0;
      }
      else
      {
        return hh->get_x();
      }
    }

    double Person::get_y()
    {
      Place* hh = this->get_household();

      if (hh == NULL)
      {
        if (Global::Enable_Hospitals && this->is_hospitalized() && this->get_permanent_household() != NULL)
        {
          hh = this->get_permanent_household();
        }
      }

      if (hh == NULL)
      {
        return 0.0;
      }
      else
      {
        return hh->get_y();
      }
    }


    void Person::become_case_fatality(int day, Disease* disease)
    {
      this->health.become_case_fatality(disease->get_id(), day);
    }

    /**
     * Make this agent unsusceptible to the given disease
     * @param disease_id the id of the disease to reference
     */
    void become_unsusceptible(int disease_id)
    {
      this->health.become_unsusceptible(disease_id);
    }

    /**
     * Make this agent unsusceptible to the given disease
     * @param disease the disease to reference
     */
    void become_unsusceptible(Disease* disease)
    {
      this->health.become_unsusceptible(disease);
    }

    void become_exposed(int disease_id, Person* infector, Mixing_Group* mixing_group, int day)
    {
      this->health.become_exposed(disease_id, infector, mixing_group, day);
    }

    void infect(Person* infectee, int disease_id, Mixing_Group* mixing_group, int day)
    {
      this->health.infect(infectee, disease_id, mixing_group, day);
    }

    /**
     * @param day the simulation day
     * @see Demographics::update(int day)
     */
    void update_demographics(int day)
    {
      this->demographics.update(day);
    }

    void update_infection(int day, int disease_id)
    {
      this->health.update_infection(day, disease_id);
    }

    void update_health_interventions(int day)
    {
      this->health.update_interventions(day);
    }

    /**
     * @param day the simulation day
     * @see Behavior::update(int day)
     */
    void update_behavior(int day)
    {
      this->behavior.update(this, day);
    }

    /**
     * @Activities::prepare()
     */
    void prepare_activities()
    {
      this->activities.prepare();
    }

    bool is_present(int sim_day, Place* place)
    {
      return this->activities.is_present(sim_day, place);
    }

    void update_schedule(int sim_day)
    {
      this->activities.update_schedule(sim_day);
    }

    void update_activities_of_infectious_person(int sim_day)
    {
      this->activities.update_activities_of_infectious_person(sim_day);
    }

    void update_enrollee_index(Mixing_Group* mixing_group, int pos)
    {
      this->activities.update_enrollee_index(mixing_group, pos);
    }

    /**
     * This agent will become infectious with the disease
     * @param disease a pointer to the Disease
     */
    void become_infectious(Disease* disease)
    {
      this->health.become_infectious(disease);
    }

    void become_noninfectious(Disease* disease)
    {
      this->health.become_noninfectious(disease);
    }

    /**
     * This agent will become symptomatic with the disease
     * @param disease a pointer to the Disease
     */
    void become_symptomatic(Disease* disease)
    {
      this->health.become_symptomatic(disease);
    }

    void resolve_symptoms(Disease* disease)
    {
      this->health.resolve_symptoms(disease);
    }

    /**
     * This agent will recover from the disease
     * @param disease a pointer to the Disease
     */
    void recover(int day, Disease* disease)
    {
      this->health.recover(disease, day);
    }

    /**
     * @Activities::update_profile()
     */
    void update_activity_profile()
    {
      this->activities.update_profile();
    }

    void become_susceptible(int disease_id)
    {
      this->health.become_susceptible(disease_id);
    }

    void become_susceptible_by_vaccine_waning(int disease_id)
    {
      this->health.become_susceptible_by_vaccine_waning(disease_id);
    }

    /**
     * Assign the agent to a Classroom
     * @see Activities::assign_classroom()
     */
    void assign_classroom()
    {
      if (this->activities.get_school())
      {
        this->activities.assign_classroom();
      }
    }

    /**
     * Assign the agent to an Office
     * @see Activities::assign_office()
     */
    void assign_office()
    {
      this->activities.assign_office();
    }

    /**
     * Assign the agent a primary healthjare facility
     * @see Activities::assign_primary_healthcare_facility()
     */
    void assign_primary_healthcare_facility()
    {
      this->activities.assign_primary_healthcare_facility();
    }

    /**
     * @return the Person's age
     * @see Demographics::get_age()
     */
    public int Age
    {
      get { return this.Demographics.Age; }
    }

    /**
     * @return the Person's initial age
     * @see Demographics::get_init_age()
     */
    int get_init_age() const {
    return this->demographics.get_init_age();
  }

  /**
   * @return the Person's age as a double value based on the number of days alive
   * @see Demographics::get_real_age()
   */
  double get_real_age() const {
    return this->demographics.get_real_age();
  }

  /**
   * @return the Person's sex
   */
  char get_sex() const {
    return this->demographics.get_sex();
  }

  /**
   * @return the Person's race
   * @see Demographics::get_race()
   */
  int get_race()
  {
    return this->demographics.get_race();
  }

  int get_relationship()
  {
    return this->demographics.get_relationship();
  }

  void set_relationship(int rel)
  {
    this->demographics.set_relationship(rel);
  }

  /**
   * @return <code>true</code> if this agent is deceased, <code>false</code> otherwise
   */
  bool is_deceased()
  {
    return this->demographics.is_deceased();
  }

  /**
   * @return <code>true</code> if this agent is an adult, <code>false</code> otherwise
   */
  bool is_adult()
  {
    return this->demographics.get_age() >= Global::ADULT_AGE;
  }

  /**
   * @return <code>true</code> if this agent is a chiild, <code>false</code> otherwise
   */
  bool is_child()
  {
    return this->demographics.get_age() < Global::ADULT_AGE;
  }

  /**
   * @return a pointer to this Person's Health
   */
  Health* get_health()
  {
    return &this->health;
  }

  /**
   * @return <code>true</code> if this agent is symptomatic, <code>false</code> otherwise
   * @see Health::is_symptomatic()
   */
  int is_symptomatic()
  {
    return this->health.is_symptomatic();
  }

  int is_symptomatic(int disease_id)
  {
    return this->health.is_symptomatic(disease_id);
  }

  int get_days_symptomatic()
  {
    return this->health.get_days_symptomatic();
  }

  bool is_immune(int disease_id)
  {
    return this->health.is_immune(disease_id);
  }

  /**
   * @param dis the disease to check
   * @return <code>true</code> if this agent is susceptible to disease, <code>false</code> otherwise
   * @see Health::is_susceptible(int dis)
   */
  bool is_susceptible(int dis)
  {
    return this->health.is_susceptible(dis);
  }

  /**
   * @param dis the disease to check
   * @return <code>true</code> if this agent is infectious with disease, <code>false</code> otherwise
   * @see Health::is_infectious(int disease)
   */
  bool is_infectious(int dis)
  {
    return this->health.is_infectious(dis);
  }

  bool is_recovered(int dis)
  {
    return this->health.is_recovered(dis);
  }

  /**
   * @param dis the disease to check
   * @return <code>true</code> if this agent is infected with disease, <code>false</code> otherwise
   * @see Health::is_infected(int disease)
   */
  bool is_infected(int dis)
  {
    return this->health.is_infected(dis);
  }

  /**
   * @param disease the disease to check
   * @return the specific Disease's susceptibility for this Person
   * @see Health::get_susceptibility(int disease)
   */
  double get_susceptibility(int disease) const {
    return this->health.get_susceptibility(disease);
  }

  /**
   * @param disease the disease to check
   * @param day the simulation day
   * @return the specific Disease's infectivity for this Person
   * @see Health::get_infectivity(int disease, int day)
   */
  double get_infectivity(int disease_id, int day) const {
    return this->health.get_infectivity(disease_id, day);
  }

  /**
   * @param disease the disease to check
   * @param day the simulation day
   * @return the Symptoms for this Person
   * @see Health::get_symptoms(int day)
   */
  double get_symptoms(int disease_id, int day) const {
    return this->health.get_symptoms(disease_id, day);
  }

  /*
   * Advances the course of the infection by moving the exposure date
   * backwards
   */
  void advance_seed_infection(int disease_id, int days_to_advance)
  {
    this->health.advance_seed_infection(disease_id, days_to_advance);
  }

  /**
   * @param disease the disease to check
   * @return the simulation day that this agent became exposed to disease
   */
  int get_exposure_date(int disease) const {
    return this->health.get_exposure_date(disease);
  }

  int get_infectious_start_date(int disease) const {
    return this->health.get_infectious_start_date(disease);
  }

  int get_infectious_end_date(int disease) const {
    return this->health.get_infectious_end_date(disease);
  }

  int get_symptoms_start_date(int disease) const {
    return this->health.get_symptoms_start_date(disease);
  }

  int get_symptoms_end_date(int disease) const {
    return this->health.get_symptoms_end_date(disease);
  }

  int get_immunity_end_date(int disease) const {
    return this->health.get_immunity_end_date(disease);
  }

  /**
   * @param disease the disease to check
   * @return the Person who infected this agent with disease
   */
  Person* get_infector(int disease) const {
    return this->health.get_infector(disease);
  }

  /**
   * @param disease the disease to check
   * @return the id of the location where this agent became infected with disease
   */
  int get_infected_mixing_group_id(int disease) const {
    return this->health.get_infected_mixing_group_id(disease);
  }

  /**
   * @param disease the disease to check
   * @return the pointer to the Place where this agent became infected with disease
   */
  Mixing_Group* get_infected_mixing_group(int disease) const {
    return this->health.get_infected_mixing_group(disease);
  }

  /**
   * @param disease the disease to check
   * @return the label of the location where this agent became infected with disease
   */
  char* get_infected_mixing_group_label(int disease) const {
    return this->health.get_infected_mixing_group_label(disease);
  }

  /**
   * @param disease the disease to check
   * @return the type of the location where this agent became infected with disease
   */
  char get_infected_mixing_group_type(int disease) const {
    return this->health.get_infected_mixing_group_type(disease);
  }

  /**
   * @param disease the disease in question
   * @return the infectees
   * @see Health::get_infectees(int disease)
   */
  int get_infectees(int disease) const {
    return this->health.get_infectees(disease);
  }

  /**
   * @return a pointer to this Person's Activities
   */
  Activities* get_activities()
  {
    return &this->activities;
  }

  /**
   * @return the a pointer to this agent's Neighborhood
   */
  Place* get_neighborhood()
  {
    return this->activities.get_neighborhood();
  }

  void reset_neighborhood()
  {
    this->activities.reset_neighborhood();
  }

  /**
   * @return a pointer to this Person's Household
   * @see Activities::get_household()
   */
  Place* get_household()
  {
    return this->activities.get_household();
  }

  int get_exposed_household_index()
  {
    return this->exposed_household_index;
  }

  void set_exposed_household(int index_)
  {
    this->exposed_household_index = index_;
  }

  Place* get_permanent_household()
  {
    return this->activities.get_permanent_household();
  }

  unsigned char get_deme_id()
  {
    return this->activities.get_deme_id();
  }

  bool is_householder()
  {
    return this->demographics.is_householder();
  }

  void make_householder()
  {
    this->demographics.make_householder();
  }

  /**
   * @return a pointer to this Person's School
   * @see Activities::get_school()
   */
  Place* get_school()
  {
    return this->activities.get_school();
  }

  /**
   * @return a pointer to this Person's Classroom
   * @see Activities::get_classroom()
   */
  Place* get_classroom()
  {
    return this->activities.get_classroom();
  }

  /**
   * @return a pointer to this Person's Workplace
   * @see Activities::get_workplace()
   */
  Place* get_workplace()
  {
    return this->activities.get_workplace();
  }

  /**
   * @return a pointer to this Person's Office
   * @see Activities::get_office()
   */
  Place* get_office()
  {
    return this->activities.get_office();
  }

  /**
   * @return a pointer to this Person's Hospital
   * @see Activities::get_hospital()
   */
  Place* get_hospital()
  {
    return this->activities.get_hospital();
  }

  /**
   * @return a pointer to this Person's Ad Hoc location
   * @see Activities::get_ad_hoc()
   */
  Place* get_ad_hoc()
  {
    return this->activities.get_ad_hoc();
  }

  /**
   *  @return the number of other agents in an agent's neighborhood, school, and workplace.
   *  @see Activities::get_degree()
   */
  int get_degree()
  {
    return this->activities.get_degree();
  }

  int get_household_size()
  {
    return this->activities.get_group_size(Activity_index::HOUSEHOLD_ACTIVITY);
  }

  int get_neighborhood_size()
  {
    return this->activities.get_group_size(Activity_index::NEIGHBORHOOD_ACTIVITY);
  }

  int get_school_size()
  {
    return this->activities.get_group_size(Activity_index::SCHOOL_ACTIVITY);
  }

  int get_classroom_size()
  {
    return this->activities.get_group_size(Activity_index::CLASSROOM_ACTIVITY);
  }

  int get_workplace_size()
  {
    return this->activities.get_group_size(Activity_index::WORKPLACE_ACTIVITY);
  }

  int get_office_size()
  {
    return this->activities.get_group_size(Activity_index::OFFICE_ACTIVITY);
  }

  bool is_hospitalized()
  {
    return this->activities.is_hospitalized;
  }

  /**
   * Have this Person begin traveling
   * @param visited the Person this agent will visit
   * @see Activities::start_traveling(Person* visited)
   */
  void start_traveling(Person* visited)
  {
    this->activities.start_traveling(visited);
  }

  /**
   * Have this Person stop traveling
   * @see Activities::stop_traveling()
   */
  void stop_traveling()
  {
    this->activities.stop_traveling();
  }

  /**
   * @return <code>true</code> if the Person is traveling, <code>false</code> if not
   * @see Activities::get_travel_status()
   */
  bool get_travel_status()
  {
    return this->activities.get_travel_status();
  }

  int get_num_past_infections(int disease)
  {
    return this->health.get_num_past_infections(disease);
  }

  Past_Infection* get_past_infection(int disease, int i)
  {
    return this->health.get_past_infection(disease, i);
  }

  void clear_past_infections(int disease)
  {
    this->health.clear_past_infections(disease);
  }

  //void add_past_infection(int d, Past_Infection *pi){ health.add_past_infection(d, pi); }  
  void add_past_infection(int strain_id, int recovery_date, int age_at_exposure, Disease* dis)
  {
    this->health.add_past_infection(strain_id, recovery_date, age_at_exposure, dis);
  }

  void take_vaccine(Vaccine* vacc, int day, Vaccine_Manager* vm)
  {
    this->health.take_vaccine(vacc, day, vm);
  }

  // set up and access health behaviors
  void setup_behavior()
  {
    this->behavior.setup(this);
  }

  bool is_health_decision_maker()
  {
    return this->behavior.is_health_decision_maker();
  }

  Person* get_health_decision_maker()
  {
    return this->behavior.get_health_decision_maker();
  }

  void set_health_decision_maker(Person* p)
  {
    this->behavior.set_health_decision_maker(p);
  }

  void become_health_decision_maker(Person* self)
  {
    this->behavior.become_health_decision_maker(self);
  }

  bool adult_is_staying_home()
  {
    return this->behavior.adult_is_staying_home();
  }

  bool child_is_staying_home()
  {
    return this->behavior.child_is_staying_home();
  }

  bool acceptance_of_vaccine()
  {
    return this->behavior.acceptance_of_vaccine();
  }

  bool acceptance_of_another_vaccine_dose()
  {
    return this->behavior.acceptance_of_another_vaccine_dose();
  }

  bool child_acceptance_of_vaccine()
  {
    return this->behavior.child_acceptance_of_vaccine();
  }

  bool child_acceptance_of_another_vaccine_dose()
  {
    return this->behavior.child_acceptance_of_another_vaccine_dose();
  }

  bool is_sick_leave_available()
  {
    return this->activities.is_sick_leave_available();
  }

  double get_transmission_modifier_due_to_hygiene(int disease_id)
  {
    return this->health.get_transmission_modifier_due_to_hygiene(disease_id);
  }

  double get_susceptibility_modifier_due_to_hygiene(int disease_id)
  {
    return this->health.get_susceptibility_modifier_due_to_hygiene(disease_id);
  }

  bool is_case_fatality(int disease_id)
  {
    return this->health.is_case_fatality(disease_id);
  }

  void set_pop_index(int idx)
  {
    this->index = idx;
  }

  int get_pop_index()
  {
    return this->index;
  }

  void birthday(int day)
  {
    this->demographics.birthday(this, day);
  }

  bool become_a_teacher(Place* school)
  {
    return this->activities.become_a_teacher(school);
  }

  bool is_teacher()
  {
    return this->activities.is_teacher();
  }

  bool is_student()
  {
    return this->activities.is_student();
  }

  void move_to_new_house(Place* house)
  {
    activities.move_to_new_house(house);
  }

  void change_school(Place* place)
  {
    this->activities.change_school(place);
  }

  void change_workplace(Place* place)
  {
    this->activities.change_workplace(place);
  }

  void change_workplace(Place* place, int include_office)
  {
    this->activities.change_workplace(place, include_office);
  }

  int get_visiting_health_status(Place* place, int day, int disease_id)
  {
    return this->activities.get_visiting_health_status(place, day, disease_id);
  }

  /**
   * @return <code>true</code> if agent is asthmatic, <code>false</code> otherwise
   * @see Health::is_asthmatic()
   */
  bool is_asthmatic()
  {
    return this->health.is_asthmatic();
  }

  /**
   * @return <code>true</code> if agent has COPD (Chronic Obstructive Pulmonary
   * disease), <code>false</code> otherwise
   * @see Health::has_COPD()
   */
  bool has_COPD()
  {
    return this->health.has_COPD();
  }

  /**
   * @return <code>true</code> if agent has chronic renal disease, <code>false</code> otherwise
   * @see Health::has_chronic_renal_disease()
   */
  bool has_chronic_renal_disease()
  {
    return this->health.has_chronic_renal_disease();
  }

  /**
   * @return <code>true</code> if agent is diabetic, <code>false</code> otherwise
   * @see Health::is_diabetic()
   */
  bool is_diabetic()
  {
    return this->health.is_diabetic();
  }

  /**
   * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
   *  @see Health::has_heart_disease()
   */
  bool has_heart_disease()
  {
    return this->health.has_heart_disease();
  }

  /**
   * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
   *  @see Health::has_hypertension()
   */
  bool has_hypertension()
  {
    return this->health.has_hypertension();
  }

  /**
   * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
   *  @see Health::has_hypercholestrolemia()
   */
  bool has_hypercholestrolemia()
  {
    return this->health.has_hypercholestrolemia();
  }

  /**
   * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
   *  @see Health::has_chronic_condition()
   */
  bool has_chronic_condition()
  {
    return this->health.has_chronic_condition();
  }

  /**
   * @return <code>true</code> if agent is alive, <code>false</code> otherwise
   * @see Health::is_alive()
   */
  bool is_alive()
  {
    return this->health.is_alive();
  }

  /**
   * @return <code>true</code> if agent is dead, <code>false</code> otherwise
   * @see Health::is_alive()
   */
  bool is_dead()
  {
    return this->health.is_alive() == false;
  }

  bool is_prisoner()
  {
    return this->activities.is_prisoner();
  }

  bool is_college_dorm_resident()
  {
    return this->activities.is_college_dorm_resident();
  }

  bool is_military_base_resident()
  {
    return this->activities.is_military_base_resident();
  }

  bool is_nursing_home_resident()
  {
    return this->activities.is_nursing_home_resident();
  }

  bool lives_in_group_quarters()
  {
    return (is_college_dorm_resident() || is_military_base_resident()
      || is_prisoner() || is_nursing_home_resident());
  }

  char get_profile()
  {
    return this->activities.get_profile();
  }

  int get_number_of_children()
  {
    return this->demographics.get_number_of_children();
  }

  int get_grade()
  {
    return this->activities.get_grade();
  }

  void set_grade(int n)
  {
    this->activities.set_grade(n);
  }

  // convenience methods for Networks

  void create_network_link_to(Person* person, Network* network)
  {
    this->activities.create_network_link_to(person, network);
  }

  void destroy_network_link_to(Person* person, Network* network)
  {
    this->activities.destroy_network_link_to(person, network);
  }

  void create_network_link_from(Person* person, Network* network)
  {
    this->activities.create_network_link_from(person, network);
  }

  void destroy_network_link_from(Person* person, Network* network)
  {
    this->activities.destroy_network_link_from(person, network);
  }

  void add_network_link_to(Person* person, Network* network)
  {
    this->activities.add_network_link_to(person, network);
  }

  void add_network_link_from(Person* person, Network* network)
  {
    this->activities.add_network_link_from(person, network);
  }

  void delete_network_link_to(Person* person, Network* network)
  {
    this->activities.delete_network_link_to(person, network);
  }

  void delete_network_link_from(Person* person, Network* network)
  {
    this->activities.delete_network_link_from(person, network);
  }

  void join_network(Network* network)
  {
    this->activities.join_network(network);
  }

  void print_network(FILE* fp, Network* network)
  {
    this->activities.print_network(fp, network);
  }

  bool is_connected_to(Person* person, Network* network)
  {
    return this->activities.is_connected_to(person, network);
  }

  bool is_connected_from(Person* person, Network* network)
  {
    return this->activities.is_connected_from(person, network);
  }

  int get_out_degree(Network* network)
  {
    return this->activities.get_out_degree(network);
  }

  int get_in_degree(Network* network)
  {
    return this->activities.get_in_degree(network);
  }

  void clear_network(Network* network)
  {
    return this->activities.clear_network(network);
  }

  Person* get_end_of_link(int n, Network* network)
  {
    return this->activities.get_end_of_link(n, network);
  }

  int get_health_state(int disease_id)
  {
    return this->health.get_health_state(disease_id);
  }

  void set_health_state(int disease_id, int s, int day)
  {
    return this->health.set_health_state(disease_id, s, day);
  }

  int get_last_health_transition_day(int disease_id)
  {
    return this->health.get_last_transition_day(disease_id);
  }

  int get_next_health_state(int disease_id)
  {
    return this->health.get_next_health_state(disease_id);
  }

  void set_next_health_state(int disease_id, int s, int day)
  {
    return this->health.set_next_health_state(disease_id, s, day);
  }

  int get_next_health_transition_day(int disease_id)
  {
    return this->health.get_next_transition_day(disease_id);
  }

  void update_health_conditions(int day)
  {
    this->health.update_health_conditions(day);
  }
}
}
