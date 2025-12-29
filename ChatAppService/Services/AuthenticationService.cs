
using ChatAppAPI.Jwt;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UserRepository.Model.Request;
using UserRepository.Model.Response;
using UserRepository.Models;
using UserRepository.Repositories;
using UserService.Repositories;

namespace UserService.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationService(IAuthenticationRepository repository, IUserRepository userRepository, IOptions<JwtSettings> jwtOptions)
        {
            _repository = repository;
            _userRepository = userRepository;
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateTokenAsync(User user, string role)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", user.Id.ToString()),
            new Claim("displayName", user.DisplayName ?? string.Empty),
            new Claim("avatarUrl", user.AvatarUrl ?? string.Empty),
            new Claim("role",role ?? string.Empty)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> LoginAsync(LoginUserRequest request, string adminEmail)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
                throw new InvalidOperationException("Email doesn't exist!");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid password!");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is inactive.");

            var role = user.Email == adminEmail ? "Admin" : "User";
            var token = GenerateTokenAsync(user, role);

            return new AuthResponse
            {
                Token = token,
            };
        }
    }
}
