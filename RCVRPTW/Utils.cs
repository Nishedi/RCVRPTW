using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class Utils
    {
        public static (double cost, double penalty, double vehicleOperationTime) calculateMetrics(double startTime, List<Location> stops, Instance instance)
        {
            double vehicleOperationTime = startTime;
            double penalty = 0.0;
            double cost = 0.0;
            for (int r = 1; r < stops.Count; r++)
            {
                Location actualCity = stops[r];
                Location prevCity = stops[r - 1];
                cost += instance.DistanceMatrix[prevCity.Id, actualCity.Id];
                vehicleOperationTime += instance.DistanceMatrix[prevCity.Id, actualCity.Id];
                if (vehicleOperationTime < actualCity.TimeWindow.Start)
                {
                    penalty += (actualCity.TimeWindow.Start - vehicleOperationTime) * instance.TooEarlyPenaltyFactor;
                }
                vehicleOperationTime += actualCity.ServiceTime;
                if (vehicleOperationTime > actualCity.TimeWindow.End)
                {
                    double toLatePenalty = (vehicleOperationTime - actualCity.TimeWindow.End) * instance.TooLatePenaltyFactor;
                    penalty += toLatePenalty;
                }
            }
            vehicleOperationTime -= startTime;
            return (instance.DistanceFactor*cost, instance.PenaltyFactor*penalty, instance.WaitingFactor*vehicleOperationTime);
        }

    }

}
