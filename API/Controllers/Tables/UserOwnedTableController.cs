using API.Data;
using API.Models.Sync;
using API.Services;
using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Authorize]
public abstract class UserOwnedTableController<TEntity> : TableController<TEntity>
    where TEntity : BaseEntityTableData, IUserOwnedSyncEntity
{
    protected UserOwnedTableController(AppDbContext context, IHttpContextAccessor accessor) : base()
    {
        Repository = new EntityTableRepository<TEntity>(context);
        AccessControlProvider = new CurrentUserAccessControlProvider<TEntity>(accessor);
        Options = new TableControllerOptions
        {
            EnableSoftDelete = true
        };
    }
}
