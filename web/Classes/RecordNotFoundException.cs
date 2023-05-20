using System.Runtime.Serialization;

namespace Viper.Classes
{
    /// <summary>
    /// RecordNotFoundException is thrown when a record is not found in the data source
    /// </summary>
    [Serializable]
    public class RecordNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the Exception class
        /// </summary>
        public RecordNotFoundException() : base("Record not found in data source.") { }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message. 
        /// </summary>
        /// <param name="message">The error message string</param>
        public RecordNotFoundException(string message) : base("Record not found: " + message) { }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message string</param>
        /// <param name="innerException">The inner exception reference</param>
        public RecordNotFoundException(string message, Exception innerException) : base("Record not found: " + message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the Exception class with serialized data
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RecordNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}