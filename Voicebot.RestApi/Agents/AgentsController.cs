﻿using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voicebot.Core.Chatbots;

namespace Voicebot.RestApi.Agents
{
    /// <summary>
    /// Agent controller
    /// </summary>
    public class AgentsController : CoreController
    {
        /// <summary>
        /// Create agent with basic information
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        [HttpPost]
        public string Create([FromBody] VmAgent agent)
        {
            var bot = new AiBot();
            string agentId = String.Empty;

            dc.DbTran(() =>
            {
                agentId = bot.CreateAgent(dc, agent.ToObject<Agent>());
            });

            return agentId;
        }

        [HttpGet("{agentId}")]
        public Object GetAgentDetail([FromRoute] string agentId)
        {
            var agent = dc.Table<Agent>().Find(agentId);

            return agent;
        }

        [HttpGet("MyAgents")]
        public PageResult<VmAgent> MyAgents()
        {
            var result = new PageResult<VmAgent>() { };
            var query = dc.Table<Agent>().Where(x => x.UserId == CurrentUserId).Select(x => x.ToObject<VmAgent>());
            return result.LoadRecords<VmAgent>(query);
        }

        [HttpGet("{userId}/query")]
        public PageResult<Object> Query([FromRoute] string userId)
        {
            var result = new PageResult<Object>() { };
            var query = dc.Table<Agent>().Where(x => x.UserId == userId);
            return result.LoadRecords<Object>(query);
        }

        [AllowAnonymous]
        [HttpGet("{agentId}/train/{clientAccessToken}")]
        public string Train([FromRoute] string agentId, [FromRoute] string clientAccessToken)
        {
            var config = new AIConfiguration(clientAccessToken, SupportedLanguage.English);

            var rasa = new RasaAi(dc, config);
            rasa.agent = rasa.LoadAgent();
            return rasa.Train(dc);
        }
    }
}
