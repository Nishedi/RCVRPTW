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
        // --- Metoda bez RL (pozostawiona bez zmian) ---
        public static Solution run(int MaxIterations, int TabuSize, Instance instance, string mutationtype = "swap", int maxTime = 120)
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
            Random rng = new Random();
            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                // Zakładamy, że NeighborhoodGeneratorLocation.GenerateAllSwaps może przyjąć mutationtype
                string mutation = "swap";
                if (mutationtype == "random")
                {
                    string [] mutations = { "swap", "invert", "insert", "2opt", "oropt", "rand" };
                    mutation = mutations[rng.Next(mutations.Length)];
                }
                else
                {
                    mutation = mutationtype;
                }
                    var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, mutation);

                foreach (var neighbor in neighborhood.Take(TabuSize * 10))
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
        // ---------------------------------------------------------------------------------------------------

        /// <summary>
        /// Run Tabu Search with RL-based operator selection
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
                    epsilon: 1.0,               // Start with full exploration
                    epsilonDecay: 0.999,        // Zmieniono na wolniejszą dekay'ę
                    epsilonMin: 0.01,           // Zmieniono na niższą wartość minimalną, aby RL dłużej miał wpływ
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

            // --- Dodano śledzenie stagnacji ---
            int iterationsSinceBestImprovement = 0;
            // ------------------------------------

            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                double currentObjective = currentSolution.TotalCost + currentSolution.TotalPenalty + currentSolution.TotalVehicleOperationTime;

                // --- 1. Wybór operatora z uwzględnieniem stagnacji ---
                selectedOperator = rlAgent.SelectOperator(
                    currentObjective,
                    bestObjective,
                    iter,
                    MaxIterations,
                    iterationsSinceBestImprovement);
                // -----------------------------------------------------

                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                // Użycie wybranego operatora do generowania sąsiedztwa
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, selectedOperator);

                foreach (var neighbor in neighborhood.Take(TabuSize * 10))
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

                // --- 2. Aktualizacja RL (tylko od drugiej iteracji) z uwzględnieniem stagnacji ---
                if (iter > 0)
                {
                    rlAgent.UpdateQValue(
                        previousObjective,
                        bestNeighborObjective, // Używamy objective najlepszego sąsiada jako aktualnego celu
                        bestObjective,
                        iter - 1,
                        iter,
                        MaxIterations,
                        selectedOperator,
                        // Przekazujemy stan stagnacji: poprzedni i nowy (przed ewentualnym wyzerowaniem)
                        iterationsSinceBestImprovement - 1,
                        iterationsSinceBestImprovement
                    );
                }
                // ----------------------------------------------------------------------------------

                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;

                    // --- Zerowanie licznika stagnacji po poprawie ---
                    iterationsSinceBestImprovement = 0;
                    // -----------------------------------------------

                    Console.Write($"{iter}:{Math.Round(bestObjective)}({selectedOperator[0]}). ");
                }
                else
                {
                    notImprovingIterations++;

                    // --- Inkrementacja licznika stagnacji ---
                    iterationsSinceBestImprovement++;
                    // ----------------------------------------
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
                    Console.WriteLine($"\nIteration {iter}, Epsilon: {rlAgent.GetEpsilon():F4}, Stagnation: {iterationsSinceBestImprovement}, Time: {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
            }

            Console.WriteLine($"\nTabu Search with RL completed {iter} iterations in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine(rlAgent.GetStatistics());
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n\n");
            bestSolution.GreedyMetrics = GreedyMetrics;
            return bestSolution;
        }

        public static Solution runWithRL_epoc(int MaxIterations, int TabuSize, Instance instance, int maxTime = 3600, int? seed = null, string? modelPath = null)
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
                    epsilon: 1.0,               // Start with full exploration
                    epsilonDecay: 0.999,        // Zmieniono na wolniejszą dekay'ę
                    epsilonMin: 0.01,           // Zmieniono na niższą wartość minimalną, aby RL dłużej miał wpływ
                    seed: seed
                );
            }

            Solution bestSolution = GreedyApproaches.generateGreedySolution(instance);
            bestSolution.calculateRoutesMetrics(instance);
            (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics = (bestSolution.TotalCost, bestSolution.TotalPenalty, bestSolution.TotalVehicleOperationTime);
            var bestObjective = bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime;
            Console.WriteLine($"TabuSize:{TabuSize} MaxIterations:{MaxIterations} MutationType: RL-based " +
                $"Initial greedy solution objective: {Math.Round(bestObjective)} epochs");
            Console.WriteLine($"RL Training enabled - maxTime: {maxTime}s");

            Solution currentSolution = bestSolution;
            Queue<Solution> tabuList = new Queue<Solution>();
            int notImprovingIterations = 0;
            int iter = 0;
            var stopwatch = Stopwatch.StartNew();
            maxTime *= 1000;

            double previousObjective = bestObjective;
            string selectedOperator = "swap";

            // --- Dodano śledzenie stagnacji ---
            int iterationsSinceBestImprovement = 0;
            // ------------------------------------

            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                double currentObjective = currentSolution.TotalCost + currentSolution.TotalPenalty + currentSolution.TotalVehicleOperationTime;

                // --- 1. Wybór operatora z uwzględnieniem stagnacji ---
                if (iter % 75 == 0) { 
                selectedOperator = rlAgent.SelectOperator(
                    currentObjective,
                    bestObjective,
                    iter,
                    MaxIterations,
                    iterationsSinceBestImprovement);
                    // -----------------------------------------------------
                }
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                // Użycie wybranego operatora do generowania sąsiedztwa
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, selectedOperator);

                foreach (var neighbor in neighborhood.Take(TabuSize * 10))
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

                // --- 2. Aktualizacja RL (tylko od drugiej iteracji) z uwzględnieniem stagnacji ---
                if (iter > 0)
                {
                    rlAgent.UpdateQValue(
                        previousObjective,
                        bestNeighborObjective, // Używamy objective najlepszego sąsiada jako aktualnego celu
                        bestObjective,
                        iter - 1,
                        iter,
                        MaxIterations,
                        selectedOperator,
                        // Przekazujemy stan stagnacji: poprzedni i nowy (przed ewentualnym wyzerowaniem)
                        iterationsSinceBestImprovement - 1,
                        iterationsSinceBestImprovement
                    );
                }
                // ----------------------------------------------------------------------------------

                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;

                    // --- Zerowanie licznika stagnacji po poprawie ---
                    iterationsSinceBestImprovement = 0;
                    // -----------------------------------------------

                    Console.Write($"{iter}:{Math.Round(bestObjective)}({selectedOperator[0]}). ");
                }
                else
                {
                    notImprovingIterations++;

                    // --- Inkrementacja licznika stagnacji ---
                    iterationsSinceBestImprovement++;
                    // ----------------------------------------
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
                    Console.WriteLine($"\nIteration {iter}, Epsilon: {rlAgent.GetEpsilon():F4}, Stagnation: {iterationsSinceBestImprovement}, Time: {stopwatch.Elapsed.TotalSeconds:F1}s");
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

            // Train the model and save it
            return TrainRLModel(MaxIterations, TabuSize, instance, maxTime, modelSavePath, seed);
        }

        /// <summary>
        /// Train an RL model and save it, returning the trained agent
        /// </summary>
        private static RLOperatorSelector TrainRLModel(int MaxIterations, int TabuSize, Instance instance, int maxTime, string modelSavePath, int? seed)
        {
            // Set up metrics logging paths
            string? directory = Path.GetDirectoryName(modelSavePath);
            string baseFileName = Path.GetFileNameWithoutExtension(modelSavePath);
            string metricsLogPath = Path.Combine(directory ?? ".", $"{baseFileName}_metrics.csv");
            string summaryPath = Path.Combine(directory ?? ".", $"{baseFileName}_summary.txt");
            string qTablePath = Path.Combine(directory ?? ".", $"{baseFileName}_qtable.csv");

            // Initialize RL agent for training with metrics tracking enabled
            var rlAgent = new RLOperatorSelector(
                learningRate: 0.1,
                discountFactor: 0.9,
                epsilon: 1.0,
                epsilonDecay: 0.999, // Zmieniono na wolniejszą dekay'ę
                epsilonMin: 0.01,
                seed: seed,
                trackMetrics: true,
                metricsLogPath: metricsLogPath
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

            // --- Dodano śledzenie stagnacji ---
            int iterationsSinceBestImprovement = 0;
            // ------------------------------------

            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                double currentObjective = currentSolution.TotalCost + currentSolution.TotalPenalty + currentSolution.TotalVehicleOperationTime;

                // --- 1. Wybór operatora z uwzględnieniem stagnacji ---
                selectedOperator = rlAgent.SelectOperator(
                    currentObjective,
                    bestObjective,
                    iter,
                    MaxIterations,
                    iterationsSinceBestImprovement);
                // -----------------------------------------------------

                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                // Użycie wybranego operatora do generowania sąsiedztwa
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance, selectedOperator);

                foreach (var neighbor in neighborhood.Take(TabuSize * 10))
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

                // --- 2. Aktualizacja RL (tylko od drugiej iteracji) z uwzględnieniem stagnacji ---
                if (iter > 0)
                {
                    rlAgent.UpdateQValue(
                        previousObjective,
                        bestNeighborObjective, // Używamy objective najlepszego sąsiada jako aktualnego celu
                        bestObjective,
                        iter - 1,
                        iter,
                        MaxIterations,
                        selectedOperator,
                        // Przekazujemy stan stagnacji: poprzedni i nowy (przed ewentualnym wyzerowaniem)
                        iterationsSinceBestImprovement - 1,
                        iterationsSinceBestImprovement
                    );
                }
                // ----------------------------------------------------------------------------------

                if (bestNeighborObjective < bestObjective)
                {
                    bestSolution = bestNeighbor;
                    bestObjective = bestNeighborObjective;
                    notImprovingIterations = 0;

                    // --- Zerowanie licznika stagnacji po poprawie ---
                    iterationsSinceBestImprovement = 0;
                    // -----------------------------------------------

                    Console.Write($"{iter}:{Math.Round(bestObjective)}({selectedOperator[0]}). ");
                }
                else
                {
                    notImprovingIterations++;

                    // --- Inkrementacja licznika stagnacji ---
                    iterationsSinceBestImprovement++;
                    // ----------------------------------------
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
                    Console.WriteLine($"\nIteration {iter}, Epsilon: {rlAgent.GetEpsilon():F4}, Stagnation: {iterationsSinceBestImprovement}, Time: {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
            }

            Console.WriteLine($"\nRL Training completed {iter} iterations in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine(rlAgent.GetStatistics());

            // Save the trained model
            rlAgent.SaveModel(modelSavePath);

            // Save training metrics and summary
            rlAgent.SaveTrainingSummary(summaryPath, iter, stopwatch.Elapsed.TotalSeconds);
            rlAgent.ExportQTableToCSV(qTablePath);

            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------\n\n");

            return rlAgent;
        }
    }
}