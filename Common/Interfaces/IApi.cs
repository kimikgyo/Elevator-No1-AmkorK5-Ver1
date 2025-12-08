using Common.Dtos.Rests;
using Common.Dtos.Rests.Settings;

namespace Common.Interfaces
{
    public interface IApi
    {
        Uri BaseAddress { get; }

        Task<List<Response_SettingDto>> Get_Elevators_Async();

        Task<Response_SettingDto> GetById_Elevators_Async(string id);

        Task<ResponseDto> Patch_Elevators_Async(string Id, object value);
    }
}