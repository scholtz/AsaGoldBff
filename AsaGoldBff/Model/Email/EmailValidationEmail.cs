namespace AsaGoldBff.Model.Email
{
    /// <summary>
    /// User with permissions to invite other people can invite them and 
    /// </summary>
    public class EmailValidationEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public EmailValidationEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            Website = url;
            SupportEmail = supportEmail;
            SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "EmailValidation";
        /// <summary>
        /// Link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Email validation code
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Terms and condition link
        /// </summary>
        public string TermsLink { get; set; }
        /// <summary>
        /// Gdpr link
        /// </summary>
        public string GDPRLink { get; set; }
        /// <summary>
        /// If user did not opt in to marketing communication, this will be true
        /// </summary>
        public bool HasNotMarketingAgreement { get; set; }
    }
}
