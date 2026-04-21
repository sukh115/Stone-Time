// -----------------------------------------------------------------------------
// IDamageable.cs
// Stone & Time — 피격 대상 계약
// ADR-0004: Hardness·재질 데이터 모델
//
// 설계 의도:
//   - Combat 로직(CombatResolver)은 구체 타입(DummyTarget·Enemy·BreakableProp 등)에
//     의존하지 말 것. 이 인터페이스만 본다.
//   - 본체 재질 노출은 Hardness 비교용. MaterialData가 null이면 "무방어"로 취급.
//   - ApplyHit / ApplyBlockReaction은 "받는 쪽 반응"이라 분리.
//     데미지 수치 계산은 CombatResolver 책임, 대상은 수치만 받아 소화.
// -----------------------------------------------------------------------------

using UnityEngine;
using StoneAndTime.Core;

namespace StoneAndTime.Combat
{
    /// <summary>
    /// 공격을 받을 수 있는 모든 것의 공통 계약.
    /// CombatResolver와 AttackController는 이 인터페이스만 안다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>본체 재질. 피격 방어 판정(BlocksAttackFrom)에 쓰임. null이면 무방어 처리.</summary>
        MaterialData BodyMaterial { get; }

        /// <summary>이미 파괴되었는가. true면 CombatResolver가 NoTarget으로 스킵.</summary>
        bool IsDead { get; }

        /// <summary>
        /// 피격 성공 처리. damage &gt; 0.
        /// knockback은 공격자 → 대상 방향의 단위 벡터(크기는 상위에서 결정).
        /// </summary>
        void ApplyHit(int damage, Vector2 knockbackDir);

        /// <summary>
        /// 공격이 막혔을 때의 피드백. 데미지는 없지만 VFX/SFX/카메라 셰이크 등은 발동 가능.
        /// attackerDir은 공격자 → 대상 방향. 역경직이 필요하면 공격자 쪽에서 사용.
        /// </summary>
        void ApplyBlockReaction(Vector2 attackerDir);
    }
}
