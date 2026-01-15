using Common.Models;
using log4net;

namespace Data.Repositorys
{
    public class StatusRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Status"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Status> _statuses = new List<Status>(); // cached data
        private readonly object _lock = new object();

        public StatusRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _statuses.Clear();

            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public void Add(Status add)
        {
            lock (_lock)
            {
                _statuses.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void Update(Status update)
        {
            lock (_lock)
            {
                logger.Info($"update: {update}");
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                _statuses.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Status remove)
        {
            lock (_lock)
            {
                _statuses.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }
        public List<Status> GetAll()
        {
            lock (_lock)
            {
                return _statuses.ToList();
            }
        }
    }
}
