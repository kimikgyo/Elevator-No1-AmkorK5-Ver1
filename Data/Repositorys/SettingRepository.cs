using Common.Models;
using log4net;

namespace Data.Repositorys
{
    public class SettingRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Setting"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Setting>  _settings = new List<Setting>(); // cached data
        private readonly object _lock = new object();

        public SettingRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }
        private void Load()
        {
            _settings.Clear();

            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public void Add(Setting add)
        {
            lock (_lock)
            {
                _settings.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void Update(Setting update)
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
                _settings.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Setting remove)
        {
            lock (_lock)
            {
                _settings.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }
        public List<Setting> GetAll()
        {
            return _settings.ToList();
        }

    }
}
