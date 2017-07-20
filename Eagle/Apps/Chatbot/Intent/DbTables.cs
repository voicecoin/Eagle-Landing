﻿using Apps.Chatbot.DomainModels;
using Apps.Chatbot.Enums;
using Core;
using Core.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Apps.Chatbot.Intent
{
    [Table("Chatbot_Intents")]
    public class IntentEntity : DbRecord, IDbRecord4SqlServer
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }
        [MaxLength(32)]
        public String Name { get; set; }

        [MaxLength(256)]
        internal String _Contexts { get; set; }
        [NotMapped]
        public String[] Contexts
        {
            get { return _Contexts == null ? null : JsonConvert.DeserializeObject<String[]>(_Contexts); }
            set { _Contexts = JsonConvert.SerializeObject(value); }
        }

        [MaxLength(128)]
        internal String _Events { get; set; }
        [NotMapped]
        public String[] Contents
        {
            get { return _Events == null ? null : JsonConvert.DeserializeObject<String[]>(_Events); }
            set { _Events = JsonConvert.SerializeObject(value); }
        }
    }

    [Table("Chatbot_IntentExpressions")]
    public class IntentExpressionEntity : DbRecord, IDbRecord4SqlServer
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }
        [Required]
        [MaxLength(128)]
        public String Text { get; set; }

        [MaxLength]
        internal String _Items { get; set; }
        [NotMapped]
        public DmIntentExpressionItem[] Items
        {
            get { return _Items == null ? null : JsonConvert.DeserializeObject<DmIntentExpressionItem[]>(_Items); }
            set { _Items = JsonConvert.SerializeObject(value); }
        }
    }

    [Table("Chatbot_IntentResponses")]
    public class IntentResponseEntity : DbRecord, IDbRecord4SqlServer
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [MaxLength(128)]
        public String Action { get; set; }

        [MaxLength(256)]
        internal String _AffectedContexts { get; set; }
        [NotMapped]
        public DmIntentResponseContext[] AffectedContexts
        {
            get { return _AffectedContexts == null ? null : JsonConvert.DeserializeObject<DmIntentResponseContext[]>(_AffectedContexts); }
            set { _AffectedContexts = JsonConvert.SerializeObject(value); }
        }
    }

    [Table("Chatbot_IntentResponseMessages")]
    public class IntentResponseMessageEntity : DbRecord, IDbRecord4SqlServer
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }
        public IntentResponseMessageType Type { get; set; }
        public IntentResponseMessagePlatform Platform { get; set; }

        internal String _Speeches { get; set; }
        [NotMapped]
        public String[] Speeches
        {
            get { return _Speeches == null ? null : JsonConvert.DeserializeObject<String[]>(_Speeches); }
            set { _Speeches = JsonConvert.SerializeObject(value); }
        }
    }

    [Table("Chatbot_IntentResponseParameters")]
    public class IntentResponseParameterEntity : DbRecord, IDbRecord4SqlServer
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }
        [Required]
        [MaxLength(32)]
        public String Name { get; set; }
        public Boolean IsList { get; set; }
        /// <summary>
        /// Entity 
        /// </summary>
        [StringLength(36)]
        public String DataType { get; set; }
        public Boolean Required { get; set; }
        [MaxLength(32)]
        public String Value { get; set; }
        [MaxLength(64)]
        public String DefaultValue { get; set; }

        internal String _Prompts { get; set; }
        [NotMapped]
        public String[] Prompts
        {
            get { return _Prompts == null ? null : JsonConvert.DeserializeObject<String[]>(_Prompts); }
            set { _Prompts = JsonConvert.SerializeObject(value); }
        }
    }
}