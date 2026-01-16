using RCVRPTW;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RCVRPTW
{
    public  class Instance
    {
        public List<Location> Locations { get; set; } = new List<Location>();
        public double[,] DistanceMatrix;
        public List<Vehicle> Vehicles = new List<Vehicle>();
        public string FileName;
        public double TooEarlyPenaltyFactor = 1.0;
        public double TooLatePenaltyFactor = 1.0;
        public double WaitingFactor = 1.0;
        public double DistanceFactor = 1.0;
        public double PenaltyFactor = 1.0;
        public Instance(string filename, int vehicleNumbers=100, bool randomDemands = false, bool randomTimeWindow = false, 
            double waitingFactor=1.0, double distanceFactor=1.0, double penaltyFactor=1.0,
            double toEarlyPenaltyFactor=1.0, double toLatePenaltyFactor = 1.0, Random rng=null)
        {
            FileName = filename;
            ParseSolomonFile(filename, randomDemands, randomTimeWindow,rng);
            for(int i = 0; i < vehicleNumbers; i++)
            {
                Vehicles.Add(new Vehicle(0, 90.0));
            }
            WaitingFactor = waitingFactor;
            DistanceFactor = distanceFactor;
            PenaltyFactor = penaltyFactor;
            TooEarlyPenaltyFactor = toEarlyPenaltyFactor;
            TooLatePenaltyFactor = toLatePenaltyFactor;
        }


        public void ParseSolomonFile(string filePath, bool randomDemands, bool randomTimeWindow, Random rng)//typowy plik solomona
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
                        (double.Parse(parts[4], CultureInfo.InvariantCulture) * 0.1, double.Parse(parts[5], CultureInfo.InvariantCulture) * 0.1),
                        (int)double.Parse(parts[6], CultureInfo.InvariantCulture),

                        1,randomDemands,randomTimeWindow,rng:rng
                    ));

                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Błąd parsowania linii: {trimmedLine}. Szczegóły: {ex.Message}");
                }
            }
            DistanceMatrix = createDistanceMatrix();
        }
        public double[,] createDistanceMatrix() 
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
