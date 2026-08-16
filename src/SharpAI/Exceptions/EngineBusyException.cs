namespace SharpAI.Exceptions
{
    using System;

    /// <summary>
    /// Thrown when a generation request cannot acquire a generation slot within the configured admission
    /// timeout because the engine is at capacity. Callers (for example a REST server) should map this to a
    /// "server busy" response such as HTTP 503 or 429 rather than blocking indefinitely.
    /// </summary>
    public class EngineBusyException : Exception
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        public EngineBusyException()
        {
        }

        /// <summary>
        /// Instantiate with a message.
        /// </summary>
        /// <param name="message">Message describing the condition.</param>
        public EngineBusyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiate with a message and inner exception.
        /// </summary>
        /// <param name="message">Message describing the condition.</param>
        /// <param name="innerException">Inner exception.</param>
        public EngineBusyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
