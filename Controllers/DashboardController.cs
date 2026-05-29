using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardAPI.Data;
using DashboardAPI.DTOs;
using DashboardAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
   

    public DashboardController(AppDbContext context, DashboardDao dao)
    {
        _context = context;
     
    }

    // =========================
    // GET ALL DASHBOARDS
    // =========================
    [HttpGet]
public IActionResult GetAll()
{
    var dashboards = _context.Dashboards
        .Include(d => d.Widgets)
        .ToList();

    var result = dashboards.Select(d => new DashboardDetailDto
    {
    Id = d.Id,
    Name = d.Name,
    DatasetId = d.DatasetId,

        Columns = JsonSerializer.Deserialize<List<string>>(d.ColumnsJson ?? "[]"),
        Insights = JsonSerializer.Deserialize<List<string>>(d.InsightsJson ?? "[]"),
        Recommendations = JsonSerializer.Deserialize<List<string>>(d.RecommendationsJson ?? "[]"),

        Widgets = d.Widgets.Select(w => new Widget
        {
            Id = w.Id,
            Type = w.Type,
            Title = w.Title,
            Data = w.Data,
            DashboardId = w.DashboardId
        }).ToList()
    });

    return Ok(result);
}

    // =========================
    // CREATE DASHBOARD
    // =========================
 [HttpPost]
public IActionResult Create([FromBody] DashboardDetailDto dto)
{
   var dashboard = new Dashboard
{
    Name = dto.Name,
    DatasetId = dto.DatasetId,
    UserId = 1,

    ColumnsJson = JsonSerializer.Serialize(dto.Columns ?? new List<string>()),
    InsightsJson = JsonSerializer.Serialize(dto.Insights ?? new List<string>()),
    RecommendationsJson = JsonSerializer.Serialize(dto.Recommendations ?? new List<string>()),

    ShareToken = Guid.NewGuid().ToString(),
    IsPublic = false,

    CreatedAt = DateTime.Now // ✅ ici
};

    _context.Dashboards.Add(dashboard);
    _context.SaveChanges();

    // 🔥 Ajouter les widgets après (important)
    if (dto.Widgets != null && dto.Widgets.Any())
    {
        foreach (var w in dto.Widgets)
        {
            w.DashboardId = dashboard.Id;
            _context.Widgets.Add(w);
        }

        _context.SaveChanges();
    }

    return Ok(dashboard);
}

    // =========================
    // SHARE DASHBOARD (PUBLIC)
    // =========================
    [HttpGet("share/{token}")]
public IActionResult GetSharedDashboard(string token)
{
    var dashboard = _context.Dashboards
        .Include(d => d.Widgets)
        .FirstOrDefault(d => d.ShareToken == token);

    if (dashboard == null)
        return NotFound("Dashboard not found");

    if (!dashboard.IsPublic)
        return NotFound("Dashboard not public");

    var result = new DashboardDetailDto
    {
    Id = dashboard.Id,
    Name = dashboard.Name,
    DatasetId = dashboard.DatasetId,

        Columns = JsonSerializer.Deserialize<List<string>>(dashboard.ColumnsJson ?? "[]"),
        Insights = JsonSerializer.Deserialize<List<string>>(dashboard.InsightsJson ?? "[]"),
        Recommendations = JsonSerializer.Deserialize<List<string>>(dashboard.RecommendationsJson ?? "[]"),

        Widgets = dashboard.Widgets.Select(w => new Widget
        {
            Id = w.Id,
            Type = w.Type,
            Title = w.Title,
            Data = w.Data,
            DashboardId = w.DashboardId
        }).ToList()
    };

    return Ok(result);
}

    // =========================
    // TOGGLE SHARE
    // =========================
    [HttpPut("share/{id}")]
    public IActionResult ToggleShare(int id, bool isPublic)
    {
        var dashboard = _context.Dashboards.FirstOrDefault(d => d.Id == id);

        if (dashboard == null)
            return NotFound();

        dashboard.IsPublic = isPublic;

        _context.SaveChanges();

        return Ok(new
        {
            dashboard.ShareToken,
            dashboard.IsPublic
        });
    }
}