using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/resumes")]
public class ResumesTableController : UserOwnedTableController<SyncResume>
{
    public ResumesTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
