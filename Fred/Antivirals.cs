using System;
using System.Collections.Generic;
using System.Linq;

namespace Fred
{
  public class Antivirals : List<Antiviral>
  {
    public Antivirals(int nav)
    {
      this.Clear();
      for (int iav = 0; iav < nav; iav++)
      {
        int Disease, CorLength, InitSt, TotAvail, PerDay;
        double RedInf, RedSusc, RedASympPer, RedSympPer, ProbSymp, Eff, PerSympt;
        int StrtDay, Proph;
        bool isProph;

        Params::get_indexed_param("av_disease", iav, &Disease);
        Params::get_indexed_param("av_initial_stock", iav, &InitSt);
        Params::get_indexed_param("av_total_avail", iav, &TotAvail);
        Params::get_indexed_param("av_additional_per_day", iav, &PerDay);
        //get_indexed_param("av_percent_resistance",iav,&Eff);
        Eff = 1.0; // Not implemented yet
        Params::get_indexed_param("av_course_length", iav, &CorLength);
        Params::get_indexed_param("av_reduce_infectivity", iav, &RedInf);
        Params::get_indexed_param("av_reduce_susceptibility", iav, &RedSusc);
        Params::get_indexed_param("av_reduce_symptomatic_period", iav, &RedSympPer);
        Params::get_indexed_param("av_reduce_asymptomatic_period", iav, &RedASympPer);
        Params::get_indexed_param("av_prob_symptoms", iav, &ProbSymp);
        Params::get_indexed_param("av_start_day", iav, &StrtDay);
        Params::get_indexed_param("av_prophylaxis", iav, &Proph);
        if (Proph == 1) isProph = true;
        else isProph = false;
        Params::get_indexed_param("av_percent_symptomatics", iav, &PerSympt);
        int n;
        Params::get_indexed_param("av_course_start_day", iav, &n);
        var AVCourseSt = new double[n];
        int MaxAVCourseSt = Params::get_indexed_param_vector("av_course_start_day", iav, AVCourseSt) - 1;

        this.Add(new Antiviral(Disease, CorLength, RedInf,
                                RedSusc, RedASympPer, RedSympPer,
                                ProbSymp, InitSt, TotAvail, PerDay,
                                Eff, AVCourseSt, MaxAVCourseSt,
                                StrtDay, isProph, PerSympt));

      }

      this.QualityControl(Global.Diseases.Count);
    }

    public int GetTotalCurrentStock()
    {
      return this.Sum(a => a.CurrentStock);
    }

    public List<Antiviral> FindApplicableAVs(int disease)
    {
      var avs = new List<Antiviral>();
      for(int iav = 0; iav < this.Count; iav++)
      {
        if(this[iav].Disease == disease && this[iav].CurrentStock != 0)
        {
          avs.Add(this[iav]);
        }
      }

      return avs;
    }

    public List<Antiviral> ProphylaxisAVs()
    {
      var avs = new List<Antiviral>();
      for (int iav = 0; iav < this.Count;iav++)
      {
        if(this[iav].IsProphylaxis)
        {
          avs.Add(this[iav]);
        }
      }

      return avs;
    }

    public void Print() {
      Console.WriteLine("Antiviral Package ");
      Console.WriteLine("There are {0} antivirals to choose from.", this.Count);
      for(int iav = 0; iav<this.Count; iav++)
      {
        Console.WriteLine("Antiviral #{0}", iav);
        this[iav].Print();
      }
      Console.WriteLine();
    }

    public void PrintStocks()
    {
      for(int iav = 0; iav<this.Count; iav++)
      {
        Console.WriteLine("Antiviral #{0}", iav);
        this[iav].PrintStocks();
        Console.WriteLine();
      }
    }

    private void QualityControl(int ndiseases)
    {
      for(int iav = 0; iav<this.Count;iav++) {
        if (Global.Verbose > 1) {
          this[iav].Print();
        }

        if(this[iav].QualityControl(ndiseases))
        {
          var error = string.Format("Help! AV# {0} failed Quality!", iav);
          Console.Error.WriteLine(error);
          throw new InvalidOperationException(error);
        }
      }
    }

    public void Update(DateTime day)
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        this[iav].Update(day);
      }
    }

    public void Reset()
    {
      for (int iav = 0; iav < this.Count; iav++)
      {
        this[iav].Reset();
      }
    }
  }
}
