using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fred
{
  /**
   * The Tracker Class is a class that contains maps that allow one to 
   * log on a daily basis different counts of things throughout FRED
   *
   * It basically contains three maps for integers, doubles, and strings
   * and stores a key, and then allows you to log daily values.
   */
  // TODO: re-implement with the counts being members of a template class
  public class Tracker<T> where T : struct
  {
    private string title;
    private string index_name;
    private readonly List<T> indices = new List<T>();
    private readonly Dictionary<string, List<int>> values_map_int = new Dictionary<string, List<int>>();
    private readonly Dictionary<string, List<string>> values_map_string = new Dictionary<string, List<string>>();
    private readonly Dictionary<string, List<double>> values_map_double = new Dictionary<string, List<double>>();
    private readonly static List<string> allowed_typenames= new List<string> { "double", "int", "string" };

    public Tracker()
    {
      this.title = "Tracker";
      this.index_name = "Generic Index";
    }

    /**
     * Constructor with a Title
     */
    public Tracker(string _title, string _index_name)
    {
      this.title = _title;
      this.index_name = _index_name;
    }

    // Accessors
    public string get_title()
    {
      return this.title;
    }

    public string get_index_name()
    {
      return this.index_name;
    }

    public T get_index_value(int i)
    {
      return this.indices[i];
    }

    public bool is_allowed_type(string type_name)
    {
      return allowed_typenames.Contains(type_name);
    }

    public string has_key(string key)
    {
      var aTypes = allowed_typenames;
      for (int i = 0; i < aTypes.Count; ++i)
      {
        var keys = this._get_keys(aTypes[i]);
        if (keys.Contains(key))
        {
          return aTypes[i];
        }
      }
      return "None";
    }

    // Modifiers

    // A new index adds an element to each array for each existing category
    public int add_index(T index, bool unique = true, bool hardfail = false)
    {
      if (unique)
      {
        if (this._index_pos(index) != -1)
        {
          if (hardfail)
          {
            Utils.fred_abort("Tracker.h::add_index Trying to add an Index that already exists (hardfail set to true)");
          }
          else
          {
            return this._index_pos(index);
          }
        }
      }

      {
        this.indices.Add(index);
        var aTypes = _get_allowed_typenames();
        for (int i = 0; i < aTypes.Count; ++i)
        {
          this._add_new_index(aTypes[i]);
        }
      }
      return this.indices.Count - 1;
    }

    public void add_key(string key_name, string TypeName)
    {
      {
        // Check if key exists
        if (has_key(key_name) != "None")
        {
          Utils.fred_abort("Tracker.h::add_key::Key %s already exists as Type %s\n",
          key_name, has_key(key_name));
        }
        this._add_new_key(key_name, TypeName);
      }
    }

    /// STB make sure OMP is taken care of in these.
    public void set_index_key_pair(T index, string key_name, int value, bool allow_add = true)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        if (allow_add)
        {
          index_position = this.add_index(index);
        }
        else
        {
          Utils.fred_abort("Tracker.h::set)index_key_pair(int) there is no index {0}", index);
        }
      }

      string key_type = this.has_key(key_name);
      if (key_type != "int")
      {
        if (allow_add && key_type == "None")
        {
          this.add_key(key_name, "int");
        }
        else
        {
          Utils.fred_abort("Tracker.h::set_index_key_pair with int, using a key that is not for integers");
        }
      }
      this.values_map_int[key_name][index_position] = value;
    }

    public void set_index_key_pair(T index, string key_name, double value, bool allow_add = true)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        if (allow_add)
        {
          index_position = this.add_index(index);
        }
        else
        {
          Utils.fred_abort("Tracker.h::set_index_key_pair (double) there is no index {0}", index);
        }
      }
      string key_type = this.has_key(key_name);
      if (key_type != "double")
      {
        if (allow_add && key_type == "None")
        {
          this.add_key(key_name, "double");
        }
        else
        {
          Utils.fred_abort("Tracker.h::set_index_key_pair with double, using a key that is not for integers");
        }
      }
      this.values_map_double[key_name][index_position] = value;
    }

    public void set_index_key_pair(T index, string key_name, string value, bool allow_add = true)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        if (allow_add)
        {
          index_position = this.add_index(index);
        }
        else
        {
          Utils.fred_abort("Tracker.h::set_index_key_pair (string) there is no index {0}", index);
        }
      }

      string key_type = this.has_key(key_name);
      if (key_type != "string")
      {
        if (allow_add && key_type == "None")
        {
          this.add_key(key_name, "string");
        }
        else
        {
          Utils.fred_abort("Tracker.h::set_index_key_pair with string, using a key that is not for integers");
        }
      }
      this.values_map_string[key_name][index_position] = value;
    }

    public void increment_index_key_pair(T index, string key_name, int value)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair there is no index {0}", index);
      }

      string key_type = this.has_key(key_name);
      if (key_type == "None")
      {
        this.add_key(key_name, "int");
        key_type = "int";
      }

      if (key_type != "int")
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair, (int) trying to increment a key %s with non integer type\n", key_name);
      }
      this.values_map_int[key_name][index_position] += value;
    }

    public void increment_index_key_pair(T index, string key_name, double value)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair there is no index {0}", index);
      }

      string key_type = this.has_key(key_name);
      if (key_type == "None")
      {
        this.add_key(key_name, "double");
        key_type = "double";
      }


      if (key_type != "double")
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair, (double) trying to increment a key %s with non double type\n", key_name);
      }
      this.values_map_double[key_name][index_position] += value;
    }

    public void increment_index_key_pair(T index, string key_name, string value)
    {
      Utils.fred_abort("Tracker.h::increment_index_key_pair, trying to increment a key %s with string type\n", key_name);
    }

    public void increment_index_key_pair(T index, string key_name)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair there is no index {0}", index);
      }

      string key_type = this.has_key(key_name);
      if (key_type == "string")
      {
        this.increment_index_key_pair(index, key_name, "FooBar");
      }
      else if (key_type == "int")
      {
        this.increment_index_key_pair(index, key_name, 1);
      }
      else if (key_type == "double")
      {
        this.increment_index_key_pair(index, key_name, 1.0);
      }
      else
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair, trying to increment a type name that doesn't exist");
      }
    }

    //Collective Operations
    public void reset_index_all_key_pairs_to_zero(T index)
    {
      int index_position = this._index_pos(index);
      if (index_position == -1)
      {
        Utils.fred_abort("Tracker.h::increment_index_key_pair there is no index {0}", index);
      }

      //for (int a = 0; a < this.values_map_int.Count; a++)
      foreach (var kvp in this.values_map_int)
      {
        kvp.Value[index_position] = 0;
      }

      foreach (var kvp in this.values_map_double)
      {
        kvp.Value[index_position] = 0;
      }
    }


    public void reset_all_index_key_pairs_to_zero()
    {
      for (int i = 0; i < this.indices.Count; ++i)
      {
        reset_index_all_key_pairs_to_zero(this.indices[i]);
      }
    }

    public void set_all_index_for_key(string key_name, int value)
    {
      for (int i = 0; i < this.indices.Count; ++i)
      {
        string key_type = this.has_key(key_name);
        if (key_type != "int")
        {
          Utils.fred_abort("Tracker.h::set_all_index_for_key, called with an integer and key {0} is not an integer value", key_name);
        }
        this.set_index_key_pair(this.indices[i], key_name, value, false);
      }
    }

    public void set_all_index_for_key(string key_name, double value)
    {
      for (int i = 0; i < this.indices.Count; ++i)
      {
        string key_type = this.has_key(key_name);
        if (key_type != "double")
        {
          Utils.fred_abort("Tracker.h::set_all_index_for_key, called with an double and key %s is not an double value", key_name);
        }
        this.set_index_key_pair(this.indices[i], key_name, value, false);
      }
    }

    public void set_all_index_for_key(string key_name, string value)
    {
      for (int i = 0; i < this.indices.Count; ++i)
      {
        string key_type = this.has_key(key_name);
        if (key_type != "string")
        {
          Utils.fred_abort("Tracker.h::set_all_index_for_key, called with an string and key %s is not an string value", key_name);
        }
        this.set_index_key_pair(this.indices[i], key_name, value, false);
      }
    }

    //Printers
    public string print_key_table()
    {
      var sList = new StringBuilder();
      sList.AppendLine("Key Table");
      sList.AppendLine("---------------------------------");
      var atypes = this._get_allowed_typenames();
      for (int i = 0; i < atypes.Count; ++i)
      {
       var keys = this._get_keys(atypes[i]);
        if (keys.Count > 0)
        {
          sList.Append($"  {atypes[i]} Keys");
          for (int j = 0; j < keys.Count; ++j)
          {
            sList.Append($"\t{keys[j]}");
          }
        }
      }
      return sList.ToString();
    }

    public string print_key_index_list(string key_name)
    {
      var returnString = new StringBuilder();
      returnString.AppendLine($"Key: {key_name}");
      returnString.AppendLine("--------------------------------------");
      returnString.AppendLine("Index\t\tValue");

      string key_type = this.has_key(key_name);
      // Can't figure out how to not do this explicitly yet
      if (key_type == "None")
      {
        Utils.fred_abort("Tracker.h::print_key_index_list requesting a key %s that does not exist\n", key_name);
      }
      else if (key_type == "int")
      {
        if (this.indices.Count != this.values_map_int[key_name].Count)
        {
          Utils.fred_abort("Tracker.h::print_key_index_list there is something wrong with the counts number of indices != number of values for key %s\n",
                key_name);
        }

        for (int i = 0; i < this.indices.Count; ++i)
        {
          returnString.Append($"{this.indices[i]}\t\t{this.values_map_int[key_name][i]}");
        }
      }
      else if (key_type == "double")
      {
        if (this.indices.Count != values_map_double[key_name].Count)
        {
          Utils.fred_abort("Tracker.h::print_key_index_list there is something wrong with the counts number of indices != number of values for key %s\n",
                key_name);
        }

        for (int i = 0; i < this.indices.Count; ++i)
        {
          returnString.Append($"{this.indices[i]}\t\t{this.values_map_double[key_name][i]}");
        }
      }
      else if (key_type == "string")
      {
        if (this.indices.Count != this.values_map_string[key_name].Count)
        {
          Utils.fred_abort("Tracker.h::print_key_index_list there is something wrong with the counts number of indices != number of values for key %s\n",
                key_name);
        }

        for (int i = 0; i < this.indices.Count; ++i)
        {
          returnString.Append($"{this.indices[i]}\t\t{this.values_map_string[key_name][i]}");
        }
      }
      else
      {
        Utils.fred_abort("Tracker.h::print_key_index_list called with an unrecognized typename for key %s",
        key_name);
      }

      returnString.Append("--------------------------------------");

      return returnString.ToString();
    }

    public string print_inline_report_format_for_index(T index)
    {
      int index_pos = this._index_pos(index);
      if (index_pos == -1)
      {
        Utils.fred_abort("Tracker.h::print_inline_report_format_for_index asked for index that does not exist");
      }
      var returnStringSt = new StringBuilder();
      returnStringSt.Append($"{this.index_name} {index} ");
      foreach (var kvp in this.values_map_string)
      {
        returnStringSt.Append($"{kvp.Key} {kvp.Value[index_pos]} ");
      }

      foreach (var kvp in this.values_map_int)
      {
        returnStringSt.Append($"{kvp.Key} {kvp.Value[index_pos]} ");
      }

      foreach (var kvp in this.values_map_double)
      {
        returnStringSt.Append($"{kvp.Key} {kvp.Value[index_pos]} ");
      }

      returnStringSt.AppendLine();
      return returnStringSt.ToString();
    }

    public void output_inline_report_format_for_index(T index, TextWriter stream)
    {
      stream.WriteLine(print_inline_report_format_for_index(index));
    }

    //public void output_inline_report_format_for_index(T index, TextWriter stream)
    //{
    //  stream.WriteLine(print_inline_report_format_for_index(index));
    //  stream.Flush();
    //}

    public string print_inline_report_format()
    {
      var returnString = new StringBuilder();

      for (int i = 0; i < this.indices.Count; ++i)
      {
        returnString.AppendLine(print_inline_report_format_for_index(this.indices[i]));
      }
      return returnString.ToString();
    }
    void output_inline_report_format(TextWriter stream)
    {
      stream.WriteLine(print_inline_report_format());
    }

    //public void output_inline_report_format(TextWriter stream)
    //{
    //  fprintf(outfile, "%s", print_inline_report_format());
    //  fflush(outfile);
    //}

    public string print_csv_report_format_for_index(T index)
    {
      int index_pos = this._index_pos(index);
      if (index_pos == -1)
      {
        Utils.fred_abort("Tracker.h::print_csv_report_format_for_index asked for index that does not exist");
      }

      var returnString = new StringBuilder();
      returnString.Append(index);
      foreach (var kvp in this.values_map_string)
      {
        returnString.Append($",{kvp.Value[index_pos]}");
      }

      foreach (var kvp in this.values_map_int)
      {
        returnString.Append($",{kvp.Value[index_pos]}");
      }

      foreach (var kvp in this.values_map_double)
      {
        returnString.Append($",{kvp.Value[index_pos]}");
      }

      returnString.AppendLine();
      return returnString.ToString();
    }

    public void output_csv_report_format_for_index(T index, TextWriter stream)
    {
      stream.WriteLine(print_csv_report_format_for_index(index));
    }

    //public void output_csv_report_format_for_index(T index, FILE* outfile)
    //{
    //  fprintf(outfile, "%s", print_csv_report_format_for_index(index));
    //  fflush(outfile);
    //}

    public string print_csv_report_format_header()
    {
      var returnString = new StringBuilder();
      returnString.Append(this.index_name);
      foreach (var kvp in this.values_map_string)
      {
        returnString.Append($",{kvp.Key}");

      }

      foreach (var kvp in this.values_map_int)
      {
        returnString.Append($",{kvp.Key}");
      }

      foreach (var kvp in this.values_map_double)
      {
        returnString.Append($",{kvp.Key}");
      }

      returnString.AppendLine();
      return returnString.ToString();
    }

    public void output_csv_report_format_header(TextWriter outfile)
    {
      outfile.WriteLine(print_csv_report_format_header());
      outfile.Flush();
    }

    public void output_csv_report_format(TextWriter outfile)
    {
      output_csv_report_format_header(outfile);
      for (int i = 0; i < this.indices.Count; ++i)
      {
        output_csv_report_format_for_index(this.indices[i], outfile);
      }
      outfile.Flush();
    }

    private List<string> _get_allowed_typenames()
    {
      return allowed_typenames;
    }

    private void _add_new_index(string TypeName)
    {
      if (this.is_allowed_type(TypeName) == false)
      {
        Utils.fred_abort("Tracker.h::_add_new_index has been called with unsupported TypeName %s, use double, int, or string\n",
        TypeName);
      }

      if (TypeName == "int")
      {
        if (this.values_map_int.Count > 0)
        {
          foreach (var kvp in this.values_map_int)
          {
            if (kvp.Value == null)
            {
              this.values_map_int[kvp.Key] = new List<int> { 0 };
            }
            else
            {
              kvp.Value.Add(0);
            }
          }
        }
      }
      else if (TypeName == "double")
      {
        if (this.values_map_double.Count > 0)
        {
          foreach (var kvp in this.values_map_double)
          {
            if (kvp.Value == null)
            {
              this.values_map_double[kvp.Key] = new List<double> { 0.0 };
            }
            else
            {
              kvp.Value.Add(0);
            }
          }
        }
      }
      else if (TypeName == "string")
      {
        if (this.values_map_string.Count > 0)
        {
          foreach (var kvp in this.values_map_string)
          {
            if (kvp.Value == null)
            {
              this.values_map_string[kvp.Key] = new List<string> { " " };
            }
            else
            {
              kvp.Value.Add(" ");
            }
          }
        }
      }
      else
      {
        Utils.fred_abort("Tracker.h::add_new_index has been called with unsupported TypeName %s, use double, int, or string\n",
        TypeName);
      }
    }

    private void _add_new_key(string key_name, string TypeName)
    {
      if (this.is_allowed_type(TypeName) == false)
      {
        Utils.fred_abort("Tracker.h::_add_new_keys has been called with unsupported TypeName %s, use double, int, or string\n",
        TypeName);
      }

      if (TypeName == "int")
      {
        for (int i = 0; i < this.indices.Count; ++i)
        {
          this.values_map_int.Add(key_name, new List<int> { 0 });//[key_name].Add(0);
        }
      }
      else if (TypeName == "double")
      {
        for (int i = 0; i < this.indices.Count; ++i)
        {
          this.values_map_double.Add(key_name, new List<double> { 0.0 });//[key_name].Add(0.0);
        }
      }
      else if (TypeName == "string")
      {
        for (int i = 0; i < this.indices.Count; ++i)
        {
          this.values_map_string.Add(key_name, new List<string> { "A String" });//[key_name].Add("A String");
        }
      }
      else
      {
        Utils.fred_abort("Tracker.h::_add_new_key got a type name %s for key %s it doesn't know how to handle (use int,double,or string)",
        key_name, TypeName);
      }
    }

    private int _index_pos(T index)
    {
      //typename vector<T>::iterator iter_index;
      //return index;
      //iter_index = find(this.indices.begin(), this.indices.end(), index);
      return this.indices.IndexOf(index);
      //var iter_index = this.indices.FirstOrDefault(i => i == index);
      //if (iter_index != null)
      //{
      //  return iter_index;
      //  //return distance(this.indices.begin(), iter_index);
      //}
      //else
      //{
      //  return -1;
      //}
    }

    private List<string> _get_keys(string TypeName)
    {
      if (this.is_allowed_type(TypeName) == false)
      {
        Utils.fred_abort("Tracker.h::_get_keys has been called with unsupported TypeName %s, use double,int, or string\n",
        TypeName);
      }

      var returnVec = new List<string>();
      if (TypeName == "int")
      {
        foreach (var kvp in this.values_map_int)
        {
          returnVec.Add(kvp.Key);
        }
      }
      else if (TypeName == "double")
      {
        foreach (var kvp in this.values_map_double)
        {
          returnVec.Add(kvp.Key);
        }
      }
      else if (TypeName == "string")
      {
        foreach (var kvp in this.values_map_string)
        {
          returnVec.Add(kvp.Key);
        }
      }
      else
      {
        Utils.fred_abort("Tracker.h::_get_keys called with an unrecognized TypeName %s\n", TypeName);
      }
      return returnVec;
    }
  }
}
