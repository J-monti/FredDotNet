using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Markov_Model
  {
    private Age_Map age_map;
    private int age_groups;
    private double[,] state_initial_percent;
    private List<double[,]> transition_matrix;
    private int period_in_transition_probabilities;

    protected string name;
    protected int number_of_states;
    protected List<string> state_name;

    public void setup(string disease_name)
    {
      this.name = disease_name;
    }

    public void get_parameters()
    {
      Utils.FRED_VERBOSE(0, "Markov_Model(%s).get_parameters\n", this.name);
      FredParameters.GetParameter($"{this.name}_states", ref this.number_of_states);
      this.state_name = new List<string>();
      this.transition_matrix = new List<double[,]>();

      // get state names
      string stateName = string.Empty;
      for (int i = 0; i < this.number_of_states; i++)
      {
        FredParameters.GetParameter($"{this.name}[{i}].name", ref stateName);
        this.state_name.Add(stateName);
      }

      // age map
      this.age_map = new Age_Map(this.name);
      this.age_map.read_from_input(this.name);
      this.age_groups = this.age_map.get_groups();

      // create initial distribution for each age group
      state_initial_percent = new double[this.age_groups, this.number_of_states];
      for (int group = 0; group < this.age_groups; group++)
      {
        double initial_total = 0.0;
        for (int i = 0; i < this.number_of_states; i++)
        {
          double init_pct = 0.0;
          FredParameters.GetParameter($"{this.name}.group[{group}].initial_percent[{i}]", ref init_pct);
          this.state_initial_percent[group, i] = init_pct;
          if (i > 0)
          {
            initial_total += init_pct;
          }
        }
        // make sure state percentages add up
        this.state_initial_percent[group, 0] = 100.0 - initial_total;
        Utils.assert(this.state_initial_percent[group, 0] >= 0.0);
      }

      // get time period for transition probabilities
      FredParameters.GetParameter($"{this.name}_period_in_transition_probabilities", ref this.period_in_transition_probabilities);

      // initialize transition matrices, one for each age group
      for (int group = 0; group < this.age_groups; group++)
      {
        this.transition_matrix.Add(new double[this.number_of_states, this.number_of_states]);

        for (int i = 0; i < this.number_of_states; i++)
        {
          for (int j = 0; j < this.number_of_states; j++)
          {
            // default value if not in params file:
            double prob = 0.0;
            FredParameters.GetParameter($"{this.name}.group[{group}].trans[{i}][{j}]", ref prob);
            this.transition_matrix[group][i, j] = prob;
          }
        }

        // guarantee probability distribution by making same-state transition the default
        for (int i = 0; i < this.number_of_states; i++)
        {
          double sum = 0;
          for (int j = 0; j < this.number_of_states; j++)
          {
            if (i != j)
            {
              sum += this.transition_matrix[group][i, j];
            }
          }
          Utils.assert(sum <= 1.0);
          this.transition_matrix[group][i, i] = 1.0 - sum;
        }
      }
    }

    public void print()
    {
      for (int i = 0; i < this.number_of_states; i++)
      {
        Console.WriteLine("MARKOV MODEL {0}[{1}].name = {2}",
         this.name, i, this.state_name[i]);
      }

      for (int g = 0; g < this.age_groups; g++)
      {
        for (int i = 0; i < this.number_of_states; i++)
        {
          Console.WriteLine("MARKOV MODEL {0}.group[{1}].initial_percent[{2}] = {3}",
           this.name, g, i, this.state_initial_percent[g, i]);
        }

        for (int i = 0; i < this.number_of_states; i++)
        {
          for (int j = 0; j < this.number_of_states; j++)
          {
            Console.WriteLine("MARKOV MODEL {0}.group[{1}].trans[{2}][{3}] = {4}",
                   this.name, g, i, j, this.transition_matrix[g][i, j]);
          }
        }
      }
    }

    public string get_name()
    {
      return this.name;
    }

    public int get_number_of_states()
    {
      return this.number_of_states;
    }

    public string get_state_name(int i)
    {
      return this.state_name[i];
    }

    public int get_initial_state(double age)
    {
      int group = Convert.ToInt32(this.age_map.find_value(age));
      double r = 100.0 * FredRandom.NextDouble();
      double sum = 0.0;
      for (int i = 0; i < this.number_of_states; i++)
      {
        sum += this.state_initial_percent[group, i];
        if (r < sum)
        {
          return i;
        }
      }
      Utils.assert(r < sum);
      return -1;
    }

    public void get_next_state_and_time(int day, double age, int old_state, out int new_state, out int transition_day)
    {
      transition_day = -1;
      new_state = old_state;
      int group = Convert.ToInt32(this.age_map.find_value(age));
      for (int j = 0; j < this.number_of_states; j++)
      {
        if (j == old_state)
        {
          continue;
        }
        double lambda = this.transition_matrix[group][old_state, j];
        if (lambda == 0.0)
        {
          continue;
        }
        int t = day + 1 + Convert.ToInt32(Math.Round(FredRandom.Exponential(lambda) * this.period_in_transition_probabilities));
        if (transition_day < 0 || t < transition_day)
        {
          transition_day = t;
          new_state = j;
        }
      }
    }

    public int get_age_group(double age)
    {
      return Convert.ToInt32(this.age_map.find_value(age));
    }
  }
}
