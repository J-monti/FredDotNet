using System;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class Antivirals : List<Antiviral>
  {
    /**
   * Default constructor<br />
   * Reads all important values from parameter file
   */
    public Antivirals()
    {

    }

    //Paramter Access Members
    /**
     * @return <code>true</code> if there are Antivirals, <code>false</code> if not
     */
    public bool do_av() { return this.Count > 0; }

    /**
     * @return the count of antivirals
     */
    public int get_number_antivirals() { return this.Count; }

    /**
     * @return the total current stock of all Antivirals in this group
     */
    public int get_total_current_stock()
    {
      return this.Sum(a => a.get_current_stock());
    }

    /**
     * @return a pointer to this groups Antiviral vector
     */
    public List<Antiviral> get_AV_vector() { return this; }

    /**
     * Return a pointer to a specific Antiviral in this group's vector
     */
    public Antiviral get_AV(int nav) { return this[nav]; }

    // Utility Functions
    /**
     * Print out information about this object
     */
    public void print()
    {
      Console.WriteLine();
      Console.WriteLine("Antiviral Package");
      Console.WriteLine($"There are {this.Count} antivirals to choose from.");
      for (int iav = 0; iav < this.Count; iav++)
      {
        Console.WriteLine();
        Console.WriteLine($"Antiviral # {iav}");
        this[iav].print();
      }

      Console.WriteLine();
      Console.WriteLine();
    }

    /**
     * Print out current stock information for each Anitviral in this group's vector
     */
    public void print_stocks()
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        Console.WriteLine();
        Console.WriteLine($"Antiviral # {iav}");
        this[iav].print_stocks();
        Console.WriteLine();
      }
    }

    /**
     * Put this object back to its original state
     */
    public void reset()
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        this[iav].reset();
      }
    }

    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public void update(int day)
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        this[iav].update(day);
      }
    }

    /**
     * Print out a daily report
     *
     * @param day the simulation day
     */
    public void report(int day)
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        this[iav].report(day);
      }
    }

    /**
     * Used during debugging to verify that code is functioning properly. <br />
     * Checks the quality_control of each Antiviral in this group's vector of AVs
     *
     * @param ndiseases the number of diseases
     * @return 1 if there is a problem, 0 otherwise
     */
    public void quality_control(int ndiseases)
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        if (Global.Verbose > 1)
        {
          this[iav].print();
        }

        if (this[iav].quality_control(ndiseases) == 1)
        {
          Utils.fred_abort("Help! AV# {0} failed Quality", iav);
        }
      }
    }

    // Polling the collection 
    /**
     * This method looks through the vector of Antiviral objects and checks to see if each one is
     * effective against the particular disease and if it also has some in stock.  If so, then that
     * particular AV is added to the return vector.
     *
     * @param the disease to poll for
     * @return a vector of pointers to Antiviral objects
     */
    public List<Antiviral> find_applicable_AVs(int disease)
    {
      return this.Where(av => av.get_disease() == disease && av.get_current_stock() != 0).ToList();
    }

    /**
     * @return a vector of pointers to all Antiviral objects in this group that are prophylaxis
     */
    public List<Antiviral> prophylaxis_AVs()
    {
      return this.Where(av => av.is_prophylaxis()).ToList();
    }
  }
}
