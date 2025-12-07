using RCVRPTW;
using System;
using System.Collections.Concurrent;
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
    /// 
    public static void Shuffle<T>(IList<T> list, Random rng = null)
    {
        if (rng == null)
            rng = new Random();

        int n = list.Count;
        if (n <= 2) return; // nie ma co tasować

        // Tasujemy tylko elementy od indeksu 1 do n-2
        for (int i = n - 2; i > 0; i--)
        {
            int k = rng.Next(1, i + 1); // losuj z zakresu [1, i]
            T value = list[k];
            list[k] = list[i];
            list[i] = value;
        }
    }
    public static Solution GenerateRandomSolutionL(List<Route> routes, List<Vehicle> vehicles, Instance instance)
    {
        bool invalidRoute = false;
        int x = 0;
        do
        {

            List<Location> allLocations = routes.SelectMany(route => route.Stops).ToList();
            List<Location> neighbor = DeepCopyLocations(allLocations);

            Shuffle(neighbor);
            List<Route> nRoutes = new List<Route>();
            List<Location> nLocations = new List<Location>();
            var routeWeight = 0.0;
            invalidRoute = false;
            foreach (var location in neighbor)
            {
                if (location.Id == 0)
                {
                    if (nLocations.Count > 0)
                    {
                        var route = new Route(90, nLocations, 0, routeWeight);
                        route.Stops.Add(allLocations[0]);
                        route.Stops.Insert(0, allLocations[0]);
                        (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, instance);
                        nRoutes.Add(route);
                        nLocations = new List<Location>();
                        routeWeight = 0;
                    }
                }
                else
                {
                    nLocations.Add(location);
                    routeWeight += location.Demand;
                }
            }
            foreach (var route in nRoutes)
            {
                if (route.CurrentLoad > vehicles[0].Capacity)
                {
                    x++;
                    invalidRoute = true;
                    break;
                }
            }
            if (invalidRoute) continue;
            var solution = new Solution(DeepCopyRoutes(nRoutes));
            foreach (var route in solution.Routes)
            {
                solution.TotalPenalty += route.Penalty;
                solution.TotalCost += route.Cost;
                solution.TotalVehicleOperationTime += route.vehicleOperationTime;
                solution.TotalMixedMetrics = solution.sumMetrics();
            }
            return solution;
        } while (invalidRoute);

        return null; 

    }

    public static Solution GenerateRandomSolution(List<Route> routes, List<Vehicle> vehicles, Instance instance)
    {
        bool invalidRoute = false;
        int x = 0;
        do
        {
            Location first = routes[0].Stops[0];
            var allLocations = routes
                .SelectMany(r => r.Stops)
                .Where(s => s.Id != 0)
                .ToList();
            List<Location> neighbor = DeepCopyLocations(allLocations);
            neighbor.Insert(0, first);
            neighbor.Add(first);




            Shuffle(neighbor);
            List<Route> nRoutes = new List<Route>();
            List<Location> nLocations = new List<Location>();
            var routeWeight = 0.0;
            invalidRoute = false;
            foreach (var location in neighbor)
            {
                if (location.Id == 0)
                {
                    if (nLocations.Count > 0)
                    {
                        var route = new Route(90, nLocations, 0, routeWeight);
                        route.Stops.Add(first);
                        route.Stops.Insert(0, first);
                        (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, instance);
                        nRoutes.Add(route);
                        nLocations = new List<Location>();
                        routeWeight = 0;
                    }
                }
                else if(routeWeight + location.Demand > vehicles[0].Capacity)
                {
                    var route = new Route(90, nLocations, 0, routeWeight);
                    route.Stops.Add(first   );
                    route.Stops.Insert(0, first);
                    (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, instance);
                    nRoutes.Add(route);
                    nLocations = new List<Location>();
                    nLocations.Add(location);
                    routeWeight = location.Demand;

                }
                else
                {
                    nLocations.Add(location);
                    routeWeight += location.Demand;
                }
            }
            foreach (var route in nRoutes)
            {
                if (route.CurrentLoad > vehicles[0].Capacity)
                {
                    x++;
                    invalidRoute = true;
                    break;
                }
            }
            if (invalidRoute) continue;
            var solution = new Solution(DeepCopyRoutes(nRoutes));
            foreach (var route in solution.Routes)
            {
                solution.TotalPenalty += route.Penalty;
                solution.TotalCost += route.Cost;
                solution.TotalVehicleOperationTime += route.vehicleOperationTime;
                solution.TotalMixedMetrics = solution.sumMetrics();
            }
            return solution;
        } while (invalidRoute);

        return null;

    }


    public static List<Location> swap(List<Location> locations, int i, int j)
    {
        List<Location> neighbor = DeepCopyLocations(locations);
        Location tempLocation = neighbor[j];
        neighbor[j] = neighbor[i];
        neighbor[i] = tempLocation;
        return neighbor;
    }

    public static List<Location> invert(List<Location> locations, int i, int j)
    {
        List<Location> neighbor = DeepCopyLocations(locations);
        while (i < j)
        {
            Location tempLocation = neighbor[j];
            neighbor[j] = neighbor[i];
            neighbor[i] = tempLocation;
            i++;
            j--;
        }
        return neighbor;
    }

    public static List<Location> insert(List<Location> locations, int i, int j)
    {
        List<Location> neighbor = DeepCopyLocations(locations);
        Location tempLocation = neighbor[i];
        neighbor.RemoveAt(i);
        neighbor.Insert(j, tempLocation);
        return neighbor;
    }

    public static List<Location> twoOpt(List<Location> locations, int i, int j)
    {
        // 2-opt: reverses the segment between i and j
        // Named differently from invert for VRP domain clarity, but uses same logic
        // Ensures i < j before calling invert for consistency
        if (i > j)
        {
            int temp = i;
            i = j;
            j = temp;
        }
        return invert(locations, i, j);
    }

    public static List<Location> orOpt(List<Location> locations, int i, int length, int j)
    {
        // Or-opt: removes a sequence of 'length' customers starting at position i
        // and inserts it at position j
        List<Location> neighbor = DeepCopyLocations(locations);
        
        if (i < 0 || i + length > neighbor.Count || j < 0 || j > neighbor.Count)
            return neighbor; // Invalid parameters, return unchanged
        
        // Extract the sequence
        List<Location> sequence = new List<Location>();
        for (int k = 0; k < length; k++)
        {
            if (i + k < neighbor.Count)
                sequence.Add(neighbor[i + k]);
        }
        
        // Remove the sequence (always remove from position i, as removal shifts elements)
        for (int k = 0; k < sequence.Count && i < neighbor.Count; k++)
        {
            neighbor.RemoveAt(i);
        }
        
        // Adjust insertion position if needed
        int insertPos = j;
        if (j > i)
            insertPos = j - length;
        if (insertPos < 0)
            insertPos = 0;
        if (insertPos > neighbor.Count)
            insertPos = neighbor.Count;
        
        // Insert the sequence at the new position
        neighbor.InsertRange(insertPos, sequence);
        
        return neighbor;
    }

    public static List<Location> crossExchange(List<Location> locations, int i, int lengthI, int j, int lengthJ)
    {
        // Cross-exchange: swaps two segments of potentially different lengths
        List<Location> neighbor = DeepCopyLocations(locations);
        
        if (i < 0 || i + lengthI > neighbor.Count || j < 0 || j + lengthJ > neighbor.Count || i == j)
            return neighbor; // Invalid parameters
        
        // Ensure i comes before j
        if (i > j)
        {
            int tempPos = i, tempLen = lengthI;
            i = j;
            lengthI = lengthJ;
            j = tempPos;
            lengthJ = tempLen;
        }
        
        // Check for overlap
        if (i + lengthI > j)
            return neighbor; // Segments overlap, return unchanged
        
        // Extract both segments
        List<Location> segmentI = new List<Location>();
        List<Location> segmentJ = new List<Location>();
        
        for (int k = 0; k < lengthI && i + k < neighbor.Count; k++)
            segmentI.Add(neighbor[i + k]);
        
        for (int k = 0; k < lengthJ && j + k < neighbor.Count; k++)
            segmentJ.Add(neighbor[j + k]);
        
        // Remove both segments (remove the later one first to avoid index issues)
        // Remove from j (the later position first)
        for (int k = 0; k < segmentJ.Count; k++)
            neighbor.RemoveAt(j);
        
        // Remove from i (now safe as j was after i)
        for (int k = 0; k < segmentI.Count; k++)
            neighbor.RemoveAt(i);
        
        // Insert segments in swapped positions
        neighbor.InsertRange(i, segmentJ);
        neighbor.InsertRange(j - lengthI + lengthJ, segmentI);
        
        return neighbor;
    }

    public static List<Solution> GenerateAllSwaps(List<Route> routes, List<Vehicle> vehicles, Instance instance,  string mutationType)
    {
        var neighborsBag = new ConcurrentBag<Solution>();

        // Zbiór wszystkich lokacji (w tym depot jako element o Id==0?)
        List<Location> allLocations = routes.SelectMany(route => route.Stops).ToList();

        // ensure depot repeated as in original code
        for (int k = 0; k < 5; k++)
            allLocations.Add(allLocations[0]);

        int count = allLocations.Count;
        var depot = allLocations[0];

        // równoległe przetwarzanie par (i,j)
        Parallel.For(1, count - 1, i =>
        {
            // Każdy wątek wykonuje wewnętrzną pętlę sekwencyjnie
            for (int j = i + 1; j < count - 1; j++)
            {
                if (i == j) continue;

                // DeepCopyLocations zakładamy, że jest bezpieczne do równoległego użycia
                List<Location> neighbor = DeepCopyLocations(allLocations);

                if (mutationType == "insert")
                    neighbor = insert(neighbor, i, j);
                else if (mutationType == "invert")
                    neighbor = invert(neighbor, i, j);
                else if (mutationType == "swap")
                    neighbor = swap(neighbor, i, j);
                else if (mutationType == "2opt")
                    neighbor = twoOpt(neighbor, i, j);
                else if (mutationType == "oropt")
                {
                    // For or-opt, use a fixed sequence length (e.g., 2 or 3)
                    int length = Math.Min(3, Math.Max(1, (j - i) / 3));
                    neighbor = orOpt(neighbor, i, length, j);
                }
                else if (mutationType == "cross")
                {
                    // For cross-exchange, use small segment lengths
                    int lengthI = Math.Min(2, count - i - 1);
                    int lengthJ = Math.Min(2, count - j - 1);
                    neighbor = crossExchange(neighbor, i, lengthI, j, lengthJ);
                }
                else
                    neighbor = swap(neighbor, i, j); // przypisanie wyniku swap w przypadku default

                List<Route> nRoutes = new List<Route>();
                List<Location> nLocations = new List<Location>();
                var routeWeight = 0.0;
                bool invalidRoute = false;

                // budowanie tras z listy lokacji
                foreach (var location in neighbor)
                {
                    if (location.Id == 0)
                    {
                        if (nLocations.Count > 0)
                        {
                            var route = new Route(90, nLocations, 0, routeWeight);
                            // dodaj depot na poczatek i koniec trasy
                            route.Stops.Insert(0, depot);
                            route.Stops.Add(depot);

                            // oblicz metryki trasy (zakladamy, ze bestStartTime jest bezpieczne)
                            (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, instance);

                            nRoutes.Add(route);

                            nLocations = new List<Location>();
                            routeWeight = 0;
                        }
                    }
                    else
                    {
                        nLocations.Add(location);
                        routeWeight += location.Demand;
                    }
                }

                // sprawdź ograniczenia pojemności
                foreach (var route in nRoutes)
                {
                    if (route.CurrentLoad > vehicles[0].Capacity)
                    {
                        invalidRoute = true;
                        break;
                    }
                }

                if (!invalidRoute && nRoutes.Count > 0)
                {
                    // stwórz Solution i policz sumy (lokalnie, w wątku)
                    var solution = new Solution(DeepCopyRoutes(nRoutes));
                    // reset pól jeśli konstruktor ich nie zeruje
                    solution.TotalPenalty = 0;
                    solution.TotalCost = 0;
                    solution.TotalVehicleOperationTime = 0;
                    solution.TotalMixedMetrics = 0;

                    foreach (var route in solution.Routes)
                    {
                        solution.TotalPenalty += route.Penalty;
                        solution.TotalCost += route.Cost;
                        solution.TotalVehicleOperationTime += route.vehicleOperationTime;
                    }
                    solution.TotalMixedMetrics = solution.sumMetrics();
                    solution.move = (i, j);
                    neighborsBag.Add(solution);
                }
            }
        });

        // na koniec zbierz wyniki i posortuj
        var neighbors = neighborsBag.OrderBy(sol => sol.TotalCost).ToList();
        return neighbors;
    }
    public static List<Solution> GenerateAllSwaps_not_parrarel(List<Route> routes, List<Vehicle> vehicles, Instance instance, string mutationType)//pomyslec czy nie liczyc juz przy generowaniu mutacji zamiast poxniej
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
                if(mutationType == "insert")
                    neighbor = insert(neighbor, i, j);
                else if (mutationType == "invert")
                    neighbor = invert(neighbor, i, j);
                else if (mutationType == "swap")
                    neighbor = swap(neighbor, i, j);
                else if (mutationType == "2opt")
                    neighbor = twoOpt(neighbor, i, j);
                else if (mutationType == "oropt")
                {
                    int length = Math.Min(3, Math.Max(1, (j - i) / 3));
                    neighbor = orOpt(neighbor, i, length, j);
                }
                else if (mutationType == "cross")
                {
                    int lengthI = Math.Min(2, allLocations.Count - i - 1);
                    int lengthJ = Math.Min(2, allLocations.Count - j - 1);
                    neighbor = crossExchange(neighbor, i, lengthI, j, lengthJ);
                }
                else 
                    neighbor = swap(neighbor, i, j);
                List<Route> nRoutes = new List<Route>();
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
                            (route.Cost, route.Penalty, route.vehicleOperationTime, route.StartTime) = bestStartTime(nLocations, instance);
                            nRoutes.Add(route);
                            nLocations = new List<Location>();
                            routeWeight = 0;
                        }                
                    }
                    else
                    {
                        nLocations.Add(location);
                        routeWeight += location.Demand;
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
        return neighbors.OrderBy(sol => sol.TotalCost).ToList();
    }

    public static (double bestCost, double bestPenalty, double bestVehicleOperationTime, double bestStartTime) bestStartTime(List<Location> stops, Instance instance)
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
            var (cost, penalty, vehicleOperationTime) = Utils.calculateMetrics(startTime, stops, instance);
            if (cost + penalty + vehicleOperationTime<bestTotalCost)
            {
                bestTotalCost = cost + penalty + vehicleOperationTime;
                bestStartTime = startTime;
                (bestCost, bestPenalty, bestVehicleOperationTime) = (cost, penalty, vehicleOperationTime);
            }
        }
        return (bestCost, bestPenalty, bestVehicleOperationTime,bestStartTime);   
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