// ViewModels/UserManagementViewModel.cs
namespace ERMS.ViewModels
{
    public class UserManagementViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public IList<string> Roles { get; set; }
        public bool IsLocked { get; set; }
    }
}
