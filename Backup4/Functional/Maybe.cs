using System;

namespace Backup4.Functional
{
    public readonly struct Maybe<T>
    {
        public readonly bool HasValue;
        private readonly T _value;

        public T Value => HasValue
            ? _value
            : throw new InvalidOperationException($"This Maybe<{typeof(T).Name}> does not have a value.");

        public Maybe(T val)
        {
            HasValue = true;
            _value = val;
        }

        public static Maybe<T> None => new Maybe<T>();

        public static implicit operator Maybe<T>(T val) => new Maybe<T>(val);

        public T ValueOr(T replacement) => HasValue ? Value : replacement;

        public Maybe<TNew> Select<TNew>(Func<T, TNew> func) =>
            HasValue ? new Maybe<TNew>(func(_value)) : Maybe<TNew>.None;

        public TRes Match<TRes>(Func<T, TRes> value, Func<TRes> none) =>
            HasValue ? value(Value) : none();

        public void Match(Action<T> value, Action none)
        {
            if (HasValue)
            {
                value(Value);
            }
            else
            {
                none();
            }
        }
    }
}