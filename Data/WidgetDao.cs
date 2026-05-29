using System.Collections.Generic;
using System.Linq;
using DashboardAPI.Models;
public class WidgetDao
{
    private static List<Widget> widgets = new List<Widget>();
    private static int idCounter = 1;

    public List<Widget> GetByDashboard(int dashboardId)
    {
        return widgets.Where(w => w.DashboardId == dashboardId).ToList();
    }

    public Widget Add(Widget widget)
    {
        widget.Id = idCounter++;
        widgets.Add(widget);
        return widget;
    }

    public void Delete(int id)
    {
        var w = widgets.FirstOrDefault(x => x.Id == id);
        if (w != null)
            widgets.Remove(w);
    }
}