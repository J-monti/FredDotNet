using System;

namespace Fred
{
  public class Transmission
  {
    protected double Seasonal_Reduction = 0.0;
    protected double[] Seasonality_multiplier;

    public Transmission get_new_transmission(TransmissionMode transmission_mode)
    {
      switch (transmission_mode)
      {
        case TransmissionMode.Respiratory:
          return new RespiratoryTransmission();
        case TransmissionMode.Vector:
          return new VectorTransmission();
        case TransmissionMode.Sexual:
          return new SexualTransmission();
        default:
          throw new InvalidOperationException("Unknown transmission mode!");
      }
    }

    public void get_parameters()
    {
      // all-disease seasonality reduction
      Params::get_param_from_string("seasonal_reduction", &Transmission::Seasonal_Reduction);
      // setup seasonal multipliers

      if (Seasonal_Reduction > 0.0)
      {
        int seasonal_peak_day_of_year; // e.g. Jan 1
        Params::get_param_from_string("seasonal_peak_day_of_year", &seasonal_peak_day_of_year);

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
  }
}
