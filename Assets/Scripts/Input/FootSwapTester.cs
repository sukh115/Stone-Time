// -----------------------------------------------------------------------------
// FootSwapTester.cs
// Stone & Time — 1/2/3 키로 FootPart 재질 스왑 (Phase 1 디버그 드라이버)
//
// 키 1: material1 (기본: MD_Granite)
// 키 2: material2 (기본: MD_Ice)
// 키 3: material3 (기본: MD_Obsidian)
// 키 0: Unequip Foot
//
// Phase 1 검증 끝나면 스크립트 + 컴포넌트 제거하거나 #if UNITY_EDITOR로 감싼다.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using StoneAndTime.Core;
using StoneAndTime.Parts;

namespace StoneAndTime.Input
{
    [RequireComponent(typeof(EquipmentManager))]
    public class FootSwapTester : MonoBehaviour
    {
        [Header("Material slots (1 / 2 / 3 keys)")]
        [SerializeField] private MaterialData material1;
        [SerializeField] private MaterialData material2;
        [SerializeField] private MaterialData material3;

        private EquipmentManager _mgr;

        private void Awake() => _mgr = GetComponent<EquipmentManager>();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) TryEquip(material1);
            if (kb.digit2Key.wasPressedThisFrame) TryEquip(material2);
            if (kb.digit3Key.wasPressedThisFrame) TryEquip(material3);

            // 0 키: 슬롯 비우기 (맨발 상태)
            if (kb.digit0Key.wasPressedThisFrame)
            {
                _mgr.Unequip(SlotType.Foot);
                Debug.Log("[FootSwapTester] Unequipped Foot.");
            }
        }

        private void TryEquip(MaterialData mat)
        {
            if (mat == null)
            {
                Debug.LogWarning("[FootSwapTester] Material slot is empty.", this);
                return;
            }

            var part = new FootPart(mat);
            _mgr.Equip(part);
            Debug.Log($"[FootSwapTester] Equipped {part}.");
        }
    }
}
