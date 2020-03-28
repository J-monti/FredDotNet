using System;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class MixingGroup
  {
    public MixingGroup(string lab)
    {
      this.id = -1;
      this.type = TYPE_UNSET;
      this.subtype = SUBTYPE_NONE;

      this.Label = lab;
      this.enrollees = new List<Person>();

      // lists of people


      int diseases = Global.Diseases.Count;
      this.infectious_people = new List<Person>[diseases];
      for (int a = 0; a < diseases; a++)
      {
        this.infectious_people[a] = new List<Person>();
      }

      // track whether or not place is infectious with each disease
      this.infectious_bitset = new bool[diseases];
      this.human_infectious_bitset = new bool[diseases];
      this.recovered_bitset = new bool[diseases];
      this.exposed_bitset = new bool[diseases];
      // epidemic counters
      this.new_infections = new int[diseases];
      this.current_infections = new int[diseases];
      this.total_infections = new int[diseases];
      this.new_symptomatic_infections = new int[diseases];
      this.current_symptomatic_infections = new int[diseases];
      this.total_symptomatic_infections = new int[diseases];
      this.current_case_fatalities = new int[diseases];
      this.total_case_fatalities = new int[diseases];
    }

    public string Label { get; }

    public DateTime? FirstDayInfectious { get; private set; }

    public DateTime? LastDayInfectious { get; private set; }

    public List<Person> enrollees { get; }

    public List<Person>[] infectious_people { get; }

    public DateTime? last_update { get; }
    public bool[] infectious_bitset { get; }
    public bool[] human_infectious_bitset { get; }
    public bool[] recovered_bitset { get; }
    public bool[] exposed_bitset { get; }
    public int[] new_infections { get; }
    public int[] current_infections { get; }
    public int[] total_infections { get; }
    public int[] new_symptomatic_infections { get; }
    public int[] current_symptomatic_infections { get; }
    public int[] total_symptomatic_infections { get; }
    public int[] current_case_fatalities { get; }
    public int[] total_case_fatalities { get; }

    public virtual int get_group(int disease_id, Person per) { return 0; }
    public virtual double get_transmission_probability(int disease_id, Person i, Person s) { return 0; }
    public virtual double get_transmission_prob(int disease_id, Person i, Person s) { return 0; }
    public virtual double get_contacts_per_day(int disease_id) { return 0; }
    public virtual double get_contact_rate(int day, int disease_id) { return 0; }
    public virtual int get_contact_count(Person infector, int disease_id, int sim_day, double contact_rate) { return 0; }

    public virtual int enroll(Person per)
    {
      this.enrollees.Add(per);
      return this.enrollees.Count - 1;
    }

    public virtual void unenroll(int pos)
    {
      if (pos >= 0 && pos < this.enrollees.Count)
      {
        this.enrollees.RemoveAt(pos);
      }
    }

    public void print_infectious(int disease_id)
    {
      Console.WriteLine("INFECTIOUS in Mixing_Group {0} Disease {1}: ", this.Label, disease_id);
      int size = this.infectious_people[disease_id].Count;
      for (int i = 0; i < size; ++i)
      {
        Console.WriteLine(" %d", this.infectious_people[disease_id][i].Id);
      }
    }

    public int get_children()
    {
      return this.enrollees.Count(e => e.Age < Global.ADULT_AGE);
    }

    public int get_adults()
    {
      return this.enrollees.Count - this.get_children();
    }

    public int get_recovereds(int disease_id)
    {
      int count = 0;
      int size = this.enrollees.Count;
      for (int i = 0; i < size; ++i)
      {
        var person = this.enrollees[i];
        count += person.is_recovered(disease_id);
      }
      return count;
    }

    public void add_infectious_person(int disease_id, Person person)
    {
      //FRED_VERBOSE(1, "ADD_INF: person %d mix_group %s\n", person.get_id(), this.Label);
      this.infectious_people[disease_id].Add(person);
    }

    public void record_infectious_days(DateTime day)
    {
      if (!this.FirstDayInfectious.HasValue)
      {
        this.FirstDayInfectious = day;
      }

      this.LastDayInfectious = day;
    }
  }
}
