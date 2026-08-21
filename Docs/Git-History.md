# Git History

위치 반복 전송에서 목적지 전달 구조로 바뀐 과정을 실제 Commit 기준으로 정리했습니다. `Evolution`의 코드는 현재 코드를 다시 작성한 것이 아니라 각 시점의 원본 Git blob을 그대로 복사했습니다.

## 변경 이력

| Commit | 날짜 | 작성자 | 제가 구현한 변화 |
|---|---|---|---|
| [`5bb4d1f`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/5bb4d1f) | 2024-06-06 13:48 | Duckbaee | 별도 서버와 Client 연결, 0.5초 위치 전송 |
| [`3926262`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/3926262) | 2024-06-06 17:08 | Duckbaee | 클릭 목적지 전송과 기존 Player payload 저장 |
| [`f9cf498`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/f9cf498) | 2024-06-06 17:38 | Duckbaee | 채팅 송수신 구현 시작 |
| [`4ec7017`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/4ec7017) | 2024-06-06 18:56 | Duckbaee | `Text:` Broadcast와 Client 처리 |
| [`deadb7d`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/deadb7d) | 2024-06-07 22:56 | Duckbaee | 명시적 Disconnect 처리 추가 |

## Position Snapshot — `5bb4d1f`

이 Commit에는 현재 위치를 0.5초마다 전송하는 초기 구조가 남아 있습니다.

- `gameObject.transform.position`을 매 프레임 저장
- `SendPositionRoutine()`에서 0.5초마다 전송
- 수신 측에서는 전달받은 좌표에 `SetDestination()` 호출

## Destination Sync — `3926262`

이 Commit에서 현재 위치 Coroutine을 제거하고 클릭 목적지를 전송하도록 바꿨습니다.

- `playerPosition`, `sendInterval`, `SendPositionRoutine()` 제거
- 마우스 클릭과 `Physics.Raycast` 추가
- `hit.point`를 `P:` 메시지로 전송
- 서버가 마지막 `P:` payload를 저장한 뒤 Broadcast

두 Commit의 `NetworkManager.cs`를 `Evolution/01_PositionSnapshot`과 `Evolution/02_DestinationSync`에 각각 보관해 변경 전후 코드를 바로 비교할 수 있도록 구성했습니다.

## Source 기준점 — `deadb7d`

`Source`에는 이동, 채팅, Disconnect가 함께 동작하던 `deadb7d` 시점의 코드를 담았습니다. 이 시점은 팀원이 작업한 캐릭터 애니메이션과 말풍선 기능이 합쳐지기 전이므로 제가 담당한 네트워크 흐름을 분리해서 보여줄 수 있습니다.

원본 팀 저장소는 [DuckBaee/IOCP-Chat-Server](https://github.com/DuckBaee/IOCP-Chat-Server)에서 확인할 수 있습니다.
