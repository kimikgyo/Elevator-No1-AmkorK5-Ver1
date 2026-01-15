using Common.Models;
using System.Xml.Linq;

namespace Common.Dtos.Mqtts.Cameras
{
    public class Subscribe_CarmeraStatusDto
    {
        public string ElevatorId { get; set; }
        public string Floor { get; set; }
        public string Sensing { get; set; }

        public override string ToString()
        {
            return
                $" ElevatorId = {ElevatorId,-5}" +
                $",Floor = {Floor,-5}" +
                $",Sensing = {Sensing,-5}";
        }
    }
}