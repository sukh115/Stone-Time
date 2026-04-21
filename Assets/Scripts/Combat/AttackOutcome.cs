// -----------------------------------------------------------------------------
// AttackOutcome.cs
// Stone & Time — 공격 판정 결과 enum
//
// CombatResolver.Resolve() 의 반환값. 상위 레이어(AttackController·UI·SFX)가
// 분기점으로 쓴다.
// -----------------------------------------------------------------------------

namespace StoneAndTime.Combat
{
    /// <summary>
    /// 한 번의 공격 시도가 어떻게 끝났는지.
    /// </summary>
    public enum AttackOutcome
    {
        /// <summary>사거리 안에 IDamageable 대상이 없어서 공격이 허공을 갈랐음.</summary>
        NoTarget = 0,

        /// <summary>대상 본체 재질 강도가 공격자 무기 강도보다 크거나 같아 튕김.</summary>
        Blocked = 1,

        /// <summary>공격이 통해서 데미지가 들어감.</summary>
        Hit = 2,
    }
}
