using System;

namespace Fred
{
  public class Person_Place_Link
  {
    private Place place;
    private int enrollee_index;

    public Person_Place_Link()
    {
      this.enrollee_index = -1;
    }

    public void enroll(Person person, Place new_place)
    {
      if (this.place != null)
      {
        Utils.FRED_VERBOSE(0, "enroll failed: place %d %s  enrollee_index %d \n",
         this.place.get_id(), this.place.get_label(), enrollee_index);
      }
      Utils.assert(this.place == null);
      Utils.assert(this.enrollee_index == -1);
      this.place = new_place;
      this.enrollee_index = this.place.enroll(person);
      // printf("ENROLL: place %s size %d\n", place.get_label(), place.get_size()); fflush(stdout);
      Utils.assert(this.enrollee_index != -1);
    }

    public void unenroll(Person person)
    {
      Utils.assert(this.enrollee_index != -1);
      Utils.assert(this.place != null);
      this.place.unenroll(this.enrollee_index);
      // printf("UNENROLL: place %s size %d\n", place.get_label(), place.get_size()); fflush(stdout);
      this.enrollee_index = -1;
      this.place = null;
    }

    public void update_enrollee_index(int new_index)
    {
      Utils.assert(this.enrollee_index != -1);
      Utils.assert(new_index != -1);
      // printf("update_enrollee_index: old = %d new = %d\n", enrollee_index, new_index); fflush(stdout);
      this.enrollee_index = new_index;
    }

    public Place get_place()
    {
      return this.place;
    }

    public int get_enrollee_index()
    {
      return this.enrollee_index;
    }

    public bool is_enrolled()
    {
      return this.enrollee_index != -1;
    }
  }
}
