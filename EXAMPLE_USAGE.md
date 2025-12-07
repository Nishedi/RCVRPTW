# RL Model Training and Usage Example

This document demonstrates the complete workflow for training an RL model and using it for optimization.

## Overview

The RCVRPTW solver now supports:
1. **Training Mode**: Train an RL model and save it for later use
2. **Inference Mode**: Load a pre-trained model to optimize problem instances
3. **Regular RL Mode**: Train from scratch during each optimization run (legacy behavior)

## Workflow Example

### Step 1: Train an RL Model

Train a model on a specific problem type (e.g., C101) for a defined time period:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 train
```

**Parameters:**
- `C101`: Problem instance type (C101, R101, RC101, etc.)
- `3600`: Training time in seconds (1 hour)
- `train`: Training mode flag

**Output:**
```
RL Training mode enabled - model will be trained and saved
Training RL model for file type: C101, maxTime: 3600s
=== RL Model Training Mode ===
Training will run for 3600 seconds
...
RL model saved to: models/rl_model_C101_20231207_143022.json
```

The trained model is saved in the `models/` directory with a timestamp.

### Step 2: Use the Trained Model for Optimization

Once you have a trained model, use it to optimize problem instances:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 600 model:models/rl_model_C101_20231207_143022.json
```

**Parameters:**
- `C101`: Problem instance type
- `600`: Optimization time in seconds (10 minutes)
- `model:...`: Path to the trained model

**Output:**
```
Loading pre-trained RL model from: models/rl_model_C101_20231207_143022.json
RL model loaded from: models/rl_model_C101_20231207_143022.json
Q-table size: 45 state-action pairs
Epsilon: 0.1523
...
```

## Benefits of Using Pre-trained Models

1. **Faster Convergence**: Pre-trained models have already learned which operators work best in different situations
2. **Transfer Learning**: A model trained on one instance (e.g., C101) can be used on similar instances (C102, C201)
3. **Reproducibility**: Save and share models for consistent results across runs
4. **Incremental Learning**: Models continue to learn and adapt during optimization

## Model File Structure

The trained models are saved as JSON files containing:

```json
{
  "LearningRate": 0.1,
  "DiscountFactor": 0.9,
  "Epsilon": 0.1523,
  "EpsilonDecay": 0.9995,
  "EpsilonMin": 0.1,
  "QTable": {
    "0,0": 0.234,
    "0,1": 0.156,
    ...
  },
  "OperatorSelectionCount": [845, 832, 823],
  "OperatorRewardSum": [194.8, 233.6, 105.3]
}
```

## Use Cases

### 1. Production Optimization
Train a model once on representative instances, then use it for all production runs:

```bash
# One-time training (e.g., 2 hours)
dotnet run C101 7200 train

# Production runs (shorter time, pre-trained model)
dotnet run C101 300 model:models/rl_model_C101_20231207_143022.json
dotnet run C102 300 model:models/rl_model_C101_20231207_143022.json
dotnet run C201 300 model:models/rl_model_C101_20231207_143022.json
```

### 2. Hyperparameter Comparison
Train models with different hyperparameters and compare their performance:

```bash
# Train different models
dotnet run C101 3600 train  # Model 1
dotnet run R101 3600 train  # Model 2

# Compare on the same test instance
dotnet run C101 600 model:models/rl_model_C101_20231207_143022.json
dotnet run C101 600 model:models/rl_model_R101_20231207_150045.json
```

### 3. Transfer Learning
Use a model trained on one problem type for another:

```bash
# Train on C-type problems (clustered customers)
dotnet run C101 3600 train

# Apply to R-type problems (random customers)
dotnet run R101 600 model:models/rl_model_C101_20231207_143022.json
```

## Performance Considerations

- **Training Time**: Longer training (1+ hours) produces better models
- **Model Size**: Q-tables are typically small (< 1KB), fast to load
- **Epsilon Value**: Loaded models retain their epsilon (exploration rate) from training
  - Lower epsilon = more exploitation of learned knowledge
  - Higher epsilon = more exploration of new strategies

## Legacy Mode

For backward compatibility, you can still train from scratch during each run:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 RL
```

This trains a new model for each optimization run but doesn't save it.

## Tips

1. **Naming Models**: The timestamp in the filename helps track when models were trained
2. **Model Directory**: All models are saved in `models/` (excluded from git)
3. **Version Control**: Consider version controlling particularly good models
4. **Documentation**: Keep notes on what instances were used for training
