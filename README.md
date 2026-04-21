# 🗿 Stone & Time

> **2D Metroidvania · Unity 6 (URP) · 재질 기반 전투**
> 흑백 페인터리 스타일의 메트로배니아. 플레이어는 3개의 슬롯에 3가지 재질(돌·나무·금속)을 조합해 자신만의 전투 루프를 만든다.

<p align="center">
  <img src="./Assets/Art/Reference/golem_master_sheet_v1.png" alt="Golem Master Sheet v7" width="720"/>
  <br/>
  <em>Master Sheet v7 — 그레이스케일 페인터리 레퍼런스</em>
</p>

---

## 🎮 개요

**Stone & Time**은 "한 번 설계한 재질이 탐험, 전투, 퍼즐, 환경 변화까지 전부 결정짓는" 메트로배니아입니다.
캐릭터의 **몸(Body)** 과 **무기(Weapon)** 에는 각각 3개의 슬롯이 있고, 각 슬롯에는 세 가지 재질 중 하나가 들어갑니다. 재질 조합은 공격 반응, 피격 반응, 지형 상호작용까지 전파되며, 같은 맵을 다른 빌드로 재탐험할 때 완전히 다른 경로를 열어줍니다.

```
 Body slots   : [ 머리 ] [ 몸통 ] [ 발 ]
 Weapon slots : [ 날 ]  [ 자루 ] [ 손잡이 ]
 Materials    : 돌  ·  나무  ·  금속
```

**핵심 모토**: *"조합은 적고, 의미는 깊게."*

---

## ✨ 특징

- **재질 × 재질 상성 매트릭스** — 공격자 재질과 대상 재질이 만나 `Bounce / Pierce / Shatter / Normal` 네 가지 결과 중 하나를 만든다.
- **3×3 슬롯 구성** — 조합의 폭은 좁지만, 한 조합이 전투·이동·퍼즐·환경 진화 전부에 영향을 준다.
- **환경 진화** — 플레이어의 빌드에 따라 일부 지형이 영구적으로 깎이거나 부서지며, 맵이 세션을 넘어 기억을 갖는다.
- **사륜안 / 시간 통찰** — 특정 조건에서 과거 또는 미래의 지형 상태를 잠시 보는 메커닉. 퍼즐과 보스전에서 핵심 레버로 쓰인다.
- **흑백 페인터리 아트** — 한 명의 2D 아티스트도 감당 가능하도록 컬러를 포기하고 톤과 실루엣에 집중.

<p align="center">
  <img src="./Assets/Art/Reference/golem_style_reference.png" alt="Style Reference" width="540"/>
  <br/>
  <em>스타일 레퍼런스 — 회색 톤 + 거친 브러시</em>
</p>

---

## 🛠 기술 스택

| 영역 | 사용 기술 |
|---|---|
| 엔진 | Unity 6 (6000.x) · URP |
| 언어 | C# (.NET Standard 2.1) |
| 2D | 2D Animation, 2D IK, Sprite Shape |
| 카메라 | Cinemachine |
| 입력 | Input System (new) |
| 테스트 | Unity Test Framework (EditMode + PlayMode) |
| 빌드 타깃 | Windows (MVP), 이후 Steam Deck 검토 |

---

## 🧪 현재 진행도

- **Phase 1 — 코어 루프** ✅
  플레이어 이동, 기본 공격 컨트롤러, 무기/바디 스왑 테스터, 더미 타겟.
- **Phase 2 — 전투 해석기** ✅
  `IDamageable`, `AttackOutcome`, `CombatResolver`, `HitStopManager` + 전투 흐름 EditMode/PlayMode 테스트.
- **Phase 3 — 환경 진화** ⏳
  재질 기반 파괴 가능 지형, 흔적 저장, 사륜안 프리뷰.
- **Phase 4 — 첫 보스 (Golem Prototype)** ⏳
  Master Sheet v7 기반 애니메이션, 페이즈 전이, 디자이너 핸드오프.

테스트 실행:
```bash
# Unity Test Runner → EditMode + PlayMode 모두 그린 상태 유지
```

---

## 📂 레포 구조

```
Assets/
├─ Art/Reference/          # 스타일 & 캐릭터 시트 레퍼런스
├─ Scripts/
│  ├─ Combat/              # IDamageable, CombatResolver, HitStop …
│  └─ Input/               # BodySwapTester, WeaponSwapTester
└─ Tests/
   ├─ EditMode/            # CombatResolverTests, DummyTargetTests
   └─ PlayMode/            # CombatFlowTests
Packages/                  # Unity 패키지 매니페스트
ProjectSettings/           # URP, Input System, 물리 설정
```

> 디자인 문서(ADR, 게임 디자인 코어, MVP 전략, 나노바나나 프롬프트 킷)는 별도 Notion 워크스페이스와 옵시디언 볼트에서 관리됩니다. 레포에는 실행 가능한 프로젝트만 둡니다.

---

## 👥 팀

| 역할 | 담당 |
|---|---|
| Lead / Programmer | **정석현** ([@sukh115](https://github.com/sukh115)) |
| 2D Designer | TBD |
| Sound Designer | **이도량** |

컨트리뷰터 온보딩 절차는 Notion 워크스페이스 내 `ONBOARDING` 문서를 참고해주세요.

---

## 📜 라이선스

현 시점에서는 **All Rights Reserved** (습작/포트폴리오 프로젝트). 외부 기여 전 Lead와 먼저 상의해주세요.

---

<p align="center">
  <sub>Made with Unity 6 · 돌과 시간, 그 사이의 이야기.</sub>
</p>
