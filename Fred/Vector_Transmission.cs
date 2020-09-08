using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vector_Transmission : Transmission
  {
    private readonly List<Person> visitors = new List<Person>();
    private readonly List<Person> susceptible_visitors = new List<Person>();

    public Vector_Transmission() { }

    public override void setup(Disease disease)
    {

    }

    public override void spread_infection(int day, int disease_id, Mixing_Group mixing_group)
    {
      var place = mixing_group as Place;
      if (place == null)
      {
        //Vector_Transmission must occur on a Place type
        return;
      }
      else
      {
        this.spread_infection(day, disease_id, place);
      }
    }

    public void spread_infection(int day, int disease_id, Place place)
    {
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

      this.visitors.Clear();
      this.susceptible_visitors.Clear();
      var tmp = place.get_enrollees();
      int tmp_size = tmp.Count;
      for (int i = 0; i < tmp_size; i++)
      {
        var person = tmp[i];
        person.update_schedule(day);
        if (person.is_present(day, place))
        {
          visitors.Add(person);
          if (person.is_susceptible(disease_id))
          {
            susceptible_visitors.Add(person);
          }
        }
      }
      Utils.FRED_VERBOSE(1, "Vector_Transmission.spread_infection entered place %s visitors %d\n", place.get_label(), visitors.Count);
      // infections of vectors by hosts
      if (place.have_vectors_been_infected_today() == false)
      {
        infect_vectors(day, place);
      }

      // transmission from vectors to hosts
      infect_hosts(day, disease_id, place);

      place.reset_place_state(disease_id);
    }

    //private bool attempt_transmission(double transmission_prob, Person infector, Person infectee, int disease_id, int day, Place place);

    private void infect_vectors(int day, Place place)
    {
      int total_hosts = this.visitors.Count;
      if (total_hosts == 0)
      {
        return;
      }

      // skip if there are no susceptible vectors
      int susceptible_vectors = place.get_susceptible_vectors();
      if (susceptible_vectors == 0)
      {
        return;
      }

      // find the percent distribution of infectious hosts
      int diseases = Global.Diseases.get_number_of_diseases();
      var infectious_hosts = new int[diseases];
      int total_infectious_hosts = 0;
      for (int disease_id = 0; disease_id < diseases; ++disease_id)
      {
        infectious_hosts[disease_id] = place.get_number_of_infectious_people(disease_id);
        total_infectious_hosts += infectious_hosts[disease_id];
      }

      Utils.FRED_VERBOSE(0, "infect_vectors on day %d in place %s susceptible_vectors %d total_inf_hosts %d\n",
             day, place.get_label(), susceptible_vectors, total_infectious_hosts);

      // return if there are no infectious hosts
      if (total_infectious_hosts == 0)
      {
        return;
      }

      Utils.FRED_VERBOSE(1, "spread_infection.infect_vectors entered susceptible_vectors %d total_inf_hosts %d total_hosts %d\n",
             susceptible_vectors, total_infectious_hosts, total_hosts);

      // the vector infection model of Chao and Longini

      // decide on total number of vectors infected by any infectious host

      // each vector's probability of infection
      double prob_infection = 1.0 - Math.Pow((1.0 - Global.Vectors.get_infection_efficiency()), (Global.Vectors.get_bite_rate() * total_infectious_hosts) / total_hosts);

      // select a number of vectors to be infected

      int total_infections = 0;
      if (susceptible_vectors > 0 && susceptible_vectors < 18)
      {
        for (int k = 0; k < susceptible_vectors; k++)
        {
          double r = FredRandom.NextDouble();
          if (r <= prob_infection)
          {
            total_infections++;
          }
        }
      }
      else
      {
        total_infections = Convert.ToInt32(prob_infection * susceptible_vectors);
      }

      Utils.FRED_VERBOSE(1, "spread_infection.infect_vectors place %s day %d prob_infection %f total infections %d\n", place.get_label(), day, prob_infection, total_infections);

      // assign strain based on distribution of infectious hosts
      int newly_infected = 0;
      for (int disease_id = 0; disease_id < diseases; ++disease_id)
      {
        int exposed_vectors = Convert.ToInt32(total_infections * (infectious_hosts[disease_id] / total_infectious_hosts));
        place.expose_vectors(disease_id, exposed_vectors);
        newly_infected += exposed_vectors;
      }
      place.mark_vectors_as_infected_today();
      if (Global.Vectors.get_vector_control_status())
      {
        Utils.FRED_VERBOSE(1, "Infect_vectors attempting to add infectious patch, day %d place %s\n", day, place.get_label());
        Global.Vectors.add_infectious_patch(place, day);
      }
      Utils.FRED_VERBOSE(1, "newly_infected_vectors %d\n", newly_infected);
    }

    private void infect_hosts(int day, int disease_id, Place place)
    {

      int total_hosts = visitors.Count;
      if (total_hosts == 0)
      {
        return;
      }

      int susceptible_hosts = susceptible_visitors.Count;
      if (susceptible_hosts == 0)
      {
        return;
      }

      int infectious_vectors = place.get_infectious_vectors(disease_id);
      if (infectious_vectors == 0)
      {
        return;
      }

      double transmission_efficiency = Global.Vectors.get_transmission_efficiency();
      if (transmission_efficiency == 0.0)
      {
        return;
      }

      double bite_rate = Global.Vectors.get_bite_rate();
      /*
      if(total_hosts <= 10){
        int actual_infections = 0;
        double number_of_bites_per_host = bite_rate * infectious_vectors / total_hosts;
        int min_number_of_bites_per_host = floor(number_of_bites_per_host);
        double remainder = number_of_bites_per_host - min_number_of_bites_per_host;
        for(int j = 0; j < visitors.Count;++j){
          Person* infectee = visitors[j];
          int max_number_of_bites_per_host = min_number_of_bites_per_host;
          if(Random.draw_random(0,1) < remainder) {
      max_number_of_bites_per_host++;
          }
          if(infectee.is_susceptible(disease_id)) {
      bool effective_bite = false;
      for(int k=0;k<max_number_of_bites_per_host && effective_bite == false;k++){
        if(Random.draw_random(0,1) < transmission_efficiency) {
          effective_bite = true;
        }
      }
      if(effective_bite){
        // create a new infection in infectee
        actual_infections++;
        FRED_VERBOSE(1,"transmitting to host %d\n", infectee.get_id());
        infectee.become_exposed(disease_id, NULL, place, day);
        Global.Diseases.get_disease(disease_id).get_epidemic().become_exposed(infectee, day);
        int diseases = Global.Diseases.get_number_of_diseases();
        if (diseases > 1) {
          // for dengue: become unsusceptible to other diseases
          for(int d = 0; d < diseases; d++) {
            if(d != disease_id) {
        Disease* other_disease = Global.Diseases.get_disease(d);
        infectee.become_unsusceptible(other_disease);
        FRED_VERBOSE(1,"host %d not susceptible to disease %d\n", infectee.get_id(),d);
            }
          }
        }
      }
          }
        }
        FRED_VERBOSE(0, "infect_hosts: place %s day %d number of bites %d number of bites %f  actual_infections %d infectious mosquitoes %d total hosts %d\n", place.get_label(),day,min_number_of_bites_per_host,number_of_bites_per_host,actual_infections,infectious_vectors,total_hosts);
      }else{
      */
      // each host's probability of infection
      double prob_infection = 1.0 - Math.Pow((1.0 - transmission_efficiency), (bite_rate * infectious_vectors / total_hosts));

      // select a number of hosts to be infected
      Utils.FRED_VERBOSE(1, "infect_hosts: place %s day %d  infectious_vectors %d prob_infection %f total_hosts %d\n", place.get_label(), day, infectious_vectors, prob_infection, total_hosts);
      //  double number_of_hosts_receiving_a_potentially_infectious_bite = susceptible_hosts * prob_infection;
      double number_of_hosts_receiving_a_potentially_infectious_bite = susceptible_hosts * prob_infection;
      int max_exposed_hosts = Convert.ToInt32(Math.Floor(number_of_hosts_receiving_a_potentially_infectious_bite));
      double remainder = number_of_hosts_receiving_a_potentially_infectious_bite - max_exposed_hosts;
      if (FredRandom.NextDouble() < remainder)
      {
        max_exposed_hosts++;
      }
      Utils.FRED_VERBOSE(1, "infect_hosts: place %s day %d  max_exposed_hosts[%d] = %d\n", place.get_label(), day, disease_id, max_exposed_hosts);
      // attempt to infect the max_exposed_hosts

      // randomize the order of processing the susceptible list
      var shuffle_index = new List<int>(total_hosts);
      for (int i = 0; i < total_hosts; ++i)
      {
        shuffle_index[i] = i;
      }
      int actual_infections = 0;
      shuffle_index.Shuffle();

      // get the disease object   
      var disease = Global.Diseases.get_disease(disease_id);
      for (int j = 0; j < max_exposed_hosts && j < susceptible_visitors.Count; ++j)
      {
        //for(int j = 0; j < max_exposed_hosts && j < visitors.Count; ++j) {
        var infectee = susceptible_visitors[shuffle_index[j]];
        //Person* infectee = visitors[shuffle_index[j]];
        Utils.FRED_VERBOSE(1, "selected host %d age %d\n", infectee.get_id(), infectee.get_age());
        // NOTE: need to check if infectee already infected
        if (infectee.is_susceptible(disease_id))
        {
          // create a new infection in infectee
          Utils.FRED_VERBOSE(1, "transmitting to host %d\n", infectee.get_id());
          infectee.become_exposed(disease_id, null, place, day);
          actual_infections++;
          Global.Diseases.get_disease(disease_id).get_epidemic().become_exposed(infectee, day);
          int diseases = Global.Diseases.get_number_of_diseases();
          if (diseases > 1)
          {
            // for dengue: become unsusceptible to other diseases
            for (int d = 0; d < diseases; d++)
            {
              if (d != disease_id)
              {
                var other_disease = Global.Diseases.get_disease(d);
                infectee.become_unsusceptible(other_disease);
                Utils.FRED_VERBOSE(1, "host %d not susceptible to disease %d\n", infectee.get_id(), d);
              }
            }
          }
        }
        else
        {
          Utils.FRED_VERBOSE(1, "host %d not susceptible\n", infectee.get_id());
        }
      }

      Utils.FRED_VERBOSE(1, "infect_hosts: place %s day %d  max_exposed_hosts[%d] = %d actual_infections %d\n", place.get_label(), day, disease_id, max_exposed_hosts, actual_infections);
    }
  }
}
