// -----------------------------------------------------------------------------
// GolemInitializer.cs
// Stone & Time — 골렘 루트에서 GolemContext를 조립해서 EquipmentManager에 주입.
//
// 문제:
//   EquipmentManager.Equip() 안에서 part.OnEquip(Context)를 호출하는데,
//   누군가 Initialize(ctx)를 먼저 불러주지 않으면 Context가 null.
//
// 책임:
//   Awake에서 Rigidbody2D/Collider2D를 자기 자신에서 찾거나 Inspector 슬롯으로
//   받아 GolemContext를 만들고 EquipmentManager에 전달.
//
// DefaultExecutionOrder로 EquipmentManager를 사용하는 다른 컴포넌트보다 일찍 돌도록 강제.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace StoneAndTime.Core
{
    [RequireComponent(typeof(EquipmentManager))]
    [DefaultExecutionOrder(-100)]
    public class GolemInitializer : MonoBehaviour
    {
        [Tooltip("골렘 루트의 Rigidbody2D. 비워두면 GetComponent로 자동 탐색.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip("골렘의 주 Collider2D (발바닥 마찰용). 비워두면 GetComponent로 자동 탐색.")]
        [SerializeField] private Collider2D mainCollider;

        private void Awake()
        {
            if (body == null)         body         = GetComponent<Rigidbody2D>();
            if (mainCollider == null) mainCollider = GetComponent<Collider2D>();

            var ctx = new GolemContext(gameObject, body, mainCollider);
            GetComponent<EquipmentManager>().Initialize(ctx);

            if (body == null)
            {
                Debug.LogWarning("[GolemInitializer] Rigidbody2D가 없습니다. FootPart의 mass/drag 적용이 무시됩니다.", this);
            }
            if (mainCollider == null)
            {
                Debug.LogWarning("[GolemInitializer] Collider2D가 없습니다. FootPart의 friction 적용이 무시됩니다.", this);
            }
        }
    }
}
