using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.DTOs.Widget
{
    // Types de graphiques supportés
    public static class WidgetTypes
    {
        public const string Bar     = "bar";
        public const string Line    = "line";
        public const string Pie     = "pie";
        public const string Donut   = "donut";
        public const string Scatter = "scatter";
        public const string Kpi     = "kpi";
        public const string Table   = "table";
        public const string Area    = "area";

        public static readonly string[] All =
            [Bar, Line, Pie, Donut, Scatter, Kpi, Table, Area];
    }

    public class CreateWidgetDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DashboardId invalide.")]
        public int DashboardId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "";

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = "";

        public string? Data   { get; set; }
        public string? Config { get; set; }
    }

    public class UpdateWidgetDto
    {
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "";

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = "";

        public string? Data   { get; set; }
        public string? Config { get; set; }
    }

    /// <summary>
    /// Configuration d'un widget pour le chargement dynamique des données.
    /// Sérialisé en JSON dans la colonne Config du widget.
    /// </summary>
    public class WidgetConfig
    {
        public int?    DataSourceId  { get; set; }
        public string? XColumn       { get; set; }
        public string? YColumn       { get; set; }
        public string  Aggregation   { get; set; } = "COUNT";
        public string? GroupBy       { get; set; }
        public string? FilterColumn  { get; set; }
        public string? FilterValue   { get; set; }
        public int     Limit         { get; set; } = 100;
        public string? SortBy        { get; set; }
        public string  SortOrder     { get; set; } = "DESC";
    }

    /// <summary>Réponse widget — partagé par WidgetController et DashboardController.</summary>
    public class WidgetResponseDto
    {
        public int     Id          { get; init; }
        public string  Type        { get; init; } = "";
        public string  Title       { get; init; } = "";
        public string? Data        { get; init; }
        public string? Config      { get; init; }
        public int     DashboardId { get; init; }
    }
}
