using System;

namespace Fred
{
  public abstract class Transmission
  {
    protected static double Seasonal_Reduction = 0.0;
    protected static double[] Seasonality_multiplier;

    public static Transmission get_new_transmission(string transmission_mode)
    {
      switch (transmission_mode)
      {
        case "respiratory":
          return new Respiratory_Transmission();
        case "vector":
          return new Vector_Transmission();
        case "sexual":
          return new Sexual_Transmission();
        default:
          throw new InvalidOperationException("Unknown transmission mode!");
      }
    }

    public static void get_parameters()
    {
      // all-disease seasonality reduction
      FredParameters.GetParameter("seasonal_reduction", ref Seasonal_Reduction);
      // setup seasonal multipliers

      if (Seasonal_Reduction > 0.0)
      {
        int seasonal_peak_day_of_year = 0; // e.g. Jan 1
        FredParameters.GetParameter("seasonal_peak_day_of_year", ref seasonal_peak_day_of_year);

        // setup seasonal multipliers
        Seasonality_multiplier = new double[367];
        for (int day = 1; day <= 366; ++day)
        {
          int days_from_peak_transmissibility = Math.Abs(seasonal_peak_day_of_year - day);
          Seasonality_multiplier[day] = (1.0 - Seasonal_Reduction) +
            Seasonal_Reduction * 0.5 * (1.0 + Math.Cos(days_from_peak_transmissibility * (2 * Math.PI / 365.0)));
          if (Seasonality_multiplier[day] < 0.0)
          {
            Seasonality_multiplier[day] = 0.0;
          }
          // printf("Seasonality_multiplier[%d] = %e %d\n", day, Transmission::Seasonality_multiplier[day], days_from_peak_transmissibility);
        }
      }
    }

    public abstract void setup(Disease disease);
    public abstract void spread_infection(int day, int disease_id, Mixing_Group mixing_group);
  }
}
