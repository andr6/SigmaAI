using Microsoft.AspNetCore.Mvc;
using Sigma.Data;
using System.Linq;

namespace Sigma.Controllers
{
    [ApiController]
    [Route("api/mitre")]
    public class MitreController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MitreController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var ids = _db.MitreMappings.Select(m => m.TechniqueId).ToList();
            return Ok(ids);
        }
    }
}
