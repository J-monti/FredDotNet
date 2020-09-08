using System.Text;

namespace Fred
{
  public struct Person_Init_Data
  {
    public string house_label;
    public string school_label;
    public string work_label;
    public string label;
    public int age;
    public int race;
    public int relationship;
    public char sex;
    public bool today_is_birthday;
    public int day;
    public Place house;
    public Place work;
    public Place school;
    public bool in_grp_qrtrs;
    public char gq_type;

    public Person_Init_Data(int _age, int _race, int _relationship, char _sex, bool _today_is_birthday, int _day)
    {
      age = _age;
      race = _race;
      relationship = _relationship;
      sex = _sex;
      today_is_birthday = _today_is_birthday;
      day = _day;
      house = null;
      work = null;
      school = null;
      gq_type = ' ';
      in_grp_qrtrs = false;
      house_label = "-1";
      school_label = "-1";
      work_label = "-1";
      label = "-1";
  }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.Append("Person Init Data:");
      builder.Append($" label {this.label}");
      builder.Append($" age {this.age}");
      builder.Append($" race {this.race}");
      builder.Append($" relationship {this.relationship}");
      builder.Append($" today_is_birthday? {this.today_is_birthday}");
      builder.Append($" day {this.day}");
      builder.Append($" house_label {this.house_label}");
      builder.Append($" work_label {this.work_label}");
      builder.Append($" school_label {this.school_label}");
      builder.Append($" in_group_quarters? {this.in_grp_qrtrs}");
      return builder.ToString();
    }
  }
}
