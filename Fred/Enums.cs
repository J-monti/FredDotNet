namespace Fred
{
  public enum Gender
  {
    Male = 0,
    Female,
  }

  public enum BehaviorEnum
  {
    TakeSickLeave = 0,
    StayHomeWhenSick,
    KeepChildHomeWhenSick,
    AcceptVaccine,
    AcceptVaccineDose,
    AcceptVaccineForChild,
    AcceptVaccineDoseForChild,
    NumBehaviors
  }

  public enum BehaviorChangeEnum
  {
    Refuse = 0,
    Accept,
    Flip,
    ImitatePrevalence,
    ImitateConsensus,
    ImitateCount,
    Hbm,
    NumBehaviorChangeModels
  }

  public enum Intervention
  {
    TakesVaccine = 0,
    TakesAV = 1,
  }

  enum Activity
  {
    Household,
    Neighborhood,
    School,
    Classroom,
    Workplace,
    Office,
    Hospital,
    AdHoc,
    DailyLocations
  }

  public enum Profile
  {
    Unknown = 0,
    Infant,
    Preschool,
    Student,
    Teacher,
    Worker,
    WeekendWorker,
    Unemployeed,
    Retired,
    Prisoner,
    CollegeStudent,
    Military,
    NursingHomeResident,
  }

  public enum PlaceType
  {
    Unset = 0,// = 'U',
    Household,// = 'H',
    Neighborhood,// = 'N',
    School,// = 'S',
    Classroom,// = 'C',
    Workplace,// = 'W',
    Office,// = 'O',
    Hospital,// = 'M',
    Community,// = 'X',
  }

  public enum PlaceSubType
  {
    None = 0,// = 'X',
    College,// = 'C',
    Prison,// = 'P',
    MilitaryBase,// = 'M',
    NursingHome,// = 'N',
    HEalthcareClinic,// = 'I',
    MobileHealthcareClinic,// = 'Z'
  }

  public enum NetworkSubType
  {
    None = 0,
    Transmission,
    SexualPartner,
  }

  public enum ChronicCondition
  {
    Asthma = 0,
    Copd, // Chronic Obstructive Pulmonary Disease
    ChronicRenalDisease,
    Diabetes,
    HeartDisease,
    Hypertension,
    Hypercholestrolemia,
    ChronicMedicalConditions
  }

  public enum InsuranceAssignment
  {
    Private,
    Medicare,
    Medicaid,
    Highmark,
    Upmc,
    Uninsured,
    Unset
  }

  public enum TransmissionMode
  {
    Respiratory = 0,
    Vector,
    Sexual
  }

  public enum HouseholdIncomeLevel
  {
    UNCLASSIFIED,
    CAT_I,
    CAT_II,
    CAT_III,
    CAT_IV,
    CAT_V,
    CAT_VI,
    CAT_VII,
  }

  public enum HouseholdExtendedAbsence
  {
    HAS_HOSPITALIZED,
    //HAS_IN_PRISON,
    //HAS_IN_NURSING_HOME,
    //HAS_IN_COLLEGE_DORM,
    HOUSEHOLD_EXTENDED_ABSENCE
  }

  public enum Household_visitation_place_index
  {
    HOSPITAL,
    //PRISON,
    //NURSING_HOME,
    //COLLEGE_DORM,
    HOUSEHOLD_VISITATION
  }

  public enum VaccinePriority
  {
    No,
    Age,
    Acip
  }

  public enum VaccineDosePriority
  {
    No,
    First,
    Random,
    Last
  }
}