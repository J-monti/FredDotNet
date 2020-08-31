using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Network : Mixing_Group
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;

    public const char TYPE_NETWORK = 'n';
    public const char SUBTYPE_TRANSMISSION = 't';
    public const char SUBTYPE_SEXUAL_PARTNER = 's';

    public Network(string label)
      : base (label)
    {
      this.set_type(Network.TYPE_NETWORK);
      this.set_subtype(SUBTYPE_NONE);
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("network_contacts", ref contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("network_trans_per_contact");
      int n = prob_transmission_per_contact.Length;
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nNetwork_contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("{0} ", prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
      }

      // normalize contact parameters
      // find max contact prob
      double max_prob = 0.0;
      for (int i = 0; i < n; ++i)
      {
        for (int j = 0; j < n; ++j)
        {
          if (prob_transmission_per_contact[i, j] > max_prob)
          {
            max_prob = prob_transmission_per_contact[i, j];
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
            prob_transmission_per_contact[i, j] /= max_prob;
          }
        }
        // compensate contact rate
        contacts_per_day *= max_prob;
      }

      if (Global.Verbose > 0)
      {
        Console.WriteLine("\nNetwork_contact_prob after normalization:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++i)
          {
            Console.WriteLine("{0} ", Network.prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine();
        }
        Console.WriteLine("\ncontact rate: {0}", Network.contacts_per_day);
      }
      // end normalization
    }

    /**
     * Get the transmission probability for a given disease between two Person objects.
     *
     * @see Mixing_Group.get_transmission_probability(int disease_id, Person* i, Person* s)
     */
    public override double get_transmission_probability(int disease_id, Person i, Person s)
    {
      return 1.0;
    }

    /**
     * @see Mixing_Group.get_transmission_prob(int disease_id, Person* i, Person* s)
     *
     * This method returns the value from the static array <code>Household.Household_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Household_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>household_prob[]</code>.
     */
    public override double get_transmission_prob(int disease_id, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease_id, i);
      int col = get_group(disease_id, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
    }

    public override double get_contacts_per_day(int disease_id)
    {
      return contacts_per_day;
    }

    public override double get_contact_rate(int day, int disease_id)
    {
      var disease = Global.Diseases.get_disease(disease_id);
      // expected number of susceptible contacts for each infectious person
      double contacts = get_contacts_per_day(disease_id) * disease.get_transmissibility();

      return contacts;
    }

    public override int get_contact_count(Person infector, int disease_id, int sim_day, double contact_rate)
    {
      // reduce number of infective contacts by infector's infectivity
      double infectivity = infector.get_infectivity(disease_id, sim_day);
      double infector_contacts = contact_rate * infectivity;

      Utils.FRED_VERBOSE(1, "infectivity = {0}, so ", infectivity);
      Utils.FRED_VERBOSE(1, "infector's effective contacts = {0}", infector_contacts);

      // randomly round off the expected value of the contact counts
      int contact_count = Convert.ToInt32(infector_contacts);
      double r = FredRandom.NextDouble();
      if (r < infector_contacts - contact_count)
      {
        contact_count++;
      }

      Utils.FRED_VERBOSE(1, "infector contact_count = {0}  r = {1}", contact_count, r);

      return contact_count;
    }

    public override int get_group(int disease, Person per)
    {
      return 0;
    }

    public void print()
    {
      var link_fileptr = new StreamWriter($"{Global.Simulation_directory}/{get_label()}.txt");
      var people_fileptr = new StreamWriter($"{Global.Simulation_directory}/{get_label()}-people.txt");
      int size = this.get_size();
      for (int i = 0; i < size; ++i)
      {
        var person = this.get_enrollee(i);
        person.print_network(link_fileptr, this);
        person.print(people_fileptr, 0);
      }
      link_fileptr.Flush();
      link_fileptr.Dispose();
      people_fileptr.Flush();
      people_fileptr.Dispose();

      if (get_label() != "Transmission_Network")
      {
        return;
      }

      var a = new int [20, 20];
      int total_out = 0;
      int max = -1;
      for (int i = 0; i < size; ++i)
      {
        var person = this.get_enrollee(i);
        int age_src = person.get_age();
        if (age_src > 99)
        {
          age_src = 99;
        }
        int out_degree = person.get_out_degree(this);
        for (int j = 0; j < out_degree; j++)
        {
          int age_dest = person.get_end_of_link(j, this).get_age();
          if (age_dest > 99)
          {
            age_dest = 99;
          }
          a[age_src / 5, age_dest / 5]++;
          if (max < a[age_src / 5, age_dest / 5])
          {
            max = a[age_src / 5, age_dest / 5];
          }
        }
        total_out += out_degree;
      }

      var fileptr = new StreamWriter($"{Global.Simulation_directory}/source.dat");
      for (int i = 0; i < 20; i++)
      {
        for (int j = 0; j < 20; j++)
        {
          double x = max != 0 ? (255.0 * a[i, j] / max) : 0.0;
          fileptr.WriteLine("{0} {1} {2}", i, j, x);
        }
        fileptr.WriteLine();
      }
      fileptr.Flush();
      fileptr.Dispose();
    }

    public void print_stdout()
    {
      int size = this.get_size();
      for (int i = 0; i < size; ++i)
      {
        var person = this.get_enrollee(i);
        person.print_network(Console.Out, this);
      }
      Console.WriteLine("mean degree = {0}", get_mean_degree());
      Console.WriteLine("=======================\n");
    }

    public bool is_connected_to(Person p1, Person p2)
    {
      return p1.is_connected_to(p2, this);
    }

    public bool is_connected_from(Person p1, Person p2)
    {
      return p1.is_connected_from(p2, this);
    }

    public double get_mean_degree()
    {
      int size = get_size();
      int total_out = 0;
      for (int i = 0; i < size; i++)
      {
        var person = get_enrollee(i);
        int out_degree = person.get_out_degree(this);
        total_out += out_degree;
      }

      double mean = 0.0;
      if (size != 0)
      {
        mean = total_out / size;
      }

      return mean;
    }

    public void test()
    {
      return;
      /*
      for (int i = 0; i < 50; ++i)
      {
        var p = Global.Pop.select_random_person();
        p.join_network(Global.Transmission_Network);
      }

      Console.WriteLine("create_random_network(1.0)\n");
      create_random_network(1.0);
      print_stdout();

      Console.WriteLine("create_random_network(4.0)\n");
      create_random_network(4.0);
      print_stdout();

      var p1 = Global.Pop.select_random_person();
      Console.WriteLine("p1 {0}\n", p1.get_id());
      p1.join_network(Global.Transmission_Network);

      var p2 = Global.Pop.select_random_person();
      Console.WriteLine("p2 {0}\n", p2.get_id());
      p1.join_network(Global.Transmission_Network);

      var p3 = Global.Pop.select_random_person();
      Console.WriteLine("p3 {0}\n", p3.get_id());
      p1.join_network(Global.Transmission_Network);

      Console.WriteLine("empty net:\n");
      print_stdout();

      Console.WriteLine("p1.create_network_link_to(p2, this);\np2.create_network_link_to(p3, this);\n");
      p1.create_network_link_to(p2, this);
      p2.create_network_link_to(p3, this);
      print_stdout();

      Console.WriteLine("p1.create_network_link_from(p3, this);\n");
      p1.create_network_link_from(p3, this);
      print_stdout();

      Console.WriteLine("p2.destroy_network_link_from(p1, this);\n");
      p2.destroy_network_link_from(p1, this);
      print_stdout();

      Console.WriteLine("p2.create_network_link_to(p1, this);\n");
      p2.create_network_link_to(p1, this);
      print_stdout();
      */
    }

    public void create_random_network(double mean_degree)
    {
      int size = this.get_size();
      if (size < 2)
      {
        return;
      }
      for (int i = 0; i < size; ++i)
      {
        var person = get_enrollee(i);
        person.clear_network(this);
      }
      // print_stdout();
      int number_edges = Convert.ToInt32(mean_degree * size + 0.5);
      Console.WriteLine("size = {0}  edges = {1}\n", size, number_edges);
      int j = 0;
      while (j < number_edges)
      {
        int pos1 = FredRandom.Next(0, size - 1);
        var src = this.get_enrollee(pos1);
        int pos2 = pos1;
        while (pos2 == pos1)
        {
          pos2 = FredRandom.Next(0, size - 1);
        }
        var dest = this.get_enrollee(pos2);
        // printf("edge from %d to %d\n", src.get_id(), dest.get_id());
        if (src.is_connected_to(dest, this) == false)
        {
          src.create_network_link_to(dest, this);
          ++j;
        }
      }
    }

    public void infect_random_nodes(double pct, Disease disease) { }
  }
}
