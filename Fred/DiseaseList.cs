using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class DiseaseList : List<Disease>
  {
    public Disease GetDisease (string name)
    {
      return this.FirstOrDefault(d => d.Name == name);
    }

    public void Setup()
    {
      foreach (var disease in this)
      {
        disease.Setup();
      }
    }

    public void Prepare()
    {
      foreach (var disease in this)
      {
        if (Global.IsViralEvolutionEnabled)
        {
          disease.NaturalHistory.InitEvolutionReportingGrid(Global.SimulationRegion);
          disease.NaturalHistory.InitPriorImmunity;
        }
        disease.Prepare();
      }
    }

    public void EndOfRun()
    {
      foreach (var disease in this)
      {
        disease.EndOfRun();
      }
    }
  }
}
