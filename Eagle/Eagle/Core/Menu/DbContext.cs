﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eagle.DbTables;
using Eagle.Models;

namespace Eagle.DataContexts
{
    public partial class CoreDbContext
    {
        public DbSet<MenuEntity> Menus { get; set; }
    }
}