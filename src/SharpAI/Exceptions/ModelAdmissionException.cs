namespace SharpAI.Exceptions
{
    using System;

    /// <summary>
    /// Thrown when a model cannot be admitted for loading because doing so would exceed the configured
    /// model-memory budget and no idle model could be evicted to make room. Callers should surface this as
    /// a capacity error rather than attempting the load, which would risk an out-of-memory condition.
    /// </summary>
    public class ModelAdmissionException : Exception
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        public ModelAdmissionException()
        {
        }

        /// <summary>
        /// Instantiate with a message.
        /// </summary>
        /// <param name="message">Message describing the condition.</param>
        public ModelAdmissionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiate with a message and inner exception.
        /// </summary>
        /// <param name="message">Message describing the condition.</param>
        /// <param name="innerException">Inner exception.</param>
        public ModelAdmissionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
