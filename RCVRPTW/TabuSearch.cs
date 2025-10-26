using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class TabuSearch
    {
        public static Solution run(int MaxIterations, int TabuSize, Instance instance, string mutationtype="swap")
        {
            Solution bestSolution = GreedyApproaches.generateGreedySolution(instance);
            bestSolution.calculateRoutesMetrics(instance.DistanceMatrix);
            (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics = (bestSolution.TotalCost, bestSolution.TotalPenalty, bestSolution.TotalVehicleOperationTime);
            var bestObjective = bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime;
            Solution currentSolution = bestSolution;
            Queue<Solution> tabuList = new Queue<Solution>();
            int notImprovingIterations = 0;
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                Solution bestNeighbor = null;
                double bestNeighborObjective = double.MaxValue;
                var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance.DistanceMatrix, mutationtype);
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
                }
                else
                {
                    notImprovingIterations++;
                }
                tabuList.Enqueue(currentSolution);
                if (tabuList.Count > TabuSize)
                    tabuList.Dequeue();
                if (notImprovingIterations >= 0.25 * MaxIterations)
                {
                    notImprovingIterations = 0;
                    currentSolution = NeighborhoodGeneratorLocation.GenerateRandomSolution(currentSolution.Routes, instance.Vehicles, instance.DistanceMatrix);
                }
            }
            bestSolution.GreedyMetrics = GreedyMetrics;
            return bestSolution;
        }
    }
}
