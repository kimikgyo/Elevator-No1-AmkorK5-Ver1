namespace Common.Interfaces
{
    public interface IApi
    {
        Uri BaseAddress { get; }

        //Task<List<Response_Worker>> GetResourceWorker();

        //Task<List<Response_MapDto>> GetResourceMap();

        //Task<List<Response_Position>> GetResourcePosition();

        //Task<List<Response_CarrierDto>> GetResourceCarrier();

        //Task<List<Response_JobTemplateDto>> STIGetResourceJobTemplate();

        //Task<List<Response_JobTemplateDto>> AmkorGetResourceJobTemplate();

        //Task<ApResponseDto> WorkerPostMissionQueueAsync(object value);

        //Task<ApResponseDto> ElevatorPostMissionQueueAsync(object value);

        //Task<ApResponseDto> MiddlewarePostMissionQueueAsync(object value);

        //Task<ApResponseDto> WorkerDeleteMissionQueueAsync(string id);

        //Task<ApResponseDto> MiddlewareDeleteMissionQueueAsync(string id);

        //Task<ApResponseDto> ElevatorDeletetMissionQueueAsync(string id);

        //Task<ApResponseDto> PositionPatchAsync(string id, object value);
    }
}