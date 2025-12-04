// See https://aka.ms/new-console-template for more information

using Microsoft.VisualBasic.FileIO;
using RCVRPTW;
using System.Diagnostics;


public static class Program
{

    static int[] iters = new[] { /*100, 500, 2000*/200 };
    static int[] tabu = new[] { 50 };
    static int numberScenarios = 500;
    static string[] fileNames = new[] { "pliki//100 lokacji//C101.txt",
        "pliki//100 lokacji//C201.txt", "pliki//100 lokacji//R101.txt", "pliki//100 lokacji//R201.txt",
        "pliki//100 lokacji//RC101.txt", "pliki//100 lokacji//RC201.txt"
    };
    static string[] mutationtypes = new[] { "swap"/*, "invert", "insert"*/ };
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            int maxTime = 1;
            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out int parsedMaxTime))
                {
                    maxTime = parsedMaxTime;
                }
            }
            string fileType = args[0];
            Console.WriteLine($"Running experiments for file type: {fileType}, maxTime of scenario: {maxTime}");
            List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(numberScenarios, "pliki//100 lokacji//" + fileType + ".txt");
            Stopwatch sw = Stopwatch.StartNew();
            
            List<ExperimentResult> rawResults = ExperimentRunner.RunExperiments(scenarios, iters, tabu, mutationtypes, fileType, repeats: 1, baseSeed: 42, parallel: false,maxTime:maxTime);
            Console.WriteLine($"\nAll experiments completed in {sw.Elapsed.TotalSeconds} seconds.");
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
                toEarlyPenaltyFactor: 1.0, toLatePenaltyFactor: 2.0
                );

            return new Scenario(scenarioId, preparedInstance);
        }

        public static List<Scenario> GenerateManyScenarios(int numScenariosPerFile, string filename)
        {
            var rng = new Random();
            var scenarios = new List<Scenario>();
            for (int i = 0; i < numScenariosPerFile; i++)
                scenarios.Add(GenerateInstance(i, rng, filename));
            return scenarios;
        }
    }
}
