namespace Fred
{
  public class DiseaseCountInfo
  {
    public int tot_ppl_evr_inf;
    public int tot_ppl_evr_sympt;
    public int tot_chldrn_evr_inf;
    public int tot_chldrn_evr_sympt;
    public int tot_sch_age_chldrn_evr_inf;
    public int tot_sch_age_chldrn_ever_sympt;
    public int tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf;
    public int tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt;
    public override string ToString()
    {
      return string.Format("Disease Count Info - tot_ppl_evr_inf: {0} | tot_ppl_evr_sympt: {1} | tot_chldrn_evr_inf: {2} | tot_chldrn_evr_sympt: {3} | tot_sch_age_chldrn_evr_inf: {4} | tot_sch_age_chldrn_ever_sympt: {5} | tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf: {6} | tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt: {7} ",
        tot_ppl_evr_inf,
        tot_ppl_evr_sympt,
        tot_chldrn_evr_inf,
        tot_chldrn_evr_sympt,
        tot_sch_age_chldrn_evr_inf,
        tot_sch_age_chldrn_ever_sympt,
        tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf,
        tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt);
    }
  }
}
