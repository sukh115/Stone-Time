// -----------------------------------------------------------------------------
// HitStopManager.cs
// Stone & Time — 프레임 단위 타임스케일 동결("히트스탑")
//
// 설계 의도:
//   - "때리는 맛" 핵심: 히트 성공 순간 Time.timeScale을 0으로 2~4프레임 유지한 뒤 원복.
//   - 프레임 기준이라 fps 종속적이지만, 액션 감각을 위해 WaitForSecondsRealtime보다
//     `yield return null` 프레임 카운팅이 더 직관적이다.
//   - Time.timeScale이 0이어도 Update/LateUpdate/Coroutine의 `yield return null`은 돈다.
//     (물리·애니메이션만 멈춘다.)
//
// 싱글톤:
//   - 첫 호출 시 DontDestroyOnLoad GameObject를 자동 생성. 씬 로드 경계에서 안전.
//   - OnDisable에서 Time.timeScale을 "동결 이전 값"으로 반드시 복원 → 정지 상태 유출 방지.
//
// 엣지케이스:
//   - 이미 동결 중에 새 Freeze 요청이 오면 "최신 우선" — 기존 코루틴 중단 후 재시작.
//     짧은 hit-stop이 긴 hit-stop을 덮어쓰지 않도록, 요청 프레임 수가 현재 남은 프레임
//     수보다 작으면 무시(짧은 요청은 버림).
//   - Time.timeScale이 원래 0이 아닌 다른 값(예: 사륜안 감속 0.3)이었으면, 동결 해제 시
//     그 값으로 복원.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

namespace StoneAndTime.Combat
{
    /// <summary>
    /// 히트스탑 싱글톤. `HitStopManager.Instance.Freeze(3)` 형태로 호출.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitStopManager : MonoBehaviour
    {
        // =========================================================================
        // 싱글톤
        // =========================================================================
        private static HitStopManager _instance;

        /// <summary>
        /// 접근 시 자동 생성. 테스트에서는 TestUtils.CreateInstance()로 교체 가능.
        /// </summary>
        public static HitStopManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // 우선 씬에 이미 있는 인스턴스를 찾는다 (중복 방지).
                var existing = FindAnyObjectByType<HitStopManager>();
                if (existing != null)
                {
                    _instance = existing;
                    return _instance;
                }

                // 없으면 새로 생성.
                var go = new GameObject(nameof(HitStopManager));
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<HitStopManager>();
                return _instance;
            }
        }

        /// <summary>
        /// PlayMode 테스트 전용. 한 테스트 끝난 후 다음 테스트에서 깨끗한 상태로 시작하도록
        /// 현재 싱글톤을 해제. 실제 씬 런타임에서는 호출하지 않는다.
        /// </summary>
        public static void ResetForTests()
        {
            if (_instance != null)
            {
                _instance.UnfreezeImmediate();
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(_instance.gameObject);
                }
            }
            _instance = null;
        }

        // =========================================================================
        // 상태
        // =========================================================================

        /// <summary>현재 동결 중인가.</summary>
        public bool IsFrozen { get; private set; }

        /// <summary>동결 시작 시 저장된 원래 타임스케일. 해제 시 여기로 복원.</summary>
        private float _scaleBeforeFreeze = 1f;

        /// <summary>현재 남은 동결 프레임 수 (대략치). 더 긴 요청만 갱신 허용 판단용.</summary>
        private int _framesRemaining;

        private Coroutine _activeRoutine;

        // =========================================================================
        // API
        // =========================================================================

        /// <summary>
        /// 프레임 단위 동결. frames &lt;= 0이면 아무 일도 일어나지 않음.
        /// 이미 동결 중이면 "더 긴 요청만" 덮어쓴다.
        /// </summary>
        public void Freeze(int frames)
        {
            if (frames <= 0) return;

            if (IsFrozen && frames <= _framesRemaining)
            {
                // 짧은 요청은 무시 — 진행 중인 긴 동결을 깨지 않는다.
                return;
            }

            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            else
            {
                // 첫 동결 진입 — 원래 스케일 기억
                _scaleBeforeFreeze = Time.timeScale;
            }

            _framesRemaining = frames;
            IsFrozen = true;
            Time.timeScale = 0f;

            _activeRoutine = StartCoroutine(FreezeRoutine(frames));
        }

        /// <summary>
        /// 실시간 초 단위 동결. WaitForSecondsRealtime으로 대기 후 복원.
        /// 프레임 카운팅과 의미가 다르니 섞어쓰지 말 것.
        /// </summary>
        public void FreezeSeconds(float unscaledSeconds)
        {
            if (unscaledSeconds <= 0f) return;

            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            else
            {
                _scaleBeforeFreeze = Time.timeScale;
            }

            IsFrozen = true;
            Time.timeScale = 0f;
            _framesRemaining = int.MaxValue; // "매우 긴" 요청으로 간주

            _activeRoutine = StartCoroutine(FreezeRoutineSeconds(unscaledSeconds));
        }

        /// <summary>즉시 동결 해제. 게임 종료·씬 전환 등에서 호출.</summary>
        public void UnfreezeImmediate()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            if (IsFrozen)
            {
                Time.timeScale = _scaleBeforeFreeze;
                IsFrozen = false;
                _framesRemaining = 0;
            }
        }

        // =========================================================================
        // 내부 루틴
        // =========================================================================

        private IEnumerator FreezeRoutine(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                _framesRemaining = frames - i;
                yield return null; // yield return null은 timeScale=0에서도 돈다
            }
            FinishFreeze();
        }

        private IEnumerator FreezeRoutineSeconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            FinishFreeze();
        }

        private void FinishFreeze()
        {
            Time.timeScale = _scaleBeforeFreeze;
            IsFrozen = false;
            _framesRemaining = 0;
            _activeRoutine = null;
        }

        // =========================================================================
        // 안전장치 — 컴포넌트 비활성/파괴 시 반드시 복원
        // =========================================================================

        private void OnDisable()
        {
            // 코루틴이 중간에 끊겨도 Time.timeScale은 복원되어야 함
            if (IsFrozen)
            {
                Time.timeScale = _scaleBeforeFreeze;
                IsFrozen = false;
                _framesRemaining = 0;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
