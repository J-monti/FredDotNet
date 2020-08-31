using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public class Person_Network_Link
  {
    private Person myself;
    private Network network;
    private int enrollee_index;
    private readonly List<Person> links_to = new List<Person>();
    private readonly List<Person> links_from = new List<Person>();

    public Person_Network_Link(Person person, Network network)
    {
      this.myself = person;
      this.network = null;
      this.enrollee_index = -1;
      this.enroll(this.myself, network);
    }

    public void enroll(Person person, Network new_network)
    {
      if (this.network != null)
      {
        Utils.FRED_VERBOSE(0, "enroll failed: network %s  enrollee_index %d \n",
          this.network.get_label(), enrollee_index);
      }
      Utils.assert(this.network == null);
      Utils.assert(this.enrollee_index == -1);
      this.network = new_network;
      this.enrollee_index = this.network.enroll(person);
      Utils.assert(this.enrollee_index != -1);
    }

    public void unenroll(Person person)
    {
      Utils.assert(this.enrollee_index != -1);
      Utils.assert(this.network != null);
      this.network.unenroll(this.enrollee_index);
      this.enrollee_index = -1;
      this.network = null;
    }

    public void remove_from_network()
    {
      // remove links to other people
      int size = this.links_to.Count;
      for (int i = 0; i < size; ++i)
      {
        this.links_to[i].delete_network_link_from(this.myself, this.network);
      }

      // remove links from other people
      size = this.links_from.Count;
      for (int i = 0; i < size; ++i)
      {
        this.links_from[i].delete_network_link_to(this.myself, this.network);
      }

      // unenroll in this network
      this.unenroll(this.myself);
    }

    public void create_link_from(Person person)
    {
      add_link_from(person);
      person.add_network_link_to(this.myself, this.network);
    }

    public void create_link_to(Person person)
    {
      add_link_to(person);
      person.add_network_link_from(this.myself, this.network);
    }

    public void destroy_link_from(Person person)
    {
      delete_link_from(person);
      person.delete_network_link_to(this.myself, this.network);
    }

    public void destroy_link_to(Person person)
    {
      delete_link_to(person);
      person.delete_network_link_from(this.myself, this.network);
    }

    public void add_link_to(Person person)
    {
      int size = this.links_to.Count;
      for (int i = 0; i < size; ++i)
      {
        if (person == this.links_to[i])
        {
          return;
        }
      }

      // add person to my links_to list.
      this.links_to.Add(person);
    }

    public void add_link_from(Person person)
    {
      int size = this.links_from.Count;
      for (int i = 0; i < size; ++i)
      {
        if (person == this.links_from[i])
        {
          return;
        }
      }

      // add person to my links_from list.
      this.links_from.Add(person);
    }

    public void delete_link_to(Person person)
    {
      // delete person from my links_to list.
      int size = this.links_to.Count;
      for (int i = 0; i < size; ++i)
      {
        if (person == this.links_to[i])
        {
          this.links_to.RemoveAt(i);
          break;
        }
      }
    }

    public void delete_link_from(Person person)
    {
      // delete person from my links_from list.
      int size = this.links_from.Count;
      for (int i = 0; i < size; i++)
      {
        if (person == this.links_from[i])
        {
          this.links_from.RemoveAt(i);
          break;
        }
      }
    }

    public void print(TextWriter fp)
    {
      fp.Write("{0} .", this.myself.get_id());
      int size = this.links_to.Count;
      for (int i = 0; i < size; ++i)
      {
        fp.Write(" {0}", this.links_to[i].get_id());
      }
      fp.WriteLine();
      fp.Flush();
      //size = links_from.size();
      //for (int i = 0; i < size; ++i)
      //{
      //  fprintf(fp, "%d ", this.links_from[i].get_id());
      //}
      //fprintf(fp, ". %d\n\n", this.myself.get_id());
    }

    public Network get_network()
    {
      return this.network;
    }
    public bool is_connected_to(Person person)
    {
      int size = this.links_to.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.links_to[i] == person)
        {
          return true;
        }
      }
      return false;
    }

    public bool is_connected_from(Person person)
    {
      int size = this.links_from.Count;
      for (int i = 0; i < size; ++i)
      {
        if (this.links_from[i] == person)
        {
          return true;
        }
      }
      return false;
    }

    public int get_out_degree()
    {
      return this.links_to.Count;
    }
    public int get_in_degree()
    {
      return this.links_from.Count;
    }
    public void clear()
    {
      this.links_to.Clear();
      this.links_from.Clear();
    }
    public Person get_end_of_link(int n)
    {
      return this.links_to[n];
    }
  }
}
