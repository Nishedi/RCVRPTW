using System;
using System.Diagnostics;

namespace RCVRPTW
{
    /// <summary>
    /// Reinforcement Learning solver for RCVRPTW using Q-Learning approach.
    /// This class provides a unified interface for training and solving
    /// instances using reinforcement learning techniques.
    /// </summary>
    internal class RLSolver
    {
        // Configuration constants
        private const int MAX_REASONABLE_EPISODES = 10000;
        /// <summary>
        /// Run Q-Learning based solution with training and inference phases
        /// </summary>
        /// <param name="instance">Problem instance to solve</param>
        /// <param name="trainingEpisodes">Number of training episodes</param>
        /// <param name="learningRate">Learning rate for Q-Learning</param>
        /// <param name="discountFactor">Discount factor for future rewards</param>
        /// <param name="epsilon">Initial exploration rate</param>
        /// <param name="epsilonDecay">Epsilon decay rate per episode</param>
        /// <param name="epsilonMin">Minimum epsilon value</param>
        /// <param name="seed">Random seed for reproducibility</param>
        /// <returns>Best solution found</returns>
        public static Solution Run(Instance instance, 
                                   int trainingEpisodes = 500, 
                                   double learningRate = 0.08,
                                   double discountFactor = 0.97,
                                   double epsilon = 1.0,
                                   double epsilonDecay = 0.995,
                                   double epsilonMin = 0.01,
                                   int seed = 42)
        {
            // Validate episode count to prevent unreasonable values
            if (trainingEpisodes > MAX_REASONABLE_EPISODES)
            {
                Console.WriteLine($"WARNING: Training episodes ({trainingEpisodes}) is very high and may take excessive time.");
                Console.WriteLine($"Recommended range: 100-1000 episodes. Consider reducing if not intentional.");
            }
            
            Console.WriteLine($"Starting RL Solver with {trainingEpisodes} training episodes");
            Console.WriteLine($"Parameters: LR={learningRate}, Gamma={discountFactor}, Epsilon={epsilon}->{epsilonMin}");
            
            var stopwatch = Stopwatch.StartNew();
            
            // Create and train agent
            QLearningAgent agent = new QLearningAgent(
                learningRate: learningRate,
                discountFactor: discountFactor,
                epsilon: epsilon,
                epsilonDecay: epsilonDecay,
                epsilonMin: epsilonMin,
                seed: seed
            );
            
            agent.Train(instance, episodes: trainingEpisodes);
            
            // Generate solution using trained agent
            Solution solution = agent.Solve(instance);
            solution.calculateRoutesMetrics(instance);
            
            stopwatch.Stop();
            
            var objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime;
            Console.WriteLine($"RL Solver completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Solution objective: {Math.Round(objective)}");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n");
            
            return solution;
        }

        /// <summary>
        /// Run RL solver with default parameters optimized for the problem
        /// </summary>
        public static Solution RunWithDefaults(Instance instance, int seed = 42)
        {
            return Run(
                instance: instance,
                trainingEpisodes: 300,
                learningRate: 0.1,
                discountFactor: 0.95,
                epsilon: 1.0,
                epsilonDecay: 0.995,
                epsilonMin: 0.05,
                seed: seed
            );
        }
    }
}
