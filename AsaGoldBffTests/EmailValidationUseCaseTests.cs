using Algorand.Algod.Model.Transactions;
using Algorand;
using AsaGoldBff.Controllers.Email;
using AsaGoldBff.Model.Config;
using AsaGoldBff.UseCase;
using Microsoft.Extensions.Options;
using Moq;
using Algorand.Algod;
using Algorand.Utils;

namespace AsaGoldBffTests
{
    public class EmailValidationUseCaseTests
    {
        private EmailValidationUseCase _validationUseCase;
        private AsaGoldBff.Model.Auth.UserWithHeader user;
        [SetUp]
        public void Setup()
        {
            var emailSender = new NoEmailSender();
            BFFOptions au = new BFFOptions()
            {
                URL = "https://www.asa.gold",
                RepositoryUrl = "https://localhost:44333"
            };
            var monitor = Mock.Of<IOptionsMonitor<BFFOptions>>(_ => _.CurrentValue == au);
            _validationUseCase = new EmailValidationUseCase(emailSender, monitor);
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
            var ret = await _validationUseCase.SendVerificationEmail("test@test.com", "1", true, user);
            Assert.That(ret, Is.True);
        }
    }
}