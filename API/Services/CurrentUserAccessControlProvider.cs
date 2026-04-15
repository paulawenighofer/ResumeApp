using API.Models.Sync;
using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Security.Claims;

namespace API.Services;

public class CurrentUserAccessControlProvider<TEntity> : IAccessControlProvider<TEntity>
    where TEntity : class, ITableData, IUserOwnedSyncEntity
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserAccessControlProvider(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private string? UserId => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public Expression<Func<TEntity, bool>> GetDataView()
        => string.IsNullOrWhiteSpace(UserId)
            ? _ => false
            : entity => entity.UserId == UserId;

    public ValueTask<bool> IsAuthorizedAsync(TableOperation operation, TEntity? entity, CancellationToken cancellationToken = default)
    {
        if (operation == TableOperation.Create || operation == TableOperation.Query)
        {
            return ValueTask.FromResult(!string.IsNullOrWhiteSpace(UserId));
        }

        return ValueTask.FromResult(entity is not null && !string.IsNullOrWhiteSpace(UserId) && entity.UserId == UserId);
    }

    public ValueTask PreCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UserId = UserId ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    public ValueTask PostCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
