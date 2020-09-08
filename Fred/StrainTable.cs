using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Fred
{
  public class StrainTable
  {
    private Mutex mutex;
    private Disease disease;
    private readonly List<Strain> strains;
    private readonly Dictionary<string, int> strain_genotype_map;

    public StrainTable()
    {
      this.strains = new List<Strain>();
      this.strain_genotype_map = new Dictionary<string, int>();
    }

    public void setup(Disease d) // Initial strains
    {
      this.disease = d;
      //int diseaseId = this.disease.get_id();
    }

    public void reset()
    {
      this.strains.Clear();
      setup(this.disease);
    }

    public void add_root_strain(int num_elements)
    {
      Utils.assert(this.strains.Count == 0);
      var new_strain = new Strain(num_elements);
      add(new_strain, this.disease.get_transmissibility());
    }

    public void add(Strain s)
    {
      this.strains.Add(s);
    }

    public int add(Strain new_strain, double transmissibility)
    {
      this.mutex.WaitOne();

      int new_strain_id;
      // if this genotype has been seen already, re-use existing id
      var new_geno_string = new_strain.ToString();
      if (this.strain_genotype_map.ContainsKey(new_geno_string))
      {
        new_strain_id = this.strain_genotype_map[new_geno_string];
      }
      else
      {
        // child strain id is next available id from strain table
        new_strain_id = this.strains.Count;
        this.strain_genotype_map.Add(new_geno_string, new_strain_id);
        // set the child strain's id, disease pointer, transmissibility, and parent strain pointer
        new_strain.setup(new_strain_id, this.disease, transmissibility, null);
        // Add the new child to the strain table
        add(new_strain);
      }

      this.mutex.ReleaseMutex();
      // return the newly created id
      return new_strain_id;
    }

    public int add(Strain child_strain, double transmissibility, int parent_strain_id)
    {
      this.mutex.WaitOne();
      int child_strain_id = parent_strain_id;
      var parent_strain = this.strains[parent_strain_id];
      // if no change, return the parent strain id
      if (child_strain.get_data() == parent_strain.get_data())
      {
        return parent_strain_id;
      }
      // if this genotype has been seen already, re-use existing id
      var child_geno_string = child_strain.ToString();
      if (this.strain_genotype_map.ContainsKey(child_geno_string))
      {
        child_strain_id = this.strain_genotype_map[child_geno_string];
      }
      else
      {
        // child strain id is next available id from strain table
        child_strain_id = this.strains.Count;
        this.strain_genotype_map.Add(child_geno_string, child_strain_id);
        // set the child strain's id, disease pointer, transmissibility, and parent strain pointer
        child_strain.setup(child_strain_id, this.disease, transmissibility, parent_strain);
        // Add the new child to the strain table
        add(child_strain);
      }

      this.mutex.ReleaseMutex();
      // return the newly created id
      return child_strain_id;
    }

    public double get_transmissibility(int id)
    {
      return this.strains[id].get_transmissibility();
    }

    public int get_num_strains()
    {
      //fred::Spin_Lock lock (this.mutex) ;
      return this.strains.Count;
    }

    public int get_num_strain_data_elements(int strain)
    {
      //if(strain >= strains.size()) return 0;
      return this.strains[strain].get_num_data_elements();
    }

    public int get_strain_data_element(int strain, int i)
    {
      return this.strains[strain].get_data_element(i);
    }

    public Strain_Data get_strain_data(int strain)
    {
      return this.strains[strain].get_strain_data();
    }

    public Strain get_strain(int strain_id)
    {
      return this.strains[strain_id];
    }

    public void print_strain(int strain_id, TextWriter o)
    {
      if (strain_id >= this.strains.Count)
      {
        return;
      }

      this.strains[strain_id].print_alternate(o);
    }

    public string get_strain_data_string(int strain_id)
    {
      return this.strains[strain_id].ToString();
    }
  }
}
