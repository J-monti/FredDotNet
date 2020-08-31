using System;
using System.Collections.Generic;

namespace Fred
{
  public class RegionalPatch : Abstract_Patch
  {
    private int m_Row;
    private int m_Col;
    private RegionalLayer m_Grid;
    private readonly List<Person> m_People;
    private readonly List<Person> m_Workers;
    private readonly List<County> m_Counties;
    private readonly List<Place> m_WorkPlaces;
    private readonly Dictionary<int, int> m_Demes;
    private readonly Dictionary<int, List<Person>> m_StudentsByAge;

    public RegionalPatch()
    {
      this.m_People = new List<Person>();
      this.m_Workers = new List<Person>();
      this.m_Counties = new List<County>();
      this.m_WorkPlaces = new List<Place>();
      this.m_StudentsByAge = new Dictionary<int, List<Person>>();
    }

    public RegionalPatch(RegionalLayer grid, int i, int j)
    {
      this.m_People = new List<Person>();
      this.m_Workers = new List<Person>();
      this.m_Counties = new List<County>();
      this.m_WorkPlaces = new List<Place>();
      this.m_StudentsByAge = new Dictionary<int, List<Person>>();
      this.Setup(grid, i, j);
    }

    public int MaxPopSize { get; protected set; }

    public int PopSize { get; protected set; }

    public double PopDensity { get; protected set; }

    public void AddPerson(Person p)
    {
      this.m_People.Add(p);
      if (Global.IsVectorLayerEnabled)
      {
        Household hh = p.GetHousehold();
        if (hh == null)
        {
          if (Global.IsHospitalsEnabled && p.is_hospitalized() && p.get_permanent_household() != null)
          {
            hh = p.get_permanent_household();
          }
        }
        int c = hh.GetCountyIndex();
        int h_county = Global.Places.get_fips_of_county_with_index(c);
        this.m_Counties.Add(h_county);
        if (p.IsStudent)
        {
          int age = 0;
          age = p.Demographics.Age;
          if (age > 100)
          {
            age = 100;
          }
          if (age < 0)
          {
            age = 0;
          }
          this.m_StudentsByAge[age].Add(p);
        }
        if (p.GetWorkplace() != null)
        {
          this.m_Workers.Add(p);
        }
      }
      ++this.m_Demes[p.GetDemeId()];
      ++this.PopSize;
    }

    public void Setup(RegionalLayer grd, int i, int j)
    {
      this.m_Grid = grd;
      this.m_Row = i;
      this.m_Col = j;
      double patch_size = this.m_Grid.PatchSize;
      double grid_min_x = this.m_Grid.MinX;
      double grid_min_y = this.m_Grid.MinY;
      this.MinX = grid_min_x + (this.m_Col) * patch_size;
      this.MinY = grid_min_y + (this.m_Row) * patch_size;
      this.MaxX = grid_min_x + (this.m_Col + 1) * patch_size;
      this.MaxY = grid_min_y + (this.m_Row + 1) * patch_size;
      this.CenterY = (this.MinY + this.MaxY) / 2.0;
      this.CenterX = (this.MinX + this.MaxY) / 2.0;
      this.PopSize = 0;
      this.MaxPopSize = 0;
      this.PopDensity = 0;
      this.m_People.Clear();
      this.m_Counties.Clear();
      this.m_WorkPlaces.Clear();
      this.m_Workers.Clear();
      this.m_StudentsByAge.Clear();
      for (int k = 0; k < 100; k++)
      {
        this.m_StudentsByAge.Add(k, new List<Person>());
      }
    }

    public void quality_control()
    {
      return;
    }

    public double DistanceToPatch(RegionalPatch p2)
    {
      double x1 = this.CenterX;
      double y1 = this.CenterY;
      double x2 = p2.CenterX;
      double y2 = p2.CenterY;
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public Person RandomPerson()
    {
      if (this.m_People.Count == 0)
      {
        return null;
      }

      int index = FredRandom.Next(0, this.m_People.Count - 1);
      return this.m_People[index];
    }

    public void SetMaxPopSize(int n)
    {
      this.MaxPopSize = n;
      this.PopDensity = this.PopSize / (double)n;

      // the following reflects noise in the estimated population in the preprocessing routine
      if (this.PopDensity > 0.8)
      {
        this.PopDensity = 1.0;
      }
    }

    public Person RandomStudent(int age)
    {
      if (this.m_StudentsByAge[age].Count == 0)
      {
        return null;
      }

      int index = FredRandom.Next(0, this.m_StudentsByAge[age].Count - 1);
      return this.m_StudentsByAge[age][index];
    }

    public Person RandomWorker()
    {
      if (this.m_Workers.Count == 0)
      {
        return null;
      }

      int index = FredRandom.Next(0, this.m_Workers.Count - 1);
      return this.m_Workers[index];
    }


    public void Unenroll(Person person)
    {
      // <-------------------------------------------------------------- Mutex
      this.m_People.Remove(person);
      /*if (this.m_People.Count > 1)
      {
        std::vector<Person*>::iterator iter;
        iter = std::find(this.m_People.begin(), this.m_People.end(), per);
        if (iter != this.m_People.end())
        {
          std::swap((*iter), this.m_People.back());
          this.m_People.erase(this.m_People.end() - 1);
        }
        assert(std::find(this.m_People.begin(), this.m_People.end(), per) == this.m_People.end());
      }
      else
      {
        this.m_People.Clear();
      }*/
    }


    public Place GetNearbyWorkplace(Place place, int staff)
    {
      // printf("get_workplace_near_place entered\n"); print(); fflush(stdout);
      double x = Geo.GetX(place.GetLongitude());
      double y = Geo.GetY(place.GetLatitude());

      // allow staff size variation by 25%
      int min_staff = (int)(0.75 * staff);
      if (min_staff < 1)
        min_staff = 1;
      int max_staff = (int)(0.5 + 1.25 * staff);
      //FredUtils.Log(1, " staff %d %d %d \n", min_staff, staff, max_staff);

      // find nearest workplace that has right number of employees
      double min_dist = 1e99;
      var nearby_workplace = this.m_Grid.GetNearbyWorkplace(this.m_Row, this.m_Col, x, y, min_staff, max_staff, ref min_dist);
      if (nearby_workplace == null)
      {
        return null;
      }

      double x2 = Geo.GetX(nearby_workplace.GetLongitude());
      double y2 = Geo.GetY(nearby_workplace.GetLatitude());
      //FredUtils.Log(1, "nearby workplace %s %f %f size %d dist %f\n", nearby_workplace.get_label(),
      //       x2, y2, nearby_workplace.get_size(), min_dist);

      return nearby_workplace;
    }

    public Place GetClosestWorkplace(double x, double y, int min_size, int max_size, ref double min_dist)
    {
      // printf("get_closest_workplace entered for patch %d %d\n", row, col); fflush(stdout);
      Place closest_workplace = null;
      int number_workplaces = this.m_WorkPlaces.Count;
      for (int j = 0; j < number_workplaces; j++)
      {
        Place workplace = this.m_WorkPlaces[j];
        if (workplace.IsGroupQuarters)
        {
          continue;
        }

        int size = workplace.Size;
        if (min_size <= size && size <= max_size)
        {
          double x2 = Geo.GetX(workplace.GetLongitude());
          double y2 = Geo.GetY(workplace.GetLatitude());
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

    public void AddWorkplace(Place workplace)
    {
      this.m_WorkPlaces.Add(workplace);
    }

    public void SwapCountyPeople()
    {
      if (this.m_Counties.Count > 1)
      {
        double percentage = 0.1;
        int people_swapped = 0;
        int people_to_reassign_place = (int)(percentage * this.m_People.Count);
        //FredUtils.Log(1, "People to reassign : %d \n", people_to_reassign_place);
        for (int k = 0; k < people_to_reassign_place; ++k)
        {
          Person p = this.RandomPerson();
          Person p2;
          if (p != null)
          {
            if (p.IsStudent)
            {
              int age = p.Demographics.Age;
              if (age > 100)
              {
                age = 100;
              }
              if (age < 0)
              {
                age = 0;
              }
              p2 = this.RandomStudent(age);
              if (p2 != null)
              {
                Place s1 = p.GetSchool();
                Place s2 = p2.GetSchool();
                var h1 = p.GetHousehold();
                int c1 = h1.get_county_index();
                int h1_county = Global.Places.get_fips_of_county_with_index(c1);
                var h2 = p2.GetHousehold();
                int c2 = h2.get_county_index();
                int h2_county = Global.Places.get_fips_of_county_with_index(c2);
                if (h1_county != h2_county)
                {
                  p.ChangeSchool(s2);
                  p2.ChangeSchool(s1);
                  //FredUtils.Log(0, "SWAPSCHOOLS\t%d\t%d\t%s\t%s\t%lg\t%lg\t%lg\t%lg\n", p.get_id(), p2.get_id(), p.get_school().get_label(), p2.get_school().get_label(), p.get_school().get_latitude(), p.get_school().get_longitude(), p2.get_school().get_latitude(), p2.get_school().get_longitude());
                  //printf("SWAPSCHOOLS\t%d\t%d\t%s\t%s\t%lg\t%lg\t%lg\t%lg\n", p.get_id(), p2.get_id(), p.get_school().get_label(), p2.get_school().get_label(), p.get_school().get_latitude(), p.get_school().get_longitude(), p2.get_school().get_latitude(), p2.get_school().get_longitude());
                  people_swapped++;
                }
              }
            }
            else if (p.GetWorkplace() != null)
            {
              p2 = this.RandomWorker();
              if (p2 != null)
              {
                Place w1 = p.GetWorkplace();
                Place w2 = p2.GetWorkplace();
                var h1 = p.GetHousehold();
                int c1 = h1.get_county_index();
                int h1_county = Global.Places.get_fips_of_county_with_index(c1);
                var h2 = p2.GetHousehold();
                int c2 = h2.get_county_index();
                int h2_county = Global.Places.get_fips_of_county_with_index(c2);
                if (h1_county != h2_county)
                {
                  p.ChangeWorkplace(w2);
                  p2.ChangeWorkplace(w1);
                  //FredUtils.Log(0, "SWAPWORKS\t%d\t%d\t%s\t%s\t%lg\t%lg\t%lg\t%lg\n", p.get_id(), p2.get_id(), p.get_workplace().get_label(), p2.get_workplace().get_label(), p.get_workplace().get_latitude(), p.get_workplace().get_longitude(), p2.get_workplace().get_latitude(), p2.get_workplace().get_longitude());
                  //printf("SWAPWORKS\t%d\t%d\t%s\t%s\t%lg\t%lg\t%lg\t%lg\n", p.get_id(), p2.get_id(), p.get_workplace().get_label(), p2.get_workplace().get_label(), p.get_workplace().get_latitude(), p.get_workplace().get_longitude(), p2.get_workplace().get_latitude(), p2.get_workplace().get_longitude());
                  people_swapped++;
                }
              }
            }
          }
        }
        //FredUtils.Log(0, "People Swapped:: %d out of %d\n", people_swapped, people_to_reassign_place);
        //printf("People Swapped:: %d out of %d\n", people_swapped, people_to_reassign_place);
      }
      return;
    }


    public int GetDemeId()
    {
      int demeId = 0;
      int maxDemeCount = 0;
      foreach (var kvp in this.m_Demes)
      {
        if (kvp.Value > maxDemeCount)
        {
          maxDemeCount = kvp.Value;
          demeId = kvp.Key;
        }
      }
      return demeId;
    }
  }
}
