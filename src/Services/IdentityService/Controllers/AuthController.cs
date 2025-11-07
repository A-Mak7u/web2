using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Api.Models;
using IdentityService.Api.Tracing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly JwtOptions _jwtOptions;
    private readonly TraceStore _trace;

    public AuthController(UserManager<IdentityUser> userManager, IOptions<JwtOptions> jwtOptions, TraceStore trace)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
        _trace = trace;
    }

    [HttpPost("register")]
    // простая регистрация через ASP.NET Core Identity
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var traceId = HttpContext.Request.Headers["X-Trace-Id"].ToString();
        _trace.Add(traceId, "Auth:Register: request accepted");
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _trace.Add(traceId, "Auth:Register: failed");
            return BadRequest(result.Errors.Select(e => e.Description));
        }
        _trace.Add(traceId, "Auth:Register: success");
        return Ok();
    }

    [HttpPost("login")]
    // выдаём JWT с базовым набором клеймов (sub/email/nameid)
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var traceId = HttpContext.Request.Headers["X-Trace-Id"].ToString();
        _trace.Add(traceId, "Auth:Login: request accepted");
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _trace.Add(traceId, "Auth:Login: user not found");
            return Unauthorized();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            _trace.Add(traceId, "Auth:Login: wrong password");
            return Unauthorized();
        }

        // составляем клеймы пользователя для токена
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // подпись токена на симметричном ключе из конфигурации
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        _trace.Add(traceId, "Auth:Login: token issued");
        return Ok(new AuthResponse(accessToken));
    }
}

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}


