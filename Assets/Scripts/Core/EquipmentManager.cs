// -----------------------------------------------------------------------------
// EquipmentManager.cs
// Stone & Time — 골렘 장비 슬롯 매니저
// ADR-0003: 장비 슬롯 아키텍처 (IEquippable + SlotType enum)
//
// Dictionary<SlotType, IEquippable>로 슬롯 상태를 보관.
// 외부(UI·전투·이동 시스템)는 OnSlotChanged 이벤트 또는 GetEquipped(slot)으로 소통.
//
// 의도적으로 "하나의 슬롯에 하나의 파츠" 규칙만 지킨다. 무게 제한·레벨 제한 같은 규칙은
// 상위 레이어(인벤토리·UI) 책임. 여기는 슬롯 딕셔너리가 본업.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoneAndTime.Core
{
    /// <summary>
    /// 슬롯 상태 변경 이벤트 시그니처.
    /// </summary>
    /// <param name="slot">변경된 슬롯</param>
    /// <param name="previous">변경 전 장착 파츠(없었으면 null)</param>
    /// <param name="current">변경 후 장착 파츠(비었으면 null)</param>
    public delegate void SlotChangedHandler(SlotType slot, IEquippable previous, IEquippable current);

    /// <summary>
    /// 골렘 한 마리의 장비 슬롯을 관리.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        // =========================================================================
        // 상태
        // =========================================================================
        private readonly Dictionary<SlotType, IEquippable> _slots = new();

        /// <summary>장비 파츠에 전달될 컨텍스트. Awake 또는 외부 주입으로 초기화.</summary>
        public GolemContext Context { get; private set; }

        // =========================================================================
        // 이벤트
        // =========================================================================
        /// <summary>슬롯 장착/해제/교체 시마다 호출.</summary>
        public event SlotChangedHandler OnSlotChanged;

        // =========================================================================
        // 초기화
        // =========================================================================

        /// <summary>
        /// GolemContext를 외부에서 주입. 일반적으로 Golem 루트 컴포넌트의 Awake에서 호출.
        /// </summary>
        public void Initialize(GolemContext ctx)
        {
            Context = ctx;
        }

        // =========================================================================
        // 조회
        // =========================================================================

        /// <summary>슬롯에 장착된 파츠를 반환. 비었으면 null.</summary>
        public IEquippable GetEquipped(SlotType slot)
        {
            _slots.TryGetValue(slot, out var equipped);
            return equipped;
        }

        /// <summary>현재 비어 있는지 여부.</summary>
        public bool IsSlotEmpty(SlotType slot) => !_slots.ContainsKey(slot);

        // =========================================================================
        // 장착 / 해제
        // =========================================================================

        /// <summary>
        /// 파츠를 해당 파츠의 Slot에 장착. 이미 같은 슬롯에 다른 파츠가 있으면 먼저 해제한다.
        /// </summary>
        /// <returns>성공 여부. part가 null이거나 Slot이 불일치하면 false.</returns>
        public bool Equip(IEquippable part)
        {
            if (part == null)
            {
                Debug.LogWarning("[EquipmentManager] Equip called with null part.");
                return false;
            }

            var slot = part.Slot;

            // 기존 장착분이 있으면 해제
            if (_slots.TryGetValue(slot, out var previous) && previous != null)
            {
                previous.OnUnequip(Context);
            }

            _slots[slot] = part;
            part.OnEquip(Context);

            OnSlotChanged?.Invoke(slot, previous, part);
            return true;
        }

        /// <summary>
        /// 해당 슬롯을 비운다. 비어 있었다면 아무 일도 일어나지 않음.
        /// </summary>
        /// <returns>실제로 해제한 파츠(비어 있었다면 null).</returns>
        public IEquippable Unequip(SlotType slot)
        {
            if (!_slots.TryGetValue(slot, out var previous) || previous == null)
            {
                return null;
            }

            previous.OnUnequip(Context);
            _slots.Remove(slot);

            OnSlotChanged?.Invoke(slot, previous, null);
            return previous;
        }

        /// <summary>모든 슬롯을 한 번에 비운다. 씬 전환·리셋용.</summary>
        public void UnequipAll()
        {
            // Copy keys to avoid collection-modified-during-iteration.
            var slots = new List<SlotType>(_slots.Keys);
            foreach (var slot in slots)
            {
                Unequip(slot);
            }
        }

        // =========================================================================
        // 디버그
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Dump Slots")]
        private void DumpSlots()
        {
            if (_slots.Count == 0)
            {
                Debug.Log("[EquipmentManager] No slots equipped.");
                return;
            }
            foreach (var kv in _slots)
            {
                var mat = kv.Value?.Material;
                Debug.Log($"[EquipmentManager] {kv.Key} -> {(mat != null ? mat.displayName : "(no material)")}");
            }
        }
#endif
    }
}
