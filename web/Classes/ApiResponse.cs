using System.Net;
using System.Runtime.Serialization;
using Viper.Models;

namespace Viper.Classes
{
    [DataContract]
    public class ApiResponse
    {
        [DataMember]
        public int StatusCode { get; set; }

        [DataMember]
        public bool Success { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string? ErrorMessage { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Object? Errors { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object? Result { get; set; }
        
        [DataMember(EmitDefaultValue = false)]
        public ApiPagination? Pagination { get; set; }

        public ApiResponse(HttpStatusCode statusCode, bool success, object? result = null, string? errorMessage = null, Object? errors = null)
        {
            StatusCode = (int)statusCode;
            Result = result;
            ErrorMessage = errorMessage;
            Errors = errors;
            Success = success;
        }
    }
}
