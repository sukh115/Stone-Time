// -----------------------------------------------------------------------------
// GolemContext.cs
// Stone & Time — 조약돌 골렘 런타임 컨텍스트
// ADR-0003: 장비 슬롯 아키텍처
//
// IEquippable.OnEquip/OnUnequip에 전달되는 의존성 번들.
// 파츠가 "골렘의 Rigidbody에 접근하려면 어떻게?" 같은 질문에 대한 답을 한 곳에서 제공.
//
// 현재 MVP에서는 필드가 적지만, 파츠 구현이 늘어나면서 필요에 따라 확장한다.
// "신께서 GolemContext에 뭐든 던져넣으시라" 금지 — 정말 파츠가 써야 할 것만.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace StoneAndTime.Core
{
    /// <summary>
    /// 장비 파츠가 골렘(주인공)에게 영향을 주기 위해 필요한 참조들을 묶은 컨텍스트.
    /// EquipmentManager가 소유하고, OnEquip/OnUnequip 호출 시 파츠에 전달한다.
    /// </summary>
    public class GolemContext
    {
        /// <summary>골렘 루트 GameObject. Transform·Tag·Layer 접근용.</summary>
        public GameObject Root { get; }

        /// <summary>골렘 전체의 이동·점프·중력 계산에 쓰이는 루트 Rigidbody2D.</summary>
        public Rigidbody2D Body { get; }

        /// <summary>골렘 전체의 충돌 판정에 쓰이는 루트 Collider2D.</summary>
        public Collider2D MainCollider { get; }

        public GolemContext(GameObject root, Rigidbody2D body, Collider2D mainCollider)
        {
            Root = root;
            Body = body;
            MainCollider = mainCollider;
        }
    }
}
