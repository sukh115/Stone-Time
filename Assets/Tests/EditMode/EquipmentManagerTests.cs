// -----------------------------------------------------------------------------
// EquipmentManagerTests.cs
// Stone & Time — EquipmentManager EditMode 테스트
// ADR-0003: 장비 슬롯 아키텍처의 계약을 단위 테스트로 고정.
//
// 실행: Unity Editor > Window > General > Test Runner > EditMode.
//
// 의도: "슬롯에 넣었다 빼는" 가장 기본 동작이 리팩토링에서 깨지면 즉시 알림.
// MonoBehaviour라 new EquipmentManager()는 불가 → GameObject.AddComponent로 생성.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using StoneAndTime.Core;

namespace StoneAndTime.Tests
{
    public class EquipmentManagerTests
    {
        // =========================================================================
        // 테스트용 가짜 IEquippable
        // =========================================================================
        private class FakeEquippable : IEquippable
        {
            public MaterialData Material { get; }
            public SlotType Slot { get; }

            public int EquipCallCount { get; private set; }
            public int UnequipCallCount { get; private set; }

            public FakeEquippable(SlotType slot, MaterialData material = null)
            {
                Slot = slot;
                Material = material;
            }

            public void OnEquip(GolemContext ctx)   => EquipCallCount++;
            public void OnUnequip(GolemContext ctx) => UnequipCallCount++;
        }

        // =========================================================================
        // 공통 헬퍼
        // =========================================================================

        private GameObject _holder;
        private EquipmentManager _mgr;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestEquipmentHolder");
            _mgr = _holder.AddComponent<EquipmentManager>();
            _mgr.Initialize(new GolemContext(_holder, null, null));
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        // =========================================================================
        // 장착
        // =========================================================================

        [Test]
        public void Equip_EmptySlot_Succeeds()
        {
            var body = new FakeEquippable(SlotType.Body);

            bool ok = _mgr.Equip(body);

            Assert.IsTrue(ok);
            Assert.AreSame(body, _mgr.GetEquipped(SlotType.Body));
            Assert.AreEqual(1, body.EquipCallCount);
            Assert.AreEqual(0, body.UnequipCallCount);
        }

        [Test]
        public void Equip_Null_Fails()
        {
            // Unity가 LogWarning을 띄우면 테스트가 실패하므로 예상 로그 등록
            LogAssert.Expect(LogType.Warning, "[EquipmentManager] Equip called with null part.");

            bool ok = _mgr.Equip(null);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Equip_SameSlotTwice_ReplacesAndCallsOnUnequipOnPrevious()
        {
            var oldFoot = new FakeEquippable(SlotType.Foot);
            var newFoot = new FakeEquippable(SlotType.Foot);

            _mgr.Equip(oldFoot);
            _mgr.Equip(newFoot);

            Assert.AreSame(newFoot, _mgr.GetEquipped(SlotType.Foot));
            Assert.AreEqual(1, oldFoot.EquipCallCount);
            Assert.AreEqual(1, oldFoot.UnequipCallCount);
            Assert.AreEqual(1, newFoot.EquipCallCount);
            Assert.AreEqual(0, newFoot.UnequipCallCount);
        }

        // =========================================================================
        // 해제
        // =========================================================================

        [Test]
        public void Unequip_FilledSlot_ReturnsPartAndEmptiesSlot()
        {
            var weapon = new FakeEquippable(SlotType.Weapon);
            _mgr.Equip(weapon);

            var removed = _mgr.Unequip(SlotType.Weapon);

            Assert.AreSame(weapon, removed);
            Assert.IsNull(_mgr.GetEquipped(SlotType.Weapon));
            Assert.IsTrue(_mgr.IsSlotEmpty(SlotType.Weapon));
            Assert.AreEqual(1, weapon.UnequipCallCount);
        }

        [Test]
        public void Unequip_EmptySlot_ReturnsNullSilently()
        {
            var removed = _mgr.Unequip(SlotType.Body);
            Assert.IsNull(removed);
        }

        [Test]
        public void UnequipAll_ClearsEveryEquippedPart()
        {
            var body   = new FakeEquippable(SlotType.Body);
            var weapon = new FakeEquippable(SlotType.Weapon);
            var foot   = new FakeEquippable(SlotType.Foot);

            _mgr.Equip(body);
            _mgr.Equip(weapon);
            _mgr.Equip(foot);

            _mgr.UnequipAll();

            Assert.IsTrue(_mgr.IsSlotEmpty(SlotType.Body));
            Assert.IsTrue(_mgr.IsSlotEmpty(SlotType.Weapon));
            Assert.IsTrue(_mgr.IsSlotEmpty(SlotType.Foot));
            Assert.AreEqual(1, body.UnequipCallCount);
            Assert.AreEqual(1, weapon.UnequipCallCount);
            Assert.AreEqual(1, foot.UnequipCallCount);
        }

        // =========================================================================
        // 이벤트
        // =========================================================================

        [Test]
        public void OnSlotChanged_FiresOnEquipWithNullPrevious()
        {
            SlotType    capturedSlot     = default;
            IEquippable capturedPrevious = new FakeEquippable(SlotType.Body); // 의도적 non-null 초기값
            IEquippable capturedCurrent  = null;
            int fireCount = 0;

            _mgr.OnSlotChanged += (slot, previous, current) =>
            {
                capturedSlot     = slot;
                capturedPrevious = previous;
                capturedCurrent  = current;
                fireCount++;
            };

            var body = new FakeEquippable(SlotType.Body);
            _mgr.Equip(body);

            Assert.AreEqual(1, fireCount);
            Assert.AreEqual(SlotType.Body, capturedSlot);
            Assert.IsNull(capturedPrevious);
            Assert.AreSame(body, capturedCurrent);
        }

        [Test]
        public void OnSlotChanged_FiresOnUnequipWithNullCurrent()
        {
            var body = new FakeEquippable(SlotType.Body);
            _mgr.Equip(body);

            IEquippable capturedPrevious = null;
            IEquippable capturedCurrent  = new FakeEquippable(SlotType.Body); // 의도적 non-null 초기값
            _mgr.OnSlotChanged += (_, previous, current) =>
            {
                capturedPrevious = previous;
                capturedCurrent  = current;
            };

            _mgr.Unequip(SlotType.Body);

            Assert.AreSame(body, capturedPrevious);
            Assert.IsNull(capturedCurrent);
        }

        // =========================================================================
        // Hardness 보조 — MaterialData.BlocksAttackFrom
        // =========================================================================

        [Test]
        public void BlocksAttackFrom_LowerHardnessAttacker_ReturnsTrue()
        {
            var ice    = ScriptableObject.CreateInstance<MaterialData>();
            var stone  = ScriptableObject.CreateInstance<MaterialData>();
            ice.hardness   = 20;
            stone.hardness = 50;

            // 얼음 무기로 돌을 때리면 튕김 (막힘)
            Assert.IsTrue(stone.BlocksAttackFrom(ice));
            // 돌 무기로 얼음을 때리면 통함 (안 막힘)
            Assert.IsFalse(ice.BlocksAttackFrom(stone));

            Object.DestroyImmediate(ice);
            Object.DestroyImmediate(stone);
        }
    }
}
