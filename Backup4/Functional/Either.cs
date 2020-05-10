using System;

namespace Backup4.Functional
{
    public class Either<TLeft, TRight>
    {
        private readonly TLeft _left;
        private readonly TRight _right;

        public readonly bool HasLeft;
        public bool HasRight => !HasLeft;

        public TLeft Left =>
            HasLeft
                ? _left
                : throw new InvalidOperationException(
                    $"This Either<{typeof(TLeft).Name}, {typeof(TRight).Name}> does not have a left value.");

        public TRight Right =>
            HasRight
                ? _right
                : throw new InvalidOperationException(
                    $"This Either<{typeof(TLeft).Name}, {typeof(TRight).Name}> does not have a right value.");

        public Either(TLeft left)
        {
            (_left, _right, HasLeft) = (left, default!, true);
        }

        public Either(TRight right)
        {
            (_left, _right, HasLeft) = (default!, right, false);
        }

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public TLeft LeftOr(TLeft replacement) => HasLeft ? Left : replacement;

        public TRight RightOr(TRight replacement) => HasRight ? Right : replacement;

        public TLeft Unite(Func<TRight, TLeft> func) => HasLeft ? Left : func(Right);

        public TRight Unite(Func<TLeft, TRight> func) => HasRight ? Right : func(Left);

        public TRes Match<TRes>(Func<TLeft, TRes> left, Func<TRight, TRes> right) =>
            HasLeft ? left(Left) : right(Right);

        public void Match(Action<TLeft> left, Action<TRight> right)
        {
            if (HasLeft)
            {
                left(Left);
            }
            else
            {
                right(Right);
            }
        }
    }
}