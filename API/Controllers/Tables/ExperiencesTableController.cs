using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/experiences")]
public class ExperiencesTableController : UserOwnedTableController<SyncExperience>
{
    public ExperiencesTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
