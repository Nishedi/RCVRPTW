// See https://aka.ms/new-console-template for more information

using Microsoft.VisualBasic.FileIO;
using RCVRPTW;
using System.Diagnostics;
using System.IO;


public static class Program
{
    static int[] iters = new[] { /*100, 500, 2000*/200 };
    static int[] tabu = new[] { 50 };
    static int numberScenarios = 10;
    static string[] fileNames = new[] { "pliki//100 lokacji//C101.txt",
        "pliki//100 lokacji//C201.txt", "pliki//100 lokacji//R101.txt", "pliki//100 lokacji//R201.txt",
        "pliki//100 lokacji//RC101.txt", "pliki//100 lokacji//RC201.txt"
    };
    static string[] mutationtypes = new[] { "swap", "invert", "insert", "2opt", "oropt", "rand" };
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            int maxTime = 1;
            bool useRL = false;
            string? rlModelPath = null;
            bool trainMode = false;
            bool both = false;
            bool both2 = false;
            string[] results = { };

            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out int parsedMaxTime))
                {
                    maxTime = parsedMaxTime;
                }
            }
            
            // Check if RL mode is requested or training mode
            if (args.Length > 2)
            {
                if (args[2].ToLower() == "rl")
                {
                    useRL = true;
                    Console.WriteLine("RL mode enabled - operators will be selected using reinforcement learning");
                }
                else if (args[2].ToLower() == "train")
                {
                    trainMode = true;
                    useRL = true;
                    Console.WriteLine("RL Training mode enabled - model will be trained and saved");
                }
                
                else if (args[2].ToLower().StartsWith("model:"))
                {
                    // Load pre-trained model
                    rlModelPath = args[2].Substring(6); // Remove "model:" prefix
                    useRL = true;
                    Console.WriteLine($"RL mode with pre-trained model: {rlModelPath}");
                }
            }
            if(args.Length > 3)
            {
                if (args[3].ToLower() == "both")
                {
                    both = true;
                    useRL = true;
                    Console.WriteLine("Both mode enabled - experiments will be run with and without RL for comparison");
                }
                if (args[3].ToLower() == "both2")
                {
                    both2 = true;
                    Console.WriteLine("Both mode enabled - experiments will be run with and without RL for comparison");
                }

            }

            string fileType = args[0];
            
            // Training mode - train and save model
            if (trainMode && !both)
            {
                Console.WriteLine($"Training RL model for file type: {fileType}, maxTime: {maxTime}s");
                List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(1, "pliki//100 lokacji//" + fileType + ".txt");
                
                if (scenarios.Count > 0)
                {
                    string modelDir = "models";
                    if (!Directory.Exists(modelDir))
                    {
                        Directory.CreateDirectory(modelDir);
                    }
                    
                    string modelPath = Path.Combine(modelDir, $"rl_model_{fileType}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    var trainedAgent = TabuSearch.TrainAndSaveRLModel(iters[0], tabu[0], scenarios[0].Instance, maxTime, modelPath, seed: 42);
                    Console.WriteLine($"\nModel training completed and saved to: {modelPath}");
                    Console.WriteLine("You can now use this model with: dotnet run <fileType> <maxTime> model:" + modelPath);
                }
            }
            else if (both)
            {
                // Run experiments with and without RL
                Console.WriteLine($"Running experiments for file type: {fileType}, maxTime of scenario: {maxTime}s, both RL and non-RL");
                List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt");
                Stopwatch sw = Stopwatch.StartNew();
                
                Console.WriteLine("Running experiments WITHOUT RL:");
                List<ExperimentResult> rawResultsNonRL = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: false);
                
                Console.WriteLine("\nRunning experiments WITH RL:");
                List<ExperimentResult> rawResultsRL = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: true, rlModelPath: rlModelPath);
                
                Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
            }
            else if (both2)
            {
                Console.WriteLine($"Running experiments for file type: {fileType}, maxTime of scenario: {maxTime}s, both RL and non-RL");
                List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt");
                Stopwatch sw = Stopwatch.StartNew();

                Console.WriteLine("Running experiments WITHOUT RL:");
                List<ExperimentResult> rawResultsNonRL = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: false, defaultFilePath: $"results_raw_no_RL_{fileType}_");

                mutationtypes = new[] { "rl_operator" };
                Console.WriteLine("\nRunning experiments WITH RL:");
                List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: useRL, rlModelPath: rlModelPath, defaultFilePath: $"results_raw_RL_{fileType}_");
                Console.WriteLine("\nRunning experiments WITH RL epochs:");
                //List<ExperimentResult> rawResultsv2 = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: useRL, rlModelPath: rlModelPath, epochs: true);
                //Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");

                //Console.WriteLine("Results for non RL operator");
                //foreach (var res in rawResultsNonRL)
                //{
                //    Console.WriteLine($"{rawResultsNonRL.IndexOf(res)}: GREEDY={res.GreedyObjective} Tabu={res.Objective} MutationType={res.MutationType}");
                //}


                //foreach (var res in rawResults)
                //{
                //    Console.WriteLine($"{rawResults.IndexOf(res)}: GREEDY={res.GreedyObjective} Tabu={res.Objective} MutationType=RL");
                //}
                //foreach (var res in rawResultsv2)
                //{
                //    Console.WriteLine($"{rawResultsv2.IndexOf(res)}: GREEDY={res.GreedyObjective} Tabu={res.Objective} MutationType=RL epochs");
                //}

            }
            else
            {
                // Regular experiment mode
                Console.WriteLine($"Running experiments for file type: {fileType}, maxTime of scenario: {maxTime}s, RL: {useRL}");
                List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt");
                Stopwatch sw = Stopwatch.StartNew();

                List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false, maxTime: maxTime, useRL: useRL, rlModelPath: rlModelPath);
                Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
            }
            //Console.WriteLine($"Running parameter tuning for file type: {fileType}");
            //int repeats = 3;
            //int maxTime = 300;
            //if(args.Length > 1) {                 
            //    if(int.TryParse(args[1], out int parsedRepeats))
            //    {
            //        repeats = parsedRepeats;
            //    }
            //}
            //if(args.Length > 2) {                 
            //    if(int.TryParse(args[2], out int parsedMaxTime))
            //    {
            //        maxTime = parsedMaxTime;
            //    }
            //}

            //TestParameters testParameters = new TestParameters(fileType,repeats,maxTime);
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
        public TestParameters(string fileType, int repeats, int maxTime)
        {
            iters = new[] { 50,100,150,200,250,300 };
            mutationtypes = new[] { "swap", "invert", "insert" };
            TabuSizes = new[] { 10,20,30,40,50 };
            var  numberScenarios = 1;

            List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt");
            Stopwatch sw = Stopwatch.StartNew();
            List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, TabuSizes, mutationtypes, fileType, 
                repeats: repeats, baseSeed: 42, parallel: false,maxTime:maxTime,defaultFilePath:"results_raw_parameter_tuning_");
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
            var preparedInstance = new Instance(filename, 100, true, true,
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
