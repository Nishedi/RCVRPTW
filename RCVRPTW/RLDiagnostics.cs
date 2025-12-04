using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RCVRPTW
{
    /// <summary>
    /// Diagnostic utilities for analyzing RL performance and comparing with baseline methods.
    /// Helps identify issues with training, rewards, and solution quality.
    /// </summary>
    public static class RLDiagnostics
    {
        /// <summary>
        /// Run comprehensive RL diagnostics on a single instance
        /// </summary>
        public static void RunFullDiagnostics(string instancePath, int[] episodeCounts = null)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("         RL PERFORMANCE DIAGNOSTICS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Instance instance = new Instance(instancePath, vehicleNumbers: 100);
            Console.WriteLine($"Instance: {System.IO.Path.GetFileName(instancePath)}");
            Console.WriteLine($"Locations: {instance.Locations.Count}");
            Console.WriteLine($"Vehicles: {instance.Vehicles.Count}\n");

            // 1. Baseline Comparison
            Console.WriteLine("───────────────────────────────────────────────────────────");
            Console.WriteLine("1. BASELINE COMPARISON");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            
            var sw = Stopwatch.StartNew();
            var greedySolution = GreedyApproaches.generateGreedySolution(instance);
            greedySolution.calculateRoutesMetrics(instance);
            sw.Stop();
            
            double greedyObj = greedySolution.TotalCost + greedySolution.TotalPenalty + 
                              greedySolution.TotalVehicleOperationTime;
            
            Console.WriteLine($"Greedy Algorithm:");
            Console.WriteLine($"  Objective: {greedyObj:F2}");
            Console.WriteLine($"  Cost: {greedySolution.TotalCost:F2}");
            Console.WriteLine($"  Penalty: {greedySolution.TotalPenalty:F2}");
            Console.WriteLine($"  VOT: {greedySolution.TotalVehicleOperationTime:F2}");
            Console.WriteLine($"  Routes: {greedySolution.Routes.Count}");
            Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms\n");

            // 2. RL Performance with Different Episode Counts
            Console.WriteLine("───────────────────────────────────────────────────────────");
            Console.WriteLine("2. RL PERFORMANCE (DIFFERENT EPISODE COUNTS)");
            Console.WriteLine("───────────────────────────────────────────────────────────");

            episodeCounts = episodeCounts ?? new int[] { 10, 25, 50, 100 };
            
            var results = new List<(int episodes, double objective, double time, int qTableSize)>();

            foreach (int episodes in episodeCounts)
            {
                sw.Restart();
                
                // Create agent for tracking
                var agent = new QLearningAgent(
                    learningRate: 0.15,
                    discountFactor: 0.9,
                    epsilon: 1.0,
                    epsilonDecay: 0.99,
                    epsilonMin: 0.05,
                    seed: 42
                );
                
                agent.Train(instance, episodes: episodes);
                var solution = agent.Solve(instance);
                solution.calculateRoutesMetrics(instance);
                
                sw.Stop();
                
                double obj = solution.TotalCost + solution.TotalPenalty + 
                           solution.TotalVehicleOperationTime;
                
                results.Add((episodes, obj, sw.Elapsed.TotalSeconds, 0));
                
                Console.WriteLine($"\nEpisodes: {episodes}");
                Console.WriteLine($"  Objective: {obj:F2} ({((obj - greedyObj) / greedyObj * 100):+0.0;-0.0;0}% vs Greedy)");
                Console.WriteLine($"  Cost: {solution.TotalCost:F2}");
                Console.WriteLine($"  Penalty: {solution.TotalPenalty:F2}");
                Console.WriteLine($"  VOT: {solution.TotalVehicleOperationTime:F2}");
                Console.WriteLine($"  Routes: {solution.Routes.Count}");
                Console.WriteLine($"  Time: {sw.Elapsed.TotalSeconds:F2}s");
            }

            // 3. Summary and Recommendations
            Console.WriteLine("\n───────────────────────────────────────────────────────────");
            Console.WriteLine("3. SUMMARY AND RECOMMENDATIONS");
            Console.WriteLine("───────────────────────────────────────────────────────────");

            var bestRL = results.MinBy(r => r.objective);
            double improvement = ((greedyObj - bestRL.objective) / greedyObj) * 100;

            Console.WriteLine($"\nBest RL Configuration:");
            Console.WriteLine($"  Episodes: {bestRL.episodes}");
            Console.WriteLine($"  Objective: {bestRL.objective:F2}");
            Console.WriteLine($"  vs Greedy: {improvement:+0.0;-0.0;0}%");

            if (improvement > 5)
            {
                Console.WriteLine("\n✓ RL is outperforming Greedy significantly!");
                Console.WriteLine("  Consider: Increasing episodes further, fine-tuning hyperparameters");
            }
            else if (improvement > 0)
            {
                Console.WriteLine("\n⚠ RL is slightly better than Greedy");
                Console.WriteLine("  Consider: Improving state representation, adjusting reward weights");
            }
            else if (improvement > -10)
            {
                Console.WriteLine("\n⚠ RL is slightly worse than Greedy");
                Console.WriteLine("  Consider: More training episodes, better reward shaping");
            }
            else
            {
                Console.WriteLine("\n✗ RL is significantly worse than Greedy");
                Console.WriteLine("  Critical issues detected:");
                Console.WriteLine("  - Reward function may be imbalanced (check penalty weights)");
                Console.WriteLine("  - State representation may be insufficient");
                Console.WriteLine("  - Training parameters need adjustment");
                Console.WriteLine("\n  Recommended actions:");
                Console.WriteLine("  1. Reduce TIME_WINDOW_PENALTY_WEIGHT to 10-50");
                Console.WriteLine("  2. Reduce CAPACITY_PENALTY_WEIGHT to 100-500");
                Console.WriteLine("  3. Increase training episodes to 200-500");
                Console.WriteLine("  4. Review RL_PERFORMANCE_ANALYSIS.md for detailed recommendations");
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Quick comparison: Greedy vs RL with default settings
        /// </summary>
        public static void QuickComparison(string instancePath)
        {
            Console.WriteLine("Quick RL vs Greedy Comparison\n");
            
            Instance instance = new Instance(instancePath, vehicleNumbers: 100);
            
            // Greedy
            var greedy = GreedyApproaches.generateGreedySolution(instance);
            greedy.calculateRoutesMetrics(instance);
            double greedyObj = greedy.TotalCost + greedy.TotalPenalty + greedy.TotalVehicleOperationTime;
            
            // RL
            var rl = RLSolver.RunWithDefaults(instance, seed: 42);
            double rlObj = rl.TotalCost + rl.TotalPenalty + rl.TotalVehicleOperationTime;
            
            // Results
            Console.WriteLine($"Instance: {System.IO.Path.GetFileName(instancePath)}");
            Console.WriteLine($"Greedy:   {greedyObj:F2}");
            Console.WriteLine($"RL:       {rlObj:F2}");
            Console.WriteLine($"Diff:     {((rlObj - greedyObj) / greedyObj * 100):+0.0;-0.0;0}%");
            
            if (rlObj < greedyObj)
                Console.WriteLine("✓ RL is better!");
            else
                Console.WriteLine("✗ Greedy is better");
        }

        /// <summary>
        /// Observe training behavior and provide guidance on reward patterns.
        /// Note: Full reward distribution analysis requires extending QLearningAgent to track per-episode rewards.
        /// </summary>
        public static void ObserveTrainingBehavior(string instancePath, int episodes = 50)
        {
            Console.WriteLine("Observing Training Behavior\n");
            
            Instance instance = new Instance(instancePath, vehicleNumbers: 100);
            
            Console.WriteLine($"Training for {episodes} episodes...");
            Console.WriteLine("Watch the console output for:");
            Console.WriteLine("  - Rewards should gradually increase (become less negative)");
            Console.WriteLine("  - If rewards stay very negative (< -10000), penalties are too high");
            Console.WriteLine("  - If rewards don't improve, learning rate or state representation may be the issue\n");
            
            var solution = RLSolver.Run(instance, trainingEpisodes: episodes, seed: 42);
            
            Console.WriteLine("\nInterpretation Guide:");
            Console.WriteLine("  • Rewards around -100 to -1000: Reasonable, algorithm is learning");
            Console.WriteLine("  • Rewards around -10000 to -100000: Penalty weights are too high!");
            Console.WriteLine("  • Rewards not improving: Learning rate too high or state space too large");
        }

        /// <summary>
        /// Test multiple instances to identify patterns
        /// </summary>
        public static void MultiInstanceTest()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("         MULTI-INSTANCE RL TEST");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            string[] instances = new string[] {
                "pliki/100 lokacji/C101.txt",
                "pliki/100 lokacji/R101.txt",
                "pliki/100 lokacji/RC101.txt"
            };

            Console.WriteLine($"{"Instance",-15} {"Greedy",-12} {"RL",-12} {"Diff %",-10} {"Status",-10}");
            Console.WriteLine(new string('─', 65));

            foreach (string path in instances)
            {
                string name = System.IO.Path.GetFileName(path);
                Instance instance = new Instance(path, vehicleNumbers: 100);
                
                var greedy = GreedyApproaches.generateGreedySolution(instance);
                greedy.calculateRoutesMetrics(instance);
                double greedyObj = greedy.TotalCost + greedy.TotalPenalty + greedy.TotalVehicleOperationTime;
                
                var rl = RLSolver.RunWithDefaults(instance, seed: 42);
                double rlObj = rl.TotalCost + rl.TotalPenalty + rl.TotalVehicleOperationTime;
                
                double diff = ((rlObj - greedyObj) / greedyObj * 100);
                string status = diff < -5 ? "✓ Better" : diff > 5 ? "✗ Worse" : "≈ Similar";
                
                Console.WriteLine($"{name,-15} {greedyObj,-12:F2} {rlObj,-12:F2} {diff,-10:+0.0;-0.0} {status,-10}");
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Check if episode count seems incorrect (too high)
        /// </summary>
        public static void ValidateEpisodeCount(int episodes)
        {
            if (episodes > 10000)
            {
                Console.WriteLine("⚠ WARNING: Episode count seems too high!");
                Console.WriteLine($"   You specified: {episodes:N0} episodes");
                Console.WriteLine($"   Recommended: 50-500 episodes for typical instances");
                Console.WriteLine($"   This will take approximately {episodes * 0.009:F0} seconds");
                Console.WriteLine($"\n   Did you mean to use {episodes / 1000000}?");
            }
            else if (episodes > 1000)
            {
                Console.WriteLine($"⚠ Episode count is high: {episodes}");
                Console.WriteLine($"   Training will take approximately {episodes * 0.01:F0} seconds");
            }
        }
    }
}
