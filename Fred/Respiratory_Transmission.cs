﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Respiratory_Transmission : Transmission
  {
    private bool enable_neighborhood_density_transmission;
    private bool enable_density_transmission_maximum_infectees;
    private int density_transmission_maximum_infectees;
    private double[,] prob_contact;

    public Respiratory_Transmission()
    {
      this.enable_neighborhood_density_transmission = false;
      this.enable_density_transmission_maximum_infectees = false;
      this.density_transmission_maximum_infectees = 10;
      this.prob_contact = null;
    }

    public override void setup(Disease disease)
    {
      int temp_int = 0;
      FredParameters.GetParameter("enable_neighborhood_density_transmission", ref temp_int);
      this.enable_neighborhood_density_transmission = (temp_int == 1);
      FredParameters.GetParameter("enable_density_transmission_maximum_infectees", ref temp_int);
      this.enable_density_transmission_maximum_infectees = (temp_int == 1);
      FredParameters.GetParameter("density_transmission_maximum_infectees", ref this.density_transmission_maximum_infectees);
      /*
      Respiratory_Transmission.prob_contact = new double * [101];
      // create a PolyMod type matrix;
      for(int i = 0; i <= 100; ++i) {
        Respiratory_Transmission.prob_contact[i] = new double [101];

        for(int j = 0; j <= 100; ++j) {
          Respiratory_Transmission.prob_contact[i][j] = 0.05;
        }
      }
      for(int i = 0; i <= 100; ++i) {
        for(int j = i - 4; j <= i+4; ++j) {
          if(j < 0 || j > 100) {
      continue;
          }
          Respiratory_Transmission.prob_contact[i][j] = 1.0 - 0.2 * abs(i - j);
        }
      }
      */
    }

    public override void spread_infection(int day, int disease_id, Mixing_Group mixing_group)
    {
      var place = mixing_group as Place;
      if (place == null)
      {
        //Respiratory_Transmission must occur on a Place type
        return;
      }
      else
      {
        this.spread_infection(day, disease_id, place);
      }
    }

    public void spread_infection(int day, int disease_id, Place place)
    {
      Utils.FRED_VERBOSE(1, "spread_infection day %d disease %d place %d %s\n",
             day, disease_id, place.get_id(), place.get_label());
      // abort if transmissibility == 0 or if place is closed
      var disease = Global.Diseases.get_disease(disease_id);
      double beta = disease.get_transmissibility();
      if (beta == 0.0 || place.is_open(day) == false || place.should_be_open(day, disease_id) == false)
      {
        place.reset_place_state(disease_id);
        return;
      }

      // have place record first and last day of infectiousness
      place.record_infectious_days(day);

      // need at least one susceptible
      if (place.get_size() == 0)
      {
        place.reset_place_state(disease_id);
        return;
      }

      if (place.is_household())
      {
        pairwise_transmission_model(day, disease_id, place);
        Utils.FRED_VERBOSE(1, "spread_infection finished day %d disease %d place %d %s\n",
         day, disease_id, place.get_id(), place.get_label());
        return;
      }

      if (place.is_neighborhood() && this.enable_neighborhood_density_transmission == true)
      {
        density_transmission_model(day, disease_id, place);
      }
      else
      {
        default_transmission_model(day, disease_id, place);
      }

      /*
      if(Global.Enable_New_Transmission_Model) {
        age_based_transmission_model(day, disease_id, place);
      } else {
        default_transmission_model(day, disease_id, place);
      }
      */
      Utils.FRED_VERBOSE(1, "spread_infection finished day %d disease %d place %d %s\n",
             day, disease_id, place.get_id(), place.get_label());

      return;
    }

    private void default_transmission_model(int day, int disease_id, Place place)
    {
      var disease = Global.Diseases.get_disease(disease_id);
      int N = place.get_size();
      var infectious = place.get_infectious_people(disease_id);
      var susceptibles = place.get_enrollees();

      Utils.FRED_VERBOSE(1, "default_transmission DAY %d PLACE %s N %d susc %d inf %d\n",
                   day, place.get_label(), N, (int)susceptibles.Count, (int)infectious.Count);

      // the number of possible infectees per infector is max of (N-1) and S[s]
      // where N is the capacity of this place and S[s] is the number of current susceptibles
      // visiting this place. S[s] might exceed N if we have some ad hoc visitors,
      // since N is estimated only at startup.
      int number_targets = (N - 1 > susceptibles.Count ? N - 1 : susceptibles.Count);

      // contact_rate is contacts_per_day with weeked and seasonality modulation (if applicable)
      double contact_rate = place.get_contact_rate(day, disease_id);

      // randomize the order of processing the infectious list
      int number_of_infectious = infectious.Count;
      var shuffle_index = new List<int>(number_of_infectious);
      for (int i = 0; i < number_of_infectious; ++i)
      {
        shuffle_index[i] = i;
      }

      shuffle_index.Shuffle();

      for (int n = 0; n < number_of_infectious; ++n)
      {
        int infector_pos = shuffle_index[n];
        // infectious visitor
        var infector = infectious[infector_pos];
        // printf("infector id %d\n", infector.get_id());
        if (infector.is_infectious(disease_id) == false)
        {
          // printf("infector id %d not infectious!\n", infector.get_id());
          continue;
        }

        // get the actual number of contacts to attempt to infect
        int contact_count = place.get_contact_count(infector, disease_id, day, contact_rate);

        Dictionary<int, int> sampling_map = new Dictionary<int, int>();
        // get a susceptible target for each contact resulting in infection
        for (int c = 0; c < contact_count; ++c)
        {
          // select a target infectee from among susceptibles with replacement
          int pos = FredRandom.Next(0, number_targets - 1);
          if (pos < susceptibles.Count)
          {
            if (infector == susceptibles[pos])
            {
              if (susceptibles.Count > 1)
              {
                --(c); // redo
                continue;
              }
              else
              {
                break; // give up
              }
            }

            if (!sampling_map.ContainsKey(pos))
            {
              sampling_map.Add(pos, 1);
            }
            else
            {
              sampling_map[pos]++;
            }
          }
        }

        foreach(var kvp in sampling_map)
        {
          int pos = kvp.Key;
          int times_drawn = kvp.Value;
          var infectee = susceptibles[pos];
          Utils.assert(infector != infectee);
          infectee.update_schedule(day);
          if (!infectee.is_present(day, place))
          {
            continue;
          }
          // get the transmission probs for given infector/infectee pair
          double transmission_prob = 1.0;
          if (Global.Enable_Transmission_Bias)
          {
            transmission_prob = place.get_transmission_probability(disease_id, infector, infectee);
          }
          else
          {
            transmission_prob = place.get_transmission_prob(disease_id, infector, infectee);
          }
          for (int draw = 0; draw < times_drawn; ++draw)
          {
            // only proceed if person is susceptible
            if (infectee.is_susceptible(disease_id))
            {
              attempt_transmission(transmission_prob, infector, infectee, disease_id, day, place);
            }
          }
        } // end contact loop
      } // end infectious list loop
      place.reset_place_state(disease_id);
    }

    //private void age_based_transmission_model(int day, int disease_id, Place place);

    private void pairwise_transmission_model(int day, int disease_id, Place place)
    {
      var infectious = place.get_infectious_people(disease_id);
      var susceptibles = place.get_enrollees();
      double contact_prob = place.get_contact_rate(day, disease_id);
      Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s N %d\n",
             day, place.get_label(), place.get_size());

      for (int infector_pos = 0; infector_pos < infectious.Count; ++infector_pos)
      {
        var infector = infectious[infector_pos];      // infectious individual
                                                             // FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infector %d is %d\n", day, place.get_label(), infector_pos, infector.get_id());

        if (infector.is_infectious(disease_id) == false)
        {
          Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infector %d is not infectious!\n",
           day, place.get_label(), infector.get_id());
          continue;
        }

        int sus_size = susceptibles.Count;
        for (int pos = 0; pos < sus_size; ++pos)
        {
          var infectee = susceptibles[pos];
          if (infector == infectee)
          {
            continue;
          }
          int infectee_id = infectee.get_id();
          var label = place.get_label();

          Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee is %d\n", day, label, infectee_id);
          if (infectee.is_infectious(disease_id) == false)
          {
            Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee %d is not infectious -- updating schedule\n",
                         day, label, infectee_id);
            infectee.update_schedule(day);
          }
          else
          {
            Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee %d is infectious\n",
                         day, label, infectee_id);
          }
          if (!infectee.is_present(day, place))
          {
            Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee %d is not present today\n",
                         day, label, infectee_id);
            continue;
          }
          // only proceed if person is susceptible
          if (infectee.is_susceptible(disease_id))
          {
            Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee %d is present and susceptible\n",
                         day, label, infectee_id);
            // get the transmission probs for infector/infectee pair
            double transmission_prob = 1.0;
            if (Global.Enable_Transmission_Bias)
            {
              transmission_prob = place.get_transmission_probability(disease_id, infector, infectee);
            }
            else
            {
              transmission_prob = place.get_transmission_prob(disease_id, infector, infectee);
            }
            double infectivity = infector.get_infectivity(disease_id, day);
            // scale transmission prob by infectivity and contact prob
            transmission_prob *= infectivity * contact_prob;
            attempt_transmission(transmission_prob, infector, infectee, disease_id, day, place);
          }
          else
          {
            Utils.FRED_VERBOSE(1, "pairwise_transmission DAY %d PLACE %s infectee %d is not susceptible\n",
                         day, label, infectee_id);
          }
        } // end susceptibles loop
      }
      place.reset_place_state(disease_id);
    }

    private void density_transmission_model(int day, int disease_id, Place place)
    {
      var infectious = place.get_infectious_people(disease_id);
      var susceptibles = place.get_enrollees();
      var disease = Global.Diseases.get_disease(disease_id);
      int N = place.get_size();

      // printf("DAY %d PLACE %s N %d susc %d inf %d\n",
      // day, place.get_label(), N, (int) susceptibles.Count, (int) infectious.Count);
      double contact_prob = place.get_contact_rate(day, disease_id);
      int sus_hosts = susceptibles.Count;
      int inf_hosts = infectious.Count;
      int exposed = 0;

      // each host's probability of infection
      double prob_infection = 1.0 - Math.Pow((1.0 - contact_prob), inf_hosts);

      // select a number of hosts to be infected
      double expected_infections = sus_hosts * prob_infection;
      exposed = Convert.ToInt32(Math.Floor(expected_infections));
      double remainder = expected_infections - exposed;
      if (FredRandom.NextDouble() < remainder)
      {
        exposed++;
      }

      var infectee_count = new int [inf_hosts];
      for (int i = 0; i < inf_hosts; ++i)
      {
        infectee_count[i] = 0;
      }

      int reached_max_infectees_count = 0;
      int number_infectious_hosts = inf_hosts;

      // randomize the order of processing the susceptible list
      int number_of_susceptibles = susceptibles.Count;
      var shuffle_index = new List<int>(number_of_susceptibles);
      for (int i = 0; i < number_of_susceptibles; ++i)
      {
        shuffle_index[i] = i;
      }
      shuffle_index.Shuffle();

      for (int j = 0; j < exposed && j < sus_hosts && 0 < inf_hosts; ++j)
      {
        var infectee = susceptibles[shuffle_index[j]];
        infectee.update_schedule(day);
        if (!infectee.is_present(day, place))
        {
          continue;
        }
        Utils.FRED_VERBOSE(1, "selected host %d age %d\n", infectee.get_id(), infectee.get_age());

        // only proceed if person is susceptible
        if (infectee.is_susceptible(disease_id))
        {
          // select a random infector
          int infector_pos = FredRandom.Next(0, inf_hosts - 1);
          var infector = infectious[infector_pos];
          Utils.assert(infector.get_health().is_infectious(disease_id));

          // get the transmission probs for  infectee/infector  pair
          double transmission_prob = infector.get_infectivity(disease_id, day);
          if (attempt_transmission(transmission_prob, infector, infectee, disease_id, day, place))
          {
            // successful transmission
            infectee_count[infector_pos]++;
            // if the infector has reached the max allowed, remove from infector list
            if (this.enable_density_transmission_maximum_infectees &&
               this.density_transmission_maximum_infectees <= infectee_count[infector_pos])
            {
              // effectively remove the infector from infector list
              infectious[infector_pos] = infectious[inf_hosts - 1];
              int tmp = infectee_count[infector_pos];
              infectee_count[infector_pos] = infectee_count[inf_hosts - 1];
              infectee_count[inf_hosts - 1] = tmp;
              inf_hosts--;
              reached_max_infectees_count++;
            }
          }
        }
        else
        {
          Utils.FRED_VERBOSE(1, "host %d not susceptible\n", infectee.get_id());
        }
      }
      if (reached_max_infectees_count != 0)
      {
        Utils.FRED_VERBOSE(1, "day %d DENSITY TRANSMISSION place %s: %d with %d infectees out of %d infectious hosts\n",
                    day, place.get_label(), reached_max_infectees_count,
                    this.density_transmission_maximum_infectees, number_infectious_hosts);
      }
      place.reset_place_state(disease_id);
    }

    private bool attempt_transmission(double transmission_prob, Person infector, Person infectee, int disease_id, int day, Place place)
    {
      Utils.assert(infectee.is_susceptible(disease_id));
      Utils.FRED_STATUS(1, "infector %d -- infectee %d is susceptible\n", infector.get_id(), infectee.get_id());

      double susceptibility = infectee.get_susceptibility(disease_id);

      // reduce transmission probability due to infector's hygiene (face masks or hand washing)
      transmission_prob *= infector.get_transmission_modifier_due_to_hygiene(disease_id);

      // reduce susceptibility due to infectee's hygiene (face masks or hand washing)
      susceptibility *= infectee.get_susceptibility_modifier_due_to_hygiene(disease_id);

      if (Global.Enable_hh_income_based_susc_mod)
      {
        int hh_income = Household.get_max_hh_income(); //Default to max (no modifier)
        var hh = (Household)(infectee.get_household());
        if (hh != null)
        {
          hh_income = hh.get_household_income();
        }
        susceptibility *= infectee.get_health().get_susceptibility_modifier_due_to_household_income(hh_income);
        //Utils.fred_log("SUSC Modifier [%.4f] for HH Income [%i] modified suscepitibility to [%.4f]\n", hh_income, infectee.get_health().get_susceptibility_modifier_due_to_household_income(hh_income), susceptibility);
      }

      Utils.FRED_VERBOSE(2, "susceptibility = %f\n", susceptibility);

      // reduce transmissibility due to seasonality
      if (Seasonal_Reduction > 0.0)
      {
        int day_of_year = Date.get_day_of_year(day);
        transmission_prob *= Seasonality_multiplier[day_of_year];
      }

      double r = FredRandom.NextDouble();
      double infection_prob = transmission_prob * susceptibility;

      if (r < infection_prob)
      {
        // successful transmission; create a new infection in infectee
        infector.infect(infectee, disease_id, place, day);

        Utils.FRED_VERBOSE(1, "transmission succeeded: r = %f  prob = %f\n", r, infection_prob);
        Utils.FRED_CONDITIONAL_VERBOSE(1, infector.get_exposure_date(disease_id) == 0,
               "SEED infection day %i from %d to %d\n", day, infector.get_id(), infectee.get_id());
        Utils.FRED_CONDITIONAL_VERBOSE(1, infector.get_exposure_date(disease_id) != 0,
               "infection day %i of disease %i from %d to %d\n", day, disease_id, infector.get_id(),
               infectee.get_id());
        Utils.FRED_CONDITIONAL_VERBOSE(0, infection_prob > 1, "infection_prob exceeded unity!\n");

        // notify the epidemic
        Global.Diseases.get_disease(disease_id).get_epidemic().become_exposed(infectee, day);
        return true;
      }
      else
      {
        Utils.FRED_VERBOSE(1, "transmission failed: r = %f  prob = %f\n", r, infection_prob);
        return false;
      }
    }
  }
}
