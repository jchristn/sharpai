namespace SharpAI.Helpers
{
    using System;

    /// <summary>
    /// Time helper.
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// Determine the total number of milliseconds between a start and end time.
        /// </summary>
        /// <param name="start">Start time.</param>
        /// <param name="end">End time.</param>
        /// <param name="decimalPlaces">Number of decimal places.</param>
        /// <returns>Milliseconds.</returns>
        public static double TotalMsBetween(DateTime start, DateTime end, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            TimeSpan total = end - start;

            return Math.Round(Math.Abs(total.TotalMilliseconds), decimalPlaces);
        }
    }
}
