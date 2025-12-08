
namespace Elevator_NO1.Mappings.interfaces
{
    public class UnitOfWorkMapping : IUnitOfWorkMapping
    {
        public StatusMapping StatusMappings { get; private set; }
        public CommandMapping CommandMappings { get; private set; }

        public UnitOfWorkMapping()
        {
            Mapping();
        }

        private void Mapping()
        {
            StatusMappings = new StatusMapping();
            CommandMappings = new CommandMapping();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}