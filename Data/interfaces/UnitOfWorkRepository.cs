using Data.Repositorys;
using System.Data;

namespace Data.Interfaces
{
    public class ConnectionStrings
    {
        public static readonly string DB1 = @"Data SOURCE=.\SQLEXPRESS;Initial Catalog=JobScheduler; User ID = sa;TrustServerCertificate=true; Password=acsserver;Connect Timeout=30;";
        //public static readonly string DB1 = @"Data Source=192.168.8.215,1433; Initial Catalog=JobScheduler; User ID = sa; Password=acsserver; Connect Timeout=30; TrustServerCertificate=true"; // STI
    }

    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private IDbConnection _db;

        private static readonly string connectionString = ConnectionStrings.DB1;

        public CommandRepository Commands { get; private set; }
        public StatusRepository ElevatorStatus { get; private set; }

        public UnitOfWorkRepository()
        {
            repository();
        }

        private void repository()
        {
            Commands = new CommandRepository(connectionString);
            ElevatorStatus = new StatusRepository(connectionString);
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}