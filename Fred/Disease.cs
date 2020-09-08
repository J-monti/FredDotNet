using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fred
{
  public class Disease
  {
    // disease identifiers
    private int id;
    private string disease_name;

    // the course of infection within a host
    private string natural_history_model;
    private Natural_History natural_history;
    private bool make_all_susceptible;

    // how the disease spreads
    private string transmission_mode;
    private Transmission transmission;

    // contagiousness
    private double transmissibility;
    private double R0;
    private double R0_a;
    private double R0_b;

    // the course of infection at the population level
    private Epidemic epidemic;
    private Age_Map at_risk;
    private Age_Map residual_immunity;
    private Dictionary<int, List<double>> residual_immunity_by_FIPS;

    // variation over time of year
    private double seasonality_max, seasonality_min;
    private double seasonality_Ka, seasonality_Kb;

    // behavioral intervention efficacies
    private int enable_face_mask_usage;
    private double face_mask_transmission_efficacy;
    private double face_mask_susceptibility_efficacy;

    private int enable_hand_washing;
    private double hand_washing_transmission_efficacy;
    private double hand_washing_susceptibility_efficacy;
    private double face_mask_plus_hand_washing_transmission_efficacy;
    private double face_mask_plus_hand_washing_susceptibility_efficacy;

    // hospitalization and outpatient healthcare parameters
    private double min_symptoms_for_seek_healthcare;
    private Age_Map hospitalization_prob;
    private Age_Map outpatient_healthcare_prob;

    /**
   * Default constructor
   */
    public Disease()
    {
      this.id = -1;
      this.transmissibility = -1.0;
      this.residual_immunity = null;
      this.at_risk = null;
      this.epidemic = null;
      this.natural_history = null;
      this.min_symptoms_for_seek_healthcare = -1.0;
      this.hospitalization_prob = null;
      this.outpatient_healthcare_prob = null;
      this.seasonality_Ka = -1.0;
      this.seasonality_Kb = -1.0;
      this.seasonality_min = -1.0;
      this.seasonality_max = -1.0;
      this.R0 = -1.0;
      this.R0_a = -1.0;
      this.R0_b = -1.0;
      this.natural_history_model = string.Empty;
      this.enable_face_mask_usage = 0;
      this.face_mask_transmission_efficacy = -1.0;
      this.face_mask_susceptibility_efficacy = -1.0;
      this.enable_hand_washing = 0;
      this.hand_washing_transmission_efficacy = -1.0;
      this.hand_washing_susceptibility_efficacy = -1.0;
      this.face_mask_plus_hand_washing_transmission_efficacy = -1.0;
      this.face_mask_plus_hand_washing_susceptibility_efficacy = -1.0;
      this.make_all_susceptible = true;
    }

    public void get_parameters(int disease_id, string name)
    {
      this.id = disease_id;
      // set disease name
      this.disease_name = name;
      Utils.FRED_VERBOSE(0, "disease {0} {1} read_parameters entered\n", this.id, this.disease_name);

      // contagiousness
      // Note: the following tries first to find "trans" but falls back to "transmissibility":
      var found = FredParameters.GetParameter($"{this.disease_name}_trans", ref this.transmissibility);
      if (!found)
      {
        FredParameters.GetParameter($"{this.disease_name}_transmissibility", ref this.transmissibility);
      }

      // type of natural history and transmission mode
      FredParameters.GetParameter($"{this.disease_name}_natural_history_model", ref this.natural_history_model);
      if (this.transmissibility > 0.0)
      {
        FredParameters.GetParameter($"{this.disease_name}_transmission_mode", ref this.transmission_mode);
      }

      // optional parameters:
      int n = 1;
      FredParameters.GetParameter($"{this.disease_name}_make_all_susceptible", ref n);
      this.make_all_susceptible = n != 0;

      // convenience parameters (for single disease simulations only)
      if (this.id == 0)
      {
        FredParameters.GetParameter("R0", ref this.R0);
        FredParameters.GetParameter("R0_a", ref this.R0_a);
        FredParameters.GetParameter("R0_b", ref this.R0_b);
        if (this.R0 > 0)
        {
          this.transmissibility = this.R0_a * this.R0 * this.R0 + this.R0_b * this.R0;
        }
      }

      // variation over time of year
      if (Global.Enable_Climate)
      {
        FredParameters.GetParameter($"{this.disease_name}_seasonality_multiplier_min",
                ref this.seasonality_min);
        FredParameters.GetParameter($"{this.disease_name}_seasonality_multiplier_max",
                ref this.seasonality_max);
        FredParameters.GetParameter($"{this.disease_name}_seasonality_multiplier_Ka",
                ref this.seasonality_Ka); // Ka = -180 by default
        this.seasonality_Kb = Math.Log(this.seasonality_max - this.seasonality_min);
      }

      //Hospitalization and Healthcare parameters
      if (Global.Enable_Hospitals)
      {
        FredParameters.GetParameter($"{this.disease_name}_min_symptoms_for_seek_healthcare",
               ref this.min_symptoms_for_seek_healthcare);
        this.hospitalization_prob = new Age_Map("Hospitalization Probability");
        this.hospitalization_prob.read_from_input($"{this.disease_name}_hospitalization_prob");
        this.outpatient_healthcare_prob = new Age_Map("Outpatient Healthcare Probability");
        this.outpatient_healthcare_prob.read_from_input($"{this.disease_name}_outpatient_healthcare_prob");
      }

      // protective behavior efficacy parameters
      FredParameters.GetParameter("enable_face_mask_usage", ref this.enable_face_mask_usage);
      if (this.enable_face_mask_usage != 0)
      {
        FredParameters.GetParameter($"{this.disease_name}_face_mask_transmission_efficacy",
                ref this.face_mask_transmission_efficacy);
        FredParameters.GetParameter($"{this.disease_name}_face_mask_susceptibility_efficacy",
                ref this.face_mask_susceptibility_efficacy);
      }
      FredParameters.GetParameter("enable_hand_washing", ref this.enable_hand_washing);
      if (this.enable_hand_washing != 0)
      {
        FredParameters.GetParameter($"{this.disease_name}_hand_washing_transmission_efficacy",
                ref this.hand_washing_transmission_efficacy);
        FredParameters.GetParameter($"{this.disease_name}_hand_washing_susceptibility_efficacy",
                ref this.hand_washing_susceptibility_efficacy);
      }
      if (this.enable_face_mask_usage != 0 && this.enable_hand_washing != 0)
      {
        FredParameters.GetParameter($"{this.disease_name}_face_mask_plus_hand_washing_transmission_efficacy",
                ref this.face_mask_plus_hand_washing_transmission_efficacy);
        FredParameters.GetParameter($"{this.disease_name}_face_mask_plus_hand_washing_susceptibility_efficacy",
                ref this.face_mask_plus_hand_washing_susceptibility_efficacy);
      }

      // Define residual immunity
      this.residual_immunity = new Age_Map("Residual Immunity");
      this.residual_immunity.read_from_input($"{this.disease_name}_residual_immunity");

      // use params "Immunization" to override the residual immunity for
      // the group starting with age 0.
      // This allows easy specification for a prior imunization rate, e.g.,
      // to specify 25% prior immunization rate, use the parameters:
      //
      // residual_immunity_ages[0] = 2 0 100
      // residual_immunity_values[0] = 1 0.0
      // Immunization = 0.25
      //

      double immunization_rate = 0;
      FredParameters.GetParameter("Immunization", ref immunization_rate);
      if (immunization_rate >= 0.0)
      {
        this.residual_immunity.set_all_values(immunization_rate);
      }

      if (this.residual_immunity.is_empty() == false)
      {
        this.residual_immunity.print();
      }

      if (Global.Residual_Immunity_by_FIPS)
      {
        this.read_residual_immunity_by_FIPS();
        Utils.FRED_STATUS(0, "Residual Immunity by FIPS enabled \n");
      }

      // Define at risk people
      if (Global.Enable_Vaccination)
      {
        this.at_risk = new Age_Map("At Risk Population");
        this.at_risk.read_from_input($"{this.disease_name}_at_risk");
      }

      Utils.FRED_VERBOSE(0, "disease {0} {1} read_parameters finished\n", this.id, this.disease_name);
    }

    /**
     * Set all of the attributes for the Disease
     */
    public void setup()
    {
      Utils.FRED_VERBOSE(0, "disease {0} {1} setup entered\n", this.id, this.disease_name);

      // Initialize Natural History Model
      this.natural_history = Natural_History.get_new_natural_history(this.natural_history_model);

      // read in parameters and files associated with this natural history model: 
      this.natural_history.setup(this);
      this.natural_history.get_parameters();

      if (this.transmissibility > 0.0)
      {
        // Initialize Transmission Model
        this.transmission = Transmission.get_new_transmission(this.transmission_mode);

        // read in parameters and files associated with this transmission mode: 
        this.transmission.setup(this);
      }

      // Initialize Epidemic Model
      this.epidemic = Epidemic.get_epidemic(this);
      this.epidemic.setup();

      Utils.FRED_VERBOSE(0, "disease {0} {1} setup finished\n", this.id, this.disease_name);
    }

    public void prepare()
    {
      Utils. FRED_VERBOSE(0, "disease {0} {1} prepare entered\n", this.id, this.disease_name);
      // final prep for epidemic
      this.epidemic.prepare();
      Utils.FRED_VERBOSE(0, "disease {0} {1} prepare finished\n", this.id, this.disease_name);
    }

    /**
     * @return this Disease's id
     */
    public int get_id()
    {
      return this.id;
    }

    /**
     * @return the transmissibility
     */
    public double get_transmissibility()
    {
      return this.transmissibility;
    }

    public double calculate_climate_multiplier(double seasonality_value)
    {
      return Math.Exp(((this.seasonality_Ka * seasonality_value) + this.seasonality_Kb)) + this.seasonality_min;
    }

    /**
     * @return the Epidemic's attack ratio
     * @see Epidemic::get_attack_rate()
     */
    public double get_attack_rate()
    {
      return this.epidemic.get_attack_rate();
    }

    /**
     * @return the Epidemic's symptomatic attack ratio
     * @see Epidemic::get_attack_rate()
     */
    public double get_symptomatic_attack_rate()
    {
      return this.epidemic.get_symptomatic_attack_rate();
    }

    /**
     * @return a pointer to this Disease's residual_immunity Age_Map
     */
    public Age_Map get_residual_immunity()
    {
      return this.residual_immunity;
    }

    /**
     * @return a pointer to this Disease's at_risk Age_Map
     */
    public Age_Map get_at_risk()
    {
      return this.at_risk;
    }

    /**
     * @param day the simulation day
     * @see Epidemic::print_stats(day);
     */
    public void print_stats(int day)
    {
      this.epidemic.print_stats(day);
    }

    /**
     * @return the epidemic with which this Disease is associated
     */
    public Epidemic get_epidemic()
    {
      return this.epidemic;
    }

    /**
     * @return the probability that agent's will stay home
     */
    //public static double get_prob_stay_home();

    /**
     * @param the new probability that agent's will stay home
     */
    //public static void set_prob_stay_home(double prob);

    //public void get_disease_parameters();

    public void increment_cohort_infectee_count(int day)
    {
      this.epidemic.increment_cohort_infectee_count(day);
    }

    public void update(int day)
    {
      this.epidemic.update(day);
    }

    public void terminate_person(Person person, int day)
    {
      this.epidemic.terminate_person(person, day);
    }

    public double get_face_mask_transmission_efficacy()
    {
      return this.face_mask_transmission_efficacy;
    }

    public double get_face_mask_susceptibility_efficacy()
    {
      return this.face_mask_susceptibility_efficacy;
    }

    public double get_hand_washing_transmission_efficacy()
    {
      return this.hand_washing_transmission_efficacy;
    }

    public double get_hand_washing_susceptibility_efficacy()
    {
      return this.hand_washing_susceptibility_efficacy;
    }

    public double get_face_mask_plus_hand_washing_transmission_efficacy()
    {
      return this.face_mask_plus_hand_washing_transmission_efficacy;
    }

    public double get_face_mask_plus_hand_washing_susceptibility_efficacy()
    {
      return this.face_mask_plus_hand_washing_susceptibility_efficacy;
    }

    public double get_min_symptoms_for_seek_healthcare()
    {
      return this.min_symptoms_for_seek_healthcare;
    }

    public double get_hospitalization_prob(Person per)
    {
      return this.hospitalization_prob.find_value(per.get_real_age());
    }

    public double get_outpatient_healthcare_prob(Person per)
    {
      return this.outpatient_healthcare_prob.find_value(per.get_real_age());
    }

    public  string get_disease_name()
    {
      return this.disease_name;
    }

    public void read_residual_immunity_by_FIPS()
    {
      //Params::get_param_from_string("residual_immunity_by_FIPS_file", Global::Residual_Immunity_File);
      string fips_string;
      int fips_int;
      //char ages_string[FRED_STRING_SIZE];
      string values_string;
      if (!File.Exists(Global.Residual_Immunity_File))
      {
        Utils.fred_abort("Residual Immunity by FIPS enabled but residual_immunity_by_FIPS_file {0} not found\n", Global.Residual_Immunity_File);
      }
      using var fp = new StreamReader(Global.Residual_Immunity_File);
      while (fp.Peek() != -1)
      {  //fips 2 lines fips first residual immunity second
        fips_string = fp.ReadLine();
        values_string = fp.ReadLine();
        fips_int = Convert.ToInt32(fips_string);
        if (string.IsNullOrWhiteSpace(values_string))
        {  //values
          Utils.fred_abort("Residual Immunity by FIPS file {0} not properly formed\n", Global.Residual_Immunity_File);
        }
        var temp_vector = FredParameters.ParseList<double>(values_string);
        this.residual_immunity_by_FIPS.Add(fips_int, temp_vector);
      }
    }

    public List<double> get_residual_immunity_values_by_FIPS(int FIPS_int)
    {
      return residual_immunity_by_FIPS[FIPS_int];
    }

    public  string get_natural_history_model()
    {
      return this.natural_history_model;
    }

    public Natural_History get_natural_history()
    {
      return this.natural_history;
    }

    // case fatality

    public virtual bool is_case_fatality_enabled()
    {
      return this.natural_history.is_case_fatality_enabled();
    }

    public virtual bool is_fatal(double real_age, double symptoms, int days_symptomatic)
    {
      return this.natural_history.is_fatal(real_age, symptoms, days_symptomatic);
    }

    public virtual bool is_fatal(Person per, double symptoms, int days_symptomatic)
    {
      return this.natural_history.is_fatal(per, symptoms, days_symptomatic);
    }

    // transmission mode

    public string get_transmission_mode()
    {
      return this.transmission_mode;
    }

    public Transmission get_transmission()
    {
      return this.transmission;
    }

    public void become_immune(Person person, bool susceptible, bool infectious, bool symptomatic)
    {
      this.epidemic.become_immune(person, susceptible, infectious, symptomatic);
    }

    public bool assume_susceptible()
    {
      return this.make_all_susceptible;
    }

    public void end_of_run()
    {
      this.epidemic.end_of_run();
      this.natural_history.end_of_run();
    }
  }
}
