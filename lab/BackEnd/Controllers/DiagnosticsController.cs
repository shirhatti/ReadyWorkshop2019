using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly MessageQueue _queue;

        public DiagnosticsController(MessageQueue queue)
        {
            _queue = queue;
        }

        // GET: api/Diagnostics
       [HttpGet]
        public IEnumerable<PathCount> Get() => _queue.GetPathCount();

        // GET: api/Diagnostics/5
        [HttpGet("{path}", Name = "Get")]
        public PathCount Get(string path) => _queue.GetPathCount(path);

    }
}
