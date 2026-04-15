using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/projects")]
public class ProjectsTableController : UserOwnedTableController<SyncProject>
{
    public ProjectsTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
