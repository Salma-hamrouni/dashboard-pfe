using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        [MaxLength(256)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [MinLength(6, ErrorMessage = "Minimum 6 caractères.")]
        [MaxLength(128)]
        public string Password { get; set; } = "";
    }

    public class RegisterRequestDto : LoginRequestDto
    {
        // Role assigné uniquement par un Admin via PUT /users/{id}/role
        // L'inscription publique est toujours Viewer
    }

    public class ChangeRoleRequestDto
    {
        [Required]
        [RegularExpression("^(Admin|Editor|Viewer)$",
            ErrorMessage = "Rôle invalide. Valeurs: Admin, Editor, Viewer.")]
        public string Role { get; set; } = "";
    }

    public class AuthResponseDto
    {
        public string Token { get; init; } = "";
        public UserInfoDto User { get; init; } = new();
    }

    public class UserInfoDto
    {
        public int    Id        { get; init; }
        public string Email     { get; init; } = "";
        public string Role      { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }
}
