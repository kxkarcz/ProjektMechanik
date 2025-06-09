using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Zapamiętaj mnie")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare("Password", ErrorMessage = "Hasła nie są identyczne")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rola jest wymagana")]
        [Display(Name = "Rola")]
        public string Role { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new List<string>();
    }

    public class AccessDeniedViewModel
    {
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequiredRole { get; set; }
        public string? UserRole { get; set; }
        public string? Action { get; set; }
        public string? Controller { get; set; }
    }

    public class LockoutViewModel
    {
        public DateTime? LockoutEnd { get; set; }
        public string? ReturnUrl { get; set; }

        public string GetLockoutTimeRemaining()
        {
            if (!LockoutEnd.HasValue || LockoutEnd <= DateTime.UtcNow)
                return "Konto zostało odblokowane";

            var remaining = LockoutEnd.Value - DateTime.UtcNow;
            if (remaining.TotalMinutes < 1)
                return "Mniej niż minutę";
            else if (remaining.TotalHours < 1)
                return $"{remaining.Minutes} minut";
            else
                return $"{remaining.Hours} godzin i {remaining.Minutes} minut";
        }
    }

    public class UserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }

        // Aktualne hasło - wymagane tylko gdy użytkownik zmienia własne hasło
        [DataType(DataType.Password)]
        [Display(Name = "Aktualne hasło")]
        public string? OldPassword { get; set; }

        [Required(ErrorMessage = "Nowe hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są identyczne")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool IsAdminChangingPassword => !string.IsNullOrEmpty(UserId);
    }
}