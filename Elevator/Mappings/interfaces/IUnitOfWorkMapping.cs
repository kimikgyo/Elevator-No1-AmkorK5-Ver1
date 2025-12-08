namespace Elevator_NO1.Mappings.interfaces
{
    public interface IUnitOfWorkMapping : IDisposable
    {
        Status_Mapping StatusMappings { get; }
        Command_Mapping CommandMappings { get; }
        Setting_Mapping SettingMappings { get; }
    }
}