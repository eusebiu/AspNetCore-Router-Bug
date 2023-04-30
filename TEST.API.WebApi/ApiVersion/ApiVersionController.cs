using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TEST.API.WebApi.ApiVersion
{
    [Route("api/version")]
    [ApiController]
    public class ApiVersionController : ControllerBase
    {
        /// <summary>
        /// Returns the current version of this Web API
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [AllowAnonymous]
        public IActionResult GetVersion()
        {
            var version = typeof(ApiVersionController).Assembly.GetName().Version;
            return Ok(version.ToString());
        }
        /// <summary>
        /// Returns the current version of this Web API
        /// </summary>
        [HttpGet("test")]
        [ProducesResponseType(typeof(string), 200)]
        [Authorize]
        public IActionResult GetVersion2()
        {
            var version = typeof(ApiVersionController).Assembly.GetName().Version;
            return Ok(version.ToString());
        }
    }
}