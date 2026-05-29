using Microsoft.AspNetCore.Mvc;
using DashboardAPI.Models;
[ApiController]
[Route("api/[controller]")]
public class WidgetController : ControllerBase
{
    private readonly WidgetDao _dao;

    public WidgetController(WidgetDao dao)
    {
        _dao = dao;
    }

    // GET widgets d’un dashboard
    [HttpGet("{dashboardId}")]
    public IActionResult GetByDashboard(int dashboardId)
    {
        return Ok(_dao.GetByDashboard(dashboardId));
    }

    // Ajouter widget
    [HttpPost]
    public IActionResult Add([FromBody] Widget widget)
    {
        var result = _dao.Add(widget);
        return Ok(result);
    }

    // Supprimer widget
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _dao.Delete(id);
        return Ok();
    }
}