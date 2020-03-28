namespace Fred
{
  public class HazelHospitalInitData
  {
    public int panel_week;
    public int reopen_after_days;
    public bool accpt_private;
    public bool accpt_medicare;
    public bool accpt_medicaid;
    public bool accpt_highmark;
    public bool accpt_upmc;
    public bool accpt_uninsured; // Person has no insurance and pays out of pocket
    public bool is_mobile;
    public bool add_capacity;

    public HazelHospitalInitData(int _panel_week, bool _accpt_private, bool _accpt_medicare,
        bool _accpt_medicaid, bool _accpt_highmark, bool _accpt_upmc,
        bool _accpt_uninsured, int _reopen_after_days, bool _is_mobile, bool _add_capacity)
    {
      Setup(_panel_week, _accpt_private, _accpt_medicare,
            _accpt_medicaid, _accpt_highmark, _accpt_upmc,
            _accpt_uninsured, _reopen_after_days, _is_mobile,
            _add_capacity);
    }

    public void Setup(int _panel_week, bool _accpt_private, bool _accpt_medicare,
                       bool _accpt_medicaid, bool _accpt_highmark, bool _accpt_upmc,
                       bool _accpt_uninsured, int _reopen_after_days, bool _is_mobile,
                       bool _add_capacity)
    {
      this.panel_week = _panel_week;
      this.reopen_after_days = _reopen_after_days;
      this.accpt_private = _accpt_private;
      this.accpt_medicare = _accpt_medicare;
      this.accpt_medicaid = _accpt_medicaid;
      this.accpt_highmark = _accpt_highmark;
      this.accpt_upmc = _accpt_upmc;
      this.accpt_uninsured = _accpt_uninsured;
      this.is_mobile = _is_mobile;
      this.add_capacity = _add_capacity;
    }
  }
}
