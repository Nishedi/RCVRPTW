using RCVRPTW;
using System;
using System.Collections.Generic;
using System.Linq;

public class Route
{
    public double TruckCapacity { get; set; }
    public double CurrentLoad { get; set; }
    public List<Location> Stops { get; set; } 
    public double StartTime { get; set; }
    public double Cost { get; set; }
    public double Penalty { get; set; }
    public double vehicleOperationTime { get; set; }
    public Route(double truckCapacity, List<Location> stops,  double startTime, double currentLoad, double cost=0.0, double penalty=0.0, double vot=0.0)
    {
        TruckCapacity = truckCapacity;
        Stops = stops;
        StartTime = startTime;
        CurrentLoad = currentLoad;
        Cost = cost;
        Penalty = penalty;
        vehicleOperationTime = vot;
    }

    public override string ToString()
    {
        var stringValue = "";
        foreach (var stop in Stops)
        {
            stringValue += stop.Id + "->";
        }
        stringValue += " |Truck: " + TruckCapacity+ " |Start at: "+StartTime+" |Weight: "+CurrentLoad;
        return stringValue;    
    }
}

public class Solution
{
    public List<Route> Routes { get; set; }
    public double TotalCost { get; set; }
    public double TotalPenalty { get; set; }
    public (double i, double j) move { get; set; }

    public double TotalVehicleOperationTime { get; set; }
    public double TotalMixedMetrics = 0.0;
    public (double greedyTotalCost, double greedyTotalPenalty, double greedyVOT) GreedyMetrics { get; set; }

    public Solution(List<Route> routes)
    {
        Routes = routes;
        TotalCost = CalculateTotalCost();
        TotalPenalty = CalculateTotalPenalty();
    }

    public double CalculateTotalCost()
    {
        return 0;
    }

    public double sumMetrics(int costMultiplier = 1, int penaltyMultiplier = 1, int vehicleOperationTimeMultiplier = 1)
    {
        return costMultiplier * TotalCost + penaltyMultiplier * TotalPenalty + vehicleOperationTimeMultiplier * TotalVehicleOperationTime;
    }

    public double CalculateTotalPenalty()
    {
        return 0;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;
        Solution other = (Solution)obj;
        if (other.move.i!= this.move.i || other.move.j != this.move.j) return false;
        return true;
    }

    public Solution DeepCopy(Solution org)
    {
        List<Route> newRoutes = new List<Route>();
        for (int r = 0; r < org.Routes.Count; r++)
        {
            List<Location> newStops = new List<Location>();
            for (int stop = 0; stop < org.Routes[r].Stops.Count; stop++)
            {
                newStops.Add(org.Routes[r].Stops[stop]);
            }
            var newRoute = new Route(org.Routes[r].TruckCapacity, newStops, org.Routes[r].StartTime, org.Routes[r].CurrentLoad, org.Routes[r].Cost, org.Routes[r].Penalty, org.Routes[r].vehicleOperationTime);

            newRoutes.Add(newRoute);
        }
        return new Solution(newRoutes)
        {
            TotalCost = org.TotalCost,
            TotalPenalty = org.TotalPenalty,
            TotalVehicleOperationTime = org.TotalVehicleOperationTime,
            move = org.move
        };

    }

    public override string ToString()
    {
        var stringValue = Routes.Count+"|";
        foreach (var route in Routes)
        {
            stringValue += route + " \n ";
        }
        return stringValue+"\n";
    }

    public void calculateRoutesMetrics(Instance instance)
    {
        foreach(var route in Routes)
        {
            var (cost, penalty, vehicleOperationTime) = Utils.calculateMetrics(route.StartTime, route.Stops, instance);
            TotalCost += cost;
            TotalPenalty += penalty;
            TotalVehicleOperationTime += vehicleOperationTime;
        }
    }
}