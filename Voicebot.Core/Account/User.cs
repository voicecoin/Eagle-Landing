﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using EntityFrameworkCore.BootKit;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voicebot.Core.Account
{
    /// <summary>
    /// User profile
    /// </summary>
    public class User : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(64)]
        [DataType(DataType.EmailAddress)]
        public String Email { get; set; }

        [Required]
        [StringLength(32)]
        public String FirstName { get; set; }

        [Required]
        [StringLength(32)]
        public String LastName { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SignupDate { get; set; }

        [NotMapped]
        public String FullName { get { return $"{FirstName} {LastName}"; } }

        public List<RoleOfUser> Roles { get; set; }

        public UserAuth Authenticaiton { get; set; }

        public User()
        {
            SignupDate = DateTime.UtcNow;
        }
    }
}
