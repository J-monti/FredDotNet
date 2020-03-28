using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  internal class TrajectoryPoint
  {
    public double infectivity;
    public double symptomaticity;
    internal TrajectoryPoint(double infectivity_value, double symptomaticity_value)
    {
      infectivity = infectivity_value;
      symptomaticity = symptomaticity_value;
    }
  }
}
