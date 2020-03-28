using System;
using System.Collections.Generic;

namespace Fred
{
  public class Health
  {
    private readonly bool[] m_AtRisk;
    private readonly bool[] m_Immunity;
    private readonly bool[] m_Recovered;
    private readonly bool[] m_Infectiuos;
    private readonly bool[] m_Symptomatic;
    private readonly bool[] m_Susceptible;
    private readonly bool[] m_CaseFatality;
    private readonly bool[] m_Interventions;

    private readonly int[] m_InfecteeCount;

    private readonly Person[] m_Infectors;
    private readonly Infection[] m_Infections;
    private readonly DateTime?[] m_ExposureDates;
    private readonly DateTime?[] m_ImmunityEndDates;
    private readonly HealthCondition[] m_HealthConditions;
    private readonly MixingGroup[] m_InfectedInMixingGroup;

    private readonly double[] m_SusceptibilityMultipliers;

    private bool m_WashesHands;
    private bool m_HasFaceMask;
    private bool m_WearsFaceMaskToday;

    private int m_DaysWearingFaceMask;

    private List<PastInfection> m_PastInfections;

    public Health(Person person)
    {
      this.Person = person;
      this.IsAlive = true;
      this.m_AtRisk = Global.DefaultDiseaseArray();
      this.m_Immunity = Global.DefaultDiseaseArray();
      this.m_Recovered = Global.DefaultDiseaseArray();
      this.m_Infectiuos = Global.DefaultDiseaseArray();
      this.m_Symptomatic = Global.DefaultDiseaseArray();
      this.m_Susceptible = Global.DefaultDiseaseArray();
      this.m_CaseFatality = Global.DefaultDiseaseArray();
      this.m_Interventions = new bool[2];
      if (Global.HandWashingCompliance > 0.0)
      {
        this.m_WashesHands = FredRandom.NextDouble() < Global.HandWashingCompliance;
      }
      if (Global.FaceMaskCompliance > 0.0)
      {
        this.m_HasFaceMask = FredRandom.NextDouble() < Global.FaceMaskCompliance;
      }

      var diseaseCount = Global.Diseases.Count;
      this.m_InfecteeCount = new int[diseaseCount];
      this.m_Infections = new Infection[diseaseCount];
      this.m_SusceptibilityMultipliers = new double[diseaseCount];
      this.m_Infectors = new Person[diseaseCount];
      this.m_ExposureDates = new DateTime?[diseaseCount];
      this.m_ImmunityEndDates = new DateTime?[diseaseCount];
      this.m_PastInfections = new List<PastInfection>();
      this.m_HealthConditions = new HealthCondition[diseaseCount];
      for (var i = 0; i < diseaseCount; i++)
      {
        this.m_ExposureDates[i] = null;
        this.m_ImmunityEndDates[i] = null;
        this.m_SusceptibilityMultipliers[i] = 1.0;
        this.m_HealthConditions[i] = new HealthCondition();

        var disease = Global.Diseases[i];
        if (disease.assume_susceptible())
        {
          become_susceptible(disease_id);
        }

        if (disease.get_at_risk() != null && !disease.get_at_risk().is_empty())
        {
          double at_risk_prob = disease.get_at_risk().find_value(myself.get_real_age());
          if (Random::draw_random() < at_risk_prob)
          { // Now a probability <=1.0
            declare_at_risk(disease);
          }
        }
      }
      this.days_symptomatic = 0;
      this.vaccine_health = null;
      this.av_health = null;
      this.checked_for_av = null;
      this.previous_infection_serotype = -1;

      if (nantivirals == -1)
      {
        Params::get_param_from_string("number_antivirals", &nantivirals);
      }

      if (Global::Enable_Chronic_Condition && is_initialized)
      {
        double prob = 0.0;
        prob = asthma_prob.find_value(myself.get_real_age());
        set_is_asthmatic((Random::draw_random() < prob));

        prob = COPD_prob.find_value(myself.get_real_age());
        set_has_COPD((Random::draw_random() < prob));

        prob = chronic_renal_disease_prob.find_value(myself.get_real_age());
        set_has_chronic_renal_disease((Random::draw_random() < prob));

        prob = diabetes_prob.find_value(myself.get_real_age());
        set_is_diabetic((Random::draw_random() < prob));

        prob = heart_disease_prob.find_value(myself.get_real_age());
        set_has_heart_disease((Random::draw_random() < prob));

        prob = hypertension_prob.find_value(myself.get_real_age());
        set_has_hypertension((Random::draw_random() < prob));

        prob = hypercholestrolemia_prob.find_value(myself.get_real_age());
        set_has_hypercholestrolemia((Random::draw_random() < prob));
      }
    }

    public Person Person { get; }

    public bool IsAlive { get; }

    void become_susceptible(int disease_id)
    {
      if (this.susceptible.test(disease_id))
      {
        FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
              "HEALTH CHART: %s person %d is already SUSCEPTIBLE for disease %d\n",
              Date::get_date_string().c_str(),
              myself.get_id(), disease_id);
        return;
      }
      assert(this.infection[disease_id] == null);
      this.susceptibility_multp[disease_id] = 1.0;
      this.susceptible.set(disease_id);
      assert(is_susceptible(disease_id));
      this.recovered.reset(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is SUSCEPTIBLE for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }

    void become_susceptible_by_vaccine_waning(int disease_id)
    {
      if (this.susceptible.test(disease_id))
      {
        return;
      }
      if (this.infection[disease_id] == null)
      {
        // not already infected
        this.susceptibility_multp[disease_id] = 1.0;
        this.susceptible.set(disease_id);
        FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
              "HEALTH CHART: %s person %d is SUSCEPTIBLE for disease %d\n",
              Date::get_date_string().c_str(),
              myself.get_id(), disease_id);
      }
      else
      {
        FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
              "HEALTH CHART: %s person %d had no vaccine waning because was already infected with disease %d\n",
              Date::get_date_string().c_str(),
              myself.get_id(), disease_id);
      }
    }

    void become_exposed(int disease_id, Person* infector, Mixing_Group* mixing_group, int day)
    {

      FRED_VERBOSE(0, "become_exposed: person %d is exposed to disease %d day %d\n",
                   myself.get_id(), disease_id, day);

      if (this.infection[disease_id] != null)
      {
        Utils::fred_abort("DOUBLE EXPOSURE: person %d dis_id %d day %d\n", myself.get_id(), disease_id, day);
      }

      if (Global::Verbose > 0)
      {
        if (mixing_group == null)
        {
          FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
                "HEALTH CHART: %s person %d is an IMPORTED EXPOSURE to disease %d\n",
                Date::get_date_string().c_str(),
                myself.get_id(), disease_id);
        }
        else
        {
          FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
                "HEALTH CHART: %s person %d is EXPOSED to disease %d\n",
                Date::get_date_string().c_str(),
                myself.get_id(), disease_id);
        }
      }

      this.infectious.reset(disease_id);
      this.symptomatic.reset(disease_id);
      Disease* disease = Global::Diseases.get_disease(disease_id);
      this.infection[disease_id] = Infection::get_new_infection(disease, infector, myself, mixing_group, day);
      FRED_VERBOSE(1, "setup infection: person %d dis_id %d day %d\n", myself.get_id(), disease_id, day);
      this.infection[disease_id].setup();
      this.infection[disease_id].report_infection(day);
      become_unsusceptible(disease);
      this.immunity_end_date[disease_id] = -1;
      if (myself.get_household() != null)
      {
        myself.get_household().set_exposed(disease_id);
        myself.set_exposed_household(myself.get_household().get_index());
      }
      if (infector != null)
      {
        this.infector_id[disease_id] = infector.get_id();
      }
      this.exposure_date[disease_id] = day;
      this.infected_in_mixing_group[disease_id] = mixing_group;

      if (Global::Enable_Transmission_Network)
      {
        FRED_VERBOSE(1, "Joining transmission network: %d\n", myself.get_id());
        myself.join_network(Global::Transmission_Network);
      }

      if (Global::Enable_Vector_Transmission && Global::Diseases.get_number_of_diseases() > 1)
      {
        // special check for multi-serotype dengue:
        if (this.previous_infection_serotype == -1)
        {
          // remember this infection's serotype
          this.previous_infection_serotype = disease_id;
          // after the first infection, become immune to other two serotypes.
          for (int sero = 0; sero < Global::Diseases.get_number_of_diseases(); ++sero)
          {
            // if (sero == previous_infection_serotype) continue;
            if (sero == disease_id)
            {
              continue;
            }
            FRED_STATUS(1, "DENGUE: person %d now immune to serotype %d\n",
            myself.get_id(), sero);
            become_unsusceptible(Global::Diseases.get_disease(sero));
          }
        }
        else
        {
          // after the second infection, become immune to other two serotypes.
          for (int sero = 0; sero < Global::Diseases.get_number_of_diseases(); ++sero)
          {
            if (sero == this.previous_infection_serotype)
            {
              continue;
            }
            if (sero == disease_id)
            {
              continue;
            }
            FRED_STATUS(1, "DENGUE: person %d now immune to serotype %d\n",
            myself.get_id(), sero);
            become_unsusceptible(Global::Diseases.get_disease(sero));
          }
        }
      }
    }

    void become_unsusceptible(int disease_id)
    {
      this.susceptible.reset(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is UNSUSCEPTIBLE for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }

    void become_unsusceptible(Disease* disease)
    {
      int disease_id = disease.get_id();
      become_unsusceptible(disease_id);
    }

    void become_infectious(Disease* disease)
    {
      int disease_id = disease.get_id();
      assert(this.infection[disease_id] != null);
      this.infectious.set(disease_id);
      int household_index = myself.get_exposed_household_index();
      Household* h = Global::Places.get_household_ptr(household_index);
      assert(h != null);
      h.set_human_infectious(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is INFECTIOUS for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }

    void become_noninfectious(Disease* disease)
    {
      int disease_id = disease.get_id();
      assert(this.infection[disease_id] != null);
      this.infectious.reset(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is NONINFECTIOUS for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }

    void become_symptomatic(Disease* disease)
    {
      int disease_id = disease.get_id();
      if (this.infection[disease_id] == null)
      {
        FRED_STATUS(1, "Help: becoming symptomatic with no infection: person %d, disease_id %d\n", myself.get_id(), disease_id);
      }
      assert(this.infection[disease_id] != null);
      if (this.symptomatic.test(disease_id))
      {
        FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
              "HEALTH CHART: %s person %d is ALREADY SYMPTOMATIC for disease %d\n",
              Date::get_date_string().c_str(),
              myself.get_id(), disease_id);
        return;
      }
      this.symptomatic.set(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is SYMPTOMATIC for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }

    void resolve_symptoms(Disease* disease)
    {
      int disease_id = disease.get_id();
      // assert(this.infection[disease_id] != null);
      if (this.symptomatic.test(disease_id))
      {
        this.symptomatic.reset(disease_id);
      }
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d RESOLVES SYMPTOMS for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }


    void recover(Disease* disease, int day)
    {
      int disease_id = disease.get_id();
      // assert(this.infection[disease_id] != null);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
             "HEALTH CHART: %s person %d is RECOVERED from disease %d\n",
             Date::get_date_string().c_str(),
             myself.get_id(), disease_id);
      this.recovered.set(disease_id);
      int household_index = myself.get_exposed_household_index();
      Household* h = Global::Places.get_household_ptr(household_index);
      h.set_recovered(disease_id);
      h.reset_human_infectious();
      myself.reset_neighborhood();

      this.immunity_end_date[disease_id] = this.infection[disease_id].get_immunity_end_date();
      if (this.immunity_end_date[disease_id] > -1)
      {
        this.immunity_end_date[disease_id] += day;
      }
      become_removed(disease_id, day);
    }

    void become_removed(int disease_id, int day)
    {
      terminate_infection(disease_id, day);
      this.susceptible.reset(disease_id);
      this.infectious.reset(disease_id);
      this.symptomatic.reset(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
             "HEALTH CHART: %s person %d is REMOVED for disease %d\n",
             Date::get_date_string().c_str(),
             myself.get_id(), disease_id);
    }

    void become_immune(Disease* disease)
    {
      int disease_id = disease.get_id();
      disease.become_immune(myself, this.susceptible.test(disease_id),
          this.infectious.test(disease_id), this.symptomatic.test(disease_id));
      this.immunity.set(disease_id);
      this.susceptible.reset(disease_id);
      this.infectious.reset(disease_id);
      this.symptomatic.reset(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
            "HEALTH CHART: %s person %d is IMMUNE for disease %d\n",
            Date::get_date_string().c_str(),
            myself.get_id(), disease_id);
    }


    void become_case_fatality(int disease_id, int day)
    {
      FRED_VERBOSE(0, "DISEASE %d is FATAL: day %d person %d\n", disease_id, day, myself.get_id());
      this.case_fatality.set(disease_id);
      FRED_CONDITIONAL_VERBOSE(0, Global::Enable_Health_Charts,
             "HEALTH CHART: %s person %d is CASE_FATALITY for disease %d\n",
             Date::get_date_string().c_str(),
             myself.get_id(), disease_id);
      become_removed(disease_id, day);

      // update household counts
      Mixing_Group* hh = myself.get_household();
      if (hh == null)
      {
        if (Global::Enable_Hospitals && myself.is_hospitalized() && myself.get_permanent_household() != null)
        {
          hh = myself.get_permanent_household();
        }
      }
      if (hh != null)
      {
        hh.increment_case_fatalities(day, disease_id);
      }


      // queue removal from population
      Global::Pop.prepare_to_die(day, myself);
    }

    void update_infection(int day, int disease_id)
    {

      if (this.has_face_mask_behavior)
      {
        update_face_mask_decision(day);
      }

      if (this.infection[disease_id] == null)
      {
        return;
      }

      FRED_VERBOSE(1, "update_infection %d on day %d person %d\n", disease_id, day, myself.get_id());
      this.infection[disease_id].update(day);

      // update days_symptomatic if needed
      if (this.is_symptomatic(disease_id))
      {
        int days_symp_so_far = (day - this.get_symptoms_start_date(disease_id));
        if (days_symp_so_far > this.days_symptomatic)
        {
          this.days_symptomatic = days_symp_so_far;
        }
      }

      // case_fatality?
      if (this.infection[disease_id].is_fatal(day))
      {
        become_case_fatality(disease_id, day);
      }

      FRED_VERBOSE(1, "update_infection %d FINISHED on day %d person %d\n",
             disease_id, day, myself.get_id());

    } // end update_infection //


    void update_face_mask_decision(int day)
    {
      // printf("update_face_mask_decision entered on day %d for person %d\n", day, myself.get_id());

      // should we start use face mask?
      if (this.is_symptomatic(day) && this.days_wearing_face_mask == 0)
      {
        FRED_VERBOSE(1, "FACEMASK: person %d starts wearing face mask on day %d\n", myself.get_id(), day);
        this.start_wearing_face_mask();
      }

      // should we stop using face mask?
      if (this.is_wearing_face_mask())
      {
        if (this.is_symptomatic(day) && this.days_wearing_face_mask < Days_to_wear_face_masks)
        {
          this.days_wearing_face_mask++;
        }
        else
        {
          FRED_VERBOSE(1, "FACEMASK: person %d stops wearing face mask on day %d\n", myself.get_id(), day);
          this.stop_wearing_face_mask();
        }
      }
    }

    void update_interventions(int day)
    {
      // if deceased, health status should have been cleared during population
      // update (by calling Person.die(), then Health.die(), which will reset (bool) alive
      if (!(this.alive))
      {
        return;
      }
      if (this.intervention_flags.any())
      {
        // update vaccine status
        if (this.intervention_flags[Intervention_flag::TAKES_VACCINE])
        {
          int size = (int)(this.vaccine_health.size());
          for (int i = 0; i < size; ++i)
          {
            (*this.vaccine_health)[i].update(day, myself.get_real_age());
          }
        }
        // update antiviral status
        if (this.intervention_flags[Intervention_flag::TAKES_AV])
        {
          for (av_health_itr i = this.av_health.begin(); i != this.av_health.end(); ++i)
          {
            (*i).update(day);
          }
        }
      }
    } // end update_interventions

    void declare_at_risk(Disease* disease)
    {
      int disease_id = disease.get_id();
      this.at_risk.set(disease_id);
    }

    void advance_seed_infection(int disease_id, int days_to_advance)
    {
      assert(this.infection[disease_id] != null);
      this.infection[disease_id].advance_seed_infection(days_to_advance);
    }

    int get_exposure_date(int disease_id) const {
  return this.exposure_date[disease_id];
}

  int get_infectious_start_date(int disease_id) const {
  if(this.infection[disease_id] == null) {
    return -1;
  } else {
    return this.infection[disease_id].get_infectious_start_date();
  }
  }

  int get_infectious_end_date(int disease_id) const {
  if(this.infection[disease_id] == null) {
    return -1;
  } else {
    return this.infection[disease_id].get_infectious_end_date();
  }
  }

  int get_symptoms_start_date(int disease_id) const {
  if(this.infection[disease_id] == null) {
    return -1;
  } else {
    return this.infection[disease_id].get_symptoms_start_date();
  }
  }

  int get_symptoms_end_date(int disease_id) const {
  if(this.infection[disease_id] == null) {
    return -1;
  } else {
    return this.infection[disease_id].get_symptoms_end_date();
  }
  }

  int get_immunity_end_date(int disease_id) const {
  return this.immunity_end_date[disease_id];
}

  bool is_recovered(int disease_id)
  {
    return this.recovered.test(disease_id);
  }


  int get_infector_id(int disease_id) const {
  return infector_id[disease_id];
}

Person* get_infector(int disease_id) const {
  if(this.infection[disease_id] == null) {
  return null;
} else {
  return this.infection[disease_id].get_infector();
}
}

Mixing_Group* get_infected_mixing_group(int disease_id) const {
  return this.infected_in_mixing_group[disease_id];
}

int get_infected_mixing_group_id(int disease_id) const {
  Mixing_Group* mixing_group = get_infected_mixing_group(disease_id);
  if(mixing_group == null) {
    return -1;
  } else {
    return mixing_group.get_id();
  }
}

char get_infected_mixing_group_type(int disease_id) const {
  Mixing_Group* mixing_group = get_infected_mixing_group(disease_id);
  if(mixing_group == null) {
    return 'X';
  } else {
    return mixing_group.get_type();
  }
}

char dummy_label[8];
char* get_infected_mixing_group_label(int disease_id) const {
  if(this.infection[disease_id] == null) {
  strcpy(dummy_label, "-");
  return dummy_label;
}
Mixing_Group* mixing_group = get_infected_mixing_group(disease_id);
  if(mixing_group == null) {
  strcpy(dummy_label, "X");
  return dummy_label;
} else {
  return mixing_group.get_label();
}
}

int get_infectees(int disease_id) const {
  return this.infectee_count[disease_id];
}

double get_susceptibility(int disease_id) const {
  double suscep_multp = this.susceptibility_multp[disease_id];

  if(this.infection[disease_id] == null) {
  return suscep_multp;
} else {
  return this.infection[disease_id].get_susceptibility() * suscep_multp;
}
}

double get_infectivity(int disease_id, int day) const {
  if(this.infection[disease_id] == null) {
  return 0.0;
} else {
  return this.infection[disease_id].get_infectivity(day);
}
}

double get_symptoms(int disease_id, int day) const {

  if(this.infection[disease_id] == null) {
  return 0.0;
} else {
  return this.infection[disease_id].get_symptoms(day);
}
}

//Modify Operators
double get_transmission_modifier_due_to_hygiene(int disease_id)
{
  Disease* disease = Global::Diseases.get_disease(disease_id);
  if (this.is_wearing_face_mask() && this.is_washing_hands())
  {
    return (1.0 - disease.get_face_mask_plus_hand_washing_transmission_efficacy());
  }
  if (this.is_wearing_face_mask())
  {
    return (1.0 - disease.get_face_mask_transmission_efficacy());
  }
  if (this.is_washing_hands())
  {
    return (1.0 - disease.get_hand_washing_transmission_efficacy());
  }
  return 1.0;
}

double get_susceptibility_modifier_due_to_hygiene(int disease_id)
{
  Disease* disease = Global::Diseases.get_disease(disease_id);
  /*
    if (this.is_wearing_face_mask() && this.is_washing_hands()) {
    return (1.0 - disease.get_face_mask_plus_hand_washing_susceptibility_efficacy());
    }
    if (this.is_wearing_face_mask()) {
    return (1.0 - disease.get_face_mask_susceptibility_efficacy());
    }
  */
  if (this.is_washing_hands())
  {
    return (1.0 - disease.get_hand_washing_susceptibility_efficacy());
  }
  return 1.0;
}

double get_susceptibility_modifier_due_to_household_income(int hh_income)
{

  if (Global::Enable_hh_income_based_susc_mod)
  {
    if (hh_income >= Household::get_min_hh_income_90_pct())
    {
      return Hh_income_susc_mod_floor;
    }
    else
    {
      double rise = 1.0 - Hh_income_susc_mod_floor;
      double run = static_cast<double>(Household::get_min_hh_income() - Household::get_min_hh_income_90_pct());
      double m = rise / run;

      // Equation of line is y - y1 = m(x - x1)
      // y = m*x - m*x1 + y1
      double x = static_cast<double>(hh_income);
      return m * x - m * Household::get_min_hh_income() + 1.0;
    }
  }
  else
  {
    return 1.0;
  }
}

void modify_susceptibility(int disease_id, double multp)
{
  this.susceptibility_multp[disease_id] *= multp;
}

void modify_infectivity(int disease_id, double multp)
{
  if (this.infection[disease_id] != null)
  {
    this.infection[disease_id].modify_infectivity(multp);
  }
}

void modify_infectious_period(int disease_id, double multp, int cur_day)
{
  if (this.infection[disease_id] != null)
  {
    this.infection[disease_id].modify_infectious_period(multp, cur_day);
  }
}

void modify_asymptomatic_period(int disease_id, double multp, int cur_day)
{
  if (this.infection[disease_id] != null)
  {
    this.infection[disease_id].modify_asymptomatic_period(multp, cur_day);
  }
}

void modify_symptomatic_period(int disease_id, double multp, int cur_day)
{
  if (this.infection[disease_id] != null)
  {
    this.infection[disease_id].modify_symptomatic_period(multp, cur_day);
  }
}

void modify_develops_symptoms(int disease_id, bool symptoms, int cur_day)
{
  if (this.infection[disease_id] != null
     && ((this.infection[disease_id].is_infectious(cur_day)
          && !this.infection[disease_id].is_symptomatic(cur_day))
          || !this.infection[disease_id].is_infectious(cur_day)))
  {

    this.infection[disease_id].modify_develops_symptoms(symptoms, cur_day);
    this.symptomatic.set(disease_id);
  }
}

//Medication operators
void take_vaccine(Vaccine* vaccine, int day, Vaccine_Manager* vm)
{
  // Compliance will be somewhere else
  double real_age = myself.get_real_age();
  // Is this our first dose?
  Vaccine_Health* vaccine_health_for_dose = null;

  if (this.vaccine_health == null)
  {
    this.vaccine_health = new vaccine_health_type();
  }

  for (unsigned int ivh = 0; ivh < this.vaccine_health.size(); ++ivh)
  {
    if ((*this.vaccine_health)[ivh].get_vaccine() == vaccine)
    {
      vaccine_health_for_dose = (*this.vaccine_health)[ivh];
    }
  }

  if (vaccine_health_for_dose == null)
  { // This is our first dose of this vaccine
    this.vaccine_health.push_back(new Vaccine_Health(day, vaccine, real_age, myself, vm));
    this.intervention_flags[Intervention_flag::TAKES_VACCINE] = true;
  }
  else
  { // Already have a dose, need to take the next dose
    vaccine_health_for_dose.update_for_next_dose(day, real_age);
  }

  if (Global::VaccineTracefp != null)
  {
    fprintf(Global::VaccineTracefp, " id %7d vaccid %3d", myself.get_id(),
      (*this.vaccine_health)[this.vaccine_health.size() - 1].get_vaccine().get_ID());
    (*this.vaccine_health)[this.vaccine_health.size() - 1].printTrace();
    fprintf(Global::VaccineTracefp, "\n");
  }

  return;
}

void take(Antiviral* av, int day)
{
  if (this.checked_for_av == null)
  {
    this.checked_for_av = new checked_for_av_type();
    this.checked_for_av.assign(nantivirals, false);
  }
  if (this.av_health == null)
  {
    this.av_health = new av_health_type();
  }
  this.av_health.push_back(new AV_Health(day, av, this));
  this.intervention_flags[Intervention_flag::TAKES_AV] = true;
  return;
}

bool is_on_av_for_disease(int day, int d) const {
  for(unsigned int iav = 0; iav< this.av_health.size(); ++iav) {
    if((*this.av_health)[iav].get_disease() == d
       && (*this.av_health)[iav].is_on_av(day)) {
  return true;
}
}
  return false;
}

int get_av_start_day(int i) const {
  assert(this.av_health != null);
  return (*this.av_health)[i].get_av_start_day();
}

void infect(Person* infectee, int disease_id, Mixing_Group* mixing_group, int day)
{
  infectee.become_exposed(disease_id, myself, mixing_group, day);

#pragma omp atomic
  ++(this.infectee_count[disease_id]);

  int exp_day = this.get_exposure_date(disease_id);
  assert(0 <= exp_day);
  Disease* disease = Global::Diseases.get_disease(disease_id);
  disease.increment_cohort_infectee_count(exp_day);

  FRED_STATUS(1, "person %d infected person %d infectees = %d\n",
        myself.get_id(), infectee.get_id(), infectee_count[disease_id]);

  if (Global::Enable_Transmission_Network)
  {
    FRED_VERBOSE(1, "Creating link in transmission network: %d . %d\n", myself.get_id(), infectee.get_id());
    myself.create_network_link_to(infectee, Global::Transmission_Network);
  }
}

void update_mixing_group_counts(int day, int disease_id, Mixing_Group* mixing_group)
{
  // this is only called for people with an active infection
  assert(is_infected(disease_id));

  // mixing group must exist to update
  if (mixing_group == null)
  {
    return;
  }

  // update infection counters
  if (is_newly_infected(day, disease_id))
  {
    mixing_group.increment_new_infections(day, disease_id);
  }
  mixing_group.increment_current_infections(day, disease_id);

  // update symptomatic infection counters
  if (is_symptomatic(disease_id))
  {
    if (is_newly_symptomatic(day, disease_id))
    {
      mixing_group.increment_new_symptomatic_infections(day, disease_id);
    }
    mixing_group.increment_current_symptomatic_infections(day, disease_id);
  }
}

void terminate_infection(int disease_id, int day)
{
  if (this.health_condition[disease_id].state > -1)
  {
    Global::Diseases.get_disease(disease_id).terminate_person(myself, day);
  }
  if (this.infection[disease_id] != null)
  {
    // delete the infection object
    delete this.infection[disease_id];
    this.infection[disease_id] = null;
  }
}

void terminate(int day)
{
  for (int disease_id = 0; disease_id < Global::Diseases.get_number_of_diseases(); ++disease_id)
  {
    if (this.infection[disease_id] != null)
    {
      become_removed(disease_id, day);
    }
    if (this.health_condition[disease_id].state == 0)
    {
      Global::Diseases.get_disease(disease_id).terminate_person(myself, day); ;
    }
  }
  this.alive = false;
}

void update_health_conditions(int day)
{
  for (int disease_id = 0; disease_id < Global::Diseases.get_number_of_diseases(); ++disease_id)
  {
    if (this.health_condition[disease_id].state > -1)
    {
      Global::Diseases.get_disease(disease_id).get_epidemic().transition_person(this.myself, day, this.health_condition[disease_id].state);
    }
  }
}


  }
}
