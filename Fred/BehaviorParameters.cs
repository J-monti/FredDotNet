namespace Fred
{
  public class BehaviorParameters
  {
    public const int NUMWEIGHTS = 7;

    public BehaviorParameters()
    {
      int changeCount = (int)BehaviorChangeEnum.NumBehaviorChangeModels + 1;
      this.BehaviorChangeModelCdf = new double[changeCount];
      this.BehaviorChangeModelPopulation = new int[changeCount];
      this.ImitatePrevalenceWeight = new double[NUMWEIGHTS];
      this.ImitateConsensusWeight = new double[NUMWEIGHTS];
      this.ImitateCountWeight = new double[NUMWEIGHTS];
      this.SusceptibilityThresholdDistr = new double[2];
      this.SeverityThresholdDistr = new double[2];
      this.BenefitsThresholdDistr = new double[2];
      this.BarriersThresholdDistr = new double[2];
  }

    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public int Frequency { get; set; }
    public int BehaviorChangeModelCdfSize { get; set; }
    public double[] BehaviorChangeModelCdf { get; }
    public int[] BehaviorChangeModelPopulation { get; }
    // FLIP
    public double MinProb { get; set; }
    public double MaxProb { get; set; }
    // IMITATE params
    public int ImitationEnabled { get; set; }
    // IMITATE PREVALENCE
    public double[] ImitatePrevalenceWeight { get; }
    public double ImitatePrevalenceTotalWeight { get; set; }
    public double ImitatePrevalenceUpdateRate { get; set; }
    public double ImitatePrevalenceThreshold { get; set; }
    public int ImitatePrevalenceCount { get; set; }
    // IMITATE CONSENSUS
    public double[] ImitateConsensusWeight { get; }
    public double ImitateConsensusTotalWeight { get; set; }
    public double ImitateConsensusUpdateRate { get; set; }
    public double ImitateConsensusThreshold { get; set; }
    public int ImitateConsensusCount { get; set; }
    // IMITATE COUNT
    public double[] ImitateCountWeight { get; }
    public double ImitateCountTotalWeight { get; set; }
    public double ImitateCountUpdateRate { get; set; }
    public double ImitateCountThreshold { get; set; }
    public int ImitateCountCount { get; set; }
    // HBM
    public double[] SusceptibilityThresholdDistr { get; }
    public double[] SeverityThresholdDistr { get; }
    public double[] BenefitsThresholdDistr { get; }
    public double[] BarriersThresholdDistr { get; }
    public double BaseOddsRatio { get; set; }
    public double SusceptibilityOddsRatio { get; set; }
    public double SeverityOddsRatio { get; set; }
    public double BenefitsOddsRatio { get; set; }
    public double BarriersOddsRatio { get; set; }
  }
}
