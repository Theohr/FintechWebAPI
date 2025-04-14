using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FintechSampleProject.Helpers
{
    public class GenerateAdminToken
    {

        // Helper method to generate an admin token
        public static string GenerateToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-here-1234567890ab"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: "TradeMonitorIssuer",
                audience: "TradeMonitorAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
