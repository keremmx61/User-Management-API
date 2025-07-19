using System.Collections.Generic;
using UserManagementApi.Models;

namespace UserManagementApi.Interfaces
{
    public interface IUserRepository
    {
        List<User> GetAll();
        User GetById(int id);
        User GetByEmail(string email);
        void Add(User user);
        void Update(User user);
        void Delete(int id);
        void SoftDelete(int id);
    }
}
