using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class ABC
    {
        public static Solution run(int FoodSourcesCount, int Limit, Instance instance, string mutationtype = "swap", int maxTime = 120, int all_or_one = 1)
        {
            if(all_or_one== 1)
                Console.WriteLine($"Running ABC with {FoodSourcesCount} food sources, limit {Limit}, mutation type {mutationtype}, max time {maxTime} seconds, using one random neighbor.");
            else
                Console.WriteLine($"Running ABC with {FoodSourcesCount} food sources, limit {Limit}, mutation type {mutationtype}, max time {maxTime} seconds, using best neighbor.");
            Console.WriteLine();
            List<Solution> foodSources = new List<Solution>();
            int[] trials = new int[FoodSourcesCount]; 

            Solution GreedySolution = GreedyApproaches.generateGreedySolution(instance);
            GreedySolution.calculateRoutesMetrics(instance);
            (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics = (GreedySolution.TotalCost, GreedySolution.TotalPenalty, GreedySolution.TotalVehicleOperationTime);

            foodSources.Add(GreedySolution);
            for (int i = 1; i < FoodSourcesCount; i++)
            {
                var isol = NeighborhoodGeneratorLocation.GenerateRandomSolution(GreedySolution.Routes, instance.Vehicles, instance);
                
                isol.calculateRoutesMetrics(instance);
                foodSources.Add(isol);
            }

            Solution bestSolution = foodSources.OrderBy(s => GetFitness(s)).First();
            double bestObjective = GetObjective(bestSolution);

            var stopwatch = Stopwatch.StartNew();
            maxTime *= 1000;
            Random rng = new Random();
            int iter = 0;

            while (stopwatch.ElapsedMilliseconds <= maxTime)
            {
                for (int i = 0; i < FoodSourcesCount; i++)
                {
                    Solution neighbor = GenerateNeighbor(foodSources[i], mutationtype, instance, rng, all_or_one);
                    if (GetFitness(neighbor) < GetFitness(foodSources[i]))
                    {
                        foodSources[i] = neighbor;
                        trials[i] = 0;
                    }
                    else
                    {
                        trials[i]++;
                    }
                }

                double totalFitness = foodSources.Sum(s => 1.0 / (1.0 + GetObjective(s)));
                for (int i = 0; i < FoodSourcesCount; i++)
                {
                    double prob = (1.0 / (1.0 + GetObjective(foodSources[i]))) / totalFitness;
                    if (rng.NextDouble() < prob)
                    {
                        Solution neighbor = GenerateNeighbor(foodSources[i], mutationtype, instance, rng, all_or_one);
                        if (GetFitness(neighbor) < GetFitness(foodSources[i]))
                        {
                            foodSources[i] = neighbor;
                            trials[i] = 0;
                        }
                        else
                        {
                            trials[i]++;
                        }
                    }
                }

                foreach (var sol in foodSources)
                {
                    double obj = GetObjective(sol);
                    if (obj < bestObjective)
                    {
                        bestObjective = obj;
                        bestSolution = sol;
                        Console.Write($"{iter}:{Math.Round(bestObjective)}. ");
                    }
                }

                for (int i = 0; i < FoodSourcesCount; i++)
                {
                    if (trials[i] >= Limit)
                    {
                        foodSources[i] = NeighborhoodGeneratorLocation.GenerateRandomSolution(bestSolution.Routes, instance.Vehicles, instance);
                        trials[i] = 0;
                    }
                }

                iter++;
            }
            bestSolution.GreedyMetrics = GreedyMetrics;
            Console.WriteLine($"\nABC completed {iter} iterations. Greedy {bestSolution.GreedyMetrics.greedyTotalCost}. Bee best solution {bestSolution.TotalCost + bestSolution.TotalPenalty + bestSolution.TotalVehicleOperationTime}");
            
            return bestSolution;
        }

        private static double GetObjective(Solution s)
        {
            return s.TotalCost + s.TotalPenalty + s.TotalVehicleOperationTime;
        }

        private static double GetFitness(Solution s)
        {
            return GetObjective(s);
        }

        private static Solution GenerateNeighbor(Solution current, string mutationtype, Instance instance, Random rng, int all_or_one)
        {
            string mutation = mutationtype == "random" ?
                (new[] { "swap", "invert", "insert", "2opt", "oropt", "rand" })[rng.Next(6)] : mutationtype;
            List<Solution> neighborhood = null;
            if(all_or_one == 1)
                neighborhood = NeighborhoodGeneratorLocation.GenerateOneSwap(current.Routes, instance.Vehicles, instance, mutation);
            else
                neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(current.Routes, instance.Vehicles, instance, mutation);
            neighborhood.Sort((s1, s2) => GetFitness(s1).CompareTo(GetFitness(s2))); // Sortujemy sąsiedztwo po fitnessie
            //zrobić wersje z jednym losowym sąsiadem i z najlepszym sąsiadem
            return neighborhood.ElementAt(rng.Next(Math.Min(10, neighborhood.Count())));
        }
    }
}
