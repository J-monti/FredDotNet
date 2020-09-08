using System;
using System.Collections.Generic;

namespace Fred
{
  public class Vaccines : List<Vaccine>
  {
    public void setup()
    {
      int number_vacc = 0;
      FredParameters.GetParameter("number_of_vaccines", ref number_vacc);

      for (int iv = 0; iv < number_vacc; iv++)
      {
        int ta  = 0;
        int apd = 0;
        int std = 0;
        int tbd = 0;
        int num_doses = 0;

        FredParameters.get_indexed_param("vaccine_number_of_doses", iv, ref num_doses);
        FredParameters.get_indexed_param("vaccine_total_avail", iv, ref ta);
        FredParameters.get_indexed_param("vaccine_additional_per_day", iv, ref apd);
        FredParameters.get_indexed_param("vaccine_starting_day", iv, ref std);
        var efficacy_duration_map = new Age_Map("Vaccine Efficacy Duration");
        efficacy_duration_map.read_from_input("vaccine_efficacy_duration", iv);

        int nstrains = 0;
        FredParameters.get_indexed_param("vaccine_strains", iv, ref nstrains);
        var strains = FredParameters.get_indexed_param_vector<int>("vaccine_strains", iv).ToArray();

        var name = $"Vaccine#{iv + 1}";
        this.Add(new Vaccine(name, iv, 0, ta, apd, std, nstrains, strains));

        for (int id = 0; id < num_doses; id++)
        {
          var efficacy_map = new Age_Map("Dose Efficacy");
          var efficacy_delay_map = new Age_Map("Dose Efficacy Delay");
          FredParameters.get_double_indexed_param("vaccine_next_dosage_day", iv, id, ref tbd);
          efficacy_map.read_from_input("vaccine_dose_efficacy", iv, id);
          efficacy_delay_map.read_from_input("vaccine_dose_efficacy_delay", iv, id);
          this[iv].add_dose(new Vaccine_Dose(efficacy_map, efficacy_delay_map, efficacy_duration_map, tbd));
        }
      }
    }

    public Vaccine get_vaccine(int i) { return this[i]; }

    //public List<int> which_vaccines_applicable(double real_age);

    public int pick_from_applicable_vaccines(double real_age)
    {
      List<int> app_vaccs = new List<int>();
      for (int i = 0; i < this.Count; i++)
      {
        // if first dose is applicable, add to vector.
        if (this[i].get_dose(0).is_within_age(real_age) &&
           this[i].get_current_stock() > 0)
        {
          app_vaccs.Add(i);
        }
      }

      if (app_vaccs.Count == 0) { return -1; }

      int randnum = 0;
      if (app_vaccs.Count > 1)
      {
        randnum = (int)(FredRandom.NextDouble() * app_vaccs.Count);
      }

      return app_vaccs[randnum];
    }

    public int get_total_vaccines_avail_today()
    {
      int total = 0;
      for (int i = 0; i < this.Count; i++)
      {
        total += this[i].get_current_stock();
      }
      return total;
    }


    //utility Functions
    public void print()
    {
      Console.WriteLine("Vaccine Package Information");
      Console.WriteLine($"There are {this.Count} vaccines in the package");
      for (int i = 0; i < this.Count; i++)
      {
        this[i].print();
      }
    }

    public void print_current_stocks()
    {
      Console.WriteLine("Vaccine Stockk Information");
      Console.WriteLine($"\nVaccines#  Current Stock       Current Reserve    ");
      for (int i = 0; i < this.Count; i++)
      {
        Console.WriteLine($"{i + 1:D10}{this[i].get_current_stock():D20}{this[i].get_current_reserve():D20}");
       // cout << setw(10) << i + 1 << setw(20) << vaccines[i].get_current_stock()
       //<< setw(20) << vaccines[i].get_current_reserve() << "\n";
      }
    }

    public void update(int day)
    {
      foreach (var vaccine in this)
      {
        vaccine.update(day);
      }
    }

    public void reset()
    {
      foreach (var vaccine in this)
      {
        vaccine.reset();
      }
    }
  }
}
