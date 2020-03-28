﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Events
  {
    public const int MAX_DAYS = 100 * 366;

    public Events()
    {
      for (int day = 0; day < MAX_DAYS; ++day)
      {
        clear_events(day);
      }
    }

    void add_event(int day, event_t item)
    {

      if (day < 0 || MAX_DAYS <= day)
      {
        // won't happen during this simulation
        return;
      }
      if (this.events[day].size() == this.events[day].capacity())
      {
        if (this.events[day].capacity() < 4)
        {
          this.events[day].reserve(4);
        }
        this.events[day].reserve(2 * this.events[day].capacity());
      }
      this.events[day].push_back(item);
      // printf("\nadd_event day %d new size %d\n", day, get_size(day));
      // print_events(day);
    }

    void delete_event(int day, event_t item)
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
          this.events[day][pos] = this.events[day].back();
          // delete last slot
          this.events[day].pop_back();
          // printf("\ndelete_event day %d final size %d\n", day, get_size(day));
          // print_events(day);
          return;
        }
      }
      // item not found
      FRED_WARNING("delete_events: item not found\n");
      assert(false);
    }

    void clear_events(int day)
    {

      assert(0 <= day && day < MAX_DAYS);
      this.events[day] = events_t();
      // printf("clear_events day %d size %d\n", day, get_size(day));
    }

    int get_size(int day)
    {

      assert(0 <= day && day < MAX_DAYS);
      return static_cast<int>(this.events[day].size());
    }

    event_t get_event(int day, int i)
    {

      assert(0 <= day && day < MAX_DAYS);
      assert(0 <= i && i < static_cast<int>(this.events[day].size()));
      return this.events[day][i];
    }


    void print_events(FILE* fp, int day)
    {

      assert(0 <= day && day < MAX_DAYS);
      events_itr_t itr_end = this.events[day].end();
      fprintf(fp, "events[%d] = %d : ", day, get_size(day));
      for (events_itr_t itr = this.events[day].begin(); itr != itr_end; ++itr)
      {
        // fprintf(fp, "id %d age %d ", (*itr).get_id(), (*itr).get_age());
      }
      fprintf(fp, "\n");
      fflush(fp);
    }

    void print_events(int day)
    {
      print_events(stdout, day);
    }
  }
}
