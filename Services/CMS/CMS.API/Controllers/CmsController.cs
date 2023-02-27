using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CMS.API.Controllers
{
    [Route("api/v1/[controller]")]
    //[Authorize]
    [ApiController]
    public class CMSController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public IActionResult GetTestNumber()
        {
            var number =  777;
            return Ok(number);
        }
    }
}
