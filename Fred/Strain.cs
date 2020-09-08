using System;
using System.IO;

namespace Fred
{
  public class Strain
  {
    private Strain parent;
    private int id;
    private double transmissibility;
    private Disease disease;
    private Strain_Data strain_data;

    public Strain(int num_elements)
    {
      this.strain_data = new Strain_Data(num_elements);
      this.transmissibility = -1.0;
    }

    public Strain(Strain other)
    {
      this.strain_data = new Strain_Data(other.strain_data);
      this.transmissibility = other.transmissibility;
      this.disease = other.disease;
    }

    //public void reset();

    public void setup(int strain, Disease _disease, double trans, Strain parent)
    {
      this.id = strain;
      this.disease = _disease;
      this.transmissibility = trans;
      this.parent = parent;
    }

    public void print()
    {
      Console.WriteLine("New Strain: {0}, Transmissibility: {1}", this.id, this.transmissibility);
      Console.WriteLine(this.strain_data.ToString());
    }

    public void print_alternate(TextWriter o )
    {
      int pid = -1;
      if (this.parent != null)
      {
        pid = this.parent.id;
      }
      o.WriteLine("{0}:{1}:", this.id, pid);
      o.WriteLine(this.strain_data.ToString());
    }

    public int get_id() { return id; }
    public double get_transmissibility() { return transmissibility; }
    public int get_num_data_elements() { return strain_data.Count; }
    public int get_data_element(int i) { return strain_data[i]; }
    public Strain_Data get_data() { return strain_data; }
    public Strain_Data get_strain_data() { return strain_data; }

    public override string ToString()
    {
      return this.strain_data.ToString();
    }
  }
}
