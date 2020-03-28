﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class PiecewiseLinear
  {
    public PiecewiseLinear() {
      disease = NULL;
    }

  void Piecewise_Linear :: setup(string _name, Disease* _disease)
  {
    name = _name; disease = _disease;
    char param_name[255];
    sprintf(param_name, "%s_dists[%d]", name.c_str(), disease->get_id());
    Params::get_param_vector(param_name, ag_distances);

    sprintf(param_name, "%s_probs[%d]", name.c_str(), disease->get_id());
    Params::get_param_vector(param_name, probabilities);

    if (quality_control() != true)
    {
      Utils::fred_abort("Piecewise Linear quality control failed!");
    }
  }

  bool Piecewise_Linear :: quality_control()
  {
    bool return_value = true;
    int disease_id = disease->get_id();
    if (ag_distances.size() != probabilities.size())
    {
      FRED_WARNING("Error parsing %s[%d]: "
  
       "number of distances not equal to number of probabilities",
       name.c_str(), disease_id);
      return_value = false;
    }

    // The antigenic distances have to be sorted and should not repeat
    for (int i = 0; i < ag_distances.size() - 1; i++)
    {
      if (ag_distances[i] >= ag_distances[i + 1])
      {
        FRED_WARNING("Error parsing %s[%d]: "
  
         "%s_distances[%d][%d] not smaller than %s_distances[%d][%d]",
         name.c_str(), disease_id, i, name.c_str(), disease_id, i + 1);
        return_value = false;
      }
    }

    // The probabilities should be valid
    for (int i = 0; i < probabilities.size(); i++)
    {
      if (probabilities[i] > 1.0 || probabilities[i] < 0.0)
      {
        FRED_WARNING("Error parsing %s[%d]: "
  
         "%s_probabilities[%d][%d] not a valid probability",
         name.c_str(), disease->get_id(), i);
        return_value = false;
      }
    }
    return return_value;
  }

  double Piecewise_Linear :: get_prob(double dist)
  {
    // Using linear search assuming the list is small... could use binary search otherwise
    unsigned int i = 0;
    for (; i < ag_distances.size(); i++)
    {
      if (ag_distances[i] > dist) break;
    }

    if (i == 0) return 1.0;
    if (i == ag_distances.size()) return 0.0;

    double dist1 = ag_distances[i - 1], dist2 = ag_distances[i];
    double prob1 = probabilities[i - 1], prob2 = probabilities[i];

    return prob1 + (prob2 - prob1) / (dist2 - dist1) * (dist - dist1);
  }


}
}