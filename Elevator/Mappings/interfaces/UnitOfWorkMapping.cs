
namespace Elevator_NO1.Mappings.interfaces
{
    public class UnitOfWorkMapping : IUnitOfWorkMapping
    {
        public Status_Mapping StatusMappings { get; private set; }
        public Command_Mapping CommandMappings { get; private set; }
        public Setting_Mapping SettingMappings { get; private set; }

        public UnitOfWorkMapping()
        {
            Mapping();
        }

        private void Mapping()
        {
            StatusMappings = new Status_Mapping();
            CommandMappings = new Command_Mapping();
            SettingMappings = new Setting_Mapping();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}