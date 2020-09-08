using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class census_tract_record
  {
    public double threshold;
    public long ind;
    public int popsize;
    public int total_neighborhoods;
    public int first_day_infectious;
    public bool exceeded_threshold;
    public bool eligible_for_vector_control;
    public readonly List<Neighborhood_Patch> neighborhoods = new List<Neighborhood_Patch>();
    public readonly List<Neighborhood_Patch> infectious_neighborhoods = new List<Neighborhood_Patch>();
    public readonly List<Neighborhood_Patch> non_infectious_neighborhoods = new List<Neighborhood_Patch>();
    public readonly List<Neighborhood_Patch> vector_control_neighborhoods = new List<Neighborhood_Patch>();
  }
}
