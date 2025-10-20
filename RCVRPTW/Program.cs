// See https://aka.ms/new-console-template for more information

using RCVRPTW;

Instance instance = new Instance("pliki\\CTEST.txt");

for(int iterations = 10; iterations <= 10000; iterations *= 10)
{
    for(int tabuSize = 10; tabuSize <= 50; tabuSize += 10)
    {
        var startTime = DateTime.Now;
        var res = TabuSearch.run(iterations, tabuSize, instance);
        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        Console.WriteLine($"{iterations} {tabuSize} {res.TotalCost+res.TotalPenalty+res.TotalVehicleOperationTime} {duration.TotalMilliseconds}");
    }
}

//ScenarioGenerator generator = new ScenarioGenerator(instance.Locations);
//var scenarios = generator.GenerateManyScenarios(5);
//Console.WriteLine("Wygenerowane scenariusze zapotrzebowań:");
//foreach (var scenario in scenarios)
//{
//    Console.WriteLine($"Scenariusz {scenario.ScenarioId}:");
//    for (int i = 0; i < scenario.Demands.Count; i++)
//    {
//        Console.WriteLine($"  Lokalizacja {i}: Zapotrzebowanie = {scenario.Demands[i]:F2}");
//    }
//}
namespace RCVRPTW
{
    public class DemandScenario
    {
        public int ScenarioId { get; set; }
        public List<double> Demands { get; set; } // Zapotrzebowania dla każdej lokalizacji-klienta

        public DemandScenario(int scenarioId, List<double> demands)
        {
            ScenarioId = scenarioId;
            Demands = demands;
        }
    }

    public class ScenarioGenerator
    {
        private List<Location> _locations;

        public ScenarioGenerator(List<Location> locations)
        {
            _locations = locations;
        }

        public DemandScenario GenerateScenario(int scenarioId, Random rng)
        {
            var demands = new List<double>();
            foreach (var loc in _locations)
            {
                if (loc.Type == LocationType.Depot)
                    demands.Add(0.0);
                else
                    demands.Add(loc.SampleDemand(rng));
            }
            return new DemandScenario(scenarioId, demands);
        }

        public List<DemandScenario> GenerateManyScenarios(int count)
        {
            var rng = new Random();
            var scenarios = new List<DemandScenario>();
            for (int i = 0; i < count; i++)
                scenarios.Add(GenerateScenario(i, rng));
            return scenarios;
        }
    }
}