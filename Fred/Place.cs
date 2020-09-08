using System;
using System.Collections.Generic;

namespace Fred
{
  public class Place : Mixing_Group
  {
    // static place type codes
    public const char TYPE_HOUSEHOLD = 'H';
    public const char TYPE_NEIGHBORHOOD = 'N';
    public const char TYPE_SCHOOL = 'S';
    public const char TYPE_CLASSROOM = 'C';
    public const char TYPE_WORKPLACE = 'W';
    public const char TYPE_OFFICE = 'O';
    public const char TYPE_HOSPITAL = 'M';
    public const char TYPE_COMMUNITY = 'X';

    // static place subtype codes
    public const char SUBTYPE_COLLEGE = 'C';
    public const char SUBTYPE_PRISON = 'P';
    public const char SUBTYPE_MILITARY_BASE = 'M';
    public const char SUBTYPE_NURSING_HOME = 'N';
    public const char SUBTYPE_HEALTHCARE_CLINIC = 'I';
    public const char SUBTYPE_MOBILE_HEALTHCARE_CLINIC = 'Z';

    protected static double[,] prob_contact;
    protected FredGeo latitude;     // geo location
    protected FredGeo longitude;    // geo location
    protected int close_date;         // this place will be closed during:
    protected int open_date;          //   [close_date, open_date)
    protected double intimacy;        // prob of intimate contact
    protected int index;              // index for households
    protected int staff_size;         // outside workers in this place
    protected int household_fips;
    protected int county_index;
    protected int census_tract_index;
    protected Neighborhood_Patch patch;       // geo patch for this place
    // optional data for vector transmission model
    protected vector_disease_data_t vector_disease_data;
    protected bool vectors_have_been_infected_today;
    protected bool vector_control_status;

    /**
   * Default constructor
   * Note: really only used by Allocator
   */
    public Place()
      : base ("BLANK")
    {
      this.set_id(-1);      // actual id assigned in Place_List::add_place
      this.set_type(TYPE_UNSET);
      this.set_subtype(SUBTYPE_NONE);
      this.index = -1;
      this.staff_size = 0;
      this.household_fips = -1;
      this.open_date = 0;
      this.close_date = int.MaxValue;
      this.intimacy = 0.0;
      this.enrollees = new List<Person>();
      this.first_day_infectious = -1;
      this.last_day_infectious = -2;
      this.county_index = -1;
      this.census_tract_index = -1;
      this.intimacy = 0.0;

      FredGeo undefined = new FredGeo { Value = -1.0 };
      this.longitude = undefined;
      this.latitude = undefined;

      int diseases = Global.Diseases.get_number_of_diseases();
      this.infectious_people = new List<Person>[diseases];

      this.new_infections = new int[diseases];
      this.current_infections = new int[diseases];
      this.total_infections = new int[diseases];
      this.new_symptomatic_infections = new int[diseases];
      this.current_symptomatic_infections = new int[diseases];
      this.total_symptomatic_infections = new int[diseases];

      // zero out all disease-specific counts
      for (int d = 0; d < diseases; ++d)
      {
        this.new_infections[d] = 0;
        this.current_infections[d] = 0;
        this.total_infections[d] = 0;
        this.new_symptomatic_infections[d] = 0;
        this.total_symptomatic_infections[d] = 0;
        this.infectious_people[d] = new List<Person>();
      }

      this.vector_disease_data = null;
      this.vectors_have_been_infected_today = false;
      this.vector_control_status = false;
    }

    /**
     * Constructor with necessary parameters
     */
    public Place(string label, FredGeo lon, FredGeo lat)
      : base (label)
    {
      this.set_id(-1);      // actual id assigned in Place_List::add_place
      this.set_type(TYPE_UNSET);
      this.set_subtype(SUBTYPE_NONE);
      this.index = -1;
      this.staff_size = 0;
      this.household_fips = -1;
      this.longitude = lon;
      this.latitude = lat;
      this.open_date = 0;
      this.close_date = int.MaxValue;
      this.intimacy = 0.0;
      this.enrollees = new List<Person>();
      this.first_day_infectious = -1;
      this.last_day_infectious = -2;
      this.county_index = -1;
      this.census_tract_index = -1;
      this.intimacy = 0.0;

      int diseases = Global.Diseases.get_number_of_diseases();
      this.infectious_people = new List<Person>[diseases];

      this.new_infections = new int[diseases];
      this.current_infections = new int[diseases];
      this.total_infections = new int[diseases];
      this.new_symptomatic_infections = new int[diseases];
      this.current_symptomatic_infections = new int[diseases];
      this.total_symptomatic_infections = new int[diseases];

      // zero out all disease-specific counts
      for (int d = 0; d < diseases; ++d)
      {
        this.new_infections[d] = 0;
        this.current_infections[d] = 0;
        this.total_infections[d] = 0;
        this.new_symptomatic_infections[d] = 0;
        this.total_symptomatic_infections[d] = 0;
        this.infectious_people[d] = new List<Person>();
      }

      this.vector_disease_data = null;
      this.vectors_have_been_infected_today = false;
      this.vector_control_status = false;
    }

    public virtual void prepare()
    {
      this.N_orig = this.enrollees.Count;
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
      {
        this.new_infections[d] = 0;
        this.current_infections[d] = 0;
        this.new_symptomatic_infections[d] = 0;
      }
      this.open_date = 0;
      this.close_date = int.MaxValue;
      this.infectious_bitset.SetAll(false);
      this.human_infectious_bitset.SetAll(false);
      this.exposed_bitset.SetAll(false);

      if (Global.Enable_Vector_Transmission)
      {
        setup_vector_model();
      }

      Global.Neighborhoods.register_place(this);

      Utils.FRED_VERBOSE(2, "Prepare place {0} label {1} type {2}", this.get_id(), this.get_label(), this.get_type());
    }

    public virtual void print(int disease_id)
    {
      Utils.FRED_STATUS(0, "Place {0} label {1} type {2}", this.get_id(), this.get_label(), this.get_type());
    }

    // daily update
    public virtual void update(int sim_day) { }

    public void reset_visualization_data(int sim_day)
    {
    }

    public virtual bool is_open(int sim_day)
    {
      return true;
    }

    public override double get_contact_rate(int day, int disease_id)
    {
      var disease = Global.Diseases.get_disease(disease_id);
      // expected number of susceptible contacts for each infectious person
      double contacts = get_contacts_per_day(disease_id) * disease.get_transmissibility();
      if (Global.Enable_Seasonality)
      {

        double m = Global.Clim.get_seasonality_multiplier_by_lat_lon(this.latitude, this.longitude, disease_id);
        //cout << "SEASONALITY: " << day << " " << m << endl;
        contacts *= m;
      }

      // increase neighborhood contacts on weekends
      if (this.is_neighborhood())
      {
        int day_of_week = Date.get_day_of_week();
        if (day_of_week == 0 || day_of_week == 6)
        {
          contacts = Neighborhood.get_weekend_contact_rate(disease_id) * contacts;
        }
      }
      // FRED_VERBOSE(1,"Disease %d, expected contacts = %f\n", disease_id, contacts);
      return contacts;
    }

    public override int get_contact_count(Person infector, int disease_id, int sim_day, double contact_rate)
    {
      // reduce number of infective contacts by infector's infectivity
      double infectivity = infector.get_infectivity(disease_id, sim_day);
      double infector_contacts = contact_rate * infectivity;

      Utils.FRED_VERBOSE(1, "infectivity = {0}, so ", infectivity);
      Utils.FRED_VERBOSE(1, "infector's effective contacts = {0}", infector_contacts);

      // randomly round off the expected value of the contact counts
      int contact_count = Convert.ToInt32(infector_contacts);
      double r = FredRandom.NextDouble();
      if (r < infector_contacts - contact_count)
      {
        contact_count++;
      }

      Utils.FRED_VERBOSE(1, "infector contact_count = {0}  r = {1}", contact_count, r);
      return contact_count;
    }

    /**
     * Determine if the place should be open. It is dependent on the disease_id and simulation day.
     *
     * @param day the simulation day
     * @param disease_id an integer representation of the disease
     * @return <code>true</code> if the place should be open; <code>false</code> if not
     */
    public virtual bool should_be_open(int sim_day, int disease_id) { return false; }

    // test place types
    public bool is_household()
    {
      return this.get_type() == TYPE_HOUSEHOLD;
    }

    public bool is_neighborhood()
    {
      return this.get_type() == TYPE_NEIGHBORHOOD;
    }

    public bool is_school()
    {
      return this.get_type() == TYPE_SCHOOL;
    }

    public bool is_classroom()
    {
      return this.get_type() == TYPE_CLASSROOM;
    }

    public bool is_workplace()
    {
      return this.get_type() ==TYPE_WORKPLACE;
    }

    public bool is_office()
    {
      return this.get_type() == TYPE_OFFICE;
    }

    public bool is_hospital()
    {
      return this.get_type() == TYPE_HOSPITAL;
    }

    public bool is_community()
    {
      return this.get_type() == TYPE_COMMUNITY;
    }

    // test place subtypes
    public bool is_college()
    {
      return this.get_subtype() == SUBTYPE_COLLEGE;
    }

    public bool is_prison()
    {
      return this.get_subtype() == SUBTYPE_PRISON;
    }

    public bool is_nursing_home()
    {
      return this.get_subtype() == SUBTYPE_NURSING_HOME;
    }

    public bool is_military_base()
    {
      return this.get_subtype() == SUBTYPE_MILITARY_BASE;
    }

    public bool is_healthcare_clinic()
    {
      return this.get_subtype() == SUBTYPE_HEALTHCARE_CLINIC;
    }

    public bool is_mobile_healthcare_clinic()
    {
      return this.get_subtype() == SUBTYPE_MOBILE_HEALTHCARE_CLINIC;
    }

    public bool is_group_quarters()
    {
      return (is_college() || is_prison() || is_military_base() || is_nursing_home());
    }

    // test for household types
    public bool is_college_dorm()
    {
      return is_household() && is_college();
    }

    public bool is_prison_cell()
    {
      return is_household() && is_prison();
    }

    public bool is_military_barracks()
    {
      return is_household() && is_military_base();
    }

    /**
     * Get the latitude.
     *
     * @return the latitude
     */
    public FredGeo get_latitude()
    {
      return this.latitude;
    }

    /**
     * Get the longitude.
     *
     * @return the longitude
     */
    public FredGeo get_longitude()
    {
      return this.longitude;
    }

    public double get_distance(Place place)
    {
      double x1 = this.get_x();
      double y1 = this.get_y();
      double x2 = place.get_x();
      double y2 = place.get_y();
      double distance = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
      return distance;
    }

    /**
     * Get the simulation day (an integer value of days from the start of the simulation) when the place will close.
     *
     * @return the close_date
     */
    public int get_close_date()
    {
      return this.close_date;
    }

    /**
     * Get the simulation day (an integer value of days from the start of the simulation) when the place will open.
     *
     * @return the open_date
     */
    public int get_open_date()
    {
      return this.open_date;
    }

    /**
     * Set the latitude.
     *
     * @param x the new latitude
     */
    public void set_latitude(double x)
    {
      this.latitude = new FredGeo { Value = x };
    }

    /**
     * Set the longitude.
     *
     * @param x the new longitude
     */
    public void set_longitude(double x)
    {
      this.longitude = new FredGeo { Value = x };
    }

    /**
     * Set the simulation day (an integer value of days from the start of the simulation) when the place will close.
     *
     * @param day the simulation day when the place will close
     */
    public void set_close_date(int sim_day)
    {
      this.close_date = sim_day;
    }

    /**
     * Set the simulation day (an integer value of days from the start of the simulation) when the place will open.
     *
     * @param day the simulation day when the place will open
     */
    public void set_open_date(int sim_day)
    {
      this.open_date = sim_day;
    }

    /**
     * Get the patch where this place is.
     *
     * @return a pointer to the patch where this place is
     */
    public Neighborhood_Patch get_patch()
    {
      return this.patch;
    }

    /**
     * Set the patch where this place will be.
     *
     * @param p the new patch
     */
    public void set_patch(Neighborhood_Patch p)
    {
      this.patch = p;
    }

    public int get_visualization_counter(int day, int disease_id, int output_code)
    {
      switch (output_code)
      {
        case Global.OUTPUT_I:
          return this.get_number_of_infectious_people(disease_id);
        case Global.OUTPUT_Is:
          return this.get_current_symptomatic_infections(day, disease_id);
        case Global.OUTPUT_C:
          return this.get_new_infections(day, disease_id);
        case Global.OUTPUT_Cs:
          return this.get_new_symptomatic_infections(day, disease_id);
        case Global.OUTPUT_P:
          return this.get_current_infections(day, disease_id);
        case Global.OUTPUT_R:
          return this.get_recovereds(disease_id);
        case Global.OUTPUT_N:
          return this.get_size();
        case Global.OUTPUT_HC_DEFICIT:
          if (this.get_type() == TYPE_HOUSEHOLD)
          {
            var hh = (Household)this;
            return hh.get_count_hc_accept_ins_unav();
          }
          return 0;
      }
      return 0;
    }

    public void turn_workers_into_teachers(Place school)
    {
      var workers = new List<Person>();
      for (int i = 0; i < this.enrollees.Count; ++i)
      {
        workers.Add(this.enrollees[i]);
      }
      Utils.FRED_VERBOSE(0, "turn_workers_into_teachers: place {0} {1} has {2} workers", this.get_id(), this.get_label(), this.enrollees.Count);
      int new_teachers = 0;
      for (int i = 0; i < workers.Count; ++i)
      {
        var person = workers[i];
        Utils.assert(person != null);
        Utils.FRED_VERBOSE(0, "Potential teacher {0} age {1}", person.get_id(), person.get_age());
        if (person.become_a_teacher(school))
        {
          new_teachers++;
          Utils.FRED_VERBOSE(0, "new teacher {0} age {1} moved from workplace {2} {3} to school {4} {5}",
           person.get_id(), person.get_age(), this.get_id(), this.get_label(), school.get_id(), school.get_label());
        }
      }
      Utils.FRED_VERBOSE(0, "{0} new teachers reassigned from workplace {1} to school {2}", new_teachers,
             this.get_label(), school.get_label());
    }

    public void reassign_workers(Place new_place)
    {
      var workers = new List<Person>();
      for (int i = 0; i < this.enrollees.Count; ++i)
      {
        workers.Add(this.enrollees[i]);
      }
      int reassigned_workers = 0;
      for (int i = 0; i < workers.Count; ++i)
      {
        workers[i].change_workplace(new_place, 0);
        // printf("worker %d age %d moving from workplace %s to place %s\n",
        //   workers[i].get_id(), workers[i].get_age(), label, new_place.get_label());
        reassigned_workers++;
      }
      Utils.FRED_VERBOSE(1, "{0} workers reassigned from workplace {1} to place {2}", reassigned_workers,
             this.get_label(), new_place.get_label());
    }

    public double get_x()
    {
      return Geo.get_x(this.longitude);
    }

    public double get_y()
    {
      return Geo.get_y(this.latitude);
    }

    public void set_index(int _index)
    {
      this.index = _index;
    }

    public int get_index()
    {
      return this.index;
    }

    public int get_staff_size()
    {
      return this.staff_size;
    }

    public void set_staff_size(int _staff_size)
    {
      this.staff_size = _staff_size;
    }

    public int get_household_fips()
    {
      return this.household_fips;
    }

    public void set_household_fips(int input_fips)
    {
      this.household_fips = input_fips;
    }

    public void set_county_index(int _county_index)
    {
      this.county_index = _county_index;
    }

    public int get_county_index()
    {
      return this.county_index;
    }

    public void set_census_tract_index(int _census_tract_index)
    {
      this.census_tract_index = _census_tract_index;
    }

    public int get_census_tract_index()
    {
      return this.census_tract_index;
    }

    public static string get_place_label(Place p)
    {
      return p == null ? "-1 " : $"{p.get_label()} ";
    }

    public double get_seeds(int dis, int sim_day)
    {
      if (sim_day > 0 || (sim_day > this.vector_disease_data.day_end_seed[dis]))
      {
        return 0.0;
      }
      
      return this.vector_disease_data.place_seeds[dis];
    }

    /*
     * Vector Transmission methods
     */
    public void setup_vector_model()
    {
      this.vector_disease_data = new vector_disease_data_t();

      // initial vector counts
      if (this.is_neighborhood())
      {
        // no vectors in neighborhoods (outdoors)
        this.vector_disease_data.vectors_per_host = 0.0;
      }
      else
      {
        this.vector_disease_data.vectors_per_host = 0.0;
        this.vector_disease_data.vectors_per_host = Global.Vectors.get_vectors_per_host(this);
      }
      this.vector_disease_data.N_vectors = Convert.ToInt32(this.N_orig * this.vector_disease_data.vectors_per_host);
      this.vector_disease_data.S_vectors = this.vector_disease_data.N_vectors;
      for (int i = 0; i < vector_disease_data_t.VECTOR_DISEASE_TYPES; ++i)
      {
        this.vector_disease_data.E_vectors[i] = 0;
        this.vector_disease_data.I_vectors[i] = 0;
      }

      // initial vector seed counts
      for (int i = 0; i < vector_disease_data_t.VECTOR_DISEASE_TYPES; ++i)
      {
        if (this.is_neighborhood())
        {
          // no vectors in neighborhoods (outdoors)
          this.vector_disease_data.place_seeds[i] = 0;
          this.vector_disease_data.day_start_seed[i] = 0;
          this.vector_disease_data.day_end_seed[i] = 1;
        }
        else
        {
          this.vector_disease_data.place_seeds[i] =    Convert.ToInt32(Global.Vectors.get_seeds(this, i));
          this.vector_disease_data.day_start_seed[i] = Convert.ToInt32(Global.Vectors.get_day_start_seed(this, i));
          this.vector_disease_data.day_end_seed[i] =   Convert.ToInt32(Global.Vectors.get_day_end_seed(this, i));
        }
      }
      Utils.FRED_VERBOSE(1, "setup_vector_model: place {0} vectors_per_host {1} N_vectors {2} N_orig {3}",
             this.get_label(), this.vector_disease_data.vectors_per_host,
             this.vector_disease_data.N_vectors, this.N_orig);
    }

    public void mark_vectors_as_not_infected_today()
    {
      this.vectors_have_been_infected_today = false;
    }

    public void mark_vectors_as_infected_today()
    {
      this.vectors_have_been_infected_today = true;
    }

    public bool have_vectors_been_infected_today()
    {
      return this.vectors_have_been_infected_today;
    }

    public int get_vector_population_size()
    {
      return this.vector_disease_data.N_vectors;
    }

    public int get_susceptible_vectors()
    {
      return this.vector_disease_data.S_vectors;
    }

    public int get_infected_vectors(int disease_id)
    {
      return this.vector_disease_data.E_vectors[disease_id] +
        this.vector_disease_data.I_vectors[disease_id];
    }

    public int get_infectious_vectors(int disease_id)
    {
      return this.vector_disease_data.I_vectors[disease_id];
    }

    public void expose_vectors(int disease_id, int exposed_vectors)
    {
      this.vector_disease_data.E_vectors[disease_id] += exposed_vectors;
      this.vector_disease_data.S_vectors -= exposed_vectors;
    }

    public vector_disease_data_t get_vector_disease_data()
    {
      Utils.assert(this.vector_disease_data != null);
      return this.vector_disease_data;
    }

    public void update_vector_population(int sim_day)
    {
      if (this.is_neighborhood() == false)
      {
        this.vector_disease_data = Global.Vectors.update_vector_population(sim_day, this);
      }
    }

    public bool get_vector_control_status()
    {
      return this.vector_control_status;
    }

    public void set_vector_control()
    {
      vector_control_status = true;
    }

    public void stop_vector_control()
    {
      vector_control_status = false;
    }
  }
}
