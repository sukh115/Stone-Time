// -----------------------------------------------------------------------------
// DummyTargetTests.cs
// Stone & Time — DummyTarget MonoBehaviour EditMode 테스트
//
// 범위:
//   - Awake — CurrentHp = maxHp, initialBodyMaterial 설정 시 BodyPart 자동 장착
//   - ApplyHit — HP 감소, damage <= 0 no-op, IsDead 상태에서 no-op
//   - ApplyBlockReaction — HP 불변, IsDead에서 no-op
//   - 이벤트 — OnHit, OnBlocked, OnDied (정확히 한 번)
//   - ResetHp
//   - BodyMaterial — EquipmentManager 상태에 따라 변경
//
// 설계 노트:
//   - maxHp는 [SerializeField private]. 테스트에서 제어하려고 리플렉션으로 접근.
//   - DummyTarget.Awake는 MonoBehaviour라 AddComponent 직후 Unity가 자동 호출.
//     따라서 리플렉션으로 maxHp를 먼저 세팅한 뒤 AddComponent는 불가능.
//     대신 AddComponent → 리플렉션으로 maxHp 덮어쓰기 → ResetHp 호출 순서로 맞춘다.
// -----------------------------------------------------------------------------

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Parts;
using StoneAndTime.Combat;
using Object = UnityEngine.Object;

namespace StoneAndTime.Tests
{
    public class DummyTargetTests
    {
        // =========================================================================
        // 공통 헬퍼
        // =========================================================================

        private GameObject _go;
        private DummyTarget _target;
        private EquipmentManager _mgr;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestDummy");
            _mgr = _go.AddComponent<EquipmentManager>();
            _target = _go.AddComponent<DummyTarget>(); // Awake가 자동 호출됨
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        /// <summary>테스트 제어용 — maxHp 강제 설정 후 CurrentHp를 이 값으로 리셋.</summary>
        private void SetMaxHp(int value)
        {
            var field = typeof(DummyTarget).GetField("maxHp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "DummyTarget.maxHp 필드가 보이지 않음. 이름이 바뀌었나?");
            field.SetValue(_target, value);
            _target.ResetHp();
        }

        private static MaterialData MakeMat(int hardness, string name = "TestMat")
        {
            var m = ScriptableObject.CreateInstance<MaterialData>();
            m.hardness = hardness;
            m.displayName = name;
            return m;
        }

        // =========================================================================
        // Awake / 초기 상태
        // =========================================================================

        [Test]
        public void Awake_CurrentHp_EqualsMaxHp()
        {
            // 기본 maxHp = 100 (Inspector default)
            Assert.AreEqual(100, _target.MaxHp);
            Assert.AreEqual(100, _target.CurrentHp);
            Assert.IsFalse(_target.IsDead);
        }

        [Test]
        public void Awake_NoInitialBodyMaterial_BodyMaterialIsNull()
        {
            Assert.IsNull(_target.BodyMaterial);
        }

        // =========================================================================
        // ApplyHit
        // =========================================================================

        [Test]
        public void ApplyHit_ReducesHp()
        {
            SetMaxHp(50);
            _target.ApplyHit(20, Vector2.right);
            Assert.AreEqual(30, _target.CurrentHp);
            Assert.IsFalse(_target.IsDead);
        }

        [Test]
        public void ApplyHit_DamageExceedsHp_ClampsToZeroAndDies()
        {
            SetMaxHp(10);
            _target.ApplyHit(999, Vector2.right);
            Assert.AreEqual(0, _target.CurrentHp);
            Assert.IsTrue(_target.IsDead);
        }

        [Test]
        public void ApplyHit_WhenDead_NoOp()
        {
            SetMaxHp(10);
            _target.ApplyHit(999, Vector2.right); // dies
            int hpBefore = _target.CurrentHp;

            int hitEvents = 0;
            _target.OnHit += (d, k) => hitEvents++;

            _target.ApplyHit(5, Vector2.right);

            Assert.AreEqual(hpBefore, _target.CurrentHp);
            Assert.AreEqual(0, hitEvents);
        }

        [Test]
        public void ApplyHit_ZeroOrNegativeDamage_NoOp()
        {
            SetMaxHp(50);
            int hitEvents = 0;
            _target.OnHit += (d, k) => hitEvents++;

            _target.ApplyHit(0, Vector2.right);
            _target.ApplyHit(-5, Vector2.right);

            Assert.AreEqual(50, _target.CurrentHp);
            Assert.AreEqual(0, hitEvents);
        }

        [Test]
        public void ApplyHit_FiresOnHitEventWithArgs()
        {
            SetMaxHp(50);
            int receivedDmg = -1;
            Vector2 receivedKnock = Vector2.zero;
            _target.OnHit += (d, k) => { receivedDmg = d; receivedKnock = k; };

            _target.ApplyHit(7, new Vector2(1, 0));

            Assert.AreEqual(7, receivedDmg);
            Assert.AreEqual(new Vector2(1, 0), receivedKnock);
        }

        [Test]
        public void ApplyHit_ThatKills_FiresOnDiedExactlyOnce()
        {
            SetMaxHp(10);
            int diedCount = 0;
            _target.OnDied += () => diedCount++;

            _target.ApplyHit(10, Vector2.right);
            Assert.AreEqual(1, diedCount);

            // 재호출해도 추가 발화 없음
            _target.ApplyHit(5, Vector2.right);
            Assert.AreEqual(1, diedCount);
        }

        // =========================================================================
        // ApplyBlockReaction
        // =========================================================================

        [Test]
        public void ApplyBlockReaction_DoesNotChangeHp()
        {
            SetMaxHp(50);
            _target.ApplyBlockReaction(Vector2.right);
            Assert.AreEqual(50, _target.CurrentHp);
        }

        [Test]
        public void ApplyBlockReaction_FiresOnBlockedEvent()
        {
            int blockCount = 0;
            Vector2 captured = Vector2.zero;
            _target.OnBlocked += (dir) => { blockCount++; captured = dir; };

            _target.ApplyBlockReaction(new Vector2(-1, 0));

            Assert.AreEqual(1, blockCount);
            Assert.AreEqual(new Vector2(-1, 0), captured);
        }

        [Test]
        public void ApplyBlockReaction_WhenDead_NoOp()
        {
            SetMaxHp(5);
            _target.ApplyHit(10, Vector2.right); // dies
            int blockCount = 0;
            _target.OnBlocked += (dir) => blockCount++;

            _target.ApplyBlockReaction(Vector2.right);

            Assert.AreEqual(0, blockCount);
        }

        // =========================================================================
        // ResetHp
        // =========================================================================

        [Test]
        public void ResetHp_RestoresCurrentToMax()
        {
            SetMaxHp(40);
            _target.ApplyHit(30, Vector2.right);
            Assert.AreEqual(10, _target.CurrentHp);

            _target.ResetHp();

            Assert.AreEqual(40, _target.CurrentHp);
            Assert.IsFalse(_target.IsDead);
        }

        // =========================================================================
        // BodyMaterial 반영
        // =========================================================================

        [Test]
        public void BodyMaterial_ReflectsEquippedBodyPart()
        {
            var mat = MakeMat(50, "granite_test");
            _mgr.Equip(new BodyPart(mat));

            Assert.AreSame(mat, _target.BodyMaterial);

            _mgr.Unequip(SlotType.Body);
            Assert.IsNull(_target.BodyMaterial);

            Object.DestroyImmediate(mat);
        }
    }
}
