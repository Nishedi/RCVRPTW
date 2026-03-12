//////using System;
//////using System.Collections.Generic;
//////using System.Linq;
//////using System.Text;
//////using System.Threading.Tasks;
//////using System;
//////using System.Linq;
//////using Gurobi;


//////namespace RCVRPTW
//////{
//////    public class CVRPTW_Model
//////    {
//////        public static void M()
//////        {
//////            try
//////            {
//////                // 1. DANE WEJŚCIOWE (Dummy Data)
//////                int N = 5; // Liczba klientów
//////                int numNodes = N + 1; // Magazyn (0) + Klienci (1..N)
//////                int V = 2; // Liczba pojazdów
//////                double Q = 90.0; // Pojemność pojazdu
//////                double alpha = 10.0; // Współczynnik kary za naruszenie okien
//////                double M = 10000.0; // Metoda Big-M

//////                // Macierz dystansów (d_ij) - symetryczna dla uproszczenia
//////                double[,] d = new double[numNodes, numNodes];
//////                Random rnd = new Random(42);
//////                for (int i = 0; i < numNodes; i++)
//////                    for (int j = 0; j < numNodes; j++)
//////                        d[i, j] = (i == j) ? 0 : rnd.Next(10, 50);

//////                // Parametry klientów (dla i = 0 parametry pozostają 0, ale ich nie używamy)
//////                double[] y = new double[numNodes]; // Zapotrzebowanie (Demand)
//////                double[] s = new double[numNodes]; // Czas obsługi (Service time)
//////                double[] a = new double[numNodes]; // Okno czasowe - start
//////                double[] b = new double[numNodes]; // Okno czasowe - koniec

//////                for (int i = 1; i <= N; i++)
//////                {
//////                    y[i] = rnd.Next(10, 30);
//////                    s[i] = rnd.Next(5, 15);
//////                    a[i] = rnd.Next(10, 50);
//////                    b[i] = a[i] + rnd.Next(20, 60);
//////                }

//////                // 2. INICJALIZACJA ŚRODOWISKA I MODELU GUROBI
//////                using (GRBEnv env = new GRBEnv(true))
//////                {
//////                    env.Set("LogFile", "cvrptw_gurobi.log");
//////                    env.Start();

//////                    using (GRBModel model = new GRBModel(env))
//////                    {
//////                        model.ModelName = "CVRPTW_SoftTimeWindows";

//////                        // 3. ZMIENNE DECYZYJNE
//////                        GRBVar[,,] x = new GRBVar[V, numNodes, numNodes];
//////                        GRBVar[,] z = new GRBVar[V, numNodes];
//////                        GRBVar[,] t = new GRBVar[V, numNodes];
//////                        GRBVar[,] w = new GRBVar[V, numNodes];
//////                        GRBVar[,] p = new GRBVar[V, numNodes];
//////                        GRBVar[,] q = new GRBVar[V, numNodes];

//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 0; i < numNodes; i++)
//////                            {
//////                                z[v, i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"z_{v}_{i}");
//////                                t[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"t_{v}_{i}");
//////                                w[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"w_{v}_{i}");
//////                                p[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"p_{v}_{i}");
//////                                q[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"q_{v}_{i}");

//////                                for (int j = 0; j < numNodes; j++)
//////                                {
//////                                    x[v, i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_{v}_{i}_{j}");
//////                                }
//////                            }
//////                        }

//////                        // 4. FUNKCJA CELU - Równanie (1)
//////                        GRBLinExpr obj = 0.0;
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 0; i < numNodes; i++)
//////                            {
//////                                for (int j = 0; j < numNodes; j++)
//////                                    obj.AddTerm(d[i, j], x[v, i, j]); // Koszt podróży

//////                                if (i > 0) // i \in \mathcal{N}
//////                                {
//////                                    obj.AddTerm(1.0, w[v, i]);        // Koszt oczekiwania
//////                                    obj.AddTerm(alpha, p[v, i]);      // Kara za wczesny przyjazd
//////                                    obj.AddTerm(alpha, q[v, i]);      // Kara za późny przyjazd
//////                                }
//////                            }
//////                        }
//////                        model.SetObjective(obj, GRB.MINIMIZE);

//////                        // 5. OGRANICZENIA

//////                        // Ograniczenie (2): Każdy klient odwiedzony dokładnie raz
//////                        for (int i = 1; i <= N; i++)
//////                        {
//////                            GRBLinExpr visitOnce = 0.0;
//////                            for (int v = 0; v < V; v++)
//////                                for (int j = 0; j < numNodes; j++)
//////                                    if (i != j) visitOnce.AddTerm(1.0, x[v, i, j]);

//////                            model.AddConstr(visitOnce == 1.0, $"VisitOnce_{i}");
//////                        }

//////                        // Ograniczenie (3) i (4): Zachowanie przepływu i powiązanie zmiennych z, x
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 1; i <= N; i++)
//////                            {
//////                                GRBLinExpr flowIn = 0.0;
//////                                GRBLinExpr flowOut = 0.0;

//////                                for (int j = 0; j < numNodes; j++)
//////                                {
//////                                    if (i != j)
//////                                    {
//////                                        flowIn.AddTerm(1.0, x[v, j, i]);
//////                                        flowOut.AddTerm(1.0, x[v, i, j]);
//////                                    }
//////                                }
//////                                // (3) Przepływ
//////                                model.AddConstr(flowIn == flowOut, $"FlowBalance_{v}_{i}");
//////                                // (4) Powiązanie z x
//////                                model.AddConstr(z[v, i] == flowIn, $"VisitCorrelation_{v}_{i}");
//////                            }
//////                        }

//////                        // Ograniczenie (5) i (6) - ZMODYFIKOWANE: Start i koniec w magazynie
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            GRBLinExpr leavesDepot = 0.0;
//////                            GRBLinExpr returnsDepot = 0.0;

//////                            for (int i = 1; i <= N; i++)
//////                            {
//////                                leavesDepot.AddTerm(1.0, x[v, 0, i]);
//////                                returnsDepot.AddTerm(1.0, x[v, i, 0]);
//////                            }

//////                            // Pojazd opuszcza magazyn co najwyżej 1 raz
//////                            model.AddConstr(leavesDepot <= 1.0, $"MaxOneDepotDeparture_{v}");
//////                            // Tyle samo razy wraca
//////                            model.AddConstr(leavesDepot == returnsDepot, $"DepotFlowBalance_{v}");
//////                        }

//////                        // Ograniczenie (7): Brak self-loopów
//////                        for (int v = 0; v < V; v++)
//////                            for (int i = 0; i < numNodes; i++)
//////                                model.AddConstr(x[v, i, i] == 0.0, $"NoSelfLoop_{v}_{i}");

//////                        // Ograniczenie (8): Spójność czasowa tras
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 0; i < numNodes; i++)
//////                            {
//////                                for (int j = 0; j < numNodes; j++)
//////                                {
//////                                    if (i != j)
//////                                    {
//////                                        model.AddConstr(t[v, j] >= t[v, i] + s[i] + d[i, j] - M * (1 - x[v, i, j]),
//////                                                        $"TimeCohesion_{v}_{i}_{j}");
//////                                    }
//////                                }
//////                            }
//////                        }

//////                        // Ograniczenie (9) i (10): Kary za miękkie okna czasowe (Klienci)
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 1; i <= N; i++)
//////                            {
//////                                // p_{v,i} >= a_i - t_{v,i} - M(1 - z_{v,i})
//////                                model.AddConstr(p[v, i] >= a[i] - t[v, i] - M * (1.0 - z[v, i]), $"SoftStart_{v}_{i}");

//////                                // t_{v,i} <= b_i + q_{v,i}
//////                                model.AddConstr(t[v, i] <= b[i] + q[v, i], $"SoftEnd_{v}_{i}");
//////                            }
//////                        }

//////                        // Ograniczenie (11) - ZMODYFIKOWANE: Czas oczekiwania
//////                        // Czas oczekiwania to przynajmniej różnica między początkiem okna a przyjazdem
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            for (int i = 1; i <= N; i++)
//////                            {
//////                                model.AddConstr(w[v, i] >= a[i] - t[v, i] - M * (1.0 - z[v, i]), $"WaitTime_{v}_{i}");
//////                                // (Wartość w_{v,i} jest naturalnie ograniczona od dołu przez 0 w deklaracji)
//////                            }
//////                        }

//////                        // Ograniczenie (12): Przepustowość
//////                        for (int v = 0; v < V; v++)
//////                        {
//////                            GRBLinExpr capExpr = 0.0;
//////                            for (int i = 1; i <= N; i++)
//////                            {
//////                                capExpr.AddTerm(y[i], z[v, i]);
//////                            }
//////                            model.AddConstr(capExpr <= Q, $"Capacity_{v}");
//////                        }

//////                        // 6. OPTYMALIZACJA
//////                        model.Optimize();

//////                        // 7. WYNIKI
//////                        if (model.Status == GRB.Status.OPTIMAL)
//////                        {
//////                            Console.WriteLine($"Znaleziono rozwiązanie optymalne! Koszt całkowity: {model.ObjVal}");
//////                            for (int v = 0; v < V; v++)
//////                            {
//////                                Console.WriteLine($"\nTrasa dla pojazdu {v}:");
//////                                for (int i = 0; i < numNodes; i++)
//////                                {
//////                                    for (int j = 0; j < numNodes; j++)
//////                                    {
//////                                        if (x[v, i, j].X > 0.5)
//////                                        {
//////                                            Console.WriteLine($"  Węzeł {i} -> Węzeł {j} (Przyjazd do {j}: {t[v, j].X:F1})");
//////                                        }
//////                                    }
//////                                }
//////                            }
//////                        }
//////                        else
//////                        {
//////                            Console.WriteLine($"Nie znaleziono rozwiązania optymalnego. Status: {model.Status}");
//////                        }
//////                    }
//////                }
//////            }
//////            catch (GRBException e)
//////            {
//////                Console.WriteLine($"Błąd Gurobi (Kod {e.ErrorCode}): {e.Message}");
//////            }
//////        }
//////    }
//////}

////using System;
////using Gurobi;
////using RCVRPTW;

////namespace RCVRPTW
////{
////    public class CVRPTW_Model
////    {
////        public static void Solve(Instance instance, double timeLimitSeconds = 300.0)
////        {
////            try
////            {
////                // 1. DANE WEJŚCIOWE Z INSTANCJIRF
////                int numNodes = instance.Locations.Count;
////                int N = numNodes - 1; // Liczba klientów (zakładamy, że węzeł 0 to magazyn)
////                int V = instance.Vehicles.Count;
////                double Q = instance.Vehicles[0].Capacity; // Zakładamy homogeniczną flotę
////                Console.WriteLine($"Instancja: {instance.FileName}, Klienci: {N}, Pojazdy: {V}, Pojemność: {Q}");

////                double alpha = instance.PenaltyFactor;
////                double M = 10000.0; // Wartość Big-M

////                double[,] d = instance.DistanceMatrix;

////                // Tablice na parametry wierzchołków
////                double[] y = new double[numNodes];
////                double[] s = new double[numNodes];
////                double[] a = new double[numNodes];
////                double[] b = new double[numNodes];

////                for (int i = 0; i < numNodes; i++)
////                {
////                    // ==========================================================
////                    // TODO: Podmień poniższe właściwości na nazwy z Twojej klasy Location
////                    // ==========================================================
////                    y[i] = instance.Locations[i].Demand; // Zapotrzebowanie
////                    s[i] = instance.Locations[i].ServiceTime; // Czas obsługi

////                    // Jeśli TimeWindow to krotka (tuple):
////                    a[i] = instance.Locations[i].TimeWindow.Start; // Start okna czasowego
////                    b[i] = instance.Locations[i].TimeWindow.End; // Koniec okna czasowego
////                }

////                // 2. INICJALIZACJA ŚRODOWISKA I MODELU GUROBI
////                using (GRBEnv env = new GRBEnv(true))
////                {
////                    env.Set("LogFile", "cvrptw_gurobi.log");
////                    env.Start();

////                    using (GRBModel model = new GRBModel(env))
////                    {
////                        model.ModelName = "CVRPTW_Solomon";

////                        // Ustawienie limitu czasu (bardzo ważne dla 100 klientów!)
////                        model.Parameters.TimeLimit = timeLimitSeconds;
////                        // Opcjonalnie: ustawienie nacisku na szukanie rozwiązań heurystycznych
////                        // model.Parameters.MIPFocus = 1; 

////                        // 3. ZMIENNE DECYZYJNE
////                        GRBVar[,,] x = new GRBVar[V, numNodes, numNodes];
////                        GRBVar[,] z = new GRBVar[V, numNodes];
////                        GRBVar[,] t = new GRBVar[V, numNodes];
////                        GRBVar[,] w = new GRBVar[V, numNodes];
////                        GRBVar[,] p = new GRBVar[V, numNodes];
////                        GRBVar[,] q = new GRBVar[V, numNodes];

////                        for (int v = 0; v < V; v++)
////                        {
////                            for (int i = 0; i < numNodes; i++)
////                            {
////                                z[v, i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"z_{v}_{i}");
////                                t[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"t_{v}_{i}");
////                                w[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"w_{v}_{i}");
////                                p[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"p_{v}_{i}");
////                                q[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"q_{v}_{i}");

////                                for (int j = 0; j < numNodes; j++)
////                                {
////                                    x[v, i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_{v}_{i}_{j}");
////                                }
////                            }
////                        }

////                        // 4. FUNKCJA CELU
////                        GRBLinExpr obj = 0.0;
////                        for (int v = 0; v < V; v++)
////                        {
////                            for (int i = 0; i < numNodes; i++)
////                            {
////                                for (int j = 0; j < numNodes; j++)
////                                    obj.AddTerm(d[i, j], x[v, i, j]); // Koszt podróży

////                                if (i > 0) // Klienci
////                                {
////                                    obj.AddTerm(instance.WaitingFactor, w[v, i]);
////                                    obj.AddTerm(instance.TooEarlyPenaltyFactor, p[v, i]);
////                                    obj.AddTerm(instance.TooLatePenaltyFactor, q[v, i]);
////                                }
////                            }
////                        }
////                        model.SetObjective(obj, GRB.MINIMIZE);

////                        // 5. OGRANICZENIA (Zoptymalizowane pod C#)
////                        for (int i = 1; i <= N; i++)
////                        {
////                            GRBLinExpr visitOnce = 0.0;
////                            for (int v = 0; v < V; v++)
////                                for (int j = 0; j < numNodes; j++)
////                                    if (i != j) visitOnce.AddTerm(1.0, x[v, i, j]);
////                            model.AddConstr(visitOnce == 1.0, $"VisitOnce_{i}");
////                        }

////                        for (int v = 0; v < V; v++)
////                        {
////                            for (int i = 1; i <= N; i++)
////                            {
////                                GRBLinExpr flowIn = 0.0;
////                                GRBLinExpr flowOut = 0.0;
////                                for (int j = 0; j < numNodes; j++)
////                                {
////                                    if (i != j)
////                                    {
////                                        flowIn.AddTerm(1.0, x[v, j, i]);
////                                        flowOut.AddTerm(1.0, x[v, i, j]);
////                                    }
////                                }
////                                model.AddConstr(flowIn == flowOut, $"FlowBalance_{v}_{i}");
////                                model.AddConstr(z[v, i] == flowIn, $"VisitCorrelation_{v}_{i}");
////                            }

////                            GRBLinExpr leavesDepot = 0.0;
////                            GRBLinExpr returnsDepot = 0.0;
////                            for (int i = 1; i <= N; i++)
////                            {
////                                leavesDepot.AddTerm(1.0, x[v, 0, i]);
////                                returnsDepot.AddTerm(1.0, x[v, i, 0]);
////                            }
////                            model.AddConstr(leavesDepot <= 1.0, $"MaxOneDepotDeparture_{v}");
////                            model.AddConstr(leavesDepot == returnsDepot, $"DepotFlowBalance_{v}");

////                            for (int i = 0; i < numNodes; i++)
////                                model.AddConstr(x[v, i, i] == 0.0, $"NoSelfLoop_{v}_{i}");

////                            GRBLinExpr capExpr = 0.0;
////                            for (int i = 1; i <= N; i++)
////                                capExpr.AddTerm(y[i], z[v, i]);
////                            model.AddConstr(capExpr <= Q, $"Capacity_{v}");
////                        }

////                        // POPRAWIONY BLOK KODU:
////                        for (int v = 0; v < V; v++)
////                        {
////                            for (int i = 0; i < numNodes; i++)
////                            {
////                                // ZMIANA TUTAJ: j zaczyna się od 1 (pomijamy powrót do magazynu w śledzeniu czasu)
////                                for (int j = 1; j < numNodes; j++)
////                                {
////                                    if (i != j)
////                                    {
////                                        model.AddConstr(t[v, j] >= t[v, i] + s[i] + d[i, j] - M * (1 - x[v, i, j]),
////                                                        $"TimeCohesion_{v}_{i}_{j}");
////                                    }
////                                }
////                            }
////                        }

////                        for (int v = 0; v < V; v++)
////                        {
////                            for (int i = 1; i <= N; i++)
////                            {
////                                model.AddConstr(p[v, i] >= a[i] - t[v, i] - M * (1.0 - z[v, i]), $"SoftStart_{v}_{i}");
////                                model.AddConstr(t[v, i] <= b[i] + q[v, i], $"SoftEnd_{v}_{i}");
////                                model.AddConstr(w[v, i] >= a[i] - t[v, i] - M * (1.0 - z[v, i]), $"WaitTime_{v}_{i}");
////                            }
////                        }

////                        // 6. OPTYMALIZACJA
////                        Console.WriteLine($"Rozpoczynam optymalizację w Gurobi. Limit czasu: {timeLimitSeconds}s");
////                        model.Optimize();

////                        // 7. WYNIKI
////                        // 7. WYNIKI
////                        if (model.SolCount > 0)
////                        {
////                            Console.WriteLine($"\n=== ZNALEZIONO ROZWIĄZANIE ===");
////                            Console.WriteLine($"Całkowity koszt funkcji celu (Gurobi ObjVal): {model.ObjVal:F2}");

////                            double totalRouteDistance = 0.0;
////                            double totalPenalties = 0.0;

////                            for (int v = 0; v < V; v++)
////                            {
////                                // Sprawdzamy, czy pojazd wyjechał z magazynu (węzeł 0)
////                                bool vehicleUsed = false;
////                                for (int j = 1; j <= N; j++)
////                                {
////                                    if (x[v, 0, j].X > 0.5) { vehicleUsed = true; break; }
////                                }

////                                if (!vehicleUsed) continue;

////                                Console.WriteLine($"\n--- TRASA POJAZDU {v} ---");
////                                int currentNode = 0;
////                                double routeDist = 0.0;

////                                Console.Write("Magazyn(0) ");

////                                while (true)
////                                {
////                                    int nextNode = -1;
////                                    // Szukamy następnego węzła na trasie
////                                    for (int j = 0; j < numNodes; j++)
////                                    {
////                                        if (x[v, currentNode, j].X > 0.5)
////                                        {
////                                            nextNode = j;
////                                            break;
////                                        }
////                                    }

////                                    if (nextNode == -1) break; // Zabezpieczenie przed błędem

////                                    routeDist += d[currentNode, nextNode];

////                                    if (nextNode != 0)
////                                    {
////                                        double arrTime = t[v, nextNode].X;
////                                        double earlyPen = p[v, nextNode].X;
////                                        double latePen = q[v, nextNode].X;
////                                        double waitTime = w[v, nextNode].X;

////                                        Console.WriteLine($"\n  -> Klient {nextNode} (Odl: {d[currentNode, nextNode]:F1})");
////                                        Console.WriteLine($"     Przyjazd: {arrTime:F1} | Okno: [{a[nextNode]}, {b[nextNode]}]");

////                                        if (earlyPen > 0 || latePen > 0)
////                                        {
////                                            Console.WriteLine($"     KARA: za wcześnie: {earlyPen:F1}, spóźnienie: {latePen:F1}");
////                                            totalPenalties += (earlyPen * instance.TooEarlyPenaltyFactor) + (latePen * instance.TooLatePenaltyFactor);
////                                        }
////                                        if (waitTime > 0)
////                                        {
////                                            Console.WriteLine($"     Czekanie przed oknem: {waitTime:F1}");
////                                        }
////                                    }
////                                    else
////                                    {
////                                        Console.WriteLine($"\n  -> Powrót do Magazynu (Odl: {d[currentNode, nextNode]:F1})");
////                                    }

////                                    currentNode = nextNode;
////                                    if (currentNode == 0) break; // Koniec trasy (wróciliśmy do bazy)
////                                }

////                                Console.WriteLine($"\nDystans tej trasy: {routeDist:F2}");
////                                totalRouteDistance += routeDist;
////                            }

////                            Console.WriteLine($"\n=== PODSUMOWANIE ===");
////                            Console.WriteLine($"Zsumowany dystans tras: {totalRouteDistance:F2}");
////                            Console.WriteLine($"Zsumowane kary (bez mnożników): {totalPenalties:F2}");
////                        }
////                        else
////                        {
////                            Console.WriteLine($"\nGurobi nie znalazło ŻADNEGO dopuszczalnego rozwiązania w czasie limitu.");
////                            Console.WriteLine($"Status: {model.Status}");
////                            if (model.Status == GRB.Status.TIME_LIMIT)
////                            {
////                                Console.WriteLine("Porada: Instancje 100 klientów są olbrzymie dla MILP. Spróbuj dać solverowi więcej czasu, albo ustawić go na szukanie rozwiązań heurystycznych.");
////                            }
////                            if (model.Status == GRB.Status.INFEASIBLE)
////                            {
////                                Console.WriteLine("Model jest sprzeczny (Infeasible) - prawdopodobnie zbyt ostre okna czasowe w stosunku do dystansów/pojemności.");
////                                model.ComputeIIS();
////                                model.Write("sprzecznosci.ilp");
////                            }
////                        }

////                    }
////                }
////            }
////            catch (GRBException e)
////            {
////                Console.WriteLine($"Błąd Gurobi (Kod {e.ErrorCode}): {e.Message}");
////            }
////        }
////    }
////}
//using System;
//using Gurobi;
//using RCVRPTW;

//namespace RCVRPTW
//{
//    public class CVRPTW_Model
//    {
//        public static void Solve(Instance instance, double timeLimitSeconds = 300.0)
//        {
//            try
//            {
//                // 1. DANE WEJŚCIOWE Z INSTANCJI
//                int numNodes = instance.Locations.Count;
//                int N = numNodes - 1; // Liczba klientów (zakładamy, że węzeł 0 to magazyn)
//                int V = instance.Vehicles.Count;
//                double Q = instance.Vehicles[0].Capacity; // Zakładamy homogeniczną flotę
//                Console.WriteLine($"Instancja: {instance.FileName}, Klienci: {N}, Pojazdy: {V}, Pojemność: {Q}");

//                double M = 10000.0; // Wartość Big-M
//                double[,] d = instance.DistanceMatrix;

//                // Tablice na parametry wierzchołków
//                double[] y = new double[numNodes];
//                double[] s = new double[numNodes];
//                double[] a = new double[numNodes];
//                double[] b = new double[numNodes];

//                for (int i = 0; i < numNodes; i++)
//                {
//                    y[i] = instance.Locations[i].Demand; // Zapotrzebowanie
//                    s[i] = instance.Locations[i].ServiceTime; // Czas obsługi
//                    a[i] = instance.Locations[i].TimeWindow.Start; // Start okna czasowego
//                    b[i] = instance.Locations[i].TimeWindow.End; // Koniec okna czasowego
//                }

//                // 2. INICJALIZACJA ŚRODOWISKA I MODELU GUROBI
//                using (GRBEnv env = new GRBEnv(true))
//                {
//                    env.Set("LogFile", "cvrptw_gurobi.log");
//                    env.Start();

//                    using (GRBModel model = new GRBModel(env))
//                    {
//                        model.ModelName = "CVRPTW_Solomon";

//                        // Ustawienie limitu czasu
//                        model.Parameters.TimeLimit = timeLimitSeconds;

//                        // 3. ZMIENNE DECYZYJNE
//                        GRBVar[,,] x = new GRBVar[V, numNodes, numNodes];
//                        GRBVar[,] z = new GRBVar[V, numNodes];
//                        GRBVar[,] t = new GRBVar[V, numNodes]; // Czas przyjazdu (Arrival Time)
//                        GRBVar[,] w = new GRBVar[V, numNodes]; // Czas oczekiwania przed oknem (Wait)
//                        GRBVar[,] e = new GRBVar[V, numNodes]; // Earliness (wczesność startu obsługi)
//                        GRBVar[,] l = new GRBVar[V, numNodes]; // Lateness (spóźnienie na koniec obsługi)

//                        for (int v = 0; v < V; v++)
//                        {
//                            for (int i = 0; i < numNodes; i++)
//                            {
//                                z[v, i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"z_{v}_{i}");
//                                t[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"t_{v}_{i}");
//                                w[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"w_{v}_{i}");
//                                e[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"e_{v}_{i}");
//                                l[v, i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"l_{v}_{i}");

//                                for (int j = 0; j < numNodes; j++)
//                                {
//                                    x[v, i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_{v}_{i}_{j}");
//                                }
//                            }
//                        }

//                        // 4. FUNKCJA CELU ODWZOROWUJĄCA 'calculateMetrics' Z C#
//                        GRBLinExpr totalDistance = 0.0;
//                        GRBLinExpr totalWait = 0.0;
//                        GRBLinExpr totalService = 0.0;
//                        GRBLinExpr totalEarlyPenalty = 0.0;
//                        GRBLinExpr totalLatePenalty = 0.0;

//                        for (int v = 0; v < V; v++)
//                        {
//                            for (int i = 0; i < numNodes; i++)
//                            {
//                                for (int j = 0; j < numNodes; j++)
//                                {
//                                    totalDistance.AddTerm(d[i, j], x[v, i, j]);
//                                }

//                                if (i > 0) // Pomijamy magazyn w karach i czasach obsługi
//                                {
//                                    totalWait.AddTerm(1.0, w[v, i]);
//                                    totalService.AddTerm(s[i], z[v, i]); // Czas obsługi doliczamy tylko dla odwiedzonych klientów
//                                    totalEarlyPenalty.AddTerm(1.0, e[v, i]);
//                                    totalLatePenalty.AddTerm(1.0, l[v, i]);
//                                }
//                            }
//                        }

//                        // Czas operacyjny to suma dystansów, oczekiwania i obsługi
//                        GRBLinExpr totalVehicleOperationTime = totalDistance + totalWait + totalService;

//                        // Suma kar ze specyficznymi mnożnikami
//                        GRBLinExpr totalPenalty = (instance.TooEarlyPenaltyFactor * totalEarlyPenalty) + 
//                                                  (instance.TooLatePenaltyFactor * totalLatePenalty);

//                        // OSTATECZNA FUNKCJA CELU
//                        GRBLinExpr obj = (instance.DistanceFactor * totalDistance) + 
//                                         (instance.WaitingFactor * totalVehicleOperationTime) + 
//                                         (instance.PenaltyFactor * totalPenalty);

//                        model.SetObjective(obj, GRB.MINIMIZE);

//                        // 5. OGRANICZENIA LOGICZNE (Trasy i Pojemność)
//                        for (int i = 1; i <= N; i++)
//                        {
//                            GRBLinExpr visitOnce = 0.0;
//                            for (int v = 0; v < V; v++)
//                                for (int j = 0; j < numNodes; j++)
//                                    if (i != j) visitOnce.AddTerm(1.0, x[v, i, j]);
//                            model.AddConstr(visitOnce == 1.0, $"VisitOnce_{i}");
//                        }

//                        for (int v = 0; v < V; v++)
//                        {
//                            for (int i = 1; i <= N; i++)
//                            {
//                                GRBLinExpr flowIn = 0.0;
//                                GRBLinExpr flowOut = 0.0;
//                                for (int j = 0; j < numNodes; j++)
//                                {
//                                    if (i != j)
//                                    {
//                                        flowIn.AddTerm(1.0, x[v, j, i]);
//                                        flowOut.AddTerm(1.0, x[v, i, j]);
//                                    }
//                                }
//                                model.AddConstr(flowIn == flowOut, $"FlowBalance_{v}_{i}");
//                                model.AddConstr(z[v, i] == flowIn, $"VisitCorrelation_{v}_{i}");
//                            }

//                            GRBLinExpr leavesDepot = 0.0;
//                            GRBLinExpr returnsDepot = 0.0;
//                            for (int i = 1; i <= N; i++)
//                            {
//                                leavesDepot.AddTerm(1.0, x[v, 0, i]);
//                                returnsDepot.AddTerm(1.0, x[v, i, 0]);
//                            }
//                            model.AddConstr(leavesDepot <= 1.0, $"MaxOneDepotDeparture_{v}");
//                            model.AddConstr(leavesDepot == returnsDepot, $"DepotFlowBalance_{v}");

//                            for (int i = 0; i < numNodes; i++)
//                                model.AddConstr(x[v, i, i] == 0.0, $"NoSelfLoop_{v}_{i}");

//                            GRBLinExpr capExpr = 0.0;
//                            for (int i = 1; i <= N; i++)
//                                capExpr.AddTerm(y[i], z[v, i]);
//                            model.AddConstr(capExpr <= Q, $"Capacity_{v}");
//                        }

//                        // 6. OGRANICZENIA CZASOWE
//                        for (int v = 0; v < V; v++)
//                        {
//                            for (int i = 0; i < numNodes; i++)
//                            {
//                                // Przesunięcie czasu między węzłami
//                                for (int j = 1; j < numNodes; j++) // j=1, aby pomijać powrót do magazynu (węzeł 0)
//                                {
//                                    if (i != j)
//                                    {
//                                        model.AddConstr(t[v, j] >= t[v, i] + w[v, i] + s[i] + d[i, j] - M * (1 - x[v, i, j]),
//                                                        $"TimeFlow_{v}_{i}_{j}");
//                                    }
//                                }
//                            }
//                        }

//                        for (int v = 0; v < V; v++)
//                        {
//                            for (int i = 1; i <= N; i++)
//                            {
//                                // Kara za wczesność: Start obsługi to (t + w). Earliness to StartOkna - StartObsługi.
//                                model.AddConstr(e[v, i] >= a[i] - (t[v, i] + w[v, i]) - M * (1.0 - z[v, i]), $"Early_{v}_{i}");

//                                // Kara za spóźnienie: Koniec obsługi to (t + w + s). Lateness to KoniecObsługi - KoniecOkna.
//                                model.AddConstr(l[v, i] >= (t[v, i] + w[v, i] + s[i]) - b[i] - M * (1.0 - z[v, i]), $"Late_{v}_{i}");
//                            }
//                        }

//                        // 7. OPTYMALIZACJA
//                        Console.WriteLine($"Rozpoczynam optymalizację w Gurobi. Limit czasu: {timeLimitSeconds}s");
//                        model.Optimize();

//                        // 8. WYNIKI
//                        if (model.SolCount > 0)
//                        {
//                            Console.WriteLine($"\n=== ZNALEZIONO ROZWIĄZANIE ===");
//                            Console.WriteLine($"Całkowity koszt funkcji celu (Gurobi ObjVal): {model.ObjVal:F2}");

//                            double totalRouteDistance = 0.0;
//                            double totalPenalties = 0.0;

//                            for (int v = 0; v < V; v++)
//                            {
//                                // Sprawdzamy, czy pojazd wyjechał z magazynu (węzeł 0)
//                                bool vehicleUsed = false;
//                                for (int j = 1; j <= N; j++)
//                                {
//                                    if (x[v, 0, j].X > 0.5) { vehicleUsed = true; break; }
//                                }

//                                if (!vehicleUsed) continue;

//                                Console.WriteLine($"\n--- TRASA POJAZDU {v} ---");
//                                int currentNode = 0;
//                                double routeDist = 0.0;

//                                Console.Write("Magazyn(0) ");

//                                while (true)
//                                {
//                                    int nextNode = -1;
//                                    // Szukamy następnego węzła na trasie
//                                    for (int j = 0; j < numNodes; j++)
//                                    {
//                                        if (x[v, currentNode, j].X > 0.5)
//                                        {
//                                            nextNode = j;
//                                            break;
//                                        }
//                                    }

//                                    if (nextNode == -1) break; // Zabezpieczenie przed błędem

//                                    routeDist += d[currentNode, nextNode];

//                                    if (nextNode != 0)
//                                    {
//                                        double arrTime = t[v, nextNode].X;
//                                        double waitTime = w[v, nextNode].X;
//                                        double earlyPen = e[v, nextNode].X; // Wczesność
//                                        double latePen = l[v, nextNode].X;  // Spóźnienie

//                                        Console.WriteLine($"\n  -> Klient {nextNode} (Odl: {d[currentNode, nextNode]:F1})");
//                                        Console.WriteLine($"     Przyjazd: {arrTime:F1} | Okno: [{a[nextNode]}, {b[nextNode]}]");

//                                        if (earlyPen > 0 || latePen > 0)
//                                        {
//                                            Console.WriteLine($"     KARA: za wcześnie: {earlyPen:F1}, spóźnienie: {latePen:F1}");
//                                            totalPenalties += (earlyPen * instance.TooEarlyPenaltyFactor) + (latePen * instance.TooLatePenaltyFactor);
//                                        }
//                                        if (waitTime > 0)
//                                        {
//                                            Console.WriteLine($"     Czekanie przed oknem: {waitTime:F1}");
//                                        }
//                                    }
//                                    else
//                                    {
//                                        Console.WriteLine($"\n  -> Powrót do Magazynu (Odl: {d[currentNode, nextNode]:F1})");
//                                    }

//                                    currentNode = nextNode;
//                                    if (currentNode == 0) break; // Koniec trasy (wróciliśmy do bazy)
//                                }

//                                Console.WriteLine($"\nDystans tej trasy: {routeDist:F2}");
//                                totalRouteDistance += routeDist;
//                            }

//                            Console.WriteLine($"\n=== PODSUMOWANIE ===");
//                            Console.WriteLine($"Zsumowany dystans tras: {totalRouteDistance:F2}");
//                            Console.WriteLine($"Zsumowane kary (z uwzględnieniem mnożników instancji): {totalPenalties:F2}");
//                        }
//                        else
//                        {
//                            Console.WriteLine($"\nGurobi nie znalazło ŻADNEGO dopuszczalnego rozwiązania w czasie limitu.");
//                            Console.WriteLine($"Status: {model.Status}");
//                            if (model.Status == GRB.Status.TIME_LIMIT)
//                            {
//                                Console.WriteLine("Porada: Instancje 100 klientów są olbrzymie dla MILP. Spróbuj dać solverowi więcej czasu, albo ustawić go na szukanie rozwiązań heurystycznych.");
//                            }
//                            if (model.Status == GRB.Status.INFEASIBLE)
//                            {
//                                Console.WriteLine("Model jest sprzeczny (Infeasible) - prawdopodobnie zbyt ostre okna czasowe w stosunku do dystansów/pojemności.");
//                                model.ComputeIIS();
//                                model.Write("sprzecznosci.ilp");
//                            }
//                        }
//                    }
//                }
//            }
//            catch (GRBException ex)
//            {
//                Console.WriteLine($"Błąd Gurobi (Kod {ex.ErrorCode}): {ex.Message}");
//            }
//        }
//    }
//}

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
                // 1. DANE WEJŚCIOWE
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

                        // 3. ZMIENNE DECYZYJNE (BRAK INDEKSU POJAZDU 'v'!)
                        GRBVar[,] x = new GRBVar[numNodes, numNodes]; // 1, jeśli przejeżdżamy z i do j
                        GRBVar[] t = new GRBVar[numNodes]; // Czas przyjazdu do węzła i
                        GRBVar[] u = new GRBVar[numNodes]; // Skumulowany ładunek w węźle i
                        GRBVar[] e = new GRBVar[numNodes]; // Wczesność
                        GRBVar[] l = new GRBVar[numNodes]; // Spóźnienie

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

                        // Czas w magazynie wynosi 0
                        model.AddConstr(t[0] == 0.0, "StartTimeDepot");

                        // 4. FUNKCJA CELU
                        GRBLinExpr totalDistance = 0.0;
                        GRBLinExpr totalEarlyPenalty = 0.0;
                        GRBLinExpr totalLatePenalty = 0.0;
                        double totalServiceTimeConstant = 0.0; // Wszyscy klienci i tak muszą być obsłużeni

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

                        // 5. OGRANICZENIA LOGICZNE

                        // Każdy klient musi być odwiedzony dokładnie raz
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

                        // Liczba wyjazdów z bazy <= Max Pojazdów
                        GRBLinExpr depotOut = 0.0;
                        GRBLinExpr depotIn = 0.0;
                        for (int j = 1; j <= N; j++)
                        {
                            depotOut.AddTerm(1.0, x[0, j]);
                            depotIn.AddTerm(1.0, x[j, 0]);
                        }
                        model.AddConstr(depotOut <= maxV, "MaxVehicles");
                        model.AddConstr(depotOut == depotIn, "DepotBalance"); // Tyle samo powrotów co wyjazdów

                        // Brak self-loopów
                        for (int i = 0; i < numNodes; i++)
                            model.AddConstr(x[i, i] == 0.0, $"NoSelfLoop_{i}");

                        // 6. SUBTOURY, POJEMNOŚĆ I CZAS
                        for (int i = 0; i < numNodes; i++)
                        {
                            for (int j = 1; j <= N; j++)
                            {
                                if (i != j)
                                {
                                    // MTZ Pojemność (eliminuje też subtoury bez powrotu do bazy)
                                    if (i > 0)
                                    {
                                        model.AddConstr(u[j] >= u[i] + y[j] - Q * (1 - x[i, j]), $"Capacity_{i}_{j}");
                                    }

                                    // Dokładny czas przyjazdu (No-Wait) wyliczany warunkowo (Indicator)
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

                        // KARY
                        for (int i = 1; i <= N; i++)
                        {
                            model.AddConstr(e[i] >= a[i] - t[i], $"Early_{i}");
                            model.AddConstr(l[i] >= (t[i] + s[i]) - b[i], $"Late_{i}");
                        }

                        // 7. OPTYMALIZACJA
                        Console.WriteLine($"Rozpoczynam zoptymalizowane liczenie 2-indeksowe...");
                        model.Optimize();

                        // 8. WYNIKI (Odtwarzanie ścieżek z dwuindeksowej macierzy)
                        if (model.SolCount > 0)
                        {
                            Console.WriteLine($"\n=== ZNALEZIONO ROZWIĄZANIE ===");
                            if (model.Status == GRB.Status.OPTIMAL)
                                Console.WriteLine(">>> UDOWODNIONE OPTIMUM ZNALEZIONE! <<<");

                            Console.WriteLine($"KOSZT Z GUROBI (ObjVal): {model.ObjVal:F2}");

                            int vehicleIndex = 1;
                            for (int first = 1; first <= N; first++)
                            {
                                if (x[0, first].X > 0.5) // Znalazł wyjazd z bazy
                                {
                                    Console.WriteLine($"\n--- TRASA POJAZDU {vehicleIndex++} ---");
                                    int curr = 0;
                                    int next = first;

                                    double currentOpTime = 0.0;
                                    double routeDist = 0.0;

                                    Console.Write("Magazyn(0) ");

                                    while (next != 0)
                                    {
                                        double stepDist = d[curr, next];
                                        routeDist += stepDist;
                                        currentOpTime += stepDist;

                                        double earlyPen = e[next].X;
                                        double latePen = l[next].X;

                                        Console.WriteLine($"\n  -> Klient {next} (Odl: {stepDist:F1})");
                                        Console.WriteLine($"     Przyjazd: {currentOpTime:F1} | wg Gurobi (t): {t[next].X:F1}");
                                        Console.WriteLine($"     Okno: [{a[next]}, {b[next]}]");

                                        if (earlyPen > 0 || latePen > 0)
                                            Console.WriteLine($"     KARA: za wcześnie: {earlyPen:F1}, spóźnienie: {latePen:F1}");

                                        currentOpTime += s[next];
                                        curr = next;

                                        // Szukamy następnego kroku
                                        next = 0; // domyślnie powrót do bazy
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