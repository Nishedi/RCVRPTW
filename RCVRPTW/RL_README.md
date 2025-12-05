# RL-based Operator Selection for RCVRPTW

This implementation adds a Reinforcement Learning (RL) agent that learns to select the best mutation operators (swap, insert, invert) during the Tabu Search optimization process.

## Overview

The RL agent uses Q-learning to adaptively choose which operator to apply at each iteration based on:
- Current solution quality
- Search progress (early, middle, late stage)
- Historical performance of each operator

## How It Works

### State Representation
The agent discretizes the search state into bins based on:
1. **Improvement rate**: How much the solution is improving (5 bins)
   - Large improvement (>10%)
   - Medium improvement (1-10%)
   - Small improvement (0-1%)
   - Small degradation (0 to -1%)
   - Large degradation (<-1%)

2. **Search progress**: Stage of the search (3 bins)
   - Early stage (0-33%)
   - Middle stage (33-66%)
   - Late stage (66-100%)

This creates 15 possible states (5 × 3).

### Action Space
The agent can choose from three mutation operators:
- **swap**: Exchange two locations
- **insert**: Move a location to a different position
- **invert**: Reverse a segment of locations

### Learning Algorithm
Uses Q-learning with:
- **Learning rate (α)**: 0.1 - controls how quickly Q-values are updated
- **Discount factor (γ)**: 0.9 - importance of future rewards
- **Epsilon-greedy policy**: Balances exploration vs exploitation
  - Initial epsilon: 1.0 (full exploration)
  - Epsilon decay: 0.9995 (slow decay for long training)
  - Minimum epsilon: 0.1 (maintains some exploration)

### Reward Function
Rewards are calculated based on objective improvement:
```
reward = (previous_objective - current_objective) / |previous_objective| × 100
```
- Positive reward for improvements
- Negative reward for degradations
- Scaled to be in a reasonable range

## Usage

### Running with RL

To enable RL-based operator selection, add `RL` as the third argument:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj <file_type> <max_time_seconds> RL
```

**Examples:**

```bash
# Run with RL for 1 hour (3600 seconds)
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 RL

# Run with RL for 30 minutes
dotnet run --project RCVRPTW/RCVRPTW.csproj R101 1800 RL

# Run without RL (traditional fixed operator)
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 120
```

### Running from Compiled Binary

```bash
cd RCVRPTW/bin/Debug/net8.0/
./RCVRPTW C101 3600 RL
```

## Output

When running with RL, you'll see:
1. **Iteration progress**: Each improvement shows the selected operator
   - `(s)` = swap
   - `(i)` = insert
   - `(v)` = invert

2. **Periodic updates**: Every 100 iterations showing:
   - Current iteration number
   - Epsilon value (exploration rate)
   - Elapsed time

3. **Final statistics**:
   - Total iterations completed
   - Total time elapsed
   - Q-table size (number of state-action pairs learned)
   - Operator usage statistics:
     - How many times each operator was selected
     - Average reward for each operator

### Example Output

```
RL mode enabled - operators will be selected using reinforcement learning
Running experiments for file type: C101, maxTime of scenario: 3600s, RL: True
TabuSize:50 MaxIterations:200 MutationType: RL-based Initial greedy solution objective: 30186
RL Training enabled - maxTime: 3600s
0:29244(s). 1:28911(i). 2:28543(i). 3:28234(s). ...

Iteration 100, Epsilon: 0.9512, Time: 150.3s
...

Tabu Search with RL completed 2500 iterations in 3600.5 seconds.

=== RL Operator Selector Statistics ===
Current epsilon (exploration rate): 0.1523
Q-table size: 45 state-action pairs

Operator Usage and Average Reward:
  swap      : selected   845 times, avg reward:   0.2305
  insert    : selected   832 times, avg reward:   0.2810
  invert    : selected   823 times, avg reward:   0.1280
```

## Implementation Details

### RLOperatorSelector Class
Located in `RLOperatorSelector.cs`, this class implements:
- Q-learning algorithm
- State discretization
- Action selection (epsilon-greedy)
- Q-value updates
- Statistics tracking

### TabuSearch Integration
The `TabuSearch.runWithRL()` method in `TabuSearch.cs`:
- Initializes the RL agent
- Selects operators using the agent at each iteration
- Updates Q-values based on outcomes
- Logs learning progress

### Experiments Integration
The experiment runner in `Experiments.cs` supports:
- `useRL` parameter to enable RL mode
- Results tracking with RL indicator
- CSV output includes RL flag

## Performance Considerations

### Training Time
- The RL agent is designed to learn over long periods (1+ hours)
- Initial iterations use high exploration (random operator selection)
- Over time, the agent learns which operators work best in different situations
- Epsilon decay is slow (0.9995) to allow thorough exploration

### Computational Overhead
- Minimal overhead from RL operations
- Q-table lookups and updates are O(1)
- State discretization is simple arithmetic
- Main computational cost is still the Tabu Search neighborhood generation

## Expected Benefits

1. **Adaptive operator selection**: The agent learns which operators work best for different problem states
2. **Improved solution quality**: By selecting better operators, the search can find better solutions
3. **Automatic tuning**: No need to manually select operators or tune operator probabilities
4. **Learning transfer**: The Q-table captures knowledge that persists across the search

## Future Enhancements

Possible improvements:
1. **More sophisticated state representation**: Include more features like:
   - Number of routes
   - Constraint violations
   - Diversity of recent moves
2. **Function approximation**: Use neural networks instead of Q-table
3. **Multi-armed bandit approaches**: UCB, Thompson sampling
4. **Operator sequencing**: Learn sequences of operators
5. **Transfer learning**: Save and load Q-tables across problem instances

## References

- Sutton, R. S., & Barto, A. G. (2018). Reinforcement learning: An introduction. MIT press.
- Watkins, C. J., & Dayan, P. (1992). Q-learning. Machine learning, 8(3), 279-292.
