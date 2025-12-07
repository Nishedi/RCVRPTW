using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class TabuSearch
    {
        public static Solution run(int MaxIterations, int TabuSize, Instance instance, string mutationtype="swap", int maxTime = 120)
        {
            Solution bestSolution = GreedyApproaches.generateGreedySolution(instance);
            bestSolution.calculateRoutesMetrics(instance);
            (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics = (bestSolution.TotalCost, bestSolution.TotalPenalty, bestSolution.TotalVehicleOperationTime);
            var bestObjective = bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime;
            Console.WriteLine($"TabuSize:{TabuSize} MaxIterations:{MaxIterations} MutationType: {mutationtype}" +
                $"Initial greedy solution objective: {Math.Round(bestObjective)}");
            Solution currentSolution = bestSolution;
            Queue<Solution> tabuList = new Queue<Solution>();
            int notImprovingIterations = 0;
            int iter = 0;
            var stopwatch = Stopwatch.StartNew();
            maxTime *= 1000;
            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, mutationtype);
                foreach (var neighbor in neighborhood.Take(TabuSize*10))
                {
                    bool isTabu = tabuList.Any(tabuSolution => tabuSolution.Equals(neighbor));
                    var objective = neighbor.TotalCost + neighbor.TotalPenalty + neighbor.TotalVehicleOperationTime;
                    if (isTabu && objective >= bestObjective)
                        continue;
                    if (objective < bestNeighborObjective)
                    {
                        bestNeighborObjective = objective;
                        bestNeighbor = neighbor;
                    }
                }
                if (bestNeighbor == null)
                {
                    break;
                }
                currentSolution = bestNeighbor;
                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;
                    Console.Write($"{iter}:{Math.Round(bestObjective)}. ");
                }
                else
                {
                    notImprovingIterations++;
                }
                tabuList.Enqueue(currentSolution);
                if (tabuList.Count > TabuSize)
                    tabuList.Dequeue();
                if (notImprovingIterations >= MaxIterations)
                {
                    Console.Write(",");
                    notImprovingIterations = 0;
                    currentSolution = NeighborhoodGeneratorLocation.GenerateRandomSolution(currentSolution.Routes, instance.Vehicles, instance);
                }
                iter++;
            }
            Console.WriteLine($"\nTabu Search completed {iter} iterations in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n\n");
            bestSolution.GreedyMetrics = GreedyMetrics;
            return bestSolution;
        }

        /// <summary>
        /// Run Tabu Search with RL-based operator selection
        /// The RL agent learns which operator (swap/insert/invert) works best during the search
        /// </summary>
        public static Solution runWithRL(int MaxIterations, int TabuSize, Instance instance, int maxTime = 3600, int? seed = null, string? modelPath = null)
        {
            // Initialize RL agent - either load from file or create new
            RLOperatorSelector rlAgent;
            if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
            {
                Console.WriteLine($"Loading pre-trained RL model from: {modelPath}");
                rlAgent = RLOperatorSelector.LoadModel(modelPath, seed);
            }
            else
            {
                rlAgent = new RLOperatorSelector(
                    learningRate: 0.1,
                    discountFactor: 0.9,
                    epsilon: 1.0,           // Start with full exploration
                    epsilonDecay: 0.9995,   // Slow decay for long training
                    epsilonMin: 0.1,        // Keep some exploration
                    seed: seed
                );
            }

            Solution bestSolution = GreedyApproaches.generateGreedySolution(instance);
            bestSolution.calculateRoutesMetrics(instance);
            (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics = (bestSolution.TotalCost, bestSolution.TotalPenalty, bestSolution.TotalVehicleOperationTime);
            var bestObjective = bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime;
            Console.WriteLine($"TabuSize:{TabuSize} MaxIterations:{MaxIterations} MutationType: RL-based " +
                $"Initial greedy solution objective: {Math.Round(bestObjective)}");
            Console.WriteLine($"RL Training enabled - maxTime: {maxTime}s");
            
            Solution currentSolution = bestSolution;
            Queue<Solution> tabuList = new Queue<Solution>();
            int notImprovingIterations = 0;
            int iter = 0;
            var stopwatch = Stopwatch.StartNew();
            maxTime *= 1000;
            
            double previousObjective = bestObjective;
            string selectedOperator = "swap";
            
            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                double currentObjective = currentSolution.TotalCost + currentSolution.TotalPenalty + currentSolution.TotalVehicleOperationTime;
                
                // Select operator using RL agent
                selectedOperator = rlAgent.SelectOperator(currentObjective, bestObjective, iter, MaxIterations);
                
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, selectedOperator);
                
                foreach (var neighbor in neighborhood.Take(TabuSize*10))
                {
                    bool isTabu = tabuList.Any(tabuSolution => tabuSolution.Equals(neighbor));
                    var objective = neighbor.TotalCost + neighbor.TotalPenalty + neighbor.TotalVehicleOperationTime;
                    if (isTabu && objective >= bestObjective)
                        continue;
                    if (objective < bestNeighborObjective)
                    {
                        bestNeighborObjective = objective;
                        bestNeighbor = neighbor;
                    }
                }
                
                if (bestNeighbor == null)
                {
                    break;
                }
                
                currentSolution = bestNeighbor;
                
                // Update RL agent with the outcome
                if (iter > 0)
                {
                    rlAgent.UpdateQValue(
                        previousObjective,
                        currentObjective,
                        bestObjective,
                        iter - 1,
                        iter,
                        MaxIterations,
                        selectedOperator
                    );
                }
                
                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;
                    Console.Write($"{iter}:{Math.Round(bestObjective)}({selectedOperator[0]}). ");
                }
                else
                {
                    notImprovingIterations++;
                }
                
                tabuList.Enqueue(currentSolution);
                if (tabuList.Count > TabuSize)
                    tabuList.Dequeue();
                    
                if (notImprovingIterations >= MaxIterations)
                {
                    Console.Write(",");
                    notImprovingIterations = 0;
                    currentSolution = NeighborhoodGeneratorLocation.GenerateRandomSolution(currentSolution.Routes, instance.Vehicles, instance);
                }
                
                previousObjective = currentObjective;
                iter++;
                
                // Print progress every 100 iterations
                if (iter % 100 == 0)
                {
                    Console.WriteLine($"\nIteration {iter}, Epsilon: {rlAgent.GetEpsilon():F4}, Time: {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
            }
            
            Console.WriteLine($"\nTabu Search with RL completed {iter} iterations in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine(rlAgent.GetStatistics());
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n\n");
            bestSolution.GreedyMetrics = GreedyMetrics;
            return bestSolution;
        }
        
        /// <summary>
        /// Train an RL model and save it to a file
        /// </summary>
        public static RLOperatorSelector TrainAndSaveRLModel(int MaxIterations, int TabuSize, Instance instance, int maxTime, string modelSavePath, int? seed = null)
        {
            Console.WriteLine("=== RL Model Training Mode ===");
            Console.WriteLine($"Training will run for {maxTime} seconds");
            
            // Train the model using runWithRL
            var solution = runWithRL(MaxIterations, TabuSize, instance, maxTime, seed);
            
            // Get the trained agent (we need to return it from runWithRL)
            // For now, we'll create a new method that returns both solution and agent
            return TrainRLModel(MaxIterations, TabuSize, instance, maxTime, modelSavePath, seed);
        }
        
        /// <summary>
        /// Train an RL model and save it, returning the trained agent
        /// </summary>
        private static RLOperatorSelector TrainRLModel(int MaxIterations, int TabuSize, Instance instance, int maxTime, string modelSavePath, int? seed)
        {
            // Initialize RL agent for training
            var rlAgent = new RLOperatorSelector(
                learningRate: 0.1,
                discountFactor: 0.9,
                epsilon: 1.0,
                epsilonDecay: 0.9995,
                epsilonMin: 0.1,
                seed: seed
            );

            Solution bestSolution = GreedyApproaches.generateGreedySolution(instance);
            bestSolution.calculateRoutesMetrics(instance);
            var bestObjective = bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime;
            
            Console.WriteLine($"TabuSize:{TabuSize} MaxIterations:{MaxIterations} MutationType: RL-based (Training)");
            Console.WriteLine($"Initial greedy solution objective: {Math.Round(bestObjective)}");
            Console.WriteLine($"RL Training enabled - maxTime: {maxTime}s");
            
            Solution currentSolution = bestSolution;
            Queue<Solution> tabuList = new Queue<Solution>();
            int notImprovingIterations = 0;
            int iter = 0;
            var stopwatch = Stopwatch.StartNew();
            maxTime *= 1000;
            
            double previousObjective = bestObjective;
            string selectedOperator = "swap";
            
            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                double currentObjective = currentSolution.TotalCost + currentSolution.TotalPenalty + currentSolution.TotalVehicleOperationTime;
                
                selectedOperator = rlAgent.SelectOperator(currentObjective, bestObjective, iter, MaxIterations);
                
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, selectedOperator);
                
                foreach (var neighbor in neighborhood.Take(TabuSize*10))
                {
                    bool isTabu = tabuList.Any(tabuSolution => tabuSolution.Equals(neighbor));
                    var objective = neighbor.TotalCost + neighbor.TotalPenalty + neighbor.TotalVehicleOperationTime;
                    if (isTabu && objective >= bestObjective)
                        continue;
                    if (objective < bestNeighborObjective)
                    {
                        bestNeighborObjective = objective;
                        bestNeighbor = neighbor;
                    }
                }
                
                if (bestNeighbor == null)
                {
                    break;
                }
                
                currentSolution = bestNeighbor;
                
                if (iter > 0)
                {
                    rlAgent.UpdateQValue(
                        previousObjective,
                        currentObjective,
                        bestObjective,
                        iter - 1,
                        iter,
                        MaxIterations,
                        selectedOperator
                    );
                }
                
                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;
                    Console.Write($"{iter}:{Math.Round(bestObjective)}({selectedOperator[0]}). ");
                }
                else
                {
                    notImprovingIterations++;
                }
                
                tabuList.Enqueue(currentSolution);
                if (tabuList.Count > TabuSize)
                    tabuList.Dequeue();
                    
                if (notImprovingIterations >= MaxIterations)
                {
                    Console.Write(",");
                    notImprovingIterations = 0;
                    currentSolution = NeighborhoodGeneratorLocation.GenerateRandomSolution(currentSolution.Routes, instance.Vehicles, instance);
                }
                
                previousObjective = currentObjective;
                iter++;
                
                if (iter % 100 == 0)
                {
                    Console.WriteLine($"\nIteration {iter}, Epsilon: {rlAgent.GetEpsilon():F4}, Time: {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
            }
            
            Console.WriteLine($"\nRL Training completed {iter} iterations in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine(rlAgent.GetStatistics());
            
            // Save the trained model
            rlAgent.SaveModel(modelSavePath);
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n\n");
            
            return rlAgent;
        }
    }
}
