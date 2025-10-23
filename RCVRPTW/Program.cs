// See https://aka.ms/new-console-template for more information

using RCVRPTW;
using System.Diagnostics;

int[] iters = new[] { 100, 500, 2000 };
int[] tabu = new[] { 10, 30, 50 };
int numberScenarios = 10;

var scenarios = InstanceGenerator.GenerateManyScenarios(10);
var sw = Stopwatch.StartNew();
var rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, repeats: 5, baseSeed: 42, parallel: true);
Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
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

        public static List<Scenario> GenerateManyScenarios(int count)
        {
            var rng = new Random();
            var scenarios = new List<Scenario>();
            for (int i = 0; i < count; i++)
                scenarios.Add(GenerateInstance(i, rng, "..\\..\\..\\pliki\\CTEST.txt"));
            return scenarios;
        }
    }
}
public class ExperimentResult
{
    public int ScenarioId { get; set; }
    public int Iterations { get; set; }
    public int TabuSize { get; set; }
    public int Repeat { get; set; }
    public int Seed { get; set; }

    public double GreedyObjective { get; set; }

    public double Objective { get; set; } // total cost + penalty + vehicleOpTime
    public double TotalCost { get; set; }
    public double TotalPenalty { get; set; }
    public double TotalVehicleOperationTime { get; set; }
    public int RoutesCount { get; set; }
    public double DurationMs { get; set; }
}



public static class ExperimentRunner
{
    // Uruchamia eksperymenty i zwraca listę surowych wyników
    public static List<ExperimentResult> RunExperiments(
        List<Scenario> scenarios,
        int[] iterationsGrid,
        int[] tabuSizeGrid,
        int repeats = 5,
        int baseSeed = 12345,
        bool parallel = true)
    {
        var results = new List<ExperimentResult>();
        var lockObj = new object();

        var tasks = new List<Action>();
        Console.WriteLine("Starting experiments..."+scenarios.Count*iterationsGrid.Length*tabuSizeGrid.Length*repeats);
        foreach (var scen in scenarios)
        {
            for (int it = 0; it < iterationsGrid.Length; it++)
                for (int ts = 0; ts < tabuSizeGrid.Length; ts++)
                {
                    int iterations = iterationsGrid[it];
                    int tabuSize = tabuSizeGrid[ts];

                    for (int rep = 0; rep < repeats; rep++)
                    {
                        int seed = baseSeed + scen.ScenarioId * 1000 + iterations * 10 + tabuSize * 100 + rep;
                        Action work = () =>
                        {
                            var rng = new Random(seed);
                            var sw = Stopwatch.StartNew();
                            
                            var instance = scen.Instance;
                            var solution = TabuSearch.run(iterations, tabuSize, instance); 
                            sw.Stop();

                            var res = new ExperimentResult
                            {
                                ScenarioId = scen.ScenarioId,
                                Iterations = iterations,
                                TabuSize = tabuSize,
                                Repeat = rep,
                                Seed = seed,
                                GreedyObjective = solution.GreedyMetrics.greedyTotalCost + solution.GreedyMetrics.greedyTotalPenalty + solution.GreedyMetrics.greedyVOT,
                                Objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime,
                                TotalCost = solution.TotalCost,
                                TotalPenalty = solution.TotalPenalty,
                                TotalVehicleOperationTime = solution.TotalVehicleOperationTime,
                                RoutesCount = solution.Routes.Count,
                                DurationMs = sw.Elapsed.TotalMilliseconds,
                            };

                            lock (lockObj)
                            {
                                results.Add(res);
                                AppendResultToCsv("..\\..\\..\\results_raw.csv", res);
                            }
                        };

                        tasks.Add(work);
                    }
                }
        }

        int total = tasks.Count;
        int completed = 0;

        if (parallel)
        {
            Parallel.ForEach(tasks, t =>
            {
                t();
                int now = System.Threading.Interlocked.Increment(ref completed);
                Console.Write($"\rDone {now}/{total}");
            });
        }
        else
        {
            foreach (var t in tasks)
            {
                t();
                int now = System.Threading.Interlocked.Increment(ref completed);
                
                Console.Write($"\rDone {now}/{total}");
            }
        }

        return results;
    }

    private static void AppendResultToCsv(string path, ExperimentResult res)
    {
        var header = "ScenarioId,Iterations,TabuSize,Repeat,Seed,GreedyOjective,Objective,TotalCost,TotalPenalty,TotalVehicleOperationTime,RoutesCount,DurationMs";
        var exists = File.Exists(path);
        using (var sw = new StreamWriter(path, append: true))
        {
            if (!exists) sw.WriteLine(header);
            sw.WriteLine($"{res.ScenarioId},{res.Iterations},{res.TabuSize},{res.Repeat},{res.Seed},{res.GreedyObjective},{res.Objective},{res.TotalCost},{res.TotalPenalty},{res.TotalVehicleOperationTime},{res.RoutesCount},{res.DurationMs}");
        }
    }
}