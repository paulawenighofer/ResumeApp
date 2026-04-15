using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/skills")]
public class SkillsTableController : UserOwnedTableController<SyncSkill>
{
    public SkillsTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
