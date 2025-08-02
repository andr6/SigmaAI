using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sigma.Core.Repositories;
using Sigma.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sigma.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IApps_Repositories _appsRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantController(ApplicationDbContext db, IApps_Repositories appsRepository, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _appsRepository = appsRepository;
            _userManager = userManager;
        }

        private async Task SetTenantAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _db.TenantId = user.TenantId;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ApiKeys()
        {
            await SetTenantAsync();
            var apps = await _appsRepository.GetListAsync();
            var result = apps.Select(a => new { a.Id, a.Name, a.SecretKey });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> RotateApiKey(string appId)
        {
            await SetTenantAsync();
            var app = await _appsRepository.GetFirstAsync(x => x.Id == appId);
            if (app == null)
            {
                return NotFound();
            }

            app.SecretKey = Guid.NewGuid().ToString("N");
            _appsRepository.Update(app);
            return Ok(new { app.Id, app.SecretKey });
        }
    }
}
