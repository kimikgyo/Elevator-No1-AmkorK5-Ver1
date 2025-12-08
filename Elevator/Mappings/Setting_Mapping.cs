using Common.Dtos.Rests.Settings;
using Common.Models;

namespace Elevator_NO1.Mappings
{
    public class Setting_Mapping
    {
        public Setting Response(Response_SettingDto response_SettingDto)
        {
            var responce = new Setting
            {
                _id = response_SettingDto._id,
                id = response_SettingDto.id,
                ip = response_SettingDto.ip,
                port = response_SettingDto.port,
                mode = response_SettingDto.mode,
                timeout = response_SettingDto.timeout,
                createBy = response_SettingDto.createBy,
                updateBy = response_SettingDto.updateBy,
                createdAt = response_SettingDto.createdAt,
                updatedAt = response_SettingDto.updatedAt
            };

            return responce;
        }

        public Request_SettingDto Request(Status  status)
        {
            var Request = new Request_SettingDto
            {
                mode = status.mode,
                updateAt = DateTime.Now
            };
            return Request;
        }
    }
}