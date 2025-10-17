using System;

public enum LocationType { Depot, Customer }

public class Location
{
    public int Id { get; set; }
    public LocationType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public double DemandMean { get; set; }
    public double DemandStdDev { get; set; }
    public (int Start, int End) TimeWindow { get; set; }
    public int ServiceTime { get; set; }
    public int Priority { get; set; }

    public Location(
        int id, LocationType type, int x, int y, double demandMean, double demandStdDev,
        (int Start, int End) timeWindow, int serviceTime, int priority)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;
        DemandMean = demandMean;
        DemandStdDev = demandStdDev;
        TimeWindow = timeWindow;
        ServiceTime = serviceTime;
        Priority = priority;
    }

    public double SampleDemand(Random rng)
    {
        if (DemandStdDev == 0) return DemandMean;
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return Math.Max(0, DemandMean + DemandStdDev * randStdNormal);
    }

    public override string ToString()
    {
        return $"{Id} {Type} ({X},{Y}) DemandMean: {DemandMean:F1} StdDev: {DemandStdDev:F1} TW: [{TimeWindow.Start}, {TimeWindow.End}]";
    }
}