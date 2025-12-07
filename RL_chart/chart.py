import pandas as pd
import matplotlib
matplotlib.use("tkAgg")
import matplotlib.pyplot as plt

# Load metrics
df = pd.read_csv('rl_model_C101_20251207_220927_metrics.csv')

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