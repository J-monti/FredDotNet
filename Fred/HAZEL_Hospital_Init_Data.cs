namespace Fred
{
  public class HAZEL_Hospital_Init_Data
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

    public HAZEL_Hospital_Init_Data(string _panel_week, string _accpt_private, string _accpt_medicare,
        string _accpt_medicaid, string _accpt_highmark, string _accpt_upmc,
        string _accpt_uninsured, string _reopen_after_days, string _is_mobile, string _add_capacity)
    {
      Setup(_panel_week, _accpt_private, _accpt_medicare,
            _accpt_medicaid, _accpt_highmark, _accpt_upmc,
            _accpt_uninsured, _reopen_after_days, _is_mobile,
            _add_capacity);
    }

    public void Setup(string _panel_week, string _accpt_private, string _accpt_medicare,
                       string _accpt_medicaid, string _accpt_highmark, string _accpt_upmc,
                       string _accpt_uninsured, string _reopen_after_days, string _is_mobile,
                       string _add_capacity)
    {
      int.TryParse(_panel_week, out this.panel_week);
      int.TryParse(_reopen_after_days, out this.reopen_after_days);
      this.accpt_private = _accpt_private == "1";
      this.accpt_medicare = _accpt_medicare == "1";
      this.accpt_medicaid = _accpt_medicaid == "1";
      this.accpt_highmark = _accpt_highmark == "1";
      this.accpt_upmc = _accpt_upmc == "1";
      this.accpt_uninsured = _accpt_uninsured == "1";
      this.is_mobile = _is_mobile == "1";
      this.add_capacity = _add_capacity == "1";
    }
  }
}
