using System;

namespace RCVRPTW
{
    /// <summary>
    /// Example program for running RL performance analysis and diagnostics.
    /// This demonstrates how to identify and diagnose RL performance issues.
    /// </summary>
    public static class RLAnalysisExample
    {
        /// <summary>
        /// Main entry point for RL analysis examples
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("RCVRPTW - RL Performance Analysis Examples\n");
            
            if (args.Length == 0)
            {
                ShowMenu();
                return;
            }

            string command = args[0].ToLower();
            string instancePath = args.Length > 1 ? args[1] : "pliki/100 lokacji/C101.txt";

            switch (command)
            {
                case "full":
                    RunFullDiagnostics(instancePath);
                    break;
                    
                case "quick":
                    RunQuickComparison(instancePath);
                    break;
                    
                case "multi":
                    RunMultiInstanceTest();
                    break;
                    
                case "rewards":
                    AnalyzeRewards(instancePath);
                    break;
                    
                case "validate":
                    ValidateEpisodeCount(args);
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowMenu();
                    break;
            }
        }

        private static void ShowMenu()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  full <instance>     - Run comprehensive diagnostics");
            Console.WriteLine("  quick <instance>    - Quick Greedy vs RL comparison");
            Console.WriteLine("  multi               - Test multiple instances");
            Console.WriteLine("  rewards <instance>  - Analyze reward distribution");
            Console.WriteLine("  validate <episodes> - Check if episode count is reasonable");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  dotnet run full \"pliki/100 lokacji/C101.txt\"");
            Console.WriteLine("  dotnet run quick");
            Console.WriteLine("  dotnet run multi");
            Console.WriteLine("  dotnet run validate 50000000");
        }

        private static void RunFullDiagnostics(string instancePath)
        {
            Console.WriteLine($"Running full diagnostics on: {instancePath}\n");
            
            try
            {
                RLDiagnostics.RunFullDiagnostics(instancePath, episodeCounts: new int[] { 10, 25, 50, 100 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("\nPlease ensure:");
                Console.WriteLine("  - Instance file exists");
                Console.WriteLine("  - Path is relative to project root");
            }
        }

        private static void RunQuickComparison(string instancePath)
        {
            Console.WriteLine($"Quick comparison on: {instancePath}\n");
            
            try
            {
                RLDiagnostics.QuickComparison(instancePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void RunMultiInstanceTest()
        {
            Console.WriteLine("Running multi-instance test...\n");
            
            try
            {
                RLDiagnostics.MultiInstanceTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void AnalyzeRewards(string instancePath)
        {
            Console.WriteLine($"Analyzing reward distribution on: {instancePath}\n");
            
            try
            {
                RLDiagnostics.AnalyzeRewardDistribution(instancePath, episodes: 50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ValidateEpisodeCount(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: validate <episode_count>");
                Console.WriteLine("Example: validate 50000000");
                return;
            }

            if (int.TryParse(args[1], out int episodes))
            {
                RLDiagnostics.ValidateEpisodeCount(episodes);
            }
            else
            {
                Console.WriteLine($"Invalid episode count: {args[1]}");
            }
        }
    }
}
