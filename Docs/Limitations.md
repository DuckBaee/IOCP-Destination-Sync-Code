# Technical Limitations

이 프로젝트는 TCP 기반 멀티플레이 구조를 처음 구현한 프로토타입입니다. 코드를 다시 검토하면서 확인한 한계를 정리했습니다.

## TCP Framing

Client는 [`WriteLine()`](../Source/Client/NetworkManager.cs#L182-L200)으로 newline을 붙이지만 서버의 [`ProcessReceive()`](../Source/Server/Program.cs#L95-L138)는 한 번의 `ReceiveAsync` 결과를 메시지 하나로 처리합니다.

```text
Receive 1: P:ID1,10.0,
Receive 2: 0.0,20.0\nText:ID1,Hello\n
```

Client별 누적 Buffer와 newline 단위 Frame 추출이 없기 때문에 메시지가 분할되거나 여러 메시지가 한 번에 도착하면 파싱에 실패할 수 있습니다.

## Parsing과 Validation

- `Split(':')`, `Split(',')`에 의존합니다.
- 채팅에 `:`가 들어가면 Client가 메시지를 정상 처리하지 못할 수 있습니다.
- 좌표 직렬화와 `float.Parse()`가 실행 환경의 문화권 설정에 영향을 받습니다.
- 서버에서 좌표 개수, 숫자 형식, 값의 범위를 검증하지 않습니다.

문자열 프로토콜을 유지한다면 명확한 Frame 규칙과 escaping, `InvariantCulture` 적용이 필요합니다.

## Player Identity

서버의 [메시지 처리 코드](../Source/Server/Program.cs#L95-L127)는 메시지 안의 Player ID와 실제로 메시지를 보낸 Socket을 비교하지 않습니다. 따라서 Client가 다른 ID를 넣은 이동·채팅·Disconnect 메시지를 보낼 수 있습니다.

현재 구조에서는 연결 시 발급한 ID를 Socket 상태에 저장하고, Client가 보낸 ID가 아니라 서버가 관리하는 ID를 기준으로 처리해야 합니다.

## Server Authority

서버는 목적지를 저장하고 Broadcast할 뿐 다음 내용을 처리하지 않습니다.

- 목적지와 이동 속도 검증
- NavMesh 경로 계산
- Player 위치 시뮬레이션
- authoritative position 생성

서버를 경유하지만 서버 권위 이동 구조는 아닙니다.

## Position Correction

클릭 이후 실제 Player 위치를 서버에 다시 보내지 않습니다. 각 Client의 `NavMeshAgent`가 같은 목적지까지 독립적으로 이동하므로 시작 위치, NavMesh, 장애물 회피와 Frame Timing 차이로 생긴 오차를 교정할 수 없습니다.

네트워크 위치 Interpolation과 Client Prediction도 구현하지 않았습니다. 화면에서 보이는 부드러운 이동은 네트워크 보간이 아니라 각 Client의 `NavMeshAgent`가 만듭니다.

## Late Join

서버의 `clientPositions`에는 마지막 `P:` payload가 저장됩니다. 목적지 동기화로 변경한 이후 이 값은 실제 현재 위치가 아니라 마지막 목적지일 수 있습니다.

이동 중 새로운 Client가 접속하면 기존 Player가 실제 이동 위치가 아닌 목적지에서 생성될 수 있습니다.

## Send Queue와 Partial Send

서버는 [Broadcast 시 Socket마다 바로 `SendAsync()`](../Source/Server/Program.cs#L225-L270)를 호출합니다.

- Socket별 Send Queue와 backpressure 정책이 없습니다.
- `ProcessSend()`에서 일부 Bytes만 전송됐을 때 나머지를 이어 보내지 않습니다.
- EventArgs Pool이 비어 `Pop()`이 null을 반환할 때의 처리가 없습니다.

## Disconnect

- 명시적인 `Disconnected:` 메시지만 다른 Client에 전달됩니다.
- 비정상 종료는 Broadcast되지 않습니다.
- `CloseClientSocket()`의 `socket.Close()`와 `semaphore.Release()`가 주석 처리돼 있습니다.
- Client의 `ReadLineAsync()`가 null을 반환해도 listener loop가 계속됩니다.
- 연결 종료 시 `Players` Dictionary에서는 ID를 제거하지 않습니다.

## Client 클래스 책임

`NetworkManager` 한 클래스가 입력, TCP 연결, 직렬화, 파싱, Player 생성, 이동 적용, 채팅과 Disconnect를 모두 담당합니다.

프로토타입 이후 구조를 확장한다면 Transport, Protocol Parser, Player Registry와 Gameplay Input 책임을 분리해야 합니다. 이 저장소의 C# 파일은 당시 구현과 Git History를 그대로 보여주기 위해 리팩터링하지 않았습니다.
