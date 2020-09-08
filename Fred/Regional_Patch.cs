using System;
using System.Collections.Generic;
using System.Threading;

namespace Fred
{
  public class Regional_Patch : Abstract_Patch
  {
    private int id;
    private int popsize;
    private int max_popsize;
    private double pop_density;
    private Mutex mutex;
    private Regional_Layer grid;
    private readonly List<int> counties = new List<int>();
    private readonly List<Person> person = new List<Person>();
    private readonly List<Place> workplaces = new List<Place>();
    private readonly List<Person> workers = new List<Person>();
    private readonly List<Person>[] students_by_age = new List<Person>[100];
    private readonly Dictionary<char, int> demes = new Dictionary<char, int>();
    private static int next_patch_id = 0;

    public Regional_Patch()
    {
      this.id = -1;
    }

    public Regional_Patch(Regional_Layer grd, int i, int j)
    {
      this.setup(grd, i, j);
    }

    public void setup(Regional_Layer grd, int i, int j)
    {
      this.grid = grd;
      this.row = i;
      this.col = j;
      double patch_size = this.grid.get_patch_size();
      double grid_min_x = this.grid.get_min_x();
      double grid_min_y = this.grid.get_min_y();
      this.min_x = grid_min_x + (this.col) * patch_size;
      this.min_y = grid_min_y + (this.row) * patch_size;
      this.max_x = grid_min_x + (this.col + 1) * patch_size;
      this.max_y = grid_min_y + (this.row + 1) * patch_size;
      this.center_y = (this.min_y + this.max_y) / 2.0;
      this.center_x = (this.min_x + this.max_x) / 2.0;
      this.popsize = 0;
      this.max_popsize = 0;
      this.pop_density = 0;
      this.person.Clear();
      this.counties.Clear();
      this.workplaces.Clear();
      this.id = next_patch_id++;
      for (int k = 0; k < 100; k++)
      {
        this.students_by_age[k] = new List<Person>();
      }
      this.workers.Clear();
    }

    public void quality_control() { }

    public double distance_to_patch(Regional_Patch patch2)
    {
      double x1 = this.center_x;
      double y1 = this.center_y;
      double x2 = patch2.get_center_x();
      double y2 = patch2.get_center_y();
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public void add_person(Person p)
    {
      // <-------------------------------------------------------------- Mutex
      this.mutex.WaitOne();
      try
      {
        this.person.Add(p);
        if (Global.Enable_Vector_Layer)
        {
          var hh = (Household)(p.get_household());
          if (hh == null)
          {
            if (Global.Enable_Hospitals && p.is_hospitalized() && p.get_permanent_household() != null)
            {
              hh = (Household)(p.get_permanent_household());
            }
          }
          int c = hh.get_county_index();
          int h_county = Global.Places.get_fips_of_county_with_index(c);
          this.counties.Add(h_county);
          if (p.is_student())
          {
            int age_ = 0;
            age_ = p.get_age();
            if (age_ > 100)
            {
              age_ = 100;
            }
            if (age_ < 0)
            {
              age_ = 0;
            }
            this.students_by_age[age_].Add(p);
          }
          if (p.get_workplace() != null)
          {
            this.workers.Add(p);
          }
        }
        ++this.demes[p.get_deme_id()];
        ++this.popsize;
      }
      finally
      {
        this.mutex.ReleaseMutex();
      }
    }

    public int get_popsize()
    {
      return this.popsize;
    }

    public Person select_random_person()
    {
      if (this.person.Count == 0)
      {
        return null;
      }

      int i = FredRandom.Next(0, this.person.Count - 1);
      return this.person[i];
    }

    public Person select_random_student(int age_)
    {
      if (this.students_by_age[age_].Count == 0)
      {
        return null;
      }

      int i = FredRandom.Next(0, this.students_by_age[age_].Count - 1);
      return this.students_by_age[age_][i];
    }

    public Person select_random_worker()
    {
      if (this.workers.Count == 0)
      {
        return null;
      }

      int i = FredRandom.Next(0, this.workers.Count - 1);
      return this.workers[i];
    }

    public void set_max_popsize(int n)
    {
      this.max_popsize = n;
      this.pop_density = this.popsize / n;

      // the following reflects noise in the estimated population in the preprocessing routine
      if (this.pop_density > 0.8)
      {
        this.pop_density = 1.0;
      }
    }

    public int get_max_popsize()
    {
      return this.max_popsize;
    }

    public double get_pop_density()
    {
      return this.pop_density;
    }

    public void unenroll(Person pers)
    {
      // <-------------------------------------------------------------- Mutex
      this.mutex.WaitOne();
      try
      {
        if (this.person.Count > 1)
        {
          this.person.Remove(pers);
        }
        else
        {
          this.person.Clear();
        }
      }
      finally
      {
        this.mutex.ReleaseMutex();
      }
    }

    public void add_workplace(Place workplace)
    {
      this.workplaces.Add(workplace);
    }

    public Place get_nearby_workplace(Place place, int staff)
    {
      // printf("get_workplace_near_place entered\n"); print(); fflush(stdout);
      double x = Geo.get_x(place.get_longitude());
      double y = Geo.get_y(place.get_latitude());

      // allow staff size variation by 25%
      int min_staff = (int)(0.75 * staff);
      if (min_staff < 1)
        min_staff = 1;
      int max_staff = (int)(0.5 + 1.25 * staff);
      Utils.FRED_VERBOSE(1, " staff %d %d %d \n", min_staff, staff, max_staff);

      // find nearest workplace that has right number of employees
      double min_dist = 1e99;
      var nearby_workplace = this.grid.get_nearby_workplace(this.row, this.col, x, y, min_staff, max_staff, ref min_dist);
      if (nearby_workplace == null)
      {
        return null;
      }

      Utils.assert(nearby_workplace != null);
      double x2 = Geo.get_x(nearby_workplace.get_longitude());
      double y2 = Geo.get_y(nearby_workplace.get_latitude());
      Utils.FRED_VERBOSE(1, "nearby workplace %s %f %f size %d dist %f\n", nearby_workplace.get_label(),
             x2, y2, nearby_workplace.get_size(), min_dist);

      return nearby_workplace;
    }

    public Place get_closest_workplace(double x, double y, int min_size, int max_size, ref double min_dist)
    {
      // printf("get_closest_workplace entered for patch %d %d\n", row, col); fflush(stdout);
      Place closest_workplace = null;
      int number_workplaces = this.workplaces.Count;
      for (int j = 0; j < number_workplaces; j++)
      {
        var workplace = this.workplaces[j];
        if (workplace.is_group_quarters())
        {
          continue;
        }
        int size = workplace.get_size();
        if (min_size <= size && size <= max_size)
        {
          double x2 = Geo.get_x(workplace.get_longitude());
          double y2 = Geo.get_y(workplace.get_latitude());
          double dist = Math.Sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2));
          if (dist < 20.0 && dist < min_dist)
          {
            min_dist = dist;
            closest_workplace = workplace;
            // printf("closer = %s size = %d min_dist = %f\n", closest_workplace.get_label(), size, *min_dist);
            // fflush(stdout);
          }
        }
      }
      return closest_workplace;
    }

    public int get_id()
    {
      return this.id;
    }

    public void swap_county_people()
    {
      if (counties.Count > 1)
      {
        double percentage = 0.1;
        int people_swapped = 0;
        int people_to_reassign_place = (int)(percentage * this.person.Count);
        Utils.FRED_VERBOSE(1, "People to reassign : %d \n", people_to_reassign_place);
        for (int k = 0; k < people_to_reassign_place; ++k)
        {
          var p = this.select_random_person();
          Person p2;
          if (p != null)
          {
            if (p.is_student())
            {
              int age_ = 0;
              age_ = p.get_age();
              if (age_ > 100)
              {
                age_ = 100;
              }
              if (age_ < 0)
              {
                age_ = 0;
              }
              p2 = select_random_student(age_);
              if (p2 != null)
              {
                var s1 = p.get_school();
                var s2 = p2.get_school();
                var h1 = (Household)p.get_household();
                int c1 = h1.get_county_index();
                int h1_county = Global.Places.get_fips_of_county_with_index(c1);
                var h2 = (Household)p2.get_household();
                int c2 = h2.get_county_index();
                int h2_county = Global.Places.get_fips_of_county_with_index(c2);
                if (h1_county != h2_county)
                {
                  p.change_school(s2);
                  p2.change_school(s1);
                  Utils.FRED_VERBOSE(0, "SWAPSCHOOLS\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", p.get_id(), p2.get_id(), p.get_school().get_label(), p2.get_school().get_label(), p.get_school().get_latitude(), p.get_school().get_longitude(), p2.get_school().get_latitude(), p2.get_school().get_longitude());
                  Console.WriteLine("SWAPSCHOOLS\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", p.get_id(), p2.get_id(), p.get_school().get_label(), p2.get_school().get_label(), p.get_school().get_latitude(), p.get_school().get_longitude(), p2.get_school().get_latitude(), p2.get_school().get_longitude());
                  people_swapped++;
                }
              }
            }
            else if (p.get_workplace() != null)
            {
              p2 = select_random_worker();
              if (p2 != null)
              {
                var w1 = p.get_workplace();
                var w2 = p2.get_workplace();
                var h1 = (Household)p.get_household();
                int c1 = h1.get_county_index();
                int h1_county = Global.Places.get_fips_of_county_with_index(c1);
                var h2 = (Household)p2.get_household();
                int c2 = h2.get_county_index();
                int h2_county = Global.Places.get_fips_of_county_with_index(c2);
                if (h1_county != h2_county)
                {
                  p.change_workplace(w2);
                  p2.change_workplace(w1);
                  Utils.FRED_VERBOSE(0, "SWAPWORKS\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", p.get_id(), p2.get_id(), p.get_workplace().get_label(), p2.get_workplace().get_label(), p.get_workplace().get_latitude(), p.get_workplace().get_longitude(), p2.get_workplace().get_latitude(), p2.get_workplace().get_longitude());
                  Console.WriteLine("SWAPWORKS\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", p.get_id(), p2.get_id(), p.get_workplace().get_label(), p2.get_workplace().get_label(), p.get_workplace().get_latitude(), p.get_workplace().get_longitude(), p2.get_workplace().get_latitude(), p2.get_workplace().get_longitude());
                  people_swapped++;
                }
              }
            }
          }
        }
        Utils.FRED_VERBOSE(0, "People Swapped. %d out of %d\n", people_swapped, people_to_reassign_place);
        Console.WriteLine("People Swapped. {0} out of {1}", people_swapped, people_to_reassign_place);
      }
      return;
    }

    public char get_deme_id()
    {
      char deme_id = default;
      int max_deme_count = 0;
      foreach (var kvp in this.demes)
      {
        if (kvp.Value > max_deme_count)
        {
          max_deme_count = kvp.Value;
          deme_id = kvp.Key;
        }
      }
      return deme_id;
    }
  }
}
