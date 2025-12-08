using Common.Dtos.Mqtts.Status;
using Common.Models;

namespace Elevator_NO1.Mappings
{
    public class Status_Mapping
    {
        public Publish_StatusDto Publish_Status(Status model)
        {
            var publish = new Publish_StatusDto()
            {
                id = model.id,
                name = model.name,
                mode = model.mode,
                state = model.state,
            };
            return publish;
        }
    }
}