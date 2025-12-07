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
The agent can choose from six mutation operators:
- **swap**: Exchange two locations
- **insert**: Move a location to a different position
- **invert**: Reverse a segment of locations
- **2opt**: 2-opt operation - removes two edges and reconnects the path differently (optimized edge swap)
- **oropt**: Or-opt operation - removes a sequence of customers and reinserts them elsewhere
- **cross**: Cross-exchange operation - swaps segments of customers between different parts of routes

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

**Note:** All commands below assume you're in the repository root directory. The `--project RCVRPTW/RCVRPTW.csproj` flag tells `dotnet run` which project to execute. 

**Alternative:** You can also `cd RCVRPTW` first and then use `dotnet run` without the `--project` flag.

### Training an RL Model

To train an RL model and save it for later use:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj <file_type> <max_time_seconds> train
```

**Examples:**

```bash
# Train a model on C101 dataset for 1 hour (3600 seconds)
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 train

# Train a model on R101 dataset for 30 minutes
dotnet run --project RCVRPTW/RCVRPTW.csproj R101 1800 train

# Alternative: Change directory first
cd RCVRPTW
dotnet run C101 3600 train
```

The trained model will be saved in the `models/` directory with a timestamp, e.g., `models/rl_model_C101_20231207_143022.json`.

### Running with a Pre-trained Model

To use a previously trained model for optimization:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj <file_type> <max_time_seconds> model:<path_to_model>
```

**Examples:**

```bash
# Use a pre-trained model for optimization
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 600 model:models/rl_model_C101_20231207_143022.json

# Use the same model on a different problem instance
dotnet run --project RCVRPTW/RCVRPTW.csproj C201 600 model:models/rl_model_C101_20231207_143022.json
```

When using a pre-trained model, the epsilon value (exploration rate) is loaded from the saved model, typically allowing for more exploitation than exploration since the model is already trained.

### Running with RL (Training Mode - Legacy)

To enable RL-based operator selection without saving the model (trains from scratch each time):

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj <file_type> <max_time_seconds> RL
```

**Examples:**

```bash
# Run with RL for 1 hour (3600 seconds) - trains from scratch
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 RL

# Run with RL for 30 minutes
dotnet run --project RCVRPTW/RCVRPTW.csproj R101 1800 RL

# Run without RL (traditional fixed operator)
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 120
```

### Running from Compiled Binary

If you prefer to build once and run the compiled executable directly (without `dotnet run`):

```bash
# Build the project first
dotnet build RCVRPTW/RCVRPTW.csproj

# Navigate to the output directory
cd RCVRPTW/bin/Debug/net8.0/

# Now you can run directly without 'dotnet run' or '--project'
./RCVRPTW C101 3600 train
./RCVRPTW C101 600 model:../../models/rl_model_C101_20231207_143022.json
./RCVRPTW C101 3600 RL
```

## Output

When running with RL, you'll see:
1. **Iteration progress**: Each improvement shows the selected operator
   - `(s)` = swap
   - `(i)` = insert
   - `(v)` = invert (also used for display of invert)
   - `(2)` = 2opt
   - `(o)` = oropt
   - `(c)` = cross

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
  2opt      : selected   789 times, avg reward:   0.1950
  oropt     : selected   801 times, avg reward:   0.2150
  cross     : selected   810 times, avg reward:   0.1890
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

## Model Persistence

### Model File Format

The trained models are saved as JSON files containing:
- Learning hyperparameters (learning rate, discount factor, epsilon, etc.)
- Q-table: State-action pairs and their Q-values
- Statistics: Operator selection counts and reward sums

**Example model structure:**
```json
{
  "LearningRate": 0.1,
  "DiscountFactor": 0.9,
  "Epsilon": 0.152,
  "EpsilonDecay": 0.9995,
  "EpsilonMin": 0.1,
  "QTable": {
    "0,0": 0.234,
    "0,1": 0.156,
    ...
  },
  "OperatorSelectionCount": [845, 832, 823, 789, 801, 810],
  "OperatorRewardSum": [194.8, 233.6, 105.3, 153.9, 172.2, 153.0]
}
```

### Transfer Learning

You can train a model on one problem instance (e.g., C101) and use it on similar instances (e.g., C201, C102). The model learns general patterns about which operators work well in different search states, which can transfer across similar problem structures.

**Workflow:**
1. Train on a representative instance: `dotnet run C101 3600 train`
2. Use the trained model on multiple similar instances for faster optimization
3. The model's Q-values guide operator selection based on learned patterns

## Metrics and Learning Assessment

### Training Metrics Output

When training a model with the `train` command, the system automatically generates several files to help assess whether the model is actually learning:

**Generated Files:**
1. **`<model_name>_metrics.csv`**: Detailed iteration-by-iteration metrics
   - Iteration number
   - Timestamp
   - State and action (operator) selected
   - Reward received
   - Q-value before and after update
   - Current epsilon (exploration rate)
   - Best and current objective values
   - Q-table size and statistics

2. **`<model_name>_summary.txt`**: Comprehensive training summary report
   - Learning progression (epsilon decay, Q-table growth)
   - Operator performance statistics
   - Objective improvement over time
   - Reward statistics
   - Q-value evolution at checkpoints
   - Learning assessment with indicators

3. **`<model_name>_qtable.csv`**: Final Q-table export
   - State-action pairs and their learned Q-values
   - Useful for detailed analysis of what the model learned

**Example:**
```bash
dotnet run C101 3600 train
```

This generates:
- `models/rl_model_C101_<timestamp>.json` (the trained model)
- `models/rl_model_C101_<timestamp>_metrics.csv` (iteration metrics)
- `models/rl_model_C101_<timestamp>_summary.txt` (training summary)
- `models/rl_model_C101_<timestamp>_qtable.csv` (Q-table)

### Assessing Learning

The training summary includes an automatic learning assessment based on multiple indicators:

**Learning Indicators:**
1. **Q-table Growth**: The model explores new state-action pairs
   - Expected: Should grow from 0 to typically 10-50 pairs
   - Indicates: The model is experiencing diverse states

2. **Epsilon Decay**: Exploration rate decreases over time
   - Expected: Should decay from 1.0 toward the minimum (default 0.1)
   - Indicates: The model is transitioning from exploration to exploitation

3. **Reward Trend**: Average rewards improve over time
   - Expected: Later iterations should have higher average rewards than early ones
   - Indicates: The model is learning which operators work better

4. **Operator Preferences**: Selection distribution becomes non-uniform
   - Expected: Some operators selected more frequently than others
   - Indicates: The model has learned operator preferences

**Example Summary Interpretation:**
```
=== Learning Assessment ===
Model appears to be learning: YES

Indicators:
- Q-table growth: 45 state-action pairs explored
- Epsilon decay: 0.152 (started at 1.0)
- Reward trend: Improving
- Operator preferences: Developed
```

### Using Metrics for Analysis

**Analyzing the Metrics CSV:**

The metrics CSV file can be imported into spreadsheet software or analyzed with Python/R for:

1. **Reward progression plots**: Visualize how rewards change over iterations
2. **Operator selection over time**: See which operators are chosen as training progresses
3. **Q-value evolution**: Track how Q-values converge
4. **Objective improvement curve**: Plot the best objective over time

**Python Example:**
```python
import pandas as pd
import matplotlib.pyplot as plt

# Load metrics
df = pd.read_csv('models/rl_model_C101_timestamp_metrics.csv')

# Plot objective improvement
plt.figure(figsize=(12, 4))

plt.subplot(1, 3, 1)
plt.plot(df['Iteration'], df['BestObjective'])
plt.xlabel('Iteration')
plt.ylabel('Best Objective')
plt.title('Objective Improvement')

plt.subplot(1, 3, 2)
plt.plot(df['Iteration'], df['Epsilon'])
plt.xlabel('Iteration')
plt.ylabel('Epsilon')
plt.title('Exploration Rate Decay')

plt.subplot(1, 3, 3)
plt.plot(df['Iteration'], df['Reward'])
plt.xlabel('Iteration')
plt.ylabel('Reward')
plt.title('Rewards Over Time')

plt.tight_layout()
plt.show()
```

### Detecting Non-Learning

If the model is **not learning**, you might see:

- Q-table size remains very small (< 5 state-action pairs)
- Epsilon doesn't decay significantly
- Rewards show no upward trend
- Operator selection remains uniform (all ~16.7% for 6 operators)
- Objective improvement is similar to random search

**Possible Causes:**
- Training time too short (try longer training)
- Learning rate too high or too low
- Reward function not providing good signal
- State representation not capturing important features

### Comparing Results

To determine if RL learning provides benefits over random operator selection:

1. Train a model for sufficient time (1+ hours recommended)
2. Check the learning assessment in the summary
3. Compare final Q-values - they should be diverse, not all similar
4. Compare operator selection percentages - should show clear preferences
5. Run experiments with and without RL using the `both` mode:

```bash
dotnet run C101 600 RL both
```

This runs both RL-based and non-RL experiments for comparison.

## Future Enhancements

Possible improvements:
1. **More sophisticated state representation**: Include more features like:
   - Number of routes
   - Constraint violations
   - Diversity of recent moves
2. **Function approximation**: Use neural networks instead of Q-table
3. **Multi-armed bandit approaches**: UCB, Thompson sampling
4. **Operator sequencing**: Learn sequences of operators
5. **Incremental learning**: Continue training an existing model with new experiences

## References

- Sutton, R. S., & Barto, A. G. (2018). Reinforcement learning: An introduction. MIT press.
- Watkins, C. J., & Dayan, P. (1992). Q-learning. Machine learning, 8(3), 279-292.
