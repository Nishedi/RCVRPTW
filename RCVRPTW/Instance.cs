using RCVRPTW;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//public int Id { get; set; }
//public LocationType Type { get; set; }
//public int X { get; set; }
//public int Y { get; set; }
//public double DemandMean { get; set; }
//public double DemandStdDev { get; set; }
//public (int Start, int End) TimeWindow { get; set; }
//public int ServiceTime { get; set; }
//public int Priority { get; set; }
namespace RCVRPTW
{
    internal class Instance
    {
        public List<Location> Locations { get; set; } = new List<Location>();
        public double[,] DistanceMatrix;
        public void ParseSolomonFile(string filePath)//typowy plik solomona
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 7) continue;

                try
                {
                    Locations.Add(new Location(
                        int.Parse(parts[0]) - 1,
                        int.Parse(parts[0]) == 1 ? LocationType.Depot : LocationType.Customer,
                        (int)double.Parse(parts[1], CultureInfo.InvariantCulture),
                        (int)double.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture) * 0.2, // odchylenie
                        ((int)double.Parse(parts[4], CultureInfo.InvariantCulture), (int)double.Parse(parts[5], CultureInfo.InvariantCulture)),
                        (int)double.Parse(parts[6], CultureInfo.InvariantCulture),
                        1
                    ));

                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Błąd parsowania linii: {trimmedLine}. Szczegóły: {ex.Message}");
                }
            }
            DistanceMatrix = createDistanceMatrix();
        }
        public double[,] createDistanceMatrix() // funkcja generująca macierz odleglosci
        {
            double[,] distanceMatrix = new double[Locations.Count, Locations.Count];
            for (int i = 0; i < Locations.Count; i++)
            {
                for (int j = 0; j < Locations.Count; j++)
                {
                    double deltaX = Locations[j].X - Locations[i].X;
                    double deltaY = Locations[j].Y - Locations[i].Y;
                    double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    distanceMatrix[i, j] = distance;
                }
            }
            return distanceMatrix;
        }

    }
     

    }
