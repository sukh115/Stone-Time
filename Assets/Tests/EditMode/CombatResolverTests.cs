// -----------------------------------------------------------------------------
// CombatResolverTests.cs
// Stone & Time — CombatResolver 순수 로직 EditMode 테스트
//
// 범위:
//   - null 가드 (weapon, target, IsDead, target.BodyMaterial null)
//   - 강도 비교 매트릭스 3×3 전수 (3 Hit / 6 Blocked)
//   - 데미지 값 검증 (= max(1, attackerHardness - targetHardness))
//   - MinHitDamage 하한
//
// 실행: Unity Editor > Window > General > Test Runner > EditMode.
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Parts;
using StoneAndTime.Combat;
using Object = UnityEngine.Object;

namespace StoneAndTime.Tests
{
    public class CombatResolverTests
    {
        // =========================================================================
        // Fake IDamageable
        // =========================================================================
        private class FakeDamageable : IDamageable
        {
            public MaterialData BodyMaterial { get; set; }
            public bool IsDead { get; set; }

            public int HitCallCount { get; private set; }
            public int BlockCallCount { get; private set; }
            public int LastDamage { get; private set; }
            public Vector2 LastKnockback { get; private set; }

            public void ApplyHit(int damage, Vector2 knockbackDir)
            {
                HitCallCount++;
                LastDamage = damage;
                LastKnockback = knockbackDir;
            }

            public void ApplyBlockReaction(Vector2 attackerDir)
            {
                BlockCallCount++;
                LastKnockback = attackerDir;
            }
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

        // 3종 재질 (Ice 20, Granite 50, Obsidian 80) — MD_*.asset 과 동일한 수치로 픽스드.
        // 실제 .asset을 로드하지 않고 ScriptableObject.CreateInstance로 제어된 인스턴스 사용.
        private MaterialData _ice;
        private MaterialData _granite;
        private MaterialData _obsidian;

        [SetUp]
        public void SetUp()
        {
            _ice      = MakeMat("얼음", 20);
            _granite  = MakeMat("화강암", 50);
            _obsidian = MakeMat("흑요석", 80);
        }

        [TearDown]
        public void TearDown()
        {
            if (_ice      != null) Object.DestroyImmediate(_ice);
            if (_granite  != null) Object.DestroyImmediate(_granite);
            if (_obsidian != null) Object.DestroyImmediate(_obsidian);
        }

        // =========================================================================
        // 가드
        // =========================================================================

        [Test]
        public void Resolve_NullWeapon_ReturnsNoTarget()
        {
            var target = new FakeDamageable { BodyMaterial = _granite };
            var outcome = CombatResolver.Resolve(null, target, out int damage);
            Assert.AreEqual(AttackOutcome.NoTarget, outcome);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void Resolve_NullTarget_ReturnsNoTarget()
        {
            var weapon = new WeaponPart(_granite);
            var outcome = CombatResolver.Resolve(weapon, null, out int damage);
            Assert.AreEqual(AttackOutcome.NoTarget, outcome);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void Resolve_DeadTarget_ReturnsNoTarget()
        {
            var weapon = new WeaponPart(_obsidian);
            var target = new FakeDamageable { BodyMaterial = _ice, IsDead = true };
            var outcome = CombatResolver.Resolve(weapon, target, out int damage);
            Assert.AreEqual(AttackOutcome.NoTarget, outcome);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void Resolve_TargetWithoutBodyMaterial_HitsForAttackerHardness()
        {
            var weapon = new WeaponPart(_granite); // hardness 50
            var target = new FakeDamageable { BodyMaterial = null };
            var outcome = CombatResolver.Resolve(weapon, target, out int damage);
            Assert.AreEqual(AttackOutcome.Hit, outcome);
            Assert.AreEqual(50, damage);
        }

        // =========================================================================
        // 3×3 매트릭스 — Blocked 6종
        // =========================================================================

        [TestCase(20, 20)] // Ice vs Ice
        [TestCase(20, 50)] // Ice vs Granite
        [TestCase(20, 80)] // Ice vs Obsidian
        [TestCase(50, 50)] // Granite vs Granite
        [TestCase(50, 80)] // Granite vs Obsidian
        [TestCase(80, 80)] // Obsidian vs Obsidian
        public void Resolve_AttackerHardnessLessOrEqualToTarget_ReturnsBlocked(int attackerHardness, int targetHardness)
        {
            var attackerMat = MakeMatLocal(attackerHardness);
            var targetMat   = MakeMatLocal(targetHardness);
            try
            {
                var weapon = new WeaponPart(attackerMat);
                var target = new FakeDamageable { BodyMaterial = targetMat };

                var outcome = CombatResolver.Resolve(weapon, target, out int damage);

                Assert.AreEqual(AttackOutcome.Blocked, outcome);
                Assert.AreEqual(0, damage);
            }
            finally
            {
                // Assert 실패 시에도 SO 인스턴스 누수 없도록 try/finally.
                Object.DestroyImmediate(attackerMat);
                Object.DestroyImmediate(targetMat);
            }
        }

        // =========================================================================
        // 3×3 매트릭스 — Hit 3종
        // =========================================================================

        [TestCase(50, 20, 30)]  // Granite vs Ice → 30
        [TestCase(80, 20, 60)]  // Obsidian vs Ice → 60
        [TestCase(80, 50, 30)]  // Obsidian vs Granite → 30
        public void Resolve_AttackerHardnessGreaterThanTarget_ReturnsHitWithDelta(
            int attackerHardness, int targetHardness, int expectedDamage)
        {
            var attackerMat = MakeMatLocal(attackerHardness);
            var targetMat   = MakeMatLocal(targetHardness);
            try
            {
                var weapon = new WeaponPart(attackerMat);
                var target = new FakeDamageable { BodyMaterial = targetMat };

                var outcome = CombatResolver.Resolve(weapon, target, out int damage);

                Assert.AreEqual(AttackOutcome.Hit, outcome);
                Assert.AreEqual(expectedDamage, damage);
            }
            finally
            {
                Object.DestroyImmediate(attackerMat);
                Object.DestroyImmediate(targetMat);
            }
        }

        // =========================================================================
        // MinHitDamage 하한 — 차이가 0~음수여도 실제 Hit은 안 되지만,
        // 무방어(null BodyMaterial)일 때 attacker.hardness = 0 상정 케이스 방어.
        // =========================================================================

        [Test]
        public void Resolve_TargetNullBody_AttackerHardnessZero_ClampsToMinDamage()
        {
            var zeroMat = MakeMatLocal(0);
            try
            {
                var weapon = new WeaponPart(zeroMat);
                var target = new FakeDamageable { BodyMaterial = null };

                var outcome = CombatResolver.Resolve(weapon, target, out int damage);

                Assert.AreEqual(AttackOutcome.Hit, outcome);
                Assert.GreaterOrEqual(damage, CombatResolver.MinHitDamage);
            }
            finally
            {
                Object.DestroyImmediate(zeroMat);
            }
        }

        // =========================================================================
        // 편의 오버로드 (out 없음)
        // =========================================================================

        [Test]
        public void Resolve_Overload_WithoutOutDamage_ReturnsSameOutcome()
        {
            var weapon = new WeaponPart(_obsidian);
            var target = new FakeDamageable { BodyMaterial = _ice };

            var outcome = CombatResolver.Resolve(weapon, target);
            Assert.AreEqual(AttackOutcome.Hit, outcome);
        }

        // =========================================================================
        // 로컬 MaterialData 빌더 — TestCase별로 SetUp의 3종과 별개로 생성
        // =========================================================================

        private static MaterialData MakeMatLocal(int hardness)
        {
            var m = ScriptableObject.CreateInstance<MaterialData>();
            m.hardness = hardness;
            m.displayName = $"TestMat_h{hardness}";
            return m;
        }
    }
}
