﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackEnd.Data;
using ConferenceDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ConferencesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConferenceResponse>>> GetConferences()
        {
            var conferences = await _db.Conferences.AsNoTracking().Select(s => new ConferenceResponse
            {
                ID = s.ID,
                Name = s.Name
            })
            .ToListAsync();

            return conferences;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ConferenceResponse>> GetConference(int id)
        {
            var conference = await _db.FindAsync<Data.Conference>(id);

            if (conference == null)
            {
                return NotFound();
            }

            var result = new ConferenceResponse
            {
                ID = conference.ID,
                Name = conference.Name
            };

            return result;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadConference([Required, FromForm]string conferenceName, IFormFile file)
        {

            var loader = new SessionizeLoader();

            using (var ms = new MemoryStream())
            {
                file.OpenReadStream().Dispose();
                file.CopyTo(ms);
                // Rewind the MemoryStream
                ms.Position = 0;
                await loader.LoadDataAsync(conferenceName, ms, _db);
            }

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<ConferenceResponse>> CreateConference(ConferenceDTO.Conference input)
        {
            var conference = new Data.Conference
            {
                Name = input.Name
            };

            _db.Conferences.Add(conference);
            await _db.SaveChangesAsync();

            var result = new ConferenceDTO.ConferenceResponse
            {
                ID = conference.ID,
                Name = conference.Name
            };

            return CreatedAtAction(nameof(GetConference), new { id = conference.ID }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutConference(int id, ConferenceDTO.Conference input)
        {
            var conference = await _db.FindAsync<Data.Conference>(id);

            if (conference == null)
            {
                return NotFound();
            }

            conference.Name = input.Name;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ConferenceResponse>> DeleteConference(int id)
        {
            var conference = await _db.FindAsync<Data.Conference>(id);

            if (conference == null)
            {
                return NotFound();
            }

            _db.Remove(conference);

            await _db.SaveChangesAsync();

            var result = new ConferenceDTO.ConferenceResponse
            {
                ID = conference.ID,
                Name = conference.Name
            };
            return result;
        }
    }
}
