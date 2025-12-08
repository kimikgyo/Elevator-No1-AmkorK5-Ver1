using Data;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using log4net;
using RestApi.Interfases;
using System.Diagnostics;

namespace Elevator_NO1.Services.Data
{
    public class Response_Data
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");

        public readonly IUnitOfWorkRepository _repository;
        public readonly IUnitOfWorkMapping _mapping;
        public readonly ILog _eventlog;
        public List<MqttTopicSubscribe> mqttTopicSubscribes = new List<MqttTopicSubscribe>();

        public Response_Data(ILog eventLog, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mapping = mapping;
            _eventlog = eventLog;
        }

        public async Task<bool> StartAsyc()
        {
            bool Complete = false;
            bool Resource = false;

            while (!Complete)
            {
                try
                {
                    ApiClient();
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            _repository.Settings.Delete();

                            var Elevator = await serviceApi.Api.GetById_Elevators_Async("NO1");

                            if (Elevator == null)
                            {
                                _eventlog.Info($"{nameof(Elevator)}GetDataFail");
                                break;
                            }
                            else
                            {
                                var elevatorSetting = _mapping.SettingMappings.Response(Elevator);
                                _repository.Settings.Add(elevatorSetting);
                                Resource = true;
                            }
                        }
                    }
                    if (Resource)
                    {
                        Complete = true;
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        public async Task<bool> ReloadAsyc()
        {
            bool Complete = false;
            bool Resource = false;

            while (!Complete)
            {
                try
                {
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            _repository.Settings.Delete();

                            var Elevator = await serviceApi.Api.GetById_Elevators_Async("NO1");

                            if (Elevator == null)
                            {
                                _eventlog.Info($"{nameof(Elevator)}GetDataFail");
                                break;
                            }
                            else
                            {
                                var elevatorSetting = _mapping.SettingMappings.Response(Elevator);
                                _repository.Settings.Add(elevatorSetting);
                                Resource = true;
                            }
                        }
                    }
                    if (Resource)
                    {
                        Complete = true;
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        private void ApiClient()
        {
            //Config 파일을 불러온다
            foreach (var apiInfo in ConfigData.ServiceApis)
            {
                var serviceInfo = _repository.ServiceApis.GetByIpPort(apiInfo.ip, apiInfo.port);
                if (serviceInfo == null)
                {
                    var client = new Api(apiInfo.type, apiInfo.ip, apiInfo.port, double.Parse(apiInfo.timeOut), apiInfo.connectId, apiInfo.connectPassword);
                    apiInfo.Api = client;
                    _repository.ServiceApis.Add(apiInfo);
                }
            }
        }

        public void LogExceptionMessage(Exception ex)
        {
            //string message = ex.InnerException?.Message ?? ex.Message;
            //string message = ex.ToString();
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            _eventlog.Info(message);
        }
    }
}