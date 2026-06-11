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
        // Tout nouvel utilisateur est automatiquement Editor.
        // Viewer est un rôle CONTEXTUEL (accès via lien de partage uniquement), pas assignable à l'inscription.
        // Pour promouvoir en Admin : PUT /api/auth/users/{id}/role
    }

    public class ChangeRoleRequestDto
    {
        [Required]
        [RegularExpression("^(Admin|Editor)$",
            ErrorMessage = "Rôle invalide. Valeurs acceptées : Admin, Editor. (Viewer est contextuel — accès par lien de partage uniquement)")]
        public string Role { get; set; } = "";
    }

    public class AuthResponseDto
    {
        public string Token { get; init; } = "";
        public UserInfoDto User { get; init; } = new();
    }

    public class UserInfoDto
    {
        public int     Id        { get; init; }
        public string  Email     { get; init; } = "";
        public string? Name      { get; init; }
        public string  Role      { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }
}
