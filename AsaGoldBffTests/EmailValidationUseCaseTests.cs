using Algorand.Algod.Model.Transactions;
using Algorand;
using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Config;
using AsaGoldBff.UseCase;
using Microsoft.Extensions.Options;
using Moq;
using Algorand.Algod;
using Algorand.Utils;
using Castle.Core.Smtp;
using AsaGoldBff.Model.Email;
using AlgorandAuthentication;

namespace AsaGoldBffTests
{
    public class EmailValidationUseCaseTests
    {
        private EmailValidationUseCase _validationUseCase;
        private AsaGoldBff.Model.Auth.UserWithHeader user;
        private NoEmailSender emailSender;
        [SetUp]
        public void Setup()
        {
            emailSender = new NoEmailSender();

            BFFOptions bffOptions = new BFFOptions()
            {
                URL = "https://www.asa.gold",
                RepositoryUrl = "https://localhost:44333",
                AirdropAlgoOnEmailVerification = 100000,
                Account = "PublicTestAccount"
            };
            var bffOptionsMock = Mock.Of<IOptionsMonitor<BFFOptions>>(_ => _.CurrentValue == bffOptions);

            AlgorandAuthenticationOptions algorandAuthenticationOptions = new AlgorandAuthenticationOptions()
            {
                AlgodServer = "https://testnet-api.algonode.cloud"
            };
            var algorandAuthenticationOptionsMock = Mock.Of<IOptionsMonitor<AlgorandAuthenticationOptions>>(_ => _.CurrentValue == algorandAuthenticationOptions);

            _validationUseCase = new EmailValidationUseCase(emailSender, bffOptionsMock, algorandAuthenticationOptionsMock);
            var account = AlgorandARC76AccountDotNet.ARC76.GetAccount("AccountForTests");

            var httpClient = HttpClientConfigurator.ConfigureHttpClient("https://testnet-api.algonode.cloud", "");
            DefaultApi algodApiInstance = new DefaultApi(httpClient);
            var transParams = algodApiInstance.TransactionParamsAsync().Result;
            var tx = PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account.Address, account.Address, 0, "ASA.Gold#ARC14", transParams);
            var signed = tx.Sign(account);
            var signedBytes = Algorand.Utils.Encoder.EncodeToMsgPackOrdered(signed);

            user = new AsaGoldBff.Model.Auth.UserWithHeader()
            {
                Name = account.Address.EncodeAsString(),
                Header = "SigTx " + Convert.ToBase64String(signedBytes)
            };
        }

        [Test]
        public async Task SendVerificationEmail()
        {
            EmailValidationUseCase.ValidateTime = false;
            emailSender.Data.Clear();
            var ret = await _validationUseCase.SendVerificationEmail("test@test.com", "1", "1", true, user);
            Assert.That(ret, Is.True);
            Assert.That(emailSender.Data.Count, Is.EqualTo(1));
        }
        [Test]
        public async Task VerifyEmail()
        {
            EmailValidationUseCase.ValidateTime = false;
            emailSender.Data.Clear();
            var rand = (new Random()).Next(1000000, 9999999);
            var ret = await _validationUseCase.SendVerificationEmail($"{rand}@test.com", "1", "1", true, user);
            Assert.That(ret, Is.True);

            Assert.That(emailSender.Data.Count, Is.EqualTo(1));
            var email = emailSender.Data.First();
            var validationEmail = email.Value.data as EmailValidationEmail;
            Assert.That(validationEmail, Is.Not.Null);
            var retVerify = await _validationUseCase.VerifyEmail(validationEmail.Code, user);
            Assert.That(retVerify.Success, Is.True);

        }
    }
}