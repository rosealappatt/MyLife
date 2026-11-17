using Microsoft.AspNetCore.Mvc;

namespace MyLife.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        // GET: api/home
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Welcome to MyLife API"
            });
        }

        // GET: api/home/checkbackendconnection
        [HttpGet("checkbackendconnection")]
        public IActionResult ConnectedToApi()
        {
            return Ok(new
            {
                title = "Personal Life Management Platform",
                message = "Connected to MyLife API"
            });
        }

        [HttpGet("test-exception")]
        public IActionResult ThrowException()
        {
            throw new Exception("This is a test exception.");
        }

        [HttpGet("cancel-test")]
        public async Task<IActionResult> TestCancellationAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return Ok(new
            {
                message = "Operation Cancelled successfully"
            });
        }
    }
}
