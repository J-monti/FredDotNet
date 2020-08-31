using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fred
{
  public class Neighborhood_Patch : Abstract_Patch
  {
    public const int GRADES = 20;

    private Neighborhood_Layer grid;
    private Place neighborhood;
    private List<Person> person;
    private int popsize;
    private double mean_household_income;
    private int vector_control_status;

    // lists of places by type
    private List<Place> households;
    private List<Place> schools;
    private List<Place> workplaces;
    private List<Place> hospitals;
    private List<Place> schools_attended_by_neighborhood_residents;
    private List<Place>[] schools_attended_by_neighborhood_residents_by_age = new List<Place>[Global.ADULT_AGE];
    private List<Place> workplaces_attended_by_neighborhood_residents;

    /**
   * Default constructor
   */
    public Neighborhood_Patch()
    {
      this.grid = null;
      this.row = -1;
      this.col = -1;
      this.min_x = 0.0;
      this.min_y = 0.0;
      this.max_x = 0.0;
      this.max_y = 0.0;
      this.center_y = 0.0;
      this.center_x = 0.0;
      this.popsize = 0;
      this.mean_household_income = 0.0;
      this.neighborhood = null;
    }

    /**
     * Set all of the attributes for the Neighborhood_Patch
     *
     * @param grd the Neighborhood_Layer to which this patch belongs
     * @param i the row of this Neighborhood_Patch in the Neighborhood_Layer
     * @param j the column of this Neighborhood_Patch in the Neighborhood_Layer
     *
     */
    public void setup(Neighborhood_Layer grd, int i, int j)
    {
      this.grid = grd;
      this.row = i;
      this.col = j;
      double patch_size = grid.get_patch_size();
      double grid_min_x = grid.get_min_x();
      double grid_min_y = grid.get_min_y();
      this.min_x = grid_min_x + (col) * patch_size;
      this.min_y = grid_min_y + (row) * patch_size;
      this.max_x = grid_min_x + (col + 1) * patch_size;
      this.max_y = grid_min_y + (row + 1) * patch_size;
      this.center_y = (min_y + max_y) / 2.0;
      this.center_x = (min_x + max_x) / 2.0;
      this.households = new List<Place>();
      this.schools = new List<Place>();
      this.workplaces = new List<Place>();
      this.hospitals = new List<Place>();
      this.schools_attended_by_neighborhood_residents = new List<Place>();
      this.workplaces_attended_by_neighborhood_residents = new List<Place>();
      this.popsize = 0;
      this.mean_household_income = 0.0;
      this.neighborhood = null;
      vector_control_status = 0;
    }

    /**
     * Used during debugging to verify that code is functioning properly.
     *
     */
    public void quality_control()
    {
      return;
      //if (this.person.size() > 0)
      //{
      //  fprintf(Global::Statusfp,
      //    "PATCH row = %d col = %d  pop = %d  houses = %d work = %d schools = %d by_age ",
      //    this.row, this.col, static_cast<int>(this.person.size()), static_cast<int>(this.households.size()), static_cast<int>(this.workplaces.size()), static_cast<int>(this.schools.size()));
      //  for (int age = 0; age < 20; ++age)
      //  {
      //    fprintf(Global::Statusfp, "%d ", static_cast<int>(this.schools_attended_by_neighborhood_residents_by_age[age].size()));
      //  }
      //  fprintf(Global::Statusfp, "\n");
      //  if (Global::Verbose > 0)
      //  {
      //    for (int i = 0; i < this.schools_attended_by_neighborhood_residents.size(); ++i)
      //    {
      //      School* s = static_cast<School*>(this.schools_attended_by_neighborhood_residents[i]);
      //      fprintf(Global::Statusfp, "School %d: %s by_age: ", i, s.get_label());
      //      for (int a = 0; a < 19; ++a)
      //      {
      //        fprintf(Global::Statusfp, " %d:%d,%d ", a, s.get_students_in_grade(a), s.get_orig_students_in_grade(a));
      //      }
      //      fprintf(Global::Statusfp, "\n");
      //    }
      //    fflush(Global::Statusfp);
      //  }
      //}
    }

    /**
      * Determines distance from this Neighborhood_Patch to another.  Note, that it is distance from the
      * <strong>center</strong> of this Neighborhood_Patch to the <strong>center</strong> of the Neighborhood_Patch in question.
      *
      * @param the patch to check against
      * @return the distance from this patch to the one in question
      */
    public double distance_to_patch(Neighborhood_Patch patch2)
    {
      double x1 = this.center_x;
      double y1 = this.center_y;
      double x2 = patch2.get_center_x();
      double y2 = patch2.get_center_y();
      return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    /**
     * Setup the neighborhood in this Neighborhood_Patch
     */
    public void make_neighborhood()
    {
      var lat = Geo.get_latitude(this.center_y);
      var lon = Geo.get_longitude(this.center_x);

      this.neighborhood = new Neighborhood($"N-{this.row:D4}-{this.col:D4}", Place.SUBTYPE_NONE, lon, lat);
    }

    /**
     * Add household to this Neighborhood_Patch's household vector
     */
    public void add_household(Household p)
    {
      this.households.Add(p);
      if (Global.Householdfp != null)
      {
        Global.Householdfp.WriteLine("{0} {1} {2} {3} {4} house_id: {5} row = {6}  col = {7}  house_number = {8}",
          p.get_label(), p.get_longitude(), p.get_latitude(),
          p.get_x(), p.get_y(), p.get_id(), row, col, get_number_of_households());
        Global.Householdfp.Flush();
      }
    }

    /**
     * Create lists of persons, workplaces, schools (by age)
     */
    public void record_daily_activity_locations()
    {
      Household house;
      Person per;
      Place p;
      School s;

      // create lists of persons, workplaces, schools (by age)
      this.person = new List<Person>();
      this.schools_attended_by_neighborhood_residents = new List<Place>();
      this.workplaces_attended_by_neighborhood_residents = new List<Place>();
      for (int age = 0; age < Global.ADULT_AGE; age++)
      {
        this.schools_attended_by_neighborhood_residents_by_age[age] = new List<Place>();
      }

      // char filename[FRED_STRING_SIZE];
      // sprintf(filename, "PATCHES/Neighborhood_Patch-%d-%d-households", row, col);
      // fp = fopen(filename, "w");
      int houses = get_number_of_households();
      for (int i = 0; i < houses; ++i)
      {
        // printf("house %d of %d\n", i, houses); fflush(stdout);
        house = (Household)this.households[i];
        house.record_profile();
        this.mean_household_income += house.get_household_income();
        int hsize = house.get_size();
        // fprintf(fp, "%d ", hsize);
        for (int j = 0; j < hsize; ++j)
        {
          per = house.get_enrollee(j);
          person.Add(per);
          p = per.get_activities().get_workplace();
          if (p != null)
          {
            insert_if_unique(workplaces_attended_by_neighborhood_residents, p);
          }
          s = (School)(per.get_activities().get_school());
          if (s != null)
          {
            insert_if_unique(this.schools_attended_by_neighborhood_residents, s);
            for (int age = 0; age < Global.ADULT_AGE; ++age)
            {
              if (s.get_students_in_grade(age) > 0)
              {
                // school_by_age[age].push_back(s);
                insert_if_unique(schools_attended_by_neighborhood_residents_by_age[age], s);
              }
            }
          }
        }
        // fprintf(fp, "\n");
      }
      // fclose(fp);
      this.popsize = this.person.Count;
      int householdsCount = get_number_of_households();
      if (householdsCount > 0)
      {
        this.mean_household_income /= householdsCount;
      }
    }

    /**
     * @return a pointer to a random Person in this Neighborhood_Patch
     */
    public Person select_random_person()
    {
      if (this.person.Count == 0)
      {
        return null;
      }
      int i = FredRandom.Next(0, this.person.Count - 1);
      return this.person[i];
    }

    /**
     * @return a pointer to a random Household in this Neighborhood_Patch
     */
    public Place select_random_household()
    {
      if (this.households.Count == 0)
      {
        return null;
      }
      int i = FredRandom.Next(0, this.households.Count - 1);
      return this.households[i];
    }

    /**
     * @return a pointer to a random Workplace in this Neighborhood_Patch
     */
    public Place select_random_workplace()
    {
      if (this.workplaces_attended_by_neighborhood_residents.Count == 0)
      {
        return null;
      }
      int i = FredRandom.Next(0, this.workplaces_attended_by_neighborhood_residents.Count - 1);
      return this.workplaces_attended_by_neighborhood_residents[i];
    }

    public Place select_workplace()
    {
      return this.grid.select_workplace_in_area(row, col);
    }

    public Place select_workplace_in_neighborhood()
    {
      Utils.FRED_VERBOSE(1, "select_workplace_in_neighborhood entered row %d col %d\n", row, col);
      int size = this.workplaces_attended_by_neighborhood_residents.Count;
      if (size == 0)
      {
        Utils.FRED_VERBOSE(1, "Found no workplaces\n");
        return null;
      }
      Utils.FRED_VERBOSE(1, "Found %d workplaces\n", size);
      double max_vacancies = -9999999;
      int max_vacancies_index = -1;
      for (int i = 0; i < size; ++i)
      {
        var p = this.workplaces_attended_by_neighborhood_residents[i];
        double vacancies;
        if (p.get_orig_size() > 0)
        {
          vacancies = (double)(p.get_orig_size() - p.get_size()) / (double)p.get_orig_size();
        }
        else
        {
          vacancies = -1.0 * p.get_size();
        }

        // avoid moves into places that are already 50% overfilled
        if (vacancies > max_vacancies && vacancies > -0.5)
        {
          max_vacancies = vacancies;
          max_vacancies_index = i;
        }
      }
      if (max_vacancies_index < 0)
      {
        Utils.FRED_VERBOSE(1, "select_workplace_in_neighborhood entered row %d col %d\n", row, col);
        Utils.FRED_VERBOSE(1, "Found no workplaces with vacancies\n");
        return null;
      }
      Utils.assert(max_vacancies_index >= 0);
      var p2 = workplaces_attended_by_neighborhood_residents[max_vacancies_index];
      Utils.FRED_VERBOSE(1, "SELECT_WORKPLACE: %s orig %d curr %d max_vacancies %0.4f\n",
             p2.get_label(), p2.get_orig_size(), p2.get_size(), max_vacancies);
      return p2;
    }

    public Place select_random_school(int age)
    {
      if (this.schools_attended_by_neighborhood_residents_by_age[age].Count == 0)
      {
        return null;
      }
      int i = FredRandom.Next(0, this.schools_attended_by_neighborhood_residents_by_age[age].Count - 1);
      return this.schools_attended_by_neighborhood_residents_by_age[age][i];
    }

    public Place select_school(int age)
    {
      return this.grid.select_school_in_area(age, row, col);
    }

    public Place select_school_in_neighborhood(int age, double threshold)
    {
      Utils.FRED_VERBOSE(1, "select_school_in_neighborhood entered age %d row %d col %d\n", age, row, col);
      int size = this.schools_attended_by_neighborhood_residents_by_age[age].Count;
      if (size == 0)
      {
        Utils.FRED_VERBOSE(1, "Found no schools for age %d\n", age);
        return null;
      }
      Utils.FRED_VERBOSE(1, "Found %d schools for age %d\n", size, age);
      double max_vacancies = -9999999.0;
      int max_vacancies_index = -1;
      for (int i = 0; i < size; ++i)
      {
        var p = this.schools_attended_by_neighborhood_residents_by_age[age][i];
        var s = (School)(p);
        double vacancies;
        if (s.get_orig_students_in_grade(age) > 0)
        {
          vacancies = (double)(s.get_orig_students_in_grade(age) - s.get_students_in_grade(age)) / (double)s.get_orig_students_in_grade(age);
        }
        else
        {
          vacancies = -1.0 * s.get_students_in_grade(age);
        }
        // avoid moves into places that are already overfilled
        if (vacancies > max_vacancies && vacancies > threshold)
        {
          max_vacancies = vacancies;
          max_vacancies_index = i;
        }
      }
      if (max_vacancies_index < 0)
      {
        Utils.FRED_VERBOSE(1, "select_school_in_neighborhood entered age %d row %d col %d\n", age, row, col);
        Utils.FRED_VERBOSE(1, "Found no schools with vacancies for age %d\n", age);
        return null;
      }

      var school = this.schools_attended_by_neighborhood_residents_by_age[age][max_vacancies_index];
      var s2 = (School)school;
      Utils.FRED_VERBOSE(1, "SELECT_SCHOOL: age %d %s size %d orig_in_grade %d curr_in_grade %d max_vacancies %0.4f\n",
             age, s2.get_label(), s2.get_size(), s2.get_orig_students_in_grade(age),
             s2.get_students_in_grade(age), max_vacancies);
      return school;
    }

    public void find_schools_for_age(int age, List<Place> schools)
    {
      Utils.FRED_VERBOSE(1, "find_schools_for_age %d row %d col %d\n", age, row, col);
      int size = this.schools_attended_by_neighborhood_residents_by_age[age].Count;
      for (int i = 0; i < size; ++i)
      {
        var p = this.schools_attended_by_neighborhood_residents_by_age[age][i];
        insert_if_unique(schools, p);
      }
    }

    /**
     * @return a count of houses in this Neighborhood_Patch
     */
    public int get_houses()
    {
      return this.households.Count;
    }

    /**
     * @return a pointer to this Neighborhood_Patch's Neighborhood
     */
    public Place get_neighborhood()
    {
      return this.neighborhood;
    }

    public int enroll(Person per)
    {
      return this.neighborhood.enroll(per);
    }

    /**
     * @return the popsize
     */
    public int get_popsize()
    {
      return this.popsize;
    }

    public double get_mean_household_income()
    {
      return this.mean_household_income;
    }

    public int get_number_of_households()
    {
      return this.households.Count;
    }

    public Place get_household(int i)
    {
      if (0 <= i && i < get_number_of_households())
      {
        return this.households[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_schools()
    {
      return (int)this.schools.Count;
    }

    public Place get_school(int i)
    {
      if (0 <= i && i < get_number_of_schools())
      {
        return this.schools[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_workplaces()
    {
      return (int)this.workplaces.Count;
    }

    public Place get_workplace(int i)
    {
      if (0 <= i && i < get_number_of_workplaces())
      {
        return this.workplaces[i];
      }
      else
      {
        return null;
      }
    }

    public int get_number_of_hospitals()
    {
      return (int)this.hospitals.Count;
    }

    public Place get_hospital(int i)
    {
      if (0 <= i && i < get_number_of_hospitals())
      {
        return this.hospitals[i];
      }
      else
      {
        return null;
      }
    }

    public void register_place(Place place)
    {
      if (place.is_school())
      {
        this.schools.Add(place);
      }
      if (place.is_workplace())
      {
        this.workplaces.Add(place);
      }
      if (place.is_hospital())
      {
        this.hospitals.Add(place);
      }
    }

    public void set_vector_control_status(int v_s) { this.vector_control_status = v_s; }

    public int get_vector_control_status() { return this.vector_control_status; }

    private void insert_if_unique(List<Place> places, Place place)
    {
      var found = places.FirstOrDefault(p => p == place);
      if (found == null)
      {
        places.Add(place);
      }
    }
  }
}
