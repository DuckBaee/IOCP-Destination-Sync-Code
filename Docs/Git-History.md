# Git History

## 선별 원칙

모든 commit을 전시하지 않고 구조적 의미가 있는 단계만 선별했습니다. Evolution 파일은 현재 코드를 수정해 재현한 것이 아니라 해당 commit 당시 Git blob을 그대로 복사했습니다.

## 주요 이력

| Commit | 날짜 | 작성자 | 변화 | 분류 |
|---|---|---|---|---|
| `5bb4d1f` | 2024-06-06 13:48 | Duckbaee | 서버·Client 최초 구현, 0.5초 위치 전송 | EVOLUTION |
| `3926262` | 2024-06-06 17:08 | Duckbaee | 클릭 목적지 전송, 기존 Player 데이터 캐시 | EVOLUTION |
| `f9cf498` | 2024-06-06 17:38 | Duckbaee | 채팅 송수신 추가 시작 | Source history |
| `4ec7017` | 2024-06-06 18:56 | Duckbaee | `Text:` Broadcast와 Client 처리 | Source history |
| `deadb7d` | 2024-06-07 22:56 | Duckbaee | Disconnect 처리 추가 | Selected final |
| `4cd5b03` | 2024-06-11 19:30 | HWAN612 | 캐릭터 애니메이션·말풍선 | 제외 |
| `f7b9bc0` | 2024-06-11 19:52 | HWAN | Merge commit | 제외 |

## Evolution 선별

### `5bb4d1f` — Position Snapshot Prototype

선별 이유:

- 현재 위치를 실제로 0.5초마다 전송한 코드가 존재합니다.
- 최종 코드 설명만으로는 확인할 수 없는 AS-IS 구조입니다.
- 이후 변경과 직접 비교할 수 있습니다.

### `3926262` — Destination Sync Decision

선별 이유:

- 주기 Coroutine 제거가 diff에 남아 있습니다.
- 클릭과 Raycast가 전송 트리거로 추가됩니다.
- `playerPosition` 대신 `hit.point`가 전송됩니다.

## 최종 Source 기준점

최종 전시 코드는 `deadb7d`를 사용합니다.

이 commit은 본인 작성 이동·채팅·Disconnect가 포함되고, 팀원 작성 `PlayerMove`와 말풍선 코드가 합쳐지기 전입니다.

## 제외한 변경

| 변경 | 제외 이유 |
|---|---|
| 채팅 중간 commit을 별도 Evolution 단계로 제공 | 목적지 동기화의 핵심 구조 변경이 아님 |
| `4cd5b03` | 팀원 작성 코드 포함 |
| Merge commit | 구현 변화 증거가 아님 |
| 후속 로그인·닉네임 branch | 팀원 작성이며 현재 사례 범위 밖 |

## 원본 저장소

- Repository: <https://github.com/DuckBaee/IOCP-Chat-Server>
- 비교: <https://github.com/DuckBaee/IOCP-Chat-Server/compare/5bb4d1f...3926262>

## 복사 동일성

복사 단계에서 commit blob과 복사본을 SHA-256으로 비교했습니다. 상세 hash는 작업 보고서에서 관리하며 모든 선별 파일이 동일 판정을 받았습니다.
