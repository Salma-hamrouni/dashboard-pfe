using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Data
{
    public class DataSourceDao
    {
        private readonly AppDbContext _db;

        public DataSourceDao(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<DataSource>> GetAllByUserAsync(int userId)
            => await _db.DataSources
                        .Where(d => d.UserId == userId)
                        .OrderByDescending(d => d.CreatedAt)
                        .ToListAsync();

        public async Task<DataSource?> GetByIdAsync(int id)
            => await _db.DataSources.FindAsync(id);

        public async Task<DataSource> CreateAsync(DataSource ds)
        {
            _db.DataSources.Add(ds);
            await _db.SaveChangesAsync();
            return ds;
        }

        public async Task<DataSource> UpdateAsync(DataSource ds)
        {
            _db.DataSources.Update(ds);
            await _db.SaveChangesAsync();
            return ds;
        }

        public async Task DeleteAsync(int id)
        {
            var ds = await _db.DataSources.FindAsync(id);
            if (ds != null)
            {
                _db.DataSources.Remove(ds);
                await _db.SaveChangesAsync();
            }
        }
    }
}