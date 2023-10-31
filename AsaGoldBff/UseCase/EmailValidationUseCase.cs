using Algorand.Algod;
using Algorand;
using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Auth;
using AsaGoldBff.Model.Email;
using AsaGoldRepository;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NLog.Web.LayoutRenderers;
using System.Globalization;
using System.Security.Claims;
using AlgorandAuthentication;
using Algorand.Utils;
using AsaGoldBff.Model.Result;

namespace AsaGoldBff.UseCase
{
    public class EmailValidationUseCase
    {
        /// <summary>
        /// Unit tests can turn off validate time feature
        /// </summary>
        public static bool ValidateTime = true;
        private readonly IEmailSender emailSender;
        private readonly IOptionsMonitor<Model.Config.BFFOptions> options;
        private readonly IOptionsMonitor<AlgorandAuthenticationOptions> algodOptions;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="emailSender"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public EmailValidationUseCase(
            IEmailSender emailSender,
            IOptionsMonitor<Model.Config.BFFOptions> options,
            IOptionsMonitor<AlgorandAuthenticationOptions> algodOptions
            )
        {
            this.emailSender = emailSender;
            this.options = options;
            this.algodOptions = algodOptions;
            if (string.IsNullOrEmpty(options.CurrentValue.RepositoryUrl)) throw new Exception("RepositoryUrl is empty");
        }
        /// <summary>
        /// Send email to user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> SendVerificationEmail(string email, string terms, string gdpr, bool marketingConsent, UserWithHeader user)
        {
            using var client = new HttpClient();

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
                    Gdpr = "",
                    TermsAndConditions = "",
                    Email = "",
                    MarketingConsent = false,
                    LastEmailValidationTime = DateTimeOffset.UtcNow
                });
            }
            else
            {
                if (userData.Data?.LastEmailValidationTime.HasValue == true)
                {
                    if (ValidateTime)
                    {
                        if (userData.Data.LastEmailValidationTime.Value.AddHours(1) > DateTimeOffset.UtcNow)
                        //if (userData.Data.LastEmailValidationTime > DateTimeOffset.UtcNow)
                        {
                            throw new Exception("You have recently requested email validaiton. Please try again in one hour");
                        }
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
                TermsAndConditions = terms,
                Gdpr = gdpr,
                Email = email,
                MarketingConsent = marketingConsent
            });
            var emailToSend = new Model.Email.EmailValidationEmail(CultureInfo.CurrentCulture.Name, options.CurrentValue.URL, options.CurrentValue.SupportEmail, options.CurrentValue.SupportPhone);
            emailToSend.Code = repo.Id;
            emailToSend.Link = $"{options.CurrentValue.URL}/email-validation/{repo.Id}";
            emailToSend.HasNotMarketingAgreement = !marketingConsent;
            emailToSend.GDPRLink = $"{options.CurrentValue.URL}/gdpr/{gdpr}";
            emailToSend.TermsLink = $"{options.CurrentValue.URL}/terms/{terms}";

            return await emailSender.SendEmail("Start your journey with ASA.Gold with validating your email", email, "", emailToSend);
        }
        /// <summary>
        /// Returns
        /// </summary>
        /// <param name="emailVerificationGuid"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>

        public async Task<SuccessWithTransaction> VerifyEmail(string emailVerificationGuid, UserWithHeader user)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ", ""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);
            if (string.IsNullOrEmpty(user?.Name)) throw new ArgumentNullException("user");

            EmailValidationDBBase? record = null;
            try
            {
                record = await repository.EmailValidationGetByIdAsync(emailVerificationGuid.ToString());
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("No Content"))
                {
                    throw; // else it is null
                }
                throw new Exception("Invalid email verification id");
            }

            if (record.CreatedBy != user.Name)
            {
                throw new Exception("Email validation was issued for different user");
            }

            if (record.Created.AddDays(7) < DateTimeOffset.UtcNow)
            {
                throw new Exception("Email validation is valid only for 7 days. Please create new validation request.");
            }
            if (record.Data.Used)
            {
                throw new Exception("This email verification code has been already used.");
            }

            /// set verification to be used 
            var updatedVerification = await repository.EmailValidationPatchAsync(user.Name, new List<AsaGoldRepository.EmailValidationOperation>() {
                new AsaGoldRepository.EmailValidationOperation()
                {
                    Op = "replace",
                    Path = "Used",
                    Value = true
                }
            });

            var updatedAccount = await repository.AccountPatchAsync(user.Name, new List<AsaGoldRepository.AccountOperation>() {
                new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "TermsAndConditions",
                    Value = record.Data.TermsAndConditions
                },
                new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "GDPR",
                    Value = record.Data.Gdpr
                },
                new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "Email",
                    Value = record.Data.Email
                },
                new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "MarketingConsent",
                    Value = record.Data.MarketingConsent
                }
            });
            string? txId = null;
            if (options.CurrentValue.AirdropAlgoOnEmailVerification > 0)
            {
                using var httpClient = HttpClientConfigurator.ConfigureHttpClient(algodOptions.CurrentValue.AlgodServer, algodOptions.CurrentValue.AlgodServerToken, algodOptions.CurrentValue.AlgodServerHeader);
                DefaultApi algodApiInstance = new DefaultApi(httpClient);
                var transParams = await algodApiInstance.TransactionParamsAsync();

                var account = AlgorandARC76AccountDotNet.ARC76.GetAccount(options.CurrentValue.Account);
                var payment = Algorand.Algod.Model.Transactions.PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account.Address, new Algorand.Address(user.Name), options.CurrentValue.AirdropAlgoOnEmailVerification, "asa.gold", transParams);
                var signed = payment.Sign(account);
                try
                {
                    txId = (await Utils.SubmitTransaction(algodApiInstance, signed))?.Txid;
                }
                catch (Algorand.ApiException<Algorand.Algod.Model.ErrorResponse> e)
                {
                    Console.Error.WriteLine(e.Result.Message);
                    if (!string.IsNullOrEmpty(e.Result.Message)) throw new Exception(e.Result.Message);
                    throw;
                }
            }


            return new SuccessWithTransaction()
            {
                TransactionId = txId
            };
        }
    }
}
