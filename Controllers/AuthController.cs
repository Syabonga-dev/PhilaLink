using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpVerificationService _otpService;

        public AuthController(IAuthService authService, IOtpVerificationService otpService)
        {
            _authService = authService;
            _otpService = otpService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("otp/generate")]
        public async Task<IActionResult> GenerateOtp(Guid userId)
        {
            var otp = await _otpService.GenerateAsync(userId);
            return Ok(otp);
        }

        [HttpPost("otp/verify")]
        public async Task<IActionResult> VerifyOtp(Guid userId, string code)
        {
            var result = await _otpService.VerifyAsync(userId, code);
            return Ok(result);
        }
    }
}

