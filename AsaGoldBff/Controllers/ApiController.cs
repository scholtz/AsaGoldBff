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
        /// <param name="consent"></param>
        /// <param name="marketingConsent"></param>
        /// <returns></returns>
        [HttpPost("send-verification-email")]
        public async Task<bool> SendVerificationEmail([FromForm] string email, [FromForm] string consent, [FromForm] bool marketingConsent)
        {
            return await emailValidationUseCase.SendVerificationEmail(email, consent, marketingConsent, new Model.Auth.UserWithHeader(User, Request));
        }
        /// <summary>
        /// Email verification
        /// </summary>
        /// <param name="emailVerificationGuid"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public async Task<bool> VerifyEmail([FromBody] Guid emailVerificationGuid)
        {
            return false;
        }

    }
}