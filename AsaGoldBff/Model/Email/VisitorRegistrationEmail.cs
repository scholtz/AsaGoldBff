﻿namespace AsaGoldBff.Model.Email
{
    /// <summary>
    /// Visitor registration email
    /// </summary>
    public class VisitorRegistrationEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public VisitorRegistrationEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            Website = url;
            SupportEmail = supportEmail;
            SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "VisitorRegistration";
        /// <summary>
        /// Registration code
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// BarCode image
        /// </summary>
        public string BarCode { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// Place
        /// </summary>
        public string Place { get; set; }
        /// <summary>
        /// Place description
        /// </summary>
        public string PlaceDescription { get; set; }
    }
}
