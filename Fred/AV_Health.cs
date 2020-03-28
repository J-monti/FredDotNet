using System;

namespace Fred
{
  public class AV_Health
  {
    public AV_Health(DateTime startDay, Antiviral antiviral, Health health)
    {
      this.AVEndDay = null;
      this.Health = health;
      this.AntiViral = antiviral;
      this.Disease = this.AntiViral.Disease;
      this.AVStartDay = startDay.AddDays(1);
      this.AVEndDay = this.AVStartDay.Add(this.AntiViral.CourseLength);
    }

    public int Disease { get; }
    public Health Health { get; }
    public DateTime? AVEndDay { get; }
    public DateTime AVStartDay { get; }
    public Antiviral AntiViral { get; }

    public virtual bool IsOnAv(DateTime day)
    {
      return (day >= AVStartDay) && (day <= AVEndDay);
    }

    public virtual bool IsEffective()
    {
      return AVEndDay.HasValue;
    }
  }
}
