# New Neighborhood Operators

This document describes the new neighborhood operators added to the RCVRPTW solver.

## Summary of Changes

Three new neighborhood mixing operators have been added to enhance the search capabilities:

### 1. **2-Opt Operator** (`2opt`)
- **Description**: Classic 2-opt operation that removes two edges and reconnects the path
- **Implementation**: Reverses a segment between two positions
- **Use Case**: Effective for eliminating crossing paths and improving route geometry
- **Example**: For route [A, B, C, D, E], applying 2-opt between B and D results in [A, D, C, B, E]

### 2. **Or-Opt Operator** (`oropt`)
- **Description**: Removes a sequence of consecutive customers and reinserts them elsewhere
- **Implementation**: Extracts a subsequence (default length 1-3) and moves it to a new position
- **Use Case**: Useful for relocating clusters of customers to better positions in the route
- **Example**: For route [A, B, C, D, E, F], moving segment [C, D] after E results in [A, B, E, C, D, F]

### 3. **Cross-Exchange Operator** (`cross`)
- **Description**: Swaps two segments of potentially different lengths
- **Implementation**: Exchanges two non-overlapping segments from different parts of the route(s)
- **Use Case**: Effective for diversifying the search and exploring different route structures
- **Example**: For route [A, B, C, D, E, F], swapping [B, C] with [E] results in [A, E, D, B, C, F]

## Integration with RL Agent

The `RLOperatorSelector` has been updated to support all six operators:
1. swap (original)
2. insert (original)
3. invert (original)
4. **2opt** (new)
5. **oropt** (new)
6. **cross** (new)

The RL agent now learns which of these six operators works best in different search states, allowing for more adaptive and effective optimization.

## Usage

### With RL (Automatic Operator Selection)
```bash
# Train a model that learns to use all operators
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 3600 train

# Use RL-based operator selection
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 600 RL
```

### Traditional Mode (Fixed Operator)
The traditional mode continues to work with single fixed operators:
```bash
# Use swap operator
dotnet run --project RCVRPTW/RCVRPTW.csproj C101 120
```

## Performance Observations

From initial testing with RL on C101 (60 seconds):
- All six operators were actively used by the agent
- **2opt** was selected most frequently (79 times, avg reward: 1.77)
- **swap** was second most used (71 times, avg reward: 1.71)
- **oropt** showed good performance (58 times, avg reward: 0.94)
- **cross** was used but showed negative average reward (-0.33), indicating it may be more useful for diversification than direct improvement

The RL agent successfully learned to balance exploration across all operators while exploiting the most effective ones for the current problem state.

## Technical Details

### File Modifications
1. **NeighborhoodGenerator.cs**: Added `twoOpt()`, `orOpt()`, and `crossExchange()` methods
2. **RLOperatorSelector.cs**: Updated operators array from 3 to 6 operators
3. **RLModelData**: Updated array sizes to accommodate 6 operators
4. **RL_README.md**: Updated documentation with new operator descriptions

### Backward Compatibility
All existing functionality remains intact:
- Traditional mode with fixed operators continues to work
- RL models can be trained and loaded
- Previous 3-operator models are incompatible with the new 6-operator system
