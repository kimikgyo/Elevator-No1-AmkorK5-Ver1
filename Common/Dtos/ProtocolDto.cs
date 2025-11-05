namespace Common.Dtos
{
    public class ProtocolDto
    {
        //cmd 20 제어요청의경우 cmd 21제어 응답이 들어올때까지 계속 보냄
        //cmd 21 제어 응답이 올경우 다시 상태 10요청하여 elev 상태확인

        public int Cmd { get; set; }                                //(10)상태요청 , (11)상태응답 ,(20)제어요청 , (21)제어응답
        public int Aid { get; set; }                                //ACS의 고유번호
        public int Count { get; set; }                              //호기 갯수
        public int Dld { get; set; }                                //Elevator 제어반ID
        public int Status { get; set; }                             //현재 Elevator 상태 : 정상,고장,파킹
        public int Floor { get; set; }                              //Elevator층 Index(최하층을 1부터) 1호기 :1,2,3
        public int Dir { get; set; }                                //Elevator 방향
        public int Door { get; set; }                               //Elevator 도어 상태 닫힘 = 0 , 열림완료상태 = 1
        public int car_f { get; set; }                              //카내부 앞문 층 버튼 누름 상태(Bit값으로 계산) / 1층 등록일 경우 1 , 2층 등록일 경우 2 , 3층 등록일 경우 4
        public int car_r { get; set; }                              //카내부 뒷문 층 버튼 누름 상태(Bit값으로 계산) / 1층 등록일 경우 1 , 2층 등록일 경우 2 , 3층 등록일 경우 4
        public int Hallup_f { get; set; }                           //승강장 외부 앞문 상향 버튼 누름 상태 (Bit값으로 계산)
        public int Hallup_r { get; set; }                           //승강장 외부 뒷문 상향 버튼 누름 상태 (Bit값으로 계산)
        public int HallDn_f { get; set; }                           //승강장 외부 앞문 하향 버튼 누름 상태 (Bit값으로 계산)
        public int HallDn_r { get; set; }                           //승강장 외부 뒷문 하향 버튼 누름 상태(Bit값으로 계산)
        public int ErrCode { get; set; }                            //엘리베이터 고장 코드(어떤 고장 때문에 운행이 안되는지만 참고하기 위함)
        public int Param { get; set; }                              //제어 명령 파라미터

        //(01:승강장 Front호출(버튼누름동작과 동일)해당층으로 이동후 문열림
        //(02:승강장 Rear 호출)
        //(03:카내부 Fornt 호출(목적층 이동 ) 해당층으로 이동 후 앞문 문열림 1,2)
        //(04:카내부 Rear 호출 (목적층 이동) 해당층으로 이동후 뒷문 열림 1R,2R(양문일 경우)
        //(05:Front Door Open)
        //(06:Front Door Close)
        //(07:Rear Door Open)
        //(08:Rear Door Close)
        //(09:AGV 운전 , Data = 1이면 운전명령, Data = 0 이면 운전 해제 명령
        public int Data { get; set; }                               //부름 층 Index

        public int Dest { get; set; }                               //목적 층 Index (목적층 호출시에는 Data값과 동일하게)
        public string Result { get; set; }                          //제어 성공 여부 (Ok/fail)
        public string ErrMsg { get; set; }                          //Result가 "fail"시 내용을 기재한다.

    }
}
