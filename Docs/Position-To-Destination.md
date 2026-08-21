# Position Snapshot to Destination Sync

## 결론

이 변경의 핵심은 `SetDestination()`을 새로 도입한 것이 아닙니다. 수신 측에 이미 존재하던 `SetDestination()`의 의미에 맞춰 송신 데이터와 전송 시점을 바꾼 것입니다.

```text
Current Position Snapshot
→ Click Destination Command
```

## 01. Position Snapshot Prototype

- Commit: `5bb4d1f`
- 날짜: 2024-06-06 13:48:59 +09:00
- 원본 파일: `Evolution/01_PositionSnapshot/NetworkManager.cs`

Client는 매 프레임 `gameObject.transform.position`을 읽고 0.5초마다 서버에 전송했습니다.

```csharp
Vector3 playerPosition;
public float sendInterval = 0.5f;

void Update()
{
    playerPosition = gameObject.transform.position;
}

IEnumerator SendPositionRoutine()
{
    while (true)
    {
        SendPositionToServer();
        yield return new WaitForSeconds(sendInterval);
    }
}
```

수신 Client는 초기 버전에서도 전달받은 좌표를 `NavMeshAgent.SetDestination(position)`에 사용했습니다.

따라서 다음 의미 불일치가 있었습니다.

```text
Sender: current transform snapshot
Receiver: navigation destination
```

## 02. Destination Sync Decision

- Commit: `3926262`
- 날짜: 2024-06-06 17:08:14 +09:00
- 원본 파일: `Evolution/02_DestinationSync/NetworkManager.cs`

`playerPosition`, `sendInterval`, `SendPositionRoutine()`이 제거되고 클릭 이벤트가 추가됐습니다.

```csharp
void Update()
{
    if(Input.GetMouseButtonDown(0))
    {
        GetClickedPosition();
    }
}

if(Physics.Raycast(ray, out hit))
{
    SendPositionToServer(hit.point);
}
```

`P:` 메시지의 형식은 유지됐지만 클릭 이동에서 payload의 의미가 바뀌었습니다.

```text
Before: P:ID,currentX,currentY,currentZ
After:  P:ID,destinationX,destinationY,destinationZ
```

## 변경 전후

| 항목 | Position Snapshot | Destination Sync |
|---|---|---|
| 전송 트리거 | 0.5초 Coroutine | Mouse click |
| 데이터 출처 | `gameObject.transform.position` | `RaycastHit.point` |
| 이동 중 전송 | 계속 전송 | 추가 클릭 전까지 없음 |
| 수신 처리 | `SetDestination(position)` | `SetDestination(destination)` |
| 서버 역할 | Broadcast | 캐시 후 Broadcast |

## 설명 가능한 결과

- 주기적인 현재 위치 전송을 입력 이벤트 기반 목적지 전달로 변경했습니다.
- 송신 payload의 의미와 수신 측 NavMesh 처리 방식이 일치하게 됐습니다.
- Client–Server–Client의 기존 Broadcast 경로는 재사용했습니다.

## 설명하면 안 되는 결과

- 트래픽을 최적화했다.
- 성능이 향상됐다.
- 네트워크 사용량이 특정 비율 감소했다.
- 완전한 위치 동기화를 구현했다.
- 서버 권위 이동을 구현했다.

패킷 수, 바이트 사용량, 지연시간 측정 자료가 없기 때문입니다.

## 검증 명령

```powershell
git show 5bb4d1f:Unity/GameServerPrograming_Finals/Assets/Scripts/Player/NetworkManager.cs

git show 3926262:Unity/GameServerPrograming_Finals/Assets/Scripts/Player/NetworkManager.cs

git diff 5bb4d1f 3926262 -- "Unity/GameServerPrograming_Finals/Assets/Scripts/Player/NetworkManager.cs"
```
