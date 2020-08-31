namespace Fred
{
  public class Behavior_params
  {
    public const int NUM_WEIGHTS = 7;

    public string name;
    public bool enabled;
    public int frequency;
    public int behavior_change_model_cdf_size;
    public double[] behavior_change_model_cdf = new double[(int)Behavior_change_model_enum.NUM_BEHAVIOR_CHANGE_MODELS];
    public int[] behavior_change_model_population = new int[(int)Behavior_change_model_enum.NUM_BEHAVIOR_CHANGE_MODELS];
    // FLIP
    public double min_prob;
    public double max_prob;
    // IMITATE params
    public int imitation_enabled;
    // IMITATE PREVALENCE
    public double[] imitate_prevalence_weight = new double[NUM_WEIGHTS];
    public double imitate_prevalence_total_weight;
    public double imitate_prevalence_update_rate;
    public double imitate_prevalence_threshold;
    public int imitate_prevalence_count;
    // IMITATE CONSENSUS
    public double[] imitate_consensus_weight = new double[NUM_WEIGHTS];
    public double imitate_consensus_total_weight;
    public double imitate_consensus_update_rate;
    public double imitate_consensus_threshold;
    public int imitate_consensus_count;
    // IMITATE COUNT
    public double[] imitate_count_weight = new double[NUM_WEIGHTS];
    public double imitate_count_total_weight;
    public double imitate_count_update_rate;
    public double imitate_count_threshold;
    public int imitate_count_count;
    // HBM
    public double[] susceptibility_threshold_distr = new double[2];
    public double[] severity_threshold_distr = new double[2];
    public double[] benefits_threshold_distr = new double[2];
    public double[] barriers_threshold_distr = new double[2];
    public double base_odds_ratio;
    public double susceptibility_odds_ratio;
    public double severity_odds_ratio;
    public double benefits_odds_ratio;
    public double barriers_odds_ratio;
  }
}
