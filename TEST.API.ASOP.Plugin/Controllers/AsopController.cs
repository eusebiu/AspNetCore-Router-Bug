using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEST.API.ASOP.Plugin.Interfaces;

namespace TEST.API.ASOP.Plugin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
#if !DISABLE_AUTHENTICATION
    //[Authorize(Roles = "admin,user")]
    [AllowAnonymous]
#endif
    public class AsopController : ControllerBase
    {
        readonly IMapper _mapper;
        readonly IAsopService _asopService;

        public AsopController(IMapper mapper, IAsopService asopService)
        {
            _mapper = mapper;
            _asopService = asopService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(200, new AsopModel { Result = "OK" });
        }
    }
}