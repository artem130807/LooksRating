using Grpc.Core;

namespace LooksRatingApi.Infrastructure.RateLimiting
{
    internal static class RateLimitGrpcPeerAddress
    {
        public static string? TryResolve(ServerCallContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.Peer))
            {
                return ParseHost(context.Peer);
            }

            return context.Host;
        }

        private static string? ParseHost(string peer)
        {
            if (peer.StartsWith("ipv4:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = peer.Split(':');
                return parts.Length >= 2 ? parts[1] : null;
            }

            if (peer.StartsWith("ipv6:", StringComparison.OrdinalIgnoreCase))
            {
                var bracketEnd = peer.IndexOf(']');
                if (bracketEnd > 0)
                {
                    return peer[..(bracketEnd + 1)];
                }
            }

            return peer;
        }
    }
}
