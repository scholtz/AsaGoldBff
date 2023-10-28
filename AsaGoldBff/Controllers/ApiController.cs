using AsaGoldBff.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsaGoldBff.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ApiController : ControllerBase
    {
        private readonly EmailValidationUseCase emailValidationUseCase;
        public ApiController(
            EmailValidationUseCase emailValidationUseCase
            )
        {
            this.emailValidationUseCase = emailValidationUseCase;
        }
        /// <summary>
        /// Email verification
        /// </summary>
        /// <param name="email"></param>
        /// <param name="consent"></param>
        /// <param name="marketingConsent"></param>
        /// <returns></returns>
        [Authorize]
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
        [Authorize]
        [HttpPost("verify-email")]
        public async Task<bool> VerifyEmail([FromBody] Guid emailVerificationGuid)
        {
            return false;
        }
    }
}