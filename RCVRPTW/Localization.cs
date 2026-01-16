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
        (int Start, int End) timeWindow, (double Start, double End) timeWindowStdDev, int serviceTime, int priority, bool randomDemand = false, bool randomTimeWindow = false, Random rng = null)
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
            Demand = DemandSampler.SampleNormalMathNet(demand, demandStdDev, rng: rng);
        }
        if(randomTimeWindow && type == LocationType.Customer)
        {
            var start = DemandSampler.SampleDemandInt(TimeWindow.Start, timeWindowStdDev.Start, rng: rng);
            var end = DemandSampler.SampleDemandInt(timeWindow.End, timeWindowStdDev.End, rng: rng);
            while (end <= start)
            {
                end = DemandSampler.SampleDemandInt(TimeWindow.Start, timeWindowStdDev.Start,rng: rng);
                end = DemandSampler.SampleDemandInt(timeWindow.End, timeWindowStdDev.End,rng: rng);
            }
            TimeWindow = (start, end);

        }
    }
}