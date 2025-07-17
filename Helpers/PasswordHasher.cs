using System.Security.Cryptography;
using System.Text;

namespace UserManagementApi.Helpers
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Verify metodu: şifreyi hash'ler ve verilen hash ile karşılaştırır
        public static bool Verify(string password, string hashedPassword)
        {
            var hashedInput = Hash(password);
            return hashedInput == hashedPassword;
        }
    }
}
