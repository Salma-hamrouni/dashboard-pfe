using System.Collections.Generic;
using System.Linq;
using DashboardAPI.Models;

public class DashboardDao
{
    private static List<Dashboard> dashboards = new List<Dashboard>();
    private static int idCounter = 1;

    public List<Dashboard> GetAll()
    {
        return dashboards;
    }

    public Dashboard Add(Dashboard dashboard)
    {
        dashboard.Id = idCounter++;
        dashboards.Add(dashboard);
        return dashboard;
    }

    public void Delete(int id)
    {
        var d = dashboards.FirstOrDefault(x => x.Id == id);
        if (d != null)
        {
            dashboards.Remove(d);
        }
    }
}