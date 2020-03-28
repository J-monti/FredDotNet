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
  public class Manager
  {
    public Manager()
    {
      this.Policies = new List<Policy>();
    }

    public Manager(Population pop)
    {
      this.CurrentPolicy = 0;
      this.Population = pop;
      this.Policies = new List<Policy>();
    }

    public Population Population { get; }

    public int CurrentPolicy { get; }

    public List<Policy> Policies { get; }

    public int PollManager(Person p, int disease, DateTime day)
    {
      return this.Policies[this.CurrentPolicy].Choose(p, disease, day);
    }

    /**
     * Perform the daily update for this object
     *
     * @param day the simulation day
     */
    public virtual void Update(int day) { }

    /**
     * Put this object back to its original state
     */
    public virtual void Reset() { }

    /**
     * Print out information about this object
     */
    public virtual void Print() { }
  }
}
