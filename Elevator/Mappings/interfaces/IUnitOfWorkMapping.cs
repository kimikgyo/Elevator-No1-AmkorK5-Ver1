namespace Elevator1.Mappings.interfaces
{
    public interface IUnitOfWorkMapping : IDisposable
    {
        StatusMapping StatusMappings { get; }
        CommandMapping CommandMappings { get; }
    }
}