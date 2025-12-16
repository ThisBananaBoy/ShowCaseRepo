using Microsoft.AspNetCore.Identity;
using ProjectME_BE.Domain.Common;

namespace ProjectME_BE.Domain.Model.User;

public class UserAggregate : BaseEntity
{
    public Guid AuthUserId { get; private set; }

    private UserAggregate() { }

    public UserAggregate(Guid authUserId)
    {
        AuthUserId = authUserId;
    }
}
