using System;
using System.Collections.Generic;

namespace Fred
{
  public static class FredParameters
  {
    private static Dictionary<string, string> _Parameters = new Dictionary<string, string>();

    public static void LoadParameters()
    {
    }

    public static bool GetParameter<T>(string key, ref T value) where T : IConvertible
    {
      if (!_Parameters.ContainsKey(key))
      {
        value = default;
        return false;
      }

      value = (T)Convert.ChangeType(_Parameters[key], typeof(T));
      return true;
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
      foreach (var item in data)
      {
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
      if (data.Length != length * length + 1)
      {
        Utils.fred_abort("Invalid matrix configuration - {0}", key);
      }
      var count = 1;
      var value = new T[length, length];
      for (int a = 0; a < length; a++)
      {
        for (int b = 0; b < length; b++)
        {
          value[a, b] = (T)Convert.ChangeType(data[count], typeof(T));
          count++;
        }
      }

      return value;
    }
  }
}
