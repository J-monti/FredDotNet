using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Fred
{
  public class Population
  {

    private int pop_size;
    private int enable_copy_files;
    private bool load_completed;
    private readonly Mutex mutex = new Mutex();
    private readonly Mutex add_person_mutex = new Mutex();
    private readonly Mutex batch_add_person_mutex = new Mutex();
    private readonly List<Person> death_list;// list of agents to die today
    private readonly List<Person> blq;// list of agents to die today
    private readonly List<string[]> demes;
    //Mitigation Managers
    internal AV_Manager av_manager;
    internal Vaccine_Manager vacc_manager;

    private static string profilefile;
    private static string pop_outfile;
    private static string output_population_date_match;
    private static int output_population;
    private static bool is_initialized;
    private static int next_id;

    /**
   * Default constructor
   */
    public Population()
    {
      this.load_completed = false;

      // clear_static_arrays();
      this.pop_size = 0;
      this.enable_copy_files = 0;

      // reserve memory for lists
      this.demes = new List<string[]>();
      this.death_list = new List<Person>();
      this.blq = new List<Person>();
    }

    /**
     * Sets the static variables for the class from the parameter file.
     */
    public void get_parameters()
    {

      // Only do this one time
      if (!is_initialized)
      {
        FredParameters.GetParameter("enable_copy_files", ref this.enable_copy_files);
        FredParameters.GetParameter("output_population", ref output_population);
        if (output_population > 0)
        {
          FredParameters.GetParameter("pop_outfile", ref pop_outfile);
          FredParameters.GetParameter("output_population_date_match", ref output_population_date_match);
        }

        is_initialized = true;
      }
    }

    /**
     * Prepare this Population (calls read_population)
     * @see Population.read_population()
     */
    public void setup()
    {
      Utils.FRED_STATUS(0, "setup population entered\n", "");

      if (Global.Enable_Vaccination)
      {
        this.vacc_manager = new Vaccine_Manager(this);
      }
      else
      {
        this.vacc_manager = new Vaccine_Manager();
      }

      if (Global.Enable_Antivirals)
      {
        Utils.fred_abort("Sorry, antivirals are not enabled in this version of FRED.");
        this.av_manager = new AV_Manager(this);
      }
      else
      {
        this.av_manager = new AV_Manager();
      }

      if (Global.Verbose > 1)
      {
        this.av_manager.print();
      }

      this.pop_size = 0;
      this.death_list.Clear();
      read_all_populations();

      if (Global.Enable_Behaviors)
      {
        // select adult to make health decisions
        initialize_population_behavior();
      }

      if (Global.Enable_Health_Insurance)
      {
        // select insurance coverage
        // try to make certain that everyone in a household has same coverage
        initialize_health_insurance();
      }

      this.load_completed = true;

      if (Global.Enable_Population_Dynamics)
      {
        initialize_demographic_dynamics();
      }

      initialize_activities();

      if (Global.Verbose > 0)
      {
        for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
        {
          int count = 0;
          for (int p = 0; p < this.get_index_size(); ++p)
          {
            var person = get_person_by_index(p);
            if (person != null)
            {
              if (person.is_immune(d))
              {
                count++;
              }
            }
          }
          Utils.FRED_STATUS(0, "number of residually immune people for disease %d = %d\n", d, count);
        }
      }
      this.av_manager.reset();
      this.vacc_manager.reset();

      // record age-specific pop size
      for (int age = 0; age <= Demographics.MAX_AGE; ++age)
      {
        Global.Popsize_by_age[age] = 0;
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          int age = person.get_age();
          if (age > Demographics.MAX_AGE)
          {
            age = Demographics.MAX_AGE;
          }
          Global.Popsize_by_age[age]++;
        }
      }

      Utils.FRED_STATUS(0, "population setup finished\n", "");
    }

    /**
     * Used during debugging to verify that code is functioning properly.
     */
    public void quality_control()
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "population quality control check\n");
      }

      // check population
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        if (person.get_household() == null)
        {
          Utils.FRED_STATUS(0, "Help! Person %d has no home.\n", person.get_id());
          person.print(Global.Statusfp, 0);
        }
      }

      if (Global.Verbose > 0)
      {
        int n0, n5, n18, n65;
        var count = new int[20];
        int total = 0;
        n0 = n5 = n18 = n65 = 0;
        // age distribution
        for (int p = 0; p < this.get_index_size(); ++p)
        {
          var person = get_person_by_index(p);
          if (person == null)
          {
            continue;
          }
          int a = person.get_age();

          if (a < 5)
          {
            n0++;
          }
          else if (a < 18)
          {
            n5++;
          }
          else if (a < 65)
          {
            n18++;
          }
          else
          {
            n65++;
          }
          int n = a / 5;
          if (n < 20)
          {
            count[n]++;
          }
          else
          {
            count[19]++;
          }
          total++;
        }
        Utils.FRED_STATUS(0, "\nAge distribution: %d people\n", total);
        for (int c = 0; c < 20; ++c)
        {
          Utils.FRED_STATUS(0, "age %2d to %d: %6d (%.2f%%)\n", 5 * c, 5 * (c + 1) - 1, count[c],
            (100.0 * count[c]) / total);
        }
        Utils.FRED_STATUS(0, "AGE 0-4: %d %.2f%%\n", n0, (100.0 * n0) / total);
        Utils.FRED_STATUS(0, "AGE 5-17: %d %.2f%%\n", n5, (100.0 * n5) / total);
        Utils.FRED_STATUS(0, "AGE 18-64: %d %.2f%%\n", n18, (100.0 * n18) / total);
        Utils.FRED_STATUS(0, "AGE 65-100: %d %.2f%%\n", n65, (100.0 * n65) / total);
        Utils.FRED_STATUS(0, "\n");

        // Print out At Risk distribution
        if (Global.Enable_Vaccination)
        {
          for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
          {
            if (Global.Diseases.get_disease(d).get_at_risk().is_empty() == false)
            {
              var dis = Global.Diseases.get_disease(d);
              var rcount = new int[20];
              for (int p = 0; p < this.get_index_size(); ++p)
              {
                var person = get_person_by_index(p);
                if (person == null)
                {
                  continue;
                }
                int a = person.get_age();
                int n = a / 10;
                if (person.get_health().is_at_risk(d) == true)
                {
                  if (n < 20)
                  {
                    rcount[n]++;
                  }
                  else
                  {
                    rcount[19]++;
                  }
                }
              }
              Utils.FRED_STATUS(0, "\n Age Distribution of At Risk for Disease %d: %d people\n", d,
                total);
              for (int c = 0; c < 10; ++c)
              {
                Utils.FRED_STATUS(0, "age %2d to %2d: %6d (%.2f%%)\n", 10 * c, 10 * (c + 1) - 1,
                  rcount[c], (100.0 * rcount[c]) / total);
              }
              Utils.FRED_STATUS(0, "\n");
            }
          }
        }
      }
      Utils.FRED_STATUS(0, "population quality control finished\n");
    }

    /**
     * Perform end of run operations (clean up)
     */
    public void end_of_run()
    {
      // Write the population to the output file if the parameter is set
      // Will write only on the first day of the simulation, days matching
      // the date pattern in the parameter file, and the last day of the
      // simulation
      if (output_population > 0)
      {
        this.write_population_output_file(Global.Days);
      }
    }

    /**
     * Report the disease statistics for a given day
     * @param day the simulation day
     */
    public void report(int day)
    {
      // give out anti-viral (after today's infections)
      this.av_manager.disseminate(day);
      for (int d = 0; d < Global.Diseases.get_number_of_diseases(); ++d)
      {
        Global.Diseases.get_disease(d).print_stats(day);
      }

      // Write out the population if the output_population parameter is set.
      // Will write only on the first day of the simulation, on days
      // matching the date pattern in the parameter file, and the on
      // the last day of the simulation
      if (Population.output_population > 0)
      {
        var tokens = output_population_date_match.Split('-');
        int month = Convert.ToInt32(tokens[0]);
        int day_of_month = Convert.ToInt32(tokens[1]);
        if ((day == 0) || (month == Date.get_month() && day_of_month == Date.get_day_of_month()))
        {
          this.write_population_output_file(day);
        }
      }
    }

    /**
     * @return the pop_size
     */
    public int get_pop_size()
    {
      return this.pop_size;
    }

    //Mitigation Managers
    /**
     * @return a pointer to this Population's AV_Manager
     */
    public AV_Manager get_av_manager()
    {
      return this.av_manager;
    }

    /**
     * @return a pointer to this Population's Vaccine_Manager
     */
    public Vaccine_Manager get_vaccine_manager()
    {
      return this.vacc_manager;
    }

    /**
     * @param args passes to Person constructor; all persons added to the
     * Population must be created through this method
     *
     * @return pointer to the person created and added
     */
    public Person add_person(int age, char sex, int race, int rel, Place house, Place school, Place work, int day, bool today_is_birthday)
    {
      this.add_person_mutex.WaitOne();
      int id = Population.next_id++;
      var idx = this.blq.IndexOf(null);
      var person = new Person();
      if (idx == -1)
      {
        idx = this.blq.Count;
        this.blq.Add(person);
      }
      else
      {
        this.blq[idx] = person;
      }

      person.setup(idx, id, age, sex, race, rel, house, school, work, day, today_is_birthday);
      Utils.assert(this.pop_size == blq.Count - 1);
      this.pop_size = this.blq.Count;
      this.add_person_mutex.ReleaseMutex();
      return person;
    }

    /**
     * Perform the necessary steps for an agent's death
     * @param day the simulation day
     * @param person the agent who will die
     */
    public void prepare_to_die(int day, Person person)
    {
      // add person to daily death_list
      this.mutex.WaitOne();
      this.death_list.Add(person);
      this.mutex.ReleaseMutex();
    }

    public void remove_dead_from_population(int day)
    {
      foreach (var person in this.death_list)
      {
        remove_dead_person_from_population(day, person);
      }

      // clear the death list
      this.death_list.Clear();
    }

    public void remove_dead_person_from_population(int day, Person person)
    {
      // remove from vaccine queues
      if (this.vacc_manager.do_vaccination())
      {
        Utils.FRED_STATUS(1, "Removing %d from Vaccine Queue\n", person.get_id());
        this.vacc_manager.remove_from_queue(person);
      }

      Utils.FRED_VERBOSE(1, "DELETING PERSON: %d ...\n", person.get_id());
      person.terminate(day);
      Utils.FRED_VERBOSE(1, "DELETED PERSON: %d\n", person.get_id());

      if (Global.Enable_Travel)
      {
        Travel.terminate_person(person);
      }

      int idx = person.get_pop_index();
      Utils.assert(get_person_by_index(idx) == person);
      this.blq[idx] = null;
      //this.blq.mark_invalid_by_index(person.get_pop_index());

      this.pop_size--;
      //Utils.assert(this.pop_size == this.blq.Count);
    }

    /**
     * @param index the index of the Person
     * Return a pointer to the Person object at this index
     */
    public Person get_person_by_index(int index)
    {
      if (index >= 0 && index < this.blq.Count)
      {
        return this.blq[index];
      }

      return null;
    }

    public int get_index_size()
    {
      return this.blq.Count;
    }

    /**
     * Assign agents in Schools to specific Classrooms within the school
     */
    public void assign_classrooms()
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "assign classrooms entered\n");
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        if (person.get_school() != null)
        {
          person.assign_classroom();
        }
      }
      Utils.FRED_VERBOSE(0, "assign classrooms finished\n");
    }

    /**
     * Assign agents in Workplaces to specific Offices within the workplace
     */
    public void assign_offices()
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "assign offices entered\n");
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        if (person.get_workplace() != null)
        {
          person.assign_office();
        }
      }
      Utils.FRED_VERBOSE(0, "assign offices finished\n");
    }

    /**
     * Assign all agents a primary healthcare facility
     */
    public void assign_primary_healthcare_facilities()
    {
      Utils.assert(this.is_load_completed());
      Utils.assert(Global.Places.is_load_completed());
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "assign primary healthcare entered\n");
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        person.assign_primary_healthcare_facility();

      }
      Utils.FRED_VERBOSE(0, "assign primary healthcare finished\n");
    }

    /**
     * Write degree information to a file degree.txt
     * @param directory the directory where the file will be written
     */
    public void get_network_stats(string directory)
    {
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "get_network_stats entered\n");
      }
      string filename = $"{directory}/degree.csv";
      using var fp = new StreamWriter(filename);
      fp.WriteLine("id,age,deg,h,n,s,c,w,o");
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        fp.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}", person.get_id(), person.get_age(), person.get_degree(),
          person.get_household_size(), person.get_neighborhood_size(), person.get_school_size(),
          person.get_classroom_size(), person.get_workplace_size(), person.get_office_size());
      }
      fp.Flush();
      fp.Dispose();
      if (Global.Verbose > 0)
      {
        Utils.FRED_STATUS(0, "get_network_stats finished\n");
      }
    }

    public void split_synthetic_populations_by_deme()
    {
      const char delim = ' ';
      Utils.FRED_STATUS(0, "Validating synthetic population identifiers before reading.\n", "");
      string pop_dir = Global.Synthetic_population_directory;
      Utils.FRED_STATUS(0, "Using population directory: {0}\n", pop_dir);
      Utils.FRED_STATUS(0, "FRED defines a \"deme\" to be a local population {0}\n{1}{2}\n",
            "of people whose households are contained in the same bounded geographic region.",
            "No Synthetic Population ID may have more than one Deme ID, but a Deme ID may ",
            "contain many Synthetic Population IDs.");

      int param_num_demes = 1;
      FredParameters.GetParameter("num_demes", ref param_num_demes);
      Utils.assert(param_num_demes > 0);
      this.demes.Clear();

      List<string> pop_id_set = new List<string>();

      for (int d = 0; d < param_num_demes; ++d)
      {
        // allow up to 195 (county) population ids per deme
        // TODO set this limit in param file / Global.h.
        string pop_id_string = string.Empty;
        if (FredParameters.does_param_exist($"synthetic_population_id[{d}]"))
        {
          FredParameters.get_indexed_param("synthetic_population_id", d, ref pop_id_string);
        }
        else
        {
          if (d == 0)
          {
            pop_id_string = Global.Synthetic_population_id;
          }
          else
          {
            Utils.fred_abort("Help! %d %s %d %s %d %s\n", param_num_demes,
            "demes were requested (param_num_demes = ", param_num_demes,
            "), but indexed parameter synthetic_population_id[", d, "] was not found!");
          }
        }
        this.demes.Add(pop_id_string.Split(delim));
        Utils.FRED_STATUS(0, "Split ID string \"%s\" into %d %s using delimiter: \'%c\'\n", pop_id_string,
          this.demes[d].Length, this.demes[d].Length > 1 ? "tokens" : "token", delim);
        Utils.assert(this.demes.Count > 0);
        // only allow up to 255 demes
        Utils.assert(this.demes.Count <= char.MaxValue);
        Utils.FRED_STATUS(0, "Deme %d comprises %d synthetic population id%s:\n", d, this.demes[d].Length,
        this.demes[d].Length > 1 ? "s" : "");
        Utils.assert(this.demes[d].Length > 0);
        for (int i = 0; i < this.demes[d].Length; ++i)
        {
          Utils.FRED_CONDITIONAL_VERBOSE(0, pop_id_set.Contains(this.demes[d][i]),
                 "%s %s %s %s\n", "Population ID ", this.demes[d][i], "was specified more than once!",
                 "Population IDs must be unique across all Demes!");
          Utils.assert(!pop_id_set.Contains(this.demes[d][i]));
          pop_id_set.Add(this.demes[d][i]);
          Utils.FRED_STATUS(0, "-. Deme %d, Synth. Pop. ID %d: %s\n", d, i, this.demes[d][i]);
        }
      }
    }

    public void read_all_populations()
    {
      string pop_dir = Global.Synthetic_population_directory;
      Utils.assert(this.demes.Count > 0);
      for (int d = 0; d < this.demes.Count; ++d)
      {
        Utils.FRED_STATUS(0, "Loading population for Deme %d:\n", d);
        Utils.assert(this.demes[d].Length > 0);
        for (int i = 0; i < this.demes[d].Length; ++i)
        {
          Utils.FRED_STATUS(0, "Loading population %d for Deme %d: %s\n", i, d, this.demes[d][i]);
          // o---------------------------------------- Call read_population to actually
          // |                                         read the population files
          // V
          read_population(pop_dir, this.demes[d][i], "people");
          if (Global.Enable_Group_Quarters)
          {
            Utils.FRED_STATUS(0, "Loading group quarters population %d for Deme %d: %s\n", i, d,
            this.demes[d][i]);
            // o---------------------------------------- Call read_population to actually
            // |                                         read the population files
            // V
            read_population(pop_dir, this.demes[d][i], "gq_people");
          }
        }
      }
      // report on time take to read populations
      //Utils.fred_print_lap_time("reading populations");
    }

    public void read_population(string pop_dir, string pop_id, string pop_type)
    {
      Utils.FRED_STATUS(0, "read population entered\n");
      string pop_file;
      string population_file;
      string temp_file;
      var scratchRamDisk = Environment.GetEnvironmentVariable("SCRATCH_RAMDISK");
      if (!string.IsNullOrWhiteSpace(scratchRamDisk))
      {
        temp_file = $"{scratchRamDisk}/temp_file-{Process.GetCurrentProcess().Id}-{Global.Simulation_run_number}";
      }
      else
      {
        temp_file = $"./temp_file-{Process.GetCurrentProcess().Id}-{Global.Simulation_run_number}";
      }

      bool is_group_quarters_pop = pop_type == "gq_people";
      //#if SNAPPY

      //  // try to open compressed population file
      //  sprintf(population_file, "%s/%s/%s_synth_%s.txt.fsz", pop_dir, pop_id, pop_id, pop_type);

      //  fp = Utils.fred_open_file(population_file);
      //  // fclose(fp); fp = null; // TEST
      //  if(fp != null) {
      //    fclose(fp);
      //    if(this.enable_copy_files) {
      //      sprintf(cmd, "cp %s %s", population_file, temp_file);
      //      printf("COPY_FILE: %s\n", cmd);
      //      fflush(stdout);
      //      system(cmd);
      //      pop_file = temp_file;
      //    } else {
      //      pop_file = population_file;
      //    }

      //    FRED_VERBOSE(0, "calling SnappyFileCompression on pop_file = %s\n", pop_file);
      //    SnappyFileCompression compressor = SnappyFileCompression(pop_file);
      //    compressor.init_compressed_block_reader();
      //    // if we have the magic, then it must be fsz block compressed
      //    if(compressor.check_magic_bytes()) {
      //      // limit to two threads to prevent locking and I/O overhead; also
      //      // helps to preserve population order in bloque assignment
      //#pragma omp parallel
      //      {
      //        std.stringstream stream;
      //        while(compressor.load_next_block_stream(stream)) {
      //          parse_lines_from_stream(stream, is_group_quarters_pop);
      //        }
      //      }
      //    }
      //    if(this.enable_copy_files) {
      //      unlink(temp_file);
      //    }
      //    FRED_VERBOSE(0, "finished reading compressed population, pop_size = %d\n", pop_size);
      //    return;
      //  }
      //#endif

      // try to read the uncompressed file
      population_file = $"{pop_dir}/{pop_id}/{pop_id}_synth_{pop_type}.txt";
      if (!File.Exists(population_file))
      {
        Utils.fred_abort("population_file %s not found\n", population_file);
      }

      if (this.enable_copy_files != 0)
      {
        if (File.Exists(temp_file))
        {
          File.Delete(temp_file);
        }

        File.Copy(population_file, temp_file);
        // printf("copy finished\n"); fflush(stdout);
        pop_file = temp_file;
      }
      else
      {
        pop_file = population_file;
      }
      using var fp = new StreamReader(pop_file);
      parse_lines_from_stream(fp, is_group_quarters_pop);
      fp.Dispose();
      if (this.enable_copy_files != 0)
      {
        File.Delete(temp_file);
      }
      Utils.FRED_VERBOSE(0, "finished reading uncompressed population, pop_size = %d\n", pop_size);
    }

    /**
     *
     */
    public void print_age_distribution(string dir, string date_string, int run)
    {
      var count = new int[Demographics.MAX_AGE + 1];
      var pct = new double[Demographics.MAX_AGE + 1];
      var filename = $"{dir}/age_dist_{date_string}.{run:D2}";
      Console.WriteLine("print_age_dist entered, filename = {0}", filename);
      for (int i = 0; i < 21; ++i)
      {
        count[i] = 0;
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        int age = person.get_age();
        if (0 <= age && age <= Demographics.MAX_AGE)
        {
          count[age]++;
        }

        if (age > Demographics.MAX_AGE)
        {
          count[Demographics.MAX_AGE]++;
        }
        Utils.assert(age >= 0);
      }

      using var fp = new StreamWriter(filename);
      for (int i = 0; i < 21; ++i)
      {
        pct[i] = 100.0 * count[i] / this.pop_size;
        fp.WriteLine("{0}  {1} {2}", i * 5, count[i], pct[i]);
      }
      fp.Flush();
      fp.Dispose();
    }

    /**
     * @return a pointer to a random Person in this population
     */
    public Person select_random_person()
    {
      int i = FredRandom.Next(0, get_index_size() - 1);
      while (get_person_by_index(i) == null)
      {
        i = FredRandom.Next(0, get_index_size() - 1);
      }
      return get_person_by_index(i);
    }

    /**
     * @return a pointer to a random Person in this population
     * whose age is in the given range
     */
    public Person select_random_person_by_age(int min_age, int max_age)
    {
      int age;
      if (max_age < min_age)
      {
        return null;
      }
      if (Demographics.MAX_AGE < min_age)
      {
        return null;
      }

      Person person;
      while (true)
      {
        person = select_random_person();
        age = person.get_age();
        if (min_age <= age && age <= max_age)
        {
          return person;
        }
      }
    }

    public void set_school_income_levels()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      //typedef std.map<School*, int> SchoolMapT;
      //typedef std.multimap < int, School) SchoolMultiMapT;

      var school_enrollment_map = new Dictionary<School, int>();
      var school_hh_income_map = new Dictionary<School, int>();
      //SchoolMapT* school_enrollment_map = new SchoolMapT();
      //SchoolMapT* school_hh_income_map = new SchoolMapT();
      var school_income_hh_mm = new List<Tuple<double, School>>();

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null) continue;
        if (person.get_school() == null)
        {
          continue;
        }
        else
        {
          var schl = (School)(person.get_school());
          //Try to find the school label
          if (!school_enrollment_map.ContainsKey(schl))
          {
            //Add the school to the map
            school_enrollment_map.Add(schl, 1);
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            school_hh_income_map.Add(schl, student_hh.get_household_income());
          }
          else
          {
            //Update the values
            school_enrollment_map[schl] += 1;
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            school_hh_income_map[schl] += student_hh.get_household_income();
          }
        }
      }

      int total = school_hh_income_map.Count;
      int q1 = total / 4;
      int q2 = q1 * 2;
      int q3 = q1 * 3;

      Utils.FRED_STATUS(0, "\nMEAN HOUSEHOLD INCOME STATS PER SCHOOL SUMMARY:\n");
      foreach (var kvp in school_enrollment_map)
      {
        double enrollment_total = kvp.Value;
        var schl = kvp.Key;
        double hh_income_tot = school_hh_income_map[schl];
        double mean_hh_income = (hh_income_tot / enrollment_total);
        // TODO: Uh... this doesn't look like it does anything...
        school_income_hh_mm.Add(new Tuple<double, School>(mean_hh_income, schl));
      }

      int mean_income_size = school_income_hh_mm.Count;
      Utils.assert(mean_income_size == total);

      int counter = 0;
      //for (SchoolMultiMapT.iterator itr = school_income_hh_mm.begin();
      //    itr != school_income_hh_mm.end(); ++itr)
      foreach(var tuple in school_income_hh_mm)
      {
        var schl = tuple.Item2;
        Utils.assert(schl != null);
        Utils.assert(school_hh_income_map.ContainsKey(schl));
        Utils.assert(school_enrollment_map.ContainsKey(schl));
        if (counter < q1)
        {
          schl.set_income_quartile(Global.Q1);
        }
        else if (counter < q2)
        {
          schl.set_income_quartile(Global.Q2);
        }
        else if (counter < q3)
        {
          schl.set_income_quartile(Global.Q3);
        }
        else
        {
          schl.set_income_quartile(Global.Q4);
        }
        // double hh_income_tot = school_hh_income_map[schl];
        // double enrollment_tot = school_enrollment_map[schl];
        // double mean_hh_income = (hh_income_tot / enrollment_tot);
        Utils.FRED_STATUS(0, "MEAN_HH_INCOME: %s %.2f\n", schl.get_label(), tuple.Item1);
        counter++;
      }
    }

    public void report_mean_hh_income_per_school()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      var school_enrollment_map = new Dictionary<School, int>();
      var school_hh_income_map = new Dictionary<School, int>();
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        if (person.get_school() == null)
        {
          continue;
        }
        else
        {
          var schl = (School)(person.get_school());
          //Try to find the school
          if (!school_enrollment_map.ContainsKey(schl))
          {
            //Add the school to the map
            school_enrollment_map.Add(schl, 1);
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            school_hh_income_map.Add(schl, student_hh.get_household_income());
          }
          else
          {
            //Update the values
            school_enrollment_map[schl] += 1;
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            school_hh_income_map[schl] += student_hh.get_household_income();
          }
        }
      }

      Utils.FRED_STATUS(0, "\nMEAN HOUSEHOLD INCOME STATS PER SCHOOL SUMMARY:\n");
      foreach (var kvp in school_enrollment_map)
      {
        double enrollment_tot = kvp.Value;
        double hh_income_tot = school_hh_income_map[kvp.Key];
        Utils.FRED_STATUS(0, "MEAN_HH_INCOME: %s %.2f\n", kvp.Key.get_label(), (hh_income_tot / enrollment_tot));
      }
    }

    public void report_mean_hh_size_per_school()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      var school_enrollment_map = new Dictionary<School, int>();
      var school_hh_size_map = new Dictionary<School, int>();

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        if (person.get_school() == null)
        {
          continue;
        }
        else
        {
          var schl = (School)(person.get_school());
          //Try to find the school
          if (!school_enrollment_map.ContainsKey(schl))
          {
            //Add the school to the map
            school_enrollment_map.Add(schl, 1);
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household());
              }
            }
            Utils.assert(student_hh != null);
            school_hh_size_map.Add(schl, student_hh.get_size());
          }
          else
          {
            //Update the values
            school_enrollment_map[schl] += 1;
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            school_hh_size_map[schl] += student_hh.get_size();
          }
        }
      }

      Utils.FRED_STATUS(0, "\nMEAN HOUSEHOLD SIZE STATS PER SCHOOL SUMMARY:\n");
      foreach (var kvp in school_enrollment_map)
      {
        double enrollmen_tot = kvp.Value;
        double hh_size_tot = school_hh_size_map[kvp.Key];
        Utils.FRED_STATUS(0, "MEAN_HH_SIZE: %s %.2f\n", kvp.Key.get_label(), (hh_size_tot / enrollmen_tot));
      }
    }

    public void report_mean_hh_distance_from_school()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());
      var school_enrollment_map = new Dictionary<School, int>();
      var school_hh_distance_map = new Dictionary<School, double>();

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        if (person.get_school() == null)
        {
          continue;
        }
        else
        {
          var schl = (School)(person.get_school());
          //Try to find the school
          if (!school_enrollment_map.ContainsKey(schl))
          {
            //Add the school to the map
            school_enrollment_map.Add(schl, 1);
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            double distance = Geo.haversine_distance(person.get_school().get_longitude(),
                  person.get_school().get_latitude(), student_hh.get_longitude(),
                  student_hh.get_latitude());
            school_hh_distance_map.Add(schl, distance);
          }
          else
          {
            //Update the values
            school_enrollment_map[schl] += 1;
            var student_hh = (Household)(person.get_household());
            if (student_hh == null)
            {
              if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
              {
                student_hh = (Household)(person.get_permanent_household()); ;
              }
            }
            Utils.assert(student_hh != null);
            double distance = Geo.haversine_distance(person.get_school().get_longitude(),
                  person.get_school().get_latitude(), student_hh.get_longitude(),
                  student_hh.get_latitude());
            school_hh_distance_map[schl] += distance;
          }
        }
      }

      Utils.FRED_STATUS(0, "\nMEAN HOUSEHOLD DISTANCE STATS PER SCHOOL SUMMARY:\n");
      foreach (var kvp in school_enrollment_map)
      {
        double enrollmen_tot = kvp.Value;
        double hh_distance_tot = school_hh_distance_map[kvp.Key];
        Utils.FRED_STATUS(0, "MEAN_HH_DISTANCE: %s %.2f\n", kvp.Key.get_label(),
        (hh_distance_tot / enrollmen_tot));
      }
    }

    public void report_mean_hh_stats_per_income_category()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());

      //First sort households into sets based on their income level
      List<Household>[] household_sets = new List<Household>[(int)Household_income_level_code.UNCLASSIFIED + 1];// Household_income_level_code.UNCLASSIFIED + 1];
      for (int i = 0; i < household_sets.Length; i++)
      {
        household_sets[i] = new List<Household>();
      }

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        if (person.get_household() == null)
        {
          if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
          {
            int income_level = ((Household)person.get_permanent_household()).get_household_income_code();
            household_sets[income_level].Add((Household)person.get_permanent_household());
            Global.Income_Category_Tracker.add_index(income_level);
          }
          else
          {
            continue;
          }
        }
        else
        {
          int income_level = ((Household)person.get_household()).get_household_income_code();
          household_sets[income_level].Add((Household)(person.get_household()));
          Global.Income_Category_Tracker.add_index(income_level);
        }

        if (person.get_household() == null)
        {
          continue;
        }
        else
        {
          int income_level = ((Household)person.get_household()).get_household_income_code();
          household_sets[income_level].Add((Household)(person.get_household()));
        }
      }

      for (int i = (int)Household_income_level_code.CAT_I; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
      {
        int count_hh = household_sets[i].Count;
        double hh_income = 0.0;
        int count_people = 0;
        int count_children = 0;

        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_households", count_hh);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_people", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_school_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_workers", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_households", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_people", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_school_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_workers", (int)0);

        foreach(var hh in household_sets[i])
        {
          count_people += hh.get_size();
          count_children += hh.get_children();

          if (hh.is_group_quarters())
          {
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_households", (int)1);
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_people", hh.get_size());
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_children", hh.get_children());
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_people", hh.get_size());
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_children", hh.get_children());
          }
          else
          {
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_people", hh.get_size());
            Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_children", hh.get_children());
          }

          hh_income += hh.get_household_income();
          var inhab_vec = hh.get_inhabitants();
          foreach(var inhab in inhab_vec)
          {
            if (inhab.is_child())
            {
              if (inhab.get_school() != null)
              {
                if (hh.is_group_quarters())
                {
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_school_children", (int)1);
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_school_children", (int)1);
                }
                else
                {
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_school_children", (int)1);
                }
              }
            }
            if (inhab.get_workplace() != null)
            {
              if (hh.is_group_quarters())
              {
                Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_workers", (int)1);
                Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_workers", (int)1);
              }
              else
              {
                Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_workers", (int)1);
              }
            }
          }
        }

        if (count_hh > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "mean_household_income",
                    (hh_income / count_hh));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "mean_household_income", 0.0);
        }

        //Store size info for later usage
        Household.count_inhabitants_by_household_income_level_map[i] = count_people;
        Household.count_children_by_household_income_level_map[i] = count_children;
      }
    }

    public void report_mean_hh_stats_per_census_tract()
    {

      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());

      //First sort households into sets based on their census tract and income category
      var household_sets = new Dictionary<long, List<Household>>();

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }

        long census_tract;
        if (person.get_household() == null)
        {
          if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
          {
            int census_tract_index = ((Household)person.get_permanent_household()).get_census_tract_index();
            census_tract = Global.Places.get_census_tract_with_index(census_tract_index);
            if (!household_sets.ContainsKey(census_tract))
            {
              household_sets.Add(census_tract, new List<Household>());
            }
            Global.Tract_Tracker.add_index(census_tract);
            household_sets[census_tract].Add((Household)(person.get_permanent_household()));
            Household.census_tract_set.Add(census_tract);
          }
          else
          {
            continue;
          }
        }
        else
        {
          int census_tract_index = ((Household)person.get_household()).get_census_tract_index();
          census_tract = Global.Places.get_census_tract_with_index(census_tract_index);
          if (!household_sets.ContainsKey(census_tract))
          {
            household_sets.Add(census_tract, new List<Household>());
          }
          Global.Tract_Tracker.add_index(census_tract);
          household_sets[census_tract].Add((Household)(person.get_household()));
          Household.census_tract_set.Add(census_tract);
        }
      }

      foreach (var censustract in Household.census_tract_set)
      {
        int count_people_per_census_tract = 0;
        int count_children_per_census_tract = 0;
        int count_hh_per_census_tract = household_sets[censustract].Count;
        double hh_income_per_census_tract = 0.0;

        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_households", count_hh_per_census_tract);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_people", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_school_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_workers", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_gq_households", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_gq_people", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_gq_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_gq_school_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(censustract, "number_of_gq_workers", (int)0);

        foreach (var hh in household_sets[censustract])
        {

          count_people_per_census_tract += hh.get_size();
          count_children_per_census_tract += hh.get_children();

          if (hh.is_group_quarters())
          {
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_gq_households", (int)1);
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_gq_people", hh.get_size());
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_gq_children", hh.get_children());
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_people", hh.get_size());
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_children", hh.get_children());
          }
          else
          {
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_people", hh.get_size());
            Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_children", hh.get_children());
          }

          hh_income_per_census_tract += hh.get_household_income();
          var inhab_vec = hh.get_inhabitants();
          foreach(var inhab in inhab_vec)
          {
            if (inhab.is_child())
            {
              if (inhab.get_school() != null)
              {
                if (hh.is_group_quarters())
                {
                  Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_gq_school_children", (int)1);
                  Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_school_children", (int)1);
                }
                else
                {
                  Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_school_children", (int)1);
                }
              }
            }
            if (inhab.get_workplace() != null)
            {
              if (hh.is_group_quarters())
              {
                Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_gq_workers", (int)1);
                Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_workers", (int)1);
              }
              else
              {
                Global.Tract_Tracker.increment_index_key_pair(censustract, "number_of_workers", (int)1);
              }
            }
          }
        }

        if (count_hh_per_census_tract > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(censustract, "mean_household_income",
                (hh_income_per_census_tract / count_hh_per_census_tract));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(censustract, "mean_household_income", 0.0);
        }

        //Store size info for later usage
        Household.count_inhabitants_by_census_tract_map[censustract] = count_people_per_census_tract;
        Household.count_children_by_census_tract_map[censustract] = count_children_per_census_tract;
      }
    }

    public void report_mean_hh_stats_per_income_category_per_census_tract()
    {
      Utils.assert(Global.Places.is_load_completed());
      Utils.assert(Global.Pop.is_load_completed());

      //First sort households into sets based on their census tract and income category
      //map<int, map<int, std.set<Household*>>> household_sets;
      var household_sets = new Dictionary<long, Dictionary<int, List<Household>>>();

      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        long census_tract;
        if (person.get_household() == null)
        {
          if (Global.Enable_Hospitals && person.is_hospitalized() && person.get_permanent_household() != null)
          {
            int census_tract_index = ((Household)person.get_permanent_household()).get_census_tract_index();
            census_tract = Global.Places.get_census_tract_with_index(census_tract_index);
            if (!household_sets.ContainsKey(census_tract))
            {
              household_sets.Add(census_tract, new Dictionary<int, List<Household>>());
            }
            Global.Tract_Tracker.add_index(census_tract);
            int income_level = ((Household)person.get_permanent_household()).get_household_income_code();
            if (!household_sets[census_tract].ContainsKey(income_level))
            {
              household_sets[census_tract].Add(income_level, new List<Household>());
            }
            Global.Income_Category_Tracker.add_index(income_level);
            household_sets[census_tract][income_level].Add((Household)person.get_permanent_household());
            Household.census_tract_set.Add(census_tract);
          }
          else
          {
            continue;
          }
        }
        else
        {
          int census_tract_index = ((Household)person.get_household()).get_census_tract_index();
          census_tract = Global.Places.get_census_tract_with_index(census_tract_index);
          if (!household_sets.ContainsKey(census_tract))
          {
            household_sets.Add(census_tract, new Dictionary<int, List<Household>>());
          }
          Global.Tract_Tracker.add_index(census_tract);
          int income_level = ((Household)person.get_household()).get_household_income_code();
          if (!household_sets[census_tract].ContainsKey(income_level))
          {
            household_sets[census_tract].Add(income_level, new List<Household>());
          }
          Global.Income_Category_Tracker.add_index(income_level);
          household_sets[census_tract][income_level].Add((Household)person.get_household());
          Household.census_tract_set.Add(census_tract);
        }
      }

      var count_hh_per_income_cat = new int[(int)Household_income_level_code.UNCLASSIFIED];
      var hh_income_per_income_cat = new double[(int)Household_income_level_code.UNCLASSIFIED];

      //Initialize the Income_Category_Tracker keys
      for (int i = (int)Household_income_level_code.CAT_I; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
      {
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_households", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_people", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_school_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_workers", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_households", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_people", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_school_children", (int)0);
        Global.Income_Category_Tracker.set_index_key_pair(i, "number_of_gq_workers", (int)0);
        hh_income_per_income_cat[i] = 0.0;
        count_hh_per_income_cat[i] = 0;
      }

      foreach (var census_tract_itr in Household.census_tract_set)
      {
        int count_people_per_census_tract = 0;
        int count_children_per_census_tract = 0;
        int count_hh_per_census_tract = 0;
        int count_hh_per_income_cat_per_census_tract = 0;
        double hh_income_per_census_tract = 0.0;

        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_people", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_school_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_workers", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_gq_households", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_gq_people", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_gq_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_gq_school_children", (int)0);
        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_gq_workers", (int)0);

        for (int i = (int)Household_income_level_code.CAT_I; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
        {
          count_hh_per_income_cat_per_census_tract += (int)household_sets[census_tract_itr][i].Count;
          count_hh_per_income_cat[i] += (int)household_sets[census_tract_itr][i].Count;
          count_hh_per_census_tract += (int)household_sets[census_tract_itr][i].Count;
          int count_people_per_income_cat = 0;
          int count_children_per_income_cat = 0;
          string buffer = string.Empty;

          //First increment the Income Category Tracker Household key (not census tract stratified)
          Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_households", (int)household_sets[census_tract_itr][i].Count);

          //Per income category
          buffer = string.Format("{0}_number_of_households", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)household_sets[census_tract_itr][i].Count);
          buffer = string.Format("{0}_number_of_people", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_children", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_school_children", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_workers", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_gq_people", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_gq_children", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_gq_school_children", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);
          buffer = string.Format("{0}_number_of_gq_workers", Household.household_income_level_lookup(i));
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, buffer, (int)0);

          foreach (var hh in household_sets[census_tract_itr][i])
          {

            count_people_per_income_cat += hh.get_size();
            count_people_per_census_tract += hh.get_size();
            //Store size info for later usage
            Household.count_inhabitants_by_household_income_level_map[i] += hh.get_size();
            Household.count_children_by_household_income_level_map[i] += hh.get_children();
            count_children_per_income_cat += hh.get_children();
            count_children_per_census_tract += hh.get_children();
            hh_income_per_income_cat[i] += (float)hh.get_household_income();
            hh_income_per_census_tract += (float)hh.get_household_income();

            //First, increment the Income Category Tracker Household key (not census tract stratified)
            if (hh.is_group_quarters())
            {
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_households", (int)1);
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_people", hh.get_size());
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_children", hh.get_children());
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_people", hh.get_size());
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_children", hh.get_children());
            }
            else
            {
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_people", hh.get_size());
              Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_children", hh.get_children());
            }

            //Next, increment the Tract tracker keys
            if (hh.is_group_quarters())
            {
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_gq_households", (int)1);
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_gq_people", hh.get_size());
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_gq_children", hh.get_children());
              buffer = string.Format("{0}_number_of_gq_people", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_size());
              buffer = string.Format("{0}_number_of_gq_children", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_children());
              //Don't forget the total counts
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_people", hh.get_size());
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_children", hh.get_children());
              buffer = string.Format("{0}_number_of_people", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_size());
              buffer = string.Format("{0}_number_of_children", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_children());
            }
            else
            {
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_people", hh.get_size());
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_children", hh.get_children());
              buffer = string.Format("{0}_number_of_people", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_size());
              buffer = string.Format("{0}_number_of_children", Household.household_income_level_lookup(i));
              Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, hh.get_children());
            }

            var inhab_vec = hh.get_inhabitants();
            foreach (var inhab in inhab_vec)
            {
              if (inhab.is_child())
              {
                if (inhab.get_school() != null)
                {
                  if (hh.is_group_quarters())
                  {
                    //First, increment the Income Category Tracker Household key (not census tract stratified)
                    Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_school_children", (int)1);
                    //Next, increment the Tract tracker keys
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_gq_school_children", (int)1);
                    buffer = string.Format("{0}_number_of_gq_school_children", Household.household_income_level_lookup(i));
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                    //Don't forget the total counts
                    //First, increment the Income Category Tracker Household key (not census tract stratified)
                    Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_school_children", (int)1);
                    //Next, increment the Tract tracker keys
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_school_children", (int)1);
                    buffer = string.Format("{0}_number_of_school_children", Household.household_income_level_lookup(i));
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                  }
                  else
                  {
                    //First, increment the Income Category Tracker Household key (not census tract stratified)
                    Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_school_children", (int)1);
                    //Next, increment the Tract tracker keys
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_school_children", (int)1);
                    buffer = string.Format("{0}_number_of_school_children", Household.household_income_level_lookup(i));
                    Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                  }
                }
              }
              if (inhab.get_workplace() != null)
              {
                if (hh.is_group_quarters())
                {
                  //First, increment the Income Category Tracker Household key (not census tract stratified)
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_gq_workers", (int)1);
                  //Next, increment the Tract tracker keys
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_gq_workers", (int)1);
                  buffer = string.Format("{0}_number_of_gq_workers", Household.household_income_level_lookup(i));
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                  //Don't forget the total counts
                  //First, increment the Income Category Tracker Household key (not census tract stratified)
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_workers", (int)1);
                  //Next, increment the Tract tracker keys
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_workers", (int)1);
                  buffer = string.Format("{0}_number_of_workers", Household.household_income_level_lookup(i));
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                }
                else
                {
                  //First, increment the Income Category Tracker Household key (not census tract stratified)
                  Global.Income_Category_Tracker.increment_index_key_pair(i, "number_of_workers", (int)1);
                  //Next, increment the Tract tracker keys
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, "number_of_workers", (int)1);
                  buffer = string.Format("{0}_number_of_workers", Household.household_income_level_lookup(i));
                  Global.Tract_Tracker.increment_index_key_pair(census_tract_itr, buffer, (int)1);
                }
              }
            }
          }
        }

        Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "number_of_households", count_hh_per_census_tract);
        if (count_hh_per_census_tract > 0)
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "mean_household_income", (hh_income_per_census_tract / (double)count_hh_per_census_tract));
        }
        else
        {
          Global.Tract_Tracker.set_index_key_pair(census_tract_itr, "mean_household_income", (double)0.0);
        }

        //Store size info for later usage
        Household.count_inhabitants_by_census_tract_map[census_tract_itr] = count_people_per_census_tract;
        Household.count_children_by_census_tract_map[census_tract_itr] = count_children_per_census_tract;
      }

      for (int i = (int)Household_income_level_code.CAT_I; i < (int)Household_income_level_code.UNCLASSIFIED; ++i)
      {
        if (count_hh_per_income_cat[i] > 0)
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "mean_household_income", (hh_income_per_income_cat[i] / (double)count_hh_per_income_cat[i]));
        }
        else
        {
          Global.Income_Category_Tracker.set_index_key_pair(i, "mean_household_income", (double)0.0);
        }
      }
    }

    public int size()
    {
      Utils.assert(this.blq.Count == this.pop_size);
      return this.blq.Count;
    }

    public void get_age_distribution(out int[] count_males_by_age, out int[] count_females_by_age)
    {
      count_males_by_age = new int[Demographics.MAX_AGE + 1];
      count_females_by_age = new int[Demographics.MAX_AGE + 1];
      for (int i = 0; i <= Demographics.MAX_AGE; ++i)
      {
        count_males_by_age[i] = 0;
        count_females_by_age[i] = 0;
      }
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person == null)
        {
          continue;
        }
        int age = person.get_age();
        if (age > Demographics.MAX_AGE)
        {
          age = Demographics.MAX_AGE;
        }
        if (person.get_sex() == 'F')
        {
          count_females_by_age[age]++;
        }
        else
        {
          count_males_by_age[age]++;
        }
      }
    }


    public List<string[]> get_demes()
    {
      return this.demes;
    }

    public void initialize_activities()
    {
      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          person.prepare_activities();
        }
      }
    }

    public void initialize_demographic_dynamics()
    {
      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          person.get_demographics().initialize_demographic_dynamics(person);
        }
      }
    }

    public void initialize_population_behavior()
    {
      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          person.setup_behavior();
        }
      }
    }

    public bool is_load_completed()
    {
      return this.load_completed;
    }

    public void initialize_health_insurance()
    {
      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          set_health_insurance(person);
        }
      }
    }

    public void set_health_insurance(Person p)
    {

      //  assert(Global.Places.is_load_completed());
      //
      //  //65 or older will use Medicare
      //  if(p.get_real_age() >= 65.0) {
      //    p.get_health().set_insurance_type(Insurance_assignment_index.MEDICARE);
      //  } else {
      //    //Get the household of the agent to see if anyone already has insurance
      //    Household* hh = static_cast<Household*>(p.get_household());
      //    if(hh == null) {
      //      if(Global.Enable_Hospitals && p.is_hospitalized() && p.get_permanent_household() != null) {
      //        hh = static_cast<Household*>(p.get_permanent_household());;
      //      }
      //    }
      //    assert(hh != null);
      //
      //    double low_income_threshold = 2.0 * Household.get_min_hh_income();
      //    double hh_income = hh.get_household_income();
      //    if(hh_income <= low_income_threshold && Random.draw_random() < (1.0 - (hh_income / low_income_threshold))) {
      //      p.get_health().set_insurance_type(Insurance_assignment_index.MEDICAID);
      //    } else {
      //      std.vector<Person*> inhab_vec = hh.get_inhabitants();
      //      for(std.vector<Person*>.iterator itr = inhab_vec.begin();
      //          itr != inhab_vec.end(); ++itr) {
      //        Insurance_assignment_index.e insr = (*itr).get_health().get_insurance_type();
      //        if(insr != Insurance_assignment_index.UNSET && insr != Insurance_assignment_index.MEDICARE) {
      //          //Set this agent's insurance to the same one
      //          p.get_health().set_insurance_type(insr);
      //          return;
      //        }
      //      }
      //
      //      //No one had insurance, so set to insurance from distribution
      //      Insurance_assignment_index.e insr = Health.get_health_insurance_from_distribution();
      //      p.get_health().set_insurance_type(insr);
      //    }
      //  }

      //If agent already has insurance set (by another household agent), then return
      if (p.get_health().get_insurance_type() != Insurance_assignment_index.UNSET)
      {
        return;
      }

      //Get the household of the agent to see if anyone already has insurance
      var hh = (Household)(p.get_household());
      if (hh == null)
      {
        if (Global.Enable_Hospitals && p.is_hospitalized() && p.get_permanent_household() != null)
        {
          hh = (Household)(p.get_permanent_household()); ;
        }
      }
      Utils.assert(hh != null);
      var inhab_vec = hh.get_inhabitants();
      foreach (var inhab in inhab_vec)
      {
        Insurance_assignment_index insrI = inhab.get_health().get_insurance_type();
        if (insrI != Insurance_assignment_index.UNSET)
        {
          //Set this agent's insurance to the same one
          p.get_health().set_insurance_type(insrI);
          return;
        }
      }

      //No one had insurance, so set everyone in household to the same insurance
      Insurance_assignment_index insr = Health.get_health_insurance_from_distribution();
      foreach (var inhab in inhab_vec)
      {
        inhab.get_health().set_insurance_type(insr);
      }
    }

    public void update_health_interventions(int day)
    {
      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          person.update_health_interventions(day);
        }
      }
    }

    /**
     * Write out the population in a format similar to the population input files (with additional runtime information)
     * @param day the simulation day
     */
    private void write_population_output_file(int day)
    {
      //Loop over the whole population and write the output of each Person's to_string to the file
      var population_output_file = $"{Global.Output_directory}/{Population.pop_outfile}_{Date.get_date_string()}.txt";
      using var fp = new StreamWriter(population_output_file);
      if (fp == null)
      {
        Utils.fred_abort("Help! population_output_file %s not found\n", population_output_file);
      }

      // NOTE: use this idiom to loop through pop.
      // Note that pop_size is the number of valid indexes, NOT the size of blq.
      for (int p = 0; p < this.get_index_size(); ++p)
      {
        var person = get_person_by_index(p);
        if (person != null)
        {
          fp.WriteLine(person.ToString());
        }
      }
      fp.Flush();
      fp.Dispose();
    }


    //private void mother_gives_birth(int day, Person mother);

    private void parse_lines_from_stream(TextReader stream, bool is_group_quarters_pop)
    {
      // vector used for batch add of new persons
      List<Person_Init_Data> pidv = new List<Person_Init_Data>(2000000);

      // flag for 2010_ver1 format
      bool is_2010_ver1_format = false;

      int n = 0;
      while (stream.Peek() != -1)
      {
        var line = stream.ReadLine();
        // check for 2010_ver1 format
        if (line.StartsWith("sp_id"))
        {
          is_2010_ver1_format = true;
          continue;
        }

        // skip empty lines...
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("sp_id") || line.StartsWith("p_id"))
        {
          continue;
        }

        line = line.Trim().Replace("\"", string.Empty);
        var pid = get_person_init_data(line, is_group_quarters_pop, is_2010_ver1_format);
        // verbose printing of all person initialization data
        if (Global.Verbose > 1)
        {
          Utils.FRED_VERBOSE(1, "{0}", pid);
        }

        //skip header line
        if (pid.label == "p_id")
        {
          continue;
        }

        if (pid.house != null)
        {
          // create a Person_Init_Data object
          pidv.Add(pid);
        }
        else
        {
          // we need at least a household (homeless people not yet supported), so
          // skip this person
          Utils.FRED_VERBOSE(0, "WARNING: skipping person %s -- %s %s\n", pid.label,
           "no household found for label =", pid.house_label);
        }
        Utils.FRED_VERBOSE(1, "person %d = %s -- house_label %s\n", n, pid.label, pid.house_label);
        n++;
      } // <----- end while loop over stream
      Utils.FRED_VERBOSE(0, "end of stream, persons = %d\n", n);

      // Iterate through vector of already parsed initialization data and
      // add to population bloque.  More efficient to do this in batches; also
      // preserves the (fine-grained) order in the population file.  Protect
      // with mutex so that we do this sequentially and avoid thrashing the 
      // scoped mutex in add_person.
      this.batch_add_person_mutex.WaitOne();
      foreach (var pid in pidv)
      {
        // here the person is actually created and added to the population
        // The person's unique id is automatically assigned
        add_person(pid.age, pid.sex, pid.race, pid.relationship, pid.house, pid.school, pid.work, pid.day, pid.today_is_birthday);
      }
      this.batch_add_person_mutex.ReleaseMutex();
    }

    private PopFileColIndex get_pop_file_col_index(bool is_group_quarters, bool is_2010_ver1)
    {
      if (is_group_quarters)
      {
        if (is_2010_ver1)
        {
          return new GQ_PopFileColIndex_2010_ver1();// gq_pop_file_col_index_2010_ver1;
        }
        else
        {
          return new GQ_PopFileColIndex();// gq_pop_file_col_index;
        }
      }
      else
      {
        return new HH_PopFileColIndex();// hh_pop_file_col_index;
      }
    }

    private Person_Init_Data get_person_init_data(string line,
              bool is_group_quarters_population,
              bool is_2010_ver1_format)
    {
      var tokens = line.Split(',');
      for (int i = 0; i < tokens.Length; i++)
      {
        string token = tokens[i];
        if (string.IsNullOrWhiteSpace(token))
        {
          tokens[i] = "-1";
        }
      }
      var col = get_pop_file_col_index(is_group_quarters_population, is_2010_ver1_format);
      Utils.assert(tokens.Length == col.number_of_columns);
      // initialized with default values
      var pid = new Person_Init_Data
      {
        label = tokens[col.p_id]
      };
      // add type indicator to label for places
      if (is_group_quarters_population)
      {
        pid.in_grp_qrtrs = true;
        pid.gq_type = tokens[col.gq_type][0];
        // columns present in group quarters population
        if (tokens[col.home_id] != "-1")
        {
          pid.house_label = $"H{tokens[col.home_id]}";
        }
        if (tokens[col.workplace_id] != "-1")
        {
          pid.work_label = $"W{tokens[col.workplace_id]}";
        }
        // printf("GQ person %s house %s work %s\n", pid.label, pid.house_label, pid.work_label);
      }
      else
      {
        // columns not present in group quarters population
        pid.relationship = Convert.ToInt32(tokens[col.relate]);
        pid.race = Convert.ToInt32(tokens[col.race_str]);
        // schools only defined for synth_people
        if (tokens[col.school_id] != "-1")
        {
          pid.school_label = $"S{tokens[col.school_id]}";
        }
        // standard formatting for house and workplace labels
        if (tokens[col.home_id] != "-1")
        {
          pid.house_label = $"H{tokens[col.home_id]}";
        }
        if (tokens[col.workplace_id] != "-1")
        {
          pid.work_label = $"W{tokens[col.workplace_id]}";
        }
      }

      // age, sex same for synth_people and synth_gq_people
      pid.age = Convert.ToInt32(tokens[col.age_str]);
      pid.sex = tokens[col.sex_str] == "1" ? 'M' : 'F';
      // set pointer to primary places in init data object
      pid.house = Global.Places.get_place_from_label(pid.house_label);
      pid.work = Global.Places.get_place_from_label(pid.work_label);
      pid.school = Global.Places.get_place_from_label(pid.school_label);
      // warn if we can't find workplace
      if (pid.work_label != "-1" && pid.work == null)
      {
        Utils.FRED_VERBOSE(2, "WARNING: person %s -- no workplace found for label = %s\n", pid.label, pid.work_label);
        if (Global.Enable_Local_Workplace_Assignment)
        {
          pid.work = Global.Places.get_random_workplace();
          Utils.FRED_CONDITIONAL_VERBOSE(0, pid.work != null, "WARNING: person %s assigned to workplace %s\n",
                 pid.label, pid.work.get_label());
          Utils.FRED_CONDITIONAL_VERBOSE(0, pid.work == null,
                 "WARNING: no workplace available for person %s\n", pid.label);
        }
      }
      // warn if we can't find school.  No school for gq_people
      Utils.FRED_CONDITIONAL_VERBOSE(0, (pid.school_label != "-1" && pid.school == null),
             "WARNING: person %s -- no school found for label = %s\n", pid.label, pid.school_label);

      return pid;
    }
  }
}
