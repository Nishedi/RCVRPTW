using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    }
}
