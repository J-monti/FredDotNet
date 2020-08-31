using System.Text;

namespace Fred
{
  public class Disease_Count_Info
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
      var builder = new StringBuilder();
      builder.AppendLine("Disease Count Info ");
      builder.AppendLine($" tot_ppl_evr_inf {tot_ppl_evr_inf}");
      builder.AppendLine($" tot_ppl_evr_sympt {tot_ppl_evr_sympt}");
      builder.AppendLine($" tot_chldrn_evr_inf {tot_chldrn_evr_inf}");
      builder.AppendLine($" tot_chldrn_evr_sympt {tot_chldrn_evr_sympt}");
      builder.AppendLine($" tot_sch_age_chldrn_evr_inf {tot_sch_age_chldrn_evr_inf}");
      builder.AppendLine($" tot_sch_age_chldrn_ever_sympt {tot_sch_age_chldrn_ever_sympt}");
      builder.AppendLine($" tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf {tot_sch_age_chldrn_w_home_adlt_crgvr_evr_inf}");
      builder.AppendLine($" tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt {tot_sch_age_chldrn_w_home_adlt_crgvr_evr_sympt}");
      return builder.ToString();
    }
  }
}
