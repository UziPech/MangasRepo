using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MangaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("token")]
        public IActionResult GetToken()
        {
            var claveSecreta = _config["Jwt:Key"]
                ?? throw new Exception("⚠️ No se encontró la clave secreta JWT en appsettings.json");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new[] { new Claim("access", "api") },
                expires: DateTime.UtcNow.AddMinutes(5), // 🔐 Expira en 5 minutos
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
    }
}


