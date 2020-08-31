using System;
using System.Collections.Generic;

namespace Fred
{
  /**
   * Manager is an abstract class that is the embodiment of a mitigation manager.
   * The Manager:
   *  1. Handles a stock of mitigation supplies
   *  2. Holds the policy for doling out a mitigation strategy
   */
  public abstract class Manager
  {
    protected List<Policy> policies;   // vector to hold the policies this manager can apply
    protected List<int> results;        // DEPRICATE holds the results of the policies
    protected Population pop;               // Population in which this manager is tied to
    protected int current_policy;            // The current policy this manager is using

    /**
   * Default constructor
   */
    public Manager()
    {
      this.current_policy = -1;
    }

    /**
     * Constructor that sets the Population to which this Manager is tied
     */
    public Manager(Population _pop)
    {
      this.pop = _pop;
      this.current_policy = 0;
    }

    /**
     * Member to allow someone to see if they fit the current policy
     *
     * @param p a pointer to a Person object
     * @param disease the disease to poll for
     * @param day the simulation day
     *
     * @return the manager's decision
     */
    public virtual int poll_manager(Person p, int disease, int day) //member to allow someone to see if they fit the current policy
    {
      return this.policies[this.current_policy].choose(p, disease, day);
    }

    // Parameters
    /**
     * @return a pointer to the Population object to which this manager is tied
     */
    public Population get_population()
    {
      return this.pop;
    }

    /**
     * @return the current policy this manager is using
     */
    public int get_current_policy()
    {
      return this.current_policy;
    }

    //Utility Members
    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public virtual void update(int day) { }

    /**
     * Put this object back to its original state
     */
    public virtual void reset() { }

    /**
     * Print out information about this object
     */
    public virtual void print() { }
  }
}
