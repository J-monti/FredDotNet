namespace Fred
{
  public enum Activity_index
  {
    HOUSEHOLD_ACTIVITY,
    NEIGHBORHOOD_ACTIVITY,
    SCHOOL_ACTIVITY,
    CLASSROOM_ACTIVITY,
    WORKPLACE_ACTIVITY,
    OFFICE_ACTIVITY,
    HOSPITAL_ACTIVITY,
    AD_HOC_ACTIVITY,
    DAILY_ACTIVITY_LOCATIONS
  }

  public enum Behavior_index
  {
    TAKE_SICK_LEAVE,
    STAY_HOME_WHEN_SICK,
    KEEP_CHILD_HOME_WHEN_SICK,
    ACCEPT_VACCINE,
    ACCEPT_VACCINE_DOSE,
    ACCEPT_VACCINE_FOR_CHILD,
    ACCEPT_VACCINE_DOSE_FOR_CHILD,
    NUM_BEHAVIORS
  }

  public enum Behavior_change_model_enum
  {
    REFUSE,
    ACCEPT,
    FLIP,
    IMITATE_PREVALENCE,
    IMITATE_CONSENSUS,
    IMITATE_COUNT,
    HBM,
    NUM_BEHAVIOR_CHANGE_MODELS
  }

  public enum Chronic_condition_index
  {
    ASTHMA,
    COPD, //Chronic Obstructive Pulmonary Disease
    CHRONIC_RENAL_DISEASE,
    DIABETES,
    HEART_DISEASE,
    HYPERTENSION,
    HYPERCHOLESTROLEMIA,
    CHRONIC_MEDICAL_CONDITIONS
  }

  public enum Insurance_assignment_index
  {
    PRIVATE,
    MEDICARE,
    MEDICAID,
    HIGHMARK,
    UPMC,
    UNINSURED,
    UNSET
  }

  public enum Intervention_flag
  {
    TAKES_VACCINE,
    TAKES_AV
  }

  public enum Household_income_level_code
  {
    CAT_I,
    CAT_II,
    CAT_III,
    CAT_IV,
    CAT_V,
    CAT_VI,
    CAT_VII,
    UNCLASSIFIED
  }

  public enum Household_extended_absence_index
  {
    HAS_HOSPITALIZED,
    //HAS_IN_PRISON,
    //HAS_IN_NURSING_HOME,
    //HAS_IN_COLLEGE_DORM,
    HOUSEHOLD_EXTENDED_ABSENCE
  }

  enum Household_visitation_place_index
  {
    HOSPITAL,
    //PRISON,
    //NURSING_HOME,
    //COLLEGE_DORM,
    HOUSEHOLD_VISITATION
  }
}