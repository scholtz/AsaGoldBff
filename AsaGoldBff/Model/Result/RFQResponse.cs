namespace AsaGoldBff.Model.Result
{
    public class RFQResponse
    {
        public string RfqId { get; set; }
        public decimal Quote { get; set; }
        public string Iban { get; set; }
        public string Bic { get; set; }
        public string ClientId { get; set; }
    }
}
