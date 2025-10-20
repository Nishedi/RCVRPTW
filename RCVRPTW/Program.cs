// See https://aka.ms/new-console-template for more information

using RCVRPTW;

Instance instance = new Instance("pliki\\CTEST.txt");
List<Location> locations = new List<Location>();
List<Route> routes = new List<Route>();


//dodac sprawdzenie czy czasem lepiej poczekac zeby nie było kary czy wykonywać z karą!!!
var (gd, VehicleStarts) = createGreedyGTR(instance);
Solution test = generateGreedySolution(gd, VehicleStarts, instance.Vehicles);
var (totalCost, totalPenalty, costOfTrucks, vehicleOperationTime) = calculateTotalCost(instance.DistanceMatrix, test);

locations.Add(instance.Locations[0]);
locations.Add(instance.Locations[8]);
locations.Add(instance.Locations[0]);
var (bc, bp, bvot, bst) = NeighborhoodGeneratorLocation.bestStartTime(locations, instance.DistanceMatrix);
var nbg = NeighborhoodGeneratorLocation.GenerateAllSwaps(test.Routes, instance.Vehicles, instance.DistanceMatrix);


for(int iterations = 10; iterations <= 10000; iterations *= 10)
{
    for(int tabuSize = 10; tabuSize <= 50; tabuSize += 10)
    {
        var startTime = DateTime.Now;
        var res = TabuSearch(iterations, tabuSize, instance);
        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        Console.WriteLine($"{iterations} {tabuSize} {res.TotalCost+res.TotalPenalty+res.TotalVehicleOperationTime} {duration.TotalMilliseconds}");
        if(res.TotalCost + res.TotalPenalty + res.TotalVehicleOperationTime < 300)
        {
            var x = res.TotalCost + res.TotalPenalty + res.TotalVehicleOperationTime;
        }
    }
  
}




(double totalCost, double totalPenalty, double costOfTrucks, double vehicleOperationTime) calculateTotalCost(double[,] distanceMatrix, Solution solution)
{
    var routes = solution.Routes;
    double cost = 0.0;
    double penalty = 0.0;
    double vehicleTime = 0.0;// koszt operowania tez musi byc uwzgledniony
    double costOfTrucks = 0.0;
    double vehicleOperationTime = 0.0;
    foreach (var route in routes)//a moze czasem lepiej sie zaczac wczesniej jak mozna, zeby potem miec wieksze mozliwosci???
    {
        vehicleTime = route.StartTime;
        for (int r = 1; r < route.Stops.Count; r++)
        {
            Location actualCity = route.Stops[r];
            Location prevCity = route.Stops[r - 1];
            cost += distanceMatrix[prevCity.Id, actualCity.Id];
            vehicleTime += distanceMatrix[prevCity.Id, actualCity.Id];
            if (vehicleTime < actualCity.TimeWindow.Start)
            {
                double costOfWaiting = actualCity.TimeWindow.Start - vehicleTime;
                double toEarlyPenalty = Math.Min(costOfWaiting, actualCity.ServiceTime) * 1; //tutaj mozna dodac jakis wspolczynnik jakby byly różne kary za zbyt wczesne dotarcie
                if (costOfWaiting <= toEarlyPenalty)//to na sytuacje gdyby kara byla wieksza niz 1 * to co się wykonywało przed czasem
                {
                    vehicleTime += costOfWaiting;
                }
                else
                {
                    penalty += toEarlyPenalty;
                }
            }
            vehicleTime += actualCity.ServiceTime;
            if (vehicleTime > actualCity.TimeWindow.End)
            {
                double toLatePenalty = Math.Min(actualCity.ServiceTime, vehicleTime - actualCity.TimeWindow.End);
                penalty += toLatePenalty;
            }
        }
        vehicleOperationTime += vehicleTime - route.StartTime;
    }
    return (cost, penalty, costOfTrucks, vehicleOperationTime);
}

Solution TabuSearch(int MaxIterations, int TabuSize, Instance instance)
{
    var (greedyGTR, VehicleStarts) = createGreedyGTR(instance);
    Solution bestSolution = generateGreedySolution(greedyGTR, VehicleStarts, instance.Vehicles);
    bestSolution.calculateRoutesMetrics(instance.DistanceMatrix);
    var bestObjective = bestSolution.TotalCost+bestSolution.TotalPenalty+bestSolution.TotalVehicleOperationTime;
    Solution currentSolution = bestSolution;

    Queue<Solution> tabuList = new Queue<Solution>();
    int notImprovingIterations = 0;
    for (int iter = 0; iter < MaxIterations; iter++)
    {
        Solution bestNeighbor = null;
        double bestNeighborObjective = double.MaxValue;
        var neighborhood = NeighborhoodGeneratorLocation.GenerateAllSwaps(currentSolution.Routes, instance.Vehicles, instance.DistanceMatrix);
        foreach(var neighbor in neighborhood)
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
            break;
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



    return bestSolution;
}

Solution generateGreedySolution(List<Location> greedyGTR, List<double> vehicleStarts, List<Vehicle> Vehicles) //wprowadzic czekanie jezeli sie oplaca
{
    List<Route> routes = new List<Route>();
    var result = new List<Route>();
    var current = new List<Location>();
    int numRoutes = 0;
    var currentLoad = 0.0;
    foreach (var loc in greedyGTR)
    {
        current.Add(loc);
        currentLoad += loc.DemandMean;
        if (loc.Id == 0 && current.Count > 1)
        {
            result.Add(new Route(Vehicles[numRoutes].Capacity, new List<Location>(current), vehicleStarts[numRoutes], currentLoad));
            current.Clear();
            currentLoad = 0.0;
            current.Add(loc); // zaczynamy nową trasę od bazy
            numRoutes++;
        }
    }
    // Jeśli coś zostało, dodaj jako ostatnią trasę
    if (current.Count > 1)
        result.Add(new Route(Vehicles[numRoutes].Capacity, new List<Location>(current), vehicleStarts[numRoutes], currentLoad));

    return new Solution(result);
}
(List<Location> initialRoute, List<double> VehicleStarts) createGreedyGTR(Instance instance)
{
    double[,] distanceMatrix = instance.DistanceMatrix;
    List<Location> locations = instance.Locations;
    List<Vehicle> Vehicles = instance.Vehicles;
    var initialRoutesSplitted = new List<List<Location>>();
    List<double> vehicleStarts = new List<double>();
    var initialRoute = new List<Location>();
    bool[] visited = new bool[locations.Count];
    int vehicleNumber = 0;
    double vehicleTime = 0;
    double currentLoad = 0.0;

    initialRoute.Add(locations[0]); // Start z bazy

    while (visited.Contains(false))
    {
        Location current = locations[0]; // Start z bazy
        visited[current.Id] = true;
        vehicleTime = 0;
        currentLoad = 0.0; // Reset załadunku przy starcie nowego pojazdu

        while (true)
        {
            Location nextCustomer = null;
            double minDistance = double.MaxValue;

            foreach (var location in locations)
            {
                if (!visited[location.Id] && location.Id != 0)
                {
                    double demand = location.DemandMean;
                    double vehicleCapacity = Vehicles[vehicleNumber].Capacity;

                    // UWAGA: sprawdzamy czy możemy dodać klienta do aktualnego pojazdu
                    if (currentLoad + demand > vehicleCapacity)
                        continue; // nie mieści się, pomiń tego klienta

                    double distance = distanceMatrix[current.Id, location.Id];
                    double estimatedUpperTimeLeft = location.TimeWindow.End - vehicleTime;
                    double estimatedLowerTimeLeft = location.TimeWindow.Start - vehicleTime;
                    double estimatedPenalty = Math.Max(0, location.ServiceTime - estimatedUpperTimeLeft);
                    estimatedPenalty += Math.Max(0, Math.Min(estimatedLowerTimeLeft, location.ServiceTime));
                    distance += estimatedPenalty;

                    if (distance < minDistance && distance + vehicleTime < locations[0].TimeWindow.End)
                    {
                        minDistance = distance;
                        nextCustomer = location;
                    }
                }
            }

            // Jeśli nie ma dostępnych klientów, wróć do bazy
            if (nextCustomer == null && current.Id != 0 || vehicleTime >= locations[0].TimeWindow.End)
            {
                initialRoute.Add(locations[0]);
                vehicleNumber++;
                if (vehicleNumber >= Vehicles.Count)
                    return (initialRoute, vehicleStarts);
                break;
            }
            else if (nextCustomer == null && current.Id == 0)
            {
                foreach (var location in locations)
                {
                    if (!visited[location.Id] && currentLoad + location.DemandMean <= Vehicles[vehicleNumber].Capacity)
                    {
                        nextCustomer = location;
                        break;
                    }
                }
            }

            if (current.Id == 0)
            {
                vehicleTime = Math.Max(nextCustomer.TimeWindow.Start - distanceMatrix[current.Id, nextCustomer.Id], 0);
                vehicleStarts.Add(Math.Max(nextCustomer.TimeWindow.Start - distanceMatrix[current.Id, nextCustomer.Id], 0.0));
            }
            vehicleTime += distanceMatrix[current.Id, nextCustomer.Id];
            double upperTimeLeft = nextCustomer.TimeWindow.End - vehicleTime;
            double lowerTimeLeft = nextCustomer.TimeWindow.Start - vehicleTime;
            double penalty = Math.Max(0, nextCustomer.ServiceTime - upperTimeLeft);
            penalty += Math.Max(0, Math.Min(lowerTimeLeft, nextCustomer.ServiceTime));
            vehicleTime += nextCustomer.ServiceTime;
            vehicleTime += penalty;

            initialRoute.Add(nextCustomer);
            visited[nextCustomer.Id] = true;
            currentLoad += nextCustomer.DemandMean; // AKTUALIZUJ bieżący załadunek!

            current = nextCustomer;
        }
    }

    if (initialRoute[initialRoute.Count - 1].Id != 0)
    {
        initialRoute.Add(locations[0]);
    }
    return (initialRoute, vehicleStarts);
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