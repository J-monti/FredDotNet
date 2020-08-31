using System;

namespace Fred
{
  public class AV_Decision_Allow_Only_One : Decision
  {
    public AV_Decision_Allow_Only_One() { }

    public AV_Decision_Allow_Only_One(Policy policy)
      : base(policy)
    {
      this.name = "AV Decision Allow Only One AV per Person";
      this.type = "Y/N";
    }

    public override int evaluate(Person person, int disease, int current_day)
    {
      return (person.get_health().get_number_av_taken() == 0) ? 0 : -1;
    }
  }
}
