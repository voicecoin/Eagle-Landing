﻿using Core.DbTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.DataContexts
{
    public partial class CoreDbContext
    {
        public DbSet<Entities> Chatbot_Entities { get; set; }
        public DbSet<EntityEntries> Chatbot_EntityEntries { get; set; }
        public DbSet<EntityEntrySynonyms> Chatbot_EntityEntrySynonyms { get; set; }
    }
}