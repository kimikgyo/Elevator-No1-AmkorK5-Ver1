using Data.Repositorys;

namespace Data.Interfaces
{
    public interface IUnitOfWorkRepository : IDisposable
    {
        CommandRepository Commands { get; }
        StatusRepository ElevatorStatus { get; }

        void SaveChanges();
    }
}