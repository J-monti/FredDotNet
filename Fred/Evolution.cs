using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Evolution
  {
    protected Disease disease;

    public virtual void setup(Disease disease)
    {
      this.disease = disease;
    }

    public virtual double residual_immunity(Person person, int challenge_strain, int day)
    {
      return person.get_health().is_susceptible(disease.get_id()) ? 1 : 0.0;
    }

    public virtual void print() { }
    public virtual void update(int day) { }
    public virtual double antigenic_distance(int strain1, int strain2)
    {
      if (strain1 == strain2) return 0;
      else return 1;
    }
    public virtual double antigenic_diversity(Person p1, Person p2)
    {
      return 0.0;
    }

    public virtual void terminate_person(Person p) { }
    public virtual void initialize_reporting_grid(Regional_Layer grid) { }
    public virtual void init_prior_immunity(Disease disease) { }
    public Disease get_disease() { return disease; }
  }
}
