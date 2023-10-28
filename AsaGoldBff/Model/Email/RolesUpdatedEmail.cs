﻿namespace AsaGoldBff.Model.Email
{
    /// <summary>
    /// User with permissions to invite other people can invite them and 
    /// </summary>
    public class RolesUpdatedEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public RolesUpdatedEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            Website = url;
            SupportEmail = supportEmail;
            SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "Invitation";
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Roles
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Roles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
    }
}
