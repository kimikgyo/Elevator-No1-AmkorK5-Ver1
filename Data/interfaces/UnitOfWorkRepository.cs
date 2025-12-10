using Data.Repositorys;
using Data.Repositorys.Services;
using System.Data;

namespace Data.Interfaces
{
    public class ConnectionStrings
    {
        public static readonly string DB1 = @"Data SOURCE=.\SQLEXPRESS;Initial Catalog=AmkorK5_Elevator_NO1; User ID = sa;TrustServerCertificate=true; Password=acsserver;Connect Timeout=30;";
        //public static readonly string DB1 = @"Data Source=192.168.8.215,1433; Initial Catalog=JobScheduler; User ID = sa; Password=acsserver; Connect Timeout=30; TrustServerCertificate=true"; // STI
    }

    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private IDbConnection _db;

        private static readonly string connectionString = ConnectionStrings.DB1;

        public CommandRepository Commands { get; private set; }
        public StatusRepository ElevatorStatus { get; private set; }
        public ServiceApiRepository ServiceApis { get; private set; }
        public SettingRepository Settings { get; private set; }

        public UnitOfWorkRepository()
        {
            repository();
        }

        private void repository()
        {
            Commands = new CommandRepository(connectionString);
            ElevatorStatus = new StatusRepository(connectionString);
            ServiceApis = new ServiceApiRepository(connectionString);
            Settings = new SettingRepository(connectionString);
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}