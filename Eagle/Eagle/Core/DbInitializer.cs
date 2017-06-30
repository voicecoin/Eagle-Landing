﻿using AutoMapper;
using Eagle.DbContexts;
using Eagle.DbTables;
using Eagle.DddServices;
using Eagle.Enums;
using Eagle.Models;
using Eagle.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Eagle
{
    public class DbInitializer
    {
        public static void Initialize(IHostingEnvironment env)
        {
            DataContexts context = new DataContexts(new DbContextOptions<DataContexts>());
            //var dbContexts = serviceProvider.GetService<DataContexts>();

            context.Database.EnsureCreated();

            InitAgent(env, context);
        }

        private static void InitAgent(IHostingEnvironment env, DataContexts context)
        {
            // create a agent
            var agentNames = LoadJson<List<String>>(env, "Agents");

            string id = Guid.NewGuid().ToString();
            string id2 = Guid.NewGuid().ToString();
            string token1 = Guid.NewGuid().ToString("N");
            string token2 = Guid.NewGuid().ToString("N");

            List<Agents> agents = new List<Agents>();

            agentNames.ForEach(agentName => {

                var agent = LoadJson<Agents>(env, $"{agentName}\\Agent");

                if (!context.Agents.Any(x => x.Name == agentName))
                {
                    context.Transaction(delegate
                    {
                        context.Agents.Add(agent);
                    });
                }

                agents.Add(agent);
            });

            agents.ForEach(agent =>
            {
                if (!context.Entities.Any(x => x.AgentId == agent.Id))
                {
                    context.Transaction(delegate
                    {
                        InitEntities(env, context, agent);
                    });
                }
            });

            agents.ForEach(agent =>
            {
                if (!context.Intents.Any(x => x.AgentId == agent.Id))
                {
                    context.Transaction(delegate
                    {
                        InitIntents(env, context, agent);
                    });
                }
            });
        }

        private static void InitIntents(IHostingEnvironment env, DataContexts context, Agents agent)
        {
            var intentNames = Directory.GetFiles($"{env.ContentRootPath}\\App_Data\\{agent.Name}\\Intents").Select(x => x.Split('\\').Last().Split('.').First()).ToList();

            intentNames.ForEach(entityName =>
            {
                // Intent
                var intentModel = LoadJson<IntentModel>(env, $"{agent.Name}\\Intents\\{entityName}");
                intentModel.AgentId = agent.Id;
                intentModel.Name = entityName;

                var intentRecord = intentModel.Map<Intents>();
                context.Intents.Add(intentRecord);

                // User expression
                intentModel.UserSays.ForEach(expression => {
                    var intentExpression = new IntentExpressions()
                    {
                        IntentId = intentRecord.Id,
                        Text = expression.Text
                    };

                    // Markup
                    var model = new AnalyzerModel { Text = intentExpression.Text };
                    model.Ner(context).ForEach(itemModel =>
                    {
                        var itemRecord = itemModel.MapByJsonString<IntentExpressionItems>();
                        itemRecord.IntentExpressionId = intentExpression.Id;
                        context.IntentExpressionItems.Add(itemRecord);
                    });

                    var userSay = intentExpression.Map<IntentExpressionModel>();
                    userSay.Add(context);
                });

                // Bot response
                intentModel.Responses.ForEach(response => {
                    response.IntentId = intentRecord.Id;
                    response.Add(context);
                });
            });
        }

        private static void InitEntities(IHostingEnvironment env, DataContexts context, Agents agent)
        {
            var entityNames = Directory.GetFiles($"{env.ContentRootPath}\\App_Data\\{agent.Name}\\Entities").Select(x => x.Split('\\').Last().Split('.').First()).ToList();

            entityNames.ForEach(entityName =>
            {
                // add entity
                EntityModel entity = LoadEntityFromJsonFile(env, agent, entityName);
                entity.AgentId = agent.Id;
                entity.Name = entityName;
                entity.Add(context);
            });
        }

        private static EntityModel LoadEntityFromJsonFile(IHostingEnvironment env, Agents agent, string name)
        {
            string json;
            using (StreamReader SourceReader = File.OpenText($"{env.ContentRootPath}\\App_Data\\{agent.Name}\\Entities\\{name}.json"))
            {
                json = SourceReader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<EntityModel>(json);
        }

        private static T LoadJson<T>(IHostingEnvironment env, string fileName)
        {
            string json;
            using (StreamReader SourceReader = File.OpenText($"{env.ContentRootPath}\\App_Data\\" + fileName + ".json"))
            {
                json = SourceReader.ReadToEnd();
                if (String.IsNullOrEmpty(json))
                {
                    return default(T);
                }
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
