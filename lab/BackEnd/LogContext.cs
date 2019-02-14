using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd
{
    public class LogContext : ILogContext
    {
        private readonly HttpContext _httpContext;
        public LogContext(HttpContext context)
        {
            _httpContext = context;
        }
        public string Path { get { return _httpContext.Request.Path; } }

        public string TraceIdentifier { get { return _httpContext.TraceIdentifier; } }
    }

    public interface ILogContext
    {
        string Path { get; }
        string TraceIdentifier { get; }
    }
}
