using System;
using System.Collections.Generic;

namespace Fred
{
  public class Age_Map
  {
    private string name;
    private List<double> ages; // vector to hold the upper age for each age group
    private List<double> values; // vector to hold the values for each age range

    /**
   * Default constructor
   */
    public Age_Map()
    {
      this.ages = new List<double>();
      this.values = new List<double>();
    }

    /**
     * Constructor that sets the Age_Map's name attribute
     *
     * @param Name the name of the Age_Map
     */
    public Age_Map(string name)
    {
      this.name = name + " Age Map";
      this.ages = new List<double>();
      this.values = new List<double>();
    }

    /**
     * @return whether or not the age group vector is empty
     */
    public bool is_empty()
    {
      return this.ages.Count == 0;
    }

    // Additional creation operations for building an Age_Map
    /**
     * @param Input a string that will be parsed to use for a parameter lookup
     */
    public void read_from_input(string input)
    {
      this.name = input + " Age Map";
      this.ages.Clear();
      this.values.Clear();
      string ages_string;
      string values_string;
      if (input.IndexOf("[") >= 0)
      {
        // Need Special parsing if this is an array from input
        // Allows Disease specific values
        int found = input.IndexOf("[");
        int found2 = input.LastIndexOf("]");
        string input_tmp = input.Substring(0, found);
        string number = input.Substring(found + 1, found2 - found - 1);
        ages_string = $"{input_tmp}_age_groups[{number}]";
        values_string = $"{input_tmp}_values[{number}]";
      }
      else
      {
        ages_string = $"{input}_age_groups";
        values_string = $"{input}_values";
      }

      this.ages = FredParameters.GetParameterList<double>(ages_string);
      this.values = FredParameters.GetParameterList<double>(values_string);

      if (quality_control() != true)
      {
        Utils.fred_abort("Bad input on age map %s", this.name);
      }
    }

    /**
     * Will concatenate an index onto the input string and then pass to <code>Age_Map::read_from_input(string Input)</code>
     *
     * @param Input a string that will be parsed to use for a parameter lookup
     * @param i an index that will be appended
     */
    public void read_from_input(string input, int i)
    {
      this.read_from_input($"{input}[{i}]");
    }

    /**
     * Will concatenate two indices onto the input string and then pass to <code>Age_Map::read_from_input(string Input)</code>
     *
     * @param Input a string that will be parsed to use for a parameter lookup
     * @param i an index that will be appended
     * @param j an index that will be appended
     */
    public void read_from_input(string input, int i, int j)
    {
      this.read_from_input($"{input}[{i}][{j}]");
    }

    public void read_from_string(string ages_string, string values_string)
    {
      this.ages = FredParameters.GetParameterList<double>(ages_string);
      this.values = FredParameters.GetParameterList<double>(values_string);
    }

    public void set_all_values(double val)
    {
      this.ages.Clear();
      this.values.Clear();
      this.ages.Add(Demographics.MAX_AGE);
      this.values.Add(val);
    }

    public List<double> get_ages()
    {
      return this.ages;
    }

    public List<double> get_values()
    {
      return this.values;
    }

    public void set_ages(List<double> input_ages)
    {
      ages = input_ages;
    }

    public void set_values(List<double> input_values)
    {
      values = input_values;
    }

    // Operations
    /**
     * Find a value given an age. Will return 0.0 if no matching range is found.
     *
     * @param (double) age the age to find
     * @return the found value
     */
    public double find_value(double age)
    {
      for (int i = 0; i < this.ages.Count; i++)
      {
        if (age < this.ages[i])
        {
          return this.values[i];
        }
      }

      return 0.0;
    }

    // Utility functions
    /**
     * Print out information about this object
     */
    public void print()
    {
      Console.WriteLine();
      Console.WriteLine(this.name);
      for (int i = 0; i < this.ages.Count; i++)
      {
        Console.WriteLine("age less than {0}: {1}", this.ages[i], this.values[i]);
      }
      Console.WriteLine();
    }

    public int get_groups()
    {
      return this.ages.Count;
    }

    /**
     * Perform validation on the Age_Map, making sure the age
     * groups are mutually exclusive.
     */
    public bool quality_control()
    {
      if (this.ages.Count != this.values.Count)
      {
        Console.WriteLine("Help! Age_Map: {0}: Must have the same number of age groups and values", this.name);
        Console.WriteLine("Number of Age Groups = {0}, Number of Values = {1}", this.ages.Count, this.values.Count);
        return false;
      }

      if (this.ages.Count > 0)
      {
        // Next check that the ages groups are correct, the low and high ages are right
        for (int i = 0; i < this.ages.Count - 1; i++)
        {
          if (this.ages[i] > this.ages[i + 1])
          {
            Console.WriteLine("Help! Age_Map: {0}: Age Group {1} invalid, low age higher than high", this.name, i);
            Console.WriteLine("Low Age = {0}, High Age = {1}", this.ages[i], this.ages[i + 1]);
            return false;
          }
        }
      }
      return true;
    }
  }
}
