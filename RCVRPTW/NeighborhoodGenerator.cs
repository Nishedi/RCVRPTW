using System;
using System.Collections.Generic;

public static class NeighborhoodGeneratorLocation
{
    /// <summary>
    /// Zwraca wszystkie możliwe sąsiedztwa powstałe przez pojedynczy swap:
    /// - wewnątrz każdej trasy (Route),
    /// - pomiędzy wszystkimi parami tras.
    /// Każdy sąsiad to nowa lista tras (deep copy), z wykonanym jednym swapem.
    /// </summary>
    public static List<List<Route>> GenerateAllSwaps(List<Route> routes)
    {
        var neighbors = new List<List<Route>>();

        List<Location> allLocations = routes.SelectMany(route => route.Stops).ToList();

        
        for(int i = 1; i < allLocations.Count - 1; i++)
        {
            for (int j = 1; j < allLocations.Count - 1; j++)
            {
                List<Location> neighbor = DeepCopyLocations(allLocations);
                Location tempLocation = neighbor[j];
                neighbor[j] = neighbor[i];
                neighbor[i] = tempLocation;
                List <Route> nRoutes = new List<Route>();
                List<Location> nLocations = new List<Location>();
                foreach (var location in neighbor)
                {
                    if (location.Id == 0)
                    {
                        if (nLocations.Count > 0)
                        {
                            nRoutes.Add(new Route(90, nLocations,0));
                            nLocations = new List<Location>();
                        }
                            
                    }
                    else
                    {
                        nLocations.Add(location);
                    }
                }
                neighbors.Add(nRoutes);
                

            }
        }



        return neighbors;
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
            copy.Add(new Route(route.TruckCapacity, new List<Location>(route.Stops),route.StartTime));
        }
        return copy;
    }
}