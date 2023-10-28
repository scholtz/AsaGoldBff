using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Auth;
using Microsoft.Extensions.Options;
using NLog.Web.LayoutRenderers;
using System.Globalization;
using System.Security.Claims;

namespace AsaGoldBff.UseCase
{
    public class EmailValidationUseCase
    {
        private readonly IEmailSender emailSender;
        private readonly IOptionsMonitor<Model.Config.BFFOptions> options;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="emailSender"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public EmailValidationUseCase(
            IEmailSender emailSender,
            IOptionsMonitor<Model.Config.BFFOptions> options
            )
        {
            this.emailSender = emailSender;
            this.options = options;
            if (string.IsNullOrEmpty(options.CurrentValue.RepositoryUrl)) throw new Exception("RepositoryUrl is empty");
        }
        /// <summary>
        /// Send email to user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal async Task<bool> SendVerificationEmail(string email, string constent, bool marketingConsent, UserWithHeader user)
        {
            var client = new HttpClient();
            
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ",""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);
            if (string.IsNullOrEmpty(user?.Name)) throw new ArgumentNullException("user");

            var repo = await repository.EmailValidationPutAsync(Guid.NewGuid().ToString(), new AsaGoldRepository.EmailValidation()
            {
                Account = user.Name,
                Consent = constent,
                Email = email,
                MarketingConsent = marketingConsent
            });
            var emailToSend = new Model.Email.EmailValidationEmail(CultureInfo.CurrentCulture.Name, options.CurrentValue.URL, options.CurrentValue.SupportEmail, options.CurrentValue.SupportPhone);
            emailToSend.Code = repo.Id;
            emailToSend.Link = $"{options.CurrentValue.URL}/email-validation/{repo.Id}";
            return await emailSender.SendEmail("Start your journey with ASA.Gold with validating your email", email, "", emailToSend);
        }
    }
}
