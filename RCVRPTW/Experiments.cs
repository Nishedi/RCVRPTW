using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            int repeats = 5,
            int baseSeed = 12345,
            bool parallel = true)
        {
            var results = new List<ExperimentResult>();
            var lockObj = new object();

            var tasks = new List<Action>();
            Console.WriteLine("Starting experiments..." + scenarios.Count * iterationsGrid.Length * tabuSizeGrid.Length * repeats);
            foreach (var scen in scenarios)
            {
                for (int it = 0; it < iterationsGrid.Length; it++)
                    for (int ts = 0; ts < tabuSizeGrid.Length; ts++)
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
                                var solution = TabuSearch.run(iterations, tabuSize, instance);
                                sw.Stop();
                                
                                var res = new ExperimentResult
                                {
                                    ScenarioId = scen.ScenarioId,
                                    FileName = scen.Instance.FileName,
                                    Iterations = iterations,
                                    TabuSize = tabuSize,
                                    Repeat = rep,
                                    Seed = seed,
                                    GreedyObjective = solution.GreedyMetrics.greedyTotalCost + solution.GreedyMetrics.greedyTotalPenalty + solution.GreedyMetrics.greedyVOT,
                                    Objective = solution.TotalCost + solution.TotalPenalty + solution.TotalVehicleOperationTime,
                                    TotalCost = solution.TotalCost,
                                    TotalPenalty = solution.TotalPenalty,
                                    TotalVehicleOperationTime = solution.TotalVehicleOperationTime,
                                    RoutesCount = solution.Routes.Count,
                                    DurationMs = sw.Elapsed.TotalMilliseconds,
                                    GTR = solution.Routes.SelectMany(r => r.Stops).Select(loc => loc.Id).ToList()
                                };

                                lock (lockObj)
                                {
                                    results.Add(res);
                                    AppendResultToCsv("results_raw.csv", res);
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

                    Console.Write($"\rDone {now}/{total}");
                }
            }

            return results;
        }

        private static void AppendResultToCsv(string path, ExperimentResult res)
        {
            var header = "ScenarioId;Filename;Iterations;TabuSize;Repeat;Seed;GreedyOjective;Objective;TotalCost;TotalPenalty;TotalVehicleOperationTime;RoutesCount;DurationMs;GTR";
            var exists = File.Exists(path);
            using (var sw = new StreamWriter(path, append: true))
            {
                if (!exists) sw.WriteLine(header);
                string result = $"{res.ScenarioId};{res.FileName};{res.Iterations};{res.TabuSize};{res.Repeat};{res.Seed};{res.GreedyObjective};{res.Objective};{res.TotalCost};{res.TotalPenalty};{res.TotalVehicleOperationTime};{res.RoutesCount};{res.DurationMs};{string.Join(",", res.GTR)}";
                //Console.WriteLine($"{res.FileName}:{res.DurationMs}");
                sw.WriteLine(result);
            }
        }
    }
}
