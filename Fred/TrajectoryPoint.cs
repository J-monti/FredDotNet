namespace Fred
{
  public struct TrajectoryPoint
  {
    public double infectivity;
    public double symptomaticity;
    public TrajectoryPoint(double infectivity_value, double symptomaticity_value)
    {
      infectivity = infectivity_value;
      symptomaticity = symptomaticity_value;
    }
  }
}
