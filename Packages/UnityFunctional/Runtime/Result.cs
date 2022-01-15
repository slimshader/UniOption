using System;
using System.Collections.Generic;
using System.Linq;

namespace Bravasoft.Unity.Functional
{
    public readonly struct Result<T>
    {
        private readonly T _value;
        private readonly Error _error;

        public readonly bool IsOk;

        public static Result<T> Ok(T value) => new Result<T>(true, value, default);
        public static Result<T> Fail(Error error) => new Result<T>(false, default, error);

        public Error Error => IsOk ? throw new InvalidOperationException() : _error;

        public Result<UValue> Map<UValue>(Func<T, UValue> map) =>
            IsOk ? (Result<UValue>) Result.Ok(map(_value)) : Result.Fail(_error);

        public Result<T> MapError(Func<Error, Error> errorMap) =>
            IsOk ? this : Result.Fail(errorMap(_error));

        public Result<UValue> BiMap<UValue>(Func<T, UValue> map, Func<Error, Error> errorMap) =>
            IsOk ? (Result<UValue>) Result.Ok(map(_value)) : Result.Fail(errorMap(_error));

        public Result<UValue> Bind<UValue>(Func<T, Result<UValue>> bind) =>
            IsOk ? bind(_value) : Result.Fail(_error);

        public U Match<U>(Func<T, U> onOk, Func<Error, U> onError) => IsOk ? onOk(_value) : onError(_error);

        public Option<T> ToOption() => IsOk ? Option<T>.Some(_value) : Option<T>.None;

        public IEnumerable<T> ToEnumerable()
        {
            if (IsOk) yield return _value;
        }

        public (bool IsOk, T Value, Error Error) AsTuple() => (IsOk, _value, _error);

        public void Deconstruct(out bool isOk, out T value, out Error error) => (isOk, value, error) = AsTuple();

        public static explicit operator T(in Result<T> result) =>
            result.IsOk ? result._value : throw new ResultCastException(result._error);

        public override string ToString() => IsOk ? $"Ok({_value})" : $"Fail({_error})";

        public static implicit operator Result<T>(in Result.ResultOk<T> ok) => Ok(ok.Value);
        public static implicit operator Result<T>(in Result.ResultFail fail) => Fail(fail.Error);

        private Result(bool isOk, T value, Error error)
        {
            IsOk = isOk;
            _value = isOk ? (value ?? throw new ArgumentNullException(nameof(value))) : default;
            _error = !isOk ? (error ?? throw new ArgumentNullException(nameof(value))) : default;
        }
    }

    public static class Result
    {
        public readonly struct ResultOk<TValue>
        {
            public readonly TValue Value;
            public ResultOk(TValue value) => Value = value;
        }

        public readonly struct ResultFail
        {
            public readonly Error Error;
            public ResultFail(Error error) => Error = error;
        }

        public static ResultOk<T> Ok<T>(T value) => new ResultOk<T>(value);
        public static ResultFail Fail(Error error) => new ResultFail(error);

        public static Result<Unit> Condition(Func<bool> cond, Error error) =>
            cond() ? (Result<Unit>)Ok(Unit.Default) : Fail(error);

        public static Result<Unit> Tee(Action action)
        {
            action();
            return Ok(Unit.Default);
        }

        public static Option<T> ToOption<T>(this Result<T> result) =>
            result.Match(Option<T>.Some, _ => Option<T>.None);

        public static Result<T> ToResult<T>(this in Option<T> option, Error error) =>
            option.Match(Result<T>.Ok, () => Result<T>.Fail(error));

        public static Result<T> ToResult<T>(this in Option<T> option, Func<Error> onError) =>
            option.Match(Result<T>.Ok, () => Result<T>.Fail(onError()));

        public static Result<TResult> Select<T, TResult>(this Result<T> result, Func<T, TResult> selector) =>
            result.Map(selector);

        public static Result<TResult> SelectMany<T, U, TResult>(this in Result<T> result,
            Func<T, Result<U>> selector,
            Func<T, U, TResult> resultSelector) =>
            result.Bind(tvalue => selector(tvalue).Bind<TResult>(uvalue => Ok(resultSelector(tvalue, uvalue))));

        public static Result<IEnumerable<U>> Traverse<T, U>(this IEnumerable<T> values, Func<T, Result<U>> f)
        {
            var seed = Result<IEnumerable<U>>.Ok(Enumerable.Empty<U>());
            return values.Aggregate(seed: seed, (acc, r) => from x in f(r)
                                                            from acc1 in acc
                                                            select acc1.Append(x));
        }
    }
}
