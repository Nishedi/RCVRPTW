# Quick Start Guide - RL Model Training and Usage

## Three Ways to Run the Optimizer

### 1. Traditional Mode (No RL)
Run the optimizer with fixed operators (swap, insert, invert):

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 120
```

**Use when:** You want to use the traditional tabu search without reinforcement learning.

### 2. RL Training Mode (Train + Save Model)
Train an RL model and save it for later use:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 train
```

**Output:** Saves model to `models/rl_model_C101_<timestamp>.json`

**Use when:** You want to train a model once and reuse it multiple times.

### 3. RL Inference Mode (Use Pre-trained Model)
Use a previously trained model for optimization:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 600 model:models/rl_model_C101_20231207_143022.json
```

**Use when:** You have a trained model and want faster optimization with learned operator selection.

### 4. RL Mode (Legacy - Train from Scratch)
Train during optimization without saving:

```bash
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 RL
```

**Use when:** You want to experiment with RL but don't need to save the model.

## Typical Workflow

### Development/Research
1. **Train** a model on representative instances (1-2 hours)
2. **Evaluate** the model on test instances (5-10 minutes each)
3. **Compare** results with traditional approach

```bash
# Step 1: Train
dotnet run C101 7200 train
# Output: models/rl_model_C101_20231207_100000.json

# Step 2: Test on multiple instances
dotnet run C101 300 model:models/rl_model_C101_20231207_100000.json
dotnet run C102 300 model:models/rl_model_C101_20231207_100000.json
dotnet run C201 300 model:models/rl_model_C101_20231207_100000.json

# Step 3: Compare with traditional
dotnet run C101 300
```

### Production
1. **Train once** on representative data
2. **Deploy** the model file with your application
3. **Run** fast optimizations with the trained model

```bash
# One-time training
dotnet run C101 3600 train

# Production runs (use same model)
dotnet run C101 180 model:models/rl_model_C101_20231207_100000.json
dotnet run C201 180 model:models/rl_model_C101_20231207_100000.json
```

## Model File Information

- **Location:** `models/` directory (automatically created)
- **Format:** JSON (human-readable)
- **Size:** Typically < 1 KB (very small)
- **Content:** Q-table, hyperparameters, statistics
- **Portability:** Can be copied and shared across machines

## Performance Notes

| Mode | Training Time | Optimization Time | Best For |
|------|--------------|-------------------|----------|
| Traditional | None | Medium | Quick runs, comparison baseline |
| Train | Long (1+ hours) | N/A | Creating reusable models |
| Pre-trained | None | Fast | Production, repeated optimization |
| RL Legacy | Long (during opt) | Long | Research, experimentation |

## Tips

1. **Model Naming:** The timestamp in filenames helps track when models were trained
2. **Transfer Learning:** Models trained on C101 often work well on C102, C201, etc.
3. **Epsilon Value:** Pre-trained models load with their saved epsilon (exploration rate)
4. **Git Ignore:** The `models/` directory is excluded from version control

## Example Output

### Training Mode:
```
RL Training mode enabled - model will be trained and saved
Training RL model for file type: C101, maxTime: 3600s
...
RL model saved to: models/rl_model_C101_20231207_100000.json
```

### Inference Mode:
```
Loading pre-trained RL model from: models/rl_model_C101_20231207_100000.json
RL model loaded successfully
Q-table size: 45 state-action pairs
Epsilon: 0.1523
...
```

## Need Help?

- See `RL_README.md` for detailed RL algorithm information
- See `EXAMPLE_USAGE.md` for comprehensive examples
- Check model files in `models/` directory (JSON format)
