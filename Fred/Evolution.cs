using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Evolution
  {
    public Disease Disease { get; private set; }

    public void Setup(Disease disease)
    {
      this.Disease = disease;
    }

    public virtual double residual_immunity(Person person, int challenge_strain, int day)
    {
      return 0.0;// double(!(person.Health.is_susceptible(this.Disease));
    }

    public virtual void print() { }

    /*
      Transmission::Loads * Evolution::get_primary_loads(int day) {
      Transmission::Loads *loads = new Transmission::Loads;
      loads->insert( pair<int, double> (1, 1) );
      return loads;
      }

      Transmission::Loads * Evolution::get_primary_loads(int day, int strain) {
      Transmission::Loads *loads = new Transmission::Loads;
      loads->insert( pair<int, double> (strain, 1) );
      return loads;
      }
    */

    public virtual double antigenic_diversity(Person p1, Person p2)
    {
      /*
      Infection *inf1 = p1->get_health()->get_infection(disease->get_id());
      Infection *inf2 = p2->get_health()->get_infection(disease->get_id());

      if(!inf1 || !inf2) return 0;

      vector<int> str1; inf1->get_strains(str1);
      vector<int> str2; inf1->get_strains(str2);

      // TODO how to handle multiple strains???

      return antigenic_distance(str1.at(0), str2.at(0));
      */
      return 0.0;
    }
  }
}
