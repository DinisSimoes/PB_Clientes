namespace PB_Clientes.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string MessageType { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime OccurredUtc { get; set; }
        public string Status { get; set; } = "Pending";
        public int Attempts { get; set; }
        public string? LastError { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? TraceId { get; set; }
        public string? SpanId { get; set; }
        public string? Baggage { get; set; }
    }
}
