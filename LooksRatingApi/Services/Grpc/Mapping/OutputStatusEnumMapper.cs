using DomainOutputStatusEnum = LooksRatingApi.Enums.OutputStatusEnum;
using GrpcOutputStatusEnum = LooksRatingGrpc.OutputStatusEnum;

namespace LooksRatingApi.Services.Grpc.Mapping;

internal static class OutputStatusEnumMapper
{
    public static bool TryToDomain(GrpcOutputStatusEnum grpcStatus, out DomainOutputStatusEnum domainStatus)
    {
        domainStatus = grpcStatus switch
        {
            GrpcOutputStatusEnum.Pending => DomainOutputStatusEnum.Pending,
            GrpcOutputStatusEnum.Cancelled => DomainOutputStatusEnum.Cancelled,
            GrpcOutputStatusEnum.Confirmed => DomainOutputStatusEnum.Confirmed,
            GrpcOutputStatusEnum.Failed => DomainOutputStatusEnum.Failed,
            _ => default,
        };

        return grpcStatus is GrpcOutputStatusEnum.Pending
            or GrpcOutputStatusEnum.Cancelled
            or GrpcOutputStatusEnum.Confirmed
            or GrpcOutputStatusEnum.Failed;
    }

    public static GrpcOutputStatusEnum ToGrpc(DomainOutputStatusEnum domainStatus) =>
        domainStatus switch
        {
            DomainOutputStatusEnum.Pending => GrpcOutputStatusEnum.Pending,
            DomainOutputStatusEnum.Cancelled => GrpcOutputStatusEnum.Cancelled,
            DomainOutputStatusEnum.Confirmed => GrpcOutputStatusEnum.Confirmed,
            DomainOutputStatusEnum.Failed => GrpcOutputStatusEnum.Failed,
            _ => GrpcOutputStatusEnum.Unspecified,
        };
}
