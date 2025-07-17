using UserManagementApi.Helpers;
using UserManagementApi.Data;
using UserManagementApi.Interfaces;
using UserManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace UserManagementApi.Implements
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;
        private static readonly object _lock = new object();

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<User> GetAllUsers()
        {
            return _context.Users.Where(u => u.IsActive).ToList();
        }

        public User GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id && u.IsActive);
        }

        public User GetUserByEmail(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            _logger.LogInformation($"GetUserByEmail: Email={email}, UserFound={user != null}, IsLoggedIn={user?.IsLoggedIn}");
            return user;
        }

        public List<User> GetAllUsersOrderByDate()
        {
            return _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.InsertDate)
                .ToList();
        }

        public void AddNewUser(User user)
        {
            user.Password = PasswordHasher.Hash(user.Password);
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            var existingUser = _context.Users.Find(user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.IsLoggedIn = user.IsLoggedIn;
                _context.SaveChanges();
                _logger.LogInformation($"UpdateUser: Id={user.Id}, IsLoggedIn={user.IsLoggedIn}");
            }
        }

        public void DeleteUserById(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        public void SoftDeleteUserById(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = false;
                _context.SaveChanges();
            }
        }

        public bool Login(string email, string password)
        {
            lock (_lock)
            {
                try
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        _logger.LogInformation($"Login: Başlangıç. Email={email}");

                        // Kullanıcıyı kilitleyip sorgula
                        var user = _context.Users
                            .FromSqlRaw("SELECT * FROM Users WITH (UPDLOCK) WHERE Email = {0} AND IsActive = 1", email)
                            .FirstOrDefault();

                        if (user == null)
                        {
                            _logger.LogWarning($"Login: Kullanıcı bulunamadı. Email={email}");
                            return false;
                        }

                        if (!PasswordHasher.Verify(password, user.Password))
                        {
                            _logger.LogWarning($"Login: Şifre yanlış. Email={email}");
                            return false;
                        }

                        _logger.LogInformation($"Login: Kullanıcı bulundu. Email={email}, IsLoggedIn={user.IsLoggedIn}");

                        if (user.IsLoggedIn)
                        {
                            _logger.LogWarning($"Login: Kullanıcı zaten giriş yapmış. Email={email}, IsLoggedIn={user.IsLoggedIn}");
                            throw new Exception("Bu kullanıcı zaten giriş yapmış.");
                        }

                        user.IsLoggedIn = true;
                        _context.Entry(user).State = EntityState.Modified;
                        _context.SaveChanges();
                        _logger.LogInformation($"Login: IsLoggedIn=true ayarlandı. Email={email}");

                        transaction.Commit();
                        _logger.LogInformation($"Login: Transaction tamamlandı. Email={email}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Login: Hata oluştu. Email={email}, Hata={ex.Message}");
                    throw;
                }
            }
        }
    }
}