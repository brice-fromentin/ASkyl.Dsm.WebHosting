using Microsoft.AspNetCore.Mvc;

namespace Askyl.Dsm.WebHosting.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelloWorldController(ILogger<HelloWorldController> logger) : ControllerBase
    {
        [HttpGet("hello")]
        public IResult GetMessage()
        {
            logger.LogInformation("Hello endpoint called");
            return Results.Ok("yooooo");
        }
    }
}
