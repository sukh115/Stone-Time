// -----------------------------------------------------------------------------
// WeaponSwapTester.cs
// Stone & Time — Q/W/E 키로 WeaponPart 재질 스왑 (Phase 2 디버그 드라이버)
//
// 키 Q: material1 (기본: MD_Granite)
// 키 W: material2 (기본: MD_Ice)
// 키 E: material3 (기본: MD_Obsidian)
// 키 R: Unequip Weapon
//
// Phase 2 검증 끝나면 스크립트 + 컴포넌트 제거하거나 #if UNITY_EDITOR로 감싼다.
// FootSwapTester와 키만 다르고 구조는 동일.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using StoneAndTime.Core;
using StoneAndTime.Parts;

namespace StoneAndTime.Input
{
    [RequireComponent(typeof(EquipmentManager))]
    public class WeaponSwapTester : MonoBehaviour
    {
        [Header("Material slots (Q / W / E keys)")]
        [SerializeField] private MaterialData material1;
        [SerializeField] private MaterialData material2;
        [SerializeField] private MaterialData material3;

        private EquipmentManager _mgr;

        private void Awake() => _mgr = GetComponent<EquipmentManager>();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame) TryEquip(material1);
            if (kb.wKey.wasPressedThisFrame) TryEquip(material2);
            if (kb.eKey.wasPressedThisFrame) TryEquip(material3);

            if (kb.rKey.wasPressedThisFrame)
            {
                _mgr.Unequip(SlotType.Weapon);
                Debug.Log("[WeaponSwapTester] Unequipped Weapon.");
            }
        }

        private void TryEquip(MaterialData mat)
        {
            if (mat == null)
            {
                Debug.LogWarning("[WeaponSwapTester] Material slot is empty.", this);
                return;
            }

            var part = new WeaponPart(mat);
            _mgr.Equip(part);
            Debug.Log($"[WeaponSwapTester] Equipped {part}.");
        }
    }
}
