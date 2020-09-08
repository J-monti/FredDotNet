using System;
using System.Collections.Generic;

namespace Fred
{
  public class Seasonality
  {
    private Abstract_Grid grid;
    private double[,] seasonality_values; // rectangular array of seasonality values
    private List<double[,]> seasonality_multiplier; // result of applying each disease's seasonality/transmissibily kernel
    private Seasonality_Timestep_Map seasonality_timestep_map;

    public Seasonality(Abstract_Grid abstract_grid)
    {
      this.grid = abstract_grid;
      seasonality_timestep_map = new Seasonality_Timestep_Map("seasonality_timestep");
      seasonality_timestep_map.read_map();
      seasonality_timestep_map.print();
      seasonality_values = new double[grid.get_rows(), grid.get_cols()];
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
      {
        seasonality_multiplier.Add(new double[grid.get_rows(), grid.get_cols()]);
      }
    }

    //~Seasonality(Abstract_Grid * grid);
    public void update(int day)
    {
      var points = new List<SeasonalityPoint>();
      foreach(var timeStep in this.seasonality_timestep_map.get_map())
      {
        if (timeStep.is_applicable(day, Global.Epidemic_offset))
        {
          int row = 0;
          int col = 0;
          //if ( timeStep.has_location() ) {
          //  row = grid.get_row(timeStep.get_lat());
          //  col = grid.get_col(timeStep.get_lon());
          //}
          //points.push_back(point(row,col,timeStep.get_seasonality_value()));

          // TODO THIS IS A TEMPORARY HACK TO SUPPORT NORTHERN/SOUTHERN HEMISPHERE EVOLUTION EXPERIMENTS
          if (timeStep.has_location() && timeStep.get_lon() > grid.get_min_lon() && timeStep.get_lon() < grid.get_max_lon())
          {
            //row = grid.get_row(timeStep.get_lat());
            col = grid.get_col(timeStep.get_lon());
            for (int r = 0; r < grid.get_rows(); ++r)
            {
              points.Add(new SeasonalityPoint(r, col, timeStep.get_seasonality_value()));
            }
          }
          else
          {
            points.Add(new SeasonalityPoint(row, col, timeStep.get_seasonality_value()));
          }
        }
      }

      nearest_neighbor_interpolation(points, ref seasonality_values);
      // store the climate modulated transmissibilities for all diseses
      // so that we don't have to re-calculate this for every place that we visit
      update_seasonality_multiplier();
    }

    public double get_seasonality_multiplier_by_lat_lon(FredGeo lat, FredGeo lon, int disease_id)
    {
      int row = grid.get_row(lat);
      int col = grid.get_col(lon);
      return get_seasonality_multiplier(row, col, disease_id);
    }

    public double get_seasonality_multiplier_by_cartesian(double x, double y, int disease_id)
    {
      int row = grid.get_row(y);
      int col = grid.get_row(x);
      return get_seasonality_multiplier(row, col, disease_id);
    }

    public double get_seasonality_multiplier(int row, int col, int disease_id)
    {
      if (row >= 0 && col >= 0 && row < grid.get_rows() && col < grid.get_cols())
        return seasonality_multiplier[disease_id][row, col];

      return 0;
    }

    public double get_average_seasonality_multiplier(int disease_id)
    {
      double total = 0;
      for (int row = 0; row < grid.get_rows(); row++)
      {
        for (int col = 0; col < grid.get_cols(); col++)
        {
          total += get_seasonality_multiplier(row, col, disease_id);
        }
      }
      return total / (grid.get_rows() * grid.get_cols());
    }

    public void print()
    {
      //return;
      //cout << "Seasonality Values" << endl;
      //print_field(&seasonality_values);
      //cout << endl;
      //for (int d = 0; d < Global::Diseases.get_number_of_diseases(); d++)
      //{
      //  printf("Seasonality Modululated Transmissibility for Disease[%d]\n", d);
      //  print_field(&(seasonality_multiplier[d]));
      //  cout << endl;
      //}
    }

    public void print_summary()
    {
      //return;
      //for (int disease_id = 0; disease_id < Global::Diseases.get_number_of_diseases(); disease_id++)
      //{
      //  double min = 9999999;
      //  double max = 0;
      //  double total = 0;
      //  double daily_avg = 0;
      //  printf("\nSeasonality Summary for disease %d:\n\n", disease_id);
      //  for (int day = 0; day < Global::Days; day++)
      //  {
      //    update(day);
      //    daily_avg = get_average_seasonality_multiplier(disease_id);
      //    if (min > daily_avg) { min = daily_avg; }
      //    if (max < daily_avg) { max = daily_avg; }
      //    total += daily_avg;
      //    printf(" %4.4f", daily_avg);
      //  }
      //  cout << endl;
      //  printf(" minimum: %4.4f\n", min);
      //  printf(" maximum: %4.4f\n", max);
      //  printf(" average: %4.4f\n\n", total / Global::Days);
      //}
      //update(0);
    }

    private void update_seasonality_multiplier()
    {
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); d++)
      {
        var disease = Global.Diseases.get_disease(d);
        if (Global.Enable_Climate)
        { // should seasonality values be interpreted by Disease as specific humidity?
          for (int r = 0; r < grid.get_rows(); r++)
          {
            for (int c = 0; c < grid.get_cols(); c++)
            {
              seasonality_multiplier[d][r, c] = disease.calculate_climate_multiplier(seasonality_values[r, c]);
            }
          }
        }
        // TODO Optionally add jitter to seasonality values
        else
        { // just use seasonality values as they are
          seasonality_multiplier[d] = seasonality_values;
        }
      }
    }

    private void nearest_neighbor_interpolation(List<SeasonalityPoint> points, ref double[,] field)
    {
      int d1, d2, ties;
      for (int r = 0; r < grid.get_rows(); r++)
      {
        for (int c = 0; c < grid.get_cols(); c++)
        {
          d1 = grid.get_rows() + grid.get_cols() + 1;
          ties = 0;
          foreach(var pit in points)
          {
            d2 = Math.Abs(pit.x - r) + Math.Abs(pit.y - c);
            if (d1 < d2)
            {
              continue;
            }
            if (d1 == d2)
            {
              if ((FredRandom.NextDouble() * (ties + 1)) > ties)
              {
                field[r, c] = pit.value;
              }
              ties++;
            }
            else
            {
              field[r, c] = pit.value; d1 = d2;
            }
          }
        }
      }
    }

    private void print_field(double[,] field)
    {
      //return;
      //for (int r = 0; r < grid.get_rows(); r++)
      //{
      //  for (int c = 0; c < grid.get_cols(); c++)
      //  {
      //    Console.WriteLine(" {0}", field[r, c]);
      //  }
      //}
    }
  }
}
