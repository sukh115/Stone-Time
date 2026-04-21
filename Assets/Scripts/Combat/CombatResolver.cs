// -----------------------------------------------------------------------------
// CombatResolver.cs
// Stone & Time — 상대적 강도(Relative Hardness) 기반 공격 판정 순수 로직
// ADR-0004: Hardness·재질 데이터 모델
//
// 설계 의도:
//   - Unity에 의존하지 않는 순수 C# static 메서드. EditMode에서 9종 매트릭스 전수 검증.
//   - 규칙(ADR-0004):
//       공격자 무기의 hardness <= 대상 본체의 hardness  →  막힘 (Blocked)
//       그 외 (attackerHardness > targetHardness)       →  관통 (Hit)
//     "동일 강도"는 의도적으로 막힘 분류. "같은 재질끼리는 상처 못 낸다"는 게임 규칙.
//   - 데미지 공식(MVP):
//       damage = max(1, attackerHardness - targetHardness)
//     즉 흑요석(80) → 얼음(20)은 60 대미지, 화강암(50) → 얼음(20)은 30 대미지.
//   - 본체 재질이 없으면(BodyMaterial == null) "무방어"로 간주, damage = attackerHardness.
//     이건 "본체 파츠가 깨진 상태"에서 여전히 공격받을 수 있게 하기 위한 완충.
//
// 상위(AttackController)가 대상 검색·knockback 방향·HitStop 호출을 담당.
// 여기서는 "이 조합이 막히는가? 얼마나 들어가는가?"만 계산한다.
// -----------------------------------------------------------------------------

using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Parts;

namespace StoneAndTime.Combat
{
    /// <summary>
    /// 재질 강도 비교 기반 공격 판정. 순수 정적 클래스.
    /// </summary>
    public static class CombatResolver
    {
        // =========================================================================
        // 튜닝 상수
        // =========================================================================

        /// <summary>Hit 판정 시 최소 보장 데미지. damage가 0 이하로 떨어져도 이 값을 반환.</summary>
        public const int MinHitDamage = 1;

        // =========================================================================
        // 주 API
        // =========================================================================

        /// <summary>
        /// 공격자 무기와 피격 대상을 받아 결과를 계산.
        /// </summary>
        /// <param name="weapon">공격자의 무기 파츠. null이면 NoTarget.</param>
        /// <param name="target">피격 대상. null이거나 IsDead면 NoTarget.</param>
        /// <param name="damage">out: 실제 적용될 데미지. NoTarget/Blocked이면 0.</param>
        /// <returns>Hit / Blocked / NoTarget 중 하나.</returns>
        public static AttackOutcome Resolve(WeaponPart weapon, IDamageable target, out int damage)
        {
            damage = 0;

            // 1. 가드 — 공격자 무기 없음
            if (weapon == null || weapon.Material == null)
            {
                return AttackOutcome.NoTarget;
            }

            // 2. 가드 — 유효 타겟 없음
            //    Unity null 안전을 위해 target as UnityEngine.Object 체크도 고려했으나,
            //    IDamageable 구현체는 MonoBehaviour가 아닐 수도 있다. 호출측에서 Unity 파괴
            //    객체 필터링을 먼저 수행한다는 계약을 둔다. IsDead로 최종 거름.
            if (target == null || target.IsDead)
            {
                return AttackOutcome.NoTarget;
            }

            // 3. 블록 판정 — targetMat.BlocksAttackFrom(attackerMat)
            //    MaterialData.BlocksAttackFrom 규칙: attacker.hardness <= target.hardness → true
            var attackerMat = weapon.Material;
            var targetMat   = target.BodyMaterial;

            // 본체 재질이 없으면 무방어 — 공격자 강도 그대로 데미지
            if (targetMat == null)
            {
                damage = Mathf.Max(MinHitDamage, attackerMat.hardness);
                return AttackOutcome.Hit;
            }

            if (targetMat.BlocksAttackFrom(attackerMat))
            {
                return AttackOutcome.Blocked;
            }

            // 4. Hit — 데미지 = max(1, attackerHardness - targetHardness)
            damage = Mathf.Max(MinHitDamage, attackerMat.hardness - targetMat.hardness);
            return AttackOutcome.Hit;
        }

        // =========================================================================
        // 편의 오버로드 — damage 값 필요 없을 때
        // =========================================================================

        /// <summary>out damage 없이 결과만 필요할 때.</summary>
        public static AttackOutcome Resolve(WeaponPart weapon, IDamageable target)
            => Resolve(weapon, target, out _);
    }
}
