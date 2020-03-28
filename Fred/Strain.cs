using System;
using System.IO;

namespace Fred
{
  public class Strain
  {
    public Strain(int num_elements)
    {
      this.StrainData = new StrainData(num_elements);
      this.Transmissibility = -1.0;
    }

    public Strain(Strain other)
    {
      this.StrainData = new StrainData(other.StrainData);
      this.Transmissibility = other.Transmissibility;
      this.Disease = other.Disease;
    }

    public int Id { get; private set; }

    public StrainData StrainData { get; }

    public Disease Disease { get; private set; }

    public Strain Parent { get; private set; }

    public double Transmissibility { get; private set; }

    public void Setup(int strainId, Disease disease, double transmissibility, Strain parent)
    {
      this.Id = strainId;
      this.Disease = disease;
      this.Transmissibility = transmissibility;
      this.Parent = parent;
    }

    public void PrintAlternate(StreamWriter o)
    {
      int pid = -1;
      if (this.Parent != null)
      {
        pid = this.Parent.Id;
      }
      o.WriteLine("{0}:{1}:", this.Id, pid);
      o.WriteLine(this.StrainData.ToString());
    }

    public void Print()
    {
      Console.WriteLine("New Strain: {0}, Transmissibility: {1}", this.Id, this.Transmissibility);
      Console.WriteLine(this.StrainData.ToString());
    }

    public override string ToString()
    {
      return this.StrainData.ToString();
    }
  }
}
