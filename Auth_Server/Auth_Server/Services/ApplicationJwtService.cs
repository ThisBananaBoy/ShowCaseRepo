using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth_Server.Services;

public class ApplicationJwtService(IConfiguration configuration) : IApplicationJwtService
{
    public string GenerateApplicationToken(string applicationId, string applicationName)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, applicationId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("application_name", applicationName),
            new("token_type", "application"),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Application:Key"]!)
        );
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresNever = configuration.GetValue<bool>("Jwt:Application:ExpiresNever");

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Application:Issuer"],
            audience: configuration["Jwt:Application:Audience"],
            claims: claims,
            expires: expiresNever ? null : DateTime.UtcNow.AddYears(100),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

