using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserManagementApi.Models;

namespace UserManagementApi.Services
{
    public class TokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expireMinutes;

        public TokenService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSettings:Key"];
            _issuer = configuration["JwtSettings:Issuer"];
            _audience = configuration["JwtSettings:Audience"];
            _expireMinutes = int.Parse(configuration["JwtSettings:ExpireMinutes"]);

            if (string.IsNullOrEmpty(_secretKey))
                throw new Exception("JwtSettings:Key appsettings içinde bulunamadı!");
            if (string.IsNullOrEmpty(_issuer))
                throw new Exception("JwtSettings:Issuer appsettings içinde bulunamadı!");
            if (string.IsNullOrEmpty(_audience))
                throw new Exception("JwtSettings:Audience appsettings içinde bulunamadı!");
        }

        public string CreateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}   