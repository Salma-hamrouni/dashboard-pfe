using System.ComponentModel.DataAnnotations;
using DashboardAPI.DTOs.Widget;

namespace DashboardAPI.DTOs.Dashboard
{
    public class CreateDashboardDto
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [MinLength(2, ErrorMessage = "Minimum 2 caractères.")]
        [MaxLength(150, ErrorMessage = "Maximum 150 caractères.")]
        public string Name { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? DatasetId { get; set; }

        public List<string> Columns         { get; set; } = [];
        public List<string> Insights        { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];

        public List<CreateWidgetInDashboardDto> Widgets { get; set; } = [];
    }

    public class UpdateDashboardDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(150)]
        public string Name { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? DatasetId { get; set; }

        public List<string> Columns         { get; set; } = [];
        public List<string> Insights        { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];
    }

    public class CreateWidgetInDashboardDto
    {
        [Required]
        [MaxLength(50)]
        public string Type  { get; set; } = "";

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = "";

        public string? Data   { get; set; }
        public string? Config { get; set; }
    }

    public class DashboardResponseDto
    {
        public int      Id              { get; init; }
        public string   Name            { get; init; } = "";
        public string?  Description     { get; init; }
        public int?     DatasetId       { get; init; }
        public bool     IsPublic        { get; init; }
        public string?  ShareToken      { get; init; }
        public DateTime CreatedAt       { get; init; }
        public int      WidgetCount     { get; init; }

        public List<string> Columns         { get; init; } = [];
        public List<string> Insights        { get; init; } = [];
        public List<string> Recommendations { get; init; } = [];

        public List<WidgetResponseDto> Widgets { get; init; } = [];
    }
}
