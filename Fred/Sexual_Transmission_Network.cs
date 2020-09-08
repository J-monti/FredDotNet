namespace Fred
{
  public class Sexual_Transmission_Network : Network
  {
    private static double sexual_contacts_per_day;
    private static double sexual_transmission_per_contact;

    public Sexual_Transmission_Network(string label)
      : base(label)
    {
      this.set_subtype(Network.SUBTYPE_SEXUAL_PARTNER);
    }

    public static new void get_parameters()
    {
      FredParameters.GetParameter("sexual_partner_contacts", ref sexual_contacts_per_day);
      FredParameters.GetParameter("sexual_trans_per_contact", ref sexual_transmission_per_contact);
    }

    public static double get_sexual_contacts_per_day()
    {
      return sexual_contacts_per_day;
    }

    public static double get_sexual_transmission_per_contact()
    {
      return sexual_transmission_per_contact;
    }

    public void setup()
    {
      // initialize MSM network
      for (int p = 0; p < Global.Pop.get_index_size(); ++p)
      {
        var person = Global.Pop.get_person_by_index(p);
        if (person != null)
        {
          int age = person.get_age();
          char sex = person.get_sex();
          person.become_unsusceptible(0);
          if (18 <= age && age < 60 && sex == 'M')
          {
            if (FredRandom.NextDouble() < 0.01)
            {
              person.join_network(Global.Sexual_Partner_Network);
              person.become_susceptible(0);
            }
          }
        }
      }

      // create random sexual partnerships
      Global.Sexual_Partner_Network.create_random_network(2.0);
    }
  }
}
