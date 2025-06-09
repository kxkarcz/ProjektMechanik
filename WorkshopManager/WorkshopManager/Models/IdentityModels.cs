using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class AccessDeniedModel
    {
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequiredRole { get; set; }
        public string? UserRole { get; set; }
        public string? Action { get; set; }
        public string? Controller { get; set; }
    }

    public class LockoutModel
    {
        public DateTime? LockoutEnd { get; set; }
        public string? ReturnUrl { get; set; }

        public string GetLockoutTimeRemaining()
        {
            if (LockoutEnd == null || LockoutEnd <= DateTime.UtcNow)
                return "Konto zostało odblokowane";

            var timeRemaining = LockoutEnd.Value - DateTime.UtcNow;

            if (timeRemaining.TotalHours >= 1)
                return $"Około {Math.Ceiling(timeRemaining.TotalHours)} godzin";
            else if (timeRemaining.TotalMinutes >= 1)
                return $"Około {Math.Ceiling(timeRemaining.TotalMinutes)} minut";
            else
                return "Mniej niż minutę";
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = "";

        [Display(Name = "Zapamiętaj mnie")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} i maksymalnie {1} znaków.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare("Password", ErrorMessage = "Hasło i potwierdzenie hasła nie pasują do siebie.")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Rola jest wymagana")]
        [Display(Name = "Rola")]
        public string Role { get; set; } = "";

        public List<string> AvailableRoles { get; set; } = new();
    }

    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Obecne hasło jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Obecne hasło")]
        public string OldPassword { get; set; } = "";

        [Required(ErrorMessage = "Nowe hasło jest wymagane")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} i maksymalnie {1} znaków.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare("NewPassword", ErrorMessage = "Nowe hasło i potwierdzenie nie pasują do siebie.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class UserProfileModel
    {
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Display(Name = "Numer telefonu")]
        [Phone(ErrorMessage = "Nieprawidłowy numer telefonu")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Rola")]
        public string Role { get; set; } = "";

        [Display(Name = "Email potwierdzony")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Konto zablokowane")]
        public bool LockoutEnabled { get; set; }

        [Display(Name = "Koniec blokady")]
        public DateTime? LockoutEnd { get; set; }

        public bool IsLockedOut()
        {
            return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
        }
    }
}