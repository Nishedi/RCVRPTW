using System;
using System.Collections.Generic;

namespace RCVRPTW
{
    /// <summary>
    /// Example demonstrating how to use the RL solver for VRPTW.
    /// This class can be called from Program.cs to run RL experiments.
    /// </summary>
    public static class RLExample
    {
        /// <summary>
        /// Run a simple RL experiment on a single instance
        /// </summary>
        public static void RunSingleInstanceExample()
        {
            Console.WriteLine("=== RL Solver Example: Single Instance ===\n");
            
            // Load a problem instance
            string filename = System.IO.Path.Combine("pliki", "100 lokacji", "C101.txt");
            Instance instance = new Instance(filename, vehicleNumbers: 100);
            
            Console.WriteLine($"Loaded instance: {filename}");
            Console.WriteLine($"Number of locations: {instance.Locations.Count}");
            Console.WriteLine($"Number of vehicles: {instance.Vehicles.Count}\n");
            
            // 1. Greedy baseline
            Console.WriteLine("--- Running Greedy Algorithm ---");
            var greedySolution = GreedyApproaches.generateGreedySolution(instance);
            greedySolution.calculateRoutesMetrics(instance);
            double greedyObjective = greedySolution.TotalCost + greedySolution.TotalPenalty + greedySolution.TotalVehicleOperationTime;
            Console.WriteLine($"Greedy Objective: {greedyObjective:F2}");
            Console.WriteLine($"  Cost: {greedySolution.TotalCost:F2}, Penalty: {greedySolution.TotalPenalty:F2}, VOT: {greedySolution.TotalVehicleOperationTime:F2}");
            Console.WriteLine($"  Routes: {greedySolution.Routes.Count}\n");
            
            // 2. RL Solver
            Console.WriteLine("--- Running RL Solver ---");
            var rlSolution = RLSolver.RunWithDefaults(instance, seed: 42);
            double rlObjective = rlSolution.TotalCost + rlSolution.TotalPenalty + rlSolution.TotalVehicleOperationTime;
            Console.WriteLine($"RL Objective: {rlObjective:F2}");
            Console.WriteLine($"  Cost: {rlSolution.TotalCost:F2}, Penalty: {rlSolution.TotalPenalty:F2}, VOT: {rlSolution.TotalVehicleOperationTime:F2}");
            Console.WriteLine($"  Routes: {rlSolution.Routes.Count}\n");
            
            // 3. Comparison
            Console.WriteLine("--- Comparison ---");
            double improvement = ((greedyObjective - rlObjective) / greedyObjective) * 100;
            Console.WriteLine($"RL vs Greedy improvement: {improvement:F2}%");
            
            if (rlObjective < greedyObjective)
            {
                Console.WriteLine("✓ RL found a better solution than Greedy!");
            }
            else
            {
                Console.WriteLine("✗ Greedy found a better solution than RL");
                Console.WriteLine("  Note: RL may need more training episodes or better hyperparameters");
            }
        }
        
        /// <summary>
        /// Run RL experiments with different training episode counts
        /// </summary>
        public static void RunTrainingComparisonExample()
        {
            Console.WriteLine("=== RL Solver Example: Training Episode Comparison ===\n");
            
            string filename = System.IO.Path.Combine("pliki", "100 lokacji", "C101.txt");
            Instance instance = new Instance(filename, vehicleNumbers: 100);
            
            Console.WriteLine($"Testing different training episode counts on {filename}\n");
            
            int[] episodeCounts = new int[] { 10, 25, 50, 100 };
            
            foreach (int episodes in episodeCounts)
            {
                Console.WriteLine($"--- Training with {episodes} episodes ---");
                var solution = RLSolver.Run(
                    instance: instance,
                    trainingEpisodes: episodes,
                    learningRate: 0.15,
                    discountFactor: 0.9,
                    epsilon: 1.0,
                    epsilonDecay: 0.99,
                    epsilonMin: 0.05,
                    seed: 42
                );
                
                double objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime;
                Console.WriteLine($"Objective: {objective:F2} (Cost: {solution.TotalCost:F2}, Penalty: {solution.TotalPenalty:F2}, VOT: {solution.TotalVehicleOperationTime:F2})");
                Console.WriteLine($"Routes: {solution.Routes.Count}\n");
            }
        }
        
        /// <summary>
        /// Run RL experiments on multiple instance types
        /// </summary>
        public static void RunMultiInstanceExample()
        {
            Console.WriteLine("=== RL Solver Example: Multiple Instances ===\n");
            
            string[] instances = new string[] { 
                System.IO.Path.Combine("pliki", "100 lokacji", "C101.txt"),
                System.IO.Path.Combine("pliki", "100 lokacji", "R101.txt"),
                System.IO.Path.Combine("pliki", "100 lokacji", "RC101.txt")
            };
            
            foreach (string filename in instances)
            {
                Console.WriteLine($"--- Instance: {System.IO.Path.GetFileName(filename)} ---");
                Instance instance = new Instance(filename, vehicleNumbers: 100);
                
                // Greedy baseline
                var greedySolution = GreedyApproaches.generateGreedySolution(instance);
                greedySolution.calculateRoutesMetrics(instance);
                double greedyObj = greedySolution.TotalCost + greedySolution.TotalPenalty + greedySolution.TotalVehicleOperationTime;
                
                // RL solution
                var rlSolution = RLSolver.RunWithDefaults(instance, seed: 42);
                double rlObj = rlSolution.TotalCost + rlSolution.TotalPenalty + rlSolution.TotalVehicleOperationTime;
                
                Console.WriteLine($"Greedy: {greedyObj:F2} | RL: {rlObj:F2} | Improvement: {((greedyObj - rlObj) / greedyObj * 100):F2}%\n");
            }
        }
        
        /// <summary>
        /// Run full RL experiments on stochastic scenarios
        /// This demonstrates the ExperimentRunner.RunRLExperiments method
        /// </summary>
        public static void RunStochasticScenarioExperiments()
        {
            Console.WriteLine("=== RL Solver Example: Stochastic Scenarios ===\n");
            
            string fileType = "C101";
            int numberScenarios = 10; // Small number for demonstration
            
            Console.WriteLine($"Generating {numberScenarios} stochastic scenarios for {fileType}...\n");
            
            string instancePath = System.IO.Path.Combine("pliki", "100 lokacji", fileType + ".txt");
            List<Scenario> scenarios = InstanceGenerator.GenerateManyScenarios(
                numberScenarios, 
                instancePath
            );
            
            Console.WriteLine("Running RL experiments on scenarios...\n");
            
            int[] trainingEpisodes = new int[] { 50 };
            
            var results = ExperimentRunner.RunRLExperiments(
                scenarios: scenarios,
                trainingEpisodesGrid: trainingEpisodes,
                fileType: fileType,
                repeats: 1,
                baseSeed: 42,
                parallel: false,
                defaultFilePath: "results_rl_demo_"
            );
            
            Console.WriteLine($"\nCompleted {results.Count} RL experiments");
            Console.WriteLine($"Results saved to: results_rl_demo_{fileType}.csv");
            
            // Calculate statistics
            double avgObjective = 0;
            double avgGreedyObjective = 0;
            foreach (var result in results)
            {
                avgObjective += result.Objective;
                avgGreedyObjective += result.GreedyObjective;
            }
            avgObjective /= results.Count;
            avgGreedyObjective /= results.Count;
            
            Console.WriteLine($"\nAverage Greedy Objective: {avgGreedyObjective:F2}");
            Console.WriteLine($"Average RL Objective: {avgObjective:F2}");
            Console.WriteLine($"Improvement: {((avgGreedyObjective - avgObjective) / avgGreedyObjective * 100):F2}%");
        }
    }
}
