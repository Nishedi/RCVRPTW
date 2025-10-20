using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    internal class Utils
    {
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

    }

}
