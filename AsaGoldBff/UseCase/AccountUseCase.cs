using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Auth;
using AsaGoldBff.Model.Result;
using AsaGoldBff.Model.Settings;
using AsaGoldRepository;
using Microsoft.Extensions.Options;

namespace AsaGoldBff.UseCase
{
    public class AccountUseCase
    {
        private readonly IEmailSender emailSender;
        private readonly IOptionsMonitor<Model.Config.BFFOptions> options;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="emailSender"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public AccountUseCase(
            IEmailSender emailSender,
            IOptionsMonitor<Model.Config.BFFOptions> options
            )
        {
            this.options = options;
            if (string.IsNullOrEmpty(options.CurrentValue.RepositoryUrl)) throw new Exception("RepositoryUrl is empty");
        }
        /// <summary>
        /// Send email to user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<AsaGoldRepository.Account?> GetAccount(UserWithHeader user)
        {
            if (string.IsNullOrEmpty(user?.Name)) throw new ArgumentNullException("user");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ", ""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);

            try
            {
                var userData = await repository.AccountGetByIdAsync(user.Name);
                return userData.Data;
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("No Content"))
                {
                    throw; // else it is null
                }
                return null;
            }
        }
        /// <summary>
        /// User can update his KYC form. In this case we create new update request and put the requestid to his account.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<KYCRequestDBBase?> UpdateProfile(KYCRequest request, UserWithHeader user)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ", ""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);
            var userData = await repository.AccountGetByIdAsync(user.Name);
            if (userData == null) throw new Exception("Please create the account first by validating email");
            if (string.IsNullOrEmpty(userData.Data.TermsAndConditions)) throw new Exception("Please create the account first by validating email and consent");
            var requestId = Guid.NewGuid().ToString();
            var storedRequest = await repository.KYCRequestUpsertAsync(requestId, request);
            var updatedAccount = await repository.AccountPatchAsync(user.Name, new List<AsaGoldRepository.AccountOperation>() {
                new AsaGoldRepository.AccountOperation()
                {
                    Op = "replace",
                    Path = "LastKYCRequestId",
                    Value = storedRequest.Id
                }
            });
            return storedRequest;
        }
        /// <summary>
        /// Return user's profile to user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<KYCRequest?> GetProfile(UserWithHeader user)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", user.Header.Replace("SigTx ", ""));
            var repository = new AsaGoldRepository.Client(options.CurrentValue.RepositoryUrl, client);
            var userData = await repository.AccountGetByIdAsync(user.Name);
            if (userData == null || string.IsNullOrEmpty(userData.Data.LastKYCRequestId)) return null;
            var request = await repository.KYCRequestGetByIdAsync(userData.Data.LastKYCRequestId);
            return request.Data;
        }
    }
}
