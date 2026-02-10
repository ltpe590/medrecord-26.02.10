using Core.Entities;

namespace Core.Interfaces.Repositories
{
    public interface IVisitRepository
    {
        Task<Visit?> GetWithDetailsAsync(int visitId);
    }
}
