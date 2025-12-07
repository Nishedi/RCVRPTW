using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RCVRPTW
{
    /// <summary>
    /// Simple Q-learning agent for selecting mutation operators (swap, insert, invert)
    /// in the Tabu Search algorithm. The agent learns which operator works best
    /// in different states of the search process.
    /// </summary>
    public class RLOperatorSelector
    {
        // Q-table: maps state-action pairs to Q-values
        private Dictionary<(int, int), double> qTable;
        
        // Available actions (operators)
        private readonly string[] operators = { "swap", "insert", "invert" };
        
        // RL hyperparameters
        private double learningRate;      // Alpha: how much to update Q-values
        private double discountFactor;    // Gamma: importance of future rewards
        private double epsilon;           // Exploration rate
        private double epsilonDecay;      // How fast epsilon decreases
        private double epsilonMin;        // Minimum epsilon value
        
        // Statistics for tracking
        private int[] operatorSelectionCount;
        private double[] operatorRewardSum;
        private Random random;
        
        /// <summary>
        /// Initialize the RL agent with specified hyperparameters
        /// </summary>
        public RLOperatorSelector(
            double learningRate = 0.1,
            double discountFactor = 0.9,
            double epsilon = 1.0,
            double epsilonDecay = 0.995,
            double epsilonMin = 0.01,
            int? seed = null)
        {
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
            this.epsilon = epsilon;
            this.epsilonDecay = epsilonDecay;
            this.epsilonMin = epsilonMin;
            
            qTable = new Dictionary<(int, int), double>();
            operatorSelectionCount = new int[operators.Length];
            operatorRewardSum = new double[operators.Length];
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        
        /// <summary>
        /// Get state representation based on current search progress
        /// State is discretized into bins based on:
        /// - Improvement rate (how much we're improving)
        /// - Progress through search (early, middle, late stage)
        /// </summary>
        private int GetState(double currentObjective, double bestObjective, int iteration, int maxIterations)
        {
            // Calculate relative improvement
            double improvement = (bestObjective - currentObjective) / Math.Max(Math.Abs(bestObjective), 1.0);
            
            // Discretize improvement into 5 bins
            int improvementBin = 0;
            if (improvement > 0.1) improvementBin = 0;      // Large improvement
            else if (improvement > 0.01) improvementBin = 1; // Medium improvement
            else if (improvement > 0.0) improvementBin = 2;  // Small improvement
            else if (improvement > -0.01) improvementBin = 3; // Small degradation
            else improvementBin = 4;                          // Large degradation
            
            // Discretize progress into 3 stages
            double progress = (double)iteration / Math.Max(maxIterations, 1);
            int progressBin = 0;
            if (progress < 0.33) progressBin = 0;      // Early stage
            else if (progress < 0.66) progressBin = 1; // Middle stage
            else progressBin = 2;                       // Late stage
            
            // Combine bins into a single state (5 * 3 = 15 possible states)
            return improvementBin * 3 + progressBin;
        }
        
        /// <summary>
        /// Select an operator using epsilon-greedy policy
        /// </summary>
        public string SelectOperator(double currentObjective, double bestObjective, int iteration, int maxIterations)
        {
            int state = GetState(currentObjective, bestObjective, iteration, maxIterations);
            int action;
            
            // Epsilon-greedy action selection
            if (random.NextDouble() < epsilon)
            {
                // Explore: random action
                action = random.Next(operators.Length);
            }
            else
            {
                // Exploit: choose best action based on Q-values
                action = GetBestAction(state);
            }
            
            operatorSelectionCount[action]++;
            return operators[action];
        }
        
        /// <summary>
        /// Get the best action for a given state based on Q-values
        /// </summary>
        private int GetBestAction(int state)
        {
            double maxQ = double.MinValue;
            int bestAction = 0;
            
            for (int action = 0; action < operators.Length; action++)
            {
                double q = GetQValue(state, action);
                if (q > maxQ)
                {
                    maxQ = q;
                    bestAction = action;
                }
            }
            
            return bestAction;
        }
        
        /// <summary>
        /// Get Q-value for state-action pair, initializing if needed
        /// </summary>
        private double GetQValue(int state, int action)
        {
            var key = (state, action);
            if (!qTable.ContainsKey(key))
            {
                qTable[key] = 0.0; // Initialize to 0
            }
            return qTable[key];
        }
        
        /// <summary>
        /// Update Q-value based on observed reward
        /// Uses Q-learning update rule: Q(s,a) = Q(s,a) + α[r + γ max Q(s',a') - Q(s,a)]
        /// </summary>
        public void UpdateQValue(
            double previousObjective,
            double currentObjective,
            double bestObjective,
            int previousIteration,
            int currentIteration,
            int maxIterations,
            string selectedOperator)
        {
            int previousState = GetState(previousObjective, bestObjective, previousIteration, maxIterations);
            int currentState = GetState(currentObjective, bestObjective, currentIteration, maxIterations);
            int action = Array.IndexOf(operators, selectedOperator);
            
            if (action < 0) return; // Invalid operator
            
            // Calculate reward based on objective improvement
            double reward = CalculateReward(previousObjective, currentObjective);
            
            // Get current Q-value
            double currentQ = GetQValue(previousState, action);
            
            // Get max Q-value for next state
            double maxNextQ = GetQValue(currentState, 0);
            for (int a = 1; a < operators.Length; a++)
            {
                double q = GetQValue(currentState, a);
                if (q > maxNextQ) maxNextQ = q;
            }
            
            // Q-learning update
            double newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
            qTable[(previousState, action)] = newQ;
            
            // Track statistics
            operatorRewardSum[action] += reward;
            
            // Decay epsilon (reduce exploration over time)
            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
        }
        
        /// <summary>
        /// Calculate reward based on objective improvement
        /// Positive reward for improvement, negative for degradation
        /// </summary>
        private double CalculateReward(double previousObjective, double currentObjective)
        {
            double improvement = previousObjective - currentObjective;
            double relativeImprovement = improvement / Math.Max(Math.Abs(previousObjective), 1.0);
            
            // Scale reward to be in reasonable range
            return relativeImprovement * 100.0;
        }
        
        /// <summary>
        /// Get statistics about operator usage and performance
        /// </summary>
        public string GetStatistics()
        {
            var stats = "\n=== RL Operator Selector Statistics ===\n";
            stats += $"Current epsilon (exploration rate): {epsilon:F4}\n";
            stats += $"Q-table size: {qTable.Count} state-action pairs\n\n";
            
            stats += "Operator Usage and Average Reward:\n";
            for (int i = 0; i < operators.Length; i++)
            {
                double avgReward = operatorSelectionCount[i] > 0 
                    ? operatorRewardSum[i] / operatorSelectionCount[i] 
                    : 0.0;
                stats += $"  {operators[i],-10}: selected {operatorSelectionCount[i],5} times, " +
                        $"avg reward: {avgReward,8:F4}\n";
            }
            
            return stats;
        }
        
        /// <summary>
        /// Reset statistics (but keep learned Q-values)
        /// </summary>
        public void ResetStatistics()
        {
            operatorSelectionCount = new int[operators.Length];
            operatorRewardSum = new double[operators.Length];
        }
        
        /// <summary>
        /// Get the current epsilon value
        /// </summary>
        public double GetEpsilon()
        {
            return epsilon;
        }
        
        /// <summary>
        /// Save Q-table to a string representation (for debugging/analysis)
        /// </summary>
        public string ExportQTable()
        {
            var output = "State,Action,QValue\n";
            foreach (var kvp in qTable.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
            {
                output += $"{kvp.Key.Item1},{operators[kvp.Key.Item2]},{kvp.Value:F6}\n";
            }
            return output;
        }
        
        /// <summary>
        /// Save the trained RL model to a JSON file
        /// </summary>
        public void SaveModel(string filePath)
        {
            var modelData = new RLModelData
            {
                LearningRate = learningRate,
                DiscountFactor = discountFactor,
                Epsilon = epsilon,
                EpsilonDecay = epsilonDecay,
                EpsilonMin = epsilonMin,
                QTable = qTable.ToDictionary(
                    kvp => $"{kvp.Key.Item1},{kvp.Key.Item2}",
                    kvp => kvp.Value
                ),
                OperatorSelectionCount = operatorSelectionCount,
                OperatorRewardSum = operatorRewardSum
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string jsonString = JsonSerializer.Serialize(modelData, options);
            File.WriteAllText(filePath, jsonString);
            Console.WriteLine($"RL model saved to: {filePath}");
        }
        
        /// <summary>
        /// Load a trained RL model from a JSON file
        /// </summary>
        public static RLOperatorSelector LoadModel(string filePath, int? seed = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Model file not found: {filePath}");
            }
            
            string jsonString = File.ReadAllText(filePath);
            var modelData = JsonSerializer.Deserialize<RLModelData>(jsonString);
            
            if (modelData == null)
            {
                throw new InvalidDataException("Failed to deserialize model data");
            }
            
            var agent = new RLOperatorSelector(
                learningRate: modelData.LearningRate,
                discountFactor: modelData.DiscountFactor,
                epsilon: modelData.Epsilon,
                epsilonDecay: modelData.EpsilonDecay,
                epsilonMin: modelData.EpsilonMin,
                seed: seed
            );
            
            // Load Q-table
            agent.qTable = modelData.QTable.ToDictionary(
                kvp => {
                    var parts = kvp.Key.Split(',');
                    return (int.Parse(parts[0]), int.Parse(parts[1]));
                },
                kvp => kvp.Value
            );
            
            // Load statistics
            agent.operatorSelectionCount = modelData.OperatorSelectionCount;
            agent.operatorRewardSum = modelData.OperatorRewardSum;
            
            Console.WriteLine($"RL model loaded from: {filePath}");
            Console.WriteLine($"Q-table size: {agent.qTable.Count} state-action pairs");
            Console.WriteLine($"Epsilon: {agent.epsilon:F4}");
            
            return agent;
        }
    }
    
    /// <summary>
    /// Data structure for serializing/deserializing RL model
    /// </summary>
    public class RLModelData
    {
        public double LearningRate { get; set; }
        public double DiscountFactor { get; set; }
        public double Epsilon { get; set; }
        public double EpsilonDecay { get; set; }
        public double EpsilonMin { get; set; }
        public Dictionary<string, double> QTable { get; set; } = new Dictionary<string, double>();
        public int[] OperatorSelectionCount { get; set; } = new int[3];
        public double[] OperatorRewardSum { get; set; } = new double[3];
    }
}
