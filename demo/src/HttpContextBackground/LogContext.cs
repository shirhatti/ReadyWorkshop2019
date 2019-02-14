using Microsoft.AspNetCore.Http;

namespace HttpContextBackground
{
    public class CaptureHttpContextLogContext : ILogContext
    {
        private readonly HttpContext _httpContext;
        public CaptureHttpContextLogContext(HttpContext context)
        {
            _httpContext = context;
        }
        public string Path { get { return _httpContext.Request.Path; } }

        public string TraceIdentifier { get { return _httpContext.TraceIdentifier; } }
    }
    public class CopyLogContext : ILogContext
    {
        public CopyLogContext(HttpContext httpContext)
        {
            Path = httpContext.Request.Path;
            TraceIdentifier = httpContext.TraceIdentifier;
        }
        public string Path { get; }

        public string TraceIdentifier { get; }
    }

    public interface ILogContext
    {
        string Path { get; }
        string TraceIdentifier { get; }
    }
}
