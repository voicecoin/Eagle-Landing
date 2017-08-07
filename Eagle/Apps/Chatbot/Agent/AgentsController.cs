﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core;
using Apps.Chatbot.DomainModels;
using Utility;
using Core.Interfaces;
using Apps.Chatbot.DmServices;
using Enyim.Caching;
using System.Collections.Generic;

namespace Apps.Chatbot.Agent
{
    public class AgentsController : CoreController
    {
        // GET: v1/Agents
        [HttpGet("Query")]
        public DmPageResult<AgentEntity> GetAllAgents(string name, [FromQuery] int page = 1)
        {
            var query = dc.Table<AgentEntity>().AsQueryable();
            if (!String.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            var total = query.Count();

            int size = 20;

            var items = query.Skip((page - 1) * size).Take(size).Select(x => x.Map<AgentEntity>()).ToList();
            return new DmPageResult<AgentEntity> { Total = total, Page = page, Size = size, Items = items };
        }

        [HttpGet("MyAgents")]
        public IEnumerable<Object> MyAgents()
        {
            return dc.Table<AgentEntity>().Where(x => x.CreatedUserId == GetCurrentUser().Id).OrderBy(x => x.CreatedDate).Select(x => new { x.Name, x.Id });
        }

        // GET: v1/Agents
        [HttpGet("{userId}/Query")]
        public DmPageResult<AgentEntity> GetAgents([FromRoute] string userId, string name, [FromQuery] int page = 1)
        {
            var query = dc.Table<AgentEntity>().Where(x => x.CreatedUserId == userId);
            if (!String.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            var total = query.Count();

            int size = 20;

            var items = query.Skip((page - 1) * size).Take(size).Select(x => x.Map<AgentEntity>()).ToList();
            return new DmPageResult<AgentEntity> { Total = total, Page = page, Size = size, Items = items };
        }

        // GET: v1/Agents/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAgent([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agents = await dc.Table<AgentEntity>().SingleOrDefaultAsync(m => m.Id == id);

            if (agents == null)
            {
                return NotFound();
            }

            var agent = agents.Map<AgentEntity>();

            return Ok(agent);
        }

        // PUT: v1/Agents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgents([FromRoute] string id, [FromBody] AgentEntity agentModel)
        {
            if (id != agentModel.Id)
            {
                return BadRequest("Agent id not match.");
            }

            dc.Transaction<IDbRecord4SqlServer>(delegate {
                var agentRecord = dc.Table<AgentEntity>().Find(id);

                agentRecord.Name = agentModel.Name;
                agentRecord.Description = agentModel.Description;
                //agentRecord.Language = agentModel.Language;
                if (!String.IsNullOrEmpty(agentModel.Avatar))
                {
                    agentRecord.Avatar = agentModel.Avatar;
                }
                agentRecord.IsPublic = agentModel.IsPublic;
            });

            return Ok();
        }

        // POST: v1/Agents
        [HttpPost]
        public async Task<IActionResult> PostAgent([FromBody] AgentEntity agentModel)
        {
            var dm = new BundleDomainModel<AgentEntity>(dc, agentModel);
            dm.ValideModel(ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            dc.Transaction<IDbRecord4SqlServer>(delegate
            {
                dm.Add();
            });

            return Ok(dm.Entity);
        }

        // DELETE: v1/Agents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgents([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agents = await dc.Table<AgentEntity>().SingleOrDefaultAsync(m => m.Id == id);
            if (agents == null)
            {
                return NotFound();
            }

            dc.Table<AgentEntity>().Remove(agents);
            dc.SaveChanges();

            return Ok(agents);
        }

        private bool AgentsExists(string id)
        {
            return dc.Table<AgentEntity>().Any(e => e.Id == id);
        }
    }
}