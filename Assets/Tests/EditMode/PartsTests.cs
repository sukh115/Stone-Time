// -----------------------------------------------------------------------------
// PartsTests.cs
// Stone & Time — WeaponPart / BodyPart / FootPart 기본 계약 EditMode 테스트
//
// 범위:
//   - 생성자 null 가드 (ArgumentNullException)
//   - Slot 타입이 올바른지
//   - Material 노출이 올바른지
//   - 편의 프로퍼티 (Hardness, AttackSpeedMultiplier)가 Material 값과 일치하는지
//
// FootPart는 OnEquip이 실제 Rigidbody에 값을 쓰기 때문에 별도 섹션에서
// 가짜 GolemContext를 만들어 검증.
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Parts;
using Object = UnityEngine.Object; // System.Object 와의 모호성 해소 (System.ArgumentNullException 때문에 using System; 필요)

namespace StoneAndTime.Tests
{
    public class PartsTests
    {
        // =========================================================================
        // 공통 헬퍼
        // =========================================================================

        private static MaterialData MakeMaterial(
            string displayName = "Test",
            int hardness = 50,
            float mass = 1f,
            float friction = 0.4f,
            float linearDrag = 0f,
            float attackSpeedMultiplier = 1f)
        {
            var m = ScriptableObject.CreateInstance<MaterialData>();
            m.displayName          = displayName;
            m.hardness             = hardness;
            m.mass                 = mass;
            m.friction             = friction;
            m.linearDrag           = linearDrag;
            m.attackSpeedMultiplier = attackSpeedMultiplier;
            return m;
        }

        // =========================================================================
        // WeaponPart
        // =========================================================================

        [Test]
        public void WeaponPart_NullMaterial_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new WeaponPart(null));
        }

        [Test]
        public void WeaponPart_ExposesWeaponSlotAndMaterial()
        {
            var mat = MakeMaterial(hardness: 80, attackSpeedMultiplier: 0.9f);
            var part = new WeaponPart(mat);

            Assert.AreEqual(SlotType.Weapon, part.Slot);
            Assert.AreSame(mat, part.Material);
            Assert.AreEqual(80, part.Hardness);
            Assert.AreEqual(0.9f, part.AttackSpeedMultiplier);

            Object.DestroyImmediate(mat);
        }

        [Test]
        public void WeaponPart_OnEquipUnequip_AreSafeOnNullContext()
        {
            var mat = MakeMaterial();
            var part = new WeaponPart(mat);

            Assert.DoesNotThrow(() => part.OnEquip(null));
            Assert.DoesNotThrow(() => part.OnUnequip(null));

            Object.DestroyImmediate(mat);
        }

        // =========================================================================
        // BodyPart
        // =========================================================================

        [Test]
        public void BodyPart_NullMaterial_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new BodyPart(null));
        }

        [Test]
        public void BodyPart_ExposesBodySlotAndHardness()
        {
            var mat = MakeMaterial(hardness: 60);
            var part = new BodyPart(mat);

            Assert.AreEqual(SlotType.Body, part.Slot);
            Assert.AreSame(mat, part.Material);
            Assert.AreEqual(60, part.Hardness);

            Object.DestroyImmediate(mat);
        }

        [Test]
        public void BodyPart_OnEquipUnequip_AreSafeOnNullContext()
        {
            var mat = MakeMaterial();
            var part = new BodyPart(mat);

            Assert.DoesNotThrow(() => part.OnEquip(null));
            Assert.DoesNotThrow(() => part.OnUnequip(null));

            Object.DestroyImmediate(mat);
        }

        // =========================================================================
        // FootPart — 실제 물리 필드 적용 검증
        // =========================================================================

        [Test]
        public void FootPart_NullMaterial_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FootPart(null));
        }

        [Test]
        public void FootPart_OnEquip_WritesMassDragFrictionIntoContext()
        {
            // Arrange: GolemContext용 GameObject + Rigidbody2D + BoxCollider2D 생성
            var go = new GameObject("FootPart_TestGolem");
            var rb = go.AddComponent<Rigidbody2D>();
            var col = go.AddComponent<BoxCollider2D>();

            // 의도된 초기값 — 장착 후 material 값으로 덮어써지는지 확인용
            rb.mass          = 999f;
            rb.linearDamping = 999f;
            Assert.IsNull(col.sharedMaterial);

            var mat = MakeMaterial(mass: 0.7f, friction: 0.05f, linearDrag: 0.1f);
            var ctx = new GolemContext(go, rb, col);
            var foot = new FootPart(mat);

            // Act
            foot.OnEquip(ctx);

            // Assert
            Assert.AreEqual(0.7f, rb.mass,          1e-5f);
            Assert.AreEqual(0.1f, rb.linearDamping, 1e-5f);
            Assert.IsNotNull(col.sharedMaterial);
            Assert.AreEqual(0.05f, col.sharedMaterial.friction, 1e-5f);
            Assert.AreEqual(0f,    col.sharedMaterial.bounciness, 1e-5f);

            // Cleanup
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void FootPart_OnEquip_WithNullContext_DoesNotThrow()
        {
            var mat = MakeMaterial();
            var foot = new FootPart(mat);

            Assert.DoesNotThrow(() => foot.OnEquip(null));
            Assert.DoesNotThrow(() => foot.OnUnequip(null));

            Object.DestroyImmediate(mat);
        }

        [Test]
        public void FootPart_ExposesFootSlot()
        {
            var mat = MakeMaterial();
            var foot = new FootPart(mat);
            Assert.AreEqual(SlotType.Foot, foot.Slot);
            Assert.AreSame(mat, foot.Material);
            Object.DestroyImmediate(mat);
        }
    }
}
