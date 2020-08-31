using System.Collections.Generic;

namespace Fred
{
  public class Piecewise_Linear
  {
    private string name;
    private Disease disease;
    private List<double> ag_distances;     // Antigenic distances
    private List<double> probabilities;    // Corresponding values of the function

    public void setup(string _name, Disease _disease)
    {
      name = _name;
      disease = _disease;
      ag_distances = FredParameters.GetParameterList<double>($"{name}_dists[{disease.get_id()}]");
      probabilities = FredParameters.GetParameterList<double>($"{name}_probs[{disease.get_id()}]");

      if (quality_control() != true)
      {
        Utils.fred_abort("Piecewise Linear quality control failed!");
      }
    }
    public double get_prob(double dist)
    {
      // Using linear search assuming the list is small... could use binary search otherwise
      int i = 0;
      for (; i < ag_distances.Count; i++)
      {
        if (ag_distances[i] > dist) break;
      }

      if (i == 0) return 1.0;
      if (i == ag_distances.Count) return 0.0;

      double dist1 = ag_distances[i - 1], dist2 = ag_distances[i];
      double prob1 = probabilities[i - 1], prob2 = probabilities[i];

      return prob1 + (prob2 - prob1) / (dist2 - dist1) * (dist - dist1);
    }

    private bool quality_control()
    {
      bool return_value = true;
      int disease_id = disease.get_id();
      if (ag_distances.Count != probabilities.Count)
      {
        Utils.FRED_STATUS(0, "Error parsing %s[%d]: number of distances not equal to number of probabilities", name, disease_id);
        return_value = false;
      }

      // The antigenic distances have to be sorted and should not repeat
      for (int i = 0; i < ag_distances.Count - 1; i++)
      {
        if (ag_distances[i] >= ag_distances[i + 1])
        {
          Utils.FRED_STATUS(0, "Error parsing %s[%d]: %s_distances[%d][%d] not smaller than %s_distances[%d][%d]",
           name, disease_id, i, name, disease_id, i + 1);
          return_value = false;
        }
      }

      // The probabilities should be valid
      for (int i = 0; i < probabilities.Count; i++)
      {
        if (probabilities[i] > 1.0 || probabilities[i] < 0.0)
        {
          Utils.FRED_STATUS(0, "Error parsing %s[%d]: %s_probabilities[%d][%d] not a valid probability",
           name, disease.get_id(), i);
          return_value = false;
        }
      }
      return return_value;
    }
  }
}
