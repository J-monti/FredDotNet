using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Infection
  {
    protected Disease disease;

    // people involved
    protected Person infector;
    protected Person host;

    // where infection was caught
    protected Mixing_Group mixing_group;

    // date of infection (all dates are in sim_days)
    protected int exposure_date;

    // person is infectious starting infectious_start_date until infectious_end_date
    protected int infectious_start_date;
    protected int infectious_end_date;

    // person is symptomatic starting symptoms_start_date until symptoms_end_date
    protected bool will_develop_symptoms;
    protected int symptoms_start_date;          // -1 if never symptomatic
    protected int symptoms_end_date;          // -1 if never symptomatic

    // person is immune from infection starting on exposure_date until immunity_end_date
    protected int immunity_end_date;    // -1 if immune forever after recovery

    // is infection fatal today?
    protected bool infection_is_fatal_today;

    // if primary infection, infector and place are null.
    public Infection(Disease _disease, Person _infector, Person _host, Mixing_Group _mixing_group, int day)
    {
      this.disease = _disease;
      this.infector = _infector;
      this.host = _host;
      this.mixing_group = _mixing_group;
      this.exposure_date = day;
      this.infectious_start_date = -1;
      this.infectious_end_date = -1;
      this.symptoms_start_date = -1;
      this.symptoms_end_date = -1;
      this.immunity_end_date = -1;
      this.will_develop_symptoms = false;
      this.infection_is_fatal_today = false;
    }

    /**
     * This static factory method is used to get an instance of a specific
     * Infection that tracks patient-specific data that depends on the
     * natural history model associated with the disease.
     *
     * @param a pointer to the disease causing this infection.
     * @return a pointer to a specific Infection object of a possible derived class
     */
    public static Infection get_new_infection(Disease disease, Person infector, Person host, Mixing_Group mixing_group, int day)
    {
      if (disease.get_natural_history_model() == "basic")
      {
        return new Infection(disease, infector, host, mixing_group, day);
      }

      if (disease.get_natural_history_model() == "markov")
      {
        return new Markov_Infection(disease, infector, host, mixing_group, day);
      }

      if (disease.get_natural_history_model() == "hiv")
      {
        return new HIV_Infection(disease, infector, host, mixing_group, day);
      }

      Utils.fred_abort("Infection.get_new_infection -- unknown natural history model: {0}",
            disease.get_natural_history_model());
      return null;
    }

    /*
     * The Infection base class defines a SEIR(S) model.  For other
     * models, define the following virtual methods in a dervived class.
     */

    public virtual void setup()
    {
      Utils.FRED_VERBOSE(1, "infection.setup entered\n");

      // decide if this host will develop symptoms
      double prob_symptoms = this.disease.get_natural_history().get_probability_of_symptoms(this.host.get_age());
      this.will_develop_symptoms = (FredRandom.NextDouble() < prob_symptoms);

      // set transition date for becoming susceptible after this infection
      int my_duration_of_immunity = this.disease.get_natural_history().get_duration_of_immunity(this.host);
      // my_duration_of_immunity <= 0 means "immune forever"
      if (my_duration_of_immunity > 0)
      {
        this.immunity_end_date = this.exposure_date + my_duration_of_immunity;
      }
      else
      {
        this.immunity_end_date = Natural_History.NEVER;
      }

      double incubation_period = 0.0;
      double symptoms_duration = 0.0;

      // determine dates for symptoms
      int symptoms_distribution_type = this.disease.get_natural_history().get_symptoms_distribution_type();
      if (symptoms_distribution_type == Natural_History.LOGNORMAL)
      {
        incubation_period = this.disease.get_natural_history().get_real_incubation_period(this.host);
        symptoms_duration = this.disease.get_natural_history().get_symptoms_duration(this.host);

        // find symptoms dates (assuming symptoms will occur)
        this.symptoms_start_date = this.exposure_date + Convert.ToInt32(Math.Round(incubation_period));
        this.symptoms_end_date = this.exposure_date + Convert.ToInt32(Math.Round(incubation_period + symptoms_duration));
      }
      else
      {
        // distribution type == CDF
        int my_incubation_period = this.disease.get_natural_history().get_incubation_period(this.host);
        Utils.assert(my_incubation_period > 0); // FRED needs at least one day to become symptomatic
        this.symptoms_start_date = this.exposure_date + my_incubation_period;

        int my_duration_of_symptoms = this.disease.get_natural_history().get_duration_of_symptoms(this.host);
        // duration_of_symptoms <= 0 would mean "symptomatic forever"
        if (my_duration_of_symptoms > 0)
        {
          this.symptoms_end_date = this.symptoms_start_date + my_duration_of_symptoms;
        }
        else
        {
          this.symptoms_end_date = Natural_History.NEVER;
        }
      }

      // determine dates for infectiousness
      int infectious_distribution_type = this.disease.get_natural_history().get_infectious_distribution_type();
      if (infectious_distribution_type == Natural_History.OFFSET_FROM_START_OF_SYMPTOMS ||
         infectious_distribution_type == Natural_History.OFFSET_FROM_SYMPTOMS)
      {
        // set infectious dates based on offset
        double infectious_start_offset = this.disease.get_natural_history().get_infectious_start_offset(this.host);
        double infectious_end_offset = this.disease.get_natural_history().get_infectious_end_offset(this.host);

        // apply the offset
        this.infectious_start_date = this.symptoms_start_date + Convert.ToInt32(Math.Round(infectious_start_offset));
        if (infectious_distribution_type == Natural_History.OFFSET_FROM_START_OF_SYMPTOMS)
        {
          this.infectious_end_date = this.symptoms_start_date + Convert.ToInt32(Math.Round(infectious_end_offset));
        }
        else
        {
          this.infectious_end_date = this.symptoms_end_date + Convert.ToInt32(Math.Round(infectious_end_offset));
        }
      }
      else if (infectious_distribution_type == Natural_History.LOGNORMAL)
      {
        double latent_period = this.disease.get_natural_history().get_real_latent_period(this.host);
        double infectious_duration = this.disease.get_natural_history().get_infectious_duration(this.host);
        this.infectious_start_date = this.exposure_date + Convert.ToInt32(Math.Round(latent_period));
        this.infectious_end_date = this.exposure_date + Convert.ToInt32(Math.Round(latent_period + infectious_duration));
      }
      else
      {
        // distribution type == CDF
        int my_latent_period = this.disease.get_natural_history().get_latent_period(this.host);
        if (my_latent_period < 0)
        {
          this.infectious_start_date = Natural_History.NEVER;
          this.infectious_end_date = Natural_History.NEVER;
        }
        else
        {
          Utils.assert(my_latent_period > 0); // FRED needs at least one day to become infectious
          this.infectious_start_date = this.exposure_date + my_latent_period;

          int my_duration_of_infectiousness = this.disease.get_natural_history().get_duration_of_infectiousness(this.host);
          // my_duration_of_infectiousness <= 0 means "infectious forever"
          if (my_duration_of_infectiousness > 0)
          {
            this.infectious_end_date = this.infectious_start_date + my_duration_of_infectiousness;
          }
          else
          {
            this.infectious_end_date = Natural_History.NEVER;
          }
        }
      }

      // code for testing ramps
      if (Global.Test > 1)
      {
        for (int i = 0; i <= 1000; ++i)
        {
          double dur = (this.infectious_end_date - this.infectious_start_date + 1);
          double t = 0.001 * i * dur;
          double start_full = this.disease.get_natural_history().get_full_infectivity_start();
          double end_full = this.disease.get_natural_history().get_full_infectivity_end();
          double x = t / dur;
          double result;
          if (x < start_full)
          {
            result = Math.Exp(x / start_full - 1.0);
          }
          else if (x < end_full)
          {
            result = 1.0;
          }
          else
          {
            result = Math.Exp(-3.5 * (x - end_full) / (1.0 - end_full));
          }
          Utils.FRED_VERBOSE(0, "RAMP: %f %f %d %d \n", t, result, this.infectious_start_date, this.infectious_end_date);
        }
        Global.Test = 0;
        Utils.fred_abort("");
      }

      if (Global.Verbose > 1)
      {
        Utils.FRED_VERBOSE(0, "INFECTION day %d incub %0.2f symp_onset %d symp_dur %0.2f symp_dur %2d symp_start_date %d inf_start_date %d inf_end_date %d inf_onset %d inf_dur %d ",
               this.exposure_date, incubation_period, this.symptoms_start_date - this.exposure_date,
               symptoms_duration, this.symptoms_end_date - this.symptoms_start_date,
               this.symptoms_start_date, this.infectious_start_date, this.infectious_end_date,
               this.infectious_start_date - this.exposure_date,
               this.infectious_end_date - this.infectious_start_date);
        for (int d = this.infectious_start_date; d <= this.infectious_end_date; ++d)
        {
          Utils.FRED_VERBOSE(0, "{0} ", get_infectivity(d));
        }
      }

      // adjust symptoms date if asymptomatic
      if (this.will_develop_symptoms == false)
      {
        this.symptoms_start_date = Natural_History.NEVER;
        this.symptoms_end_date = Natural_History.NEVER;
      }
      // print();
      return;
    }

    public virtual void update(int today)
    {
      // if host is symptomatic, determine if infection is fatal today.
      // if so, set flag and terminate infection update.
      if (this.disease.is_case_fatality_enabled() && is_symptomatic(today))
      {
        int days_symptomatic = today - this.symptoms_start_date;
        if (Global.Enable_Chronic_Condition)
        {
          if (this.disease.is_fatal(this.host, get_symptoms(today), days_symptomatic))
          {
            set_fatal_infection();
          }
        }
        else
        {
          if (this.disease.is_fatal(this.host.get_real_age(), get_symptoms(today), days_symptomatic))
          {
            set_fatal_infection();
          }
        }
      }
    }

    public virtual double get_infectivity(int day)
    {
      if (day < this.infectious_start_date || this.infectious_end_date <= day)
      {
        Utils.FRED_VERBOSE(0, "INFECTION: day %d OUT OF BOUNDS id %d inf_start %d inf_end %d result 0.0\n",
         day, this.host.get_id(), this.infectious_start_date, this.infectious_end_date);
        return 0.0;
      }

      // day is during infectious period
      double start_full = this.disease.get_natural_history().get_full_infectivity_start();
      double end_full = this.disease.get_natural_history().get_full_infectivity_end();

      // assumes total duration of infectiousness starts one day before infectious_start_date:
      int total_duration = this.infectious_end_date - this.infectious_start_date + 1;
      int days_infectious = day - this.infectious_start_date + 1;
      double fraction = days_infectious / total_duration;
      double result;
      if (fraction < start_full)
      {
        result = Math.Exp(fraction / start_full - 1.0);
      }
      else if (fraction <= end_full)
      {
        result = 1.0;
      }
      else
      {
        result = Math.Exp(-3.5 * (fraction - end_full) / (1.0 - end_full));
      }
      Utils.FRED_VERBOSE(1, "INFECTION: day %d days_infectious %d / %d  fract %f start_full %f end_full %f result %f\n",
             day, days_infectious, total_duration, fraction, start_full, end_full, result);

      if (this.will_develop_symptoms == false)
      {
        result *= this.disease.get_natural_history().get_asymptomatic_infectivity();
      }
      return result;
    }

    public virtual double get_symptoms(int day)
    {
      if (day < this.symptoms_start_date || this.symptoms_end_date <= day)
      {
        return 0.0;
      }

      // day is during symptoms period
      double start_full = this.disease.get_natural_history().get_full_symptoms_start();
      double end_full = this.disease.get_natural_history().get_full_symptoms_end();

      // assumes total duration of symptoms starts one day before symptoms_start_date:
      int total_duration = this.symptoms_end_date - this.symptoms_start_date + 1;

      int days_symptomatic = day - this.symptoms_start_date + 1;
      double fraction = days_symptomatic / total_duration;
      double result;
      if (fraction < start_full)
      {
        result = Math.Exp(fraction / start_full - 1.0);
      }
      else if (fraction <= end_full)
      {
        result = 1.0;
      }
      else
      {
        result = Math.Exp(-3.5 * (fraction - end_full) / (1.0 - end_full));
      }
      return result;
    }

    public virtual void print()
    {
      Utils.FRED_VERBOSE(0, "INF: Infection of disease type: %d in person %d " +
             "dates: exposed: %d, infectious_start: %d, infectious_end: %d " +
             "symptoms_start: %d, symptoms_end: %d\n",
             this.disease.get_id(), this.host.get_id(),
             this.exposure_date, this.infectious_start_date, this.infectious_end_date,
             this.symptoms_start_date, this.symptoms_end_date);
    }

    public virtual void report_infection(int day)
    {
      if (Global.Infectionfp == null)
      {
        return;
      }

      int mixing_group_id = (this.mixing_group == null ? -1 : this.mixing_group.get_id());
      char mixing_group_type = (this.mixing_group == null ? 'X' : this.mixing_group.get_type());
      char mixing_group_subtype = 'X';
      var place = mixing_group as Place;
      if (place != null)
      {
        if (place.is_group_quarters())
        {
          if (place.is_college())
          {
            mixing_group_subtype = 'D';
          }
          if (place.is_prison())
          {
            mixing_group_subtype = 'J';
          }
          if (place.is_nursing_home())
          {
            mixing_group_subtype = 'L';
          }
          if (place.is_military_base())
          {
            mixing_group_subtype = 'B';
          }
        }
      }
      int mixing_group_size = (this.mixing_group == null ? -1 : this.mixing_group.get_container_size());
      var builder = new StringBuilder();
      builder.AppendLine($"day {day} dis {this.disease.get_disease_name()} host {this.host.get_id()}");
      builder.AppendLine($" age {this.host.get_real_age()}");
      builder.AppendLine($" | DATES exp {this.exposure_date}");
      builder.AppendLine($" inf {get_infectious_start_date()} {get_infectious_end_date()}");
      builder.AppendLine($" symp {get_symptoms_start_date()} {get_symptoms_end_date()}");
      builder.AppendLine($" rec {get_infectious_end_date()} sus {get_immunity_end_date()}");
      builder.AppendLine($" infector_exp_date {(this.infector == null ? -1 : this.infector.get_exposure_date(this.disease.get_id()))}");
      builder.AppendLine(" | ");

      if (Global.Track_infection_events > 1)
      {
        builder.AppendLine($" sick_leave {this.host.is_sick_leave_available()}");
        builder.AppendLine($" infector {(this.infector == null ? -1 : this.infector.get_id())}");
        builder.AppendLine($" inf_age {(this.infector == null ? -1 : this.infector.get_real_age())}");
        builder.AppendLine($" inf_sympt {(this.infector == null ? -1 : this.infector.is_symptomatic())}");
        builder.AppendLine($" inf_sick_leave {(this.infector == null ? false : this.infector.is_sick_leave_available())}");
        builder.AppendLine($" at {mixing_group_type} mixing_group {mixing_group_id} subtype {mixing_group_subtype}");
        builder.AppendLine($" size {mixing_group_size} is_teacher {this.host.is_teacher()}");

        if (mixing_group_type != 'X')
        {
          var lat = place.get_latitude();
          var lon = place.get_longitude();
          builder.AppendLine($" lat {lat.Value}");
          builder.AppendLine($" lon {lon.Value}");
        }
        else
        {
          builder.AppendLine($" lat {-999}");
          builder.AppendLine($" lon {-999}");
        }
        double host_lat = this.host.get_household().get_latitude().Value;
        double host_lon = this.host.get_household().get_longitude().Value;
        builder.AppendLine($" home_lat {host_lat}");
        builder.AppendLine($" home_lon {host_lon}");
        builder.AppendLine(" | }");
      }

      if (Global.Track_infection_events > 2)
      {
        if (mixing_group_type != 'X' && this.infector != null)
        {
          double host_x = this.host.get_x();
          double host_y = this.host.get_y();
          double infector_x = this.infector.get_x();
          double infector_y = this.infector.get_y();
          double distance = Math.Sqrt((host_x - infector_x) * (host_x - infector_x) + (host_y - infector_y) * (host_y - infector_y));
          builder.AppendLine($" dist {distance}");
        }
        else
        {
          builder.AppendLine(" dist -1 ");
        }
        //Add Census Tract information. If there was no infector, censustract is -1
        if (this.infector == null)
        {
          builder.AppendLine(" infctr_census_tract -1");
          builder.AppendLine(" host_census_tract -1");
        }
        else
        {
          var hh = (Household)(this.infector.get_household());
          if (hh == null)
          {
            if (Global.Enable_Hospitals && this.infector.is_hospitalized() && this.infector.get_permanent_household() != null)
            {
              hh = (Household)(this.infector.get_permanent_household());
            }
          }
          int census_tract_index = (hh == null ? -1 : hh.get_census_tract_index());
          long census_tract = (census_tract_index == -1 ? -1 : Global.Places.get_census_tract_with_index(census_tract_index));
          builder.AppendLine($" infctr_census_tract {census_tract}");

          hh = (Household)(this.host.get_household());
          if (hh == null)
          {
            if (Global.Enable_Hospitals && this.host.is_hospitalized() && this.host.get_permanent_household() != null)
            {
              hh = (Household)(this.host.get_permanent_household());
            }
          }
          census_tract_index = (hh == null ? -1 : hh.get_census_tract_index());
          census_tract = (census_tract_index == -1 ? -1 : Global.Places.get_census_tract_with_index(census_tract_index));
          builder.AppendLine($" host_census_tract {census_tract}");
        }
        builder.AppendLine(" | ");
      }
      if (Global.Track_infection_events > 3)
      {
        var pt = this.host.get_household().get_patch();
        if (pt != null)
        {
          var patch_lat = Geo.get_latitude(pt.get_center_y());
          var patch_lon = Geo.get_longitude(pt.get_center_x());
          int patch_pop = pt.get_popsize();
          builder.AppendLine($" patch_lat {patch_lat}");
          builder.AppendLine($" patch_lon {patch_lon}");
          builder.AppendLine($" patch_pop {patch_pop}");
          builder.AppendLine(" | ");
        }
      }
      Global.Infectionfp.Write(builder.ToString());
      Global.Infectionfp.Flush();
    }

    // methods for antivirals
    public virtual bool provides_immunity()
    {
      return true;
    }

    public virtual void modify_infectivity(double multp) { }

    public virtual void advance_seed_infection(int days_to_advance) { }
    public virtual void modify_infectious_period(double multp, int cur_day) { }
    public virtual void modify_symptomatic_period(double multp, int cur_day) { }
    public virtual double get_susceptibility()
    {
      return 1.0;
    }

    public virtual void modify_asymptomatic_period(double multp, int cur_day) { }
    public virtual void modify_develops_symptoms(bool symptoms, int cur_day) { }

    public Disease get_disease()
    {
      return this.disease;
    }

    public Person get_host()
    {
      return this.host;
    }

    public Person get_infector()
    {
      return this.infector;
    }

    public Mixing_Group get_mixing_group()
    {
      return this.mixing_group;
    }

    public int get_exposure_date()
    {
      return this.exposure_date;
    }

    public int get_infectious_start_date()
    {
      return this.infectious_start_date;
    }

    public int get_infectious_end_date()
    {
      return this.infectious_end_date;
    }

    public int get_symptoms_start_date()
    {
      return this.symptoms_start_date;
    }

    public int get_symptoms_end_date()
    {
      return this.symptoms_end_date;
    }

    public int get_immunity_end_date()
    {
      return this.immunity_end_date;
    }

    public bool is_infectious(int day)
    {
      if (this.infectious_start_date != Natural_History.NEVER)
      {
        return (this.infectious_start_date <= day && day < this.infectious_end_date);
      }
      else
      {
        return false;
      }
    }

    public bool is_symptomatic(int day)
    {
      if (this.symptoms_start_date != Natural_History.NEVER)
      {
        return (this.symptoms_start_date <= day && day < this.symptoms_end_date);
      }
      else
      {
        return false;
      }
    }

    public void set_fatal_infection()
    {
      this.infection_is_fatal_today = true;
    }

    public virtual bool is_fatal(int day)
    {
      return this.infection_is_fatal_today;
    }

    public virtual int get_state()
    {
      return 0;
    }

    public virtual void set_state(int state) { }

    public void terminate(int day)
    {
      this.disease.terminate_person(host, day);
    }
  }
}
