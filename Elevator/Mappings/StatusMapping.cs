using Common.Dtos;
using Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.ComponentModel.Design;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace Elevator1.Mappings
{
    public class StatusMapping
    {
        public MqttPublishDtoStatus MqttPublishStatus(Status model)
        {
            var publish = new MqttPublishDtoStatus()
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