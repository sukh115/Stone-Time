# Contributing to Stone & Time

Stone & Time에 기여하는 방법. 현재는 Lead 1인 + Contributors 2인 (아트·사운드) 체제이지만, 앞으로 합류하는 사람과 Future-Lead 자신을 위한 규칙을 여기 남긴다.

> 🗿 **프로젝트 허브**: [Notion Workspace](https://www.notion.so/Stone-Time-3499037976b981338679eff61c6f89c4) — 기획·디자인·ADR·태스크는 여기서 관리.

## 역할 경계

- **Lead Developer (정석현)** — 엔진·코드·빌드·기획·조율·최종 머지 권한
- **Art Contributor** — `Assets/Art/` · `Assets/Prefabs/` 하위 + `Assets/Data/Materials/` 의 VFX 필드
- **Sound Contributor (이도량)** — `Assets/Audio/` 하위 + `Assets/Data/Materials/` 의 SFX 필드

**코드 수정(`Assets/Scripts/`)·아키텍처 결정은 Lead 단독 권한**. Contributors는 에셋 레이어에 한정한다.

## 브랜치 전략

```
main         — 항상 빌드 가능. 보호 브랜치
dev          — 통합 브랜치
feat/<name>  — 기능 브랜치 (병합 후 삭제)
art/<name>   — 아트 에셋 브랜치
sfx/<name>   — 사운드 에셋 브랜치
```

머지는 **Lead 리뷰 + 통합 빌드 통과** 후에만 진행.

## 커밋 메시지

영어, 현재형 동사로 시작. Conventional Commits 스타일.

```
feat(combat): add hardness comparison in OverlapBox resolver
fix(input): prevent diagonal input leak during snap
art: add granite foot piece sprites (MD_Granite references)
sfx: replace ice impact with v2 clip set
docs(adr): accept ADR-0005 for timeScale UI split
chore: bump Unity packages to 2D Animation 10.1.1
```

접두사: `feat`, `fix`, `refactor`, `art`, `sfx`, `docs`, `chore`, `test`.

## 코드 규약 (C#)

- Unity C# 컨벤션 + Rider/ReSharper 기본 룰
- `private` 필드는 `_camelCase`, `public`·프로퍼티는 `PascalCase`
- `[SerializeField] private` 선호, `public` 필드 지양
- `MonoBehaviour` 의존성은 생성자 아닌 `[SerializeField]` 주입
- 비즈니스 로직은 `MonoBehaviour`에서 분리 → POCO 클래스 + 테스트
- `async void` 금지. `async Task` 또는 Unity 코루틴
- LINQ는 프레임당 호출 경로에서 지양 (알로케이션)

## 테스트

- Play Mode 테스트: `Assets/Tests/PlayMode/`
- Edit Mode 테스트: `Assets/Tests/EditMode/`
- 핵심 시스템은 테스트 필수 (EquipmentManager, CombatResolver, InputRouter, TimeControl)
- 아트·사운드 리소스 테스트는 제외 — Lead가 빌드에서 스모크 테스트로 검증

## ADR 프로세스

아키텍처·기술·팀 차원의 결정은 ADR(Architecture Decision Record)로 기록한다.

- 위치: Notion 워크스페이스 내 `7. 결정 로그 & 회의록` 페이지 및 Lead 볼트 `design/adr/`
- 번호는 순차. 기존 ADR을 대체하면 `superseded-by: ADR-NNNN` 메타 표기
- 현재까지: ADR-0001 (Unity), 0002 (팀·스코프), 0003 (슬롯), 0004 (MaterialData)

## 밸런싱 데이터 수정

`Assets/Data/Materials/*.asset` 편집은 **Lead 승인 필요**. 급한 밸런싱 제안은 Notion 워크스페이스 `7. 결정 로그 & 회의록` 페이지에 의견 기록 후 Lead와 협의.

## 런타임 ScriptableObject 수정 금지

`MaterialData`는 런타임에 읽기 전용. 수정 필요 시 `Instantiate(materialData)`로 복사본을 만들어 사용. 프로덕션 빌드에서 원본 `.asset` 파일이 영구 변경되는 것을 막기 위함 (ADR-0004 참조).

## Git LFS 대상

다음 확장자는 Git LFS 추적 (`.gitattributes`에 등록됨):

- **이미지**: `*.psd`, `*.png`, `*.jpg`, `*.jpeg`, `*.tif`, `*.gif`, `*.bmp`
- **오디오**: `*.wav`, `*.ogg`, `*.mp3`, `*.flac`
- **영상·3D**: `*.fbx`, `*.mp4`, `*.mov`

## 통합 빌드 체크리스트

주 1회 Lead가 돌리는 스모크 테스트:

1. `git pull origin dev` + Unity 프로젝트 열기
2. 씬: `TestScene_Integration` 로드 → Play
3. 4방향 이동·공격 Z 짧게/홀드 작동 여부
4. 재질 교체 시 `Rigidbody2D` 값 변화 로그 확인
5. 콘솔 Error·Warning 0건
6. 통과 시 `dev` → `main` 머지

## 자주 묻는 결정 (FAQ)

- **"이 기능 MVP에 넣을까?"** → ADR-0002 스코프 표 확인. 표 밖이면 post-MVP.
- **"새 재질 추가하고 싶은데?"** → `Assets/Data/Materials/MD_<Name>.asset` 만들고 수치 채우면 끝. 코드 변경 불필요.
- **"Unity 버전 올려도 돼?"** → 안 됨. `Packages/manifest.json`은 Lead만 수정.
- **"망토 슬롯 쓰고 싶은데?"** → Post-MVP. `SlotType.Cape` enum은 이미 있지만 시스템이 아직 반응 안 함.

## 신규 Contributor

합류 직후 먼저 읽을 문서: [ONBOARDING.md](./ONBOARDING.md)
