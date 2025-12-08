namespace Common.Dtos.Rests.Settings
{
    public class Response_SettingDto
    {
        public string _id { get; set; }
        public string id { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string mode { get; set; }
        public string timeout { get; set; }
        public string createBy { get; set; }
        public string updateBy { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }

        public override string ToString()
        {
            return
                $"_id = {_id,-5}" +
                $",id = {id,-5}" +
                $",ip = {ip,-5}" +
                $",port = {port,-5}" +
                $",mode = {mode,-5}" +
                $",timeout = {timeout,-5}" +
                $",createBy = {createBy,-5}" +
                $",updateBy = {updateBy,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}";
        }
    }
}