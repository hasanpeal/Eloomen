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
        private readonly IS3Service? _s3Service;

        public HealthController(ApplicationDBContext db, IS3Service? s3Service = null)
        {
            _db = db;
            _s3Service = s3Service;
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

            // Test S3 connection
            if (_s3Service != null)
            {
                try
                {
                    var s3Connected = await _s3Service.TestConnectionAsync();
                    result["s3"] = new { type = "s3-bucket", connected = s3Connected };
                }
                catch (Exception ex)
                {
                    result["s3"] = new { type = "s3-bucket", connected = false, error = ex.Message };
                }
            }
            else
            {
                result["s3"] = new { type = "s3-bucket", connected = false, error = "S3 service not configured" };
            }

            return Ok(result);
        }
    }
}