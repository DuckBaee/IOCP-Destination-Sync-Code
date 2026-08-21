# Code Ownership

이 프로젝트는 팀 작업이었기 때문에 포트폴리오 저장소에는 제가 직접 작성한 네트워크 코드만 분리했습니다.

## 제 Git 작성자 정보

- `DuckBaee <49149806+DuckBaee@users.noreply.github.com>`
- `Duckbaee <joy1655817@gmail.com>`

프로젝트 기간에 두 가지 이메일을 사용했지만 모두 제 Commit입니다.

## 포트폴리오에 포함한 코드

| 파일 | 원본 Commit | 제가 구현한 내용 |
|---|---|---|
| `Source/Server/Program.cs` | `deadb7d` | 연결 수락, Player ID, 비동기 Receive/Send, Broadcast |
| `Source/Client/NetworkManager.cs` | `deadb7d` | Client 연결, 이동 입력, 메시지 파싱, Player 생성·이동 |
| `Source/UI/PlayerTextManager.cs` | `deadb7d` | 채팅 입력과 Disconnect UI 연결 |
| `Evolution/01_PositionSnapshot/NetworkManager.cs` | `5bb4d1f` | 초기 위치 반복 전송 구조 |
| `Evolution/02_DestinationSync/NetworkManager.cs` | `3926262` | 클릭 목적지 전달 구조 |

`Source`와 `Evolution`의 C# 파일은 원본 Commit에서 수정하지 않고 복사했습니다.

## 포함하지 않은 팀원 작업

`4cd5b03` 이후 추가된 다음 기능은 팀원 HWAN612의 작업이므로 이 저장소에 포함하지 않았습니다.

- `PlayerMove.cs`
- NavMeshAgent velocity 기반 이동 애니메이션
- Sprite 방향 전환
- 말풍선 표시와 자동 숨김
- Player Prefab의 Animator와 표현 Asset
- 로그인과 닉네임 기능

목적지 메시지를 받아 `NavMeshAgent.SetDestination()`을 호출하는 코드는 제가 작성한 `NetworkManager.UpdatePlayerPosition()`에 있습니다. 팀원의 표현 기능을 제외해도 이 저장소에 담은 이동 동기화 흐름은 그대로 확인할 수 있습니다.

제가 작성한 `UnityMainThreadDispatcher.cs`도 실제 호출되는 지점이 없어 이번 저장소에서는 제외했습니다. 코드의 양보다 이동·채팅 동기화 과정에서 실제로 사용한 코드만 남겼습니다.
