// -----------------------------------------------------------------------------
// MaterialData.cs
// Stone & Time — 재질 데이터 ScriptableObject
// ADR-0004: Hardness·재질 데이터 모델
//
// 런타임 수정 금지. 수정이 필요하면 Instantiate()로 복사본 사용할 것.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace StoneAndTime.Core
{
    /// <summary>
    /// 단일 재질(화강암·얼음·흑요석 등)의 물리·전투·VFX·SFX 데이터.
    /// 같은 재질을 여러 파츠가 공유 참조해도 되도록 ScriptableObject로 분리.
    /// 에셋 경로 규약: Assets/Data/Materials/MD_{Name}.asset
    /// </summary>
    [CreateAssetMenu(
        fileName = "MD_NewMaterial",
        menuName = "Stone&Time/Material Data",
        order = 0)]
    public class MaterialData : ScriptableObject
    {
        // =========================================================================
        // Identity
        // =========================================================================
        [Header("Identity")]
        [Tooltip("UI·로그에 표시할 이름. 한국어 가능.")]
        public string displayName;

        // =========================================================================
        // Combat
        // =========================================================================
        [Header("Combat")]
        [Tooltip("강도. 내 공격 파츠의 hardness가 대상보다 작거나 같으면 타격 실패.")]
        [Range(0, 100)] public int hardness = 10;

        // =========================================================================
        // Physics — Rigidbody2D & PhysicsMaterial2D 파라미터
        // =========================================================================
        [Header("Physics")]
        [Tooltip("Rigidbody2D.mass 로 반영. 무거운 재질일수록 높음.")]
        [Min(0.01f)] public float mass = 1f;

        [Tooltip("PhysicsMaterial2D.friction. 얼음은 0 근처, 화강암은 1 근처.")]
        [Range(0f, 1f)] public float friction = 0.4f;

        [Tooltip("Rigidbody2D.linearDamping (구 linearDrag). 공기·지면 저항.")]
        [Min(0f)] public float linearDrag = 0f;

        // =========================================================================
        // Weapon-Only
        // =========================================================================
        [Header("Weapon Only")]
        [Tooltip("무기 슬롯에 끼웠을 때 공격 속도 배율. 1.0이 기준.")]
        [Range(0.1f, 3f)] public float attackSpeedMultiplier = 1f;

        // =========================================================================
        // FX — 외부(아트·사운드 담당자)가 채우는 영역
        // =========================================================================
        [Header("FX")]
        [Tooltip("충돌·타격 시 재생할 사운드 클립 베리에이션. 랜덤 선택.")]
        public AudioClip[] hitSfx;

        [Tooltip("강도 판정 실패 시 파츠에 생기는 균열 파티클 프리팹.")]
        public GameObject crackVfx;

        // =========================================================================
        // 유틸리티
        // =========================================================================

        /// <summary>
        /// hitSfx 배열에서 무작위 선택. 비어 있으면 null.
        /// </summary>
        public AudioClip PickRandomHitSfx()
        {
            if (hitSfx == null || hitSfx.Length == 0) return null;
            return hitSfx[Random.Range(0, hitSfx.Length)];
        }

        /// <summary>
        /// 방어 체크. Hardness 비교에서 공격자가 실패하는 조건.
        /// </summary>
        public bool BlocksAttackFrom(MaterialData attackerMaterial)
        {
            if (attackerMaterial == null) return true;
            return attackerMaterial.hardness <= hardness;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 DisplayName이 비어 있으면 경고
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name; // 에셋 파일 이름을 기본값으로
            }
        }
#endif
    }
}
