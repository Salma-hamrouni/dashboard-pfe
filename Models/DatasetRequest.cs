namespace DashboardAPI.Models
{
    public class DatasetRequest
    {
        public string                           FileName { get; set; } = string.Empty;
        public List<string>                     Columns  { get; set; } = new();
        public List<Dictionary<string, string>> Rows     { get; set; } = new();
    }
}