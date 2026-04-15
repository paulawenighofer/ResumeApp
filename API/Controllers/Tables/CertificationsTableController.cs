using API.Data;
using API.Models.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tables;

[Route("tables/certifications")]
public class CertificationsTableController : UserOwnedTableController<SyncCertification>
{
    public CertificationsTableController(AppDbContext context, IHttpContextAccessor accessor) : base(context, accessor) { }
}
