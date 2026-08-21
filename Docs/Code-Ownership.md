# Code Ownership

## 본인 Git 계정

- `DuckBaee <49149806+DuckBaee@users.noreply.github.com>`
- `Duckbaee <joy1655817@gmail.com>`

두 작성자 표기는 동일한 본인 계정임을 확인했습니다.

팀원 표기:

- `HWAN612 <y66726379@gmail.com>`
- `HWAN <152396489+HWAN612@users.noreply.github.com>`

## 현재 main의 핵심 파일 blame

| 파일 | Duckbaee | HWAN612 | 판정 |
|---|---:|---:|---|
| `Server/IOCPServerC#/Program.cs` | 332 | 0 | 본인 작성 확인 |
| `NetworkManager.cs` | 226 | 2 | 공동 수정 |
| `PlayerTextManager.cs` | 13 | 4 | 공동 수정 |
| `PlayerMove.cs` | 0 | 70 | 타인 작성 |
| `UnityMainThreadDispatcher.cs` | 56 | 0 | 본인 작성 확인, 미사용 |

## Source 선별

| 전시 파일 | 원본 Commit | 작성자 상태 | 선별 이유 |
|---|---|---|---|
| `Source/Server/Program.cs` | `deadb7d` | 본인 작성 확인 | 서버 전체 흐름 |
| `Source/Client/NetworkManager.cs` | `deadb7d` | 본인 작성 확인 | 팀원 의존성 합류 전 최종 Client 코드 |
| `Source/UI/PlayerTextManager.cs` | `deadb7d` | 본인 작성 확인 | 본인이 작성한 채팅 입력·Disconnect 연결 |

## Evolution 선별

| 전시 파일 | 원본 Commit | 작성자 상태 |
|---|---|---|
| `Evolution/01_PositionSnapshot/NetworkManager.cs` | `5bb4d1f` | 본인 작성 확인 |
| `Evolution/02_DestinationSync/NetworkManager.cs` | `3926262` | 본인 작성 확인 |

## 제외한 팀원 코드

### `PlayerMove.cs`

`4cd5b03`에서 HWAN612가 추가했습니다.

포함 기능:

- NavMeshAgent velocity 기반 이동 애니메이션
- Sprite 좌우 반전
- 말풍선 표시
- 채팅 텍스트 자동 숨김

네트워크 목적지 적용 코드는 `PlayerMove`가 아니라 본인 작성 `NetworkManager.UpdatePlayerPosition()`에 있으므로 이 파일을 제외해도 핵심 사례 설명에 문제가 없습니다.

### 현재 Player Prefab

`4cd5b03`에서 Animator, `PlayerMove`, 말풍선 GameObject와 표현 Asset이 추가됐습니다. 코드 전시 저장소에는 Scene과 Prefab을 포함하지 않습니다.

### 로그인·닉네임

`origin/Dev`의 Login Server, Local Nickname, World Login 관련 commit은 HWAN612 작성이므로 제외했습니다.

## 미사용 본인 코드

`UnityMainThreadDispatcher.cs`는 본인 작성이지만 실제 호출 지점이 발견되지 않아 제외했습니다. 작성량보다 현재 사례를 이해하는 데 필요한 코드만 선별했습니다.
