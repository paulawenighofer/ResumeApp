using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/educations")]
public class EducationsTableController : UserOwnedTableController<SyncEducation>
{
    public EducationsTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
