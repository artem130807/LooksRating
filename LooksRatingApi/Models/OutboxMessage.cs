using System.Text.Json;

namespace LooksRatingApi.Models
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string MessageType { get; private set; } = string.Empty;
        public string PayloadJson { get; private set; } = "{}";
        public string StateJson { get; private set; } = "{}";

        public OutboxMessageStatus Status { get; private set; }
        public int Attempts { get; private set; }
        public string? LastError { get; private set; }
        public DateTime? NextAttemptAt { get; private set; }
        public DateTime? ProcessingStartedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private OutboxMessage()
        {
        }

        public static OutboxMessage Create<TPayload, TState>(
            string messageType,
            TPayload payload,
            TState state)
        {
            var now = DateTime.UtcNow;
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = messageType,
                PayloadJson = JsonSerializer.Serialize(payload),
                StateJson = JsonSerializer.Serialize(state),
                Status = OutboxMessageStatus.Pending,
                Attempts = 0,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public bool TryReadPayload<TPayload>(out TPayload? payload)
        {
            try
            {
                payload = JsonSerializer.Deserialize<TPayload>(PayloadJson);
                return payload is not null;
            }
            catch
            {
                payload = default;
                return false;
            }
        }

        public bool TryReadState<TState>(out TState? state)
        {
            try
            {
                state = JsonSerializer.Deserialize<TState>(StateJson);
                return state is not null;
            }
            catch
            {
                state = default;
                return false;
            }
        }

        public void SetState<TState>(TState state, DateTime nowUtc)
        {
            StateJson = JsonSerializer.Serialize(state);
            UpdatedAt = nowUtc;
            LastError = null;
        }

        public void MarkFailed(string error, DateTime nowUtc, TimeSpan retryDelay)
        {
            Status = OutboxMessageStatus.Failed;
            LastError = error;
            NextAttemptAt = nowUtc.Add(retryDelay);
            ProcessingStartedAt = null;
            UpdatedAt = nowUtc;
        }

        public void MarkCompleted(DateTime nowUtc)
        {
            Status = OutboxMessageStatus.Completed;
            LastError = null;
            NextAttemptAt = null;
            ProcessingStartedAt = null;
            UpdatedAt = nowUtc;
        }
    }
}
