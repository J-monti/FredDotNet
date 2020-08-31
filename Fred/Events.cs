using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fred
{
  public class Events
  {
    public const int MAX_DAYS = 100 * 366;
    private List<Person>[] events = new List<Person>[MAX_DAYS];

    public Events()
    {
      for (int day = 0; day < MAX_DAYS; ++day)
      {
        clear_events(day);
      }
    }

    public void add_event(int day, Person item)
    {
      if (day < 0 || MAX_DAYS <= day)
      {
        // won't happen during this simulation
        return;
      }
      this.events[day].Add(item);
    }

    public void delete_event(int day, Person item)
    {

      if (day < 0 || MAX_DAYS <= day)
      {
        // won't happen during this simulation
        return;
      }
      // find item in the list
      int size = get_size(day);
      for (int pos = 0; pos < size; ++pos)
      {
        if (this.events[day][pos] == item)
        {
          // copy last item in list into this slot
          this.events[day][pos] = this.events[day].Last();
          // delete last slot
          this.events[day].PopBack();
          // printf("\ndelete_event day %d final size %d\n", day, get_size(day));
          // print_events(day);
          return;
        }
      }
      // item not found
      Utils.fred_abort("delete_events: item not found\n");
    }

    public void clear_events(int day)
    {
      Utils.assert(0 <= day && day < MAX_DAYS);
      this.events[day] = new List<Person>();
      // printf("clear_events day %d size %d\n", day, get_size(day));
    }

    public int get_size(int day)
    {
      Utils.assert(0 <= day && day < MAX_DAYS);
      return this.events[day].Count;
    }

    public Person get_event(int day, int i)
    {
      Utils.assert(0 <= day && day < MAX_DAYS);
      Utils.assert(0 <= i && i < this.events[day].Count);
      return this.events[day][i];
    }

    public void print_events(TextWriter fp, int day)
    {
      Utils.assert(0 <= day && day < MAX_DAYS);
      fp.WriteLine("events[{0}] = {1} : ", day, get_size(day));
      foreach (var e in this.events[day])
      {
        fp.WriteLine("id {0} age {1} ", e.get_id(), e.get_age());
      }

      fp.WriteLine();
      fp.Flush();
    }

    public void print_events(int day)
    {
      print_events(Console.Out, day);
    }
  }
}
