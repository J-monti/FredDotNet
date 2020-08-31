using System;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class Disease_List : List<Disease>
  {
    public Disease_List() { }

    public void get_parameters()
    {
      this.Clear();
      int number_of_diseases = 0;
      FredParameters.GetParameter("diseases", ref number_of_diseases);

      // sanity check
      if (number_of_diseases > Global.MAX_NUM_DISEASES)
      {
        Utils.fred_abort("Disease_List::number_of_diseases (= {0}) > Global::MAX_NUM_DISEASES (= {1})!",
              number_of_diseases, Global.MAX_NUM_DISEASES);
      }

      // get disease names
      var disease_names = FredParameters.GetParameterList<string>("disease_names");
      for (int disease_id = 0; disease_id < number_of_diseases; ++disease_id)
      {
        // create new Disease object
        var disease = new Disease();
        // get its parameters
        disease.get_parameters(disease_id, disease_names[disease_id]);
        this.Add(disease);
        Console.WriteLine("disease {0} = {1}", disease_id, disease_names[disease_id]);
      }
    }

    public void setup()
    {
      foreach (var disease in this)
      {
        disease.setup();
      }
    }

    public Disease get_disease(int disease_id)
    {
      return this[disease_id];
    }

    public Disease get_disease(string disease_name)
    {
      return this.FirstOrDefault(d => d.get_disease_name() == disease_name);
    }

    public int get_number_of_diseases()
    {
      return this.Count;
    }

    public void prepare_diseases()
    {
      foreach (var disease in this)
      {
        if (Global.Enable_Viral_Evolution)
        {
          disease.get_natural_history().initialize_evolution_reporting_grid(Global.Simulation_Region);
          disease.get_natural_history().init_prior_immunity();
        }
        disease.prepare();
      }
    }

    public void end_of_run()
    {
      for (int disease_id = 0; disease_id < this.Count; ++disease_id)
      {
        this[disease_id].end_of_run();
      }
    }
  }
}
