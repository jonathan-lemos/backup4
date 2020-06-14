using System;
using System.Collections.Generic;
using System.Linq;
using Backup4.Misc;

namespace Backup4.Functional
{
    public static class Result
    {
        public static Result<IList<TValue>, AggregateException> Combine<TValue, TException>(
            this IEnumerable<Result<TValue, TException>> enumerable) where TException : Exception
        {
            var values = new List<TValue>();
            var exceptions = new List<TException>();

            enumerable.ForEach(res => res.Match(
                val => values.Add(val),
                ex => exceptions.Add(ex)
            ));

            if (exceptions.Any())
            {
                return new AggregateException(exceptions);
            }

            return values;
        }

        public static Result<AggregateException> Combine<TException>(
            this IEnumerable<Result<TException>> enumerable) where TException : Exception
        {
            var exceptions = enumerable
                .Where(x => !x)
                .Select(x => x.Error)
                .ToList();

            return exceptions.Any() ? new AggregateException(exceptions) : Result<AggregateException>.Success;
        }

        public static Result<TValue, Exception> Of<TValue>(Func<TValue> func) => new Result<TValue, Exception>(func);
        public static Result<Exception> Of(Action func) => new Result<Exception>(func);
    }

    public class Result<TError> where TError : Exception
    {
        private readonly TError _err;

        public bool IsError { get; }
        public bool IsSuccess => !IsError;

        public Result(Action func)
        {
            try
            {
                func();
                (_err, IsError) = (default!, false);
            }
            catch (TError e)
            {
                (_err, IsError) = (e, true);
            }
        }

        public Result(TError value)
        {
            (_err, IsError) = (value, true);
        }

        public Result()
        {
            (_err, IsError) = (default!, false);
        }

        public static Result<TError> Success => new Result<TError>();

        public static implicit operator Result<TError>(TError value) => new Result<TError>(value);

        public TError Error => IsError
            ? _err
            : throw new InvalidOperationException($"This Result<{typeof(TError).Name}> is not an error.");

        public void Match(Action none, Action<TError> err)
        {
            if (IsError)
            {
                err(_err);
            }
            else
            {
                none();
            }
        }

        public TRes Match<TRes>(Func<TRes> none, Func<TError, TRes> err) => IsError ? err(_err) : none();

        public Result<TNew> SelectResult<TNew>(Func<TError, TNew> func) where TNew : Exception =>
            IsError ? new Result<TNew>(func(_err)) : Result<TNew>.Success;

        public static implicit operator bool(Result<TError> res) => res.IsSuccess;

        public static bool operator true(Result<TError> res) => res.IsSuccess;

        public static bool operator false(Result<TError> res) => res.IsError;

        public override string ToString() => (IsError ? _err!.ToString() : $"Result<{typeof(TError).Name}>.Success")!;
    }

    public class Result<TValue, TError> where TError : Exception
    {
        private readonly TValue _val;
        private readonly TError _err;

        public TValue Value =>
            HasValue
                ? _val
                : throw new InvalidOperationException($"This {_className} has an error and not a value.");

        public Option<TValue> ValueOption =>
            HasValue ? _val! : Option<TValue>.Empty;

        public TError Error =>
            HasError
                ? _err
                : throw new InvalidOperationException($"This {_className} has a value and not an error.");

        public Option<TError> RightOption =>
            HasError ? _err! : Option<TError>.Empty;

        public bool HasValue { get; }
        public bool HasError => !HasValue;

        public Result(Func<TValue> func)
        {
            try
            {
                (_val, _err, HasValue) = (func(), default!, true);
            }
            catch (TError e)
            {
                (_val, _err, HasValue) = (default!, e, false);
            }
        }

        public Result(TValue val)
        {
            (_val, _err, HasValue) = (val, default!, true);
        }

        public Result(TError err)
        {
            (_val, _err, HasValue) = (default!, err, false);
        }

        public Result<TNew, TError> SelectValue<TNew>(Func<TValue, TNew> func) =>
            HasError ? new Result<TNew, TError>(_err) : new Result<TNew, TError>(func(_val));

        public Result<TValue, TNew> SelectError<TNew>(Func<TError, TNew> func) where TNew : Exception =>
            HasValue ? new Result<TValue, TNew>(_val) : new Result<TValue, TNew>(func(_err));

        public static implicit operator Result<TValue, TError>(TValue left) => new Result<TValue, TError>(left);

        public static implicit operator Result<TValue, TError>(TError right) => new Result<TValue, TError>(right);

        public void Match(Action<TValue> left, Action<TError> right)
        {
            if (HasValue)
            {
                left(_val);
            }
            else
            {
                right(_err);
            }
        }

        public TRes Match<TRes>(Func<TValue, TRes> left, Func<TError, TRes> right) =>
            HasValue ? left(_val) : right(_err);

        public static implicit operator Result<TError>(Result<TValue, TError> res) =>
            res.HasError ? new Result<TError>(res._err) : Result<TError>.Success;

        public static implicit operator bool(Result<TValue, TError> res) => res.HasValue;

        public static bool operator true(Result<TValue, TError> res) => res.HasValue;

        public static bool operator false(Result<TValue, TError> res) => !res.HasValue;

        private string _className => $"Result<{typeof(TValue).Name}, {typeof(TError).Name}>";

        public override string ToString() => (HasValue ? _val!.ToString() : _err!.Message)!;
    }
}