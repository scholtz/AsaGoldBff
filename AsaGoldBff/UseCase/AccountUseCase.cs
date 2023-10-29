using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Auth;
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

            var client = new HttpClient();
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
    }
}
