# RCVRPTW - Robust Capacitated Vehicle Routing Problem with Time Windows

This repository contains the implementation and research article for solving the Resource Constrained Capacitated Vehicle Routing Problem with Time Windows (RCVRPTW) under uncertainty.

## Overview

The project implements and compares three distinct algorithmic approaches for solving VRPTW under stochastic demand and time window perturbations:

1. **Greedy Constructive Heuristic** - Fast baseline approach
2. **Tabu Search Metaheuristic** - Advanced optimization with neighborhood exploration
3. **Q-Learning Reinforcement Learning** - Learning-based approach that adapts through experience

## Key Features

- Comprehensive MILP formulation of RCVRPTW with soft time windows
- Stochastic scenario generation for robustness testing
- Implementation of three algorithmic paradigms for comparative analysis
- Parallel execution framework for large-scale experiments
- Statistical analysis and visualization tools
- Support for Solomon benchmark instances

## Implementation

### Core Components

- **Instance**: Problem representation with locations, vehicles, and distance matrix
- **Solution**: Route representation with cost and penalty calculations
- **GreedyApproaches**: Constructive heuristic implementation
- **TabuSearch**: Metaheuristic with tabu list and aspiration criterion
- **QLearningAgent**: Reinforcement learning agent with Q-table
- **RLSolver**: Unified interface for RL training and inference
- **ExperimentRunner**: Framework for running experiments across scenarios

### Algorithms

#### Greedy Algorithm
- Iteratively inserts nearest feasible customer
- Respects capacity and time window constraints
- Fast execution, provides baseline for comparison

#### Tabu Search
- Starts from greedy solution
- Explores neighborhood using swap/insert/invert operators
- Maintains tabu list to avoid cycling
- Aspiration criterion for accepting improving tabu moves
- Configurable parameters: iterations, tabu size, mutation type

#### Q-Learning
- State representation: current location, load level, time level, unvisited count
- Action space: feasible customer locations
- Reward function: -1 × (travel cost + waiting time + 1000×penalties + 10000×violations)
- Training: 50 episodes with ε-greedy exploration (ε: 1.0 → 0.05)
- Inference: Pure exploitation (ε = 0)
- Parameters: α=0.15, γ=0.9

## Building and Running

### Requirements
- .NET 8.0 SDK
- C# compiler

### Build
```bash
cd RCVRPTW
dotnet build
```

### Run Experiments
```bash
# Run Tabu Search experiments
dotnet run -- C101 120

# Run RL experiments (modify Program.cs to enable)
# See Experiments.cs for RunRLExperiments method
```

### RL Performance Analysis and Diagnostics

⚠️ **Important Note**: The RL implementation is currently underperforming compared to Greedy and Tabu Search methods. 

For detailed analysis of RL performance issues and recommendations, see:
- **[RL_PERFORMANCE_ANALYSIS.md](RL_PERFORMANCE_ANALYSIS.md)** - Comprehensive analysis of current issues and improvement recommendations

To diagnose RL performance issues, use the diagnostic utilities:

```csharp
// Run full diagnostics on an instance
RLDiagnostics.RunFullDiagnostics("pliki/100 lokacji/C101.txt");

// Quick comparison
RLDiagnostics.QuickComparison("pliki/100 lokacji/C101.txt");

// Test multiple instances
RLDiagnostics.MultiInstanceTest();

// Validate episode count before training
RLDiagnostics.ValidateEpisodeCount(50000000); // Will warn if too high
```

**Common Issues:**
- **Excessive training episodes**: Use 50-500, not millions
- **Poor solution quality**: Often 3-8x worse than Greedy baseline
- **High penalty weights**: Causing extremely negative rewards
- **Limited state representation**: Q-table too small for problem complexity

## Research Article

The `Article/` directory contains the LaTeX source for the research paper:

- `IJET_Template.tex` - Main document
- `sections/` - Individual sections:
  - `Introduction.tex` - Research hypotheses (H1-H5)
  - `Literature.tex` - Literature review including RL approaches
  - `Problem.tex` - Mathematical formulation
  - `Implementation.tex` - Software architecture and algorithms
  - `Experiment.tex` - Experimental setup and results
  - `Conclusion.tex` - Findings and future work
- `BibTexBibliography.bib` - References including RL literature

### Key Research Contributions

1. MILP formulation for RCVRPTW with soft time windows
2. Stochastic scenario-based evaluation framework
3. Comprehensive comparison of three algorithmic paradigms
4. Q-learning implementation for VRPTW
5. Analysis of robustness and risk under uncertainty

### Research Hypotheses

- **H1**: Tabu Search outperforms Greedy in expected cost
- **H2**: Tabu Search shows lower variability (higher robustness)
- **H3**: Tabu Search reduces time window violation penalties
- **H4**: Tabu Search mitigates extreme-cost scenarios (loss tail)
- **H5**: Q-learning demonstrates adaptive behavior with competitive performance

## Citation

If you use this code or article in your research, please cite:

```
[Article citation to be added upon publication]
```

## License

[License information to be added]

## Acknowledgments

Research supported by [funding information if applicable].