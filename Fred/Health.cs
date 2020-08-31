using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class Health
  {
    private Person myself;

    // active infections (null if not infected)
    private Infection[] infection;

    // health condition array
    private health_condition_t[] health_condition;

    // persistent infection data (kept after infection clears)
    private double[] susceptibility_multp;
    private int[] infectee_count;
    private int[] immunity_end_date;
    private int[] exposure_date;
    private int[] infector_id;
    private Mixing_Group[] infected_in_mixing_group;
    private int days_symptomatic;       // over all diseases

    // living or not?
    bool alive;

    // bitset removes need to check each infection in above array to
    // find out if any are not null
    private BitArray immunity = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray at_risk = new BitArray(Global.MAX_NUM_DISEASES); // Agent is/isn't at risk for severe complications

    // Per-disease health status flags
    private BitArray susceptible = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray infectious = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray symptomatic = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray recovered_today = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray recovered = new BitArray(Global.MAX_NUM_DISEASES);
    private BitArray case_fatality = new BitArray(Global.MAX_NUM_DISEASES);

    // Define a bitset type to hold health flags
    // Enumeration corresponding to positions in health
    private BitArray intervention_flags = new BitArray(2);

    // Antivirals.  These are all dynamically allocated to save space
    // when not in use
    private List<bool> checked_for_av;
    private List<AV_Health> av_health;

    // Vaccines.  These are all dynamically allocated to save space
    // when not in use
    private List<Vaccine_Health> vaccine_health;

    // health behaviors
    private bool has_face_mask_behavior;
    private bool wears_face_mask_today;
    private int days_wearing_face_mask;
    private bool washes_hands;        // every day

    // current chronic conditions
    private Dictionary<Chronic_condition_index, bool> chronic_conditions_map;

    //Insurance Type
    private Insurance_assignment_index insurance_type;

    // Past_Infections used for reignition
    private List<Past_Infection>[] past_infections;

    // previous infection serotype (for dengue)
    private int previous_infection_serotype;

    // processes related to health
    private List<int> health_state;

    /////// STATIC MEMBERS

    private static bool is_initialized;
    private static Age_Map asthma_prob;
    private static Age_Map COPD_prob;
    private static Age_Map chronic_renal_disease_prob;
    private static Age_Map diabetes_prob;
    private static Age_Map heart_disease_prob;
    private static Age_Map hypertension_prob;
    private static Age_Map hypercholestrolemia_prob;

    private static Age_Map asthma_hospitalization_prob_mult;
    private static Age_Map COPD_hospitalization_prob_mult;
    private static Age_Map chronic_renal_disease_hospitalization_prob_mult;
    private static Age_Map diabetes_hospitalization_prob_mult;
    private static Age_Map heart_disease_hospitalization_prob_mult;
    private static Age_Map hypertension_hospitalization_prob_mult;
    private static Age_Map hypercholestrolemia_hospitalization_prob_mult;

    private static Age_Map asthma_case_fatality_prob_mult;
    private static Age_Map COPD_case_fatality_prob_mult;
    private static Age_Map chronic_renal_disease_case_fatality_prob_mult;
    private static Age_Map diabetes_case_fatality_prob_mult;
    private static Age_Map heart_disease_case_fatality_prob_mult;
    private static Age_Map hypertension_case_fatality_prob_mult;
    private static Age_Map hypercholestrolemia_case_fatality_prob_mult;

    private static Age_Map pregnancy_hospitalization_prob_mult;
    private static Age_Map pregnancy_case_fatality_prob_mult;

    // health protective behavior parameters
    private static int Days_to_wear_face_masks;
    private static double Face_mask_compliance;
    private static double Hand_washing_compliance;

    private static double Hh_income_susc_mod_floor;

    // health insurance probabilities
    private static List<double> health_insurance_distribution;
    private static int health_insurance_cdf_size;

    /*
     * Initialize any static variables needed by the Health class
     */
    static Health()
    {
      if (!Health.is_initialized)
      {

        FredParameters.GetParameter("days_to_wear_face_masks", ref Days_to_wear_face_masks);
        FredParameters.GetParameter("face_mask_compliance", ref Face_mask_compliance);
        FredParameters.GetParameter("hand_washing_compliance", ref Hand_washing_compliance);
        if (Global.Enable_hh_income_based_susc_mod)
        {
          FredParameters.GetParameter("hh_income_susc_mod_floor", ref Hh_income_susc_mod_floor);
        }

        if (Global.Enable_Chronic_Condition)
        {
          Health.asthma_prob = new Age_Map("Asthma Probability");
          Health.asthma_prob.read_from_input("asthma_prob");
          Health.asthma_hospitalization_prob_mult = new Age_Map("Asthma Hospitalization Probability Mult");
          Health.asthma_hospitalization_prob_mult.read_from_input("asthma_hospitalization_prob_mult");
          Health.asthma_case_fatality_prob_mult = new Age_Map("Asthma Case Fatality Probability Mult");
          Health.asthma_case_fatality_prob_mult.read_from_input("asthma_case_fatality_prob_mult");

          Health.COPD_prob = new Age_Map("COPD Probability");
          Health.COPD_prob.read_from_input("COPD_prob");
          Health.COPD_hospitalization_prob_mult = new Age_Map("COPD Hospitalization Probability Mult");
          Health.COPD_hospitalization_prob_mult.read_from_input("COPD_hospitalization_prob_mult");
          Health.COPD_case_fatality_prob_mult = new Age_Map("COPD Case Fatality Probability Mult");
          Health.COPD_case_fatality_prob_mult.read_from_input("COPD_case_fatality_prob_mult");

          Health.chronic_renal_disease_prob = new Age_Map("Chronic Renal Disease Probability");
          Health.chronic_renal_disease_prob.read_from_input("chronic_renal_disease_prob");
          Health.chronic_renal_disease_hospitalization_prob_mult = new Age_Map("Chronic Renal Disease Hospitalization Probability Mult");
          Health.chronic_renal_disease_hospitalization_prob_mult.read_from_input("chronic_renal_disease_hospitalization_prob_mult");
          Health.chronic_renal_disease_case_fatality_prob_mult = new Age_Map("Chronic Renal Disease Case Fatality Probability Mult");
          Health.chronic_renal_disease_case_fatality_prob_mult.read_from_input("chronic_renal_disease_case_fatality_prob_mult");

          Health.diabetes_prob = new Age_Map("Diabetes Probability");
          Health.diabetes_prob.read_from_input("diabetes_prob");
          Health.diabetes_hospitalization_prob_mult = new Age_Map("Diabetes Hospitalization Probability Mult");
          Health.diabetes_hospitalization_prob_mult.read_from_input("diabetes_hospitalization_prob_mult");
          Health.diabetes_case_fatality_prob_mult = new Age_Map("Diabetes Case Fatality Probability Mult");
          Health.diabetes_case_fatality_prob_mult.read_from_input("diabetes_case_fatality_prob_mult");

          Health.heart_disease_prob = new Age_Map("Heart Disease Probability");
          Health.heart_disease_prob.read_from_input("heart_disease_prob");
          Health.heart_disease_hospitalization_prob_mult = new Age_Map("Heart Disease Hospitalization Probability Mult");
          Health.heart_disease_hospitalization_prob_mult.read_from_input("heart_disease_hospitalization_prob_mult");
          Health.heart_disease_case_fatality_prob_mult = new Age_Map("Heart Disease Case Fatality Probability Mult");
          Health.heart_disease_case_fatality_prob_mult.read_from_input("heart_disease_case_fatality_prob_mult");

          Health.hypertension_prob = new Age_Map("Hypertension Probability");
          Health.hypertension_prob.read_from_input("hypertension_prob");
          Health.hypertension_hospitalization_prob_mult = new Age_Map("Hypertension Hospitalization Probability Mult");
          Health.hypertension_hospitalization_prob_mult.read_from_input("hypertension_hospitalization_prob_mult");
          Health.hypertension_case_fatality_prob_mult = new Age_Map("Hypertension Case Fatality Probability Mult");
          Health.hypertension_case_fatality_prob_mult.read_from_input("hypertension_case_fatality_prob_mult");

          Health.hypercholestrolemia_prob = new Age_Map("Hypercholestrolemia Probability");
          Health.hypercholestrolemia_prob.read_from_input("hypercholestrolemia_prob");
          Health.hypercholestrolemia_hospitalization_prob_mult = new Age_Map("Hypercholestrolemia Hospitalization Probability Mult");
          Health.hypercholestrolemia_hospitalization_prob_mult.read_from_input("hypercholestrolemia_hospitalization_prob_mult");
          Health.hypercholestrolemia_case_fatality_prob_mult = new Age_Map("Hypercholestrolemia Case Fatality Probability Mult");
          Health.hypercholestrolemia_case_fatality_prob_mult.read_from_input("hypercholestrolemia_case_fatality_prob_mult");

          Health.pregnancy_hospitalization_prob_mult = new Age_Map("Pregnancy Hospitalization Probability Mult");
          Health.pregnancy_hospitalization_prob_mult.read_from_input("pregnancy_hospitalization_prob_mult");
          Health.pregnancy_case_fatality_prob_mult = new Age_Map("Pregnancy Case Fatality Probability Mult");
          Health.pregnancy_case_fatality_prob_mult.read_from_input("pregnancy_case_fatality_prob_mult");
        }

        if (Global.Enable_Health_Insurance)
        {

          health_insurance_distribution = FredParameters.GetParameterList<double>("health_insurance_distribution");
          health_insurance_cdf_size = health_insurance_distribution.Count;

          // convert to cdf
          double stotal = 0;
          for (int i = 0; i < Health.health_insurance_cdf_size; ++i)
          {
            stotal += Health.health_insurance_distribution[i];
          }
          if (stotal != 100.0 && stotal != 1.0)
          {
            Utils.fred_abort("Bad distribution health_insurance_distribution params_str\nMust sum to 1.0 or 100.0\n");
          }
          double cumm = 0.0;
          for (int i = 0; i < Health.health_insurance_cdf_size; ++i)
          {
            Health.health_insurance_distribution[i] /= stotal;
            Health.health_insurance_distribution[i] += cumm;
            cumm = Health.health_insurance_distribution[i];
          }
        }

        Health.is_initialized = true;
      }
    }

    public Health()
    {
      this.myself = null;
      this.past_infections = null;
      this.alive = true;
      this.av_health = null;
      this.checked_for_av = null;
      this.vaccine_health = null;
      this.has_face_mask_behavior = false;
      this.wears_face_mask_today = false;
      this.days_wearing_face_mask = 0;
      this.washes_hands = false;
      this.days_symptomatic = 0;
      this.previous_infection_serotype = 0;
      this.insurance_type = Insurance_assignment_index.UNSET;
      this.infection = null;
      this.immunity_end_date = null;
      this.infectee_count = null;
      this.susceptibility_multp = null;
      this.exposure_date = null;
      this.infector_id = null;
      this.infected_in_mixing_group = null;
      this.health_condition = null;
      this.health_state = new List<int>();
      this.chronic_conditions_map = new Dictionary<Chronic_condition_index, bool>();
      var values = Enum.GetValues(typeof(Chronic_condition_index));
      foreach (Chronic_condition_index cc in values)
      {
        this.chronic_conditions_map.Add(cc, false);
      }
    }

    public void setup(Person self)
    {
      this.myself = self;
      Utils.FRED_VERBOSE(1, "Health.setup for person {0}", myself.get_id());
      this.alive = true;
      // Determine if the agent washes hands
      this.washes_hands = false;
      if (Health.Hand_washing_compliance > 0.0)
      {
        this.washes_hands = (FredRandom.NextDouble() < Health.Hand_washing_compliance);
      }

      // Determine if the agent will wear a face mask if sick
      this.has_face_mask_behavior = false;
      this.wears_face_mask_today = false;
      this.days_wearing_face_mask = 0;
      if (Health.Face_mask_compliance > 0.0)
      {
        if (FredRandom.NextDouble() < Health.Face_mask_compliance)
        {
          this.has_face_mask_behavior = true;
        }
        // printf("FACEMASK: has_face_mask_behavior = %d\n", this.has_face_mask_behavior?1:0);
      }

      int diseases = Global.Diseases.get_number_of_diseases();
      Utils.FRED_VERBOSE(1, "Health.setup diseases {0}", diseases);
      this.infection = new Infection[diseases];
      this.susceptibility_multp = new double[diseases];
      this.infectee_count = new int[diseases];
      this.exposure_date = new int[diseases];
      this.infector_id = new int[diseases];
      this.infected_in_mixing_group = new Mixing_Group[diseases];
      this.immunity_end_date = new int[diseases];
      this.past_infections = new List<Past_Infection>[diseases];
      this.health_condition = new health_condition_t[diseases];

      for (int disease_id = 0; disease_id < diseases; ++disease_id)
      {
        this.susceptibility_multp[disease_id] = 1.0;
        this.infectee_count[disease_id] = 0;
        this.exposure_date[disease_id] = -1;
        this.infector_id[disease_id] = -1;
        this.immunity_end_date[disease_id] = -1;
        this.past_infections[disease_id] = new List<Past_Infection>();
        this.health_condition[disease_id].state = -1;
        this.health_condition[disease_id].last_transition_day = -1;
        this.health_condition[disease_id].next_state = -1;
        this.health_condition[disease_id].next_transition_day = -1;

        var disease = Global.Diseases.get_disease(disease_id);
        if (disease.assume_susceptible())
        {
          become_susceptible(disease_id);
        }

        if (disease.get_at_risk() != null && !disease.get_at_risk().is_empty())
        {
          double at_risk_prob = disease.get_at_risk().find_value(myself.get_real_age());
          if (FredRandom.NextDouble() < at_risk_prob)
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

      if (Health.nantivirals == -1)
      {
        FredParameters.GetParameter("number_antivirals", ref nantivirals);
      }

      if (Global.Enable_Chronic_Condition && Health.is_initialized)
      {
        double prob = Health.asthma_prob.find_value(myself.get_real_age());
        set_is_asthmatic((FredRandom.NextDouble() < prob));

        prob = Health.COPD_prob.find_value(myself.get_real_age());
        set_has_COPD((FredRandom.NextDouble() < prob));

        prob = Health.chronic_renal_disease_prob.find_value(myself.get_real_age());
        set_has_chronic_renal_disease((FredRandom.NextDouble() < prob));

        prob = Health.diabetes_prob.find_value(myself.get_real_age());
        set_is_diabetic((FredRandom.NextDouble() < prob));

        prob = Health.heart_disease_prob.find_value(myself.get_real_age());
        set_has_heart_disease((FredRandom.NextDouble() < prob));

        prob = Health.hypertension_prob.find_value(myself.get_real_age());
        set_has_hypertension((FredRandom.NextDouble() < prob));

        prob = Health.hypercholestrolemia_prob.find_value(myself.get_real_age());
        set_has_hypercholestrolemia((FredRandom.NextDouble() < prob));
      }
    }

    // UPDATE THE PERSON'S HEALTH CONDITIONS
    public void update_infection(int day, int disease_id)
    {
      if (this.has_face_mask_behavior)
      {
        update_face_mask_decision(day);
      }

      if (this.infection[disease_id] == null)
      {
        return;
      }

      Utils.FRED_VERBOSE(1, "update_infection %d on day %d person %d\n", disease_id, day, myself.get_id());
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

      Utils.FRED_VERBOSE(1, "update_infection %d FINISHED on day %d person %d\n",
             disease_id, day, myself.get_id());
    }

    public void update_face_mask_decision(int day)
    {
      // printf("update_face_mask_decision entered on day %d for person %d\n", day, myself.get_id());

      // should we start use face mask?
      if (this.is_symptomatic(day) && this.days_wearing_face_mask == 0)
      {
        Utils.FRED_VERBOSE(1, "FACEMASK: person %d starts wearing face mask on day %d\n", myself.get_id(), day);
        this.start_wearing_face_mask();
      }

      // should we stop using face mask?
      if (this.is_wearing_face_mask())
      {
        if (this.is_symptomatic(day) && this.days_wearing_face_mask < Health.Days_to_wear_face_masks)
        {
          this.days_wearing_face_mask++;
        }
        else
        {
          Utils.FRED_VERBOSE(1, "FACEMASK: person %d stops wearing face mask on day %d\n", myself.get_id(), day);
          this.stop_wearing_face_mask();
        }
      }
    }

    public void update_interventions(int day)
    {
      // if deceased, health status should have been cleared during population
      // update (by calling Person.die(), then Health.die(), which will reset (bool) alive
      if (!this.alive)
      {
        return;
      }

      if (this.intervention_flags.any())
      {
        // update vaccine status
        if (this.intervention_flags[(int)Intervention_flag.TAKES_VACCINE])
        {
          foreach (var vHealth in this.vaccine_health)
          {
            vHealth.update(day, myself.get_real_age());
          }
        }
        // update antiviral status
        if (this.intervention_flags[(int)Intervention_flag.TAKES_AV])
        {
          foreach (var avHealth in this.av_health)
          {
            avHealth.update(day);
          }
        }
      }
    }

    public void become_exposed(int disease_id, Person infector, Mixing_Group mixing_group, int day)
    {

      Utils.FRED_VERBOSE(0, "become_exposed: person %d is exposed to disease %d day %d\n",
                   myself.get_id(), disease_id, day);

      if (this.infection[disease_id] != null)
      {
        Utils.fred_abort("DOUBLE EXPOSURE: person %d dis_id %d day %d\n", myself.get_id(), disease_id, day);
      }

      if (Global.Verbose > 0)
      {
        if (mixing_group == null)
        {
          Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
                "HEALTH CHART: %s person %d is an IMPORTED EXPOSURE to disease %d\n",
                Date.get_date_string(),
                myself.get_id(), disease_id);
        }
        else
        {
          Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
                "HEALTH CHART: %s person %d is EXPOSED to disease %d\n",
                Date.get_date_string(),
                myself.get_id(), disease_id);
        }
      }

      this.infectious.Set(disease_id, false);
      this.symptomatic.Set(disease_id, false);
      var disease = Global.Diseases.get_disease(disease_id);
      this.infection[disease_id] = Infection.get_new_infection(disease, infector, myself, mixing_group, day);
      Utils.FRED_VERBOSE(1, "setup infection: person %d dis_id %d day %d\n", myself.get_id(), disease_id, day);
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

      if (Global.Enable_Transmission_Network)
      {
        Utils.FRED_VERBOSE(1, "Joining transmission network: %d\n", myself.get_id());
        myself.join_network(Global.Transmission_Network);
      }

      if (Global.Enable_Vector_Transmission && Global.Diseases.get_number_of_diseases() > 1)
      {
        // special check for multi-serotype dengue:
        if (this.previous_infection_serotype == -1)
        {
          // remember this infection's serotype
          this.previous_infection_serotype = disease_id;
          // after the first infection, become immune to other two serotypes.
          for (int sero = 0; sero < Global.Diseases.get_number_of_diseases(); ++sero)
          {
            // if (sero == previous_infection_serotype) continue;
            if (sero == disease_id)
            {
              continue;
            }
            Utils.FRED_STATUS(1, "DENGUE: person %d now immune to serotype %d\n",
            myself.get_id(), sero);
            become_unsusceptible(Global.Diseases.get_disease(sero));
          }
        }
        else
        {
          // after the second infection, become immune to other two serotypes.
          for (int sero = 0; sero < Global.Diseases.get_number_of_diseases(); ++sero)
          {
            if (sero == this.previous_infection_serotype)
            {
              continue;
            }
            if (sero == disease_id)
            {
              continue;
            }
            Utils.FRED_STATUS(1, "DENGUE: person %d now immune to serotype %d\n",
            myself.get_id(), sero);
            become_unsusceptible(Global.Diseases.get_disease(sero));
          }
        }
      }
    }

    public void become_susceptible(int disease_id)
    {
      if (this.susceptible[disease_id])
      {
        Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
              "HEALTH CHART: %s person %d is already SUSCEPTIBLE for disease %d\n",
              Date.get_date_string(),
              myself.get_id(), disease_id);
        return;
      }

      Utils.assert(this.infection[disease_id] == null);
      this.susceptibility_multp[disease_id] = 1.0;
      this.susceptible.Set(disease_id, true);
      Utils.assert(is_susceptible(disease_id));
      this.recovered.Set(disease_id, false);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is SUSCEPTIBLE for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_susceptible(Disease disease)
    {
      this.become_susceptible(disease.get_id());
    }

    public void become_susceptible_by_vaccine_waning(int disease_id)
    {
      if (this.susceptible[disease_id])
      {
        return;
      }
      if (this.infection[disease_id] == null)
      {
        // not already infected
        this.susceptibility_multp[disease_id] = 1.0;
        this.susceptible.Set(disease_id, true);
        Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
              "HEALTH CHART: %s person %d is SUSCEPTIBLE for disease %d\n",
              Date.get_date_string(),
              myself.get_id(), disease_id);
      }
      else
      {
        Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
              "HEALTH CHART: %s person %d had no vaccine waning because was already infected with disease %d\n",
              Date.get_date_string(),
              myself.get_id(), disease_id);
      }
    }

    public void become_unsusceptible(Disease disease)
    {
      this.become_unsusceptible(disease.get_id());
    }

    public void become_unsusceptible(int disease_id)
    {
      this.susceptible.Set(disease_id, false);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is UNSUSCEPTIBLE for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_infectious(Disease disease)
    {
      int disease_id = disease.get_id();
      Utils.assert(this.infection[disease_id] != null);
      this.infectious.Set(disease_id, true);
      int household_index = myself.get_exposed_household_index();
      var h = Global.Places.get_household_ptr(household_index);
      Utils.assert(h != null);
      h.set_human_infectious(disease_id);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is INFECTIOUS for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_noninfectious(Disease disease)
    {
      int disease_id = disease.get_id();
      Utils.assert(this.infection[disease_id] != null);
      this.infectious.Set(disease_id, false);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is NONINFECTIOUS for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_symptomatic(Disease disease)
    {
      int disease_id = disease.get_id();
      if (this.infection[disease_id] == null)
      {
        Utils.FRED_STATUS(1, "Help: becoming symptomatic with no infection: person %d, disease_id %d\n", myself.get_id(), disease_id);
      }
      Utils.assert(this.infection[disease_id] != null);
      if (this.symptomatic[disease_id])
      {
        Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
              "HEALTH CHART: %s person %d is ALREADY SYMPTOMATIC for disease %d\n",
              Date.get_date_string(),
              myself.get_id(), disease_id);
        return;
      }
      this.symptomatic.Set(disease_id, true);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is SYMPTOMATIC for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void resolve_symptoms(Disease disease)
    {
      int disease_id = disease.get_id();
      // assert(this.infection[disease_id] != null);
      if (this.symptomatic[disease_id])
      {
        this.symptomatic.Set(disease_id, false);
      }
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d RESOLVES SYMPTOMS for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_case_fatality(int disease_id, int day)
    {
      Utils.FRED_VERBOSE(0, "DISEASE %d is FATAL: day %d person %d\n", disease_id, day, myself.get_id());
      this.case_fatality.Set(disease_id, true);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
             "HEALTH CHART: %s person %d is CASE_FATALITY for disease %d\n",
             Date.get_date_string(),
             myself.get_id(), disease_id);
      become_removed(disease_id, day);

      // update household counts
      var hh = myself.get_household();
      if (hh == null)
      {
        if (Global.Enable_Hospitals && myself.is_hospitalized() && myself.get_permanent_household() != null)
        {
          hh = myself.get_permanent_household();
        }
      }
      if (hh != null)
      {
        hh.increment_case_fatalities(day, disease_id);
      }


      // queue removal from population
      Global.Pop.prepare_to_die(day, myself);
    }

    public void become_immune(Disease disease)
    {
      int disease_id = disease.get_id();
      disease.become_immune(myself, this.susceptible[disease_id],
          this.infectious[disease_id], this.symptomatic[disease_id]);
      this.immunity.Set(disease_id, true);
      this.susceptible.Set(disease_id, false);
      this.infectious. Set(disease_id, false);
      this.symptomatic.Set(disease_id, false);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
            "HEALTH CHART: %s person %d is IMMUNE for disease %d\n",
            Date.get_date_string(),
            myself.get_id(), disease_id);
    }

    public void become_removed(int disease_id, int day)
    {
      terminate_infection(disease_id, day);
      this.susceptible.Set(disease_id, false);
      this.infectious.Set(disease_id, false);
      this.symptomatic.Set(disease_id, false);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
             "HEALTH CHART: %s person %d is REMOVED for disease %d\n",
             Date.get_date_string(),
             myself.get_id(), disease_id);
    }

    public void declare_at_risk(Disease disease)
    {
      this.at_risk.Set(disease.get_id(), true);
    }

    public void recover(Disease disease, int day)
    {
      int disease_id = disease.get_id();
      // assert(this.infection[disease_id] != null);
      Utils.FRED_CONDITIONAL_VERBOSE(0, Global.Enable_Health_Charts,
             "HEALTH CHART: %s person %d is RECOVERED from disease %d\n",
             Date.get_date_string(),
             myself.get_id(), disease_id);
      this.recovered.Set(disease_id, true);
      int household_index = myself.get_exposed_household_index();
      var h = Global.Places.get_household_ptr(household_index);
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

    public void advance_seed_infection(int disease_id, int days_to_advance)
    {
      Utils.assert(this.infection[disease_id] != null);
      this.infection[disease_id].advance_seed_infection(days_to_advance);
    }

    public void infect(Person infectee, int disease_id, Mixing_Group mixing_group, int day)
    {
      infectee.become_exposed(disease_id, myself, mixing_group, day);

      ++(this.infectee_count[disease_id]);

      int exp_day = this.get_exposure_date(disease_id);
      Utils.assert(0 <= exp_day);
      var disease = Global.Diseases.get_disease(disease_id);
      disease.increment_cohort_infectee_count(exp_day);

      Utils.FRED_STATUS(1, "person %d infected person %d infectees = %d\n",
            myself.get_id(), infectee.get_id(), infectee_count[disease_id]);

      if (Global.Enable_Transmission_Network)
      {
        Utils.FRED_VERBOSE(1, "Creating link in transmission network: %d . %d\n", myself.get_id(), infectee.get_id());
        myself.create_network_link_to(infectee, Global.Transmission_Network);
      }
    }

    //  void increment_infectee_count(int disease_id, Person* infectee, Mixing_Group* mixing_group, int day);
    public void start_wearing_face_mask()
    {
      this.wears_face_mask_today = true;
    }

    public void stop_wearing_face_mask()
    {
      this.wears_face_mask_today = false;
    }

    public void clear_past_infections(int disease_id)
    {
      this.past_infections[disease_id].Clear();
    }

    public void add_past_infection(int strain_id, int recovery_date, int age_at_exposure, Disease dis)
    {
      this.past_infections[dis.get_id()].Add(new Past_Infection(strain_id, recovery_date, age_at_exposure));
    }

    public void update_mixing_group_counts(int day, int disease_id, Mixing_Group mixing_group)
    {
      // this is only called for people with an active infection
      Utils.assert(is_infected(disease_id));

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

    public void terminate_infection(int disease_id, int day)
    {
      if (this.health_condition[disease_id].state > -1)
      {
        Global.Diseases.get_disease(disease_id).terminate_person(myself, day);
      }

      if (this.infection[disease_id] != null)
      {
        // delete the infection object
        this.infection[disease_id] = null;
      }
    }

    public void terminate(int day)
    {
      for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
      {
        if (this.infection[disease_id] != null)
        {
          become_removed(disease_id, day);
        }
        if (this.health_condition[disease_id].state == 0)
        {
          Global.Diseases.get_disease(disease_id).terminate_person(myself, day); ;
        }
      }
      this.alive = false;
    }

    // ACCESS TO HEALTH CONDITIONS

    public int get_days_symptomatic()
    {
      return this.days_symptomatic;
    }
    public int get_exposure_date(int disease_id)
    {
      return this.exposure_date[disease_id];
    }

    public int get_infectious_start_date(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return -1;
      }
      
      return this.infection[disease_id].get_infectious_start_date();
    }

    public int get_infectious_end_date(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return -1;
      }

      return this.infection[disease_id].get_infectious_end_date();
    }

    public int get_symptoms_start_date(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return -1;
      }

      return this.infection[disease_id].get_symptoms_start_date();
    }

    public int get_symptoms_end_date(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return -1;
      }

      return this.infection[disease_id].get_symptoms_end_date();
    }

    public int get_immunity_end_date(int disease_id)
    {
      return this.immunity_end_date[disease_id];
    }

    public int get_infector_id(int disease_id)
    {
      return infector_id[disease_id];
    }

    public Person get_infector(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return null;
      }
      
      return this.infection[disease_id].get_infector();
    }

    public Mixing_Group get_infected_mixing_group(int disease_id)
    {
      return this.infected_in_mixing_group[disease_id];
    }

    public int get_infected_mixing_group_id(int disease_id)
    {
      var mixing_group = get_infected_mixing_group(disease_id);
      if (mixing_group == null)
      {
        return -1;
      }

      return mixing_group.get_id();
    }

    public string get_infected_mixing_group_label(int disease_id)
    {
      if (this.infection[disease_id] == null)
      {
        return "-";
      }

      var mixing_group = get_infected_mixing_group(disease_id);
      if (mixing_group == null)
      {
        return "X";
      }
      
      return mixing_group.get_label();
    }

    public char get_infected_mixing_group_type(int disease_id)
    {
      var mixing_group = get_infected_mixing_group(disease_id);
      if (mixing_group == null)
      {
        return Mixing_Group.SUBTYPE_NONE;
      }

      return mixing_group.get_type();
    }

    public int get_infectees(int disease_id)
    {
      return this.infectee_count[disease_id];
    }

    public double get_susceptibility(int disease_id)
    {
      double suscep_multp = this.susceptibility_multp[disease_id];

      if (this.infection[disease_id] == null)
      {
        return suscep_multp;
      }

      return this.infection[disease_id].get_susceptibility() * suscep_multp;
    }

    public double get_infectivity(int disease_id, int day)
    {
      if (this.infection[disease_id] == null)
      {
        return 0.0;
      }
      
      return this.infection[disease_id].get_infectivity(day);
    }

    public double get_symptoms(int disease_id, int day)
    {

      if (this.infection[disease_id] == null)
      {
        return 0.0;
      }
      
      return this.infection[disease_id].get_symptoms(day);
    }

    public Infection get_infection(int disease_id)
    {
      return this.infection[disease_id];
    }

    public double get_transmission_modifier_due_to_hygiene(int disease_id)
    {
      var disease = Global.Diseases.get_disease(disease_id);
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

    public double get_susceptibility_modifier_due_to_hygiene(int disease_id)
    {
      var disease = Global.Diseases.get_disease(disease_id);
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

    public double get_susceptibility_modifier_due_to_household_income(int hh_income)
    {

      if (Global.Enable_hh_income_based_susc_mod)
      {
        if (hh_income >= Household.get_min_hh_income_90_pct())
        {
          return Hh_income_susc_mod_floor;
        }
        else
        {
          double rise = 1.0 - Hh_income_susc_mod_floor;
          double run = Household.get_min_hh_income() - Household.get_min_hh_income_90_pct();
          double m = rise / run;

          // Equation of line is y - y1 = m(x - x1)
          // y = m*x - m*x1 + y1
          double x = hh_income;
          return m * x - m * Household.get_min_hh_income() + 1.0;
        }
      }
      else
      {
        return 1.0;
      }
    }

    public int get_num_past_infections(int disease)
    {
      return this.past_infections[disease].Count;
    }

    public Past_Infection get_past_infection(int disease, int i)
    {
      return this.past_infections[disease][i];
    }


    // TESTS FOR HEALTH CONDITIONS

    public bool is_case_fatality(int disease_id)
    {
      return this.case_fatality[disease_id];
    }

    public bool is_susceptible(int disease_id)
    {
      return this.susceptible[disease_id];
    }

    public bool is_infectious(int disease_id)
    {
      return this.infectious[disease_id];
    }

    public bool is_infected(int disease_id)
    {
      return this.infection[disease_id] != null;
    }

    public bool is_symptomatic()
    {
      return this.symptomatic.any();
    }

    public bool is_symptomatic(int disease_id)
    {
      return this.symptomatic[disease_id];
    }

    public bool is_recovered(int disease_id)
    {
      return this.recovered[disease_id];
    }

    public bool is_immune(int disease_id)
    {
      return this.immunity[disease_id];
    }

    public bool is_at_risk(int disease_id)
    {
      return this.at_risk[disease_id];
    }

    public bool is_on_av_for_disease(int day, int disease_id)
    {
      foreach (var avHealth in this.av_health)
      {
        if (avHealth.get_disease() == disease_id && avHealth.is_on_av(day))
        {
          return true;
        }
      }
      return false;
    }

    // Personal Health Behaviors
    public bool is_wearing_face_mask()
    {
      return this.wears_face_mask_today;
    }

    public bool is_washing_hands()
    {
      return this.washes_hands;
    }

    public bool is_newly_infected(int day, int disease_id)
    {
      return day == get_exposure_date(disease_id);
    }

    public bool is_newly_symptomatic(int day, int disease_id)
    {
      return day == get_symptoms_start_date(disease_id);
    }

    public bool is_alive()
    {
      return this.alive;
    }


    // MEDICATION OPERATORS

    /**
     * Agent will take a vaccine
     * @param vacc pointer to the Vaccine to take
     * @param day the simulation day
     * @param vm a pointer to the Manager of the Vaccinations
     */
    public void take_vaccine(Vaccine vaccine, int day, Vaccine_Manager vm)
    {
      // Compliance will be somewhere else
      double real_age = myself.get_real_age();
      // Is this our first dose?
      Vaccine_Health vaccine_health_for_dose = null;

      if (this.vaccine_health == null)
      {
        this.vaccine_health = new List<Vaccine_Health>();
      }

      foreach (var vHealth in this. vaccine_health)
      {
        if (vHealth.get_vaccine() == vaccine)
        {
          vaccine_health_for_dose = vHealth;
        }
      }

      if (vaccine_health_for_dose == null)
      { // This is our first dose of this vaccine
        this.vaccine_health.Add(new Vaccine_Health(day, vaccine, real_age, myself, vm));
        this.intervention_flags[(int)Intervention_flag.TAKES_VACCINE] = true;
      }
      else
      { // Already have a dose, need to take the next dose
        vaccine_health_for_dose.update_for_next_dose(day, real_age);
      }

      if (Global.VaccineTracefp != null)
      {
        Global.VaccineTracefp.WriteLine(" id {0} vaccid {1}", myself.get_id(),
          this.vaccine_health[this.vaccine_health.Count - 1].get_vaccine().get_ID());
        this.vaccine_health[this.vaccine_health.Count - 1].printTrace();
      }
    }

    /**
     * Agent will take an antiviral
     * @param av pointer to the Antiviral to take
     * @param day the simulation day
     */
    public void take(Antiviral av, int day)
    {
      if (this.checked_for_av == null)
      {
        this.checked_for_av = new List<bool>(nantivirals);
      }
      if (this.av_health == null)
      {
        this.av_health = new List<AV_Health>();
      }
      this.av_health.Add(new AV_Health(day, av, this));
      this.intervention_flags[(int)Intervention_flag.TAKES_AV] = true;
      return;
    }

    /**
     * @return a count of the antivirals this agent has already taken
     */
    public int get_number_av_taken()
    {
      if (this.av_health != null)
      {
        return this.av_health.Count;
      }

      return 0;
    }

    /**
     * @param s the index of the av to check
     * @return the checked_for_av with the given index
     */
    public int get_checked_for_av(int s)
    {
      Utils.assert(this.checked_for_av != null);
      return this.checked_for_av[s] ? 1 : 0;
    }

    /**
     * Set the checked_for_av value at the given index to 1
     * @param s the index of the av to set
     */
    public void flip_checked_for_av(int s)
    {
      if (this.checked_for_av == null)
      {
        this.checked_for_av = new List<bool>(nantivirals);
      }

      this.checked_for_av[s] = true;
    }

    /**
     * @return <code>true</code> if the agent is vaccinated, <code>false</code> if not
     */
    public bool is_vaccinated()
    {
      if (this.vaccine_health != null)
      {
        return this.vaccine_health.Count > 0;
      }

      return false;
    }

    /**
     * @return the number of vaccines this agent has taken
     */
    int get_number_vaccines_taken()
    {
      if (this.vaccine_health != null)
      {
        return this.vaccine_health.Count;
      }

      return 0;
    }

    /**
     * @return a pointer to this instance's AV_Health object
     */
    public AV_Health get_av_health(int i)
    {
      Utils.assert(this.av_health != null);
      return this.av_health[i];
    }

    /**
     * @return this instance's av_start day
     */
    public int get_av_start_day(int i)
    {
      Utils.assert(this.av_health != null);
      return this.av_health[i].get_av_start_day();
    }

    /**
     * @return a pointer to this instance's Vaccine_Health object
     */
    public Vaccine_Health get_vaccine_health(int i)
    {
      if (this.vaccine_health != null)
      {
        return this.vaccine_health[i];
      }
      return null;
    }

    // MODIFIERS

    /**
     * Alter the susceptibility of the agent to the given disease by a multiplier
     * @param disease the disease to which the agent is suceptible
     * @param multp the multiplier to apply
     */
    public void modify_susceptibility(int disease_id, double multp)
    {
      this.susceptibility_multp[disease_id] *= multp;
    }

    /**
     * Alter the infectivity of the agent to the given disease by a multiplier
     * @param disease the disease with which the agent is infectious
     * @param multp the multiplier to apply
     */
    public void modify_infectivity(int disease_id, double multp)
    {
      if (this.infection[disease_id] != null)
      {
        this.infection[disease_id].modify_infectivity(multp);
      }
    }

    /**
     * Alter the infectious period of the agent for the given disease by a multiplier.
     * Modifying infectious period is equivalent to modifying symptomatic and asymptomatic
     * periods by the same amount. Current day is needed to modify infectious period, because we can't cause this
     * infection to recover in the past.
     *
     * @param disease the disease with which the agent is infectious
     * @param multp the multiplier to apply
     * @param cur_day the simulation day
     */
    public void modify_infectious_period(int disease_id, double multp, int cur_day)
    {
      if (this.infection[disease_id] != null)
      {
        this.infection[disease_id].modify_infectious_period(multp, cur_day);
      }
    }

    /**
     * Alter the symptomatic period of the agent for the given disease by a multiplier.
     * Current day is needed to modify symptomatic period, because we can't cause this
     * infection to recover in the past.
     *
     * @param disease the disease with which the agent is symptomatic
     * @param multp the multiplier to apply
     * @param cur_day the simulation day
     */
    public void modify_symptomatic_period(int disease_id, double multp, int cur_day)
    {
      if (this.infection[disease_id] != null)
      {
        this.infection[disease_id].modify_asymptomatic_period(multp, cur_day);
      }
    }

    /**
     * Alter the asymptomatic period of the agent for the given disease by a multiplier.
     * Current day is needed to modify symptomatic period, because we can't cause this
     * infection to recover in the past.
     *
     * @param disease the disease with which the agent is asymptomatic
     * @param multp the multiplier to apply
     * @param cur_day the simulation day
     */
    public void modify_asymptomatic_period(int disease_id, double multp, int cur_day)
    {
      if (this.infection[disease_id] != null)
      {
        this.infection[disease_id].modify_symptomatic_period(multp, cur_day);
      }
    }

    /**
     * Alter the whether or not the agent will develop symptoms.
     * Can't change develops_symptoms if this person is not asymptomatic ('i' or 'E')
     * Current day is needed to modify symptomatic period, because we can't change symptomaticity that
     * is in the past.
     *
     * @param disease the disease with which the agent is asymptomatic
     * @param symptoms whether or not the agent is showing symptoms
     * @param cur_day the simulation day
     */
    public void modify_develops_symptoms(int disease_id, bool symptoms, int cur_day)
    {
      if (this.infection[disease_id] != null
         && ((this.infection[disease_id].is_infectious(cur_day)
              && !this.infection[disease_id].is_symptomatic(cur_day))
              || !this.infection[disease_id].is_infectious(cur_day)))
      {

        this.infection[disease_id].modify_develops_symptoms(symptoms, cur_day);
        this.symptomatic.Set(disease_id, true);
      }
    }


    // CHRONIC CONDITIONS

    /**
     * @return <code>true</code> if agent is asthmatic, <code>false</code> otherwise
     */
    public bool is_asthmatic()
    {
      return has_chronic_condition(Chronic_condition_index.ASTHMA);
    }

    /**
     * Sets whether or not agent is asthmatic
     * @param has_cond whether or not the agent is asthmatic
     */
    public void set_is_asthmatic(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.ASTHMA, has_cond);
    }

    /**
     * @return <code>true</code> if agent has COPD (Chronic Obstructive Pulmonary
     * disease), <code>false</code> otherwise
     */
    public bool has_COPD()
    {
      return has_chronic_condition(Chronic_condition_index.COPD);
    }

    /**
     * Sets whether or not the agent has COPD (Chronic Obstructive Pulmonary
     * disease)
     * @param has_cond whether or not the agent has COPD
     */
    public void set_has_COPD(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.COPD, has_cond);
    }

    /**
     * @return <code>true</code> if agent has chronic renal disease, <code>false</code> otherwise
     */
    public bool has_chronic_renal_disease()
    {
      return has_chronic_condition(Chronic_condition_index.CHRONIC_RENAL_DISEASE);
    }

    /**
     * Sets whether or not the agent has chronic renal disease
     * @param has_cond whether or not the agent has chronic renal disease
     */
    public void set_has_chronic_renal_disease(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.CHRONIC_RENAL_DISEASE,
              has_cond);
    }

    /**
     * @return <code>true</code> if agent is diabetic, <code>false</code> otherwise
     */
    public bool is_diabetic()
    {
      return has_chronic_condition(Chronic_condition_index.DIABETES);
    }

    /**
     * Sets whether or not the agent is diabetic
     * @param has_cond whether or not the agent is diabetic
     */
    public void set_is_diabetic(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.DIABETES, has_cond);
    }

    /**
     * @return <code>true</code> if agent has heart disease, <code>false</code> otherwise
     */
    public bool has_heart_disease()
    {
      return has_chronic_condition(Chronic_condition_index.HEART_DISEASE);
    }

    /**
     * Sets whether or not the agent has heart disease
     * @param has_cond whether or not the agent has heart disease
     */
    public void set_has_heart_disease(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.HEART_DISEASE, has_cond);
    }

    /**
     * @return <code>true</code> if agent has hypertension, <code>false</code> otherwise
     */
    public bool has_hypertension()
    {
      return has_chronic_condition(Chronic_condition_index.HYPERTENSION);
    }

    /**
     * Sets whether or not the agent has hypertension
     * @param has_cond whether or not the agent has hypertension
     */
    public void set_has_hypertension(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.HYPERTENSION, has_cond);
    }

    /**
     * @return <code>true</code> if agent has hypercholestrolemia, <code>false</code> otherwise
     */
    public bool has_hypercholestrolemia()
    {
      return has_chronic_condition(Chronic_condition_index.HYPERCHOLESTROLEMIA);
    }

    /**
     * Sets whether or not the agent has hypercholestrolemia 
     * @param has_cond whether or not the agent has hypercholestrolemia 
     */
    public void set_has_hypercholestrolemia(bool has_cond)
    {
      set_has_chronic_condition(Chronic_condition_index.HYPERCHOLESTROLEMIA, has_cond);
    }

    /*
     * @return <code>true</code> if the map contains the condition and it is true,
     *  <code>false</code> otherwise
     * @param cond_idx the Chronic_condition_index to search for
     */
    public bool has_chronic_condition(Chronic_condition_index cond_idx)
    {
      if (this.chronic_conditions_map.ContainsKey(cond_idx))
      {
        return this.chronic_conditions_map[cond_idx];
      }
      
      return false;
    }

    /*
     * Set the given Chronic_medical_condtion to <code>true</code> or <code>false</code>
     * @param cond_idx the Chronic_condition_index to set
     * @param has_cond whether or not the agent has the condition
     */
    public void set_has_chronic_condition(Chronic_condition_index cond_idx, bool has_cond)
    {
      this.chronic_conditions_map[cond_idx] = has_cond;
    }

    /*
     * @return <code>true</code> if the map contains any condition and it that condition is true,
     *  <code>false</code> otherwise
     */
    public bool has_chronic_condition()
    {
      foreach (Chronic_condition_index condition in Enum.GetValues(typeof(Chronic_condition_index)))
      {
        if (has_chronic_condition(condition))
        {
          return true;
        }
      }
      return false;
    }

    // HEALTH INSURANCE

    public Insurance_assignment_index get_insurance_type()
    {
      return this.insurance_type;
    }

    public void set_insurance_type(Insurance_assignment_index insurance_type)
    {
      this.insurance_type = insurance_type;
    }

    /////// STATIC MEMBERS

    public static int nantivirals;

    public static string chronic_condition_lookup(Chronic_condition_index.e idx)
    {
      Utils.assert(idx >= 0);
      Utils.assert(idx < Chronic_condition_index.CHRONIC_MEDICAL_CONDITIONS);
      switch (idx)
      {
        case Chronic_condition_index.ASTHMA:
          return "Asthma";
        case Chronic_condition_index.COPD:
          return "COPD";
        case Chronic_condition_index.CHRONIC_RENAL_DISEASE:
          return "Chronic Renal Disease";
        case Chronic_condition_index.DIABETES:
          return "Diabetes";
        case Chronic_condition_index.HEART_DISEASE:
          return "Heart Disease";
        case Chronic_condition_index.HYPERTENSION:
          return "Hypertension";
        case Chronic_condition_index.HYPERCHOLESTROLEMIA:
          return "Hypercholestrolemia";
        default:
          Utils.fred_abort("Invalid Chronic Condition Type", "");
          break;
      }

      return null;
    }

    public static string insurance_lookup(Insurance_assignment_index.e idx)
    {
      Utils.assert(idx >= Insurance_assignment_index.PRIVATE);
      Utils.assert(idx <= Insurance_assignment_index.UNSET);
      switch (idx)
      {
        case Insurance_assignment_index.PRIVATE:
          return "Private";
        case Insurance_assignment_index.MEDICARE:
          return "Medicare";
        case Insurance_assignment_index.MEDICAID:
          return "Medicaid";
        case Insurance_assignment_index.HIGHMARK:
          return "Highmark";
        case Insurance_assignment_index.UPMC:
          return "UPMC";
        case Insurance_assignment_index.UNINSURED:
          return "Uninsured";
        case Insurance_assignment_index.UNSET:
          return "UNSET";
        default:
          Utils.fred_abort("Invalid Health Insurance Type", "");
          break;
      }

      return null;
    }

    static Insurance_assignment_index get_insurance_type_from_int(int insurance_type)
    {
      switch (insurance_type)
      {
        case 0:
          return Insurance_assignment_index.PRIVATE;
        case 1:
          return Insurance_assignment_index.MEDICARE;
        case 2:
          return Insurance_assignment_index.MEDICAID;
        case 3:
          return Insurance_assignment_index.HIGHMARK;
        case 4:
          return Insurance_assignment_index.UPMC;
        case 5:
          return Insurance_assignment_index.UNINSURED;
        default:
          return Insurance_assignment_index.UNSET;
      }
    }

    public static Insurance_assignment_index get_health_insurance_from_distribution()
    {
      if (Global.Enable_Health_Insurance && is_initialized)
      {
        int i = FredRandom.DrawFromDistribution(health_insurance_cdf_size, health_insurance_distribution);
        return Health.get_insurance_type_from_int(i);
      }

      return Insurance_assignment_index.UNSET;
    }

    public static double get_chronic_condition_case_fatality_prob_mult(double real_age, Chronic_condition_index cond_idx)
    {
      if (Global.Enable_Chronic_Condition && Health.is_initialized)
      {
        Utils.assert(cond_idx >= Chronic_condition_index.ASTHMA);
        Utils.assert(cond_idx < Chronic_condition_index.CHRONIC_MEDICAL_CONDITIONS);
        switch (cond_idx)
        {
          case Chronic_condition_index.ASTHMA:
            return Health.asthma_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.COPD:
            return Health.COPD_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.CHRONIC_RENAL_DISEASE:
            return Health.chronic_renal_disease_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.DIABETES:
            return Health.diabetes_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.HEART_DISEASE:
            return Health.heart_disease_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.HYPERTENSION:
            return Health.hypertension_case_fatality_prob_mult.find_value(real_age);
          case Chronic_condition_index.HYPERCHOLESTROLEMIA:
            return Health.hypercholestrolemia_case_fatality_prob_mult.find_value(real_age);
          default:
            return 1.0;
        }
      }

      return 1.0;
    }

    public static double get_chronic_condition_hospitalization_prob_mult(double real_age, Chronic_condition_index cond_idx)
    {
      if (Global.Enable_Chronic_Condition && Health.is_initialized)
      {
        Utils.assert(cond_idx >= Chronic_condition_index.ASTHMA);
        Utils.assert(cond_idx < Chronic_condition_index.CHRONIC_MEDICAL_CONDITIONS);
        switch (cond_idx)
        {
          case Chronic_condition_index.ASTHMA:
            return Health.asthma_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.COPD:
            return Health.COPD_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.CHRONIC_RENAL_DISEASE:
            return Health.chronic_renal_disease_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.DIABETES:
            return Health.diabetes_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.HEART_DISEASE:
            return Health.heart_disease_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.HYPERTENSION:
            return Health.hypertension_hospitalization_prob_mult.find_value(real_age);
          case Chronic_condition_index.HYPERCHOLESTROLEMIA:
            return Health.hypercholestrolemia_hospitalization_prob_mult.find_value(real_age);
          default:
            return 1.0;
        }
      }
      return 1.0;
    }

    public static double get_pregnancy_case_fatality_prob_mult(double real_age)
    {
      if (Global.Enable_Chronic_Condition && Health.is_initialized)
      {
        return Health.pregnancy_case_fatality_prob_mult.find_value(real_age);
      }
      return 1.0;
    }

    public static double get_pregnancy_hospitalization_prob_mult(double real_age)
    {
      if (Global.Enable_Chronic_Condition && Health.is_initialized)
      {
        return Health.pregnancy_hospitalization_prob_mult.find_value(real_age);
      }
      return 1.0;
    }

    public static bool Enable_hh_income_based_susc_mod;

    public void set_fatal_infection(int disease_id)
    {
      Utils.assert(this.infection[disease_id] != null);
      this.infection[disease_id].set_fatal_infection();
    }

    public int get_health_state(int disease_id)
    {
      return this.health_condition[disease_id].state;
    }

    public void set_health_state(int disease_id, int s, int day)
    {
      this.health_condition[disease_id].state = s;
      this.health_condition[disease_id].last_transition_day = day;
    }

    public int get_last_transition_day(int disease_id)
    {
      return this.health_condition[disease_id].last_transition_day;
    }

    public int get_next_health_state(int disease_id)
    {
      return this.health_condition[disease_id].next_state;
    }

    public void set_next_health_state(int disease_id, int s, int day)
    {
      this.health_condition[disease_id].next_state = s;
      this.health_condition[disease_id].next_transition_day = day;
    }

    public int get_next_transition_day(int disease_id)
    {
      return this.health_condition[disease_id].next_transition_day;
    }

    public void update_health_conditions(int day)
    {
      for (int disease_id = 0; disease_id < Global.Diseases.get_number_of_diseases(); ++disease_id)
      {
        if (this.health_condition[disease_id].state > -1)
        {
          Global.Diseases.get_disease(disease_id).get_epidemic().transition_person(this.myself, day, this.health_condition[disease_id].state);
        }
      }
    }
  }
}
