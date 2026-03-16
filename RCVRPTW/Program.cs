using Microsoft.VisualBasic.FileIO;
using RCVRPTW;
using System.Diagnostics;
using System.IO;
using System.Numerics;


public static class Program
{
    static int[] iters = new[] { /*100, 500, 2000*/200 };
    static int[] FoodSourcesCounts = new[] { 20/*100, 500, 2000*/};
    static int[] tabu = new[] { 50 };
    static int[] limits = new[] { 100 }; 
    static int numberScenarios = 1;
    static string[] fileNames = new[] { "pliki//100 lokacji//C101.txt",
        "pliki//100 lokacji//C201.txt", "pliki//100 lokacji//R101.txt", "pliki//100 lokacji//R201.txt",
        "pliki//100 lokacji//RC101.txt", "pliki//100 lokacji//RC201.txt"
    };
    static string[] mutationtypes = new[] { "swap" };//, "invert", "insert", "2opt", "oropt", "rand" };
    static void Main(string[] args)
    {
        //args = new[] { "C201", "3", "1" };
        if (args.Length > 0)
        {
            Console.WriteLine($"Running experiments for file type: {args[0]} {args[1]} second per test");
            int maxTime = 1;
            int numberScenarios = 1;
            bool useRL = false;
            string? rlModelPath = null;
            
            string[] results = { };

            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out int parsedMaxTime))
                {
                    maxTime = parsedMaxTime;
                }
                if (int.TryParse(args[2], out int parsednumberScenarios))
                {
                    numberScenarios = parsednumberScenarios;
                }
            }
            
            
            string fileType = args[0];
            var scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt"); Stopwatch sw = Stopwatch.StartNew();


            TestParametersBee testBee = new TestParametersBee(fileType, repeats: 2, maxTime: maxTime, numberScenario: numberScenarios, scenarios: scenarios);
            TestParameters testTabu = new TestParameters(fileType, repeats: 2, maxTime: maxTime, numberScenario: numberScenarios, scenarios: scenarios);
        }
        else
        {
            fileNames = new[] { "CTEST9.txt", "CTEST.txt", "CTEST11.txt", "CTEST12.txt"};

            fileNames = new [] { "100 lokacji//C101.txt" }; 
            //fileNames = new[] { "CTEST9.txt" }
            Dictionary<string, (double gurobi, double experiment, double beeExperiment, int time)> results = new Dictionary<string, (double gurobi, double experiment, double experimentBee, int time)>();
            foreach (var file in fileNames)
            {
                bool gurobi = false;
                Console.WriteLine("Tryb Gurobi");
                string testFile = "pliki//"+file;

                var scenarios = InstanceGenerator.GenerateManyScenarios(5, testFile);

                Stopwatch sw2 = Stopwatch.StartNew();
                var executionTime = 600;
                List<double> gurobiresults = new List<double>();
                for(int i = 0; i < scenarios.Count; i++)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    Instance instance = scenarios[i].Instance;
                    if (gurobi)
                        gurobiresults.Add(CVRPTW_Model.Solve(instance, timeLimitSeconds: 600.0));
                    else
                        gurobiresults.Add(0.0);
                    executionTime = (int) sw.Elapsed.TotalSeconds;
                }
                if (gurobi)
                    executionTime = Math.Min(120,Math.Max(executionTime, 10));
                else
                    executionTime = 300;


                List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, file, repeats: 1, baseSeed: 42, parallel: false, maxTime: (int) executionTime, useRL: false, rlModelPath: null);
                List<ExperimentResultBee> rawBeeResults = ExperimentRunner.RunExperimentsBee(scenarios, FoodSourcesCounts, limits, mutationtypes, file, repeats: 1, baseSeed: 42, parallel: false, maxTime: (int) executionTime);

                for (int i = 0; i < rawResults.Count; i++)
                {
                    results.Add(i+"_"+file, ((int)gurobiresults[i], (int)rawResults[i].Objective, (int)rawBeeResults[i].Objective, -1));
                    Console.WriteLine(i+"_"+file +" "+ ((int)gurobiresults[i] +" "+ (int)rawResults[i].Objective +" "+ (int)rawBeeResults[i].Objective));
                }
               
                Console.WriteLine($"\nAll experiments completed in {sw2.Elapsed.TotalSeconds} seconds.");
                using (var writer = new StreamWriter("results_comparison.csv"))
                {
                    writer.WriteLine("File,GurobiCost,ExperimentCost,BeeExperimentCost,gurobi_vs_tabu");
                    foreach (var kvp in results)
                    {
                        writer.WriteLine($"{kvp.Key},{kvp.Value.gurobi},{kvp.Value.experiment},{kvp.Value.beeExperiment},{(kvp.Value.gurobi - kvp.Value.experiment) / kvp.Value.experiment}");
                        Console.WriteLine($"{kvp.Key},{kvp.Value.gurobi},{kvp.Value.experiment},{kvp.Value.beeExperiment},{(kvp.Value.gurobi - kvp.Value.experiment) / kvp.Value.experiment}");

                    }
                }
            }
            using (var writer = new StreamWriter("results_comparison.csv"))
            {
                writer.WriteLine("File,GurobiCost,ExperimentCost,BeeExperimentCost,gurobi_vs_tabu");
                foreach (var kvp in results)
                {
                    writer.WriteLine($"{kvp.Key},{kvp.Value.gurobi},{kvp.Value.experiment},{kvp.Value.beeExperiment},{(kvp.Value.gurobi - kvp.Value.experiment) / kvp.Value.experiment}");
                }
            }
        }
    }
}
namespace RCVRPTW
{
    public class TestParameters
    {
        public int [] iters { get; set; }
        public int [] TabuSizes { get; set; }
        public string[] mutationtypes { get; set; }
        public TestParameters(string fileType, int repeats, int maxTime, int numberScenario, List<Scenario> scenarios = null)
        {
            iters = new[] { 50,100,150,200,250,300 };
            mutationtypes = new[] { "swap", "invert", "insert" };
            TabuSizes = new[] { 10,20,30,40,50 };
            

            if (fileType.Contains("C101"))
            {
                iters = new[] { 200 };
                mutationtypes = new[] { "swap" };
                TabuSizes = new[] {  50 };
            }
            iters = new[] { 200 };
            mutationtypes = new[] { "swap" };
            TabuSizes = new[] { 50 };

            if (scenarios == null)
                scenarios = InstanceGenerator.GenerateManyScenarios(numberScenario, "pliki//100 lokacji//" + fileType + ".txt"); Stopwatch sw = Stopwatch.StartNew();
            List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, TabuSizes, mutationtypes, fileType, 
                repeats: repeats, baseSeed: 42, parallel: false,maxTime:maxTime,defaultFilePath:"results_raw_parameter_tuning_");
            Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
        }
    }
    public class TestParametersBee
    {
        int[] FoodSourcesCounts { get; set; }
        int[] Limits { get; set; }
        public string[] mutationtypes { get; set; }
        public TestParametersBee(string fileType, int repeats, int maxTime, int numberScenario, List<Scenario> scenarios = null)
        {
            
            FoodSourcesCounts = new[] { 50, 100, 200 };
            mutationtypes = new[] { "swap", "2opt", "oropt", "rand" };
            Limits = new[] { 150, 300 };
            if (fileType.Contains("C101"))
            {
                FoodSourcesCounts = new[] { 50 };
                mutationtypes = new[] { "swap" };
                Limits = new[] { 300 };
            }
            FoodSourcesCounts = new[] { 50 };
            mutationtypes = new[] { "swap" };
            Limits = new[] { 300 };
            if (scenarios==null)
                scenarios = InstanceGenerator.GenerateManyScenarios(numberScenario, "pliki//100 lokacji//" + fileType + ".txt");
            Stopwatch sw = Stopwatch.StartNew();
            List<ExperimentResultBee> rawResults = ExperimentRunner.RunExperimentsBee(scenarios, FoodSourcesCounts, Limits, mutationtypes, fileType,
                repeats: repeats, baseSeed: 42, parallel: false, maxTime: maxTime, defaultFilePath: "results_raw_bee_parameter_tuning_");
            Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
        }
    }



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
            var preparedInstance = new Instance(filename, 10, true, true,
                waitingFactor: 1.0, distanceFactor: 1.0, penaltyFactor: 2.0,
                toEarlyPenaltyFactor: 1.0, toLatePenaltyFactor: 2.0, rng
                );

            return new Scenario(scenarioId, preparedInstance);
        }

        public static List<Scenario> GenerateManyScenarios(int numScenariosPerFile, string filename)
        {
            var rng = new Random(1);
            var scenarios = new List<Scenario>();
            for (int i = 0; i < numScenariosPerFile; i++)
                scenarios.Add(GenerateInstance(i, rng, filename));
            return scenarios;
        }
    }
}
