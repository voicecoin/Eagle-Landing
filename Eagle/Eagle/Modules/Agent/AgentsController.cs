﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Eagle.DbContexts;
using Eagle.DbTables;
using Eagle.Models;
using Eagle.Utility;

namespace Eagle.Modules.Agent
{
    [Route("v1/Agents")]
    public class AgentsController : ControllerBase
    {
        private readonly DataContexts _context = new DataContexts();

        // GET: v1/Agents
        [HttpGet("list/{userId}")]
        public IEnumerable<AgentModel> GetAgents([FromRoute] string userId)
        {
            return _context.Agents.Where(x => x.UserId == userId).Select(x => x.Map<AgentModel>());
        }

        // GET: v1/Agents/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAgent([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agents = await _context.Agents.SingleOrDefaultAsync(m => m.Id == id);

            if (agents == null)
            {
                return NotFound();
            }

            var agent = agents.Map<AgentDetailModel>();

            return Ok(agent);
        }

        // PUT: v1/Agents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgents([FromRoute] string id, [FromBody] AgentDetailModel agentModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != agentModel.Id)
            {
                return BadRequest("Agent id not match.");
            }

            var agentRecord = _context.Agents.Find(id);

            agentRecord.Name = agentModel.Name;
            agentRecord.Description = agentModel.Description;
            agentRecord.Language = agentModel.Language;

            _context.Entry(agentRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgentsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: v1/Agents
        [HttpPost]
        public async Task<IActionResult> PostAgent([FromBody] AgentDetailModel agentModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Agents agentRecord = agentModel.Map<Agents>();

            agentRecord.Language = "zh-cn";
            agentRecord.ClientAccessToken = Guid.NewGuid().ToString("N");
            agentRecord.DeveloperAccessToken = Guid.NewGuid().ToString("N");

            _context.Agents.Add(agentRecord);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAgents", new { id = agentRecord.Id }, new { id = agentRecord.Id });
        }

        // DELETE: v1/Agents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgents([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agents = await _context.Agents.SingleOrDefaultAsync(m => m.Id == id);
            if (agents == null)
            {
                return NotFound();
            }

            _context.Agents.Remove(agents);
            await _context.SaveChangesAsync();

            return Ok(agents);
        }

        private bool AgentsExists(string id)
        {
            return _context.Agents.Any(e => e.Id == id);
        }
    }
}