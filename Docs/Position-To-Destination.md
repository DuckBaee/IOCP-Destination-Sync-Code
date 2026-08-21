# Position Snapshot to Destination Sync

이 프로젝트에서 가장 크게 다시 생각한 부분은 “Player 이동을 위해 무엇을 동기화해야 하는가”였습니다.

## 처음 구현한 방식

- Commit: [`5bb4d1f`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/5bb4d1f)
- 날짜: 2024-06-06 13:48
- 코드: [`Evolution/01_PositionSnapshot/NetworkManager.cs`](../Evolution/01_PositionSnapshot/NetworkManager.cs)

처음에는 Player의 현재 위치를 매 프레임 읽고 0.5초마다 서버로 보냈습니다.

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

하지만 수신 Client의 코드는 받은 좌표로 Transform을 보정하는 구조가 아니었습니다. 전달받은 좌표를 `NavMeshAgent.SetDestination(position)`에 넣어 이동 목적지로 사용하고 있었습니다.

```text
보내는 값: 현재 위치
사용하는 값: 이동 목적지
```

전송 데이터와 수신 코드가 같은 좌표를 서로 다른 의미로 사용하고 있었습니다.

## 목적지를 보내도록 변경

- Commit: [`3926262`](https://github.com/DuckBaee/IOCP-Chat-Server/commit/3926262)
- 날짜: 2024-06-06 17:08
- 코드: [`Evolution/02_DestinationSync/NetworkManager.cs`](../Evolution/02_DestinationSync/NetworkManager.cs)

클릭 이동 게임에서는 매 순간의 Transform보다 사용자가 선택한 목적지가 이동의 기준이라고 판단했습니다. 그래서 위치 전송 Coroutine을 제거하고 클릭했을 때 Raycast로 구한 좌표를 전송하도록 바꿨습니다.

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

메시지 형식은 유지하면서 payload의 의미를 변경했습니다.

```text
Before: P:ID,currentX,currentY,currentZ
After:  P:ID,destinationX,destinationY,destinationZ
```

## 변경 결과

| 항목 | 변경 전 | 변경 후 |
|---|---|---|
| 전송 시점 | 0.5초 Coroutine | Mouse Click |
| 전송 데이터 | 현재 Transform 위치 | `RaycastHit.point` |
| 이동 중 추가 전송 | 반복 전송 | 다음 클릭 전까지 없음 |
| Client 적용 | `SetDestination(position)` | `SetDestination(destination)` |
| Server 역할 | Broadcast | 마지막 payload 저장 후 Broadcast |

이 변경으로 송신 데이터와 `SetDestination()`이 같은 의미를 갖게 됐습니다. 기존 Client–Server–Client Broadcast 구조는 그대로 사용하면서 동기화할 데이터만 게임의 이동 방식에 맞게 바꿨습니다.

패킷 수와 트래픽을 별도로 측정하지 않았기 때문에 성능 최적화로 판단하지는 않았습니다. 제가 해결한 문제는 전송량의 수치 개선보다 이동 구조와 동기화 데이터 사이의 의미 불일치를 바로잡은 것입니다.
