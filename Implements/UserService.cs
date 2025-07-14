using UserManagementApi.Helpers;
using UserManagementApi.Data;
using UserManagementApi.Interfaces;
using UserManagementApi.Models;

namespace UserManagementApi.Implements
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public List<User> GetAllUsers()
        {
            return _context.Users.Where(u => u.IsActive).ToList();
        }

        public User GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id && u.IsActive);
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
            user.Password = PasswordHasher.Hash(user.Password); // şifreyi hashleme kısmı burası
            //user.InsertDate = DateTime.Now;
            //user.IsActive = true;
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
                _context.SaveChanges();
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
            var hashedPassword = PasswordHasher.Hash(password);
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == hashedPassword && u.IsActive);

            return user != null;
        }
    }
}
