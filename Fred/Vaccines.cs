using System;
using System.Collections.Generic;

namespace Fred
{
  public class Vaccines : List<Vaccine>
  {
    public void Update (DateTime day)
    {
      foreach (var vaccine in this)
      {
        vaccine.update(day);
      }
    }
    public void Reset()
    {
      foreach (var vaccine in this)
      {
        vaccine.reset();
      }
    }

    public int pick_from_applicable_vaccines(double real_age)
    {
      var app_vaccs = new List<int>();
      Vaccines list = this;
      for (int i = 0; i < list.Count; i++)
      {
        Vaccine vaccine = list[i];
        // if first dose is applicable, add to vector.
        if (vaccine.get_dose(0).is_within_age(real_age) && vaccine.get_current_stock() > 0)
        {
          app_vaccs.Add(i);
        }
      }
  
      if(app_vaccs.Count == 0)
      {
        return -1;
      }
  
      int randnum = 0;
      if(app_vaccs.Count > 1){
        randnum = FredRandom.Next(0, app_vaccs.Count - 1);
      }

      return app_vaccs[randnum];
    }

    public int get_total_vaccines_avail_today()
    {
      int total = 0;
      foreach (var vaccine in this)
      {
        total += vaccine.get_current_stock();
      }
      return total;
    }
  }
}
