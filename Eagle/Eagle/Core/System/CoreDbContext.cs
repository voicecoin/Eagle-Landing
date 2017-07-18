﻿using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eagle.Core.Account;
using Microsoft.Extensions.Configuration;

namespace Eagle.DataContexts
{
    public partial class CoreDbContext : DbContextWithTriggers
    {
        public DmAccount CurrentUser { get; set; }
        public static IConfigurationRoot Configuration { get; set; }

        public CoreDbContext(DbContextOptions<CoreDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // http://www.learnentityframeworkcore.com/
            // modelBuilder.Entity<TaxonomyTermEntity>().HasIndex(x => x.Name);
            // don't need this code.
            //modelBuilder.Entity<Bundle>().ForSqlServerToTable("Bundles");
            //modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
        }

        public int Transaction(Action action)
        {
            using (IDbContextTransaction transaction = Database.BeginTransaction())
            {
                int affected = 0;
                try
                {
                    action();
                    affected = SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    if (ex.Message.Contains("See the inner exception for details"))
                    {
                        throw ex.InnerException;
                    }
                    else
                    {
                        throw ex;
                    }
                }

                return affected;
            }
        }
    }
}
