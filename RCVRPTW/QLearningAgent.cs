using System;
using System.Collections.Generic;
using System.Linq;

namespace RCVRPTW
{
    /// <summary>
    /// Q-Learning based agent for solving RCVRPTW using Reinforcement Learning.
    /// The agent learns to construct routes by selecting locations sequentially,
    /// learning from experience which decisions lead to better overall solutions.
    /// </summary>
    internal class QLearningAgent
    {
        // Configuration constants
        private const int LOAD_DISCRETIZATION_LEVELS = 10;
        private const int TIME_DISCRETIZATION_LEVELS = 10;
        private const double DEFAULT_TIME_HORIZON = 500.0;
        private const double CAPACITY_VIOLATION_TOLERANCE = 1.1; // Allow 10% capacity violation during training
        private const double TIME_WINDOW_PENALTY_WEIGHT = 10.0;
        private const double CAPACITY_PENALTY_WEIGHT = 100.0;
        
        private Dictionary<string, Dictionary<int, double>> qTable;
        private Random random;
        private double learningRate;
        private double discountFactor;
        private double epsilon; // exploration rate
        private double epsilonDecay;
        private double epsilonMin;

        public QLearningAgent(double learningRate = 0.1, double discountFactor = 0.95, 
                             double epsilon = 1.0, double epsilonDecay = 0.995, double epsilonMin = 0.01,
                             int seed = 42)
        {
            this.qTable = new Dictionary<string, Dictionary<int, double>>();
            this.random = new Random(seed);
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
            this.epsilon = epsilon;
            this.epsilonDecay = epsilonDecay;
            this.epsilonMin = epsilonMin;
        }

        /// <summary>
        /// Get Q-value for a state-action pair
        /// </summary>
        private double GetQValue(string state, int action)
        {
            if (!qTable.ContainsKey(state))
            {
                qTable[state] = new Dictionary<int, double>();
            }
            if (!qTable[state].ContainsKey(action))
            {
                qTable[state][action] = 0.0;
            }
            return qTable[state][action];
        }

        /// <summary>
        /// Update Q-value for a state-action pair
        /// </summary>
        private void UpdateQValue(string state, int action, double reward, string nextState, List<int> possibleNextActions)
        {
            double currentQ = GetQValue(state, action);
            double maxNextQ = 0.0;

            if (possibleNextActions.Count > 0)
            {
                maxNextQ = possibleNextActions.Max(a => GetQValue(nextState, a));
            }

            double newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
            qTable[state][action] = newQ;
        }

        /// <summary>
        /// Select action using epsilon-greedy policy
        /// </summary>
        private int SelectAction(string state, List<int> possibleActions)
        {
            if (possibleActions.Count == 0)
                return -1;

            // Exploration
            if (random.NextDouble() < epsilon)
            {
                return possibleActions[random.Next(possibleActions.Count)];
            }

            // Exploitation: select action with highest Q-value
            int bestAction = possibleActions[0];
            double bestQ = GetQValue(state, bestAction);

            foreach (int action in possibleActions.Skip(1))
            {
                double q = GetQValue(state, action);
                if (q > bestQ)
                {
                    bestQ = q;
                    bestAction = action;
                }
            }

            return bestAction;
        }

        /// <summary>
        /// Create state representation from current route state
        /// </summary>
        private string CreateStateRepresentation(int currentLocation, double currentLoad, 
                                                 double currentTime, List<int> unvisitedLocations,
                                                 Instance instance)
        {
            // Simplified state representation: current location, load level, time level, number of unvisited
            double capacity = Math.Max(1.0, instance.Vehicles[0].Capacity); // Avoid division by zero
            int loadLevel = (int)(currentLoad / capacity * LOAD_DISCRETIZATION_LEVELS);
            int timeLevel = (int)(currentTime / DEFAULT_TIME_HORIZON * TIME_DISCRETIZATION_LEVELS);
            int unvisitedCount = unvisitedLocations.Count;
            
            return $"{currentLocation}_{loadLevel}_{timeLevel}_{unvisitedCount}";
        }

        /// <summary>
        /// Calculate immediate reward for selecting a location
        /// </summary>
        private double CalculateReward(Location currentLoc, Location nextLoc, 
                                       double currentTime, double currentLoad, 
                                       Instance instance)
        {
            double travelCost = instance.DistanceMatrix[currentLoc.Id, nextLoc.Id];
            double arrivalTime = currentTime + travelCost;
            double serviceStartTime = Math.Max(arrivalTime, nextLoc.TimeWindow.Start);
            double waitingTime = serviceStartTime - arrivalTime;
            
            // Penalties for time window violations
            double earlyPenalty = Math.Max(0, nextLoc.TimeWindow.Start - serviceStartTime);
            double latePenalty = Math.Max(0, serviceStartTime - nextLoc.TimeWindow.End);
            
            // Capacity constraint violation
            double newLoad = currentLoad + nextLoc.Demand;
            double capacityViolation = Math.Max(0, newLoad - instance.Vehicles[0].Capacity);
            
            // Composite reward (negative cost)
            double reward = -(travelCost + waitingTime + 
                            TIME_WINDOW_PENALTY_WEIGHT * (earlyPenalty + latePenalty) + 
                            CAPACITY_PENALTY_WEIGHT * capacityViolation);
            
            return reward;
        }

        /// <summary>
        /// Train the agent on a given instance
        /// </summary>
        public void Train(Instance instance, int episodes = 100, int maxSteps = 200)
        {
            Console.WriteLine($"Training Q-Learning agent for {episodes} episodes...");
            
            for (int episode = 0; episode < episodes; episode++)
            {
                // Initialize episode
                List<int> unvisited = instance.Locations
                    .Where(l => l.Id != 0)
                    .Select(l => l.Id)
                    .ToList();
                
                int currentLocationId = 0; // Start at depot
                double currentLoad = 0.0;
                double currentTime = 0.0;
                double episodeReward = 0.0;
                int steps = 0;

                while (unvisited.Count > 0 && steps < maxSteps)
                {
                    Location currentLoc = instance.Locations.First(l => l.Id == currentLocationId);
                    string state = CreateStateRepresentation(currentLocationId, currentLoad, currentTime, unvisited, instance);
                    
                    // Filter feasible actions (locations that can be visited)
                    List<int> feasibleActions = new List<int>();
                    foreach (int locId in unvisited)
                    {
                        Location loc = instance.Locations.First(l => l.Id == locId);
                        // Allow slight capacity violation during training for exploration
                        if (currentLoad + loc.Demand <= instance.Vehicles[0].Capacity * CAPACITY_VIOLATION_TOLERANCE)
                        {
                            feasibleActions.Add(locId);
                        }
                    }

                    if (feasibleActions.Count == 0)
                    {
                        // Return to depot and start new route
                        currentLocationId = 0;
                        currentLoad = 0.0;
                        currentTime += instance.DistanceMatrix[currentLoc.Id, 0];
                        continue;
                    }

                    // Select action
                    int action = SelectAction(state, feasibleActions);
                    Location nextLoc = instance.Locations.First(l => l.Id == action);
                    
                    // Calculate reward
                    double reward = CalculateReward(currentLoc, nextLoc, currentTime, currentLoad, instance);
                    episodeReward += reward;
                    
                    // Update state
                    currentTime += instance.DistanceMatrix[currentLoc.Id, action];
                    currentTime = Math.Max(currentTime, nextLoc.TimeWindow.Start);
                    currentTime += nextLoc.ServiceTime;
                    currentLoad += nextLoc.Demand;
                    unvisited.Remove(action);
                    
                    // Next state
                    string nextState = CreateStateRepresentation(action, currentLoad, currentTime, unvisited, instance);
                    List<int> nextFeasibleActions = unvisited
                        .Where(locId => {
                            var loc = instance.Locations.First(l => l.Id == locId);
                            return currentLoad + loc.Demand <= instance.Vehicles[0].Capacity * CAPACITY_VIOLATION_TOLERANCE;
                        })
                        .ToList();
                    
                    // Update Q-value
                    UpdateQValue(state, action, reward, nextState, nextFeasibleActions);
                    
                    currentLocationId = action;
                    steps++;
                }
                
                // Decay epsilon
                epsilon = Math.Max(epsilonMin, epsilon * epsilonDecay);
                
                if ((episode + 1) % 10 == 0)
                {
                    string avgRewardStr = steps > 0 ? $"{episodeReward / steps:F2}" : "N/A";
                    Console.WriteLine($"Episode {episode + 1}/{episodes}, Avg Reward: {avgRewardStr}, Epsilon: {epsilon:F3}");
                }
            }
            
            Console.WriteLine($"Training completed. Q-table size: {qTable.Count} states");
        }

        /// <summary>
        /// Generate solution using trained Q-Learning agent
        /// </summary>
        public Solution Solve(Instance instance)
        {
            List<Route> routes = new List<Route>();
            List<int> unvisited = instance.Locations
                .Where(l => l.Id != 0)
                .Select(l => l.Id)
                .ToList();
            
            int vehicleIndex = 0;
            double epsilon_backup = epsilon;
            epsilon = 0.0; // Pure exploitation during inference
            
            while (unvisited.Count > 0 && vehicleIndex < instance.Vehicles.Count)
            {
                List<Location> stops = new List<Location>();
                stops.Add(instance.Locations.First(l => l.Id == 0)); // Start at depot
                
                int currentLocationId = 0;
                double currentLoad = 0.0;
                double currentTime = 0.0;
                int maxStepsPerRoute = 50;
                int steps = 0;
                double vehicleCapacity = instance.Vehicles[vehicleIndex].Capacity;
                
                while (unvisited.Count > 0 && steps < maxStepsPerRoute)
                {
                    Location currentLoc = instance.Locations.First(l => l.Id == currentLocationId);
                    string state = CreateStateRepresentation(currentLocationId, currentLoad, currentTime, unvisited, instance);
                    
                    // Find feasible actions
                    List<int> feasibleActions = new List<int>();
                    foreach (int locId in unvisited)
                    {
                        Location loc = instance.Locations.First(l => l.Id == locId);
                        if (currentLoad + loc.Demand <= vehicleCapacity)
                        {
                            feasibleActions.Add(locId);
                        }
                    }
                    
                    if (feasibleActions.Count == 0)
                        break;
                    
                    // Select best action
                    int action = SelectAction(state, feasibleActions);
                    Location nextLoc = instance.Locations.First(l => l.Id == action);
                    
                    // Add to route
                    stops.Add(nextLoc);
                    unvisited.Remove(action);
                    
                    // Update state
                    currentTime += instance.DistanceMatrix[currentLoc.Id, action];
                    currentTime = Math.Max(currentTime, nextLoc.TimeWindow.Start);
                    currentTime += nextLoc.ServiceTime;
                    currentLoad += nextLoc.Demand;
                    currentLocationId = action;
                    steps++;
                }
                
                // Return to depot
                stops.Add(instance.Locations.First(l => l.Id == 0));
                
                // Create route
                Route route = new Route(
                    truckCapacity: vehicleCapacity,
                    stops: stops,
                    startTime: 0.0,
                    currentLoad: currentLoad,
                    cost: 0.0,
                    penalty: 0.0,
                    vot: 0.0
                );
                
                routes.Add(route);
                vehicleIndex++;
            }
            
            epsilon = epsilon_backup;
            
            Solution solution = new Solution(routes);
            solution.calculateRoutesMetrics(instance);
            
            return solution;
        }
    }
}
