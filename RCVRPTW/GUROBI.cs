using System;
using Gurobi;
using RCVRPTW;

namespace RCVRPTW
{
    public class CVRPTW_Model
    {
        public static double Solve(Instance instance, double timeLimitSeconds = 300.0)
        {
            try
            {
                int numNodes = instance.Locations.Count;
                int N = numNodes - 1;
                int maxV = instance.Vehicles.Count;
                double Q = instance.Vehicles[0].Capacity;
                Console.WriteLine($"Instancja: {instance.FileName}, Klienci: {N}, Max Pojazdów: {maxV}, Pojemność: {Q}");

                double[,] d = instance.DistanceMatrix;
                double[] y = new double[numNodes];
                double[] s = new double[numNodes];
                double[] a = new double[numNodes];
                double[] b = new double[numNodes];

                for (int i = 0; i < numNodes; i++)
                {
                    y[i] = instance.Locations[i].Demand;
                    s[i] = instance.Locations[i].ServiceTime;
                    a[i] = instance.Locations[i].TimeWindow.Start;
                    b[i] = instance.Locations[i].TimeWindow.End;
                }

                using (GRBEnv env = new GRBEnv(true))
                {
                    env.Set("LogFile", "cvrptw_gurobi_2index.log");
                    env.Start();

                    using (GRBModel model = new GRBModel(env))
                    {
                        model.ModelName = "CVRPTW_2_Index_NoWait";
                        model.Parameters.TimeLimit = timeLimitSeconds;
 
                        GRBVar[,] x = new GRBVar[numNodes, numNodes]; 
                        GRBVar[] t = new GRBVar[numNodes]; 
                        GRBVar[] u = new GRBVar[numNodes]; 
                        GRBVar[] e = new GRBVar[numNodes]; 
                        GRBVar[] l = new GRBVar[numNodes]; 

                        for (int i = 0; i < numNodes; i++)
                        {
                            t[i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"t_{i}");
                            u[i] = model.AddVar(y[i], Q, 0.0, GRB.CONTINUOUS, $"u_{i}");
                            e[i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"e_{i}");
                            l[i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"l_{i}");

                            for (int j = 0; j < numNodes; j++)
                            {
                                x[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_{i}_{j}");
                            }
                        }

                        model.AddConstr(t[0] == 0.0, "StartTimeDepot");

                        GRBLinExpr totalDistance = 0.0;
                        GRBLinExpr totalEarlyPenalty = 0.0;
                        GRBLinExpr totalLatePenalty = 0.0;
                        double totalServiceTimeConstant = 0.0; 

                        for (int i = 0; i < numNodes; i++)
                        {
                            if (i > 0)
                            {
                                totalEarlyPenalty.AddTerm(1.0, e[i]);
                                totalLatePenalty.AddTerm(1.0, l[i]);
                                totalServiceTimeConstant += s[i];
                            }

                            for (int j = 0; j < numNodes; j++)
                            {
                                totalDistance.AddTerm(d[i, j], x[i, j]);
                            }
                        }

                        GRBLinExpr totalOpTime = totalDistance + totalServiceTimeConstant;
                        GRBLinExpr totalPenalty = (instance.TooEarlyPenaltyFactor * totalEarlyPenalty) +
                                                  (instance.TooLatePenaltyFactor * totalLatePenalty);

                        GRBLinExpr obj = (instance.DistanceFactor * totalDistance) +
                                         (instance.WaitingFactor * totalOpTime) +
                                         (instance.PenaltyFactor * totalPenalty);

                        model.SetObjective(obj, GRB.MINIMIZE);

                        for (int i = 1; i <= N; i++)
                        {
                            GRBLinExpr flowOut = 0.0;
                            GRBLinExpr flowIn = 0.0;
                            for (int j = 0; j < numNodes; j++)
                            {
                                if (i != j)
                                {
                                    flowOut.AddTerm(1.0, x[i, j]);
                                    flowIn.AddTerm(1.0, x[j, i]);
                                }
                            }
                            model.AddConstr(flowOut == 1.0, $"VisitOnceOut_{i}");
                            model.AddConstr(flowIn == 1.0, $"VisitOnceIn_{i}");
                        }

                        GRBLinExpr depotOut = 0.0;
                        GRBLinExpr depotIn = 0.0;
                        for (int j = 1; j <= N; j++)
                        {
                            depotOut.AddTerm(1.0, x[0, j]);
                            depotIn.AddTerm(1.0, x[j, 0]);
                        }
                        model.AddConstr(depotOut <= maxV, "MaxVehicles");
                        model.AddConstr(depotOut == depotIn, "DepotBalance"); // Tyle samo powrotów co wyjazdów

                        for (int i = 0; i < numNodes; i++)
                            model.AddConstr(x[i, i] == 0.0, $"NoSelfLoop_{i}");

                        for (int i = 0; i < numNodes; i++)
                        {
                            for (int j = 1; j <= N; j++)
                            {
                                if (i != j)
                                {
                                    if (i > 0)
                                    {
                                        model.AddConstr(u[j] >= u[i] + y[j] - Q * (1 - x[i, j]), $"Capacity_{i}_{j}");
                                    }

                                    if (i == 0)
                                    {
                                        model.AddGenConstrIndicator(x[0, j], 1, t[j] == d[0, j], $"TimeFlow_0_{j}");
                                    }
                                    else
                                    {
                                        model.AddGenConstrIndicator(x[i, j], 1, t[j] == t[i] + s[i] + d[i, j], $"TimeFlow_{i}_{j}");
                                    }
                                }
                            }
                        }

                        for (int i = 1; i <= N; i++)
                        {
                            model.AddConstr(e[i] >= a[i] - t[i], $"Early_{i}");
                            model.AddConstr(l[i] >= (t[i] + s[i]) - b[i], $"Late_{i}");
                        }

                        model.Optimize();

                        if (model.SolCount > 0)
                        {
                            if (model.Status == GRB.Status.OPTIMAL)
                                Console.WriteLine("SOLUTION is OPTIMAL");

                            Console.WriteLine($" COST GUROBI (ObjVal): {model.ObjVal:F2}");

                            int vehicleIndex = 1;
                            for (int first = 1; first <= N; first++)
                            {
                                if (x[0, first].X > 0.5) 
                                {
                                    Console.WriteLine($"\n--- ROUTE VEHICLE: {vehicleIndex++} ---");
                                    int curr = 0;
                                    int next = first;

                                    double currentOpTime = 0.0;
                                    double routeDist = 0.0;

                                    Console.Write("Depot (0) ");

                                    while (next != 0)
                                    {
                                        double stepDist = d[curr, next];
                                        routeDist += stepDist;
                                        currentOpTime += stepDist;

                                        double earlyPen = e[next].X;
                                        double latePen = l[next].X;

                                        Console.WriteLine($"\n  -> Location {next} (Distance: {stepDist:F1})");
                                        Console.WriteLine($"     Arrival: {currentOpTime:F1} |  Gurobi (t): {t[next].X:F1}");
                                        Console.WriteLine($"     Time window: [{a[next]}, {b[next]}]");

                                        if (earlyPen > 0 || latePen > 0)
                                            Console.WriteLine($"     PENALTY too early: {earlyPen:F1}, too late: {latePen:F1}");

                                        currentOpTime += s[next];
                                        curr = next;

                                        next = 0; 
                                        for (int j = 0; j <= N; j++)
                                        {
                                            if (curr != j && x[curr, j].X > 0.5)
                                            {
                                                next = j;
                                                break;
                                            }
                                        }
                                    }

                                    double returnDist = d[curr, 0];
                                    routeDist += returnDist;
                                    currentOpTime += returnDist;
                                    Console.WriteLine($"\n  -> Powrót do Magazynu (Odl: {returnDist:F1})");
                                    Console.WriteLine($"\nPodsumowanie pojazdu: Dystans {routeDist:F2}, Czas operacyjny {currentOpTime:F2}");
                                }
                            }
                            return model.ObjVal;
                        }
                        else
                        {
                            Console.WriteLine($"\nGurobi nie znalazło dopuszczalnego rozwiązania.");
                            return double.PositiveInfinity;
                        }
                    }
                }
            }
            catch (GRBException ex)
            {
                Console.WriteLine($"Błąd Gurobi: {ex.Message}");
                return double.PositiveInfinity;
            }
        }
    }
}