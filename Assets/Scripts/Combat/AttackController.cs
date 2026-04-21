// -----------------------------------------------------------------------------
// AttackController.cs
// Stone & Time — 공격 입력 → 판정 스캔 → Resolver → Outcome 적용
// ADR-0003, ADR-0004
//
// 책임:
//   1. InputRouter.AttackPressedThisFrame 소비 (프레임당 1회)
//   2. 쿨다운 체크. 쿨다운 = baseCooldownSeconds / Weapon.AttackSpeedMultiplier
//   3. 전방 OverlapBox로 피격 대상 스캔
//   4. 각 대상에 대해 CombatResolver.Resolve → Hit/Blocked 분기 실행
//   5. Hit 성공 시 HitStopManager.Freeze(히트스탑 프레임)
//   6. Knockback 방향 = 공격자 → 대상 단위 벡터
//
// 요구사항:
//   - 같은 GameObject에 EquipmentManager, InputRouter가 이미 있어야 함.
//     (RequireComponent로 강제)
//   - 무기가 없으면 아무 것도 안 함(경고 로그 1회).
//   - 자기 자신을 때리지 않도록 IDamageable 컴포넌트가 본인 GameObject라면 스킵.
//
// 디자인 노트:
//   - 쿨다운에 Time.time 사용 → timeScale=0일 때 자연스럽게 쿨다운도 정지. 원하는 거동.
//   - Physics2D.OverlapBoxNonAlloc으로 GC 회피. 버퍼 크기 8 (동시 8명 이상 타격은 사양 밖).
//   - 공격 방향: InputRouter.CurrentAxis가 0 방향이면 마지막 비영 수평 방향을 유지.
//     시작 시 기본값은 +X (오른쪽).
// -----------------------------------------------------------------------------

using UnityEngine;
using StoneAndTime.Core;
using StoneAndTime.Input;
using StoneAndTime.Parts;

namespace StoneAndTime.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentManager))]
    [RequireComponent(typeof(InputRouter))]
    public class AttackController : MonoBehaviour
    {
        // =========================================================================
        // Inspector — 쿨다운
        // =========================================================================
        [Header("Cooldown")]
        [Tooltip("기본 공격 쿨다운(초). 실제 쿨다운은 이 값 / Weapon.AttackSpeedMultiplier.")]
        [Min(0.01f)] [SerializeField] private float baseCooldownSeconds = 0.4f;

        // =========================================================================
        // Inspector — 히트박스
        // =========================================================================
        [Header("Hitbox (local space, facing +X)")]
        [Tooltip("공격자 위치에서 히트박스 중심까지의 로컬 오프셋(facing 방향 기준).")]
        [SerializeField] private Vector2 hitboxLocalCenter = new Vector2(0.8f, 0f);

        [Tooltip("히트박스 크기(가로, 세로).")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.2f);

        [Tooltip("히트 판정에 쓰일 레이어 마스크. 기본 Everything.")]
        [SerializeField] private LayerMask hitLayers = ~0;

        // =========================================================================
        // Inspector — 히트스탑 / 넉백
        // =========================================================================
        [Header("Feedback")]
        [Tooltip("Hit 성공 시 HitStopManager에 요청할 프레임 수. 0이면 히트스탑 없음.")]
        [Min(0)] [SerializeField] private int hitStopFrames = 3;

        [Tooltip("Blocked 시 HitStopManager 프레임 수. 막힐 때도 살짝 체감주려면 1~2프레임.")]
        [Min(0)] [SerializeField] private int blockStopFrames = 1;

        // =========================================================================
        // Inspector — 디버그
        // =========================================================================
        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool verboseLog = false;

        // =========================================================================
        // 상태
        // =========================================================================
        private EquipmentManager _equipment;
        private InputRouter _input;

        /// <summary>마지막 공격 시각(Time.time 기준). 쿨다운 판정에 사용.</summary>
        private float _lastAttackTime = -999f;

        /// <summary>공격 방향 캐시. 초기값은 +X(오른쪽).</summary>
        private Vector2 _facing = Vector2.right;

        /// <summary>Physics2D.OverlapBox(ContactFilter2D, Collider2D[]) 버퍼. GC 회피.</summary>
        private readonly Collider2D[] _hitBuffer = new Collider2D[8];

        /// <summary>스캔용 ContactFilter2D. hitLayers 와 트리거 여부를 묶어 캐시.</summary>
        private ContactFilter2D _hitFilter;

        /// <summary>무기가 없을 때 경고를 매 프레임 뱉지 않도록 1회만.</summary>
        private bool _warnedNoWeapon;

        // =========================================================================
        // Unity lifecycle
        // =========================================================================

        private void Awake()
        {
            _equipment = GetComponent<EquipmentManager>();
            _input     = GetComponent<InputRouter>();

            // Unity 6에서 Physics2D.OverlapBoxNonAlloc이 deprecated. 새 API는 ContactFilter2D 기반.
            // useTriggers = true 로 기존 Physics2D.queriesHitTriggers 기본(true) 과 동등 거동.
            _hitFilter = new ContactFilter2D { useTriggers = true };
            _hitFilter.SetLayerMask(hitLayers); // SetLayerMask가 useLayerMask = true 도 같이 켠다
        }

        private void Update()
        {
            // 방향 캐시 갱신 — 비영 수평 입력이 있을 때만 업데이트
            var ax = _input.CurrentAxis;
            if (Mathf.Abs(ax.x) > 0.01f)
            {
                _facing = new Vector2(Mathf.Sign(ax.x), 0f);
            }

            // 공격 입력 소비 (프레임당 1회)
            if (_input.AttackPressedThisFrame)
            {
                TryAttack();
            }
        }

        // =========================================================================
        // 공격 시도
        // =========================================================================

        /// <summary>
        /// 공격 시도. 외부(AI, Combo 시스템 등)가 직접 호출할 수도 있게 public 유지.
        /// </summary>
        /// <returns>쿨다운 또는 무기 부재로 스킵되면 false.</returns>
        public bool TryAttack()
        {
            var weapon = _equipment.GetEquipped(SlotType.Weapon) as WeaponPart;
            if (weapon == null)
            {
                if (!_warnedNoWeapon)
                {
                    Debug.LogWarning("[AttackController] No weapon equipped; attack skipped.", this);
                    _warnedNoWeapon = true;
                }
                return false;
            }
            _warnedNoWeapon = false;

            // 쿨다운 체크
            float cooldown = GetCooldownFor(weapon);
            if (Time.time < _lastAttackTime + cooldown)
            {
                return false;
            }

            _lastAttackTime = Time.time;
            PerformScanAndResolve(weapon);
            return true;
        }

        /// <summary>현재 무기 기준 실효 쿨다운(초).</summary>
        public float GetCooldownFor(WeaponPart weapon)
        {
            if (weapon == null || weapon.AttackSpeedMultiplier <= 0f) return baseCooldownSeconds;
            return baseCooldownSeconds / weapon.AttackSpeedMultiplier;
        }

        // =========================================================================
        // 판정 스캔
        // =========================================================================

        private void PerformScanAndResolve(WeaponPart weapon)
        {
            var hitboxCenter = (Vector2)transform.position + GetHitboxWorldOffset();

            // Unity 6 신규 API — ContactFilter2D 기반. 할당 없이 _hitBuffer에 최대 8개 채움.
            int count = Physics2D.OverlapBox(
                hitboxCenter,
                hitboxSize,
                0f,
                _hitFilter,
                _hitBuffer
            );

            if (verboseLog)
            {
                Debug.Log($"[AttackController] Scan found {count} colliders at {hitboxCenter} facing {_facing}.", this);
            }

            bool anyHitSuccess = false;
            bool anyBlocked    = false;

            // 버퍼 포화 경고 — 9번째 이상 타깃은 소리 없이 누락되므로, 디버깅할 때 감지 가능하도록.
            if (verboseLog && count == _hitBuffer.Length)
            {
                Debug.LogWarning($"[AttackController] Hit buffer saturated ({_hitBuffer.Length}). 일부 타깃이 누락됐을 수 있음.", this);
            }

            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer[i];
                if (col == null) continue;

                // IDamageable 스캔 (자식 콜라이더도 허용)
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                // Unity null 가드 — IDamageable 인터페이스 포인터는 "fake null"이 안 걸린다.
                // 구현체가 UnityEngine.Object이면서 파괴된 상태일 수 있으므로 명시적으로 비교.
                if (damageable is UnityEngine.Object uObj && uObj == null) continue;

                // 자기 자신 제외 — damageable의 GameObject 기준으로 비교 (Component 계층 무관)
                if (damageable is Component comp && comp.gameObject == gameObject) continue;

                // Resolve
                var outcome = CombatResolver.Resolve(weapon, damageable, out int damage);
                var knockDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                if (knockDir == Vector2.zero) knockDir = _facing;

                switch (outcome)
                {
                    case AttackOutcome.Hit:
                        damageable.ApplyHit(damage, knockDir);
                        anyHitSuccess = true;
                        if (verboseLog)
                        {
                            Debug.Log($"[AttackController] HIT on {col.name} for {damage}.", col);
                        }
                        break;

                    case AttackOutcome.Blocked:
                        damageable.ApplyBlockReaction(knockDir);
                        anyBlocked = true;
                        if (verboseLog)
                        {
                            Debug.Log($"[AttackController] BLOCKED by {col.name}.", col);
                        }
                        break;

                    case AttackOutcome.NoTarget:
                    default:
                        break;
                }
                // 주의: `_hitBuffer[i] = null;` 은 불필요. OverlapBoxNonAlloc은 다음 호출 시
                // 0..count 범위를 덮어쓰고, 본 루프는 i < count만 순회하므로 잔존 참조는 읽히지 않음.
            }

            // 히트스탑 우선순위 — Hit > Blocked
            if (anyHitSuccess && hitStopFrames > 0)
            {
                HitStopManager.Instance.Freeze(hitStopFrames);
            }
            else if (anyBlocked && blockStopFrames > 0)
            {
                HitStopManager.Instance.Freeze(blockStopFrames);
            }
        }

        /// <summary>히트박스 중심의 월드 오프셋 (facing 방향 적용).</summary>
        private Vector2 GetHitboxWorldOffset()
        {
            // 로컬 기준 오프셋이 +X 가정 → facing.x 부호로 좌우 미러
            return new Vector2(hitboxLocalCenter.x * Mathf.Sign(_facing.x), hitboxLocalCenter.y);
        }

        // =========================================================================
        // Gizmos (Scene View 디버그)
        // =========================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // 런타임 정보가 없는 Edit 모드에서도 기본 facing=+X로 그린다.
            Vector2 center = (Vector2)transform.position
                + new Vector2(hitboxLocalCenter.x, hitboxLocalCenter.y);

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(center, hitboxSize);
        }
#endif
    }
}
