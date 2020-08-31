using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public abstract class Mixing_Group
  {
    protected int N_orig;             // orig number of enrollees

    // lists of people
    protected List<Person>[] infectious_people;

    // epidemic counters
    protected int[] new_infections;        // new infections today
    protected int[] current_infections;        // current active infections today
    protected int[] new_symptomatic_infections;     // new sympt infections today
    protected int[] current_symptomatic_infections; // current active sympt infections
    protected int[] total_infections;         // total infections over all time
    protected int[] total_symptomatic_infections; // total sympt infections over all time
    protected int[] current_case_fatalities;
    protected int[] total_case_fatalities;

    // first and last days when visited by infectious people
    protected int first_day_infectious;
    protected int last_day_infectious;

    // lists of people
    protected List<Person> enrollees;

    // track whether or not place is infectious with each disease
    protected BitArray infectious_bitset;
    protected BitArray human_infectious_bitset;
    protected BitArray recovered_bitset;
    protected BitArray exposed_bitset;

    private int id; // id
    private string label; // external id
    private char type; // HOME, WORK, SCHOOL, COMMUNITY, etc;
    private char subtype;
    private int last_update;

    public static char TYPE_UNSET = 'U';
    public static char SUBTYPE_NONE = 'X';

    public Mixing_Group(string _label)
    {

      this.id = -1;
      this.type = Mixing_Group.TYPE_UNSET;
      this.subtype = Mixing_Group.SUBTYPE_NONE;
      this.label = _label;
      this.first_day_infectious = -1;
      this.last_day_infectious = -2;

      this.N_orig = 0;             // orig number of enrollees

      // lists of people
      this.enrollees = new List<Person>();

      // track whether or not place is infectious with each disease
      this.infectious_bitset = new BitArray(Global.MAX_NUM_DISEASES);
      this.human_infectious_bitset = new BitArray(Global.MAX_NUM_DISEASES);
      this.recovered_bitset = new BitArray(Global.MAX_NUM_DISEASES);
      this.exposed_bitset = new BitArray(Global.MAX_NUM_DISEASES);

      int diseases = Global.Diseases.get_number_of_diseases();
      this.infectious_people = new List<Person>[diseases];

      // epidemic counters
      this.new_infections = new int[diseases];
      this.current_infections = new int[diseases];
      this.total_infections = new int[diseases];
      this.new_symptomatic_infections = new int[diseases];
      this.current_symptomatic_infections = new int[diseases];
      this.total_symptomatic_infections = new int[diseases];
      this.current_case_fatalities = new int[diseases];
      this.total_case_fatalities = new int[diseases];

      // zero out all disease-specific counts
      this.last_update = 0;
      for (int d = 0; d < diseases; ++d)
      {
        this.new_infections[d] = 0;
        this.current_infections[d] = 0;
        this.total_infections[d] = 0;
        this.new_symptomatic_infections[d] = 0;
        this.total_symptomatic_infections[d] = 0;
        this.infectious_people[d] = new List<Person>();
        this.current_case_fatalities[d] = 0;
        this.total_case_fatalities[d] = 0;
      }
    }

    /**
     * Get the id.
     * @return the id
     */
    public int get_id()
    {
      return this.id;
    }

    public void set_id(int _id)
    {
      this.id = _id;
    }

    /**
     * @return the type
     */
    public char get_type()
    {
      return this.type;
    }

    public void set_type(char _type)
    {
      this.type = _type;
    }

    /**
     * @return the subtype
     */
    public char get_subtype()
    {
      return this.subtype;
    }

    public void set_subtype(char _subtype)
    {
      this.subtype = _subtype;
    }

    /**
     * Get the label.
     *
     * @return the label
     */
    public string get_label()
    {
      return this.label;
    }

    // access methods:
    public int get_adults()
    {
      return (this.enrollees.Count - this.get_children());
    }

    public int get_children()
    {
      int children = 0;
      for (int i = 0; i < this.enrollees.Count; ++i)
      {
        children += (this.enrollees[i].get_age() < Global.ADULT_AGE) ? 1 : 0;
      }
      return children;
    }

    // enroll / unenroll:
    public virtual int enroll(Person per)
    {
      this.enrollees.Add(per);
      Utils.FRED_VERBOSE(1, "Enroll person {0} age {1} in mixing group {2} {3}", per.get_id(), per.get_age(), this.get_id(), this.get_label());
      return this.enrollees.Count - 1;
    }

    public virtual void unenroll(int pos)
    {
      int size = this.enrollees.Count;
      if (!(0 <= pos && pos < size))
      {
        Utils.FRED_VERBOSE(1, "mixing group {0} {1} pos = {2} size = {3}", this.get_id(), this.get_label(), pos, size);
      }
      Utils.assert(0 <= pos && pos < size);
      this.enrollees.RemoveAt(pos);
      for (int a = pos; a < size; a++)
      {
        this.enrollees[a].update_enrollee_index(this, a);
      }
      //var removed = this.enrollees[pos];
      //if (pos < size - 1)
      //{
      //  var moved = this.enrollees[size - 1];
      //  FRED_VERBOSE(1, "UNENROLL mixing group %d %s pos = %d size = %d removed %d moved %d\n",
      //    this.get_id(), this.get_label(), pos, size, removed.get_id(), moved.get_id());
      //  this.enrollees[pos] = moved;
      //  moved.update_enrollee_index(this, pos);
      //}
      //else
      //{
      //  FRED_VERBOSE(1, "UNENROLL mixing group %d %s pos = %d size = %d removed %d moved NONE\n",
      //   this.get_id(), this.get_label(), pos, size, removed.get_id());
      //}
      //this.enrollees.pop_back();
      //FRED_VERBOSE(1, "UNENROLL mixing group %d %s size = %d\n", this.get_id(), this.get_label(), this.enrollees.Count);
    }

    /**
     * Get the age group for a person given a particular disease_id.
     *
     * @param disease_id an integer representation of the disease
     * @param per a pointer to a Person object
     * @return the age group for the given person for the given disease
     */
    public virtual int get_group(int disease_id, Person per) { return 0; }

    /**
     * Get the transmission probability for a given disease between two Person objects.
     *
     * @param disease_id an integer representation of the disease
     * @param i a pointer to a Person object
     * @param s a pointer to a Person object
     * @return the probability that there will be a transmission of disease_id from i to s
     */
    public virtual double get_transmission_probability(int disease_id, Person i, Person s) { return 0; }
    public virtual double get_transmission_prob(int disease_id, Person i, Person s) { return 0; }
    public virtual double get_contacts_per_day(int disease_id) { return 0; }
    public virtual double get_contact_rate(int day, int disease_id) { return 0; }
    public virtual int get_contact_count(Person infector, int disease_id, int sim_day, double contact_rate) { return 0; }

    /**
     * Get the count of agents in this place.
     *
     * @return the count of agents
     */
    public int get_size()
    {
      return this.enrollees.Count;
    }

    public virtual int get_container_size()
    {
      return this.get_size();
    }

    public virtual int get_orig_size()
    {
      return this.N_orig;
    }

    public int get_recovereds(int disease_id)
    {
      int count = 0;
      int size = this.enrollees.Count;
      for (int i = 0; i < size; ++i)
      {
        var person = this.get_enrollee(i);
        count += person.is_recovered(disease_id) ? 1 : 0;
      }
      return count;
    }

    public List<Person> get_enrollees()
    {
      return this.enrollees;
    }

    public List<Person> get_infectious_people(int disease_id)
    {
      return this.infectious_people[disease_id];
    }

    public Person get_enrollee(int i)
    {
      return this.enrollees[i];
    }

    /*
     * Disease transmission
     */
    public List<Person> get_potential_infectors(int disease_id)
    {
      return this.infectious_people[disease_id];
    }

    public List<Person> get_potential_infectees(int disease_id)
    {
      return this.enrollees;
    }

    public void record_infectious_days(int day)
    {
      if (this.first_day_infectious == -1)
      {
        this.first_day_infectious = day;
      }
      this.last_day_infectious = day;
    }

    public void print_infectious(int disease_id)
    {
      Console.Write("INFECTIOUS in Mixing_Group {0} Disease {1}: ", this.get_label(), disease_id);
      int size = this.infectious_people[disease_id].Count;
      for (int i = 0; i < size; ++i)
      {
        Console.Write(" {0}", this.infectious_people[disease_id][i].get_id());
      }
      Console.WriteLine();
    }

    // infectious people
    public void clear_infectious_people(int disease_id)
    {
      this.infectious_people[disease_id].Clear();
    }

    public void add_infectious_person(int disease_id, Person person)
    {
      Utils.FRED_VERBOSE(1, "ADD_INF: person {0} mix_group {1}", person.get_id(), this.label);
      this.infectious_people[disease_id].Add(person);
    }

    public int get_number_of_infectious_people(int disease_id)
    {
      return this.infectious_people[disease_id].Count;
    }

    public Person get_infectious_person(int disease_id, int n)
    {
      Utils.assert(n < this.infectious_people[disease_id].Count);
      return this.infectious_people[disease_id][n]; ;
    }

    public bool has_infectious_people(int disease_id)
    {
      return this.infectious_people[disease_id].Count > 0;
    }

    public bool is_infectious(int disease_id)
    {
      return this.infectious_people[disease_id].Count > 0;
    }

    public bool is_infectious()
    {
      for (int a = 0; a < Global.MAX_NUM_DISEASES; a++)
      {
        if (this.infectious_bitset[a])
        {
          return true;
        }
      }

      return false;
    }

    public bool is_human_infectious(int disease_id)
    {
      return this.human_infectious_bitset[disease_id];
    }

    public void set_human_infectious(int disease_id)
    {
      if (!this.human_infectious_bitset[disease_id])
      {
        this.human_infectious_bitset.Set(disease_id, true);
      }
    }

    public void reset_human_infectious()
    {
      this.human_infectious_bitset.SetAll(false);
    }

    public void set_exposed(int disease_id)
    {
      this.exposed_bitset.Set(disease_id, true);
    }

    public void reset_exposed()
    {
      this.exposed_bitset.SetAll(false);
    }

    public bool is_exposed(int disease_id)
    {
      return this.exposed_bitset[disease_id];
    }

    public void set_recovered(int disease_id)
    {
      this.recovered_bitset.Set(disease_id, true);
    }

    public void reset_recovered()
    {
      this.recovered_bitset.SetAll(false);
    }

    public bool is_recovered(int disease_id)
    {
      return this.recovered_bitset[disease_id];
    }

    public void reset_place_state(int disease_id)
    {
      this.infectious_bitset.Set(disease_id, false);
    }

    public void increment_new_infections(int day, int disease_id)
    {
      if (this.last_update < day)
      {
        this.last_update = day;
        this.new_infections[disease_id] = 0;
      }
      this.new_infections[disease_id]++;
      this.total_infections[disease_id]++;
    }

    public void increment_current_infections(int day, int disease_id)
    {
      if (this.last_update < day)
      {
        this.last_update = day;
        this.current_infections[disease_id] = 0;
      }
      this.current_infections[disease_id]++;
    }

    public void increment_new_symptomatic_infections(int day, int disease_id)
    {
      if (this.last_update < day)
      {
        this.last_update = day;
        this.new_symptomatic_infections[disease_id] = 0;
      }
      this.new_symptomatic_infections[disease_id]++;
      this.total_symptomatic_infections[disease_id]++;
    }

    public void increment_current_symptomatic_infections(int day, int disease_id)
    {
      if (this.last_update < day)
      {
        this.last_update = day;
        this.current_symptomatic_infections[disease_id] = 0;
      }
      this.current_symptomatic_infections[disease_id]++;
    }

    public void increment_case_fatalities(int day, int disease_id)
    {
      if (this.last_update < day)
      {
        this.last_update = day;
        this.current_case_fatalities[disease_id] = 0;
      }
      this.current_case_fatalities[disease_id]++;
      this.total_case_fatalities[disease_id]++;
    }

    public int get_current_case_fatalities(int day, int disease_id)
    {
      if (last_update < day)
      {
        return 0;
      }
      return current_case_fatalities[disease_id];
    }

    public int get_total_case_fatalities(int disease_id)
    {
      return total_case_fatalities[disease_id];
    }

    public int get_new_infections(int day, int disease_id)
    {
      if (last_update < day)
      {
        return 0;
      }
      return this.new_infections[disease_id];
    }

    public int get_current_infections(int day, int disease_id)
    {
      if (last_update < day)
      {
        return 0;
      }
      return this.current_infections[disease_id];
    }

    public int get_total_infections(int disease_id)
    {
      return this.total_infections[disease_id];
    }

    public int get_new_symptomatic_infections(int day, int disease_id)
    {
      if (last_update < day)
      {
        return 0;
      }
      return this.new_symptomatic_infections[disease_id];
    }

    public int get_current_symptomatic_infections(int day, int disease_id)
    {
      if (last_update < day)
      {
        return 0;
      }
      return this.current_symptomatic_infections[disease_id];
    }

    public int get_total_symptomatic_infections(int disease_id)
    {
      return this.total_symptomatic_infections[disease_id];
    }

    /**
     * Get the number of cases of a given disease for the simulation thus far.
     *
     * @param disease_id an integer representation of the disease
     * @return the count of cases for a given disease
     */
    public int get_total_cases(int disease_id)
    {
      return this.total_symptomatic_infections[disease_id];
    }

    /**
     * Get the number of cases of a given disease for the simulation thus far divided by the
     * number of agents in this place.
     *
     * @param disease_id an integer representation of the disease
     * @return the count of rate of cases per people for a given disease
     */
    public double get_incidence_rate(int disease_id)
    {
      return this.total_symptomatic_infections[disease_id] / get_size();
    }

    /**
     * Get the clincal attack rate = 100 * number of cases thus far divided by the
     * number of agents in this place.
     *
     * @param disease_id an integer representation of the disease
     * @return the count of rate of cases per people for a given disease
     */
    public double get_symptomatic_attack_rate(int disease_id)
    {
      return (100.0 * this.total_symptomatic_infections[disease_id]) / get_size();
    }

    /**
     * Get the attack rate = 100 * number of infections thus far divided by the
     * number of agents in this place.
     *
     * @param disease_id an integer representation of the disease
     * @return the count of rate of cases per people for a given disease
     */
    public double get_attack_rate(int disease_id)
    {
      int n = get_size();
      return (n > 0 ? (100.0 * this.total_infections[disease_id]) / n : 0.0);
    }

    public int get_first_day_infectious()
    {
      return this.first_day_infectious;
    }

    public int get_last_day_infectious()
    {
      return this.last_day_infectious;
    }

    public void resets(int disease_id)
    {
      new_infections[disease_id] = 0;
      current_infections[disease_id] = 0;
      new_symptomatic_infections[disease_id] = 0;
      current_symptomatic_infections[disease_id] = 0;
    }
  }
}
