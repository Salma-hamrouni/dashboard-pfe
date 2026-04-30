using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Data
{
    public class WidgetDao
    {
        private readonly AppDbContext _context;

        public WidgetDao(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Widget>> GetByDashboardAsync(int dashboardId)
            => await _context.Widgets
                             .Where(w => w.DashboardId == dashboardId)
                             .ToListAsync();

        public async Task<Widget?> GetByIdAsync(int id)
            => await _context.Widgets.FindAsync(id);

        public async Task<Widget> AddAsync(Widget widget)
        {
            _context.Widgets.Add(widget);
            await _context.SaveChangesAsync();
            return widget;
        }

        public async Task<Widget> UpdateAsync(Widget widget)
        {
            _context.Widgets.Update(widget);
            await _context.SaveChangesAsync();
            return widget;
        }

        public async Task DeleteAsync(int id)
        {
            var widget = await _context.Widgets.FindAsync(id);
            if (widget != null)
            {
                _context.Widgets.Remove(widget);
                await _context.SaveChangesAsync();
            }
        }
    }
}