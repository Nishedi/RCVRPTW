using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RCVRPTW
{
   
    
    public class RLOperatorSelector
    {
        private Dictionary<(int, int), double> qTable;
        private readonly string[] operators = { "swap", "insert", "invert", "2opt", "oropt", "cross" };


        private double learningRate;      
        private double discountFactor;    
        private double epsilon;           
        private double epsilonDecay;      
        private double epsilonMin;       

        private int[] operatorSelectionCount;
        private double[] operatorRewardSum;
        private Random random;

        private List<IterationMetrics> iterationMetrics;
        private string? metricsLogPath;
        private bool trackMetrics;
        private List<string> metricsBuffer;
        private const int MetricsBufferSize = 50; 

        private const int ImprovementBins = 5;
        private const int ProgressBins = 3;
        private const int StagnationBins = 3;
        private const int TotalStates = ImprovementBins * ProgressBins * StagnationBins;

        
        public RLOperatorSelector(
            double learningRate = 0.1,
            double discountFactor = 0.9,
            double epsilon = 1.0,
            double epsilonDecay = 0.999, 
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

           
            if (trackMetrics && !string.IsNullOrEmpty(metricsLogPath))
            {
                InitializeMetricsLog();
            }
        }
        private int GetState(
            double currentObjective,
            double bestObjective,
            int iteration,
            int maxIterations,
            int iterationsSinceBestImprovement)
        {
            double improvement = (bestObjective - currentObjective) / Math.Max(Math.Abs(bestObjective), 1.0);
            int improvementBin = 0;
            if (improvement > 0.1) improvementBin = 0;       
            else if (improvement > 0.01) improvementBin = 1; 
            else if (improvement > 0.001) improvementBin = 2; 
            else if (improvement > 0.0) improvementBin = 3;  
            else improvementBin = 4;                        

            double progress = (double)iteration / Math.Max(maxIterations, 1);
            int progressBin = 0;
            if (progress < 0.33) progressBin = 0;      
            else if (progress < 0.66) progressBin = 1; 
            else progressBin = 2;                      

            int stagnationBin = 0;
            if (iterationsSinceBestImprovement < 50) stagnationBin = 0; 
            else if (iterationsSinceBestImprovement < 250) stagnationBin = 1; 
            else stagnationBin = 2; 
            return improvementBin * ProgressBins * StagnationBins +
                   progressBin * StagnationBins +
                   stagnationBin;
        }
        public string SelectOperator(
            double currentObjective,
            double bestObjective,
            int iteration,
            int maxIterations,
            int iterationsSinceBestImprovement)
        {
            int state = GetState(currentObjective, bestObjective, iteration, maxIterations, iterationsSinceBestImprovement);
            int action;

            if (random.NextDouble() < epsilon)
            {
                action = random.Next(operators.Length);
            }
            else
            {
                action = GetBestAction(state);
            }

            operatorSelectionCount[action]++;
            return operators[action];
        }

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

        private double GetQValue(int state, int action)
        {
            var key = (state, action);
            if (!qTable.ContainsKey(key))
            {
                qTable[key] = 0.0; 
            }
            return qTable[key];
        }

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

            if (action < 0) return; 

            double reward = CalculateReward(previousObjective, currentObjective, bestObjective);
            double currentQ = GetQValue(previousState, action);

            double maxNextQ = GetQValue(currentState, 0);
            for (int a = 1; a < operators.Length; a++)
            {
                double q = GetQValue(currentState, a);
                if (q > maxNextQ) maxNextQ = q;
            }

            double newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
            qTable[(previousState, action)] = newQ;

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

            operatorRewardSum[action] += reward;
            epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
        }

       private double CalculateReward(double previousObjective, double currentObjective, double bestObjective)
        {
            const double ScaleFactor = 10.0;
            if (currentObjective < bestObjective)
            {
                return ScaleFactor * 2.0;
            }
            else if (currentObjective < previousObjective)
            {
                double relativeImprovement = (previousObjective - currentObjective) / Math.Max(Math.Abs(previousObjective), 1.0);
                return relativeImprovement * ScaleFactor * 0.5;
            }
            else
            {
                return -0.2;
            }
        }

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

        public double GetEpsilon()
        {
            return epsilon;
        }

        public string ExportQTable()
        {
            var output = "State,Action,QValue\n";
            foreach (var kvp in qTable.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
            {
                output += $"{kvp.Key.Item1},{operators[kvp.Key.Item2]},{kvp.Value:F6}\n";
            }
            return output;
        }

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

            agent.operatorSelectionCount = modelData.OperatorSelectionCount;
            agent.operatorRewardSum = modelData.OperatorRewardSum;

            Console.WriteLine($"RL model loaded from: {filePath}");
            Console.WriteLine($"Q-table size: {agent.qTable.Count} state-action pairs");
            Console.WriteLine($"Epsilon: {agent.epsilon:F4}");

            return agent;
        }

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

            if (!string.IsNullOrEmpty(metricsLogPath))
            {
                string line = $"{metrics.Iteration},{metrics.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{metrics.State},{metrics.Action},{metrics.Operator},{metrics.Reward:F6},{metrics.QValueBefore:F6},{metrics.QValueAfter:F6},{metrics.Epsilon:F6},{metrics.BestObjective:F2},{metrics.CurrentObjective:F2},{metrics.QTableSize},{metrics.AvgQValue:F6},{metrics.MaxQValue:F6},{metrics.MinQValue:F6}";
                metricsBuffer.Add(line);

                if (metricsBuffer.Count >= MetricsBufferSize)
                {
                    FlushMetricsBuffer();
                }
            }
        }

        private void FlushMetricsBuffer()
        {
            if (metricsBuffer.Count > 0 && !string.IsNullOrEmpty(metricsLogPath))
            {
                File.AppendAllLines(metricsLogPath, metricsBuffer);
                metricsBuffer.Clear();
            }
        }

        public void SaveTrainingSummary(string summaryPath, int totalIterations, double totalTime)
        {
            if (!trackMetrics || iterationMetrics.Count == 0) return;

            FlushMetricsBuffer();

            var summary = new StringBuilder();
            summary.AppendLine("=== RL Training Summary Report ===");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Total Iterations: {totalIterations}");
            summary.AppendLine($"Total Time: {totalTime:F2} seconds");
            summary.AppendLine($"Average Time per Iteration: {totalTime / totalIterations:F4} seconds");
            summary.AppendLine();

            summary.AppendLine("=== Learning Progression ===");
            summary.AppendLine($"Initial Epsilon: 1.0");
            summary.AppendLine($"Final Epsilon: {epsilon:F6}");
            summary.AppendLine($"Q-table Size: {qTable.Count} state-action pairs (Total: {TotalStates})");
            summary.AppendLine($"Average Q-value: {(qTable.Count > 0 ? qTable.Values.Average() : 0.0):F6}");
            summary.AppendLine($"Max Q-value: {(qTable.Count > 0 ? qTable.Values.Max() : 0.0):F6}");
            summary.AppendLine($"Min Q-value: {(qTable.Count > 0 ? qTable.Values.Min() : 0.0):F6}");
            summary.AppendLine();

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

        private bool AssessIfLearning()
        {
            if (iterationMetrics.Count < 100) return false;

            bool qTableGrowing = qTable.Count > 10;

            bool rewardImproving = GetRewardTrend() > 0;

            bool hasPreferences = HasOperatorPreferences();

            bool epsilonDecayed = epsilon < 0.9;

            int indicators = (qTableGrowing ? 1 : 0) + (rewardImproving ? 1 : 0) +
                             (hasPreferences ? 1 : 0) + (epsilonDecayed ? 1 : 0);

            return indicators >= 2;
        }
        private double GetRewardTrend()
        {
            if (iterationMetrics.Count < 100) return 0.0;

            int windowSize = Math.Min(100, iterationMetrics.Count / 4);
            var firstWindow = iterationMetrics.Take(windowSize).Select(m => m.Reward).Average();
            var lastWindow = iterationMetrics.TakeLast(windowSize).Select(m => m.Reward).Average();

            return lastWindow - firstWindow;
        }

        private bool HasOperatorPreferences()
        {
            if (operatorSelectionCount.Sum() < 100) return false;

            double totalSelections = operatorSelectionCount.Sum();
            double expectedUniform = totalSelections / operators.Length;

            return operatorSelectionCount.Any(count => Math.Abs(count - expectedUniform) > expectedUniform * 0.3);
        }

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