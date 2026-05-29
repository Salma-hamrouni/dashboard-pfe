using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using System.Text.Json;
using DashboardAPI.Models;
[ApiController]
[Route("api/datasets")]
public class DatasetController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Fichier vide");

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = new List<Dictionary<string, string>>();

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var row = new Dictionary<string, string>();

            foreach (var header in csv.HeaderRecord)
            {
                row[header] = csv.GetField(header);
            }

            records.Add(row);
        }

        return Ok(new
        {
            rows = records.Take(5),
            totalRows = records.Count,
            columns = csv.HeaderRecord
        });
    }

    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] List<Dictionary<string, string>> rows)
    {
        if (rows == null || !rows.Any())
            return BadRequest("No data");

        var firstRow = rows?.FirstOrDefault();
        if (firstRow == null)
            return BadRequest("No data");
        var profile = new List<ColumnProfile>();

        foreach (var col in firstRow.Keys)
        {
            profile.Add(new ColumnProfile
            {
                Name = col,
                Type = DetectType(firstRow[col])
            });
        }

        return Ok(profile);
    }
   [HttpPost("recommend")]
    public IActionResult Recommend([FromBody] List<ColumnProfile> columns)
    {
        var charts = new List<object>();

        for (int i = 0; i < columns.Count; i++)
{
    for (int j = 0; j < columns.Count; j++)
    {
        if (i == j) continue;

        var x = columns[i];
        var y = columns[j];

        var chartType = RecommendChart(x.Name, x.Type, y.Name, y.Type);

        charts.Add(new
        {
            x = x.Name,
            y = y.Name,
            chart = chartType
        });
    }
}

        return Ok(charts.Distinct().Take(10));
    }
[HttpPost("ai-analyze")]
public async Task<IActionResult> AnalyzeWithAI(
    List<DatasetRequest> rows,
    [FromServices] AiService ai,
    [FromServices] DashboardDao dao,
     [FromServices] WidgetDao widgetDao)
{
    var result = await ai.AnalyzeAsync(rows[0]);

var dashboard = new Dashboard
{
    Name = "Dashboard " + DateTime.Now.ToString("HH:mm"),
    DatasetId = 0, // TODO: set a valid DatasetId when available
    InsightsJson = JsonSerializer.Serialize(result.Insights),
    RecommendationsJson = JsonSerializer.Serialize(result.Recommendations)
};

dao.Add(dashboard);
// 🔥 AUTO WIDGETS
// 1. BAR CHART
widgetDao.Add(new Widget
{
    DashboardId = dashboard.Id,
    Type = "bar",
    Title = "Analyse des données (Bar Chart)",
    Data = System.Text.Json.JsonSerializer.Serialize(new
    {
        labels = new[] { "A", "B", "C", "D" },
        values = new[] { 120, 200, 150, 300 }
    })
});
// 2. LINE CHART
widgetDao.Add(new Widget
{
    DashboardId = dashboard.Id,
    Type = "line",
    Title = "Tendance temporelle",
    Data = System.Text.Json.JsonSerializer.Serialize(new
    {
        labels = new[] { "Jan", "Fév", "Mar", "Avr" },
        values = new[] { 100, 180, 250, 400 }
    })
});
// 3. KPI
widgetDao.Add(new Widget
{
    DashboardId = dashboard.Id,
    Type = "kpi",
    Title = "Score global",
    Data = System.Text.Json.JsonSerializer.Serialize(new
    {
        value = 87
    })
});
widgetDao.Add(new Widget
{
    DashboardId = dashboard.Id,
    Type = "insight",
    Title = "AI Insight",
    Data = JsonSerializer.Serialize(new
    {
        text = string.Join(" | ", result.Insights)
    })
});
return Ok(dashboard);
}
    private string RecommendChart(string colX, string typeX, string colY, string typeY)
    {
        if (typeX == "date" && typeY == "number")
            return "line chart";

        if (typeX == "category" && typeY == "number")
            return "bar chart";

        if (typeX == "number" && typeY == "number")
            return "scatter plot";

        return "table";
    }
    private string DetectType(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "unknown";

    if (int.TryParse(value, out _))
        return "number";

    if (double.TryParse(value, out _))
        return "number";

    if (DateTime.TryParse(value, out _))
        return "date";

    return "category";
}
}