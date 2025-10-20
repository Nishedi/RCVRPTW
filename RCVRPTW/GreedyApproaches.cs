using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class GreedyApproaches
    {
        public static Solution generateGreedySolution(Instance instance) //wprowadzic czekanie jezeli sie oplaca
        {
            var (greedyGTR, vehicleStarts) = createGreedyGTR(instance);
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
                    result.Add(new Route(instance.Vehicles[numRoutes].Capacity, new List<Location>(current), vehicleStarts[numRoutes], currentLoad));
                    current.Clear();
                    currentLoad = 0.0;
                    current.Add(loc); // zaczynamy nową trasę od bazy
                    numRoutes++;
                }
            }
            // Jeśli coś zostało, dodaj jako ostatnią trasę
            if (current.Count > 1)
                result.Add(new Route(instance.Vehicles[numRoutes].Capacity, new List<Location>(current), vehicleStarts[numRoutes], currentLoad));

            return new Solution(result);
        }
        private static (List<Location> initialRoute, List<double> VehicleStarts) createGreedyGTR(Instance instance)
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
    }
}
