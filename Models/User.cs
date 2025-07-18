﻿namespace UserManagementApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsLoggedIn { get; set; } = false;

        public DateTime InsertDate { get; set; } = DateTime.Now;
    }
}
