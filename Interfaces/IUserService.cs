using UserManagementApi.Models;

namespace UserManagementApi.Interfaces
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        User GetUserById(int id);
        void AddNewUser(User user);
        void UpdateUser(User user);
        void DeleteUserById(int id);
        void SoftDeleteUserById(int id);
        bool Login(string email, string password);
        List<User> GetAllUsersOrderByDate();


    }
}
