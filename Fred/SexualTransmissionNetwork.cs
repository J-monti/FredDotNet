using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class SexualTransmissionNetwork : Network
  {
    private static double sexual_contacts_per_day = 0.0;
    private static double sexual_transmission_per_contact = 0.0;

    public static double get_sexual_contacts_per_day()
    {
      return sexual_contacts_per_day;
    }

    public static double get_sexual_transmission_per_contact()
    {
      return sexual_transmission_per_contact;
    }
    public SexualTransmissionNetwork(string lab)
      : base(lab)
    {
      this.set_subtype(NetworkSubType.SexualPartner);
    }

    public void get_parameters()
    {
      Params::get_param_from_string("sexual_partner_contacts", sexual_contacts_per_day);
      Params::get_param_from_string("sexual_trans_per_contact", sexual_transmission_per_contact);
    }

    public void setup()
    {

      // initialize MSM network
      for (int p = 0; p < Global::Pop.get_index_size(); ++p)
      {
        Person person = Global::Pop.get_person_by_index(p);
        if (person != NULL)
        {
          int age = person.get_age();
          char sex = person.get_sex();
          person.become_unsusceptible(0);
          if (18 <= age && age < 60 && sex == 'M')
          {
            if (FredRandom.NextDouble() < 0.01)
            {
              person.join_network(Global::Sexual_Partner_Network);
              person.become_susceptible(0);
            }
          }
        }
      }

      // create random sexual partnerships
      Global::Sexual_Partner_Network.create_random_network(2.0);
    }
  }
}
