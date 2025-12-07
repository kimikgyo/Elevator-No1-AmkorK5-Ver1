namespace Common.Dtos.Rests
{
    public class ResponseDto
    {
        public int statusCode { get; set; }
        public string statusText { get; set; }
        public string? message { get; set; }

        public override string ToString()
        {
            return
                $"statusCode = {statusCode,-5}" +
                $",statusText = {statusText,-5}" +
                $",message = {message,-5}";
        }
    }
}