using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCVRPTW
{
    public class Vehicle
    {
        public double Capacity;
        public int Id;
        public bool isUsed;
        public Vehicle(int id, double capacity) {
            this.Id = id;
            this.Capacity = capacity;
            this.isUsed = false;
        }
    }
}
