using Data.Repositorys;
using Data.Repositorys.Services;

namespace Data.Interfaces
{
    public interface IUnitOfWorkRepository : IDisposable
    {
        CommandRepository Commands { get; }
        StatusRepository ElevatorStatus { get; }
        ServiceApiRepository ServiceApis { get; }
        SettingRepository Settings { get; }

        void SaveChanges();
    }
}