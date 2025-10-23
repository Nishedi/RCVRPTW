//using MathNet.Numerics.Distributions;
using System;

public static class DemandSampler
{
    public static double SampleNormalMathNet(double mean, double stdDev)
    {
        if (stdDev <= 0) return mean;
        return MathNet.Numerics.Distributions.Normal.Sample(mean, stdDev);
    }

    public static double SampleNormalMathNet(double mean, double stdDev, Random rng)
    {
        if (stdDev <= 0) return mean;
        return MathNet.Numerics.Distributions.Normal.Sample(rng, mean, stdDev);
    }

    public static double SampleTruncatedNormalMathNet(double mean, double stdDev, double min = 0.0, int maxAttempts = 1000, Random rng = null)
    {
        if (stdDev <= 0) return Math.Max(min, mean);
        rng ??= new Random();
        for (int i = 0; i < maxAttempts; i++)
        {
            double v = MathNet.Numerics.Distributions.Normal.Sample(rng, mean, stdDev);
            if (v >= min) return v;
        }
        return Math.Max(min, MathNet.Numerics.Distributions.Normal.Sample(mean, stdDev));
    }

    public static int SampleDemandInt(double mean, double stdDev, int min = 0, Random rng = null)
    {
        double v = SampleTruncatedNormalMathNet(mean, stdDev, min, 1000, rng);
        return Math.Max(min, (int)Math.Round(v));
    }
}