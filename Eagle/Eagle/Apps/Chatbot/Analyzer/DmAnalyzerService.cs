﻿using Eagle.Apps.Chatbot.DomainModels;
using Eagle.Apps.Chatbot.Enums;
using Eagle.DataContexts;
using Eagle.DbTables;
using Eagle.DomainModels;
using Eagle.Utility;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eagle.Apps.Chatbot.DmServices
{
    public static class DmAnalyzerService
    {
        /// <summary>
        /// 分词与词性标注,  Part-Of-Speech Tagger (POS Tagger) 
        /// </summary>
        /// <param name="analyzerModel"></param>
        /// <param name="dc"></param>
        /// <returns></returns>
        public static List<DmIntentExpressionItem> PosTagger(this DmAgentRequest analyzerModel, CoreDbContext dc)
        {
            string text = analyzerModel.Text;
            List<DmEntity> allEntities = dc.Chatbot_Entities/*.Where(x => x.AgentId == analyzerModel.Agent.Id || x.AgentId == Constants.GenesisAgentId)*/.Select(x => x.Map<DmEntity>()).ToList();

            // 识别所有单元实体
            // 用Entry识别
            var unitEntityInEntry = (from entity in dc.Chatbot_Entities
                                         join entry in dc.Chatbot_EntityEntries on entity.Id equals entry.EntityId
                                         where text.Contains(entry.Value)
                                         select new DmIntentExpressionItem
                                         {
                                             Text = entry.Value,
                                             Meta = $"@{entity.Name}",
                                             Alias = entity.Name,
                                             Length = entry.Value.Length,
                                             Color = entity.Color,
                                             Value = entry.Value
                                         }).ToList();

            // 用Synonym识别一次
            var unitEntityInSynonym = (from entity in dc.Chatbot_Entities
                                       join entry in dc.Chatbot_EntityEntries on entity.Id equals entry.EntityId
                                       join synonym in dc.Chatbot_EntityEntrySynonyms on entry.Id equals synonym.EntityEntryId
                                       where text.Contains(synonym.Synonym)
                                       select new DmIntentExpressionItem
                                       {
                                           Text = synonym.Synonym,
                                           Meta = $"@{entity.Name}",
                                           Alias = entity.Name,
                                           Length = entry.Value.Length,
                                           Color = entity.Color,
                                           Value = entry.Value
                                       }).ToList();


            // 未识别出的词，再用识别一次
            var client = new RestClient(CoreDbContext.Configuration.GetSection("NlpApi:NlpirUrl").Value);
            var request = new RestRequest("nlpir/wordsplit/" + text, Method.GET);
            var response = client.Rest(request);
            string jsongContent = response.Result.Content;
            var result = JsonConvert.DeserializeObject<NlpirResult>(jsongContent);

            // 只需要识别实体
            List<String> entityNames = allEntities.Select(x => x.Name).ToList();
            result.WordSplit.Where(x => entityNames.Contains(x.Entity)).ToList().ForEach(seg => {
                MatchCollection mc = Regex.Matches(seg.Entity, "\".+\"");
                foreach (Match m in mc)
                {
                    seg.Entity = seg.Entity.Replace(m.Value, String.Empty);
                }

                unitEntityInEntry.Add(new DmIntentExpressionItem
                {
                    Text = seg.Word,
                    Meta = "@" + seg.Entity,
                    Alias = seg.Entity,
                    Length = seg.Word.Length,
                    Color = allEntities.First(x => x.Name == seg.Entity).Color,
                    Value = seg.Word
                });
            });

            unitEntityInEntry = unitEntityInEntry.Distinct(x => x.Text).ToList();

            var unitEntityTotal = new List<DmIntentExpressionItem>();
            unitEntityTotal.AddRange(unitEntityInEntry);
            unitEntityTotal.AddRange(unitEntityInSynonym);

            var unitEntities = Process(text, unitEntityTotal);

            string template = unitEntities.GetTemplateString(); // String.Concat(unitEntities.Select(x => String.IsNullOrEmpty(x.Meta) ? x.Text : x.Meta).ToArray());
            // 识别组合实体
            var compositEntitiesQueryable = (from entity in dc.Chatbot_Entities
                                             join entry in dc.Chatbot_EntityEntries on entity.Id equals entry.EntityId
                                             where entity.IsEnum // && (entity.AgentId == analyzerModel.Agent.Id || entity.AgentId == Constants.GenesisAgentId)
                                             select new DmIntentExpressionItem
                                             {
                                                 Text = entry.Value,
                                                 Meta = $"@{entity.Name}",
                                                 Alias = entity.Name,
                                                 IsEnum = entity.IsEnum,
                                                 Color = entity.Color,
                                                 Value = entry.Value
                                             }).ToList();

            var compositEntities = Process(template, compositEntitiesQueryable).Where(x => x.Meta != null).ToList();

            int pos = 0;

            compositEntities.ForEach(comEntity =>
            {
                pos = CorrectPosition(comEntity, unitEntities, pos);
            });

            List<DmIntentExpressionItem> merged = compositEntities;

            // Merge unit and composite
            for (int idx = 0; idx < unitEntities.Count; idx++)
            {
                var unitEntity = unitEntities[idx];

                var list = compositEntities.Where(comEntity => unitEntity.Position >= comEntity.Position && (unitEntity.Length + unitEntity.Position) <= (comEntity.Length + comEntity.Position)).ToList();
                if (list.Count() == 0)
                {
                    merged.Add(unitEntity);
                }
            }

            return merged.OrderBy(x => x.Position).ToList();
        }

        public static List<DmIntentExpressionItem> Segment(this DmAgentRequest analyzerModel, CoreDbContext dc)
        {
            var taggers = PosTagger(analyzerModel, dc);

            List<DmIntentExpressionItem> segments = new List<DmIntentExpressionItem>();

            // 把识别出的实体标准化
            taggers.Where(x => !String.IsNullOrEmpty(x.Meta))
                .ToList()
                .ForEach(x => {
                    //x.Text = x.Alias;
                    segments.Add(x);
                });
            

            // 分隔未识别的词，每个词分成字符。
            taggers.Where(x => String.IsNullOrEmpty(x.Meta))
                .ToList()
                .ForEach(tag =>
                {
                    var chars = tag.Text.ToCharArray();
                    for (int idx = 0; idx < chars.Length; idx++)
                    {
                        segments.Add(new DmIntentExpressionItem
                        {
                            Position = tag.Position + idx,
                            Length = 1,
                            Text = chars[idx].ToString()
                        });
                    }
                });


            return segments;
        }

        public static string GetTemplateString(this IEnumerable<DmIntentExpressionItem> items)
        {
            return String.Concat(items.Select(x => String.IsNullOrEmpty(x.Meta) ? x.Text : "{" + x.Meta + ":" + x.Alias + "}").ToArray());
        }

        /// <summary>
        /// 机器人回复进一步处理，替换变量，填充参数
        /// </summary>
        /// <param name="responseModel"></param>
        /// <param name="dc"></param>
        public static DmIntentResponseMessage PostResponse(this DmIntentResponse responseModel, CoreDbContext dc, DmAgentRequest agentRequestModel)
        {
            // 随机选择一个回答。
            DmIntentResponseMessage messageModel = responseModel.Messages.Random();

            // Replace system token
            messageModel.ReplaceSystemToken(dc, agentRequestModel);
            messageModel.ReplaceParameterToken(dc, agentRequestModel, responseModel);

            return messageModel;
        }

        public static void ExtractParameter(this DmIntentResponse responseModel, CoreDbContext dc, DmAgentRequest agentRequestModel)
        {
            var segments = agentRequestModel.Segment(dc).Where(x => !String.IsNullOrEmpty(x.Meta)).ToList();

            responseModel.Parameters.ForEach(parameter =>
            {
                parameter.Value = segments.First(x => x.Meta == parameter.DataType).Text;
            });
        }

        /// <summary>
        /// 转换系统内置变量
        /// </summary>
        /// <param name="messageModel"></param>
        /// <param name="dc"></param>
        /// <param name="agent"></param>
        public static void ReplaceSystemToken(this DmIntentResponseMessage messageModel, CoreDbContext dc, DmAgentRequest agentRequestModel)
        {
            List<String> speechs = new List<string>();

            messageModel.Speeches.ForEach(speech => {
                speech = speech.Replace("{@agent.name}", agentRequestModel.Agent.Name);
                speech = speech.Replace("{@agent.description}", agentRequestModel.Agent.Description);

                TimeSpan age = DateTime.UtcNow - agentRequestModel.Agent.CreatedDate;
                speech = speech.Replace("{@agent.age}", $"我刚出生{(int)age.TotalDays}天");

                speechs.Add(speech);
            });

            messageModel.Speeches = speechs;
        }

        /// <summary>
        /// 转换识别实体参数
        /// </summary>
        /// <param name="messageModel"></param>
        /// <param name="dc"></param>
        /// <param name="agent"></param>
        public static void ReplaceParameterToken(this DmIntentResponseMessage messageModel, CoreDbContext dc, DmAgentRequest agentRequestModel, DmIntentResponse responseModel)
        {
            List<String> speechs = new List<string>();

            messageModel.Speeches.ForEach(speech => {
                responseModel.Parameters.ForEach(parameter => {
                    speech = speech.Replace("{$" + parameter.Name + "}", parameter.Value);
                });
                speechs.Add(speech);
            });

            messageModel.Speeches = speechs;
        }

        private static int CorrectPosition(DmIntentExpressionItem compositedEntity, List<DmIntentExpressionItem> unitEntities, int pos)
        {
            var source = unitEntities.Where(x => !String.IsNullOrEmpty(x.Meta) && compositedEntity.Text.Contains(x.Meta)).Select(x => x.Meta).Distinct().ToList();

            for (; pos < unitEntities.Count;)
            {
                var target = unitEntities.Select(x => new { Meta = x.Meta == null ? x.Text : x.Meta, x.Position, x.Text, x.Length })
                    .Skip(pos).Take(source.Count).ToList();
                var join = (from s in source
                            join t in target on s equals t.Meta
                            select t).OrderBy(x => x.Position).ToList();

                if (join.Count > 0 && join.Count == source.Count)
                {
                    compositedEntity.Position = join.FirstOrDefault().Position;
                    compositedEntity.Length = join.Sum(x => x.Length);
                    compositedEntity.Text = String.Concat(join.Select(x => x.Text));
                    pos += source.Count;

                    break;
                }
                else
                {
                    pos++;
                }
            }

            return pos;
        }

        private static bool CheckToken(DmIntentExpressionItem compositedEntity, List<DmIntentExpressionItem> unitEntities, int idx)
        {
            var source = compositedEntity.Text.Split(' ')
                .ToList()
                .Select(x => x).ToList();

            var target = unitEntities.Select(x => x.Meta == null ? x.Text : x.Meta).Skip(idx).Take(source.Count).ToList();

            var join = (from s in source
                        join t in target on s equals t
                        select s).ToList();

            return join.Count == source.Count;
        }

        /// <summary>
        /// 按顺序返回实体数组和未识别的实体，计算实体在句子中的位置。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        private static List<DmIntentExpressionItem> Process(string text, List<DmIntentExpressionItem> entities)
        {
            var tags = new List<DmIntentExpressionItem>();

            entities.ForEach(token =>
            {
                // 是组合实体
                if (token.IsEnum)
                {
                    MatchCollection mcc = Regex.Matches(token.Text, @"\{(.+?)\}");
                    List<String> entityNames = new List<String>();
                    foreach (Match m in mcc)
                    {
                        var entityName = Regex.Match(m.Value, "@.+:").ToString().Replace("@", "").Replace(":", "");
                        entityNames.Add("{@" + entityName + ":" + entityName + "}");
                    }

                    token.Text =  String.Concat(entityNames);
                }

                MatchCollection mc = Regex.Matches(text, token.Text);
                foreach (Match m in mc)
                {
                    DmIntentExpressionItem temp = new DmIntentExpressionItem()
                    {
                        Position = m.Index,
                        Length = m.Length,
                        Alias = token.Alias,
                        Text = token.Text,
                        Meta = token.Meta,
                        Color = token.Color,
                        Value = token.Text
                    };

                    tags.Add(temp);
                }

            });

            tags = tags.OrderBy(x => x.Position).ToList();

            var results = new List<DmIntentExpressionItem>();

            // 扫描字符串
            for (int pos = 0; pos < text.Length;)
            {
                // 查找实体
                var tag = tags.FirstOrDefault(x => x.Position == pos);
                if (tag != null)
                {
                    results.Add(tag);
                    pos += tag.Length;
                }
                else
                {
                    // 取下一个, 如果没找到实体，就一直取到最后一个字符。
                    tag = tags.FirstOrDefault(x => x.Position > pos);
                    int length = tag == null ? text.Length - pos : tag.Position - pos;

                    // 如果没有识别为实体，则切开每个字符。
                    // int length = 1;

                    var item = new DmIntentExpressionItem
                    {
                        Text = text.Substring(pos, length),
                        Position = pos,
                        Length = length,
                        Value = text.Substring(pos, length)
                    };

                    results.Add(item);

                    pos += length;
                }
            }

            return results;
        }
    }
}
