using System;
using System.Collections;
using System.Collections.Generic;

namespace Backup4.Functional
{
    public readonly struct Option<T> : IEnumerable<T>
    {
        private readonly T _val;

        public bool HasValue { get; }

        public Option(T value)
        {
            (_val, HasValue) = (value, value != null);
        }

        public static Option<T> Empty => new Option<T>();
        
        public static implicit operator Option<T>(T value) => new Option<T>(value);

        public T Value => HasValue
            ? _val
            : throw new InvalidOperationException($"This Option<{typeof(T).Name}> does not have a value.");

        public void Match(Action<T> value, Action none)
        {
            if (HasValue)
            {
                value(_val);
            }
            else
            {
                none();
            }
        }

        public TRes Match<TRes>(Func<T, TRes> value, Func<TRes> none) => HasValue ? value(_val) : none();

        public Option<TNew> SelectOption<TNew>(Func<T, TNew> func) => HasValue ? new Option<TNew>(func(_val)) : Option<TNew>.Empty;

        public static implicit operator bool(Option<T> option) => option.HasValue;

        public static bool operator true(Option<T> option) => option.HasValue;
        
        public static bool operator false(Option<T> option) => !option.HasValue;
        
        public override string ToString() => (HasValue ? _val!.ToString() : $"Option<{typeof(T).Name}>.Empty")!;

        public IEnumerator<T> GetEnumerator()
        {
            if (HasValue) yield return _val;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}