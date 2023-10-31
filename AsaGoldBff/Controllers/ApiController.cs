using AsaGoldBff.Model.Email;
using AsaGoldBff.Model.Result;
using AsaGoldBff.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsaGoldBff.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1")]
    public class ApiController : ControllerBase
    {
        private readonly EmailValidationUseCase emailValidationUseCase;
        private readonly AccountUseCase accountUseCase;
        public ApiController(
            EmailValidationUseCase emailValidationUseCase,
            AccountUseCase accountUseCase
            )
        {
            this.emailValidationUseCase = emailValidationUseCase;
            this.accountUseCase = accountUseCase;
        }

        /// <summary>
        /// User's account
        /// </summary>
        /// <param name="emailVerificationGuid"></param>
        /// <returns></returns>
        [HttpGet("account")]
        public Task<AsaGoldRepository.Account?> GetAccount()
        {
            return accountUseCase.GetAccount(new Model.Auth.UserWithHeader(User, Request));
        }

        /// <summary>
        /// Email verification
        /// </summary>
        /// <param name="email"></param>
        /// <param name="terms">Terms and conditions version</param>
        /// <param name="gdpr">GDPR policy version</param>
        /// <param name="marketingConsent"></param>
        /// <returns></returns>
        [HttpPost("send-verification-email")]
        public async Task<bool> SendVerificationEmail([FromForm] string email, [FromForm] string terms, [FromForm] string gdpr, [FromForm] bool marketingConsent)
        {
            return await emailValidationUseCase.SendVerificationEmail(email, terms, gdpr, marketingConsent, new Model.Auth.UserWithHeader(User, Request));
        }
        /// <summary>
        /// Email verification
        /// </summary>
        /// <param name="emailVerificationGuid"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public async Task<SuccessWithTransaction> VerifyEmail([FromBody] Guid emailVerificationGuid)
        {
            return await emailValidationUseCase.VerifyEmail(emailVerificationGuid.ToString(), new Model.Auth.UserWithHeader(User, Request));
        }

    }
}