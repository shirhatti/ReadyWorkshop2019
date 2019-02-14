using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using ConferenceDTO;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SessionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult<List<SessionResponse>> Get()
        {
            var sessions = _db.Sessions.AsNoTracking()
                                             .Include(s => s.Track)
                                             .Include(s => s.SessionSpeakers)
                                                .ThenInclude(ss => ss.Speaker)
                                             .Include(s => s.SessionTags)
                                                .ThenInclude(st => st.Tag)
                                             .Select(m => m.MapSessionResponse())
                                             .ToListAsync()
                                             .GetAwaiter()
                                             .GetResult();
            return sessions;
        }

        [HttpGet("{id:int}")]
        public ActionResult<SessionResponse> Get(int id)
        {
            var session = _db.Sessions.AsNoTracking()
                                            .Include(s => s.Track)
                                            .Include(s => s.SessionSpeakers)
                                                .ThenInclude(ss => ss.Speaker)
                                            .Include(s => s.SessionTags)
                                                .ThenInclude(st => st.Tag)
                                            .SingleOrDefaultAsync(s => s.ID == id)
                                            .GetAwaiter()
                                            .GetResult();

            if (session == null)
            {
                return NotFound();
            }

            return session.MapSessionResponse();
        }

        [HttpPost]
        public ActionResult<SessionResponse> Post(ConferenceDTO.Session input)
        {
            var session = new Data.Session
            {
                Title = input.Title,
                ConferenceID = input.ConferenceID,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                Abstract = input.Abstract,
                TrackId = input.TrackId
            };

            _db.Sessions.Add(session);
            _db.SaveChangesAsync()
                .GetAwaiter()
                .GetResult();

            var result = session.MapSessionResponse();

            return CreatedAtAction(nameof(Get), new { id = result.ID }, result);
        }

        [HttpPut("{id:int}")]
        public IActionResult Put(int id, ConferenceDTO.Session input)
        {
            var session = _db.Sessions.FindAsync(id)
                                    .GetAwaiter()
                                    .GetResult();

            if (session == null)
            {
                return NotFound();
            }

            session.ID = input.ID;
            session.Title = input.Title;
            session.Abstract = input.Abstract;
            session.StartTime = input.StartTime;
            session.EndTime = input.EndTime;
            session.TrackId = input.TrackId;
            session.ConferenceID = input.ConferenceID;

            _db.SaveChangesAsync()
                .GetAwaiter()
                .GetResult();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public ActionResult<SessionResponse> Delete(int id)
        {
            var session = _db.Sessions.FindAsync(id)
                            .GetAwaiter()
                            .GetResult();

            if (session == null)
            {
                return NotFound();
            }

            _db.Sessions.Remove(session);
            _db.SaveChangesAsync()
                .GetAwaiter()
                .GetResult();

            return session.MapSessionResponse();
        }
    }
}