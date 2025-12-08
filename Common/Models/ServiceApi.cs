using Common.Interfaces;

namespace Common.Models
{
    public class ServiceApi
    {
        private IApi _api = null;

        public IApi Api
        {
            get => _api;
            set
            {
                if (_api != null) new InvalidOperationException("Api는 최초 한번만 할당 가능하다!");
                _api = value;
            }
        }


        public string type { get; set; }
        public string subType { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string connectId { get; set; } = string.Empty;
        public string connectPassword { get; set; } = string.Empty;
        public string timeOut { get; set; } = "5000";
    }
}