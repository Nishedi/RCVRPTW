using RCVRPTW;
using System;
using System.Collections.Generic;
using static System.Formats.Asn1.AsnWriter;

public static class NeighborhoodGeneratorLocation
{
    /// <summary>
    /// Zwraca wszystkie możliwe sąsiedztwa powstałe przez pojedynczy swap:
    /// - wewnątrz każdej trasy (Route),
    /// - pomiędzy wszystkimi parami tras.
    /// Każdy sąsiad to nowa lista tras (deep copy), z wykonanym jednym swapem.
    /// </summary>
    public static List<Solution> GenerateAllSwaps(List<Route> routes, List<Vehicle> vehicles, double[,] distanceMatrix)//pomyslec czy nie liczyc juz przy generowaniu mutacji zamiast poxniej
    {
        List<Solution> neighbors = new List<Solution>();
        var routeNeighbors = new List<List<Route>>();
        List<Location> allLocations = routes.SelectMany(route => route.Stops).ToList();
        for(int i = 0; i < 5; i++)
        {
            allLocations.Add(allLocations[0]);
        }
        for (int i = 1; i < allLocations.Count - 1; i++)
        {
            for (int j = i + 1; j < allLocations.Count - 1; j++)
            {
                if (i == j) continue;
                List<Location> neighbor = DeepCopyLocations(allLocations);
                Location tempLocation = neighbor[j];
                neighbor[j] = neighbor[i];
                neighbor[i] = tempLocation;
                List <Route> nRoutes = new List<Route>();
                List<Location> nLocations = new List<Location>();
                var routeWeight = 0.0;
                bool invalidRoute = false;
                foreach (var location in neighbor)
                {
                    if (location.Id == 0)
                    {
                        if (nLocations.Count > 0)
                        {   
                            var route = new Route(90, nLocations,0,routeWeight);
                            route.Stops.Add(allLocations[0]);
                            route.Stops.Insert(0, allLocations[0]);
                            (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, distanceMatrix);
                            nRoutes.Add(route);
                            nLocations = new List<Location>();
                            routeWeight = 0;
                        }                
                    }
                    else
                    {
                        nLocations.Add(location);
                        routeWeight += location.DemandMean;
                    }
                }
                foreach(var route in nRoutes)
                {
                    if (route.CurrentLoad > vehicles[0].Capacity)
                    {
                        invalidRoute = true;
                        break;
                    }
                }
                if(!invalidRoute)
                    routeNeighbors.Add(nRoutes);
                
             }
        }
        foreach (var routeSet in routeNeighbors)
        {
            var solution = new Solution(DeepCopyRoutes(routeSet));
            foreach (var route in solution.Routes)
            {
                solution.TotalPenalty += route.Penalty;
                solution.TotalCost += route.Cost;
                solution.TotalVehicleOperationTime += route.vehicleOperationTime;
                solution.TotalMixedMetrics = solution.sumMetrics();
            }
            neighbors.Add(solution);
        }
        return neighbors.OrderBy(sol => sol.TotalMixedMetrics).ToList();
    }

    public static (double bestCost, double bestPenalty, double bestVehicleOperationTime, double bestStartTime) bestStartTime(List<Location> stops, double[,] distanceMatrix)
    {
        int multiplier = Math.Max(8, stops.Count);
        double[] bestStartTimes = new double[multiplier];
        multiplier = Math.Min(20, multiplier);
        for (int i = 0; i < multiplier; i++)
        {
            if (stops[1].TimeWindow.Start * i / multiplier > stops[0].TimeWindow.End) break;
            if (stops[1].TimeWindow.Start > 0)
                bestStartTimes[i] = stops[1].TimeWindow.Start * i / multiplier;
            else
                bestStartTimes[i] = i * 10;
            
        }
        double bestTotalCost = double.MaxValue;
        double bestStartTime = stops[1].TimeWindow.Start; 
        var (bestCost, bestPenalty, bestVehicleOperationTime) = (0.0, 0.0, 0.0);
        foreach (var startTime in bestStartTimes)
        {
            var (cost, penalty, vehicleOperationTime) = calculateMetrics(startTime, stops, distanceMatrix);
            if (cost + penalty + vehicleOperationTime<bestTotalCost)
            {
                bestTotalCost = cost + penalty + vehicleOperationTime;
                bestStartTime = startTime;
                (bestCost, bestPenalty, bestVehicleOperationTime) = (cost, penalty, vehicleOperationTime);
            }
        }
        return (bestCost, bestPenalty, bestVehicleOperationTime,bestStartTime);   
    } 

    public static (double cost, double penalty, double vehicleOperationTime) calculateMetrics(double startTime, List<Location> stops, double[,] distanceMatrix)
    {
        double vehicleOperationTime = startTime;
        double penalty = 0.0;
        double cost = 0.0;
        for (int r = 1; r < stops.Count; r++)
        {
            Location actualCity = stops[r];
            Location prevCity = stops[r - 1];
            cost += distanceMatrix[prevCity.Id, actualCity.Id];
            vehicleOperationTime += distanceMatrix[prevCity.Id, actualCity.Id];
            if (vehicleOperationTime < actualCity.TimeWindow.Start)
            {
                double costOfWaiting = actualCity.TimeWindow.Start - vehicleOperationTime;
                double toEarlyPenalty = Math.Min(costOfWaiting, actualCity.ServiceTime) * 1; //tutaj mozna dodac jakis wspolczynnik jakby byly różne kary za zbyt wczesne dotarcie
                if (costOfWaiting <= toEarlyPenalty)//to na sytuacje gdyby kara byla wieksza niz 1 * to co się wykonywało przed czasem
                {
                    vehicleOperationTime += costOfWaiting;
                }
                else
                {
                    penalty += toEarlyPenalty;
                }
            }
            vehicleOperationTime += actualCity.ServiceTime;
            if (vehicleOperationTime > actualCity.TimeWindow.End)
            {
                double toLatePenalty = Math.Min(actualCity.ServiceTime, vehicleOperationTime - actualCity.TimeWindow.End);
                penalty += toLatePenalty;
            }
        }
        vehicleOperationTime -= startTime;
        return (cost, penalty, vehicleOperationTime);
    }

    private static List<Location> DeepCopyLocations(List<Location> locations)
    {
        var copy = new List<Location>();
        foreach (var location in locations) {
            copy.Add(location);
        }
        return copy;
    }

    private static List<Route> DeepCopyRoutes(List<Route> routes)
    {
        var copy = new List<Route>();
        foreach (var route in routes)
        {
            // Załóżmy, że Route ma konstruktor: Route(double cost, List<Location> locations)
            copy.Add(new Route(route.TruckCapacity, new List<Location>(route.Stops),route.StartTime,route.CurrentLoad, route.Cost, route.Penalty, route.vehicleOperationTime));
        }
        return copy;
    }
}