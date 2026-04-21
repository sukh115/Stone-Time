// -----------------------------------------------------------------------------
// CombatFlowTests.cs
// Stone & Time — Phase 2 전투 통합 PlayMode 테스트
//
// 범위:
//   - 실제 씬 런타임에 Attacker + DummyTarget을 조립해 AttackController.TryAttack()을
//     호출하고 HP·이벤트·쿨다운·히트스탑을 전수 검증.
//   - EditMode 테스트(CombatResolverTests, DummyTargetTests)와 달리 Physics2D/Coroutine/
//     Time.timeScale 경로까지 포함.
//
// 왜 PlayMode인가:
//   - Physics2D.OverlapBoxNonAlloc 는 PlayMode에서만 실제 콜라이더를 감지한다.
//   - HitStopManager의 StartCoroutine / Time.timeScale 경로는 PlayMode에서만 유의미.
//
// 설계 노트:
//   - InputRouter는 PlayerInput SendMessages에 의존하지만, AttackController.TryAttack()은
//     public 이므로 입력 시스템을 우회해 직접 호출 가능.
//   - HitStopManager 싱글톤은 테스트 간 오염되지 않도록 SetUp/TearDown에서 ResetForTests.
//   - Time.timeScale은 OnDisable에서 복원되지만, 안전장치로 TearDown에서 명시적으로 1 세팅.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using StoneAndTime.Core;
using StoneAndTime.Parts;
using StoneAndTime.Combat;
using StoneAndTime.Input;
using Object = UnityEngine.Object;

namespace StoneAndTime.Tests.PlayMode
{
    public class CombatFlowTests
    {
        // =========================================================================
        // 공통 상태
        // =========================================================================
        private GameObject _attackerGo;
        private GameObject _targetGo;
        private EquipmentManager _attackerEquip;
        private AttackController _attackCtrl;
        private DummyTarget _target;

        private MaterialData _ice;      // 20
        private MaterialData _granite;  // 50
        private MaterialData _obsidian; // 80

        // =========================================================================
        // SetUp / TearDown
        // =========================================================================

        [SetUp]
        public void SetUp()
        {
            // Time.timeScale은 이전 테스트에서 오염될 수 있음 — 강제 초기화
            Time.timeScale = 1f;
            HitStopManager.ResetForTests();

            _ice      = MakeMat("얼음",   20);
            _granite  = MakeMat("화강암", 50);
            _obsidian = MakeMat("흑요석", 80);

            // --- Attacker ---
            _attackerGo = new GameObject("Attacker");
            _attackerGo.transform.position = Vector3.zero;
            _attackerEquip = _attackerGo.AddComponent<EquipmentManager>();
            _attackerGo.AddComponent<InputRouter>();              // AttackController RequireComponent
            _attackCtrl = _attackerGo.AddComponent<AttackController>();
            // Attacker의 EquipmentManager에 Context 주입 (없어도 WeaponPart.OnEquip은 no-op이지만
            // 향후 확장 안전용)
            _attackerEquip.Initialize(new GolemContext(_attackerGo, null, null));

            // --- Target ---
            // 기본 hitbox 중심: localCenter(0.8, 0), size(1.2, 1.2), facing +X.
            // => 월드 히트박스 = center(0.8, 0), size(1.2, 1.2). 타깃을 x=0.8에 배치.
            _targetGo = new GameObject("Target");
            _targetGo.transform.position = new Vector3(0.8f, 0f, 0f);
            _targetGo.AddComponent<EquipmentManager>();           // DummyTarget RequireComponent
            var col = _targetGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.5f);
            col.isTrigger = false;                                // OverlapBoxNonAlloc는 trigger 여부와 무관하게 탐지
            _target = _targetGo.AddComponent<DummyTarget>();
            // DummyTarget.Awake가 EquipmentManager.Initialize를 처리한다 (initialBodyMaterial이 null이어도 OK).
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackerGo != null) Object.Destroy(_attackerGo);
            if (_targetGo   != null) Object.Destroy(_targetGo);

            HitStopManager.ResetForTests();
            Time.timeScale = 1f;

            if (_ice      != null) Object.Destroy(_ice);
            if (_granite  != null) Object.Destroy(_granite);
            if (_obsidian != null) Object.Destroy(_obsidian);
        }

        // =========================================================================
        // 헬퍼
        // =========================================================================

        private static MaterialData MakeMat(string name, int hardness)
        {
            var m = ScriptableObject.CreateInstance<MaterialData>();
            m.displayName = name;
            m.hardness = hardness;
            return m;
        }

        /// <summary>DummyTarget의 SerializeField private maxHp를 강제 세팅하고 ResetHp.</summary>
        private void SetTargetMaxHp(int value)
        {
            var field = typeof(DummyTarget).GetField("maxHp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "DummyTarget.maxHp 필드가 보이지 않음. 이름이 바뀌었나?");
            field.SetValue(_target, value);
            _target.ResetHp();
        }

        /// <summary>AttackController의 SerializeField private hitStopFrames을 0으로 세팅(쿨다운 테스트용).</summary>
        private void DisableHitStop()
        {
            SetHitStopFrames(0);
        }

        /// <summary>hitStopFrames와 blockStopFrames을 명시적으로 고정 — 기본값 변경에 대한 테스트 내성.</summary>
        private void SetHitStopFrames(int frames)
        {
            var field = typeof(AttackController).GetField("hitStopFrames", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) field.SetValue(_attackCtrl, frames);
            var fieldB = typeof(AttackController).GetField("blockStopFrames", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldB != null) fieldB.SetValue(_attackCtrl, frames);
        }

        // =========================================================================
        // 해피 패스 — Hit
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_ObsidianVsIce_ReducesHpBy60()
        {
            SetTargetMaxHp(100);
            _attackerEquip.Equip(new WeaponPart(_obsidian));
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            DisableHitStop(); // Time.timeScale 오염 방지

            yield return null; // Awake/Start 1프레임 흐르게

            int beforeHp = _target.CurrentHp;
            int receivedDmg = -1;
            _target.OnHit += (d, k) => receivedDmg = d;

            bool attacked = _attackCtrl.TryAttack();

            Assert.IsTrue(attacked, "TryAttack은 성공해야 한다.");
            Assert.AreEqual(60, receivedDmg, "데미지 = 80 - 20 = 60.");
            Assert.AreEqual(beforeHp - 60, _target.CurrentHp);
            Assert.IsFalse(_target.IsDead);
        }

        // =========================================================================
        // Blocked
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_IceVsObsidian_BlockedNoDamage()
        {
            SetTargetMaxHp(50);
            _attackerEquip.Equip(new WeaponPart(_ice));           // hardness 20
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_obsidian)); // hardness 80
            DisableHitStop();

            yield return null;

            int blockCount = 0;
            _target.OnBlocked += (dir) => blockCount++;
            int hitCount = 0;
            _target.OnHit += (d, k) => hitCount++;

            bool attacked = _attackCtrl.TryAttack();

            Assert.IsTrue(attacked, "TryAttack은 쿨다운 통과로 true.");
            Assert.AreEqual(0, hitCount, "OnHit는 발화되면 안 됨.");
            Assert.AreEqual(1, blockCount, "OnBlocked가 정확히 한 번.");
            Assert.AreEqual(50, _target.CurrentHp, "HP는 변하지 않음.");
        }

        // =========================================================================
        // 무방어(Body null) → Hit with attacker hardness
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_TargetWithNoBody_HitsForAttackerHardness()
        {
            SetTargetMaxHp(500);
            _attackerEquip.Equip(new WeaponPart(_granite)); // hardness 50
            // target body 미장착
            DisableHitStop();

            yield return null;

            bool attacked = _attackCtrl.TryAttack();

            Assert.IsTrue(attacked);
            Assert.AreEqual(450, _target.CurrentHp, "무방어 피격은 attacker hardness(50) 만큼.");
        }

        // =========================================================================
        // 무기 없음
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_WithoutWeapon_ReturnsFalseAndNoHpChange()
        {
            SetTargetMaxHp(100);
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            DisableHitStop();

            // 무기 경고 로그는 예상된 거동
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No weapon equipped"));

            yield return null;

            bool attacked = _attackCtrl.TryAttack();

            Assert.IsFalse(attacked);
            Assert.AreEqual(100, _target.CurrentHp);
        }

        // =========================================================================
        // 쿨다운
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_WithinCooldown_SecondCallReturnsFalse()
        {
            SetTargetMaxHp(1000);
            _attackerEquip.Equip(new WeaponPart(_obsidian));
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            DisableHitStop();

            yield return null;

            int hitCount = 0;
            _target.OnHit += (d, k) => hitCount++;

            bool first  = _attackCtrl.TryAttack();
            bool second = _attackCtrl.TryAttack(); // 즉시 재호출 — 쿨다운 진행 중

            Assert.IsTrue(first,   "첫 공격은 쿨다운 통과.");
            Assert.IsFalse(second, "즉시 재공격은 쿨다운에 막혀야 함.");
            Assert.AreEqual(1, hitCount, "OnHit는 첫 공격에서만 한 번.");
        }

        // =========================================================================
        // 히트스탑 — Time.timeScale 조작이 실제로 일어나는지
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_OnHit_FreezesTimeScaleAndRestores()
        {
            SetTargetMaxHp(100);
            _attackerEquip.Equip(new WeaponPart(_obsidian));
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            // hitStopFrames를 테스트 내에서 명시적으로 고정 — Inspector 기본값이 바뀌어도 본 테스트는 안정
            SetHitStopFrames(3);

            yield return null;

            Assert.AreEqual(1f, Time.timeScale, "초기 timeScale은 1.");

            bool attacked = _attackCtrl.TryAttack();
            Assert.IsTrue(attacked);

            // Freeze 직후: timeScale = 0
            Assert.IsTrue(HitStopManager.Instance.IsFrozen, "Hit 직후 Freeze 상태여야 함.");
            Assert.AreEqual(0f, Time.timeScale, "히트스탑 중에는 timeScale=0.");

            // 자동 해제 대기 — IsFrozen 플래그를 bounded loop로 폴링 (yield return null은 timeScale=0에서도 돈다)
            const int maxWaitFrames = 60; // 안전 상한. 현재 프레임 3으로도 충분하지만 여유를 둠.
            int waited = 0;
            while (HitStopManager.Instance.IsFrozen && waited < maxWaitFrames)
            {
                yield return null;
                waited++;
            }

            Assert.IsFalse(HitStopManager.Instance.IsFrozen, $"일정 프레임({maxWaitFrames}) 안에 자동 해제되어야 함.");
            Assert.AreEqual(1f, Time.timeScale, "원래 timeScale(=1)로 복원.");
        }

        // =========================================================================
        // 치명타(HP 전소) → OnDied 정확히 한 번
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_KillsTarget_FiresOnDiedOnce()
        {
            SetTargetMaxHp(10);                                  // 한 방에 죽는 HP
            _attackerEquip.Equip(new WeaponPart(_obsidian));     // damage = 80 - 20 = 60
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            DisableHitStop();

            yield return null;

            int diedCount = 0;
            _target.OnDied += () => diedCount++;

            _attackCtrl.TryAttack();

            Assert.AreEqual(0, _target.CurrentHp);
            Assert.IsTrue(_target.IsDead);
            Assert.AreEqual(1, diedCount);
        }

        // =========================================================================
        // Scan 누락 — 타깃이 히트박스 바깥에 있으면 NoTarget
        // =========================================================================

        [UnityTest]
        public IEnumerator Attack_TargetOutsideHitbox_NoHit()
        {
            SetTargetMaxHp(100);
            _attackerEquip.Equip(new WeaponPart(_obsidian));
            _targetGo.GetComponent<EquipmentManager>().Equip(new BodyPart(_ice));
            DisableHitStop();

            // 타깃을 히트박스 바깥으로 이동 (x=10 은 hitbox(0.8 ± 0.6) 바깥)
            _targetGo.transform.position = new Vector3(10f, 0f, 0f);

            yield return null;
            // Physics2D 월드 업데이트 확보 — SyncTransforms로 콜라이더 위치 즉시 반영
            Physics2D.SyncTransforms();

            int hitCount = 0;
            _target.OnHit += (d, k) => hitCount++;

            _attackCtrl.TryAttack();

            Assert.AreEqual(0, hitCount, "히트박스 바깥 타깃은 감지되지 않음.");
            Assert.AreEqual(100, _target.CurrentHp);
        }
    }
}
