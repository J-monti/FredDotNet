using System.Collections.Generic;

namespace Fred
{
  public class StrainData : List<int>
  {
    public StrainData(int capacity)
      : base (capacity)
    {
    }

    public StrainData(StrainData copy)
      : base(copy.Count)
    {
      for (int i = 0; i < copy.Count; i++)
      {
        this[i] = copy[i];
      }
    }

    public override bool Equals(object obj)
    {
      var strainData = obj as StrainData;
      if (strainData == null)
      {
        return false;
      }
      if (strainData.Count != this.Count)
      {
        return false;
      }

      for (int i = 0; i < strainData.Count; i++)
      {
        if (this[i] != strainData[i])
        {
          return false;
        }
      }

      return true;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      return string.Join(',', this);
    }
  }
}
