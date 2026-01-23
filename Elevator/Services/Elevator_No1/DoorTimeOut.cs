using Common.Models;
using Data;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        // DoorOpen 상태가 "처음 감지된 시각" 기록용
        private DateTime _doorOpenSince = DateTime.MinValue;
        // DoorOpen 상태가 "어느 층에서 열렸는지" 기록용 (상태가 바뀌면 초기화 판단에 도움)
        private string _doorOpenState = string.Empty;


        private string EnsureDoorCloseOnOpenTimeoutByState()
        {
            // ============================================================
            // [목적]
            // - ElevatorStatus.state 가 DOOROPEN_* 인 상태가 일정 시간 유지되는데
            // - 현재 처리해야 할 커맨드가 하나도 없으면(=유령 오픈)
            // - 안전을 위해 DOORCLOSE 커맨드를 생성하고, 닫기 메시지를 리턴한다.
            //
            // [왜 필요한가]
            // - HOLD/OPEN 유지 중 장애로 DoorClose가 안 오거나
            // - 커맨드가 중간에 유실/삭제되면 문이 열린 채로 계속 유지될 수 있음
            //
            // [디버깅 포인트]
            // 1) DoorOpen 상태가 언제 시작됐는지(_doorOpenSince)
            // 2) 현재 ElevatorStatus.state 값이 실제로 DOOROPEN_* 인지
            // 3) active command가 진짜 0개인지
            // 4) 강제 DOORCLOSE 커맨드가 DB에 생성되는지
            // ============================================================

            // 0) 엘리베이터 상태 조회
            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(elevator.state))
                return string.Empty;

            // 1) DOOROPEN_* 상태인지 판단
            bool isDoorOpen = IsDoorOpenState(elevator.state);

            // 2) DOOROPEN이 아니면 타이머 초기화하고 종료
            // - 문이 닫혔거나 이동중이거나 다른 상태면 "오픈 유지 타임아웃" 대상 아님
            if (isDoorOpen == false)
            {
                if (_doorOpenSince != DateTime.MinValue)
                {
                    EventLogger.Info(
                        $"[DoorTimeout][RESET] state changed from DOOROPEN to {elevator.state}. " +
                        $"openSince={_doorOpenSince:HH:mm:ss}, openState={_doorOpenState}"
                    );
                }

                _doorOpenSince = DateTime.MinValue;
                _doorOpenState = string.Empty;
                return string.Empty;
            }

            // 3) 여기부터는 DOOROPEN_* 상태
            // 3-1) 처음 감지되었거나, 열려있는 층이 바뀌었다면 타이머 새로 시작
            if (_doorOpenSince == DateTime.MinValue)
            {
                _doorOpenSince = DateTime.Now;
                _doorOpenState = elevator.state;

                EventLogger.Info($"[DoorTimeout][START] door open detected. state={elevator.state}, openSince={_doorOpenSince:HH:mm:ss}");
                return string.Empty; // 시작한 tick에서는 바로 닫지 않음
            }

            if (_doorOpenState != elevator.state)
            {
                // 같은 DOOROPEN이라도 층이 바뀌는 건 "새로운 오픈"으로 간주
                EventLogger.Info(
                    $"[DoorTimeout][RESTART] door open state changed. prev={_doorOpenState}, now={elevator.state}. " +
                    $"prevOpenSince={_doorOpenSince:HH:mm:ss}"
                );

                _doorOpenSince = DateTime.Now;
                _doorOpenState = elevator.state;
                return string.Empty;
            }

            // 4) timeoutSec 확인
            int timeoutSec = ConfigData.ElevatorPolicy.DoorOpenHoldTimeoutSec;
            if (timeoutSec <= 0) timeoutSec = 30;

            var elapsed = DateTime.Now - _doorOpenSince;
            if (elapsed.TotalSeconds < timeoutSec)
                return string.Empty;

            // 5) active command 존재 여부 확인
            // - "커맨드가 없을 때만" 강제 닫기
            var all = _repository.Commands.GetAll();

            bool hasActive = false;

            if (all != null)
            {
                var active = all.FirstOrDefault(c =>
                    c != null &&
                    (c.state == nameof(CommandState.PENDING)
                  || c.state == nameof(CommandState.REQUEST)
                  || c.state == nameof(CommandState.EXECUTING))
                );

                if (active != null) hasActive = true;
            }

            if (hasActive)
            {
                // 문은 열려있고 timeout도 지났지만, 커맨드가 있으면 정상 흐름일 수 있다.
                // (예: HOLD 실행 중 / 작업 대기 / DoorClose 곧 들어올 예정)
                EventLogger.Info(
                    $"[DoorTimeout][SKIP] door open timeout but active commands exist. " +
                    $"state={elevator.state}, elapsedSec={(int)elapsed.TotalSeconds}, timeoutSec={timeoutSec}"
                );

                return string.Empty;
            }

            // 6) 최종: DOOROPEN + 커맨드 없음 + timeout 초과 → 강제 닫기
            EventLogger.Warn(
                $"[DoorTimeout][FORCE_CLOSE] door open too long with NO commands. " +
                $"state={elevator.state}, elapsedSec={(int)elapsed.TotalSeconds}, timeoutSec={timeoutSec}"
            );

            // (A) DOORCLOSE 커맨드 생성 (DB에 남겨 디버깅 쉽게)
            // - CreateCommand가 내부에서 중복 생성 방지(동일 action의 PENDING 존재 시 생성 금지)가 있으면 더 좋다.
            CreateCommand(nameof(SubType.DOORCLOSE), nameof(CommandAction.DOORCLOSE), nameof(CommandState.PENDING));

            // (B) 즉시 닫기 메시지 리턴
            var sendMsg = BuildSendMsgByActionName(nameof(CommandAction.DOORCLOSE));

            // (C) 중복 생성 방지: 한번 강제 닫기 트리거 후에는 타이머 리셋
            // - DoorClose 완료 이벤트가 늦게 오거나 누락되면 매 tick 생성될 수 있으니 방지
            _doorOpenSince = DateTime.MinValue;
            _doorOpenState = string.Empty;

            return sendMsg;
        }

        private bool IsDoorOpenState(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return false;

            if (state == nameof(State.DOOROPEN_B1F)) return true;
            if (state == nameof(State.DOOROPEN_1F)) return true;
            if (state == nameof(State.DOOROPEN_2F)) return true;
            if (state == nameof(State.DOOROPEN_3F)) return true;
            if (state == nameof(State.DOOROPEN_4F)) return true;
            if (state == nameof(State.DOOROPEN_5F)) return true;
            if (state == nameof(State.DOOROPEN_6F)) return true;

            return false;
        }

    }
}
