using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Neighborhood : Place
  {
    double contacts_per_day = 0.0;
    double same_age_bias = 0.0;
    double[,] prob_transmission_per_contact;
    double weekend_contact_rate = 0.0;

    public Neighborhood(string lab, char _subtype, double lon, double lat) : base(lab, lon, lat)
    {
      this.set_type(Place::TYPE_NEIGHBORHOOD);
      this.set_subtype(_subtype);
      this.intimacy = 0.0025;
    }

    void get_parameters()
    {
      Params::get_param_from_string("neighborhood_contacts", &contacts_per_day);
      Params::get_param_from_string("neighborhood_same_age_bias", &same_age_bias);
      int n = Params::get_param_matrix((char*)"neighborhood_trans_per_contact", &prob_transmission_per_contact);
      if (Global::Verbose > 1)
      {
        printf("\nNeighborhood_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", prob_transmission_per_contact[i,j]);
          }
          printf("\n");
        }
      }
      Params::get_param_from_string("weekend_contact_rate", &weekend_contact_rate);

      if (Global::Verbose > 0)
      {
        printf("\nprob_transmission_per_contact before normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            printf("%f ", prob_transmission_per_contact[i][j]);
          }
          printf("\n");
        }
        printf("\ncontact rate: %f\n", contacts_per_day);
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (prob_transmission_per_contact[i,j] > max_prob)
          {
            max_prob = prob_transmission_per_contact[i,j];
          }
        }
      }

      // convert max contact prob to 1.0
      if (max_prob > 0)
      {
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            prob_transmission_per_contact[i,j] /= max_prob;
          }
        }
        // compensate contact rate
        contacts_per_day *= max_prob;
        // end normalization

        if (Global::Verbose > 0)
        {
          printf("\nprob_transmission_per_contact after normalization:\n");
          for (int i = 0; i < n; ++i)
          {
            for (int j = 0; j < n; ++j)
            {
              printf("%f ", prob_transmission_per_contact[i,j]);
            }
            printf("\n");
          }
          printf("\ncontact rate: %f\n", contacts_per_day);
        }
        // end normalization
      }
    }

    int get_group(int disease, Person* per)
    {
      int age = per.get_age();
      if (age < Global::ADULT_AGE)
      {
        return 0;
      }
      else
      {
        return 1;
      }
    }

    double get_transmission_probability(int disease, Person* i, Person* s)
    {
      double age_i = i.get_real_age();
      double age_s = s.get_real_age();
      double diff = fabs(age_i - age_s);
      double prob = exp(-same_age_bias * diff);
      return prob;
    }

    double get_transmission_prob(int disease, Person* i, Person* s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = prob_transmission_per_contact[row][col];
      return tr_pr;
    }

    double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }


  }
}
