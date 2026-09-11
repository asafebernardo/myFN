using MyFn.Application.Abstractions;
using MyFn.Domain.Common;

namespace MyFn.Infrastructure;

public sealed class SingleUserContext : ICurrentUser
{
    public Guid UserId => WellKnownIds.DefaultUserId;
}
