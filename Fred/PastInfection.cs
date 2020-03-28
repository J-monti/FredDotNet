using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class PastInfection
  {
    public PastInfection() { }

    public PastInfection(int _strain_id, int _recovery_date, int _age_at_exposure) {
      strain_id = _strain_id;
      recovery_date = _recovery_date;
      age_at_exposure = _age_at_exposure;
    }

      int get_strain()
      {
        return strain_id;
      }

      void report()
      {
        printf("DEBUG %d %d %d\n", recovery_date, age_at_exposure, strain_id);
      }

      string format_header()
      {
        return "# person_id disease_id recovery_date age_at_exposure strain_id\n";
      }
    }
}
