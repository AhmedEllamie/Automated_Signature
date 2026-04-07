using Microsoft.EntityFrameworkCore;
using UUNATEK.Domain.Entities;
using UUNATRK.Application.Data;

namespace UUNATRK.Application.Repositories;

public class RequestLogRepository : IRequestLogRepository
{
    private readonly ApplicationDbContext _context;

    public RequestLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(RequestLog log)
    {
        _context.RequestLogs.Add(log);
    }

    public async Task<RequestLog?> GetByIdAsync(Guid id)
    {
        return await _context.RequestLogs.FindAsync(id);
    }

    public async Task<RequestLog?> GetByRequestIdAsync(Guid requestId)
    {
        return await _context.RequestLogs
            .Where(r => r.RequestId == requestId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<RequestLog>> GetAllByRequestIdAsync(Guid requestId)
    {
        return await _context.RequestLogs
            .Where(r => r.RequestId == requestId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public void Update(RequestLog log)
    {
        _context.RequestLogs.Update(log);
    }

    public async Task<List<RequestLog>> GetRecentAsync(int count = 50)
    {
        return await _context.RequestLogs
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
