﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Household : Place
  {

    /**
     * Set the household income.
     */
    public void set_household_income(int x)
    {
      this->household_income = x;
      this->set_household_income_code(static_cast<int>(Household::get_household_income_level_code_from_income(x)));
      if (x != -1)
      {
        if (x >= 0 && x < Household::Min_hh_income)
        {
          Household::Min_hh_income = x;
          //Utils::fred_log("HH_INCOME: Min[%i]\n", Household::Min_hh_income);
        }
        if (x > Household::Max_hh_income)
        {
          Household::Max_hh_income = x;
          //Utils::fred_log("HH_INCOME: Max[%i]\n", Household::Max_hh_income);
        }
      }
    }

    static string household_income_level_lookup(HouseholdIncomeLevel idx) {
      switch(idx) {
        case HouseholdIncomeLevel.CAT_I:
          return "cat_I";
        case HouseholdIncomeLevel.CAT_II:
          return "cat_II";
        case HouseholdIncomeLevel.CAT_III:
          return "cat_III";
        case HouseholdIncomeLevel.CAT_IV:
          return "cat_IV";
        case HouseholdIncomeLevel.CAT_V:
          return "cat_V";
        case HouseholdIncomeLevel.CAT_VI:
          return "cat_VI";
        case HouseholdIncomeLevel.CAT_VII:
          return "cat_VII";
        case HouseholdIncomeLevel.UNCLASSIFIED:
          return "Unclassified";
        default:
          return null;
      }
    }

    static int get_household_income_level_code_from_income(int income) {
      if(income < 0) {
        return HouseholdIncomeLevel.UNCLASSIFIED;
      } else if(income < Household::Cat_I_Max_Income) {
        return HouseholdIncomeLevel.CAT_I;
      } else if(income < Household::Cat_II_Max_Income) {
        return HouseholdIncomeLevel.CAT_II;
      } else if(income < Household::Cat_III_Max_Income) {
        return HouseholdIncomeLevel.CAT_III;
      } else if(income < Household::Cat_IV_Max_Income) {
        return HouseholdIncomeLevel.CAT_IV;
      } else if(income < Household::Cat_V_Max_Income) {
        return HouseholdIncomeLevel.CAT_V;
      } else if(income < Household::Cat_VI_Max_Income) {
        return HouseholdIncomeLevel.CAT_VI;
      } else {
        return HouseholdIncomeLevel.CAT_VII;
      }
    }

    Household() : Place()
    {
      this->set_type(Place::TYPE_HOUSEHOLD);
      this->set_subtype(Place::SUBTYPE_NONE);
      this->sheltering = false;
      this->hh_schl_aged_chld_unemplyd_adlt_chng = false;
      this->hh_schl_aged_chld = false;
      this->hh_schl_aged_chld_unemplyd_adlt = false;
      this->hh_sympt_child = false;
      this->hh_working_adult_using_sick_leave = false;
      this->seeking_healthcare = false;
      this->primary_healthcare_available = true;
      this->other_healthcare_location_that_accepts_insurance_available = true;
      this->healthcare_available = true;
      this->count_seeking_hc = 0;
      this->count_primary_hc_unav = 0;
      this->count_hc_accept_ins_unav = 0;
      this->shelter_start_day = 0;
      this->shelter_end_day = 0;
      this->deme_id = ' ';
      this->group_quarters_units = 0;
      this->group_quarters_workplace = NULL;
      this->income_quartile = -1;
      this->household_income = -1;
      this->household_income_code = Household_income_level_code::UNCLASSIFIED;
    }

    Household::Household(const char* lab, char _subtype, fred::geo lon, fred::geo lat) : Place(lab, lon, lat)
    {
      this->set_type(Place::TYPE_HOUSEHOLD);
      this->set_subtype(_subtype);
      this->sheltering = false;
      this->hh_schl_aged_chld_unemplyd_adlt_chng = false;
      this->hh_schl_aged_chld = false;
      this->hh_schl_aged_chld_unemplyd_adlt = false;
      this->hh_sympt_child = false;
      this->hh_working_adult_using_sick_leave = false;
      this->seeking_healthcare = false;
      this->primary_healthcare_available = true;
      this->other_healthcare_location_that_accepts_insurance_available = true;
      this->healthcare_available = true;
      this->count_seeking_hc = 0;
      this->count_primary_hc_unav = 0;
      this->count_hc_accept_ins_unav = 0;
      this->shelter_start_day = 0;
      this->shelter_end_day = 0;
      this->deme_id = ' ';
      this->intimacy = 1.0;
      this->group_quarters_units = 0;
      this->group_quarters_workplace = NULL;
      this->income_quartile = -1;
      this->household_income = -1;
      this->household_income_code = Household_income_level_code::UNCLASSIFIED;
    }

    void Household::get_parameters()
    {

      Params::get_param_from_string("household_contacts", &Household::contacts_per_day);
      Params::get_param_from_string("neighborhood_same_age_bias", &Household::same_age_bias);
      Household::same_age_bias *= 0.5;
      int n = Params::get_param_matrix((char*)"household_trans_per_contact", &Household::prob_transmission_per_contact);
      if (Global::Verbose > 1)
      {
        printf("\nHousehold contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", Household::prob_transmission_per_contact[i][j]);
          }
          printf("\n");
        }
      }

      //Get the Household Income Cutoffs
      Params::get_param_from_string("cat_I_max_income", &Household::Cat_I_Max_Income);
      Params::get_param_from_string("cat_II_max_income", &Household::Cat_II_Max_Income);
      Params::get_param_from_string("cat_III_max_income", &Household::Cat_III_Max_Income);
      Params::get_param_from_string("cat_IV_max_income", &Household::Cat_IV_Max_Income);
      Params::get_param_from_string("cat_V_max_income", &Household::Cat_V_Max_Income);
      Params::get_param_from_string("cat_VI_max_income", &Household::Cat_VI_Max_Income);
    }

    int Household::get_group(int disease, Person* per)
    {
      int age = per->get_age();
      if (age < Global::ADULT_AGE)
      {
        return 0;
      }
      else
      {
        return 1;
      }
    }

    double Household::get_transmission_probability(int disease, Person* i, Person* s)
    {
      double age_i = i->get_real_age();
      double age_s = s->get_real_age();
      double diff = fabs(age_i - age_s);
      double prob = exp(-Household::same_age_bias * diff);
      return prob;
    }

    double Household::get_transmission_prob(int disease, Person* i, Person* s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = Household::prob_transmission_per_contact[row][col];
      return tr_pr;
    }

    double Household::get_contacts_per_day(int disease)
    {
      return Household::contacts_per_day;
    }

    void Household::set_household_has_hospitalized_member(bool does_have)
    {
      this->hh_schl_aged_chld_unemplyd_adlt_chng = true;
      if (does_have)
      {
        this->not_home_bitset[Household_extended_absence_index::HAS_HOSPITALIZED] = true;
      }
      else
      {
        //Initially say no one is hospitalized
        this->not_home_bitset[Household_extended_absence_index::HAS_HOSPITALIZED] = false;
        //iterate over all housemates  to see if anyone is still hospitalized
        vector<Person*>::iterator it;

        for (it = this->enrollees.begin(); it != this->enrollees.end(); ++it)
        {
          if ((*it)->is_hospitalized())
          {
            this->not_home_bitset[Household_extended_absence_index::HAS_HOSPITALIZED] = true;
            if (this->get_household_visitation_hospital() == NULL)
            {
              this->set_household_visitation_hospital(static_cast<Hospital*>((*it)->get_activities()->get_hospital()));
            }
            break;
          }
        }
      }
    }

    bool Household::has_school_aged_child()
    {
      //Household has been loaded
      assert(Global::Pop.is_load_completed());
      //If the household status hasn't changed, just return the flag
      if (!this->hh_schl_aged_chld_unemplyd_adlt_chng)
      {
        return this->hh_schl_aged_chld;
      }
      else
      {
        bool ret_val = false;
        for (int i = 0; i < static_cast<int>(this->enrollees.size()); ++i)
        {
          Person* per = this->enrollees[i];
          if (per->is_student() && per->is_child())
          {
            ret_val = true;
            break;
          }
        }
        this->hh_schl_aged_chld = ret_val;
        this->hh_schl_aged_chld_unemplyd_adlt_chng = false;
        return ret_val;
      }
    }

    bool Household::has_school_aged_child_and_unemployed_adult()
    {
      //Household has been loaded
      assert(Global::Pop.is_load_completed());
      //If the household status hasn't changed, just return the flag
      if (!this->hh_schl_aged_chld_unemplyd_adlt_chng)
      {
        return this->hh_schl_aged_chld_unemplyd_adlt;
      }
      else
      {
        bool ret_val = false;
        if (has_school_aged_child())
        {
          for (int i = 0; i < static_cast<int>(this->enrollees.size()); ++i)
          {
            Person* per = this->enrollees[i];
            if (per->is_child())
            {
              continue;
            }

            //Person is an adult, but is also a student
            if (per->is_adult() && per->is_student())
            {
              continue;
            }

            //Person is an adult, but isn't at home
            if (per->is_adult() &&
               (per->is_hospitalized() ||
                per->is_college_dorm_resident() ||
                per->is_military_base_resident() ||
                per->is_nursing_home_resident() ||
                per->is_prisoner()))
            {
              continue;
            }

            //Person is an adult AND is either retired or unemployed
            if (per->is_adult() &&
               (per->get_activities()->get_profile() == RETIRED_PROFILE ||
                per->get_activities()->get_profile() == UNEMPLOYED_PROFILE))
            {
              ret_val = true;
              break;
            }
          }
        }
        this->hh_schl_aged_chld_unemplyd_adlt = ret_val;
        this->hh_schl_aged_chld_unemplyd_adlt_chng = false;
        return ret_val;
      }
    }

    void Household::prepare_person_childcare_sickleave_map()
    {
      //Household has been loaded
      assert(Global::Pop.is_load_completed());

      if (Global::Report_Childhood_Presenteeism)
      {
        if (has_school_aged_child() && !has_school_aged_child_and_unemployed_adult())
        {
          for (int i = 0; i < static_cast<int>(this->enrollees.size()); ++i)
          {
            Person* per = this->enrollees[i];
            if (per->is_child())
            {
              continue;
            }

            //Person is an adult, but is also a student
            if (per->is_adult() && per->is_student())
            {
              continue;
            }

            //Person is an adult, but isn't at home
            if (per->is_adult() &&
               (per->is_hospitalized() ||
                per->is_college_dorm_resident() ||
                per->is_military_base_resident() ||
                per->is_nursing_home_resident() ||
                per->is_prisoner()))
            {
              continue;
            }

            //Person is an adult AND is neither retired nor unemployed
            if (per->is_adult() &&
               per->get_activities()->get_profile() != RETIRED_PROFILE &&
               per->get_activities()->get_profile() != UNEMPLOYED_PROFILE)
            {
              //Insert the adult into the sickleave info map
              HH_Adult_Sickleave_Data sickleave_info;

              //Add any school-aged children to that person's info
              for (int j = 0; j < static_cast<int>(this->enrollees.size()); ++j)
              {
                Person* child_check = this->enrollees[j];
                if (child_check->is_student() && child_check->is_child())
                {
                  sickleave_info.add_child_to_maps(child_check);
                }
              }
              std::pair<std::map<Person*, HH_Adult_Sickleave_Data>::iterator, bool> ret;
              ret = this->adult_childcare_sickleave_map.insert(
                     std::pair<Person*, HH_Adult_Sickleave_Data>(per, sickleave_info));
              assert(ret.second); // Shouldn't insert the same person twice
            }
          }
        }
      }
      else
      {
        return;
      }
    }

    bool Household::have_working_adult_use_sickleave_for_child(Person* adult, Person* child)
    {
      std::map<Person*, HH_Adult_Sickleave_Data>::iterator itr;
      itr = this->adult_childcare_sickleave_map.find(adult);
      if (itr != this->adult_childcare_sickleave_map.end())
      {
        if (!itr->second.stay_home_with_child(adult))
        { //didn't already stayed home with this child
          return itr->second.stay_home_with_child(child);
        }
      }
      return false;
    }

    void Household::record_profile()
    {
      // record the ages
      this->ages.clear();
      int size = get_size();
      for (int i = 0; i < size; ++i)
      {
        this->ages.push_back(this->enrollees[i]->get_age());
      }

      // record the id's of the original members of the household
      this->ids.clear();
      for (int i = 0; i < size; ++i)
      {
        this->ids.push_back(this->enrollees[i]->get_id());
      }
    }
  }
}
