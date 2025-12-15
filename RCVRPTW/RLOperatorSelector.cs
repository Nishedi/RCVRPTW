//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Text.Json;

//namespace RCVRPTW
//{
//    /// <summary>
//    /// Simple Q-learning agent for selecting mutation operators (swap, insert, invert)
//    /// in the Tabu Search algorithm. The agent learns which operator works best
//    /// in different states of the search process.
//    /// </summary>
//    public class RLOperatorSelector
//    {
//        // Q-table: maps state-action pairs to Q-values
//        private Dictionary<(int, int), double> qTable;

//        // Available actions (operators)
//        private readonly string[] operators = { "swap", "insert", "invert", "2opt", "oropt", "cross" };

//        // RL hyperparameters
//        private double learningRate;      // Alpha: how much to update Q-values
//        private double discountFactor;    // Gamma: importance of future rewards
//        private double epsilon;           // Exploration rate
//        private double epsilonDecay;      // How fast epsilon decreases
//        private double epsilonMin;        // Minimum epsilon value

//        // Statistics for tracking
//        private int[] operatorSelectionCount;
//        private double[] operatorRewardSum;
//        private Random random;

//        // Metrics tracking for learning assessment
//        private List<IterationMetrics> iterationMetrics;
//        private string? metricsLogPath;
//        private bool trackMetrics;
//        private List<string> metricsBuffer;
//        private const int MetricsBufferSize = 50; // Flush every 50 iterations

//        /// <summary>
//        /// Initialize the RL agent with specified hyperparameters
//        /// </summary>
//        public RLOperatorSelector(
//            double learningRate = 0.1,
//            double discountFactor = 0.9,
//            double epsilon = 1.0,
//            double epsilonDecay = 0.995,
//            double epsilonMin = 0.01,
//            int? seed = null,
//            bool trackMetrics = false,
//            string? metricsLogPath = null)
//        {
//            this.learningRate = learningRate;
//            this.discountFactor = discountFactor;
//            this.epsilon = epsilon;
//            this.epsilonDecay = epsilonDecay;
//            this.epsilonMin = epsilonMin;
//            this.trackMetrics = trackMetrics;
//            this.metricsLogPath = metricsLogPath;

//            qTable = new Dictionary<(int, int), double>();
//            operatorSelectionCount = new int[operators.Length];
//            operatorRewardSum = new double[operators.Length];
//            random = seed.HasValue ? new Random(seed.Value) : new Random();
//            iterationMetrics = new List<IterationMetrics>();
//            metricsBuffer = new List<string>();

//            // Initialize metrics log file if tracking is enabled
//            if (trackMetrics && !string.IsNullOrEmpty(metricsLogPath))
//            {
//                InitializeMetricsLog();
//            }
//        }

//        /// <summary>
//        /// Get state representation based on current search progress
//        /// State is discretized into bins based on:
//        /// - Improvement rate (how much we're improving)
//        /// - Progress through search (early, middle, late stage)
//        /// </summary>
//        private int GetState(double currentObjective, double bestObjective, int iteration, int maxIterations)
//        {
//            // Calculate relative improvement
//            double improvement = (bestObjective - currentObjective) / Math.Max(Math.Abs(bestObjective), 1.0);

//            // Discretize improvement into 5 bins
//            int improvementBin = 0;
//            if (improvement > 0.1) improvementBin = 0;      // Large improvement
//            else if (improvement > 0.01) improvementBin = 1; // Medium improvement
//            else if (improvement > 0.0) improvementBin = 2;  // Small improvement
//            else if (improvement > -0.01) improvementBin = 3; // Small degradation
//            else improvementBin = 4;                          // Large degradation

//            // Discretize progress into 3 stages
//            double progress = (double)iteration / Math.Max(maxIterations, 1);
//            int progressBin = 0;
//            if (progress < 0.33) progressBin = 0;      // Early stage
//            else if (progress < 0.66) progressBin = 1; // Middle stage
//            else progressBin = 2;                       // Late stage

//            // Combine bins into a single state (5 * 3 = 15 possible states)
//            return improvementBin * 3 + progressBin;
//        }

//        /// <summary>
//        /// Select an operator using epsilon-greedy policy
//        /// </summary>
//        public string SelectOperator(double currentObjective, double bestObjective, int iteration, int maxIterations)
//        {
//            int state = GetState(currentObjective, bestObjective, iteration, maxIterations);
//            int action;

//            // Epsilon-greedy action selection
//            if (random.NextDouble() < epsilon)
//            {
//                // Explore: random action
//                action = random.Next(operators.Length);
//            }
//            else
//            {
//                // Exploit: choose best action based on Q-values
//                action = GetBestAction(state);
//            }

//            operatorSelectionCount[action]++;
//            return operators[action];
//        }

//        /// <summary>
//        /// Get the best action for a given state based on Q-values
//        /// </summary>
//        private int GetBestAction(int state)
//        {
//            double maxQ = double.MinValue;
//            int bestAction = 0;

//            for (int action = 0; action < operators.Length; action++)
//            {
//                double q = GetQValue(state, action);
//                if (q > maxQ)
//                {
//                    maxQ = q;
//                    bestAction = action;
//                }
//            }

//            return bestAction;
//        }

//        /// <summary>
//        /// Get Q-value for state-action pair, initializing if needed
//        /// </summary>
//        private double GetQValue(int state, int action)
//        {
//            var key = (state, action);
//            if (!qTable.ContainsKey(key))
//            {
//                qTable[key] = 0.0; // Initialize to 0
//            }
//            return qTable[key];
//        }

//        /// <summary>
//        /// Update Q-value based on observed reward
//        /// Uses Q-learning update rule: Q(s,a) = Q(s,a) + α[r + γ max Q(s',a') - Q(s,a)]
//        /// </summary>
//        public void UpdateQValue(
//            double previousObjective,
//            double currentObjective,
//            double bestObjective,
//            int previousIteration,
//            int currentIteration,
//            int maxIterations,
//            string selectedOperator)
//        {
//            int previousState = GetState(previousObjective, bestObjective, previousIteration, maxIterations);
//            int currentState = GetState(currentObjective, bestObjective, currentIteration, maxIterations);
//            int action = Array.IndexOf(operators, selectedOperator);

//            if (action < 0) return; // Invalid operator

//            // Calculate reward based on objective improvement
//            double reward = CalculateReward(previousObjective, currentObjective);

//            // Get current Q-value (before update)
//            double currentQ = GetQValue(previousState, action);

//            // Get max Q-value for next state
//            double maxNextQ = GetQValue(currentState, 0);
//            for (int a = 1; a < operators.Length; a++)
//            {
//                double q = GetQValue(currentState, a);
//                if (q > maxNextQ) maxNextQ = q;
//            }

//            // Q-learning update
//            double newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
//            qTable[(previousState, action)] = newQ;

//            // Log metrics if tracking is enabled
//            if (trackMetrics)
//            {
//                LogIterationMetrics(
//                    currentIteration,
//                    previousState,
//                    action,
//                    reward,
//                    currentQ,
//                    newQ,
//                    bestObjective,
//                    currentObjective
//                );
//            }

//            // Track statistics
//            operatorRewardSum[action] += reward;

//            // Decay epsilon (reduce exploration over time)
//            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
//        }

//        /// <summary>
//        /// Calculate reward based on objective improvement
//        /// Positive reward for improvement, negative for degradation
//        /// </summary>
//        private double CalculateReward(double previousObjective, double currentObjective)
//        {
//            double improvement = previousObjective - currentObjective;
//            double relativeImprovement = improvement / Math.Max(Math.Abs(previousObjective), 1.0);

//            // Scale reward to be in reasonable range
//            return relativeImprovement * 100.0;
//        }

//        /// <summary>
//        /// Get statistics about operator usage and performance
//        /// </summary>
//        public string GetStatistics()
//        {
//            var stats = "\n=== RL Operator Selector Statistics ===\n";
//            stats += $"Current epsilon (exploration rate): {epsilon:F4}\n";
//            stats += $"Q-table size: {qTable.Count} state-action pairs\n\n";

//            stats += "Operator Usage and Average Reward:\n";
//            for (int i = 0; i < operators.Length; i++)
//            {
//                double avgReward = operatorSelectionCount[i] > 0 
//                    ? operatorRewardSum[i] / operatorSelectionCount[i] 
//                    : 0.0;
//                stats += $"  {operators[i],-10}: selected {operatorSelectionCount[i],5} times, " +
//                        $"avg reward: {avgReward,8:F4}\n";
//            }

//            return stats;
//        }

//        /// <summary>
//        /// Reset statistics (but keep learned Q-values)
//        /// </summary>
//        public void ResetStatistics()
//        {
//            operatorSelectionCount = new int[operators.Length];
//            operatorRewardSum = new double[operators.Length];
//        }

//        /// <summary>
//        /// Get the current epsilon value
//        /// </summary>
//        public double GetEpsilon()
//        {
//            return epsilon;
//        }

//        /// <summary>
//        /// Save Q-table to a string representation (for debugging/analysis)
//        /// </summary>
//        public string ExportQTable()
//        {
//            var output = "State,Action,QValue\n";
//            foreach (var kvp in qTable.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
//            {
//                output += $"{kvp.Key.Item1},{operators[kvp.Key.Item2]},{kvp.Value:F6}\n";
//            }
//            return output;
//        }

//        /// <summary>
//        /// Save the trained RL model to a JSON file
//        /// </summary>
//        public void SaveModel(string filePath)
//        {
//            var modelData = new RLModelData
//            {
//                LearningRate = learningRate,
//                DiscountFactor = discountFactor,
//                Epsilon = epsilon,
//                EpsilonDecay = epsilonDecay,
//                EpsilonMin = epsilonMin,
//                QTable = qTable.ToDictionary(
//                    kvp => $"{kvp.Key.Item1},{kvp.Key.Item2}",
//                    kvp => kvp.Value
//                ),
//                OperatorSelectionCount = operatorSelectionCount,
//                OperatorRewardSum = operatorRewardSum
//            };

//            var options = new JsonSerializerOptions
//            {
//                WriteIndented = true
//            };

//            string jsonString = JsonSerializer.Serialize(modelData, options);
//            File.WriteAllText(filePath, jsonString);
//            Console.WriteLine($"RL model saved to: {filePath}");
//        }

//        /// <summary>
//        /// Load a trained RL model from a JSON file
//        /// </summary>
//        public static RLOperatorSelector LoadModel(string filePath, int? seed = null)
//        {
//            if (!File.Exists(filePath))
//            {
//                throw new FileNotFoundException($"Model file not found: {filePath}");
//            }

//            string jsonString = File.ReadAllText(filePath);
//            var modelData = JsonSerializer.Deserialize<RLModelData>(jsonString);

//            if (modelData == null)
//            {
//                throw new InvalidDataException("Failed to deserialize model data");
//            }

//            var agent = new RLOperatorSelector(
//                learningRate: modelData.LearningRate,
//                discountFactor: modelData.DiscountFactor,
//                epsilon: modelData.Epsilon,
//                epsilonDecay: modelData.EpsilonDecay,
//                epsilonMin: modelData.EpsilonMin,
//                seed: seed
//            );

//            // Load Q-table with error handling
//            try
//            {
//                agent.qTable = modelData.QTable.ToDictionary(
//                    kvp => {
//                        var parts = kvp.Key.Split(',');
//                        if (parts.Length != 2)
//                        {
//                            throw new InvalidDataException($"Invalid Q-table key format: {kvp.Key}");
//                        }
//                        if (!int.TryParse(parts[0], out int state) || !int.TryParse(parts[1], out int action))
//                        {
//                            throw new InvalidDataException($"Invalid Q-table key values: {kvp.Key}");
//                        }
//                        return (state, action);
//                    },
//                    kvp => kvp.Value
//                );
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidDataException($"Failed to load Q-table from model file: {ex.Message}", ex);
//            }

//            // Load statistics
//            agent.operatorSelectionCount = modelData.OperatorSelectionCount;
//            agent.operatorRewardSum = modelData.OperatorRewardSum;

//            Console.WriteLine($"RL model loaded from: {filePath}");
//            Console.WriteLine($"Q-table size: {agent.qTable.Count} state-action pairs");
//            Console.WriteLine($"Epsilon: {agent.epsilon:F4}");

//            return agent;
//        }

//        /// <summary>
//        /// Initialize the metrics log file with header
//        /// </summary>
//        private void InitializeMetricsLog()
//        {
//            if (string.IsNullOrEmpty(metricsLogPath)) return;

//            string? directory = Path.GetDirectoryName(metricsLogPath);
//            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//            {
//                Directory.CreateDirectory(directory);
//            }

//            string header = "Iteration,Timestamp,State,Action,Operator,Reward,QValueBefore,QValueAfter,Epsilon,BestObjective,CurrentObjective,QTableSize,AvgQValue,MaxQValue,MinQValue";
//            File.WriteAllText(metricsLogPath, header + Environment.NewLine);
//            Console.WriteLine($"Metrics logging initialized: {metricsLogPath}");
//        }

//        /// <summary>
//        /// Log metrics for the current iteration
//        /// </summary>
//        public void LogIterationMetrics(
//            int iteration,
//            int state,
//            int action,
//            double reward,
//            double qValueBefore,
//            double qValueAfter,
//            double bestObjective,
//            double currentObjective)
//        {
//            if (!trackMetrics) return;

//            var metrics = new IterationMetrics
//            {
//                Iteration = iteration,
//                Timestamp = DateTime.Now,
//                State = state,
//                Action = action,
//                Operator = operators[action],
//                Reward = reward,
//                QValueBefore = qValueBefore,
//                QValueAfter = qValueAfter,
//                Epsilon = epsilon,
//                BestObjective = bestObjective,
//                CurrentObjective = currentObjective,
//                QTableSize = qTable.Count,
//                AvgQValue = qTable.Count > 0 ? qTable.Values.Average() : 0.0,
//                MaxQValue = qTable.Count > 0 ? qTable.Values.Max() : 0.0,
//                MinQValue = qTable.Count > 0 ? qTable.Values.Min() : 0.0
//            };

//            iterationMetrics.Add(metrics);

//            // Buffer metrics for efficient file writing
//            // Note: Operator names are fixed strings (swap, insert, etc.) without commas,
//            // so no CSV escaping is needed in this specific case
//            if (!string.IsNullOrEmpty(metricsLogPath))
//            {
//                string line = $"{metrics.Iteration},{metrics.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{metrics.State},{metrics.Action},{metrics.Operator},{metrics.Reward:F6},{metrics.QValueBefore:F6},{metrics.QValueAfter:F6},{metrics.Epsilon:F6},{metrics.BestObjective:F2},{metrics.CurrentObjective:F2},{metrics.QTableSize},{metrics.AvgQValue:F6},{metrics.MaxQValue:F6},{metrics.MinQValue:F6}";
//                metricsBuffer.Add(line);

//                // Flush buffer periodically for better performance
//                if (metricsBuffer.Count >= MetricsBufferSize)
//                {
//                    FlushMetricsBuffer();
//                }
//            }
//        }

//        /// <summary>
//        /// Flush buffered metrics to file
//        /// </summary>
//        private void FlushMetricsBuffer()
//        {
//            if (metricsBuffer.Count > 0 && !string.IsNullOrEmpty(metricsLogPath))
//            {
//                File.AppendAllLines(metricsLogPath, metricsBuffer);
//                metricsBuffer.Clear();
//            }
//        }

//        /// <summary>
//        /// Save a comprehensive training summary report
//        /// </summary>
//        public void SaveTrainingSummary(string summaryPath, int totalIterations, double totalTime)
//        {
//            if (!trackMetrics || iterationMetrics.Count == 0) return;

//            // Flush any remaining buffered metrics before creating summary
//            FlushMetricsBuffer();

//            var summary = new StringBuilder();
//            summary.AppendLine("=== RL Training Summary Report ===");
//            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
//            summary.AppendLine($"Total Iterations: {totalIterations}");
//            summary.AppendLine($"Total Time: {totalTime:F2} seconds");
//            summary.AppendLine($"Average Time per Iteration: {totalTime / totalIterations:F4} seconds");
//            summary.AppendLine();

//            // Learning progression
//            summary.AppendLine("=== Learning Progression ===");
//            summary.AppendLine($"Initial Epsilon: 1.0");
//            summary.AppendLine($"Final Epsilon: {epsilon:F6}");
//            summary.AppendLine($"Q-table Size: {qTable.Count} state-action pairs");
//            summary.AppendLine($"Average Q-value: {(qTable.Count > 0 ? qTable.Values.Average() : 0.0):F6}");
//            summary.AppendLine($"Max Q-value: {(qTable.Count > 0 ? qTable.Values.Max() : 0.0):F6}");
//            summary.AppendLine($"Min Q-value: {(qTable.Count > 0 ? qTable.Values.Min() : 0.0):F6}");
//            summary.AppendLine();

//            // Operator performance
//            summary.AppendLine("=== Operator Performance ===");
//            summary.AppendLine("Operator    | Selections | Avg Reward | Total Reward | Selection %");
//            summary.AppendLine("------------|------------|------------|--------------|------------");
//            int totalSelections = operatorSelectionCount.Sum();
//            for (int i = 0; i < operators.Length; i++)
//            {
//                double avgReward = operatorSelectionCount[i] > 0 ? operatorRewardSum[i] / operatorSelectionCount[i] : 0.0;
//                double selectionPct = totalSelections > 0 ? (operatorSelectionCount[i] * 100.0 / totalSelections) : 0.0;
//                summary.AppendLine($"{operators[i],-11} | {operatorSelectionCount[i],10} | {avgReward,10:F4} | {operatorRewardSum[i],12:F2} | {selectionPct,10:F2}%");
//            }
//            summary.AppendLine();

//            // Objective improvement
//            if (iterationMetrics.Count > 0)
//            {
//                double initialObjective = iterationMetrics[0].BestObjective;
//                double finalObjective = iterationMetrics[iterationMetrics.Count - 1].BestObjective;
//                double improvement = initialObjective - finalObjective;
//                double improvementPct = (improvement / initialObjective) * 100.0;

//                summary.AppendLine("=== Objective Improvement ===");
//                summary.AppendLine($"Initial Best Objective: {initialObjective:F2}");
//                summary.AppendLine($"Final Best Objective: {finalObjective:F2}");
//                summary.AppendLine($"Total Improvement: {improvement:F2} ({improvementPct:F2}%)");
//                summary.AppendLine();
//            }

//            // Reward statistics
//            var allRewards = iterationMetrics.Select(m => m.Reward).ToList();
//            if (allRewards.Count > 0)
//            {
//                summary.AppendLine("=== Reward Statistics ===");
//                summary.AppendLine($"Total Rewards Collected: {allRewards.Count}");
//                summary.AppendLine($"Average Reward: {allRewards.Average():F6}");
//                summary.AppendLine($"Max Reward: {allRewards.Max():F6}");
//                summary.AppendLine($"Min Reward: {allRewards.Min():F6}");
//                summary.AppendLine($"Positive Rewards: {allRewards.Count(r => r > 0)} ({(allRewards.Count(r => r > 0) * 100.0 / allRewards.Count):F2}%)");
//                summary.AppendLine($"Negative Rewards: {allRewards.Count(r => r < 0)} ({(allRewards.Count(r => r < 0) * 100.0 / allRewards.Count):F2}%)");
//                summary.AppendLine();
//            }

//            // Q-value evolution (sample checkpoints)
//            summary.AppendLine("=== Q-value Evolution (Checkpoints) ===");
//            int[] checkpoints = { 0, totalIterations / 4, totalIterations / 2, 3 * totalIterations / 4, totalIterations - 1 };
//            summary.AppendLine("Iteration | Q-table Size | Avg Q-value | Max Q-value | Min Q-value");
//            summary.AppendLine("----------|--------------|-------------|-------------|------------");
//            foreach (int checkpoint in checkpoints)
//            {
//                if (checkpoint < iterationMetrics.Count)
//                {
//                    var m = iterationMetrics[checkpoint];
//                    summary.AppendLine($"{m.Iteration,9} | {m.QTableSize,12} | {m.AvgQValue,11:F6} | {m.MaxQValue,11:F6} | {m.MinQValue,11:F6}");
//                }
//            }
//            summary.AppendLine();

//            // Learning indicators
//            summary.AppendLine("=== Learning Assessment ===");
//            bool isLearning = AssessIfLearning();
//            summary.AppendLine($"Model appears to be learning: {(isLearning ? "YES" : "NO")}");
//            summary.AppendLine();
//            summary.AppendLine("Indicators:");
//            summary.AppendLine($"- Q-table growth: {qTable.Count} state-action pairs explored");
//            summary.AppendLine($"- Epsilon decay: {epsilon:F6} (started at 1.0)");
//            summary.AppendLine($"- Reward trend: {(GetRewardTrend() > 0 ? "Improving" : "Stable/Declining")}");
//            summary.AppendLine($"- Operator preferences: {(HasOperatorPreferences() ? "Developed" : "Still exploring")}");

//            File.WriteAllText(summaryPath, summary.ToString());
//            Console.WriteLine($"Training summary saved to: {summaryPath}");
//        }

//        /// <summary>
//        /// Assess if the model is learning based on metrics
//        /// </summary>
//        private bool AssessIfLearning()
//        {
//            if (iterationMetrics.Count < 100) return false;

//            // Check if Q-table is growing (exploring states)
//            bool qTableGrowing = qTable.Count > 10;

//            // Check if rewards are improving over time
//            bool rewardImproving = GetRewardTrend() > 0;

//            // Check if operator preferences are developing
//            bool hasPreferences = HasOperatorPreferences();

//            // Check if epsilon has decayed (reducing exploration)
//            bool epsilonDecayed = epsilon < 0.9;

//            // At least 2 of these indicators should be true
//            int indicators = (qTableGrowing ? 1 : 0) + (rewardImproving ? 1 : 0) + 
//                           (hasPreferences ? 1 : 0) + (epsilonDecayed ? 1 : 0);

//            return indicators >= 2;
//        }

//        /// <summary>
//        /// Calculate reward trend (positive = improving, negative = declining)
//        /// </summary>
//        private double GetRewardTrend()
//        {
//            if (iterationMetrics.Count < 100) return 0.0;

//            int windowSize = Math.Min(100, iterationMetrics.Count / 4);
//            var firstWindow = iterationMetrics.Take(windowSize).Select(m => m.Reward).Average();
//            var lastWindow = iterationMetrics.TakeLast(windowSize).Select(m => m.Reward).Average();

//            return lastWindow - firstWindow;
//        }

//        /// <summary>
//        /// Check if operator preferences have developed
//        /// </summary>
//        private bool HasOperatorPreferences()
//        {
//            if (operatorSelectionCount.Sum() < 100) return false;

//            double totalSelections = operatorSelectionCount.Sum();
//            double expectedUniform = totalSelections / operators.Length;

//            // Check if any operator is selected significantly more than uniform distribution
//            return operatorSelectionCount.Any(count => Math.Abs(count - expectedUniform) > expectedUniform * 0.3);
//        }

//        /// <summary>
//        /// Export Q-table to CSV for detailed analysis
//        /// </summary>
//        public void ExportQTableToCSV(string filePath)
//        {
//            var csv = new StringBuilder();
//            csv.AppendLine("State,Action,Operator,QValue");

//            foreach (var kvp in qTable.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
//            {
//                csv.AppendLine($"{kvp.Key.Item1},{kvp.Key.Item2},{operators[kvp.Key.Item2]},{kvp.Value:F6}");
//            }

//            File.WriteAllText(filePath, csv.ToString());
//            Console.WriteLine($"Q-table exported to: {filePath}");
//        }
//    }

//    /// <summary>
//    /// Data structure for tracking metrics at each iteration
//    /// </summary>
//    public class IterationMetrics
//    {
//        public int Iteration { get; set; }
//        public DateTime Timestamp { get; set; }
//        public int State { get; set; }
//        public int Action { get; set; }
//        public string Operator { get; set; } = "";
//        public double Reward { get; set; }
//        public double QValueBefore { get; set; }
//        public double QValueAfter { get; set; }
//        public double Epsilon { get; set; }
//        public double BestObjective { get; set; }
//        public double CurrentObjective { get; set; }
//        public int QTableSize { get; set; }
//        public double AvgQValue { get; set; }
//        public double MaxQValue { get; set; }
//        public double MinQValue { get; set; }
//    }

//    /// <summary>
//    /// Data structure for serializing/deserializing RL model
//    /// </summary>
//    public class RLModelData
//    {
//        public double LearningRate { get; set; }
//        public double DiscountFactor { get; set; }
//        public double Epsilon { get; set; }
//        public double EpsilonDecay { get; set; }
//        public double EpsilonMin { get; set; }
//        public Dictionary<string, double> QTable { get; set; } = new Dictionary<string, double>();
//        public int[] OperatorSelectionCount { get; set; } = new int[6];
//        public double[] OperatorRewardSum { get; set; } = new double[6];
//    }
//}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RCVRPTW
{
    /// <summary>
    /// Q-learning agent for selecting mutation operators in the Tabu Search algorithm,
    /// with improved reward function and state definition to handle stagnation.
    /// </summary>
    public class RLOperatorSelector
    {
        // Q-table: maps state-action pairs to Q-values
        private Dictionary<(int, int), double> qTable;

        // Available actions (operators)
        private readonly string[] operators = { "swap", "insert", "invert", "2opt", "oropt", "cross" };

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

        // Metrics tracking for learning assessment
        private List<IterationMetrics> iterationMetrics;
        private string? metricsLogPath;
        private bool trackMetrics;
        private List<string> metricsBuffer;
        private const int MetricsBufferSize = 50; // Flush every 50 iterations

        // --- Poprawiona Definicja Stanu ---
        private const int ImprovementBins = 5;
        private const int ProgressBins = 3;
        private const int StagnationBins = 3;
        private const int TotalStates = ImprovementBins * ProgressBins * StagnationBins;
        // ------------------------------------

        /// <summary>
        /// Initialize the RL agent with specified hyperparameters
        /// </summary>
        public RLOperatorSelector(
            double learningRate = 0.1,
            double discountFactor = 0.9,
            double epsilon = 1.0,
            double epsilonDecay = 0.999, // Zmieniono z 0.995 na 0.999 (wolniejsza dekay'a)
            double epsilonMin = 0.01,
            int? seed = null,
            bool trackMetrics = false,
            string? metricsLogPath = null)
        {
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
            this.epsilon = epsilon;
            this.epsilonDecay = epsilonDecay;
            this.epsilonMin = epsilonMin;
            this.trackMetrics = trackMetrics;
            this.metricsLogPath = metricsLogPath;

            qTable = new Dictionary<(int, int), double>();
            operatorSelectionCount = new int[operators.Length];
            operatorRewardSum = new double[operators.Length];
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            iterationMetrics = new List<IterationMetrics>();
            metricsBuffer = new List<string>();

            // Initialize metrics log file if tracking is enabled
            if (trackMetrics && !string.IsNullOrEmpty(metricsLogPath))
            {
                InitializeMetricsLog();
            }
        }

        /// <summary>
        /// Get state representation based on current search progress.
        /// State is discretized into bins based on:
        /// 1. Improvement rate (vs best objective). (5 bins)
        /// 2. Progress through search. (3 bins)
        /// 3. Stagnation (iterations since best improvement). (3 bins)
        /// </summary>
        private int GetState(
            double currentObjective,
            double bestObjective,
            int iteration,
            int maxIterations,
            int iterationsSinceBestImprovement)
        {
            // 1. Discretize Improvement (5 bins)
            double improvement = (bestObjective - currentObjective) / Math.Max(Math.Abs(bestObjective), 1.0);
            int improvementBin = 0;
            if (improvement > 0.1) improvementBin = 0;       // Large improvement
            else if (improvement > 0.01) improvementBin = 1; // Medium improvement
            else if (improvement > 0.001) improvementBin = 2; // Small improvement
            else if (improvement > 0.0) improvementBin = 3;  // Tiny improvement
            else improvementBin = 4;                         // No improvement / Degradation

            // 2. Discretize Progress (3 bins)
            double progress = (double)iteration / Math.Max(maxIterations, 1);
            int progressBin = 0;
            if (progress < 0.33) progressBin = 0;      // Early stage
            else if (progress < 0.66) progressBin = 1; // Middle stage
            else progressBin = 2;                      // Late stage

            // 3. Discretize Stagnation (3 bins)
            int stagnationBin = 0;
            // Przykładowe progi (zależne od skali problemu i maxIterations)
            if (iterationsSinceBestImprovement < 50) stagnationBin = 0; // Fresh improvement
            else if (iterationsSinceBestImprovement < 250) stagnationBin = 1; // Moderate stagnation
            else stagnationBin = 2; // Deep stagnation

            // Combine bins into a single state (5 * 3 * 3 = 45 possible states)
            return improvementBin * ProgressBins * StagnationBins +
                   progressBin * StagnationBins +
                   stagnationBin;
        }

        /// <summary>
        /// Select an operator using epsilon-greedy policy.
        /// </summary>
        public string SelectOperator(
            double currentObjective,
            double bestObjective,
            int iteration,
            int maxIterations,
            int iterationsSinceBestImprovement)
        {
            int state = GetState(currentObjective, bestObjective, iteration, maxIterations, iterationsSinceBestImprovement);
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
        /// (Implementation unchanged, relies on GetQValue)
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
        /// (Implementation unchanged)
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
        /// </summary>
        public void UpdateQValue(
            double previousObjective,
            double currentObjective,
            double bestObjective,
            int previousIteration,
            int currentIteration,
            int maxIterations,
            string selectedOperator,
            int previousIterationsSinceBestImprovement,
            int currentIterationsSinceBestImprovement)
        {
            // Now we need two state representations due to the added parameter
            int previousState = GetState(
                previousObjective,
                bestObjective,
                previousIteration,
                maxIterations,
                previousIterationsSinceBestImprovement);

            int currentState = GetState(
                currentObjective,
                bestObjective,
                currentIteration,
                maxIterations,
                currentIterationsSinceBestImprovement);

            int action = Array.IndexOf(operators, selectedOperator);

            if (action < 0) return; // Invalid operator

            // --- Poprawione Obliczanie Nagrody ---
            double reward = CalculateReward(previousObjective, currentObjective, bestObjective);
            // ------------------------------------

            // Get current Q-value (before update)
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

            // Log metrics if tracking is enabled
            if (trackMetrics)
            {
                LogIterationMetrics(
                    currentIteration,
                    previousState,
                    action,
                    reward,
                    currentQ,
                    newQ,
                    bestObjective,
                    currentObjective
                );
            }

            // Track statistics
            operatorRewardSum[action] += reward;

            // Decay epsilon (reduce exploration over time)
            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
        }

        /// <summary>
        /// Calculate reward based on objective improvement (Reward Shaping).
        /// - High reward for global improvement.
        /// - Medium reward for local improvement.
        /// - Small penalty for degradation (avoids large negative spikes).
        /// </summary>
        private double CalculateReward(double previousObjective, double currentObjective, double bestObjective)
        {
            // Wartość do skalowania nagród (można dostosować)
            const double ScaleFactor = 10.0;

            // Nagroda za poprawę globalnego minimum
            if (currentObjective < bestObjective)
            {
                // Duża, stała nagroda za przełom (Nowy bestObjective)
                return ScaleFactor * 2.0;
            }
            // Nagroda za poprawę lokalną (currentObjective jest lepsze niż previousObjective, ale gorsze niż bestObjective)
            else if (currentObjective < previousObjective)
            {
                double relativeImprovement = (previousObjective - currentObjective) / Math.Max(Math.Abs(previousObjective), 1.0);
                // Nagroda proporcjonalna do wielkości poprawy
                return relativeImprovement * ScaleFactor * 0.5;
            }
            // Kara za pogorszenie
            else
            {
                // Mała, stała kara za ruch akceptowany w Tabu Search (dywersyfikacja)
                return -0.2;
            }
        }

        // --- Pozostałe metody (GetStatistics, ResetStatistics, ExportQTable, SaveModel, LoadModel, itp.) ---
        // --- Pozostają bez zmian, aby zachować funkcjonalność zapisu/odczytu i logowania ---

        /// <summary>
        /// Get statistics about operator usage and performance
        /// </summary>
        public string GetStatistics()
        {
            var stats = "\n=== RL Operator Selector Statistics ===\n";
            stats += $"Current epsilon (exploration rate): {epsilon:F4}\n";
            stats += $"Q-table size: {qTable.Count} state-action pairs (Total states: {TotalStates})\n\n";

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

            // Load Q-table with error handling
            try
            {
                agent.qTable = modelData.QTable.ToDictionary(
                    kvp => {
                        var parts = kvp.Key.Split(',');
                        if (parts.Length != 2)
                        {
                            throw new InvalidDataException($"Invalid Q-table key format: {kvp.Key}");
                        }
                        if (!int.TryParse(parts[0], out int state) || !int.TryParse(parts[1], out int action))
                        {
                            throw new InvalidDataException($"Invalid Q-table key values: {kvp.Key}");
                        }
                        return (state, action);
                    },
                    kvp => kvp.Value
                );
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to load Q-table from model file: {ex.Message}", ex);
            }

            // Load statistics
            agent.operatorSelectionCount = modelData.OperatorSelectionCount;
            agent.operatorRewardSum = modelData.OperatorRewardSum;

            Console.WriteLine($"RL model loaded from: {filePath}");
            Console.WriteLine($"Q-table size: {agent.qTable.Count} state-action pairs");
            Console.WriteLine($"Epsilon: {agent.epsilon:F4}");

            return agent;
        }

        /// <summary>
        /// Initialize the metrics log file with header
        /// </summary>
        private void InitializeMetricsLog()
        {
            if (string.IsNullOrEmpty(metricsLogPath)) return;

            string? directory = Path.GetDirectoryName(metricsLogPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string header = "Iteration,Timestamp,State,Action,Operator,Reward,QValueBefore,QValueAfter,Epsilon,BestObjective,CurrentObjective,QTableSize,AvgQValue,MaxQValue,MinQValue";
            File.WriteAllText(metricsLogPath, header + Environment.NewLine);
            Console.WriteLine($"Metrics logging initialized: {metricsLogPath}");
        }

        /// <summary>
        /// Log metrics for the current iteration
        /// </summary>
        public void LogIterationMetrics(
            int iteration,
            int state,
            int action,
            double reward,
            double qValueBefore,
            double qValueAfter,
            double bestObjective,
            double currentObjective)
        {
            if (!trackMetrics) return;

            var metrics = new IterationMetrics
            {
                Iteration = iteration,
                Timestamp = DateTime.Now,
                State = state,
                Action = action,
                Operator = operators[action],
                Reward = reward,
                QValueBefore = qValueBefore,
                QValueAfter = qValueAfter,
                Epsilon = epsilon,
                BestObjective = bestObjective,
                CurrentObjective = currentObjective,
                QTableSize = qTable.Count,
                AvgQValue = qTable.Count > 0 ? qTable.Values.Average() : 0.0,
                MaxQValue = qTable.Count > 0 ? qTable.Values.Max() : 0.0,
                MinQValue = qTable.Count > 0 ? qTable.Values.Min() : 0.0
            };

            iterationMetrics.Add(metrics);

            // Buffer metrics for efficient file writing
            if (!string.IsNullOrEmpty(metricsLogPath))
            {
                string line = $"{metrics.Iteration},{metrics.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{metrics.State},{metrics.Action},{metrics.Operator},{metrics.Reward:F6},{metrics.QValueBefore:F6},{metrics.QValueAfter:F6},{metrics.Epsilon:F6},{metrics.BestObjective:F2},{metrics.CurrentObjective:F2},{metrics.QTableSize},{metrics.AvgQValue:F6},{metrics.MaxQValue:F6},{metrics.MinQValue:F6}";
                metricsBuffer.Add(line);

                // Flush buffer periodically for better performance
                if (metricsBuffer.Count >= MetricsBufferSize)
                {
                    FlushMetricsBuffer();
                }
            }
        }

        /// <summary>
        /// Flush buffered metrics to file
        /// </summary>
        private void FlushMetricsBuffer()
        {
            if (metricsBuffer.Count > 0 && !string.IsNullOrEmpty(metricsLogPath))
            {
                File.AppendAllLines(metricsLogPath, metricsBuffer);
                metricsBuffer.Clear();
            }
        }

        /// <summary>
        /// Save a comprehensive training summary report
        /// </summary>
        public void SaveTrainingSummary(string summaryPath, int totalIterations, double totalTime)
        {
            if (!trackMetrics || iterationMetrics.Count == 0) return;

            // Flush any remaining buffered metrics before creating summary
            FlushMetricsBuffer();

            var summary = new StringBuilder();
            summary.AppendLine("=== RL Training Summary Report ===");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Total Iterations: {totalIterations}");
            summary.AppendLine($"Total Time: {totalTime:F2} seconds");
            summary.AppendLine($"Average Time per Iteration: {totalTime / totalIterations:F4} seconds");
            summary.AppendLine();

            // Learning progression
            summary.AppendLine("=== Learning Progression ===");
            summary.AppendLine($"Initial Epsilon: 1.0");
            summary.AppendLine($"Final Epsilon: {epsilon:F6}");
            summary.AppendLine($"Q-table Size: {qTable.Count} state-action pairs (Total: {TotalStates})");
            summary.AppendLine($"Average Q-value: {(qTable.Count > 0 ? qTable.Values.Average() : 0.0):F6}");
            summary.AppendLine($"Max Q-value: {(qTable.Count > 0 ? qTable.Values.Max() : 0.0):F6}");
            summary.AppendLine($"Min Q-value: {(qTable.Count > 0 ? qTable.Values.Min() : 0.0):F6}");
            summary.AppendLine();

            // Operator performance
            summary.AppendLine("=== Operator Performance ===");
            summary.AppendLine("Operator    | Selections | Avg Reward | Total Reward | Selection %");
            summary.AppendLine("------------|------------|------------|--------------|------------");
            int totalSelections = operatorSelectionCount.Sum();
            for (int i = 0; i < operators.Length; i++)
            {
                double avgReward = operatorSelectionCount[i] > 0 ? operatorRewardSum[i] / operatorSelectionCount[i] : 0.0;
                double selectionPct = totalSelections > 0 ? (operatorSelectionCount[i] * 100.0 / totalSelections) : 0.0;
                summary.AppendLine($"{operators[i],-11} | {operatorSelectionCount[i],10} | {avgReward,10:F4} | {operatorRewardSum[i],12:F2} | {selectionPct,10:F2}%");
            }
            summary.AppendLine();

            // Objective improvement
            if (iterationMetrics.Count > 0)
            {
                double initialObjective = iterationMetrics[0].BestObjective;
                double finalObjective = iterationMetrics[iterationMetrics.Count - 1].BestObjective;
                double improvement = initialObjective - finalObjective;
                double improvementPct = (improvement / initialObjective) * 100.0;

                summary.AppendLine("=== Objective Improvement ===");
                summary.AppendLine($"Initial Best Objective: {initialObjective:F2}");
                summary.AppendLine($"Final Best Objective: {finalObjective:F2}");
                summary.AppendLine($"Total Improvement: {improvement:F2} ({improvementPct:F2}%)");
                summary.AppendLine();
            }

            // Reward statistics
            var allRewards = iterationMetrics.Select(m => m.Reward).ToList();
            if (allRewards.Count > 0)
            {
                summary.AppendLine("=== Reward Statistics ===");
                summary.AppendLine($"Total Rewards Collected: {allRewards.Count}");
                summary.AppendLine($"Average Reward: {allRewards.Average():F6}");
                summary.AppendLine($"Max Reward: {allRewards.Max():F6}");
                summary.AppendLine($"Min Reward: {allRewards.Min():F6}");
                summary.AppendLine($"Positive Rewards: {allRewards.Count(r => r > 0)} ({(allRewards.Count(r => r > 0) * 100.0 / allRewards.Count):F2}%)");
                summary.AppendLine($"Negative Rewards: {allRewards.Count(r => r < 0)} ({(allRewards.Count(r => r < 0) * 100.0 / allRewards.Count):F2}%)");
                summary.AppendLine();
            }

            // Q-value evolution (sample checkpoints)
            summary.AppendLine("=== Q-value Evolution (Checkpoints) ===");
            int[] checkpoints = { 0, totalIterations / 4, totalIterations / 2, 3 * totalIterations / 4, totalIterations - 1 };
            summary.AppendLine("Iteration | Q-table Size | Avg Q-value | Max Q-value | Min Q-value");
            summary.AppendLine("----------|--------------|-------------|-------------|------------");
            foreach (int checkpoint in checkpoints)
            {
                if (checkpoint < iterationMetrics.Count)
                {
                    var m = iterationMetrics[checkpoint];
                    summary.AppendLine($"{m.Iteration,9} | {m.QTableSize,12} | {m.AvgQValue,11:F6} | {m.MaxQValue,11:F6} | {m.MinQValue,11:F6}");
                }
            }
            summary.AppendLine();

            // Learning indicators
            summary.AppendLine("=== Learning Assessment ===");
            bool isLearning = AssessIfLearning();
            summary.AppendLine($"Model appears to be learning: {(isLearning ? "YES" : "NO")}");
            summary.AppendLine();
            summary.AppendLine("Indicators:");
            summary.AppendLine($"- Q-table growth: {qTable.Count} state-action pairs explored");
            summary.AppendLine($"- Epsilon decay: {epsilon:F6} (started at 1.0)");
            summary.AppendLine($"- Reward trend: {(GetRewardTrend() > 0 ? "Improving" : "Stable/Declining")}");
            summary.AppendLine($"- Operator preferences: {(HasOperatorPreferences() ? "Developed" : "Still exploring")}");

            File.WriteAllText(summaryPath, summary.ToString());
            Console.WriteLine($"Training summary saved to: {summaryPath}");
        }

        /// <summary>
        /// Assess if the model is learning based on metrics
        /// </summary>
        private bool AssessIfLearning()
        {
            if (iterationMetrics.Count < 100) return false;

            // Check if Q-table is growing (exploring states)
            bool qTableGrowing = qTable.Count > 10;

            // Check if rewards are improving over time
            bool rewardImproving = GetRewardTrend() > 0;

            // Check if operator preferences are developing
            bool hasPreferences = HasOperatorPreferences();

            // Check if epsilon has decayed (reducing exploration)
            bool epsilonDecayed = epsilon < 0.9;

            // At least 2 of these indicators should be true
            int indicators = (qTableGrowing ? 1 : 0) + (rewardImproving ? 1 : 0) +
                             (hasPreferences ? 1 : 0) + (epsilonDecayed ? 1 : 0);

            return indicators >= 2;
        }

        /// <summary>
        /// Calculate reward trend (positive = improving, negative = declining)
        /// </summary>
        private double GetRewardTrend()
        {
            if (iterationMetrics.Count < 100) return 0.0;

            int windowSize = Math.Min(100, iterationMetrics.Count / 4);
            var firstWindow = iterationMetrics.Take(windowSize).Select(m => m.Reward).Average();
            var lastWindow = iterationMetrics.TakeLast(windowSize).Select(m => m.Reward).Average();

            return lastWindow - firstWindow;
        }

        /// <summary>
        /// Check if operator preferences have developed
        /// </summary>
        private bool HasOperatorPreferences()
        {
            if (operatorSelectionCount.Sum() < 100) return false;

            double totalSelections = operatorSelectionCount.Sum();
            double expectedUniform = totalSelections / operators.Length;

            // Check if any operator is selected significantly more than uniform distribution
            return operatorSelectionCount.Any(count => Math.Abs(count - expectedUniform) > expectedUniform * 0.3);
        }

        /// <summary>
        /// Export Q-table to CSV for detailed analysis
        /// </summary>
        public void ExportQTableToCSV(string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("State,Action,Operator,QValue");

            foreach (var kvp in qTable.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
            {
                csv.AppendLine($"{kvp.Key.Item1},{kvp.Key.Item2},{operators[kvp.Key.Item2]},{kvp.Value:F6}");
            }

            File.WriteAllText(filePath, csv.ToString());
            Console.WriteLine($"Q-table exported to: {filePath}");
        }
    }

    /// <summary>
    /// Data structure for tracking metrics at each iteration
    /// </summary>
    public class IterationMetrics
    {
        public int Iteration { get; set; }
        public DateTime Timestamp { get; set; }
        public int State { get; set; }
        public int Action { get; set; }
        public string Operator { get; set; } = "";
        public double Reward { get; set; }
        public double QValueBefore { get; set; }
        public double QValueAfter { get; set; }
        public double Epsilon { get; set; }
        public double BestObjective { get; set; }
        public double CurrentObjective { get; set; }
        public int QTableSize { get; set; }
        public double AvgQValue { get; set; }
        public double MaxQValue { get; set; }
        public double MinQValue { get; set; }
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
        public int[] OperatorSelectionCount { get; set; } = new int[6];
        public double[] OperatorRewardSum { get; set; } = new double[6];
    }
}