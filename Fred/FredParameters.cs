using System;
using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public static class FredParameters
  {
    private static Dictionary<string, string> _Parameters = new Dictionary<string, string>();

    public static void read_parameters(string file)
    {
      if (!File.Exists(file))
      {
        Utils.fred_abort("Parameter file does not exist!");
      }
      string line;
      string key;
      string value;
      using var reader = new StreamReader(file);
      while(reader.Peek() != -1)
      {
        line = reader.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
          continue;
        }

        var tokens = line.Split('=');
        if (tokens.Length != 2)
        {
          Utils.FRED_VERBOSE(0, "Strange parameter: {0}", line);
          continue;
        }

        key = tokens[0].Trim();
        value = tokens[1].Trim();
        if (_Parameters.ContainsKey(key))
        {
          Utils.FRED_VERBOSE(0, "Duplicate parameter: {0}", line);
          continue;
        }

        if (value.EndsWith(';'))
        {
          value = value.Substring(0, value.Length - 1);
        }
        _Parameters.Add(key, value);
      }
    }

    public static bool get_indexed_param<T>(string key, int index, ref T value) where T : IConvertible
    {
      return GetParameter($"{key}[{index}]", ref value);
    }

    public static bool get_double_indexed_param<T>(string key, int index1, int index2, ref T value) where T : IConvertible
    {
      return GetParameter($"{key}[{index1}][{index2}]", ref value);
    }

    public static bool get_indexed_param<T>(string key, string index, ref T value) where T : IConvertible
    {
      return GetParameter($"{key}_{index}", ref value);
    }

    public static bool GetParameter<T>(string key, ref T value) where T : IConvertible
    {
      if (!_Parameters.ContainsKey(key))
      {
        value = default;
        return false;
      }

      var storedValue = _Parameters[key];
      value = (T)Convert.ChangeType(storedValue, typeof(T));
      return true;
    }

    public static List<T> get_indexed_param_vector<T>(string key, int index) where T : IConvertible
    {
      return GetParameterList<T>($"{key}[{index}]");
    }

    public static List<T> get_indexed_param_vector<T>(string key, string index) where T : IConvertible
    {
      return GetParameterList<T>($"{key}_{index}");
    }

    public static List<T> GetParameterList<T>(string key) where T : IConvertible
    {
      if (!_Parameters.ContainsKey(key))
      {
        return new List<T>();
      }

      var list = _Parameters[key];
      return ParseList<T>(list);
    }

    public  static List<T> ParseList<T>(string list) where T : IConvertible
    {
      var value = new List<T>();
      var data = list.Split(' ');
      if (data.Length == 1 && data[0] == "0")
      {
        return value;
      }

      if (data.Length < 2)
      {
        Utils.fred_abort("List was in incorrect format - {0}", list);
      }

      // Start at 1, the first index in the file is the length of the array.
      for (int i = 1; i < data.Length; i++)
      {
        string item = data[i];
        value.Add((T)Convert.ChangeType(item, typeof(T)));
      }

      return value;
    }

    public static T[,] GetParameterMatrix<T>(string key) where T : IConvertible
    {
      if (!_Parameters.ContainsKey(key))
      {
        return default;
      }

      var list = _Parameters[key];
      var data = list.Split(' ');
      var length = Convert.ToInt32(data[0]);
      if (data.Length != length + 1)
      {
        Utils.fred_abort("Invalid matrix configuration - {0}", key);
      }

      var count = 1;
      var bounds = Convert.ToInt32(Math.Sqrt(length));
      var value = new T[bounds, bounds];
      for (int a = 0; a < bounds; a++)
      {
        for (int b = 0; b < bounds; b++)
        {
          value[a, b] = (T)Convert.ChangeType(data[count], typeof(T));
          count++;
        }
      }

      return value;
    }

    public static bool does_param_exist(string key)
    {
      return _Parameters.ContainsKey(key);
    }
  }
}
