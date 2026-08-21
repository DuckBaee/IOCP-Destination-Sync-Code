# Technical Limitations

이 문서는 원본 프로토타입 코드의 한계를 기록합니다. `Source`와 `Evolution` 코드는 역사적 증거 보존을 위해 수정하지 않았습니다.

## TCP Framing

Client는 `WriteLine()`으로 newline을 붙이지만 서버는 한 번의 `ReceiveAsync` 결과를 메시지 하나로 가정합니다.

TCP에서는 다음 상황이 가능합니다.

```text
Receive 1: P:ID1,10.0,
Receive 2: 0.0,20.0\nText:ID1,Hello\n
```

현재 서버에는 Client별 누적 buffer와 newline 단위 frame 추출이 없습니다. 메시지가 분할되거나 합쳐지면 파싱이 실패하거나 payload가 섞일 수 있습니다.

## Delimiter와 숫자 Parsing

- `Split(':')`와 `Split(',')`에 의존합니다.
- 채팅에 `:`가 포함되면 Client가 메시지를 무시하거나 서버에서 잘릴 수 있습니다.
- 좌표 직렬화 `F2`와 `float.Parse()`가 현재 문화권 설정을 사용합니다.
- 소수점 구분자가 쉼표인 환경에서는 field delimiter와 충돌할 수 있습니다.
- Server는 좌표 개수, 숫자 형식, 값 범위를 검증하지 않습니다.

## Identity Validation

Server는 메시지에 적힌 Player ID와 실제 송신 Socket을 비교하지 않습니다.

따라서 Client가 다른 ID를 담은 이동·채팅·Disconnect 메시지를 보낼 수 있습니다. Server가 연결 시 발급한 ID를 Socket 상태에서 조회해 사용해야 하지만 원본에는 구현돼 있지 않습니다.

## Server Authority

Server는 목적지를 저장하고 Broadcast할 뿐 다음을 수행하지 않습니다.

- 목적지 유효성 검증
- NavMesh 경로 계산
- 이동 속도 검증
- 실제 위치 시뮬레이션
- authoritative position 생성

따라서 서버 경유 구조이지만 authoritative movement server는 아닙니다.

## Position Correction

클릭 이후 실제 Player 위치를 서버에 다시 전송하지 않습니다. 각 Client가 동일 목적지로 자신의 NavMeshAgent를 이동시키므로 다음 차이를 교정할 수 없습니다.

- 시작 위치 차이
- NavMesh 상태 차이
- 장애물·회피 결과 차이
- frame timing 차이

## Interpolation과 Prediction

- 네트워크 위치 interpolation은 없습니다.
- Client prediction도 없습니다.
- 부드러운 움직임은 네트워크 보간이 아니라 각 Client의 NavMeshAgent가 생성합니다.
- 조작 Client도 Server가 Broadcast한 `P:`를 받은 뒤 `SetDestination()`을 호출합니다.

## Late Join 상태

`clientPositions`라는 이름과 달리 목적지 전환 이후 저장 값은 마지막으로 전송된 목적지일 수 있습니다.

신규 Client는 이 값을 `I:`로 받아 원격 Player의 생성 위치로 사용합니다. 실제 Player가 목적지로 이동 중이라면 신규 Client가 실제 현재 위치가 아닌 목적지에서 Player를 생성할 수 있습니다.

## Send Queue와 Partial Send

Server는 Broadcast 시 각 Socket에 바로 `SendAsync()`를 호출합니다.

- Socket별 send queue가 없습니다.
- backpressure 정책이 없습니다.
- `ProcessSend()`가 `BytesTransferred`를 확인하지 않습니다.
- 일부 데이터만 전송됐을 때 나머지를 이어 보내지 않습니다.
- EventArgs Pool이 비면 `Pop()`이 null을 반환하지만 호출 측 null 검사가 없습니다.

## Disconnect

- 명시적 `Disconnected:` 메시지는 Broadcast됩니다.
- 비정상 종료 경로에서는 다른 Client에 Disconnect가 전달되지 않습니다.
- `CloseClientSocket()`의 `socket.Close()`와 `semaphore.Release()`가 주석 처리돼 있습니다.
- Client의 `ReadLineAsync()`가 null을 반환해도 listener loop가 계속됩니다.
- Client `Players` Dictionary에서 제거하지 않고 GameObject만 Destroy합니다.

## Concurrency

- 여러 Accept callback에서 `++playerIdCounter`를 수행하지만 원자적 연산이 아닙니다.
- 하나의 EventArgs Pool을 Receive와 Send가 함께 사용합니다.
- 연결 수가 늘면 Receive용 EventArgs가 Pool을 점유해 Broadcast용 객체가 부족해질 수 있습니다.
- `MaxClients = 250`은 성능이나 안정성이 검증된 지원 수치가 아닙니다.

## Client 구조

`NetworkManager`는 다음 책임을 동시에 가집니다.

- 입력
- TCP 연결 수명주기
- 메시지 직렬화와 파싱
- Player 생성
- Player Registry
- NavMesh 이동 적용
- 채팅
- Disconnect

클래스 책임이 크지만 이번 저장소는 과거 원본 코드의 검토를 목적으로 하므로 분리하거나 재작성하지 않습니다.

## 검증되지 않은 주장

저장소에는 다음 자료가 없습니다.

- 4 Client 실행 로그·영상·스크린샷
- 패킷 수 비교
- 트래픽 바이트 측정
- 지연시간 측정
- 부하 테스트
- 자동화 테스트

따라서 다음 표현은 사용하지 않습니다.

- 최대 4 Client 지원
- 네트워크 최적화
- 트래픽 감소 검증
- 고성능 서버
- 완전한 위치 동기화
- 서버 권위 이동
