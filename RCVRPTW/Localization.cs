using System;

public enum LocationType { Depot, Customer }

public class Location
{
    public int Id { get; set; }
    public LocationType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    
    public double Demand { get; set; }
    public double DemandMean { get; set; }
    public double DemandStdDev { get; set; }
    public (int Start, int End) TimeWindow { get; set; }
    public (int Start, int End) MeanTimeWindow { get; set; }
    public int ServiceTime { get; set; }
    public int Priority { get; set; }

    public Location(
        int id, LocationType type, int x, int y, double demand, double demandStdDev,
        (int Start, int End) timeWindow, (double Start, double End) timeWindowStdDev, int serviceTime, int priority, bool randomDemand = false, bool randomTimeWindow = false)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;
        Demand = demand;
        DemandMean = demand;
        DemandStdDev = demandStdDev;
        TimeWindow = timeWindow;
        MeanTimeWindow = timeWindow;
        ServiceTime = serviceTime;
        Priority = priority;
        if(randomDemand && type == LocationType.Customer)
        {
            Demand = DemandSampler.SampleNormalMathNet(demand, demandStdDev);
        }
        if(randomTimeWindow && type == LocationType.Customer)
        {
            var start = DemandSampler.SampleDemandInt(TimeWindow.Start, timeWindowStdDev.Start);
            var end = DemandSampler.SampleDemandInt(timeWindow.End, timeWindowStdDev.End);
            while (end <= start)
            {
                end = DemandSampler.SampleDemandInt(TimeWindow.Start, timeWindowStdDev.Start);
                end = DemandSampler.SampleDemandInt(timeWindow.End, timeWindowStdDev.End);
            }
            TimeWindow = (start, end);

        }
    }

    public double SampleDemand(Random rng)
    {
        if (DemandStdDev == 0) return Demand;
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return Math.Max(0, Demand + DemandStdDev * randStdNormal);
    }

    public override string ToString()
    {
        return $"{Id} {Type} ({X},{Y}) Demand: {Demand:F1} DemandMean: {DemandMean:F1} StdDev: {DemandStdDev:F1} " +
            $"TW: [{TimeWindow.Start}, {TimeWindow.End}] TWMEAN: [{MeanTimeWindow.Start}, {MeanTimeWindow.End}]";
    }
}