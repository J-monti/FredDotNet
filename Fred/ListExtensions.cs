using System.Collections;
using System.Collections.Generic;

namespace Fred
{
  public static class ListExtensions
  {
    public static void Shuffle<T>(this IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = FredRandom.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    public static void PopBack<T>(this IList<T> list)
    {
      list.RemoveAt(list.Count - 1);
    }

    public static bool any(this BitArray array)
    {
      for (int a = 0; a < array.Length; a++)
      {
        if (array[a])
        {
          return true;
        }
      }

      return false;
    }
  }
}
