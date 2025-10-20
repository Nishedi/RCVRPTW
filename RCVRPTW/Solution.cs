using RCVRPTW;
using System;
using System.Collections.Generic;
using System.Linq;

public class Route
{
    public double TruckCapacity { get; set; }
    public double CurrentLoad { get; set; }
    public List<Location> Stops { get; set; } // Lista ID lokalizacji (np. [hub, customer1, customer2, ..., hub])
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

    public double TotalVehicleOperationTime { get; set; }
    public double TotalMixedMetrics = 0.0;

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
        if (other.Routes.Count != this.Routes.Count) return false;
        for(int r = 0; r < this.Routes.Count; r++)
        {
            if (other.Routes[r].Stops.Count != this.Routes[r].Stops.Count) return false;
            for(int s = 0; s < this.Routes[r].Stops.Count; s++)
            {
                if (other.Routes[r].Stops[s].Id != this.Routes[r].Stops[s].Id) return false;
            }
        }
        return true;
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

    public void calculateRoutesMetrics(double[,] distanceMatrix)
    {
        foreach(var route in Routes)
        {
            var (cost, penalty, vehicleOperationTime) = Utils.calculateMetrics(route.StartTime, route.Stops, distanceMatrix);
            TotalCost += cost;
            TotalPenalty += penalty;
            TotalVehicleOperationTime += vehicleOperationTime;
        }
    }

   

}