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
        public async Task<bool> SendVerificationEmail(string email, string constent, bool marketingConsent, UserWithHeader user)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ", ""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);
            if (string.IsNullOrEmpty(user?.Name)) throw new ArgumentNullException("user");

            AsaGoldRepository.AccountDBBase? userData = null;
            try
            {
                userData = await repository.AccountGetByIdAsync(user.Name);
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("No Content"))
                {
                    throw; // else it is null
                }
            }
            if (userData == null)
            {
                userData = await repository.AccountUpsertAsync(user.Name, new AsaGoldRepository.Account()
                {
                    Consent = "",
                    Email = "",
                    MarketingConsent = false,
                    LastEmailValidationTime = DateTimeOffset.UtcNow
                });
            }
            else
            {
                if (userData.Data?.LastEmailValidationTime.HasValue == true)
                {
                    if (userData.Data.LastEmailValidationTime.Value.AddHours(1) > DateTimeOffset.UtcNow)
                    //if (userData.Data.LastEmailValidationTime > DateTimeOffset.UtcNow)
                    {
                        throw new Exception("You have recently requested email validaiton. Please try again in one hour");
                    }
                }

                userData = await repository.AccountPatchAsync(user.Name, new List<AsaGoldRepository.AccountOperation>() { new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "LastEmailValidationTime",
                    Value = DateTimeOffset.UtcNow
                } });

            }



            var repo = await repository.EmailValidationUpsertAsync(Guid.NewGuid().ToString(), new AsaGoldRepository.EmailValidation()
            {
                Account = user.Name,
                Consent = constent,
                Email = email,
                MarketingConsent = marketingConsent
            });
            var emailToSend = new Model.Email.EmailValidationEmail(CultureInfo.CurrentCulture.Name, options.CurrentValue.URL, options.CurrentValue.SupportEmail, options.CurrentValue.SupportPhone);
            emailToSend.Code = repo.Id;
            emailToSend.Link = $"{options.CurrentValue.URL}/email-validation/{repo.Id}";
            emailToSend.HasNotMarketingAgreement = !marketingConsent;
            emailToSend.GDPRLink = $"{options.CurrentValue.URL}/gdpr/{constent}";
            emailToSend.TermsLink = $"{options.CurrentValue.URL}/terms/{constent}";

            return await emailSender.SendEmail("Start your journey with ASA.Gold with validating your email", email, "", emailToSend);
        }
    }
}
