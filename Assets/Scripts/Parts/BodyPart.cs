// -----------------------------------------------------------------------------
// BodyPart.cs
// Stone & Time — 본체 슬롯 장비 파츠 스텁 (IEquippable 구현)
// ADR-0003: 장비 슬롯 아키텍처 / ADR-0004: 재질 데이터
//
// 현재 스코프(MVP Phase 1~2):
//   - 본체 재질 정보만 보관. OnEquip/OnUnequip 모두 no-op.
//   - 본체 재질은 "피격 시 기본 Hardness" 판정과 기본 타격 SFX 소스로 쓰인다.
//     (전투 시스템이 GetEquipped(Body).Material 로 조회)
//
// Post-MVP 확장:
//   - 피격 시 HP/내구도 관리
//   - 스프라이트/SpriteRenderer 교체
//   - 충돌 레이어 변경
// -----------------------------------------------------------------------------

using System;
using StoneAndTime.Core;

namespace StoneAndTime.Parts
{
    /// <summary>
    /// 본체(몸통) 슬롯용 장비. 피격 시 기본 Hardness 판정의 근거가 되는 재질.
    /// POCO라 `new BodyPart(material)`로 자유롭게 생성.
    /// </summary>
    public class BodyPart : IEquippable
    {
        public MaterialData Material { get; }
        public SlotType Slot => SlotType.Body;

        /// <summary>피격 방어 판정에 쓰일 본체 강도.</summary>
        public int Hardness => Material.hardness;

        public BodyPart(MaterialData material)
        {
            Material = material ?? throw new ArgumentNullException(nameof(material));
        }

        public void OnEquip(GolemContext ctx)
        {
            // MVP: no-op. 향후 스프라이트 스왑·HP 재계산 등 훅.
        }

        public void OnUnequip(GolemContext ctx)
        {
            // MVP: no-op. "본체가 없는 골렘"은 현재 게임 규칙상 허용 안 되지만 매니저가 결정.
        }

        public override string ToString() =>
            $"BodyPart({Material?.displayName ?? "no-mat"}, hardness={Material?.hardness})";
    }
}
