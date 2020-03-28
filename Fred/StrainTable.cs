using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public class StrainTable
  {
    public StrainTable ()
    {
      this.Strains = new List<Strain>();
      this.StrainGenoTypeMap = new Dictionary<string, int>();
    }

    public List<Strain> Strains { get; }

    public Dictionary<string, int> StrainGenoTypeMap { get; }

    public Disease Disease { get; private set; }

    public void setup(Disease d)
    {
      this.Disease = d;
      this.Strains.Clear();
    }

    public void add_root_strain(int num_elements)
    {
      var new_strain = new Strain(num_elements);
      add(new_strain, this.Disease.get_transmissibility());
    }

    public void reset()
    {
      this.Strains.Clear();
      setup(this.Disease);
    }

    public void add(Strain strain)
    {
      this.Strains.Add(strain);
    }

    public int add(Strain new_strain, double transmissibility)
    {
      int new_strain_id;
      // if this genotype has been seen already, re-use existing id
      string new_geno_string = new_strain.ToString();
      if (this.StrainGenoTypeMap.ContainsKey(new_geno_string))
      {
        new_strain_id = this.StrainGenoTypeMap[new_geno_string];
      }
      else
      {
        // child strain id is next available id from strain table
        new_strain_id = this.Strains.Count;
        this.StrainGenoTypeMap.Add(new_geno_string, new_strain_id);
        // set the child strain's id, disease pointer, transmissibility, and parent strain pointer
        new_strain.Setup(new_strain_id, this.Disease, transmissibility, null);
        // Add the new child to the strain table
        add(new_strain);
      }
      // return the newly created id
      return new_strain_id;
    }

    public int add(Strain child_strain, double transmissibility, int parent_strain_id)
    {
      int child_strain_id;
      var parent_strain = this.Strains[parent_strain_id];
      // if no change, return the parent strain id
      if (child_strain.StrainData == parent_strain.StrainData)
      {
        return parent_strain_id;
      }
      // if this genotype has been seen already, re-use existing id
      string child_geno_string = child_strain.ToString();
      if (this.StrainGenoTypeMap.ContainsKey(child_geno_string))
      {
        child_strain_id = this.StrainGenoTypeMap[child_geno_string];
      }
      else
      {
        // child strain id is next available id from strain table
        child_strain_id = this.Strains.Count;
        this.StrainGenoTypeMap.Add(child_geno_string, child_strain_id);
        // set the child strain's id, disease pointer, transmissibility, and parent strain pointer
        child_strain.Setup(child_strain_id, this.Disease, transmissibility, parent_strain);
        // Add the new child to the strain table
        add(child_strain);
      }
      // return the newly created id
      return child_strain_id;
    }

    public double get_transmissibility(int id)
    {
      return this.Strains[id].Transmissibility;
    }

    public int get_num_strain_data_elements(int strain)
    {
      //if(strain >= strains.size()) return 0;
      return this.Strains[strain].StrainData.Count;
    }

    public StrainData get_strain_data(int strain) {
      return this.Strains[strain].StrainData;
    }

    public int get_strain_data_element(int strain, int i)
    {
      return this.Strains[strain].StrainData[i];
    }

    public void print_strain(int strain_id, StreamWriter o)
    {
      if (strain_id >= this.Strains.Count)
      {
        return;
      }

      this.Strains[strain_id].PrintAlternate(o);
    }

    public string get_strain_data_string(int strain_id)
    {
      return this.Strains[strain_id].ToString();
    }
}
}
