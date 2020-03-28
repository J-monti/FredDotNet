using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class PersonInitData
  {
    char house_label[32], school_label[32], work_label[32];
    char label[32];
    int age, race, relationship;
    char sex;
    bool today_is_birthday;
    int day;
    Place* house;
    Place* work;
    Place* school;
    bool in_grp_qrtrs;
    char gq_type;

    Person_Init_Data()
    {
      default_initialization();
    }

    Person_Init_Data(int _age, int _race, int _relationship,
         char _sex, bool _today_is_birthday, int _day)
    {

      default_initialization();
      age = _age;
      race = _race;
      relationship = _relationship;
      sex = _sex;
      today_is_birthday = _today_is_birthday;
      day = _day;
    }

    void default_initialization()
    {
      this->house = NULL;
      this->work = NULL;
      this->school = NULL;
      strcpy(this->label, "-1");
      strcpy(this->house_label, "-1");
      strcpy(this->school_label, "-1");
      strcpy(this->work_label, "-1");
      this->age = -1;
      this->race = -1;
      this->relationship = -1;
      this->sex = -1;
      this->day = 0;
      this->today_is_birthday = false;
      this->in_grp_qrtrs = false;
      this->gq_type = ' ';
    }

    const std::string to_string() const {
    std::stringstream ss;
    //ss << setw( 8 ) << setfill( ' ' ); 
    ss << "Person Init Data:"
       << " label " << this->label
       << " age " << this->age
       << " race " << this->race
       << " relationship " << this->relationship
       << " today_is_birthday? " << this->today_is_birthday
       << " day " << this->day
       << " house_label " << this->house_label
       << " work_label " << this->work_label
       << " school_label " << this->school_label
       << " in_group_quarters? " << this->in_grp_qrtrs;

    return ss.str();
  }
}
}
