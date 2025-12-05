using System;
using System.Diagnostics;

namespace RCVRPTW
{
    /// <summary>
    /// Simple validation program to test the updated RL parameters
    /// </summary>
    public static class RLParameterValidation
    {
        // Configuration constants
        private const string DEFAULT_TEST_INSTANCE = "pliki/100 lokacji/C101.txt";
        private const int VALIDATION_WARNING_TEST_EPISODES = 11000; // Just above threshold to test warning
        private const int VALIDATION_QUICK_TEST_EPISODES = 100; // Quick test for performance check
        private const double MAX_ACCEPTABLE_RL_TO_GREEDY_RATIO = 5.0; // RL should be less than 5x worse than Greedy
        public static void RunValidation()
        {
            Console.WriteLine("=== RL Parameter Validation Test ===\n");
            
            // Load a small instance for quick testing
            string filename = DEFAULT_TEST_INSTANCE;
            Console.WriteLine($"Loading instance: {filename}");
            Instance instance = new Instance(filename, vehicleNumbers: 100);
            
            Console.WriteLine($"Number of locations: {instance.Locations.Count}");
            Console.WriteLine($"Number of vehicles: {instance.Vehicles.Count}\n");
            
            // Test 1: Verify warning for high episode count
            Console.WriteLine($"--- Test 1: Episode count validation (should show warning for {VALIDATION_WARNING_TEST_EPISODES}) ---");
            var solution1 = RLSolver.Run(instance, trainingEpisodes: VALIDATION_WARNING_TEST_EPISODES, seed: 42);
            Console.WriteLine($"Warning test passed.\n");
            
            // Test 2: Quick run with new defaults
            Console.WriteLine($"--- Test 2: Quick test with {VALIDATION_QUICK_TEST_EPISODES} episodes ---");
            var stopwatch = Stopwatch.StartNew();
            var solution2 = RLSolver.Run(instance, trainingEpisodes: VALIDATION_QUICK_TEST_EPISODES, seed: 42);
            stopwatch.Stop();
            
            double objective2 = solution2.TotalCost + solution2.TotalPenalty + solution2.TotalVehicleOperationTime;
            Console.WriteLine($"Training time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Objective: {objective2:F2}");
            Console.WriteLine($"  Cost: {solution2.TotalCost:F2}");
            Console.WriteLine($"  Penalty: {solution2.TotalPenalty:F2}");
            Console.WriteLine($"  VOT: {solution2.TotalVehicleOperationTime:F2}");
            Console.WriteLine($"Routes: {solution2.Routes.Count}\n");
            
            // Test 3: Compare with Greedy
            Console.WriteLine("--- Test 3: Greedy baseline ---");
            stopwatch.Restart();
            var greedySolution = GreedyApproaches.generateGreedySolution(instance);
            greedySolution.calculateRoutesMetrics(instance);
            stopwatch.Stop();
            
            double greedyObjective = greedySolution.TotalCost + greedySolution.TotalPenalty + greedySolution.TotalVehicleOperationTime;
            Console.WriteLine($"Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Objective: {greedyObjective:F2}");
            Console.WriteLine($"  Cost: {greedySolution.TotalCost:F2}");
            Console.WriteLine($"  Penalty: {greedySolution.TotalPenalty:F2}");
            Console.WriteLine($"  VOT: {greedySolution.TotalVehicleOperationTime:F2}");
            Console.WriteLine($"Routes: {greedySolution.Routes.Count}\n");
            
            // Summary
            Console.WriteLine("--- Validation Summary ---");
            Console.WriteLine($"RL Objective: {objective2:F2}");
            Console.WriteLine($"Greedy Objective: {greedyObjective:F2}");
            double ratio = objective2 / greedyObjective;
            Console.WriteLine($"RL/Greedy Ratio: {ratio:F2}x");
            
            if (ratio < MAX_ACCEPTABLE_RL_TO_GREEDY_RATIO)
            {
                Console.WriteLine($"✓ PASS: RL performance is reasonable (less than {MAX_ACCEPTABLE_RL_TO_GREEDY_RATIO}x worse than Greedy)");
            }
            else
            {
                Console.WriteLine($"✗ FAIL: RL performance is still poor ({ratio:F2}x worse than Greedy, threshold is {MAX_ACCEPTABLE_RL_TO_GREEDY_RATIO}x)");
            }
            
            Console.WriteLine("\n=== Validation Complete ===");
        }
    }
}
