using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class MSEvolution : Evolution
  {
    private Age_Map halflife_inf;
    private Age_Map halflife_vac;
    private double prob_inoc_norm;
    private double init_prot_inf;
    private double init_prot_vac;
    private double sat_quantity;
    private Piecewise_Linear protection;

    public MSEvolution()
    {
      this.halflife_inf = null;
      this.halflife_vac = null;
      this.init_prot_inf = 0.0;
      this.init_prot_vac = 0.0;
      this.sat_quantity = 0.0;
      this.protection = null;
      this.prob_inoc_norm = 0.0;
    }

    public override void setup(Disease disease)
    {
      base.setup(disease);
      this.halflife_inf = new Age_Map("Infection Protection Half Life");
      this.halflife_inf.read_from_input("half_life_inf", disease.get_id());

      this.halflife_vac = new Age_Map("Vaccination Protection Half Life");
      this.halflife_vac.read_from_input("half_life_vac", disease.get_id());

      FredParameters.GetParameter("init_protection_inf", ref this.init_prot_inf);
      FredParameters.GetParameter("init_protection_vac", ref this.init_prot_vac);
      FredParameters.GetParameter("saturation_quantity", ref this.sat_quantity);

      this.protection = new Piecewise_Linear();
      this.protection.setup("strain_dependent_protection", disease);
      this.prob_inoc_norm = 1 - Math.Exp(-1);
    }

    public override double antigenic_distance(int strain1, int strain2)
    {
      int diff = strain1 - strain2;
      if (diff * diff == 0)
      {
        return 0.0;
      }
      else if (diff * diff == 1)
      {
        return 1.0;
      }
      else
      {
        return 10.0;
      }
    }

    public override double residual_immunity(Person person, int challenge_strain, int day)
    {
      double probTaking = 1 - base.residual_immunity(person, challenge_strain, day);
      // Pr(Taking | Past-Infections)
      probTaking *= prob_past_infections(person, challenge_strain, day);
      // Pr(Taking | Protective Vaccinations)
      //probTaking *= prob_past_vaccinations(person, challenge_strain, day);
      return (1 - probTaking);
    }

    protected virtual double prob_inf_blocking(int old_strain, int new_strain, int time, double real_age)
    {
      Utils.FRED_VERBOSE(3, "Prob Blocking %f old strain %d new strain %d time %d halflife %f age %.2f init prot inf %f\n",
        prob_blocking(old_strain, new_strain, time, halflife_inf.find_value(real_age), init_prot_inf),
        old_strain, new_strain, time, halflife_inf.find_value(real_age), real_age, init_prot_inf);
      return prob_blocking(old_strain, new_strain, time, this.halflife_inf.find_value(real_age), this.init_prot_inf);
    }

    protected virtual double prob_vac_blocking(int old_strain, int new_strain, int time, double real_age)
    {
      return prob_blocking(old_strain, new_strain, time, this.halflife_vac.find_value(real_age), this.init_prot_vac);
    }

    protected virtual double prob_blocking(int old_strain, int new_strain, int time, double halflife, double init_prot)
    {
      double prob_block = 1.0;
      // Generalized Immunity
      prob_block *= (1 - (init_prot * Math.Exp((0 - time) / (halflife / 0.693))));
      // Strain Dependent Immunity 
      double ad = antigenic_distance(old_strain, new_strain);
      prob_block *= (1 - this.protection.get_prob(ad));
      // Make sure that it's a valid probability 
      Utils.assert(prob_block >= 0.0 && prob_block <= 1.0);
      return (1 - prob_block);
    }

    protected virtual double prob_past_infections(Person infectee, int new_strain, int day)
    {
      int disease_id = this.disease.get_id();
      double probTaking = 1.0;
      int n = infectee.get_num_past_infections(disease_id);
      for (int i = 0; i < n; ++i)
      {
        var past_infection = infectee.get_past_infection(disease_id, i);
        //printf("DATES: %d %d\n", day, pastInf.get_infectious_end_date()); 
        probTaking *= (1 - prob_inf_blocking(past_infection.get_strain(), new_strain,
                 day - past_infection.get_infectious_end_date(), past_infection.get_age_at_exposure()));
      }
      return probTaking;
    }

    protected virtual double prob_past_vaccinations(Person infectee, int new_strain, int day)
    {
      double probTaking = 1.0;
      // TODO Handle getting past vaccinations through person instead of infection
      /*  int n = infection.get_num_past_vaccinations();
          cout << "VACC " << n << endl;
          Infection *pastInf;
          vector<int> old_strains; 
          for(int i=0; i<n; i++){
          pastInf = infection.get_past_vaccination(i);
          if(! pastInf.provides_immunity()) continue;
          else{
          pastInf.get_strains(old_strains);
          for(unsigned int i=0; i<old_strains.size(); i++){
          probTaking *= (1 - prob_vac_blocking(old_strains[i], new_strain, 
          day - pastInf.get_exposure_date(), pastInf.get_age_at_exposure()));
          }
          }
          }*/
      return probTaking;
    }

    protected virtual double get_prob_taking(Person infectee, int new_strain, double quantity, int day)
    {
      double probTaking = 1.0;
      // Pr(Taking | quantity)
      probTaking *= prob_inoc(quantity);
      // Pr(Taking | Past-Infections)
      probTaking *= prob_past_infections(infectee, new_strain, day);
      // Pr(Taking | Protective Vaccinations)
      probTaking *= prob_past_vaccinations(infectee, new_strain, day);
      return probTaking;
    }

    protected virtual double prob_inoc(double quantity)
    {
      //static double norm = 1 - exp( -1 );
      double prob = (1.0 - Math.Exp((0 - quantity) / this.sat_quantity)) / this.prob_inoc_norm;
      return (prob < 1.0) ? prob : 1.0;
    }
  }
}
