using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Place : MixingGroup
  {
    public Place() : base("BLANK")
    {
      this.set_id(-1);      // actual id assigned in Place_List::add_place
      this.set_type(Place::TYPE_UNSET);
      this.set_subtype(Place::SUBTYPE_NONE);
      this.index = -1;
      this.staff_size = 0;
      this.household_fips = -1;
      this.open_date = 0;
      this.close_date = INT_MAX;
      this.intimacy = 0.0;
      this.first_day_infectious = -1;
      this.last_day_infectious = -2;
      this.county_index = -1;
      this.census_tract_index = -1;
      this.intimacy = 0.0;
      this.patch = NULL;

      double undefined = -1.0;
      this.longitude = undefined;
      this.latitude = undefined;

      int diseases = Global::Diseases.get_number_of_diseases();
      this.infectious_people = new std::vector<Person*>[diseases];

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
        this.infectious_people[d].clear();
      }

      this.vector_disease_data = NULL;
      this.vectors_have_been_infected_today = false;
      this.vector_control_status = false;
    }

    public Place(string lab, double lon, double lat) : base(lab)
    {
      this.set_id(-1);      // actual id assigned in Place_List::add_place
      this.set_type(Place::TYPE_UNSET);
      this.set_subtype(Place::SUBTYPE_NONE);
      this.index = -1;
      this.staff_size = 0;
      this.household_fips = -1;
      this.longitude = lon;
      this.latitude = lat;
      this.open_date = 0;
      this.close_date = INT_MAX;
      this.intimacy = 0.0;
      this.first_day_infectious = -1;
      this.last_day_infectious = -2;
      this.county_index = -1;
      this.census_tract_index = -1;
      this.intimacy = 0.0;
      this.patch = NULL;

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
        this.infectious_people[d].clear();
      }

      this.vector_disease_data = NULL;
      this.vectors_have_been_infected_today = false;
      this.vector_control_status = false;
    }

    public virtual void prepare()
    {
      this.N_orig = this.enrollees.size();
      for (int d = 0; d < Global::Diseases.get_number_of_diseases(); ++d)
      {
        this.new_infections[d] = 0;
        this.current_infections[d] = 0;
        this.new_symptomatic_infections[d] = 0;
      }
      this.open_date = 0;
      this.close_date = INT_MAX;
      this.infectious_bitset.reset();
      this.human_infectious_bitset.reset();
      this.exposed_bitset.reset();

      if (Global::Enable_Vector_Transmission)
      {
        setup_vector_model();
      }

      Global::Neighborhoods.register_place(this);

      FRED_VERBOSE(2, "Prepare place %d label %s type %c\n", this.get_id(), this.get_label(), this.get_type());
    }

    void Place::update(int sim_day)
    {
      // stub for future use.
    }

    void Place::print(int disease_id)
    {
      FRED_STATUS(0, "Place %d label %s type %c\n", this.get_id(), this.get_label(), this.get_type());
      fflush(stdout);
    }

    void Place::turn_workers_into_teachers(Place* school)
    {
      std::vector<Person*> workers;
      workers.reserve(static_cast<int>(this.enrollees.size()));
      workers.clear();
      for (int i = 0; i < static_cast<int>(this.enrollees.size()); ++i)
      {
        workers.push_back(this.enrollees[i]);
      }
      FRED_VERBOSE(0, "turn_workers_into_teachers: place %d %s has %d workers\n", this.get_id(), this.get_label(), this.enrollees.size());
      int new_teachers = 0;
      for (int i = 0; i < static_cast<int>(workers.size()); ++i)
      {
        Person* person = workers[i];
        assert(person != NULL);
        FRED_VERBOSE(0, "Potential teacher %d age %d\n", person.get_id(), person.get_age());
        if (person.become_a_teacher(school))
        {
          new_teachers++;
          FRED_VERBOSE(0, "new teacher %d age %d moved from workplace %d %s to school %d %s\n",
           person.get_id(), person.get_age(), this.get_id(), this.get_label(), school.get_id(), school.get_label());
        }
      }
      FRED_VERBOSE(0, "%d new teachers reassigned from workplace %s to school %s\n", new_teachers,
             this.get_label(), school.get_label());
    }

    void Place::reassign_workers(Place* new_place)
    {
      std::vector<Person*> workers;
      workers.reserve((int)this.enrollees.size());
      workers.clear();
      for (int i = 0; i < static_cast<int>(this.enrollees.size()); ++i)
      {
        workers.push_back(this.enrollees[i]);
      }
      int reassigned_workers = 0;
      for (int i = 0; i < static_cast<int>(workers.size()); ++i)
      {
        workers[i].change_workplace(new_place, 0);
        // printf("worker %d age %d moving from workplace %s to place %s\n",
        //   workers[i].get_id(), workers[i].get_age(), label, new_place.get_label());
        reassigned_workers++;
      }
      FRED_VERBOSE(1, "%d workers reassigned from workplace %s to place %s\n", reassigned_workers,
             this.get_label(), new_place.get_label());
    }

    int Place::get_visualization_counter(int day, int disease_id, int output_code)
    {
      switch (output_code)
      {
        case Global::OUTPUT_I:
          return this.get_number_of_infectious_people(disease_id);
          break;
        case Global::OUTPUT_Is:
          return this.get_current_symptomatic_infections(day, disease_id);
          break;
        case Global::OUTPUT_C:
          return this.get_new_infections(day, disease_id);
          break;
        case Global::OUTPUT_Cs:
          return this.get_new_symptomatic_infections(day, disease_id);
          break;
        case Global::OUTPUT_P:
          return this.get_current_infections(day, disease_id);
          break;
        case Global::OUTPUT_R:
          return this.get_recovereds(disease_id);
          break;
        case Global::OUTPUT_N:
          return this.get_size();
          break;
        case Global::OUTPUT_HC_DEFICIT:
          if (this.get_type() == Place::TYPE_HOUSEHOLD)
          {
            Household* hh = static_cast<Household*>(this);
            return hh.get_count_hc_accept_ins_unav();
          }
          else
          {
            return 0;
          }
          break;
      }
      return 0;
    }

    /////////////////////////////////////////
    //
    // PLACE-SPECIFIC TRANSMISSION DATA
    //
    /////////////////////////////////////////

    double Place::get_contact_rate(int sim_day, int disease_id)
    {

      Disease* disease = Global::Diseases.get_disease(disease_id);
      // expected number of susceptible contacts for each infectious person
      double contacts = get_contacts_per_day(disease_id) * disease.get_transmissibility();
      if (Global::Enable_Seasonality)
      {

        double m = Global::Clim.get_seasonality_multiplier_by_lat_lon(this.latitude, this.longitude, disease_id);
        //cout << "SEASONALITY: " << day << " " << m << endl;
        contacts *= m;
      }

      // increase neighborhood contacts on weekends
      if (this.is_neighborhood())
      {
        int day_of_week = Date::get_day_of_week();
        if (day_of_week == 0 || day_of_week == 6)
        {
          contacts = Neighborhood::get_weekend_contact_rate(disease_id) * contacts;
        }
      }
      // FRED_VERBOSE(1,"Disease %d, expected contacts = %f\n", disease_id, contacts);
      return contacts;
    }

    int Place::get_contact_count(Person* infector, int disease_id, int sim_day, double contact_rate)
    {
      // reduce number of infective contacts by infector's infectivity
      double infectivity = infector.get_infectivity(disease_id, sim_day);
      double infector_contacts = contact_rate * infectivity;

      FRED_VERBOSE(1, "infectivity = %f, so ", infectivity);
      FRED_VERBOSE(1, "infector's effective contacts = %f\n", infector_contacts);

      // randomly round off the expected value of the contact counts
      int contact_count = static_cast<int>(infector_contacts);
      double r = Random::draw_random();
      if (r < infector_contacts - contact_count)
      {
        contact_count++;
      }

      FRED_VERBOSE(1, "infector contact_count = %d  r = %f\n", contact_count, r);

      return contact_count;
    }

    //////////////////////////////////////////////////////////
    //
    // PLACE SPECIFIC VECTOR DATA
    //
    //////////////////////////////////////////////////////////


    void Place::setup_vector_model()
    {

      this.vector_disease_data = new vector_disease_data_t;

      // initial vector counts
      if (this.is_neighborhood())
      {
        // no vectors in neighborhoods (outdoors)
        this.vector_disease_data.vectors_per_host = 0.0;
      }
      else
      {
        this.vector_disease_data.vectors_per_host = 0.0;
        this.vector_disease_data.vectors_per_host = Global::Vectors.get_vectors_per_host(this);
      }
      this.vector_disease_data.N_vectors = this.N_orig * this.vector_disease_data.vectors_per_host;
      this.vector_disease_data.S_vectors = this.vector_disease_data.N_vectors;
      for (int i = 0; i < VECTOR_DISEASE_TYPES; ++i)
      {
        this.vector_disease_data.E_vectors[i] = 0;
        this.vector_disease_data.I_vectors[i] = 0;
      }

      // initial vector seed counts
      for (int i = 0; i < VECTOR_DISEASE_TYPES; ++i)
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
          this.vector_disease_data.place_seeds[i] = Global::Vectors.get_seeds(this, i);
          this.vector_disease_data.day_start_seed[i] = Global::Vectors.get_day_start_seed(this, i);
          this.vector_disease_data.day_end_seed[i] = Global::Vectors.get_day_end_seed(this, i);
        }
      }
      FRED_VERBOSE(1, "setup_vector_model: place %s vectors_per_host %f N_vectors %d N_orig %d\n",
             this.get_label(), this.vector_disease_data.vectors_per_host,
             this.vector_disease_data.N_vectors, this.N_orig);
    }

    double Place::get_seeds(int dis, int sim_day)
    {
      if ((sim_day) || (sim_day > this.vector_disease_data.day_end_seed[dis]))
      {
        return 0.0;
      }
      else
      {
        return this.vector_disease_data.place_seeds[dis];
      }
    }

    void Place::update_vector_population(int day)
    {
      if (this.is_neighborhood() == false)
      {
        *(this.vector_disease_data) = Global::Vectors.update_vector_population(day, this);
      }
    }

    public static string get_place_label(Place p)
    {
      return (p == null) ? "-1" : p.Label;
    }




    // test place types
    bool is_household()
    {
      return this.get_type() == Place::TYPE_HOUSEHOLD;
    }

    bool is_neighborhood()
    {
      return this.get_type() == Place::TYPE_NEIGHBORHOOD;
    }

    bool is_school()
    {
      return this.get_type() == Place::TYPE_SCHOOL;
    }

    bool is_classroom()
    {
      return this.get_type() == Place::TYPE_CLASSROOM;
    }

    bool is_workplace()
    {
      return this.get_type() == Place::TYPE_WORKPLACE;
    }

    bool is_office()
    {
      return this.get_type() == Place::TYPE_OFFICE;
    }

    bool is_hospital()
    {
      return this.get_type() == Place::TYPE_HOSPITAL;
    }

    bool is_community()
    {
      return this.get_type() == Place::TYPE_COMMUNITY;
    }

    // test place subtypes
    bool is_college()
    {
      return this.get_subtype() == Place::SUBTYPE_COLLEGE;
    }

    bool is_prison()
    {
      return this.get_subtype() == Place::SUBTYPE_PRISON;
    }

    bool is_nursing_home()
    {
      return this.get_subtype() == Place::SUBTYPE_NURSING_HOME;
    }

    bool is_military_base()
    {
      return this.get_subtype() == Place::SUBTYPE_MILITARY_BASE;
    }

    bool is_healthcare_clinic()
    {
      return this.get_subtype() == Place::SUBTYPE_HEALTHCARE_CLINIC;
    }

    bool is_mobile_healthcare_clinic()
    {
      return this.get_subtype() == Place::SUBTYPE_MOBILE_HEALTHCARE_CLINIC;
    }

    bool is_group_quarters()
    {
      return (is_college() || is_prison() || is_military_base() || is_nursing_home());
    }

    // test for household types
    bool is_college_dorm()
    {
      return is_household() && is_college();
    }

    bool is_prison_cell()
    {
      return is_household() && is_prison();
    }

    bool is_military_barracks()
    {
      return is_household() && is_military_base();
    }

    /**
     * Get the latitude.
     *
     * @return the latitude
     */
    public double get_latitude()
    {
      return this.latitude;
    }

    /**
     * Get the longitude.
     *
     * @return the longitude
     */
    public double get_longitude()
    {
      return this.longitude;
    }

    double get_distance(Place place)
    {
      double x1 = this.get_x();
      double y1 = this.get_y();
      double x2 = place.get_x();
      double y2 = place.get_y();
      double distance = sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
      return distance;
    }

    /**
     * Get the simulation day (an integer value of days from the start of the simulation) when the place will close.
     *
     * @return the close_date
     */
    int get_close_date()
    {
      return this.close_date;
    }

    /**
     * Get the simulation day (an integer value of days from the start of the simulation) when the place will open.
     *
     * @return the open_date
     */
    int get_open_date()
    {
      return this.open_date;
    }

    /**
     * Set the latitude.
     *
     * @param x the new latitude
     */
    void set_latitude(double x)
    {
      this.latitude = x;
    }

    /**
     * Set the longitude.
     *
     * @param x the new longitude
     */
    void set_longitude(double x)
    {
      this.longitude = x;
    }

    /**
     * Set the simulation day (an integer value of days from the start of the simulation) when the place will close.
     *
     * @param day the simulation day when the place will close
     */
    void set_close_date(int sim_day)
    {
      this.close_date = sim_day;
    }

    /**
     * Set the simulation day (an integer value of days from the start of the simulation) when the place will open.
     *
     * @param day the simulation day when the place will open
     */
    void set_open_date(int sim_day)
    {
      this.open_date = sim_day;
    }

    /**
     * Get the patch where this place is.
     *
     * @return a pointer to the patch where this place is
     */
    NeighborhoodPatch get_patch()
    {
      return this.patch;
    }

    /**
     * Set the patch where this place will be.
     *
     * @param p the new patch
     */
    void set_patch(NeighborhoodPatch p)
    {
      this.patch = p;
    }

    double get_x()
    {
      return Geo::get_x(this.longitude);
    }

    double get_y()
    {
      return Geo::get_y(this.latitude);
    }

    void set_index(int _index)
    {
      this.index = _index;
    }

    int get_index()
    {
      return this.index;
    }

    int get_staff_size()
    {
      return this.staff_size;
    }

    void set_staff_size(int _staff_size)
    {
      this.staff_size = _staff_size;
    }

    int get_household_fips()
    {
      return this.household_fips;
    }

    void set_household_fips(int input_fips)
    {
      this.household_fips = input_fips;
    }

    void set_county_index(int _county_index)
    {
      this.county_index = _county_index;
    }

    int get_county_index()
    {
      return this.county_index;
    }

    void set_census_tract_index(int _census_tract_index)
    {
      this.census_tract_index = _census_tract_index;
    }

    int get_census_tract_index()
    {
      return this.census_tract_index;
    }

    void mark_vectors_as_not_infected_today()
    {
      this.vectors_have_been_infected_today = false;
    }

    void mark_vectors_as_infected_today()
    {
      this.vectors_have_been_infected_today = true;
    }

    bool have_vectors_been_infected_today()
    {
      return this.vectors_have_been_infected_today;
    }

    int get_vector_population_size()
    {
      return this.vector_disease_data.N_vectors;
    }

    int get_susceptible_vectors()
    {
      return this.vector_disease_data.S_vectors;
    }

    int get_infected_vectors(int disease_id)
    {
      return this.vector_disease_data.E_vectors[disease_id] +
        this.vector_disease_data.I_vectors[disease_id];
    }

    int get_infectious_vectors(int disease_id)
    {
      return this.vector_disease_data.I_vectors[disease_id];
    }

    void expose_vectors(int disease_id, int exposed_vectors)
    {
      this.vector_disease_data.E_vectors[disease_id] += exposed_vectors;
      this.vector_disease_data.S_vectors -= exposed_vectors;
    }

    vector_disease_data_t get_vector_disease_data()
    {
      assert(this.vector_disease_data != NULL);
      return (*this.vector_disease_data);
    }

    void update_vector_population(int sim_day);

    bool get_vector_control_status()
    {
      return this.vector_control_status;
    }
    void set_vector_control()
    {
      vector_control_status = true;
    }
    void stop_vector_control()
    {
      vector_control_status = false;
    }
    public virtual void print(int disease_id) { }

    // daily update
    public virtual void update(int sim_day) { }

    public virtual bool is_open(int sim_day)
    {
      return true;
    }

    /**
     * Get the transmission probability for a given disease between two Person objects.
     *
     * @see Mixing_Group::get_transmission_probability(int disease_id, Person* i, Person* s)
     */
    public virtual double get_transmission_probability(int disease_id, Person i, Person s)
    {
      return 1.0;
    }

    public virtual double get_contacts_per_day(int disease_id) { return 0.0; } // access functions

  /**
   * Determine if the place should be open. It is dependent on the disease_id and simulation day.
   *
   * @param day the simulation day
   * @param disease_id an integer representation of the disease
   * @return <code>true</code> if the place should be open; <code>false</code> if not
   */
    public virtual bool should_be_open(int sim_day, int disease_id) { return false; }
  }
}
