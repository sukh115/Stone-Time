// -----------------------------------------------------------------------------
// InputRouterTests.cs
// Stone & Time — InputRouter의 Last Registered Input 큐 규칙을 단위 테스트로 고정.
//
// 실제 PlayerInput·InputValue 없이 PressDirection/ReleaseDirection helper로 검증.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using StoneAndTime.Input;

namespace StoneAndTime.Tests
{
    public class InputRouterTests
    {
        private GameObject _holder;
        private InputRouter _router;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestInputRouter");
            _router = _holder.AddComponent<InputRouter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void CurrentDirection_InitiallyNone()
        {
            Assert.AreEqual(InputRouter.Direction.None, _router.CurrentDirection);
        }

        [Test]
        public void PressOne_BecomesCurrent()
        {
            _router.PressDirection(InputRouter.Direction.Left);
            Assert.AreEqual(InputRouter.Direction.Left, _router.CurrentDirection);
        }

        [Test]
        public void PressTwo_LastWins()
        {
            _router.PressDirection(InputRouter.Direction.Left);
            _router.PressDirection(InputRouter.Direction.Right);

            Assert.AreEqual(InputRouter.Direction.Right, _router.CurrentDirection);
        }

        [Test]
        public void ReleaseLast_FallsBackToPrevious()
        {
            _router.PressDirection(InputRouter.Direction.Left);
            _router.PressDirection(InputRouter.Direction.Right);
            _router.ReleaseDirection(InputRouter.Direction.Right);

            Assert.AreEqual(InputRouter.Direction.Left, _router.CurrentDirection);
        }

        [Test]
        public void ReleaseAll_ReturnsToNone()
        {
            _router.PressDirection(InputRouter.Direction.Up);
            _router.PressDirection(InputRouter.Direction.Down);
            _router.ReleaseDirection(InputRouter.Direction.Up);
            _router.ReleaseDirection(InputRouter.Direction.Down);

            Assert.AreEqual(InputRouter.Direction.None, _router.CurrentDirection);
        }

        [Test]
        public void PressSameTwice_NoDuplicateInStack()
        {
            // 같은 방향을 두 번 눌러도 Release 한 번으로 완전히 사라져야 함
            _router.PressDirection(InputRouter.Direction.Right);
            _router.PressDirection(InputRouter.Direction.Right);
            _router.ReleaseDirection(InputRouter.Direction.Right);

            Assert.AreEqual(InputRouter.Direction.None, _router.CurrentDirection);
        }

        [Test]
        public void CurrentAxis_MatchesDirection()
        {
            _router.PressDirection(InputRouter.Direction.Left);
            Assert.AreEqual(Vector2.left, _router.CurrentAxis);

            _router.PressDirection(InputRouter.Direction.Up);
            Assert.AreEqual(Vector2.up, _router.CurrentAxis);
        }
    }
}
