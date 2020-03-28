using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Vaccine
  {
    string name;
    int id;                              // Which in the number of vaccines is it
    int disease;                          // Which Disease is this vaccine for
    int number_doses;                    // How many doses does the vaccine need.
    List<VaccineDose> doses;       // Data structure to hold the efficacy of each dose.

    int initial_stock;                   // How much available at the beginning
    int total_avail;                     // How much total in reserve
    int stock;                           // How much do you currently have
    int reserve;                         // How much is still left to system
    int additional_per_day;              // How much can be introduced into the system on a given day

    int start_day;                       // When to start production

    int[] strains;
    int num_strains;

    // for statistics
    int number_delivered;
    int number_effective;

    public Vaccine(string _name, int _id, int _disease,
                 int _total_avail, int _additional_per_day,
                 int _start_day, int num_strains, int[] _strains)
    {
      name = _name;
      id = _id;
      disease = _disease;
      additional_per_day = _additional_per_day;
      start_day = _start_day;
      strains = _strains;

      initial_stock = 0;
      stock = 0;
      reserve = _total_avail;
      total_avail = _total_avail;
      number_delivered = 0;
      number_effective = 0;
    }

    public void add_dose(VaccineDose _vaccine_dose)
    {
      doses.Add(_vaccine_dose);
    }

    public void print()  {
      //cout << "Name = \t\t\t\t" <<name << "\n";
      //cout << "Applied to disease = \t\t" << disease << "\n";
      //cout << "Initial Stock = \t\t" << initial_stock << "\n";
      //cout << "Total Available = \t\t"<< total_avail << "\n";
      //cout << "Amount left to system = \t" << reserve << "\n";
      //cout << "Additional Stock per day =\t" << additional_per_day << "\n";
      //cout << "Starting on day = \t\t" << start_day << "\n";
      //cout << "Dose Information\n";
      //for(unsigned int i = 0; i<doses.size();i++){
      //  cout <<"Dose #"<<i+1 << "\n";
      //  doses[i].print();
      //}
    }

    public void reset()
    {
      stock = 0;
      reserve = total_avail;
    }

    public void update(int day)
    {
      if (day >= start_day) add_stock(additional_per_day);
    }

    public int get_strain(int i)
    {
      if (i < num_strains) return strains[i];
      else return -1;
    }


    public void add_stock(int add)
    {
      if (add <= reserve)
      {
        stock += add;
        reserve -= add;
      }
      else
      {
        stock += reserve;
        reserve = 0;
      }
    }

    public void remove_stock(int remove)
    {
      stock -= remove;
      if (stock < 0) stock = 0;
    }

    public int get_num_strains()
    {
      return num_strains;
    }
  }
}
