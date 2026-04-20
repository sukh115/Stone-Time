// -----------------------------------------------------------------------------
// IEquippable.cs
// Stone & Time — 장비 가능 파츠 인터페이스
// ADR-0003: 장비 슬롯 아키텍처
//
// 모든 장비 가능한 파츠(Body/Weapon/Foot/...)가 구현해야 하는 공통 계약.
// 실제 MonoBehaviour/ScriptableObject 여부는 파츠마다 다를 수 있으나,
// EquipmentManager는 이 인터페이스만 본다.
// -----------------------------------------------------------------------------

namespace StoneAndTime.Core
{
    /// <summary>
    /// 장비 슬롯에 끼울 수 있는 파츠의 공통 인터페이스.
    /// 구현체는 최소 재질 데이터와 대상 슬롯 타입을 노출한다.
    /// </summary>
    public interface IEquippable
    {
        /// <summary>이 파츠가 어떤 재질로 이루어졌는지.</summary>
        MaterialData Material { get; }

        /// <summary>이 파츠가 장착될 슬롯 종류. 런타임 중 변하지 않는다.</summary>
        SlotType Slot { get; }

        /// <summary>
        /// 슬롯에 장착되는 순간 호출. 골렘 쪽 상태(Rigidbody·공격 훅 등)를 갱신할 기회.
        /// 예) Foot 파츠가 PhysicsMaterial2D.friction을 Material.friction으로 덮어쓴다.
        /// </summary>
        void OnEquip(GolemContext ctx);

        /// <summary>
        /// 슬롯에서 벗겨지는 순간 호출. 원복·이벤트 해제 등 정리 작업.
        /// </summary>
        void OnUnequip(GolemContext ctx);
    }
}
