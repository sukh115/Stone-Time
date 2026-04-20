// -----------------------------------------------------------------------------
// InputRouter.cs
// Stone & Time — 이동 입력 4방향 스냅 + Last Registered Input 큐
//
// 설계 의도:
//   - 2D 액션에서 45도 이동은 버그 유발 지름길. 입력을 8방향이 아닌 4방향으로 스냅한다.
//   - 플레이어가 왼쪽을 누른 채 오른쪽을 추가로 누르면, 가장 최근에 눌린 키를 따른다.
//     (Super Mario·Celeste 등 고전 계승) 이를 "Last Registered Input" 큐로 구현.
//   - Unity New Input System의 PlayerInput 컴포넌트 또는 InputAction을 통해 입력을 받는다.
//     여기서는 PlayerInput의 SendMessages 방식을 가정.
//
// 외부 사용:
//   FootController.Move(router.CurrentAxis) 같은 방식으로 CurrentAxis를 폴링한다.
//
// 주의:
//   - 이 스크립트는 "입력을 해석"만 한다. 실제 Rigidbody2D 제어는 Foot/MovementController 책임.
//   - TODO: 아날로그 조이스틱 지원 추가 시 deadzone + 45도 절단 로직을 별도 분기로.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoneAndTime.Input
{
    /// <summary>
    /// 4방향 + Last Registered Input 큐를 구현하는 입력 라우터.
    /// GameObject에 PlayerInput과 함께 붙이거나, InputAction을 직접 바인딩해서 사용.
    /// </summary>
    [DisallowMultipleComponent]
    public class InputRouter : MonoBehaviour
    {
        // =========================================================================
        // 방향 정의
        // =========================================================================
        public enum Direction
        {
            None,
            Left,
            Right,
            Up,
            Down,
        }

        // =========================================================================
        // 상태
        // =========================================================================

        /// <summary>현재 눌린 키들의 스택. top(마지막 원소)이 가장 최근 입력.</summary>
        private readonly List<Direction> _stack = new(4);

        /// <summary>현재 유효 방향. 외부(이동 시스템)가 이 값을 폴링.</summary>
        public Direction CurrentDirection => _stack.Count == 0 ? Direction.None : _stack[^1];

        /// <summary>현재 유효 방향을 단위 벡터로. 예: Right = (1, 0).</summary>
        public Vector2 CurrentAxis => DirectionToAxis(CurrentDirection);

        /// <summary>점프 입력이 이번 프레임에 들어왔는지 여부. Update에서 소비 후 자동 초기화.</summary>
        public bool JumpPressedThisFrame { get; private set; }

        /// <summary>공격 입력이 이번 프레임에 들어왔는지 여부. Update에서 소비 후 자동 초기화.</summary>
        public bool AttackPressedThisFrame { get; private set; }

        // =========================================================================
        // Unity lifecycle
        // =========================================================================
        private void LateUpdate()
        {
            // 1 프레임만 유효. 소비되지 않았어도 다음 프레임에는 리셋.
            JumpPressedThisFrame = false;
            AttackPressedThisFrame = false;
        }

        // =========================================================================
        // PlayerInput SendMessages 콜백
        // =========================================================================
        // Input Action Asset에 다음 액션이 있어야 한다:
        //   - MoveLeft  (Button, Hold)
        //   - MoveRight (Button, Hold)
        //   - MoveUp    (Button, Hold)   // Post-MVP: 사륜안 상방 타겟팅 예약
        //   - MoveDown  (Button, Hold)   // Post-MVP: 내려가기 예약
        //   - Jump      (Button, Press)
        //   - Attack    (Button, Press)
        //
        // 각 액션은 Send Messages 방식에서 "OnMoveLeft"처럼 메서드로 디스패치.

        public void OnMoveLeft(InputValue value)  => HandleDirection(Direction.Left,  value);
        public void OnMoveRight(InputValue value) => HandleDirection(Direction.Right, value);
        public void OnMoveUp(InputValue value)    => HandleDirection(Direction.Up,    value);
        public void OnMoveDown(InputValue value)  => HandleDirection(Direction.Down,  value);

        public void OnJump(InputValue value)
        {
            if (value.isPressed) JumpPressedThisFrame = true;
        }

        public void OnAttack(InputValue value)
        {
            if (value.isPressed) AttackPressedThisFrame = true;
        }

        // =========================================================================
        // 큐 관리
        // =========================================================================

        /// <summary>
        /// 방향키 눌림/해제 처리. Last Registered Input 스택을 유지.
        /// </summary>
        private void HandleDirection(Direction dir, InputValue value)
        {
            if (value.isPressed)
            {
                // 같은 방향 중복 제거 후 맨 뒤(=최신)에 추가
                _stack.Remove(dir);
                _stack.Add(dir);
            }
            else
            {
                _stack.Remove(dir);
            }
        }

        // =========================================================================
        // 변환 유틸
        // =========================================================================
        public static Vector2 DirectionToAxis(Direction dir)
        {
            switch (dir)
            {
                case Direction.Left:  return Vector2.left;
                case Direction.Right: return Vector2.right;
                case Direction.Up:    return Vector2.up;
                case Direction.Down:  return Vector2.down;
                default:              return Vector2.zero;
            }
        }

        // =========================================================================
        // 테스트·디버그용
        // =========================================================================
        //
        // EditMode 테스트는 InputValue를 만들 방법이 없다. 그래서 내부 로직을
        // 외부에서 직접 호출할 수 있는 PressDirection / ReleaseDirection helper를 노출.
        // 실제 게임에서는 OnMoveLeft 등이 호출되므로 이 helper는 쓰이지 않는다.

        /// <summary>테스트·디버그 전용. 방향키 눌림을 직접 시뮬레이션.</summary>
        public void PressDirection(Direction dir)
        {
            if (dir == Direction.None) return;
            _stack.Remove(dir);
            _stack.Add(dir);
        }

        /// <summary>테스트·디버그 전용. 방향키 뗌을 직접 시뮬레이션.</summary>
        public void ReleaseDirection(Direction dir)
        {
            _stack.Remove(dir);
        }
    }
}
