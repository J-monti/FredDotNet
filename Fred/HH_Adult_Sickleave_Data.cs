using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class HH_Adult_Sickleave_Data
  {
    private Dictionary<Person, bool> stayed_home_for_child_map;

    public void add_child_to_maps(Person child)
    {
      if (this.stayed_home_for_child_map.ContainsKey(child))
      {
        Utils.fred_abort("That kids is already in here!");
      }
      this.stayed_home_for_child_map.Add(child, false);
    }

    public bool stay_home_with_child(Person child)
    {
      if (this.stayed_home_for_child_map.ContainsKey(child))
      {
        this.stayed_home_for_child_map[child] = true;
        return true;
      }

      return false;
    }
  }
}
