using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using UserManagementApi.Dtos;
using UserManagementApi.Helpers;
using UserManagementApi.Interfaces;
using UserManagementApi.Models;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers
{
    [Authorize] // Tüm controller korumalı
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly TokenService _tokenService;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, TokenService tokenService, IMapper mapper)
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        // DTO ile kullanıcıları listele
        [HttpGet("dtos")]
        public IActionResult GetUserDtos()
        {
            var users = _userService.GetAllUsers();
            var userDtos = _mapper.Map<List<UserDto>>(users);
            return Ok(userDtos);
        }

        // Tüm kullanıcıları getir
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return Ok(users);
        }

        // Kullanıcıları tarihe göre sırala
        [HttpGet("orderbydate")]
        public IActionResult GetAllUsersOrderByDate()
        {
            var users = _userService.GetAllUsersOrderByDate();
            return Ok(users);
        }

        // ID'ye göre kullanıcı getir
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found or inactive." });

            return Ok(user);
        }

        // Token gerektiren genel test
        [HttpGet("secret")]
        public IActionResult SecretData()
        {
            return Ok("🎉 Bu veriyi sadece token sahibi kullanıcılar görebilir.");
        }

        // ROL: Admin olanlar görebilir
        [Authorize(Roles = "User")]
        [HttpGet("user-only")]
        public IActionResult UserOnlyData()
        {
            return Ok("🔐 Bu veriye sadece User rolündekiler erişebilir!");
        }

        // ROL: Admin olanlar görebilir
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyData()
        {
            return Ok("🔐 Bu veriye sadece Admin rolündekiler erişebilir!");
        }

        // ROL: Manager olanlar görebilir
        [Authorize(Roles = "Manager")]
        [HttpGet("manager-only")]
        public IActionResult ManagerOnlyData()
        {
            return Ok("📁 Manager rolüne özel veri.");
        }

        // ROL: Admin VEYA Manager olanlar görebilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("admin-or-manager")]
        public IActionResult AdminOrManagerData()
        {
            return Ok("📂 Admin veya Manager rolündekilere açık.");
        }

        // Token'ı çözümle ve bilgileri göster
        [Authorize]
        [HttpGet("decode-token")]
        public IActionResult DecodeToken()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
                return Unauthorized("Kimlik doğrulaması başarısız.");

            var claims = identity.Claims;

            var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var expClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (expClaim == null)
                return BadRequest("Token geçerlilik süresi bulunamadı.");

            var expUnixTime = long.Parse(expClaim);
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
            var now = DateTime.UtcNow;
            var timeLeft = expirationTime - now;

            var tokenInfo = new TokenInfoDto
            {
                UserId = userId,
                Email = email,
                Username = username,
                Role = role,
                ExpirationTime = expirationTime,
                TimeLeft = timeLeft
            };

            return Ok(tokenInfo);
        }


        // Yeni kullanıcı ekle
        [AllowAnonymous]
        [HttpPost]
        public IActionResult AddUser(User user)
        {
            _userService.AddNewUser(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        // Kullanıcı güncelle
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, User user)
        {
            if (id != user.Id)
                return BadRequest(new { message = "Id mismatch" });

            var existingUser = _userService.GetUserById(id);
            if (existingUser == null)
                return NotFound(new { message = "User not found or inactive" });

            _userService.UpdateUser(user);
            return NoContent();
        }

        // Kullanıcı sil (hard delete)
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            _userService.DeleteUserById(id);
            return NoContent();
        }

        // Kullanıcıyı pasif hale getirme (soft delete)
        [HttpPatch("softdelete/{id}")]
        public IActionResult SoftDeleteUserById(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { Message = "User not found or inactive." });

            _userService.SoftDeleteUserById(id);
            return Ok(new { Message = "User has been soft-deleted (IsActive = false)." });
        }

        [HttpGet("with-roles-sp")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsersWithRolesViaSP()
        {
            var usersWithRoles = _userService.GetUsersWithRolesFromSP();
            return Ok(usersWithRoles);
        }


        // Giriş
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                var loginSuccess = _userService.Login(request.Email, request.Password);
                if (!loginSuccess)
                    return Unauthorized(new { message = "Geçersiz email veya şifre." });

                var user = _userService.GetUserByEmail(request.Email);
                var token = _tokenService.CreateToken(user);

                return Ok(new
                {
                    message = "Giriş başarılı",
                    token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Çıkış
        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            var user = _userService.GetUserByEmail(request.Email);
            if (user == null || !user.IsLoggedIn)
                return BadRequest(new { message = "Kullanıcı zaten çıkış yapmış veya bulunamadı." });

            user.IsLoggedIn = false;
            _userService.UpdateUser(user);
            return Ok(new { message = "Çıkış başarılı." });
        }
    }
}
