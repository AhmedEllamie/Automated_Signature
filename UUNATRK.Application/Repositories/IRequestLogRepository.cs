using UUNATEK.Domain.Entities;

namespace UUNATRK.Application.Repositories;

public interface IRequestLogRepository
{
    void Add(RequestLog log);
    Task<RequestLog?> GetByIdAsync(Guid id);
    Task<RequestLog?> GetByRequestIdAsync(Guid requestId);
    Task<List<RequestLog>> GetAllByRequestIdAsync(Guid requestId);
    void Update(RequestLog log);
    Task<List<RequestLog>> GetRecentAsync(int count = 50);
}
