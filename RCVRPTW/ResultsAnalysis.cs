using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{

    public static class ResultsAggregator
    {
        public static void SaveAggregates(string outputCsv, List<ExperimentResult> raw)
        {
            var groups = raw.GroupBy(r => new { r.ScenarioId, r.Iterations, r.TabuSize })
                            .Select(g => new
                            {
                                g.Key.ScenarioId,
                                g.Key.Iterations,
                                g.Key.TabuSize,
                                Runs = g.Count(),
                                MeanGreedyObjective = g.Average(x => x.GreedyObjective),
                                MeanObjective = g.Average(x => x.Objective),
                                StdObjective = StdDev(g.Select(x => x.Objective)),
                                MedianObjective = Median(g.Select(x => x.Objective)),
                                BestObjective = g.Min(x => x.Objective),
                                WorstObjective = g.Max(x => x.Objective),
                                MeanDurationMs = g.Average(x => x.DurationMs),
                            }).ToList();

            using (var sw = new StreamWriter(outputCsv))
            {
                sw.WriteLine("ScenarioId,Iterations,TabuSize,Runs,MeanGreedyObjective,MeanObjective,StdObjective,MedianObjective,BestObjective,WorstObjective,MeanDurationMs");
                foreach (var a in groups)
                {
                    sw.WriteLine($"{a.ScenarioId},{a.Iterations},{a.TabuSize},{a.Runs},{a.MeanGreedyObjective},{a.MeanObjective},{a.StdObjective},{a.MedianObjective},{a.BestObjective},{a.WorstObjective},{a.MeanDurationMs}");
                }
            }
        }

        private static double StdDev(IEnumerable<double> values)
        {
            var arr = values.ToArray();
            if (arr.Length <= 1) return 0;
            var avg = arr.Average();
            var sum = arr.Sum(v => (v - avg) * (v - avg));
            return Math.Sqrt(sum / (arr.Length - 1));
        }

        private static double Median(IEnumerable<double> values)
        {
            var arr = values.OrderBy(x => x).ToArray();
            int n = arr.Length;
            if (n == 0) return 0;
            if (n % 2 == 1) return arr[n / 2];
            return (arr[n / 2 - 1] + arr[n / 2]) / 2.0;
        }
    }

    public class ScenarioFeatures
    {
        public int ScenarioId { get; set; }
        public double[] Features { get; set; } // feature vector
    }

    public static class ScenarioAnalyzer
    {
        public static ScenarioFeatures ExtractFeatures(Scenario scen)
        {
            var inst = scen.Instance;
            var locs = inst.Locations; // dostosuj do swojej klasy Instance

            int n = locs.Count - 1; // bez deponu
            double meanDemand = locs.Where(l => l.Id != 0).Average(l => l.DemandMean);
            double stdDemand = Math.Sqrt(locs.Where(l => l.Id != 0).Select(l => Math.Pow(l.DemandMean - meanDemand, 2)).Average());
            double avgWindowLen = locs.Where(l => l.Id != 0).Average(l => l.TimeWindow.End - l.TimeWindow.Start);
            double fleetCapacity = inst.Vehicles.Sum(v => v.Capacity);
            double totalDemand = locs.Where(l => l.Id != 0).Sum(l => l.DemandMean);
            double demandCapacityRatio = totalDemand / Math.Max(1e-6, fleetCapacity);

            // możesz dodać geograficzne statystyki (stddev of coords), etc.
            var features = new double[] { n, meanDemand, stdDemand, avgWindowLen, demandCapacityRatio };
            return new ScenarioFeatures { ScenarioId = scen.ScenarioId, Features = features };
        }

        // Prosty KMeans (Euclidean). Zwraca przypisanie: dict scenarioId -> clusterId
        public static Dictionary<int, int> KMeansCluster(List<ScenarioFeatures> feats, int k, int maxIter = 100, Random rng = null)
        {
            rng ??= new Random();
            int m = feats.Count;
            int dim = feats[0].Features.Length;

            // inicjalizuj centroidy losowo (wybierz k różnych punktów)
            var centroids = new List<double[]>();
            var chosen = new HashSet<int>();
            while (centroids.Count < k)
            {
                int idx = rng.Next(m);
                if (chosen.Add(idx)) centroids.Add((double[])feats[idx].Features.Clone());
            }

            int[] labels = new int[m];
            for (int iter = 0; iter < maxIter; iter++)
            {
                bool changed = false;
                // przypisz
                for (int i = 0; i < m; i++)
                {
                    double bestD = double.MaxValue; int bestC = 0;
                    for (int c = 0; c < k; c++)
                    {
                        double d = EuclideanSquared(feats[i].Features, centroids[c]);
                        if (d < bestD) { bestD = d; bestC = c; }
                    }
                    if (labels[i] != bestC) { labels[i] = bestC; changed = true; }
                }
                // przelicz centroidy
                for (int c = 0; c < k; c++)
                {
                    var members = feats.Where((f, idx) => labels[idx] == c).ToList();
                    if (members.Count == 0) continue;
                    double[] mean = new double[dim];
                    foreach (var mbr in members)
                        for (int d = 0; d < dim; d++) mean[d] += mbr.Features[d];
                    for (int d = 0; d < dim; d++) mean[d] /= members.Count;
                    centroids[c] = mean;
                }
                if (!changed) break;
            }

            var mapping = new Dictionary<int, int>();
            for (int i = 0; i < feats.Count; i++) mapping[feats[i].ScenarioId] = labels[i];
            return mapping;
        }

        private static double EuclideanSquared(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double d = a[i] - b[i];
                s += d * d;
            }
            return s;
        }
    }
}
