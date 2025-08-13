namespace Entities.Dtos
{
    public class CancelSubscriptionDto
    {
        public int UserSubscriptionId { get; set; }
        public string CancellationReason { get; set; }
        public bool ImmediateCancellation { get; set; }
    }
}