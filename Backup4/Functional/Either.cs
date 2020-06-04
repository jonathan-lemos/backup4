using System;
using System.Collections;
using System.Collections.Generic;

namespace Backup4.Functional
{
    public class Either<TLeft, TRight>
    {
        private readonly TLeft _left;
        private readonly TRight _right;

        public TLeft Left =>
            HasLeft
                ? _left
                : throw new InvalidOperationException($"This {_className} has a Right value and not a Left value.");

        public Option<TLeft> LeftOption =>
            HasLeft ? _left! : Option<TLeft>.Empty;

        public TRight Right =>
            HasRight
                ? _right
                : throw new InvalidOperationException($"This {_className} has a Left value and not a Right value.");

        public Option<TRight> RightOption =>
            HasRight ? _right! : Option<TRight>.Empty;

        public bool HasLeft { get; }
        public bool HasRight => !HasLeft;

        public Either(TLeft left)
        {
            (_left, _right, HasLeft) = (left, default!, true);
        }

        public Either(TRight right)
        {
            (_left, _right, HasLeft) = (default!, right, false);
        }

        public TLeft Unite(Func<TRight, TLeft> func) => HasLeft ? _left : func(_right);

        public TRight Unite(Func<TLeft, TRight> func) => HasRight ? _right : func(_left);

        public Either<TNew, TRight> SelectLeft<TNew>(Func<TLeft, TNew> func) =>
            HasRight ? new Either<TNew, TRight>(_right) : new Either<TNew, TRight>(func(_left));

        public Either<TLeft, TNew> SelectRight<TNew>(Func<TRight, TNew> func) =>
            HasLeft ? new Either<TLeft, TNew>(_left) : new Either<TLeft, TNew>(func(_right));

        public bool LeftIs(Func<TLeft, bool> predicate) => HasLeft && predicate(_left);
        
        public bool RightIs(Func<TRight, bool> predicate) => HasRight && predicate(_right);

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public void Match(Action<TLeft> left, Action<TRight> right)
        {
            if (HasLeft)
            {
                left(_left);
            }
            else
            {
                right(_right);
            }
        }

        public TRes Match<TRes>(Func<TLeft, TRes> left, Func<TRight, TRes> right) =>
            HasLeft ? left(_left) : right(_right);

        private string _className => $"Either<{typeof(TLeft).Name}, {typeof(TRight).Name}>";
        
        public override string ToString() => (HasLeft ? _left!.ToString() : _right!.ToString())!;

        public IEnumerator<TLeft> LeftEnumerator()
        {
            if (HasLeft) yield return _left;
        }

        public IEnumerator<TRight> RightEnumerator()
        {
            if (HasRight) yield return _right;
        }
    }
}