using AsaGoldBff.Model.Email;
using AsaGoldBff.Model.Result;
using AsaGoldBff.UseCase;
using AsaGoldRepository;
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
        /// <param name="code">Email verification code guid</param>
        /// <returns></returns>
        [HttpPost("verify-code-from-email")]
        public async Task<SuccessWithTransaction> VerifyCodeFromEmail([FromForm] Guid code)
        {
            return await emailValidationUseCase.VerifyEmail(code.ToString(), new Model.Auth.UserWithHeader(User, Request));
        }

        /// <summary>
        /// User can update his KYC form. In this case we create new update request and put the requestid to his account.
        /// </summary>
        /// <param name="request">User profile change request</param>
        /// <returns></returns>
        [HttpPost("update-profile")]
        public async Task<string?> UpdateProfile([FromBody] AsaGoldRepository.KYCRequest request)
        {
            return await accountUseCase.UpdateProfile(request, new Model.Auth.UserWithHeader(User, Request));
        }

        /// <summary>
        /// Return user's profile to user
        /// </summary>
        /// <returns></returns>
        [HttpGet("profile")]
        public Task<KYCRequest?> GetProfile()
        {
            return accountUseCase.GetProfile(new Model.Auth.UserWithHeader(User, Request));
        }
    }
}