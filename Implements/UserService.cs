using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using UserManagementApi.Data;
using UserManagementApi.Dtos;
using UserManagementApi.Helpers;
using UserManagementApi.Interfaces;
using UserManagementApi.Models;

namespace UserManagementApi.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;
        private static readonly object _lock = new object();

        public UserService(IUserRepository userRepository, AppDbContext context, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }

        public List<User> GetAllUsers()
        {
            return _userRepository.GetAll();
        }
        public List<UserWithRoleDto> GetUsersWithRolesFromSP()
        {
            var result = _context.UserWithRoleDtos
                .FromSqlRaw("EXEC GetUsersWithRoles")
                .ToList();

            return result;
        }

        public User GetUserById(int id)
        {
            return _userRepository.GetById(id);
        }

        public User GetUserByEmail(string email)
        {
            var user = _userRepository.GetByEmail(email);   
            _logger.LogInformation($"GetUserByEmail: Email={email}, UserFound={user != null}, IsLoggedIn={user?.IsLoggedIn}");
            return user;
        }

        public List<User> GetAllUsersOrderByDate()
        {
            return _userRepository.GetAll().OrderByDescending(u => u.InsertDate).ToList();
        }

        public void AddNewUser(User user)
        {
            user.Password = PasswordHasher.Hash(user.Password);
            _userRepository.Add(user);
        }

        public void UpdateUser(User user)
        {
            var existingUser = _userRepository.GetById(user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.IsLoggedIn = user.IsLoggedIn;
                _userRepository.Update(existingUser);
                _logger.LogInformation($"UpdateUser: Id={user.Id}, IsLoggedIn={user.IsLoggedIn}");
            }
        }

        public void DeleteUserById(int id)
        {
            _userRepository.Delete(id);
        }

        public void SoftDeleteUserById(int id)
        {
            _userRepository.SoftDelete(id);
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

                        if (user.IsLoggedIn)
                        {
                            _logger.LogWarning($"Login: Kullanıcı zaten giriş yapmış. Email={email}");
                            throw new Exception("Bu kullanıcı zaten giriş yapmış.");
                        }

                        user.IsLoggedIn = true;
                        _context.Entry(user).State = EntityState.Modified;
                        _context.SaveChanges();
                        _logger.LogInformation($"Login: IsLoggedIn=true ayarlandı. Email={email}");

                        transaction.Commit();
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
