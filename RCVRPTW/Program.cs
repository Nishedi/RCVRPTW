// See https://aka.ms/new-console-template for more information

using RCVRPTW;
using System.Diagnostics;

int[] iters = new[] { /*100, 500, 2000*/1000 };
int[] tabu = new[] { /*10, 30,*/ 50 };
int numberScenarios = 1000;
string[] fileNames = new[] { "pliki//100 lokacji//C101.txt", "pliki//100 lokacji//C201.txt", "pliki//100 lokacji//R101.txt", "pliki//100 lokacji//R201.txt",
    "pliki//100 lokacji//RC101.txt", "pliki//100 lokacji//RC201.txt",
    //"pliki//200 lokacji//C1_2_2_o.TXT", "pliki//200 lokacji//C2_2_2_o.TXT", "pliki//200 lokacji//C1_2_2_o.TXT", "pliki//200 lokacji//R1_2_1_o.TXT",
    //"pliki//200 lokacji//R2_2_1_o.TXT", "pliki//200 lokacji//RC1_2_1_o.TXT", "pliki//200 lokacji//RC2_2_1_o.TXT"
};

var scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios,fileNames);
var sw = Stopwatch.StartNew();
var rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, repeats: 2, baseSeed: 42, parallel: true);
Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");


var features = scenarios.Select(s => ScenarioAnalyzer.ExtractFeatures(s)).ToList();
var mapping = ScenarioAnalyzer.KMeansCluster(features, k: 3);

var groupedByCluster = rawResults.GroupBy(r => mapping[r.ScenarioId]);
foreach (var g in groupedByCluster)
{
    Console.WriteLine($"Cluster {g.Key}: runs={g.Count()}, mean objective={g.Average(x => x.Objective)}");
}
namespace RCVRPTW
{
    public class Scenario
    {
        public int ScenarioId { get; }
        public Instance Instance { get; set; } 
        public Scenario(int scenarioId, Instance instance)
        {
            ScenarioId = scenarioId;
            Instance = instance;
        }
    }

    public class InstanceGenerator
    {
        private List<Location> _locations;

        public InstanceGenerator(List<Location> locations)
        {
            _locations = locations;
        }

        public static Scenario GenerateInstance(int scenarioId, Random rng, string filename)
        {
            var preparedInstance = new Instance(filename, 4, true, true);

            return new Scenario(scenarioId, preparedInstance);
        }

        public static List<Scenario> GenerateManyScenarios(int numScenariosPerFile, string[] FileNames)
        {
            var rng = new Random();
            var scenarios = new List<Scenario>();
            foreach (var filename in FileNames)
                for (int i = 0; i < numScenariosPerFile; i++)
                    scenarios.Add(GenerateInstance(i, rng, filename));
            return scenarios;
        }
    }
}
