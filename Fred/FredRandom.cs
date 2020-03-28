using System;
using System.Collections;
using System.Collections.Generic;

namespace Fred
{
  public static class FredRandom
  {
    private static Random s_Random = new Random();

    public static void SetSeed(int seed)
    {
      s_Random = new Random(seed);
    }

    public static int Next()
    {
      return s_Random.Next();
    }

    public static int Next(int lower, int higher)
    {
      return s_Random.Next(lower, higher);
    }

    public static double NextDouble()
    {
      return s_Random.NextDouble();
    }

    public static int NextDayInYear()
    {
      return s_Random.Next(1, 265);
    }

    /// <summary>
    /// Generates normally distributed numbers. Each operation makes two Gaussians for the price of one,
    /// and apparently they can be cached or something for better performance.
    /// </summary>
    /// <param name="r"></param>
    /// <param name = "mu">Mean of the distribution</param>
    /// <param name = "sigma">Standard deviation</param>
    /// <returns></returns>
    public static double NextGaussian(double mu = 0, double sigma = 1)
    {
      var u1 = 1.0 - s_Random.NextDouble();
      var u2 = s_Random.NextDouble();
      var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                          Math.Sin(2.0 * Math.PI * u2);
      var rand_normal = mu + sigma * rand_std_normal;
      return rand_normal;
    }

    /// <summary>
    ///   Generates values from a triangular distribution.
    /// </summary>
    /// <remarks>
    /// See http://en.wikipedia.org/wiki/Triangular_distribution for a description of the triangular probability distribution and the algorithm for generating one.
    /// </remarks>
    /// <param name="r"></param>
    /// <param name = "a">Minimum</param>
    /// <param name = "b">Maximum</param>
    /// <param name = "c">Mode (most frequent value)</param>
    /// <returns></returns>
    public static double NextTriangular(double a, double b, double c)
    {
      var u = s_Random.NextDouble();

      return u < (c - a) / (b - a)
                 ? a + Math.Sqrt(u * (b - a) * (c - a))
                 : b - Math.Sqrt((1 - u) * (b - a) * (b - c));
    }

    /// <summary>
    ///   Equally likely to return true or false. Uses <see cref="Random.Next()"/>.
    /// </summary>
    /// <returns></returns>
    public static bool NextBoolean()
    {
      return s_Random.Next(2) > 0;
    }

    /// <summary>
    ///   Shuffles a list in O(n) time by using the Fisher-Yates/Knuth algorithm.
    /// </summary>
    /// <param name="r"></param>
    /// <param name = "list"></param>
    public static void Shuffle(IList list)
    {
      for (var i = 0; i < list.Count; i++)
      {
        var j = s_Random.Next(0, i + 1);

        var temp = list[j];
        list[j] = list[i];
        list[i] = temp;
      }
    }

    /// <summary>
    /// Returns n unique random numbers in the range [1, n], inclusive. 
    /// This is equivalent to getting the first n numbers of some random permutation of the sequential numbers from 1 to max. 
    /// Runs in O(k^2) time.
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="n">Maximum number possible.</param>
    /// <param name="k">How many numbers to return.</param>
    /// <returns></returns>
    public static int[] Permutation(int n, int k)
    {
      var result = new List<int>();
      var sorted = new SortedSet<int>();

      for (var i = 0; i < k; i++)
      {
        var r = s_Random.Next(1, n + 1 - i);

        foreach (var q in sorted)
          if (r >= q) r++;

        result.Add(r);
        sorted.Add(r);
      }

      return result.ToArray();
    }

    public static int DrawFromDistribution(int n, IList<double> dist)
    {
      var r = NextDouble();
      int i = 0;
      while (i <= n && dist[i] < r)
      {
        i++;
      }

      if (i <= n)
      {
        return i;
      }

      Console.Error.WriteLine("Help! draw from distribution failed.");
      Console.Error.WriteLine("Is distribution properly formed? (should end with 1.0)");
      for (var a = 0; a <= n; a++)
      {
        Console.Error.WriteLine("{0} ", dist[a]);
      }
      Console.Error.WriteLine();
      return -1;
    }

    public static double Exponential(double lambda)
    {
      return -Math.Log(NextDouble()) / lambda;
    }

    public static double Normal(double mu, double sigma)
    {
      return NextGaussian(mu, sigma);
    }

    public static double LogNormal(double mu, double sigma)
    {
      double z = Normal(0.0, 1.0);
      return Math.Exp(mu + sigma * z);
    }


    public static int DrawFromCdf(IList<double> v, int size)
    {
      double r = NextDouble();
      int top = size - 1;
      int bottom = 0;
      int s = top / 2;
      while (bottom <= top)
      {
        if (r <= v[s])
        {
          if (s == 0 || r > v[s - 1])
          {
            return s;
          }
          else
          {
            top = s - 1;
          }
        }
        else
        { // r > v[s]
          if (s == size - 1)
          {
            return s;
          }
          if (r < v[s + 1])
          {
            return s + 1;
          }
          else
          {
            bottom = s + 1;
          }
        }
        s = bottom + (top - bottom) / 2;
      }
      // assert(bottom <= top);
      return -1;
    }

    public static int DrawFromCdfVector(IList<double> v) {
      int size = v.Count;
      double r = NextDouble();
      int top = size - 1;
      int bottom = 0;
      int s = top / 2;
      while(bottom <= top) {
        if(r <= v[s]) {
          if(s == 0 || r > v[s - 1]) {
            return s;
          } else {
            top = s - 1;
          }
        } else { // r > v[s]
          if(s == size - 1) {
            return s;
          }
          if(r<v[s + 1]) {
            return s + 1;
          } else {
            bottom = s + 1;
          }
        }
        s = bottom + (top - bottom) / 2;
      }
      return -1;
    }

    public static double BinomialCoefficient(int n, int k)
    {
      if (k < 0 || k > n)
      {
        return 0;
      }

      if (k > n - k)
      {
        k = n - k;
      }

      double c = 1.0;
      for (int i = 0; i < k; ++i)
      {
        c *= n - (k - (i + 1));
        c /= i + 1;
      }
      return c;
    }

    public static List<double> BuildBinomialCdf(double p, int n)
    {
      var cdf = new List<double>();
      for (int i = 0; i <= n; ++i)
      {
        double prob = 0.0;
        for (int j = 0; j <= i; ++j)
        {
          prob += BinomialCoefficient(n, i) * Math.Pow(10, (i * Math.Log10(p)) + ((n - 1) * Math.Log10(1 - p)));
        }
        if (i > 0)
        {
          prob += cdf[^1];
        }
        if (prob < 1)
        {
          cdf.Add(prob);
        }
        else
        {
          cdf.Add(1.0);
          break;
        }
      }

      cdf[^1] = 1.0;
      return cdf;
    }

    public static List<double> BuildLognormalCdf(double mu, double sigma)
    {
      var cdf = new List<double>();
      int maxval = -1;
      var count = new int[1000];
      for (int i = 0; i < 1000; i++)
      {
        count[i] = 0;
      }
      for (int i = 0; i < 1000; i++)
      {
        double x = LogNormal(mu, sigma);
        int j = Convert.ToInt32(x + 0.5);
        if (j > 999)
        {
          j = 999;
        }
        count[j]++;
        if (j > maxval)
        {
          maxval = j;
        }
      }
      for (int i = 0; i <= maxval; ++i)
      {
        double prob = (double)count[i] / 1000.0;
        if (i > 0)
        {
          prob += cdf[^1];
        }
        if (prob < 1.0)
        {
          cdf.Add(prob);
        }
        else
        {
          cdf.Add(1.0);
          break;
        }
      }

      cdf[^1] = 1.0;
      return cdf;
    }

    public static List<int> SampleRangeWithoutReplacement(int length, int s)
    {
      var result = new List<int>(s);
      var selected = new List<bool>(length);
      for (int a = 0; a < s; ++a)
      {
        int i = s_Random.Next(0, length - 1);
        if (selected[i])
        {
          if (i < length - 1 && !selected[i + 1])
          {
            ++i;
          }
          else if (i > 0 && !(selected[i - 1]))
          {
            --i;
          }
          else
          {
            --a;
            continue;
          }
        }

        selected[i] = true;
        result[a] = i;
      }

      return result;
    }
  }
}
