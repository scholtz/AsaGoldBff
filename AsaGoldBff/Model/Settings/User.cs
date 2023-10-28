﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsaGoldBff.Model.Settings
{
    /// <summary>
    /// Configuration user
    /// 
    /// Users in the configurataion cannot be deleted
    /// </summary>
    public class User
    {
        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Password will be sent to this email in first run
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Admin Phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Do not setup password. This is for tesing purposes only.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// If not defined, Admin role is set
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Roles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
