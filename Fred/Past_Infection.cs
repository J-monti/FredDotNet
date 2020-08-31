using System;

namespace Fred
{
  public class Past_Infection
  {
    private int strain_id;
    private int recovery_date;
    private int age_at_exposure;

    public Past_Infection() { }

    public Past_Infection(int _strain_id, int _recovery_date, int _age_at_exposure)
    {
      strain_id = _strain_id;
      recovery_date = _recovery_date;
      age_at_exposure = _age_at_exposure;
    }

    public int get_strain()
    {
      return strain_id;
    }

    public int get_infectious_end_date() { return recovery_date; }
    public int get_age_at_exposure() { return age_at_exposure; }
    public void report()
    {
      Console.WriteLine("DEBUG {0} {1} {2}", recovery_date, age_at_exposure, strain_id);
    }

    public static string format_header()
    {
      return "# person_id disease_id recovery_date age_at_exposure strain_id\n";
    }
  }
}
