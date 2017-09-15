using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Core;
using Apps.Chatbot.DomainModels;
using Utility;
using Apps.Chatbot.DmServices;
using Apps.Chatbot.Agent;
using Enyim.Caching;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Collections.Generic;

namespace Apps.Chatbot.Conversation
{
    public class ConversationController : CoreController
    {
        [HttpGet("{conversationId}/Reset")]
        public void Reset(string conversationId)
        {
            dc.Transaction<IDbRecord4Core>(delegate {

                var conversation = dc.Table<ConversationEntity>().Find(conversationId);

                dc.Table<ConversationParameterEntity>()
                    .RemoveRange(dc.Table<ConversationParameterEntity>()
                                    .Where(x => x.ConversationId == conversationId)
                                    .ToList());

                dc.Table<ConversationMessageEntity>()
                    .RemoveRange(dc.Table<ConversationMessageEntity>()
                                    .Where(x => x.ConversationId == conversationId)
                                    .ToList());

            });
        }

        [HttpGet("{agentId}")]
        public String Init(string agentId)
        {
            var conversationId = Guid.Empty.ToString();

            dc.CurrentUser = GetCurrentUser();
            var conversation = dc.Table<ConversationEntity>().FirstOrDefault(x => x.AgentId == agentId && x.CreatedUserId == dc.CurrentUser.Id);
            if (conversation == null)
            {
                dc.Transaction<IDbRecord4Core>(delegate
                {
                    var dm = new DomainModel<ConversationEntity>(dc, new ConversationEntity
                    {
                        AgentId = agentId
                    });

                    dm.AddEntity();

                    conversationId = dm.Entity.Id;
                });
            }
            else
            {
                conversationId = conversation.Id;
            }

            return conversationId;
        }

        [AllowAnonymous]
        [HttpGet("Test")]
        public async Task<String> Test()
        {
            var agentRecord = dc.Table<AgentEntity>().First(x => x.ClientAccessToken == "8084658aed844e3a985bca7b6c8cf0d3");
            DmAgentRequest agentRequestModel = new DmAgentRequest { Agent = agentRecord, ConversationId = Guid.NewGuid().ToString() };
            StringBuilder contents = new StringBuilder();

            List<String> questions = new List<string>() {
                "张学友身高"
            };
            List<String> answers = new List<string>();

            foreach (String question in questions)
            {
                agentRequestModel.Text = question;
                DmAgentResponse response = agentRequestModel.TextRequest(dc);
                answers.Add(response.Text);
            }

            // return text
            for (int i = 0; i < questions.Count; i++)
            {
                contents.AppendLine($"User: {questions[i]}");
                contents.AppendLine($" Bot: {answers[i]}");
                //contents.AppendLine();
            }

            return contents.ToString();
        }

        public async Task<String> Text(DmAgentRequest analyzerModel)
        {
            // analyzerModel.Log(MyLogLevel.DEBUG);
            // Yaya UserName: gh_0a3fe78f2d13, key: ce36fa6d0ec047248da3354519658734
            // Lingxihuagu UserName: gh_c96a6311ab6d, key: f8bc556e63364c5a8b4e37000d897704
            dc.CurrentUser = GetCurrentUser();
            var timeStart = DateTime.UtcNow;

            var agentRecord = dc.Table<AgentEntity>().First(x => x.ClientAccessToken == analyzerModel.ClientAccessToken);
            DmAgentRequest agentRequestModel = new DmAgentRequest { Agent = agentRecord, Text = analyzerModel.Text, ConversationId = analyzerModel.ConversationId };
            DmAgentResponse response;
            // 人物通，特殊处理
            if (agentRequestModel.Agent.Id == "b8d4d157-611a-40cb-ad5a-142987a73b8a")
            {
                /*response = await HttpClientHelper.PostAsJsonAsync<DmAgentResponse>(Configuration.GetSection("NlpApi:PeopleHost").Value, Configuration.GetSection("NlpApi:PeoplePath").Value,
                    new
                    {
                        Text = analyzerModel.Text,
                        ConversationId = analyzerModel.ConversationId,
                        AgentId = "f3123461-cdeb-4f0f-bdea-8b2c984115e8",
                        AccessToken = "ea60f7d6e6ee45209370248f15eb91a1"
                    });*/
            }
            else
            {
                // response = agentRequestModel.TextRequest(dc, Configuration);
            }

            response = agentRequestModel.TextRequest(dc);

            if (response == null || String.IsNullOrEmpty(response.Text))
            {
                var url = Configuration.GetSection("NlpApi:TulingUrl").Value;
                var key = Configuration.GetSection("NlpApi:TulingKey").Value;

                var result = await RestHelper.Rest<TulingResponse>(url,
                    new
                    {
                        userid = analyzerModel.ConversationId,
                        key = key,
                        info = analyzerModel.Text
                    });

                result.ResponseTime = (DateTime.UtcNow - timeStart).Milliseconds;
                result.Log(MyLogLevel.DEBUG);
                return result.Text;
            }
            else
            {
                response.Log(MyLogLevel.DEBUG);

                return response.Text;
            }

        }
    }

    public class TulingResponse
    {
        public int Code { get; set; }
        public string Text { get; set; }
        public int ResponseTime { get; set; }
    }
}