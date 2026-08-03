using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _userService.GetAllUsersAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }
    }
}
