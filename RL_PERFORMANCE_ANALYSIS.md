# RL Performance Analysis Report

## Executive Summary

The Reinforcement Learning (RL) implementation for RCVRPTW is currently underperforming significantly. This document analyzes the issues and provides recommendations for improvement.

## Observed Performance Issues

### User Report (@Nishedi)
**Status**: Słabo działa RL (RL works poorly)

**Training Results:**
- Episodes: 50,000,000 (likely error - should be 50)
- Average Reward: -73,659.09
- Epsilon: 0.010
- Training Time: 449.53 seconds
- Q-table Size: 1,815 states
- Solution Objective: 5,473

### Issue #1: Incorrect Episode Count
The training was run with 50,000,000 episodes instead of the intended 50 episodes. This is visible in the output:
```
Episode 49500000/50000000, Avg Reward: -73659.09
Episode 50000000/50000000, Avg Reward: -73659.09
```

**Impact**: 
- Excessive training time (449 seconds for what should take ~5-10 seconds)
- Likely overfitting or convergence to poor local optima
- Wasted computational resources

### Issue #2: Poor Solution Quality
The final solution objective of 5,473 is significantly worse than expected:
- Typical greedy solutions on 100-location instances: ~800-1,500
- TabuSearch solutions: ~600-1,200
- RL solution: 5,473 (3-8x worse)

### Issue #3: Extremely Negative Rewards
Average reward per step: -73,659.09

This indicates several problems:
1. **Penalty weights are too high**: 
   - `TIME_WINDOW_PENALTY_WEIGHT = 1000.0`
   - `CAPACITY_PENALTY_WEIGHT = 10000.0`
   
2. **Agent is consistently violating constraints**
3. **Reward function may not be properly balanced**

### Issue #4: Small Q-table Size
With 1,815 states for a 100-location problem, the state space coverage is very limited:
- 100 locations × multiple load levels × time levels = potential 10,000+ states
- Only 1,815 states explored suggests poor state space exploration
- May indicate state representation is not capturing important features

## Root Cause Analysis

### 1. State Representation Limitations
Current state: `{currentLocation}_{loadLevel}_{timeLevel}_{unvisitedCount}`

**Problems:**
- Too coarse discretization (10 levels for load, 10 for time)
- Doesn't capture:
  - Which specific locations are unvisited
  - Time window urgency
  - Spatial distribution of remaining customers
  
**Impact**: Agent cannot learn meaningful patterns

### 2. Reward Function Issues
Current reward calculation:
```csharp
reward = -(travelCost + waitingTime + 
          TIME_WINDOW_PENALTY_WEIGHT * (earlyPenalty + latePenalty) + 
          CAPACITY_PENALTY_WEIGHT * capacityViolation)
```

**Problems:**
- Penalties dominate the signal (1000x and 10000x multipliers)
- Travel cost (~10-50) is negligible compared to penalties
- Makes it hard for agent to learn subtle optimizations

### 3. Training Configuration
Default parameters may not be suitable:
- Learning rate: 0.15 (might be too high)
- Discount factor: 0.9 (short-sighted planning)
- Epsilon decay: 0.99 (too slow)
- Training episodes: 50 (too few for complex problem)

### 4. Feasibility Constraints During Training
Training allows 10% capacity violation (`CAPACITY_VIOLATION_TOLERANCE = 1.1`), but inference enforces strict constraints. This creates a train-test mismatch.

## Comparison with Baseline Methods

| Method | Typical Objective | Time (seconds) | Notes |
|--------|------------------|----------------|-------|
| Greedy | 800-1,500 | <1 | Fast, reasonable quality |
| TabuSearch | 600-1,200 | 10-120 | Best quality |
| **RL (Current)** | **5,473** | **449** | **Significantly worse** |

## Recommendations

### Immediate Actions (High Priority)

1. **Fix Episode Count Configuration**
   - Ensure users are running with correct episode counts (50-100, not 50M)
   - Add validation in code to prevent unreasonable episode counts

2. **Rebalance Reward Function**
   ```csharp
   // Suggested changes:
   TIME_WINDOW_PENALTY_WEIGHT = 10.0    // Down from 1000
   CAPACITY_PENALTY_WEIGHT = 100.0      // Down from 10000
   ```

3. **Improve State Representation**
   - Add time window urgency features
   - Include nearest unvisited customer information
   - Consider customer clustering information

4. **Adjust Training Parameters**
   ```csharp
   trainingEpisodes: 200-500  (up from 50)
   learningRate: 0.05-0.1     (down from 0.15)
   discountFactor: 0.95-0.99  (up from 0.9)
   epsilonDecay: 0.995        (keep current)
   ```

### Medium-term Improvements

1. **Enhanced Q-Learning Algorithm**
   - Implement experience replay
   - Add prioritized experience replay for constraint violations
   - Consider double Q-learning to reduce overestimation

2. **Better Feature Engineering**
   - Normalize all features to [0,1] range
   - Add features: customer density, time pressure, route fragmentation
   - Use more granular discretization (20 levels instead of 10)

3. **Reward Shaping**
   - Add progress rewards for visiting customers
   - Penalize infeasible solutions more gradually
   - Reward compact, efficient routes

### Long-term Considerations

1. **Deep Reinforcement Learning**
   - Replace Q-table with neural network (DQN)
   - Can handle continuous state space
   - Better generalization

2. **Policy-based Methods**
   - Try Actor-Critic methods
   - Consider Proximal Policy Optimization (PPO)

3. **Hybrid Approaches**
   - Use RL to learn construction heuristics
   - Combine with local search (TabuSearch) for refinement
   - Warm-start RL with greedy solutions

## Testing and Validation Plan

1. **Create RL Performance Test Suite**
   - Small instances (25 locations) for quick validation
   - Medium instances (50 locations) for tuning
   - Full instances (100 locations) for final comparison

2. **Metrics to Track**
   - Solution quality (objective value)
   - Training convergence (reward over time)
   - Q-table coverage (unique states visited)
   - Constraint violation rate
   - Computation time

3. **Comparison Protocol**
   - Run each configuration 10 times with different seeds
   - Compare against greedy baseline
   - Report mean, std dev, min, max
   - Statistical significance testing

## Conclusion

The current RL implementation has significant issues preventing it from being competitive with greedy and tabu search methods. The most critical problems are:

1. **Incorrect episode count usage** (50M instead of 50)
2. **Imbalanced reward function** (penalties too high)
3. **Limited state representation** (too coarse)
4. **Insufficient training** (50 episodes too few)

With the recommended changes, RL has the potential to:
- Match greedy performance with proper tuning
- Potentially outperform greedy with better features
- Serve as a learned construction heuristic for TabuSearch initialization

However, significant development and experimentation will be required. In the short term, **TabuSearch should remain the primary solution method** while RL is improved.

## Next Steps

1. Implement immediate fixes (reward rebalancing, parameter tuning)
2. Run comprehensive experiments comparing configurations
3. Document results and iterate
4. Consider whether deep RL or hybrid approaches are warranted
5. If improvements are insufficient, consider RL as a research direction rather than production method

---

**Document Version**: 1.0  
**Date**: December 4, 2025  
**Author**: Copilot Analysis Agent  
