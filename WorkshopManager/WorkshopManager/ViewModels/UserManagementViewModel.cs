using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Potwierdź hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare("Password", ErrorMessage = "Hasła nie są zgodne")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Wybierz rolę")]
        [Display(Name = "Rola")]
        public string SelectedRole { get; set; } = null!;

        public List<string> AvailableRoles { get; set; } = new() { "Admin", "Mechanik", "Recepcjonista" };
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = null!;

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Aktualna rola")]
        public List<string> CurrentRoles { get; set; } = new();

        [Required(ErrorMessage = "Wybierz rolę")]
        [Display(Name = "Nowa rola")]
        public string SelectedRole { get; set; } = null!;

        public List<string> AvailableRoles { get; set; } = new() { "Admin", "Mechanik", "Recepcjonista" };

        [Display(Name = "Zablokowany")]
        public bool IsLocked { get; set; }
    }
}
