﻿namespace Fred
{
  public class Vaccine_Priority_Policy_ACIP : Policy
  {
    public Vaccine_Priority_Policy_ACIP(VaccineManager vcm)
      : base(vcm)
    {
      this.Name = "Vaccine Priority Policy - ACIP Priority";
      this.Decisions.Add(new Vaccine_Priority_Decision_Specific_Age(this));
      this.Decisions.Add(new Vaccine_Priority_Decision_Pregnant(this));
      this.Decisions.Add(new Vaccine_Priority_Decision_At_Risk(this));
    }
  {
  }
}