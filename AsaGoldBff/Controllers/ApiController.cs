using AsaGoldBff.Model.Email;
using AsaGoldBff.Model.Result;
using AsaGoldBff.UseCase;
using AsaGoldRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsaGoldBff.Controllers
{
    
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        [HttpPost("update-profile")]
        public async Task<string?> UpdateProfile([FromBody] AsaGoldRepository.KYCRequest request)
        {
            return await accountUseCase.UpdateProfile(request, new Model.Auth.UserWithHeader(User, Request));
        }

        /// <summary>
        /// Return user's profile to user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("profile")]
        public Task<KYCRequest?> GetProfile()
        {
            return accountUseCase.GetProfile(new Model.Auth.UserWithHeader(User, Request));
        }


        /// <summary>
        /// Create RFQ for user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("rfq")]
        public Task<RFQ?> RFQ([FromQuery] decimal amount, [FromQuery] string currency)
        {
            return null;
        }

        /// <summary>
        /// Confirm RFQ by user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPut("rfq/{id}")]
        public Task<RFQ?> ConfirmRFQ([FromRoute] string id)
        {
            return null;
        }

        /// <summary>
        /// List of mainnet accounts managed by GoldDAO
        /// </summary>
        /// <returns></returns>
        [HttpGet("dao-managed-accounts")]
        public string[] DAOAccounts()
        {
            return new string[]
            {
                "GOPQRISPHSWZBLHM2K67RJNSELDQEDNCL4DS3FTICW2Q2UM3KZWEME7BPE",
                "AC5MB73BJFJKCVDOVJWG6QSUVTGMPL2UUYHGXEG6CQWI5XQ4OAHVZQ4Z6U",
                "N43YZHJB4LDJQZJH3RIOQP5WVPJQWWX5ROEUZRNZCRON64XMNXPJG3PLJU",
                "GNBZ4YN24HLCKFDVAVKEO5SZYZME3ERXHY5ORDCE6VD4QKT372OXDUOTHU",
                "UDEXJH6KEWKQEZUVR24XCJIXPOFGOWDF2LI2XTBIZFK4IFCSZJTXQ2DTFU",
                "OAOYIJWXVC44DBCX5GKCDZVDGGHQJFAKHLTYDCT5DKJPQL7TNWLUFH5KOM"
            };
        }
        
    }
}