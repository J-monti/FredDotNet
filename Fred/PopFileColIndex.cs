namespace Fred
{
  public class PopFileColIndex
  {
    // all populations
    public readonly int p_id = 0;
    public readonly int home_id = 1; // <-- this is either hh_id or gq_id
    public int sporder;
    public int age_str;
    public int sex_str;
    public int workplace_id; // <-- same as home_id for gq pop
    public int number_of_columns;
    // only synth_people population
    public int serial_no;
    public int stcotrbg;
    public int race_str;
    public int relate;
    public int school_id;
    // only synth_gq_people population
    public int gq_type;
  }

  public class HH_PopFileColIndex : PopFileColIndex
  {
    public HH_PopFileColIndex()
    {
      serial_no = 2;
      stcotrbg = 3;
      age_str = 4;
      sex_str = 5;
      race_str = 6;
      sporder = 7;
      relate = 8;
      school_id = 9;
      workplace_id = 10;
      number_of_columns = 11;
    }
  }

  public class GQ_PopFileColIndex : PopFileColIndex
  {
    public GQ_PopFileColIndex()
    {
      gq_type = 2;
      sporder = 3;
      age_str = 4;
      sex_str = 5;
      workplace_id = 1; // <-- same as home_id
      number_of_columns = 6;
    }
  }

  public class GQ_PopFileColIndex_2010_ver1 : PopFileColIndex
  {
    public GQ_PopFileColIndex_2010_ver1()
    {
      sporder = 2;
      age_str = 3;
      sex_str = 4;
      workplace_id = 1; // <-- same as home_id
      number_of_columns = 5;
    }
  }
}
