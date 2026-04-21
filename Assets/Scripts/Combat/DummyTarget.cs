// -----------------------------------------------------------------------------
// DummyTarget.cs
// Stone & Time — Phase 2 공격 실험용 피격 대상 MonoBehaviour
//
// 설계 의도:
//   - IDamageable을 구현하는 "가장 단순한 피격 대상". Enemy 계층의 조상이 아니라
//     전투 검증용 샌드백 역할. Phase 3에서 Enemy.cs로 확장·대체될 수 있음.
//   - BodyMaterial은 "동일 GameObject의 EquipmentManager"에서 BodyPart를 조회해
//     간접 노출. 이래야 BodySwapTester로 런타임에 재질을 바꿔도 IDamageable 계약이
//     같은 지점으로 일관된다.
//   - initialBodyMaterial이 설정되어 있으면 Awake에서 BodyPart를 조립해 장착.
//     이 기능이 없으면 사용자가 별도로 BodyPart를 Equip해줘야 하는데, 실험용이라 번거롭다.
//
// HP / 피격 이펙트:
//   - hp는 정수. 0이 되면 IsDead = true. 실제 파괴(Destroy)는 이벤트로 위임.
//   - OnHit / OnBlocked / OnDied 이벤트로 VFX·SFX·카메라 셰이크 훅 가능.
//
// 주의:
//   - ApplyHit이 호출되는 시점에 IsDead가 이미 true일 수 있다(이중 히트). 재방어.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Parts;

namespace StoneAndTime.Combat
{
    /// <summary>
    /// 공격 실험용 더미 피격 대상. HP 소진 시 IsDead가 true가 되며, 파괴는 상위 이벤트로 위임.
    /// *프로덕션용 Enemy 기반 클래스 아님.* Phase 3에서 Enemy.cs로 교체 예정.
    /// </summary>
    [AddComponentMenu("Stone&Time/Debug/Dummy Target")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentManager))]
    public class DummyTarget : MonoBehaviour, IDamageable
    {
        // =========================================================================
        // Inspector
        // =========================================================================
        [Header("HP")]
        [SerializeField, Min(1)] private int maxHp = 100;

        [Header("Initial Body")]
        [Tooltip("비워두면 Awake에서 BodyPart를 장착하지 않음. 외부에서 Equip 호출 필요.")]
        [SerializeField] private MaterialData initialBodyMaterial;

        [Header("Feedback (선택)")]
        [Tooltip("피격 성공 시 위치에 생성될 VFX 프리팹. MaterialData.crackVfx 와 별개로 본체 피격용.")]
        [SerializeField] private GameObject onHitVfx;

        [Tooltip("블록 시 위치에 생성될 VFX 프리팹. 보통 공격자 쪽 재질의 crackVfx가 더 적절.")]
        [SerializeField] private GameObject onBlockVfx;

        // =========================================================================
        // 상태
        // =========================================================================
        private EquipmentManager _equipment;

        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }

        // =========================================================================
        // IDamageable
        // =========================================================================

        public MaterialData BodyMaterial
        {
            get
            {
                if (_equipment == null) return null;
                var body = _equipment.GetEquipped(SlotType.Body);
                return body?.Material;
            }
        }

        public bool IsDead => CurrentHp <= 0;

        // =========================================================================
        // 이벤트
        // =========================================================================

        /// <summary>피격 성공 시 호출. (damage, knockbackDir) — 이미 HP에 반영된 뒤.</summary>
        public event Action<int, Vector2> OnHit;

        /// <summary>블록 시 호출. (attackerDir)</summary>
        public event Action<Vector2> OnBlocked;

        /// <summary>HP가 0에 도달한 순간 한 번 호출. 이후 ApplyHit는 무시.</summary>
        public event Action OnDied;

        // =========================================================================
        // Lifecycle
        // =========================================================================

        private void Awake()
        {
            _equipment = GetComponent<EquipmentManager>();
            CurrentHp = maxHp;

            // Context 주입 — GolemInitializer와 동일 패턴이지만 Dummy는 Rigidbody2D/Collider2D가
            // 필수가 아니다. 있으면 쓰고 없으면 null로 넘긴다.
            var ctx = new GolemContext(
                gameObject,
                GetComponent<Rigidbody2D>(),
                GetComponent<Collider2D>()
            );
            _equipment.Initialize(ctx);

            // 초기 Body 자동 장착
            if (initialBodyMaterial != null)
            {
                _equipment.Equip(new BodyPart(initialBodyMaterial));
            }
        }

        // =========================================================================
        // IDamageable 구현
        // =========================================================================

        public void ApplyHit(int damage, Vector2 knockbackDir)
        {
            if (IsDead || damage <= 0) return;

            CurrentHp = Mathf.Max(0, CurrentHp - damage);

            if (onHitVfx != null)
            {
                Instantiate(onHitVfx, transform.position, Quaternion.identity);
            }

            OnHit?.Invoke(damage, knockbackDir);

            if (CurrentHp == 0)
            {
                OnDied?.Invoke();
            }
        }

        public void ApplyBlockReaction(Vector2 attackerDir)
        {
            if (IsDead) return;

            if (onBlockVfx != null)
            {
                Instantiate(onBlockVfx, transform.position, Quaternion.identity);
            }

            OnBlocked?.Invoke(attackerDir);
        }

        // =========================================================================
        // 편의 — 테스트/디버그용 (외부 검증 시 HP 리셋 가능)
        // =========================================================================

        /// <summary>HP를 maxHp로 되돌리고 "살아있음" 상태로 복귀. 테스트·리트라이용.</summary>
        public void ResetHp()
        {
            CurrentHp = maxHp;
        }

#if UNITY_EDITOR
        [ContextMenu("Dump State")]
        private void DumpState()
        {
            var matName = BodyMaterial != null ? BodyMaterial.displayName : "(no body)";
            Debug.Log($"[DummyTarget] hp={CurrentHp}/{maxHp}, body={matName}, dead={IsDead}", this);
        }
#endif
    }
}
