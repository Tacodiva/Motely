using System;

namespace Motely
{
    /// <summary>
    /// Progress information reported during Motely search operations
    /// </summary>
    public class MotelyProgress
    {
        public ulong CompletedBatchCount { get; set; }
        public ulong TotalBatchCount { get; set; }
        public ulong SeedsSearched { get; set; }
        public double SeedsPerMillisecond { get; set; }
        public double PercentComplete { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string FormattedMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// Creates a formatted progress message
        /// </summary>
        public static string FormatProgress(double percent, TimeSpan? timeRemaining, double seedsPerMillisecond)
        {
            // Format percentage with appropriate precision for tiny values
            string percentStr;
            if (percent < 0.01)
                percentStr = $"{percent:0.0000}%";
            else if (percent < 1.0)
                percentStr = $"{percent:0.000}%";
            else
                percentStr = $"{percent:0.00}%";
                
            if (!timeRemaining.HasValue)
            {
                return $"{percentStr} ({seedsPerMillisecond * 1000:F0} seeds/s)";
            }
            
            string timeLeftFormatted;
            var timeLeft = timeRemaining.Value;
            
            if (timeLeft.TotalDays >= 1)
            {
                timeLeftFormatted = $"{(int)timeLeft.TotalDays}d {timeLeft.Hours}h";
            }
            else if (timeLeft.TotalHours >= 1)
            {
                timeLeftFormatted = $"{(int)timeLeft.TotalHours}h {timeLeft.Minutes}m";
            }
            else if (timeLeft.TotalMinutes >= 1)
            {
                timeLeftFormatted = $"{(int)timeLeft.TotalMinutes}m {timeLeft.Seconds}s";
            }
            else
            {
                timeLeftFormatted = $"{timeLeft.Seconds}s";
            }
            
            return $"{percentStr} ETA: {timeLeftFormatted} ({seedsPerMillisecond * 1000:F0} seeds/s)";
        }
    }
}