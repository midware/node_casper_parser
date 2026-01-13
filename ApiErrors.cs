using Newtonsoft.Json;
using System.Net;

namespace NodeCasperParser.ApiErrors
{
    public class Success
    {
        public bool success { get; set; }
        public string? error { get; set; }
    }

    public class ApiError
    {
        public int StatusCode { get; private set; }

        public string StatusDescription { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message { get; private set; }

        public ApiError(int statusCode, string statusDescription)
        {
            this.StatusCode = statusCode;
            this.StatusDescription = statusDescription;
        }

        public ApiError(int statusCode, string statusDescription, string message)
            : this(statusCode, statusDescription)
        {
            this.Message = message;
        }
    }

    public abstract class BadRequestException : Exception
    {
        protected BadRequestException(string message)
            : base(message)
        {
        }
    }

    public abstract class NotFoundException : Exception
    {
        protected NotFoundException(string message)
            : base(message)
        {
        }
    }

    public class InternalServerError : ApiError
    {
        public InternalServerError()
            : base(500, HttpStatusCode.InternalServerError.ToString())
        {
        }


        public InternalServerError(string message)
            : base(500, HttpStatusCode.InternalServerError.ToString(), message)
        {
        }
    }

    public class NotFoundError : ApiError
    {
        public NotFoundError()
            : base(404, HttpStatusCode.NotFound.ToString())
        {
        }


        public NotFoundError(string message)
            : base(404, HttpStatusCode.NotFound.ToString(), message)
        {
        }
    }

    public class PostgressError : ApiError
    {
        public PostgressError()
            : base(409, HttpStatusCode.Conflict.ToString())
        {
        }


        public PostgressError(string message)
            : base(409, HttpStatusCode.Conflict.ToString(), message)
        {
        }
    }

    public class NotModifiedError : ApiError
    {
        public NotModifiedError()
            : base(304, HttpStatusCode.NotModified.ToString())
        {
        }


        public NotModifiedError(string message)
            : base(304, HttpStatusCode.NotModified.ToString(), message)
        {
        }
    }

    public class UnauthorizedError : ApiError
    {
        public UnauthorizedError()
            : base(401, HttpStatusCode.Unauthorized.ToString())
        {
        }


        public UnauthorizedError(string message)
            : base(401, HttpStatusCode.Unauthorized.ToString(), message)
        {
        }
    }
}