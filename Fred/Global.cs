using System;
using System.IO;

namespace Fred
{
  internal static class Global
  {
    static Global()
    {
      SimulationRunNumber = 1;
      Places = new PlaceList();
      Diseases = new DiseaseList();
      Population = new Population();
      SimulationRegion = new RegionalLayer();
    }

    public const int MAX_NUM_DISEASES = 4;
    public const int ADULT_AGE = 18;

    public static int Days { get; set; }
    public static DateTime SimulationDay { get; set; }
    public static int DayCount { get; set; }
    public static DateTime? ReseedDay { get; set; }
    public static DateTime StartDate { get; set; }
    public static int SimulationRunNumber { get; set; }
    public static int EpidemicOffset { get; set; }
    public static int VaccineOffset { get; set; }
    public static int SimulationSeed { get; set; }
    public static int Seed { get; set; }
    public static int DaysToWearFaceMask { get; set; }
    public static double FaceMaskCompliance { get; set; }
    public static double HandWashingCompliance { get; set; }
    public static bool IsViralEvolutionEnabled { get; set; }
    public static bool IsVectorLayerEnabled { get; set; }
    public static bool IsHospitalsEnabled { get; set; }
    public static bool IsBehaviorsEnabled { get; set; }
    public static bool TrackAgeDistribution { get; set; }

    public static PlaceList Places { get; }
    public static Population Population { get; }
    public static RegionalLayer SimulationRegion { get; }
    public static DiseaseList Diseases { get; }

    public static int Debug { get; set; }
    public static int Verbose { get; set; }
    public static StreamWriter Output { get; set; }
    public static string SimulationDirectory { get; set; }
    public static bool IsTravelEnabled { get; internal set; }

    public static bool[] DefaultDiseaseArray()
    {
      return new bool[MAX_NUM_DISEASES];
    }
  }
}
