// -----------------------------------------------------------------------------
// SlotType.cs
// Stone & Time — 장비 슬롯 타입 열거형
// ADR-0003: 장비 슬롯 아키텍처 (IEquippable + SlotType enum)
//
// MVP에서 사용: Body, Weapon, Foot (3슬롯)
// Post-MVP 예약: Cape, HandLeft, HandRight, FootLeft, FootRight
// -----------------------------------------------------------------------------

namespace StoneAndTime.Core
{
    /// <summary>
    /// 조약돌 골렘이 장착할 수 있는 슬롯 종류.
    /// ADR-0003 참조. MVP는 Body/Weapon/Foot 3개만 활성화.
    /// </summary>
    public enum SlotType
    {
        // ---- MVP (Phase 1~2) ----
        Body    = 0,   // 본체. 체력·기본 Hardness·전역 물리 영향
        Weapon  = 1,   // 무기. 공격 Hardness·속도·범위 (합성 타격 전용)
        Foot    = 2,   // 발. 이동 Rigidbody2D·PhysicsMaterial2D 결정

        // ---- Post-MVP (Phase 2+ 확장) ----
        // 현재 코드 경로에서 사용되지 않음. 파츠 제작·시스템 추가 시 활성화.
        Cape        = 10,
        HandLeft    = 20,
        HandRight   = 21,
        FootLeft    = 30,
        FootRight   = 31,
    }
}
