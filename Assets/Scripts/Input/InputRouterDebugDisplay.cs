// -----------------------------------------------------------------------------
// InputRouterDebugDisplay.cs
// Stone & Time — 임시 디버그 HUD.
//
// 표시:
//   Direction  — InputRouter.CurrentDirection
//   Axis       — InputRouter.CurrentAxis
//   Jump fires — Space 누를 때마다 누적
//   Attack fires — J/좌클릭 누를 때마다 누적
//   Mass       — 붙어있는 Rigidbody2D.mass (FootPart 스왑 확인용)
//   Drag       — Rigidbody2D.linearDamping
//   Friction   — Collider2D.sharedMaterial.friction
//
// Phase 1 검증 끝나면 컴포넌트 삭제 권장.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace StoneAndTime.Input
{
    [RequireComponent(typeof(InputRouter))]
    public class InputRouterDebugDisplay : MonoBehaviour
    {
        private InputRouter _router;
        private Rigidbody2D _body;
        private Collider2D _collider;

        private int _jumpFireCount;
        private int _attackFireCount;

        // OnGUI 호출 여부를 눈으로 확인할 카운터 (프레임마다 증가)
        private int _onGuiTicks;
        // 배경용 1x1 솔리드 텍스처 (기본 GUI.Box는 에디터 스킨에 따라 매우 투명할 수 있음)
        private Texture2D _bgTex;

        private void Awake()
        {
            _router   = GetComponent<InputRouter>();
            _body     = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            _bgTex.Apply();
        }

        private void Start()
        {
            Debug.Log($"[InputRouterDebugDisplay] Start on '{name}'. " +
                      $"router={(_router != null)}, body={(_body != null)}, collider={(_collider != null)}",
                      this);
        }

        private void Update()
        {
            if (_router == null) return;
            if (_router.JumpPressedThisFrame)   _jumpFireCount++;
            if (_router.AttackPressedThisFrame) _attackFireCount++;
        }

        private void OnGUI()
        {
            _onGuiTicks++;

            // 강한 대비 스타일 — 혹시 기본 skin이 이상해도 보이게.
            var style = new GUIStyle
            {
                fontSize = 18,
                normal = { textColor = Color.yellow },
                richText = false,
            };

            const float w = 380f;
            const float h = 210f;

            // 배경 (확실히 보이는 반투명 검정)
            GUI.DrawTexture(new Rect(5, 5, w, h), _bgTex, ScaleMode.StretchToFill);

            float y = 10f;
            GUI.Label(new Rect(10, y, w - 10, 22), $"[HUD alive] OnGUI ticks: {_onGuiTicks}",       style); y += 22;

            if (_router == null)
            {
                GUI.Label(new Rect(10, y, w - 10, 22), "InputRouter 가 null 입니다.", style);
                return;
            }

            GUI.Label(new Rect(10, y, w - 10, 22), $"Direction    : {_router.CurrentDirection}", style); y += 22;
            GUI.Label(new Rect(10, y, w - 10, 22), $"Axis         : {_router.CurrentAxis}",      style); y += 22;
            GUI.Label(new Rect(10, y, w - 10, 22), $"Jump fires   : {_jumpFireCount}",            style); y += 22;
            GUI.Label(new Rect(10, y, w - 10, 22), $"Attack fires : {_attackFireCount}",          style); y += 22;

            // Rigidbody2D / Collider2D 정보 (FootPart 스왑 검증)
            var mass    = _body != null ? _body.mass.ToString("F2")          : "—";
            var damping = _body != null ? _body.linearDamping.ToString("F2") : "—";
            var fric    = _collider != null && _collider.sharedMaterial != null
                ? _collider.sharedMaterial.friction.ToString("F2")
                : "—";

            GUI.Label(new Rect(10, y, w - 10, 22), $"Mass         : {mass}",    style); y += 22;
            GUI.Label(new Rect(10, y, w - 10, 22), $"Drag         : {damping}", style); y += 22;
            GUI.Label(new Rect(10, y, w - 10, 22), $"Friction     : {fric}",    style);
        }
    }
}
