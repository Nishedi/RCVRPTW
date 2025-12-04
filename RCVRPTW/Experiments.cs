using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    public class ExperimentResult
    {
        public int ScenarioId { get; set; }
        public string FileName { get; set; }
        public int Iterations { get; set; }
        public int TabuSize { get; set; }
        public int Repeat { get; set; }
        public int Seed { get; set; }
        public List<int> GTR {  get; set; }

        public double GreedyObjective { get; set; }
        public (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics { get; set; }
        public string MutationType { get; set; }
        public string AlgorithmType { get; set; } = "TabuSearch"; // "TabuSearch" or "RL"

        public double Objective { get; set; } // total cost + penalty + vehicleOpTime
        public double TotalCost { get; set; }
        public double TotalPenalty { get; set; }
        public double TotalVehicleOperationTime { get; set; }
        public int RoutesCount { get; set; }
        public double DurationMs { get; set; }
    }



    public static class ExperimentRunner
    {
        public static List<ExperimentResult> RunExperiments(
            List<Scenario> scenarios,
            int[] iterationsGrid,
            int[] tabuSizeGrid,
            string[] mutationtypes,
            string fileType,
            int repeats = 5,
            int baseSeed = 12345,
            bool parallel = true,
            int maxTime = 120,
            string defaultFilePath = "results_raw_"
            )
        {
            if (File.Exists($"{defaultFilePath}{fileType}.csv"))
            {
                File.Delete($"{defaultFilePath}{fileType}.csv");
            }
            var results = new List<ExperimentResult>();
            var lockObj = new object();

            var tasks = new List<Action>();
            Console.WriteLine("Starting experiments..." + scenarios.Count * iterationsGrid.Length * tabuSizeGrid.Length * repeats * mutationtypes.Length);
            foreach (var scen in scenarios)
            {
                for (int it = 0; it < iterationsGrid.Length; it++)
                    for (int ts = 0; ts < tabuSizeGrid.Length; ts++)
                    {
                        foreach (var mutationtype in mutationtypes)
                        {
                            int iterations = iterationsGrid[it];
                            int tabuSize = tabuSizeGrid[ts];

                            for (int rep = 0; rep < repeats; rep++)
                            {
                                int seed = baseSeed + scen.ScenarioId * 1000 + iterations * 10 + tabuSize * 100 + rep;
                                Action work = () =>
                                {
                                    var rng = new Random(seed);
                                    var sw = Stopwatch.StartNew();

                                    var instance = scen.Instance;
                                    var solution = TabuSearch.run(iterations, tabuSize, instance, mutationtype, maxTime: maxTime);
                                    sw.Stop();

                                    var res = new ExperimentResult
                                    {
                                        ScenarioId = scen.ScenarioId,
                                        FileName = scen.Instance.FileName,
                                        Iterations = iterations,
                                        TabuSize = tabuSize,
                                        MutationType = mutationtype,
                                        Repeat = rep,
                                        Seed = seed,
                                        GreedyObjective = solution.GreedyMetrics.greedyTotalCost + solution.GreedyMetrics.greedyTotalPenalty + solution.GreedyMetrics.greedyVOT,
                                        Objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime,
                                        TotalCost = solution.TotalCost,
                                        TotalPenalty = solution.TotalPenalty,
                                        TotalVehicleOperationTime = solution.TotalVehicleOperationTime,
                                        RoutesCount = solution.Routes.Count,
                                        DurationMs = sw.Elapsed.TotalMilliseconds,
                                        GreedyMetrics = solution.GreedyMetrics,
                                        GTR = solution.Routes.SelectMany(r => r.Stops).Select(loc => loc.Id).ToList()
                                    };

                                    lock (lockObj)
                                    {
                                        results.Add(res);
                                        AppendResultToCsv($"{defaultFilePath}{fileType}.csv", res);
                                    }
                                };

                                tasks.Add(work);
                            }
                        }
                    }
            }

            int total = tasks.Count;
            int completed = 0;

            if (parallel)
            {
                Parallel.ForEach(tasks, t =>
                {
                    t();
                    int now = System.Threading.Interlocked.Increment(ref completed);
                    Console.Write($"\rDone {now}/{total}");
                });
            }
            else
            {
                foreach (var t in tasks)
                {
                    t();
                    int now = System.Threading.Interlocked.Increment(ref completed);

                    Console.Write($"\rDone {now}/{total} ");
                }
            }

            return results;
        }

        public static List<ExperimentResult> RunRLExperiments(
            List<Scenario> scenarios,
            int[] trainingEpisodesGrid,
            string fileType,
            int repeats = 5,
            int baseSeed = 12345,
            bool parallel = true,
            string defaultFilePath = "results_raw_rl_"
            )
        {
            if (File.Exists($"{defaultFilePath}{fileType}.csv"))
            {
                File.Delete($"{defaultFilePath}{fileType}.csv");
            }
            var results = new List<ExperimentResult>();
            var lockObj = new object();

            var tasks = new List<Action>();
            Console.WriteLine("Starting RL experiments..." + scenarios.Count * trainingEpisodesGrid.Length * repeats);
            foreach (var scen in scenarios)
            {
                foreach (int episodes in trainingEpisodesGrid)
                {
                    for (int rep = 0; rep < repeats; rep++)
                    {
                        int seed = baseSeed + scen.ScenarioId * 1000 + episodes * 10 + rep;
                        Action work = () =>
                        {
                            var sw = Stopwatch.StartNew();

                            var instance = scen.Instance;
                            
                            // Get greedy baseline for comparison
                            var greedySolution = GreedyApproaches.generateGreedySolution(instance);
                            greedySolution.calculateRoutesMetrics(instance);
                            var greedyMetrics = (greedySolution.TotalCost, greedySolution.TotalPenalty, greedySolution.TotalVehicleOperationTime);
                            
                            // Run RL solver
                            var solution = RLSolver.Run(instance, trainingEpisodes: episodes, seed: seed);
                            sw.Stop();

                            var res = new ExperimentResult
                            {
                                ScenarioId = scen.ScenarioId,
                                FileName = scen.Instance.FileName,
                                Iterations = episodes, // Using episodes instead of iterations
                                TabuSize = 0, // Not applicable for RL
                                MutationType = "RL-QLearning",
                                AlgorithmType = "RL",
                                Repeat = rep,
                                Seed = seed,
                                GreedyObjective = greedyMetrics.TotalCost + greedyMetrics.TotalPenalty + greedyMetrics.TotalVehicleOperationTime,
                                Objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime,
                                TotalCost = solution.TotalCost,
                                TotalPenalty = solution.TotalPenalty,
                                TotalVehicleOperationTime = solution.TotalVehicleOperationTime,
                                RoutesCount = solution.Routes.Count,
                                DurationMs = sw.Elapsed.TotalMilliseconds,
                                GreedyMetrics = greedyMetrics,
                                GTR = solution.Routes.SelectMany(r => r.Stops).Select(loc => loc.Id).ToList()
                            };

                            lock (lockObj)
                            {
                                results.Add(res);
                                AppendResultToCsv($"{defaultFilePath}{fileType}.csv", res);
                            }
                        };

                        tasks.Add(work);
                    }
                }
            }

            int total = tasks.Count;
            int completed = 0;

            if (parallel)
            {
                Parallel.ForEach(tasks, t =>
                {
                    t();
                    int now = System.Threading.Interlocked.Increment(ref completed);
                    Console.Write($"\rDone {now}/{total}");
                });
            }
            else
            {
                foreach (var t in tasks)
                {
                    t();
                    int now = System.Threading.Interlocked.Increment(ref completed);

                    Console.Write($"\rDone {now}/{total} ");
                }
            }

            return results;
        }

        private static void AppendResultToCsv(string path, ExperimentResult res)
        {
            var header = "ScenarioId;Filename;Iterations;TabuSize;MutationType;AlgorithmType;Repeat;Seed;GreedyOjective;GreedyTotalCost;GreedyTotalPenalty;GreedyTotalVehicleOperationTime;Objective;TotalCost;TotalPenalty;TotalVehicleOperationTime;RoutesCount;DurationMs;GTR";
            var exists = File.Exists(path);
            using (var sw = new StreamWriter(path, append: true))
            {
                if (!exists) sw.WriteLine(header);
                string result = $"{res.ScenarioId};{res.FileName};{res.Iterations};{res.TabuSize};{res.MutationType};{res.AlgorithmType};{res.Repeat};{res.Seed};{res.GreedyObjective};{res.GreedyMetrics.greedyTotalCost};{res.GreedyMetrics.greedyTotalPenalty};{res.GreedyMetrics.greedyVOT};{res.Objective};{res.TotalCost};{res.TotalPenalty};{res.TotalVehicleOperationTime};{res.RoutesCount};{res.DurationMs};{string.Join(",", res.GTR)}";
                //Console.WriteLine($"{res.FileName}:{res.DurationMs}");
                sw.WriteLine(result);
            }
        }
    }
}
