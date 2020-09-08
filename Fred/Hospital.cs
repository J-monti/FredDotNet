using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Fred
{
  public class Hospital : Place
  {
    private static double contacts_per_day;
    private static double[,] prob_transmission_per_contact;
    private static List<double> Hospital_health_insurance_prob;
    private static double HAZEL_disaster_capacity_multiplier = 1.0;
    private static int HAZEL_mobile_van_open_delay;
    private static int HAZEL_mobile_van_closure_day;
    private static Dictionary<string, HAZEL_Hospital_Init_Data> HAZEL_hospital_init_map;
    private static bool HAZEL_hospital_init_map_file_exists;
    private static List<double> HAZEL_reopening_CDF;

    private int bed_count;
    private int occupied_bed_count;
    private int daily_patient_capacity;
    private int current_daily_patient_count;
    private bool add_capacity;
    private bool HAZEL_closure_dates_have_been_set;

    private const int HOSP_ID = 0;
    private const int PNL_WK = 1;
    private const int ACCPT_PRIV = 2;
    private const int ACCPT_MEDICR = 3;
    private const int ACCPT_MEDICD = 4;
    private const int ACCPT_HGHMRK = 5;
    private const int ACCPT_UPMC = 6;
    private const int ACCPT_UNINSRD = 7;
    private const int REOPEN_AFTR_DAYS = 8;
    private const int IS_MOBILE = 9;
    private const int ADD_CAPACITY = 10;

    // true if a the hospital accepts the indexed Insurance Coverage
    private BitArray accepted_insurance_bitset = new BitArray((int)Insurance_assignment_index.UNSET);

    static Hospital()
    {
    }

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Hospital()
    {
      this.set_subtype(Place.SUBTYPE_NONE);
      this.Init();
    }

    /**
     * Constructor with necessary parameters
     */
    public Hospital(string label, char _subtype, FredGeo lon, FredGeo lat)
      : base(label, lon, lat)
    {
      this.set_subtype(_subtype);
      this.Init();
    }

    private void Init()
    {
      this.set_type(Place.TYPE_HOSPITAL);
      this.bed_count = 0;
      this.occupied_bed_count = 0;
      this.daily_patient_capacity = -1;
      this.current_daily_patient_count = 0;
      this.add_capacity = false;

      if (Global.Enable_Health_Insurance)
      {
        int index = 0;
        var values = Enum.GetValues(typeof(Insurance_assignment_index));
        foreach (Insurance_assignment_index value in values)
        {
          set_accepts_insurance(value, FredRandom.NextDouble() < Hospital_health_insurance_prob[index]);
          index++;
        }
      }

      if (Global.Enable_HAZEL && Hospital.HAZEL_hospital_init_map_file_exists)
      {
        //Use the values read in from the map file
        if (Hospital.HAZEL_hospital_init_map.ContainsKey(this.get_label()))
        {
          var init_data = Hospital.HAZEL_hospital_init_map[this.get_label()];
          this.set_accepts_insurance(Insurance_assignment_index.HIGHMARK, init_data.accpt_highmark);
          this.set_accepts_insurance(Insurance_assignment_index.MEDICAID, init_data.accpt_medicaid);
          this.set_accepts_insurance(Insurance_assignment_index.MEDICARE, init_data.accpt_medicare);
          this.set_accepts_insurance(Insurance_assignment_index.PRIVATE, init_data.accpt_private);
          this.set_accepts_insurance(Insurance_assignment_index.UNINSURED, init_data.accpt_uninsured);
          this.set_accepts_insurance(Insurance_assignment_index.UPMC, init_data.accpt_upmc);
          if (init_data.is_mobile)
          {
            this.set_subtype(Place.SUBTYPE_MOBILE_HEALTHCARE_CLINIC);
          }
          this.set_daily_patient_capacity((init_data.panel_week / 5) + 1);
          this.add_capacity = init_data.add_capacity;
        }
      }
    }

    public static void get_parameters()
    {
      FredParameters.GetParameter("hospital_contacts", ref Hospital.contacts_per_day);
      prob_transmission_per_contact = FredParameters.GetParameterMatrix<double>("hospital_trans_per_contact");
      var n = Convert.ToInt32(Math.Sqrt(Hospital.prob_transmission_per_contact.Length));
      if (Global.Verbose > 1)
      {
        Console.WriteLine("\nHospital contact_prob:\n");
        for (int i = 0; i < n; ++i)
        {
          for (int j = 0; j < n; ++j)
          {
            Console.WriteLine("%f ", Hospital.prob_transmission_per_contact[i, j]);
          }
          Console.WriteLine("\n");
        }
      }

      if (Global.Enable_HAZEL)
      {
        string HAZEL_hosp_init_file_name = string.Empty;
        string hosp_init_file_dir = string.Empty;

        HAZEL_reopening_CDF = FredParameters.GetParameterList<double>("HAZEL_reopening_CDF");
        FredParameters.GetParameter("HAZEL_disaster_capacity_multiplier", ref Hospital.HAZEL_disaster_capacity_multiplier);
        FredParameters.GetParameter("HAZEL_mobile_van_open_delay", ref Hospital.HAZEL_mobile_van_open_delay);
        FredParameters.GetParameter("HAZEL_mobile_van_closure_day", ref Hospital.HAZEL_mobile_van_closure_day);

        FredParameters.GetParameter("HAZEL_hospital_init_file_directory", ref hosp_init_file_dir);
        FredParameters.GetParameter("HAZEL_hospital_init_file_name", ref HAZEL_hosp_init_file_name);
        if (HAZEL_hosp_init_file_name == "none")
        {
          Hospital.HAZEL_hospital_init_map_file_exists = false;
        }
        else
        {
          var hazelFile = $"{hosp_init_file_dir}{HAZEL_hosp_init_file_name}";
          if (File.Exists(hazelFile))
          {
            using var HAZEL_hosp_init_map_fp = new StreamReader(hazelFile);
            Hospital.HAZEL_hospital_init_map_file_exists = true;
            while (HAZEL_hosp_init_map_fp.Peek() != -1)
            {
              var line = HAZEL_hosp_init_map_fp.ReadLine();
              var tokens = line.Split(',');
              // skip header line
              if (tokens[HOSP_ID] != "sp_id")
              {
                char place_type = Place.TYPE_HOSPITAL;
                string hosp_id_str = $"{place_type }{tokens[HOSP_ID]}";
                var init_data = new HAZEL_Hospital_Init_Data(tokens[PNL_WK], tokens[ACCPT_PRIV],
                  tokens[ACCPT_MEDICR], tokens[ACCPT_MEDICD], tokens[ACCPT_HGHMRK],
                  tokens[ACCPT_UPMC], tokens[ACCPT_UNINSRD], tokens[REOPEN_AFTR_DAYS],
                  tokens[IS_MOBILE], tokens[ADD_CAPACITY]);

                Hospital.HAZEL_hospital_init_map.Add(hosp_id_str, init_data);
              }
            }
            HAZEL_hosp_init_map_fp.Dispose();
          }
        }
      }

      if (Global.Enable_Health_Insurance || (Global.Enable_HAZEL && !Hospital.HAZEL_hospital_init_map_file_exists))
      {
        Hospital_health_insurance_prob = FredParameters.GetParameterList<double>("hospital_health_insurance_prob");
        Utils.assert((int)(Hospital.Hospital_health_insurance_prob.Count) == (int)(Insurance_assignment_index.UNSET));
      }
    }

    /**
     * @see Place.get_group(int disease, Person* per)
     */
    public override int get_group(int disease, Person per)
    {
      // 0 - Healthcare worker
      // 1 - Patient
      // 2 - Visitor
      if (per.get_activities().is_hospital_staff() && !per.is_hospitalized())
      {
        return 0;
      }

      var hosp = per.get_activities().get_hospital();
      if (hosp != null && hosp.get_id() == this.get_id())
      {
        return 1;
      }
      else
      {
        return 2;
      }
    }

    /**
     * @see Mixing_Group.get_transmission_prob(int disease, Person* i, Person* s)
     *
     * This method returns the value from the static array <code>Hospital.Hospital_contact_prob</code> that
     * corresponds to a particular age-related value for each person.<br />
     * The static array <code>Hospital_contact_prob</code> will be filled with values from the parameter
     * file for the key <code>hospital_prob[]</code>.
     */
    public override double get_transmission_prob(int disease, Person i, Person s)
    {
      // i = infected agent
      // s = susceptible agent
      int row = get_group(disease, i);
      int col = get_group(disease, s);
      double tr_pr = prob_transmission_per_contact[row, col];
      return tr_pr;
    }

    /**
     * @see Place.get_contacts_per_day(int disease)
     *
     * This method returns the value from the static array <code>Hospital.Hospital_contacts_per_day</code>
     * that corresponds to a particular disease.<br />
     * The static array <code>Hospital_contacts_per_day</code> will be filled with values from the parameter
     * file for the key <code>hospital_contacts[]</code>.
     */
    public override double get_contacts_per_day(int disease)
    {
      return contacts_per_day;
    }

    public override bool is_open(int sim_day)
    {
      return (sim_day < this.close_date || this.open_date <= sim_day);
    }

    /**
     * @see Place.should_be_open(int day)
     *
     * Determine if the Hospital should be open. This is independent of any disease.
     *
     * @param sim_day the simulation day
     * @return whether or not the hospital is open on the given day for the given disease
     */
    public bool should_be_open(int sim_day)
    {
      if (Global.Enable_HAZEL)
      {
        if (this.is_mobile_healthcare_clinic())
        {
          if (sim_day <= (Place_List.get_HAZEL_disaster_end_sim_day() + Hospital.HAZEL_mobile_van_open_delay))
          {
            //Not open until after disaster ends + some delay
            return false;
          }
          else
          {
            Utils.assert(this.HAZEL_closure_dates_have_been_set);
          }
        }
        else
        {
          // If we haven't made closure decision, do it now
          if (!this.HAZEL_closure_dates_have_been_set)
          {
            apply_individual_HAZEL_closure_policy();
          }
        }
      }
      return is_open(sim_day);
    }

    /**
     * @see Place.should_be_open(int day, int disease)
     *
     * Determine if the Hospital should be open. It is dependent on the disease and simulation day.
     *
     * @param sim_day the simulation day
     * @param disease an integer representation of the disease
     * @return whether or not the hospital is open on the given day for the given disease
     */
    public override bool should_be_open(int sim_day, int disease)
    {
      if (Global.Enable_HAZEL)
      {
        return this.should_be_open(sim_day);
      }

      return is_open(sim_day);
    }

    public void set_accepts_insurance(Insurance_assignment_index insr, bool does_accept)
    {
      switch (insr)
      {
        case Insurance_assignment_index.PRIVATE:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.PRIVATE] = does_accept;
          break;
        case Insurance_assignment_index.MEDICARE:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.MEDICARE] = does_accept;
          break;
        case Insurance_assignment_index.MEDICAID:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.MEDICAID] = does_accept;
          break;
        case Insurance_assignment_index.HIGHMARK:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.HIGHMARK] = does_accept;
          break;
        case Insurance_assignment_index.UPMC:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.UPMC] = does_accept;
          break;
        case Insurance_assignment_index.UNINSURED:
          this.accepted_insurance_bitset[(int)Insurance_assignment_index.UNINSURED] = does_accept;
          break;
        default:
          Utils.fred_abort("Invalid Insurance Assignment Type", "");
          break;
      }
    }

    public void set_accepts_insurance(int insr_indx, bool does_accept)
    {
      Utils.assert(insr_indx >= 0);
      Utils.assert(insr_indx < (int)(Insurance_assignment_index.UNSET));
      switch (insr_indx)
      {
        case (int)(Insurance_assignment_index.PRIVATE):
          set_accepts_insurance(Insurance_assignment_index.PRIVATE, does_accept);
          break;
        case (int)(Insurance_assignment_index.MEDICARE):
          set_accepts_insurance(Insurance_assignment_index.MEDICARE, does_accept);
          break;
        case (int)(Insurance_assignment_index.MEDICAID):
          set_accepts_insurance(Insurance_assignment_index.MEDICAID, does_accept);
          break;
        case (int)(Insurance_assignment_index.HIGHMARK):
          set_accepts_insurance(Insurance_assignment_index.HIGHMARK, does_accept);
          break;
        case (int)(Insurance_assignment_index.UPMC):
          set_accepts_insurance(Insurance_assignment_index.UPMC, does_accept);
          break;
        case (int)(Insurance_assignment_index.UNINSURED):
          set_accepts_insurance(Insurance_assignment_index.UNINSURED, does_accept);
          break;
        default:
          Utils.fred_abort("Invalid Insurance Assignment Type", "");
          break;
      }
    }

    public bool accepts_insurance(Insurance_assignment_index insr)
    {
      return this.accepted_insurance_bitset[(int)insr];
    }

    public int get_bed_count(int sim_day)
    {
      if (Global.Enable_HAZEL)
      {
        if (sim_day < Place_List.get_HAZEL_disaster_end_sim_day())
        {
          return this.bed_count;
        }
        else
        {
          if (this.add_capacity)
          {
            return Convert.ToInt32(Math.Ceiling(Hospital.HAZEL_disaster_capacity_multiplier * this.bed_count));
          }
          else
          {
            return this.bed_count;
          }
        }
      }
      else
      {
        return this.bed_count;
      }
    }

    public void set_bed_count(int _bed_count)
    {
      this.bed_count = _bed_count;
    }

    public int get_daily_patient_capacity(int sim_day)
    {
      if (Global.Enable_HAZEL)
      {
        if (sim_day < Place_List.get_HAZEL_disaster_end_sim_day())
        {
          return this.daily_patient_capacity;
        }
        else
        {
          if (this.add_capacity)
          {
            return Convert.ToInt32(Math.Ceiling(Hospital.HAZEL_disaster_capacity_multiplier * this.daily_patient_capacity));
          }
          else
          {
            return this.daily_patient_capacity;
          }
        }
      }
      else
      {
        return this.daily_patient_capacity;
      }
    }

    public void set_daily_patient_capacity(int _capacity)
    {
      this.daily_patient_capacity = _capacity;
    }

    public int get_current_daily_patient_count()
    {
      return this.current_daily_patient_count;
    }

    public void increment_current_daily_patient_count()
    {
      this.current_daily_patient_count++;
    }

    public void reset_current_daily_patient_count()
    {
      this.current_daily_patient_count = 0;
    }

    public int get_occupied_bed_count()
    {
      return this.occupied_bed_count;
    }

    public void increment_occupied_bed_count()
    {
      this.occupied_bed_count++;
    }

    public void decrement_occupied_bed_count()
    {
      this.occupied_bed_count--;
    }

    public void reset_occupied_bed_count()
    {
      this.occupied_bed_count = 0;
    }

    public void have_HAZEL_closure_dates_been_set(bool is_set)
    {
      this.HAZEL_closure_dates_have_been_set = is_set;
    }

    public static int get_HAZEL_mobile_van_open_delay()
    {
      return HAZEL_mobile_van_open_delay;
    }

    public static int get_HAZEL_mobile_van_closure_day()
    {
      return HAZEL_mobile_van_closure_day;
    }

    public override string ToString()
    {
      return $"Hospital[{this.get_label()}]: bed_count: {this.bed_count}"
        + $", occupied_bed_count: {this.occupied_bed_count}"
        + $", daily_patient_capacity: {this.daily_patient_capacity}"
        + $", current_daily_patient_count: {this.current_daily_patient_count}"
        + $", add_capacity: {this.add_capacity}"
        + $", HAZEL_closure_dates_have_been_set: {this.HAZEL_closure_dates_have_been_set}"
        + $", subtype: {this.get_subtype()}";
    }

    private void apply_individual_HAZEL_closure_policy()
    {
      Utils.assert(Global.Enable_HAZEL);
      if (!this.HAZEL_closure_dates_have_been_set)
      {
        if (Hospital.HAZEL_hospital_init_map_file_exists)
        {
          if (Hospital.HAZEL_hospital_init_map.ContainsKey(this.get_label()))
          {
            var init_data = Hospital.HAZEL_hospital_init_map[this.get_label()];
            if (init_data.reopen_after_days > 0)
            {
              if (Place_List.get_HAZEL_disaster_start_sim_day() != -1 && Place_List.get_HAZEL_disaster_end_sim_day() != -1)
              {
                this.set_close_date(Place_List.get_HAZEL_disaster_start_sim_day());
                this.set_open_date(Place_List.get_HAZEL_disaster_end_sim_day() + init_data.reopen_after_days);
                this.HAZEL_closure_dates_have_been_set = true;
                return;
              }
            }
            else if (init_data.reopen_after_days == 0 && !this.is_mobile_healthcare_clinic())
            {
              if (Place_List.get_HAZEL_disaster_start_sim_day() != -1 && Place_List.get_HAZEL_disaster_end_sim_day() != -1)
              {
                this.set_open_date(0);
                this.HAZEL_closure_dates_have_been_set = true;
                return;
              }
            }
          }
        }
      }
      else
      {
        return;
      }

      if (!this.HAZEL_closure_dates_have_been_set)
      {
        int cdf_day = FredRandom.DrawFromCdfVector(Hospital.HAZEL_reopening_CDF);
        this.set_close_date(Place_List.get_HAZEL_disaster_start_sim_day());
        this.set_open_date(Place_List.get_HAZEL_disaster_end_sim_day() + cdf_day);
        this.HAZEL_closure_dates_have_been_set = true;
      }
    }
  }
}
