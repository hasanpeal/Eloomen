using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Interfaces;
using server.Models;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDBContext _db;
        private readonly ICloudflareR2Service? _r2Service;

        public HealthController(ApplicationDBContext db, ICloudflareR2Service? r2Service = null)
        {
            _db = db;
            _r2Service = r2Service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = new Dictionary<string, object>();
            
            // Test database connection
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                result["database"] = new { type = "postgres", connected = canConnect };
            }
            catch (Exception ex)
            {
                result["database"] = new { type = "postgres", connected = false, error = ex.Message };
            }

            // Test R2 connection
            if (_r2Service != null)
            {
                try
                {
                    var r2Connected = await _r2Service.TestConnectionAsync();
                    result["r2"] = new { type = "cloudflare-r2", connected = r2Connected };
                }
                catch (Exception ex)
                {
                    result["r2"] = new { type = "cloudflare-r2", connected = false, error = ex.Message };
                }
            }
            else
            {
                result["r2"] = new { type = "cloudflare-r2", connected = false, error = "R2 service not configured" };
            }

            return Ok(result);
        }
    }
}