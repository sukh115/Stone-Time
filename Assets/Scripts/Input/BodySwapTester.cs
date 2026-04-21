// -----------------------------------------------------------------------------
// BodySwapTester.cs
// Stone & Time — Z/X/C 키로 BodyPart 재질 스왑 (Phase 2 디버그 드라이버)
//
// 피격 대상 DummyTarget 쪽에 붙여 실험 — 재질 바꿔가며 같은 공격을 맞았을 때
// Blocked / Hit이 뒤집히는지 검증.
//
// 키 Z: material1 (기본: MD_Granite)
// 키 X: material2 (기본: MD_Ice)
// 키 C: material3 (기본: MD_Obsidian)
// 키 V: Unequip Body
//
// Phase 2 검증 끝나면 #if UNITY_EDITOR 로 감싸거나 제거.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using StoneAndTime.Core;
using StoneAndTime.Parts;

namespace StoneAndTime.Input
{
    [RequireComponent(typeof(EquipmentManager))]
    public class BodySwapTester : MonoBehaviour
    {
        [Header("Material slots (Z / X / C keys)")]
        [SerializeField] private MaterialData material1;
        [SerializeField] private MaterialData material2;
        [SerializeField] private MaterialData material3;

        private EquipmentManager _mgr;

        private void Awake() => _mgr = GetComponent<EquipmentManager>();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.zKey.wasPressedThisFrame) TryEquip(material1);
            if (kb.xKey.wasPressedThisFrame) TryEquip(material2);
            if (kb.cKey.wasPressedThisFrame) TryEquip(material3);

            if (kb.vKey.wasPressedThisFrame)
            {
                _mgr.Unequip(SlotType.Body);
                Debug.Log("[BodySwapTester] Unequipped Body.");
            }
        }

        private void TryEquip(MaterialData mat)
        {
            if (mat == null)
            {
                Debug.LogWarning("[BodySwapTester] Material slot is empty.", this);
                return;
            }

            var part = new BodyPart(mat);
            _mgr.Equip(part);
            Debug.Log($"[BodySwapTester] Equipped {part}.");
        }
    }
}
