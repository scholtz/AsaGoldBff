namespace AsaGoldBff.Model.Result
{
    public class SuccessWithTransaction
    {
        public bool Success { get; set; } = true;
        public string? TransactionId { get; set; } = null;
        public string? Error { get; set; } = null;
    }
}
