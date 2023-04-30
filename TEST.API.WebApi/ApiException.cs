using System;
using System.Runtime.Serialization;

namespace TEST.API.WebApi
{
    /// <summary>
    /// Generic API exception class, used to throw when an application exception occurs at controller level
    /// </summary>
    [Serializable]
    public class ApiException : Exception
    {
        public ApiException(string message)
            : base(message)
        {
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        protected ApiException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}