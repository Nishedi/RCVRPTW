using System;
using System.Collections.Generic;
using System.Linq;

public class Route
{
    public double TruckCapacity { get; set; }
    public List<Location> Stops { get; set; } // Lista ID lokalizacji (np. [hub, customer1, customer2, ..., hub])
    public double StartTime { get; set; }
    public Route(double truckCapacity, List<Location> stops,  double startTime)
    {
        TruckCapacity = truckCapacity;
        Stops = stops;
        StartTime = startTime;
    }

    public override string ToString()
    {
        var stringValue = "";
        foreach (var stop in Stops)
        {
            stringValue += stop.Id + "->";
        }
        stringValue += " |Truck: " + TruckCapacity+ " |Start at: "+StartTime;
        return stringValue;    
    }
}

public class Solution
{
    public List<Route> Routes { get; set; }
    public double TotalCost { get; set; }
    public double Penalty { get; set; }

    public Solution(List<Route> routes)
    {
        Routes = routes;
        TotalCost = CalculateTotalCost();
        Penalty = CalculateTotalPenalty();
    }

    public double CalculateTotalCost()
    {
        return 0;
    }

    public double CalculateTotalPenalty()
    {
        return 0;
    }

    public override string ToString()
    {
        var stringValue = "";
        foreach (var route in Routes)
        {
            stringValue += route + " \n ";
        }
        return stringValue+"\n";
    }
    
}