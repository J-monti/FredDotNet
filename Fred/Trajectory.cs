using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Trajectory
  {
    private int duration;
    private List<double> symptomaticity = new List<double>();
    private Dictionary<int, List<double>> infectivity = new Dictionary<int, List<double>>();

    public Trajectory()
    {
      duration = 0;
    }

    public Trajectory(Dictionary<int, List<double>> infectivity_copy, List<double> symptomaticity_copy)
    {
      duration = 0;
      foreach(var kvp in infectivity_copy)
      {
        infectivity.Add(kvp.Key, new List<double>(kvp.Value));
        if (kvp.Value.Count > duration)
        {
          duration = kvp.Value.Count;
        }
      }

      foreach (var value in symptomaticity_copy)
      {
        symptomaticity.Add(value);
      }

      if (symptomaticity.Count > duration)
      {
        duration = symptomaticity.Count;
      }
    }

    /**
     * Create a copy of this Trajectory and return a pointer to it
     * @return a pointer to the new Trajectory
     */
    public Trajectory clone()
    {
      return new Trajectory(this.infectivity, this.symptomaticity);
    }

    /**
     * @param strain the strain to check for
     * @return <code>true</code> if the Trajectory contains the strain, <code>false</code> if not
     */
    public bool contains(int strain)
    {
      return this.infectivity.ContainsKey(strain);
    }

    public List<double> get_infectivity_trajectory(int strain)
    {
      return infectivity[strain];
    }

    public List<double> get_symptomaticity_trajectory()
    {
      return symptomaticity;
    }

    public void get_all_strains(List<int> strains)
    {
      strains = new List<int>();
      foreach (var kvp in this.infectivity)
      {
        strains.Add(kvp.Key);
      }
    }

    public void set_symptomaticity_trajectory(List<double> symt)
    {
      symptomaticity = symt;
      if (duration < (int)symptomaticity.Count)
      {
        duration = (int)symptomaticity.Count;
      }
    }

    public void set_infectivity_trajectory(int strain, List<double> inf)
    {
      if (!infectivity.ContainsKey(strain))
      {
        infectivity.Add(strain, inf);
      }
      else
      {
        infectivity[strain] = inf;
      }

      if (duration < (int)infectivity[strain].Count)
      {
        duration = infectivity[strain].Count;
      }
    }

    public void set_infectivities(Dictionary<int, List<double>> inf)
    {
      this.infectivity = inf;
    }

    public Dictionary<int, double> get_current_loads(int day)
    {
      var result = new Dictionary<int, double>();
      foreach (var kvp in this.infectivity)
      {
        result.Add(kvp.Key, kvp.Value[day]);
      }

      return result;
    }

    public Dictionary<int, double> getInoculum(int day)
    {
      return get_current_loads(day);
    }

    public int get_duration()
    {
      return duration;
    }

    public TrajectoryPoint get_data_point(int t)
    {
      double point_infectivity = 0.0;
      double point_symptomaticity = 0.0;

      if (duration > t)
      {
        foreach(var kvp in this.infectivity)
        {
          if (kvp.Value.Count > t)
          {
            point_infectivity += kvp.Value[t];
          }
        }

        if ((int)symptomaticity.Count > t)
        {
          point_symptomaticity += symptomaticity[t];
        }
      }

      return new TrajectoryPoint(point_infectivity, point_symptomaticity);
    }

    public void modify_symp_period(int startDate, int days_left)
    {
      // symptomaticty and infectivity trajectories become 0 after startDate + days_left
      //int end_date = startDate + days_left;

      //if (end_date > (int)symptomaticity.Count)
      //{
      //  return;
      //}

      //symptomaticity.resize(end_date, 1);
      //map<int, trajectory_t> :: iterator it;

      //foreach (var kvp in this.infectivity)
      //{
      //  (it->second).resize(end_date, 1);
      //}
    }

    public void modify_asymp_period(int startDate, int days_left, int sympDate)
    {
      //int end_date = startDate + days_left;

      //// if decreasing the asymp period
      //if (end_date < sympDate)
      //{
      //  for (int i = startDate, j = sympDate; i < (int)symptomaticity.Count; i++, j++)
      //  {
      //    symptomaticity[i] = symptomaticity[j];
      //  }

      //  symptomaticity.resize(symptomaticity.Count - sympDate + days_left, 0);
      //  map<int, trajectory_t> :: iterator it;

      //  for (it = infectivity.begin(); it != infectivity.end(); it++)
      //  {
      //    trajectory_t & inf = it->second;

      //    for (int i = startDate, j = sympDate; i < (int)inf.size(); i++, j++)
      //    {
      //      inf[i] = inf[j];
      //    }

      //    inf.resize(symptomaticity.size() - sympDate + days_left, 0);
      //  }
      //}
      //// if increasing the asymp period
      //else
      //{
      //  int days_extended = end_date - sympDate;
      //  trajectory_t::iterator it = symptomaticity.begin();

      //  for (int i = 0; i < sympDate; i++) it++;

      //  symptomaticity.insert(it, days_extended, 0.0);

      //  map<int, trajectory_t> :: iterator inf_it;

      //  for (inf_it = infectivity.begin(); inf_it != infectivity.end(); inf_it++)
      //  {
      //    // infectivity value for new period = infectivity just before becoming symptomatic
      //    //  == asymp_infectivity for FixedIntraHost
      //    trajectory_t & inf = inf_it->second;
      //    it = inf.begin();

      //    for (int i = 0; i < sympDate; i++) it++;

      //    inf.insert(it, days_extended, inf[sympDate - 1]);
      //  }
      //}
    }

    public void modify_develops_symp(int sympDate, int sympPeriod)
    {
      //int end_date = sympDate + sympPeriod;

      //if (end_date < (int)symptomaticity.size())
      //{
      //  symptomaticity.resize(end_date);
      //}
      //else
      //{
      //  symptomaticity.resize(end_date, 1);
      //  map<int, trajectory_t> :: iterator inf_it;

      //  for (inf_it = infectivity.begin(); inf_it != infectivity.end(); inf_it++)
      //  {
      //    trajectory_t & inf = inf_it->second;
      //    trajectory_t::iterator it = inf.end();
      //    it--;
      //    inf.insert(it, end_date - symptomaticity.size(), inf[end_date - 1]);
      //  }
      //}
    }

    public void mutate(int old_strain, int new_strain, int day)
    {
      if (infectivity.ContainsKey(new_strain))
      {
        return; // HACK
      }

      if (!infectivity.ContainsKey(old_strain))
      {
        Utils.fred_abort($"Trajectory: Strain not found! {old_strain}");
      }

      var old_inf = infectivity[old_strain];
      List<double> new_inf = new List<double>();
      if (day > old_inf.Count)
      {
        return;
      }

      for (int d = 0; d < day; ++d)
      {
        new_inf.Add(0);
      }

      for (int d = day; d < old_inf.Count; ++d)
      {
        new_inf.Add(old_inf[d]);
        old_inf[d] = 0;
      }

      infectivity.Add(new_strain, new_inf);
      //infectivity.insert( pair<int, vector<double> > (old_strain, old_inf) );
      //cout << "Mutated from " << old_strain << " to " << new_strain << " on day " << day << endl;
    }

    public void print()
    {
      Console.WriteLine(this.ToString());
    }

    public void print_alternate(TextWriter o)
    {
      //out << "Strains: ";
      //print();
    //  if (infectivity.Count == 1)
    //  {
    //    map<int, trajectory_t>::iterator it = infectivity.begin();
    //out << it->first << " " << it->second.size();
    //  }
    //  else
    //  {
    //    map<int, trajectory_t>::iterator it1 = infectivity.begin();
    //    map<int, trajectory_t>::iterator it2 = infectivity.end();
    //    int mut_day = 0;
    //    for (int i = 0; i < it2->second.size(); i++)
    //    {
    //      if (it2->second.at(i) != 0) break;
    //      mut_day++;
    //    }
    //    if (mut_day != 0)
    //    {
    //  out << it1->first << " " << mut_day << " ";
    //    }
    //out << it2->first << " " << it2->second.size() - mut_day + 1;
    //  }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("Infection Trajectories:");
      foreach (var kvp in this.infectivity)
      {
        builder.Append($" Strain {kvp.Key}:");
        foreach (var value in kvp.Value)
        {
          builder.Append($" {value}");
        }
        builder.AppendLine();
      }

      return builder.ToString();
    }
  }
}
