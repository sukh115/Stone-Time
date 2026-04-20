// -----------------------------------------------------------------------------
// FootPart.cs
// Stone & Time — 발 슬롯 장비 파츠 (IEquippable 구현)
// ADR-0003: 장비 슬롯 아키텍처 / ADR-0004: 재질 데이터
//
// 설계 의도:
//   - "발이 바뀌면 골렘 전체의 이동 감각이 바뀐다"가 핵심 USP.
//   - MaterialData를 받아 OnEquip에서 Rigidbody2D.mass·linearDamping·
//     PhysicsMaterial2D.friction을 갱신한다.
//   - ScriptableObject 원본은 **절대 수정하지 않는다**. 자체 PhysicsMaterial2D
//     인스턴스를 들고 다니며 이 인스턴스의 값만 바꾸고 Collider에 sharedMaterial로 건다.
//
// 스프라이트·VFX 스왑은 Post-MVP. 여기서는 "물리 거동 변화"만 다룬다.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using StoneAndTime.Core;

namespace StoneAndTime.Parts
{
    /// <summary>
    /// 발 슬롯용 장비. 재질에 따라 mass·friction·drag를 골렘 본체에 적용.
    /// POCO라 `new FootPart(material)`로 자유롭게 생성.
    /// </summary>
    public class FootPart : IEquippable
    {
        public MaterialData Material { get; }
        public SlotType Slot => SlotType.Foot;

        // 재질별 PhysicsMaterial2D 인스턴스. 파츠가 살아있는 동안 재사용.
        private readonly PhysicsMaterial2D _physicsMaterial;

        public FootPart(MaterialData material)
        {
            Material = material ?? throw new ArgumentNullException(nameof(material));

            _physicsMaterial = new PhysicsMaterial2D($"Foot_{material.name}_phys")
            {
                friction   = material.friction,
                bounciness = 0f, // 발바닥은 탱탱볼이 아니다.
            };
        }

        public void OnEquip(GolemContext ctx)
        {
            if (ctx == null) return;

            if (ctx.Body != null)
            {
                ctx.Body.mass          = Material.mass;
                ctx.Body.linearDamping = Material.linearDrag;
            }

            if (ctx.MainCollider != null)
            {
                ctx.MainCollider.sharedMaterial = _physicsMaterial;
            }
        }

        public void OnUnequip(GolemContext ctx)
        {
            // MVP: 다음 FootPart가 곧 덮어쓰거나, 슬롯이 비면 기본값 복구는 상위가 책임.
            // 여기서 basePhysics를 복구하지 않는 이유: "맨발 상태의 기본값"을 이 레이어가 모름.
        }

        public override string ToString() =>
            $"FootPart({Material?.displayName ?? "no-mat"}, mass={Material?.mass}, fric={Material?.friction})";
    }
}
