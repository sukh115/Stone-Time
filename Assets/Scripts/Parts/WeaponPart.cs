// -----------------------------------------------------------------------------
// WeaponPart.cs
// Stone & Time — 무기 슬롯 장비 파츠 (IEquippable 구현)
// ADR-0003: 장비 슬롯 아키텍처 / ADR-0004: 재질 데이터
//
// 설계 의도:
//   - FootPart가 "이동 감각"을 바꾸듯, WeaponPart는 "타격 감각"을 바꾼다.
//   - MVP에서는 OnEquip이 직접 Rigidbody를 건드리지 않는다. 공격 시스템(추후 구현)이
//     EquipmentManager.GetEquipped(SlotType.Weapon)으로 현재 무기를 조회해서
//     Material.hardness 비교 + Material.attackSpeedMultiplier를 쿨다운 계산에 반영.
//   - 즉 여기서는 "데이터 컨테이너"가 본업이고, 물리/이펙트 훅은 Post-MVP.
//
// 무기 VFX/SFX 스왑은 MaterialData.hitSfx / crackVfx가 이미 담고 있으므로
// 공격 시스템에서 그대로 소비하면 된다.
// -----------------------------------------------------------------------------

using System;
using StoneAndTime.Core;

namespace StoneAndTime.Parts
{
    /// <summary>
    /// 무기 슬롯용 장비. 재질에 따라 공격 Hardness·공격 속도·타격 이펙트를 결정.
    /// POCO라 `new WeaponPart(material)`로 자유롭게 생성.
    /// </summary>
    public class WeaponPart : IEquippable
    {
        public MaterialData Material { get; }
        public SlotType Slot => SlotType.Weapon;

        /// <summary>공격 판정에 쓰일 강도. Material.hardness 그대로 노출.</summary>
        public int Hardness => Material.hardness;

        /// <summary>쿨다운/애니메이션 재생 속도 배율. 1.0이 기준.</summary>
        public float AttackSpeedMultiplier => Material.attackSpeedMultiplier;

        public WeaponPart(MaterialData material)
        {
            Material = material ?? throw new ArgumentNullException(nameof(material));
        }

        public void OnEquip(GolemContext ctx)
        {
            // MVP: 별도 훅 없음. 공격 시스템이 폴링 방식으로 현재 무기를 조회한다.
            // 향후 "무기 교체 시 Animator Override" 같은 훅 생기면 여기에.
        }

        public void OnUnequip(GolemContext ctx)
        {
            // MVP: no-op.
        }

        public override string ToString() =>
            $"WeaponPart({Material?.displayName ?? "no-mat"}, hardness={Material?.hardness}, " +
            $"speedMul={Material?.attackSpeedMultiplier})";
    }
}
