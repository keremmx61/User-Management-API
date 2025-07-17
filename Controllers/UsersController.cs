using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using UserManagementApi.Helpers;
using UserManagementApi.Interfaces;
using UserManagementApi.Models;
using UserManagementApi.Services; // TokenService için namespace ekle

namespace UserManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly TokenService _tokenService;

        public UsersController(IUserService userService, TokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        // GET: api/users
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return Ok(users);
        }

        // GET: api/users/orderbydate
        [HttpGet("orderbydate")]
        public IActionResult GetAllUsersOrderByDate()
        {
            var users = _userService.GetAllUsersOrderByDate();
            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
                return NotFound(new { message = "User not found or inactive." });

            return Ok(user);
        }

        // GET: api/users/secret
        [Authorize]
        [HttpGet("secret")]
        public IActionResult SecretData()
        {
            return Ok("🎉 Bu veriyi sadece token sahibi kullanıcılar görebilir.");
        }


        // POST: api/users
        [HttpPost]
        public IActionResult AddUser(User user)
        {
            _userService.AddNewUser(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        // PUT: api/users/5
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

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            _userService.DeleteUserById(id);
            return NoContent();
        }

        // PATCH: api/users/softdelete/5
        [HttpPatch("softdelete/{id}")]
        public IActionResult SoftDeleteUserById(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { Message = "User not found or inactive." });

            _userService.SoftDeleteUserById(id);
            return Ok(new { Message = "User has been soft-deleted (IsActive = false)." });
        }

        // POST: api/users/login
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
