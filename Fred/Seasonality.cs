using System;
using System.Collections.Generic;

namespace Fred
{
  public class Seasonality
  {
    public Seasonality(AbstractGrid abstract_grid)
    {
      this.Grid = abstract_grid;
      this.SeasonalityTimestepMaps = new List<SeasonalityTimestep>();
      this.SeasonalityValues = new double[Grid.Rows, Grid.Cols];
      for (int d = 0; d < Global.Diseases.Count; d++)
      {
        SeasonalityMultipliers.Add(new double[Grid.Rows, Grid.Cols]);
      }
    }

    public AbstractGrid Grid { get; }

    public double[,] SeasonalityValues { get; }

    public List<double[,]> SeasonalityMultipliers { get; }

    public List<SeasonalityTimestep> SeasonalityTimestepMaps { get; }

    public void update(DateTime day)
    {
      var points = new List<SeasonalityPoint>();
      foreach (var cts in this.SeasonalityTimestepMaps)
      {
        if (cts.is_applicable(day, Global.EpidemicOffset))
        {
          int row = 0;
          int col = 0;

          //if ( cts.has_location() ) {
          //  row = grid.get_row(cts.get_lat());
          //  col = grid.get_col(cts.get_lon());
          //}
          //points.push_back(point(row,col,cts.get_seasonality_value()));

          // TODO THIS IS A TEMPORARY HACK TO SUPPORT NORTHERN/SOUTHERN HEMISPHERE EVOLUTION EXPERIMENTS
          if (cts.has_location() && cts.get_lon() > Grid.MinLon && cts.get_lon() < Grid.MaxLon)
          {
            //row = grid.get_row(cts.get_lat());
            col = Grid.GetCol(cts.get_lon());
            for (int r = 0; r < Grid.Rows; ++r)
            {
              points.Add(new SeasonalityPoint(r, col, cts.get_seasonality_value()));
            }
          }
          else
          {
            points.Add(new SeasonalityPoint(row, col, cts.get_seasonality_value()));
          }
        }
      }
      this.nearest_neighbor_interpolation(points);
      // store the climate modulated transmissibilities for all diseses
      // so that we don't have to re-calculate this for every place that we visit
      this.update_seasonality_multiplier();
    }

    private void update_seasonality_multiplier()
    {
      for (int d = 0; d < Global.Diseases.Count; d++)
      {
        var disease = Global.Diseases[d];
        if (Global.IsClimateEnabled)
        { // should seasonality values be interpreted by Disease as specific humidity?
          for (int r = 0; r < Grid.Rows; r++)
          {
            for (int c = 0; c < Grid.Cols; c++)
            {
              SeasonalityMultipliers[d][r,c] = disease.calculate_climate_multiplier(SeasonalityValues[r,c]);
            }
          }
        }
        // TODO Optionally add jitter to seasonality values
        else
        { // just use seasonality values as they are
          SeasonalityMultipliers[d] = SeasonalityValues;
        }
      }
    }

    public double get_seasonality_multiplier_by_lat_lon(FredGeo lat, FredGeo lon, int disease_id)
    {
      int row = Grid.GetRowGeo(lat);
      int col = Grid.GetColGeo(lon);
      return get_seasonality_multiplier(row, col, disease_id);
    }

   public double get_seasonality_multiplier_by_cartesian(double x, double y, int disease_id)
    {
      int row = Grid.GetRow(y);
      int col = Grid.GetCol(x);
      return get_seasonality_multiplier(row, col, disease_id);
    }

    public double get_seasonality_multiplier(int row, int col, int disease_id)
    {
      if (row >= 0 && col >= 0 && row < Grid.Rows && col < Grid.Cols)
      {
        return SeasonalityMultipliers[disease_id][row,col];
      }

      return 0;
    }

    private void nearest_neighbor_interpolation(List<SeasonalityPoint> points)
    {
      int d1, d2, ties;
      for (int r = 0; r < this.Grid.Rows; r++)
      {
        for (int c = 0; c < this.Grid.Cols; c++)
        {
          d1 = this.Grid.Rows + this.Grid.Cols + 1;
          ties = 0;
          foreach (var pit in points)// (vector<point>::iterator pit = points.begin(); pit != points.end(); pit++)
          {
            d2 = Math.Abs(pit.X - r) + Math.Abs(pit.Y - c);
            if (d1 < d2)
            {
              continue;
            }
            if (d1 == d2)
            {
              if ((FredRandom.NextDouble() * (ties + 1)) > ties)
              {
                this.SeasonalityValues[r,c] = pit.Value;
              }
              ties++;
            }
            else
            {
              this.SeasonalityValues[r,c] = pit.Value; d1 = d2;
            }
          }
        }
      }
    }

    public double get_average_seasonality_multiplier(int disease_id)
    {
      double total = 0;
      for (int row = 0; row < Grid.Rows; row++)
      {
        for (int col = 0; col < Grid.Cols; col++)
        {
          total += get_seasonality_multiplier(row, col, disease_id);
        }
      }
      return total / (Grid.Rows * Grid.Cols);
    }
  }
}
