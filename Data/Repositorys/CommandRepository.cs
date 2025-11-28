using Common.Models;
using log4net;
using Microsoft.Extensions.Logging;

namespace Data.Repositorys
{
    public class CommandRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Command"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Command> _commands = new List<Command>(); // cached data
        private readonly object _lock = new object();

        public CommandRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _commands.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public void Add(Command add)
        {
            lock (_lock)
            {
                _commands.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void Update(Command update)
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
                _commands.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Command remove)
        {
            lock (_lock)
            {
                _commands.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<Command> GetAll()
        {
            return _commands.ToList();
        }

        public Command GetById(string id)
        {
            return _commands.FirstOrDefault(c => c.commnadId == id);
        }
    }
}