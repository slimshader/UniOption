﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bravasoft.Functional
{
    public readonly struct Option<T> : IEquatable<Option<T>>, IEquatable<T>, IEnumerable<T>
    {
        private readonly (bool IsSome, T Value) _data;

        public bool IsSome => _data.IsSome;
        public bool IsNone => !IsSome;

        public static readonly Option<T> None = default;
        public static Option<T> Some(T value) => new Option<T>(true, value);

        public bool TryGetValue(out T value)
        {
            if (IsSome)
            {
                value = _data.Value;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T IfNoneUnsafe(T fallback) => IsSome ? _data.Value : fallback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T IfNoneUnsafe(Func<T> onFallback)
        {
            if (onFallback is null)
                throw new ArgumentNullException(nameof(onFallback));

            return IsSome ? _data.Value : onFallback();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T IfNone(T fallback)
        {
            if (IsSome)
                return _data.Value;

            if (Check.IsNull(fallback))
                throw new ArgumentNullException(nameof(fallback));

            return fallback;
        }

        public T IfNone(Func<T> onFallback)
        {
            if (onFallback is null)
                throw new ArgumentNullException(nameof(onFallback));

            if (IsSome)
                return _data.Value;

            var fallback = onFallback();

            if (Check.IsNull(fallback))
                throw new OptionException("Fallback function produced null value");

            return fallback;
        }

        public U Match<U>(Func<T, U> onSome, Func<U> onNone)
        {
            if (IsSome)
            {
                var someMatch = onSome(_data.Value);

                if (Check.IsNull(someMatch))
                    throw new OptionException("Some function produced null value");

                return someMatch;
            }

            var noneMatch = onNone();

            if (Check.IsNull(noneMatch))
                throw new OptionException("None function produced null value");

            return noneMatch;
                
        }

        public Option<U> Map<U>(Func<T, U> map) => IsSome ? Prelude.Some(map(_data.Value)) : Option<U>.None;
        public Option<U> MapOptional<U>(Func<T, U> map) => IsSome ? (map(_data.Value) ?? Option<U>.None) : Option<U>.None;
        public Option<U> Bind<U>(Func<T, Option<U>> bind) => IsSome ? bind(_data.Value) : Option<U>.None;
        public Option<T> Filter(Func<T, bool> predicate) => IsSome && predicate(_data.Value) ? Some(_data.Value) : None;
        public Option<U> TryCast<U>() => IsSome && _data.Value is U u ? u : Option<U>.None;

        public U Fold<U>(U seed, Func<U, T, U> folder) => IsSome ? folder(seed, _data.Value) : seed;

        public void Deconstruct(out bool isSome, out T value) => (isSome, value) = _data;

        public bool Equals(Option<T> other) =>
            (!IsSome && !other.IsSome) || _data.Equals(other._data);

        public bool Equals(T value) => IsSome && EqualityComparer<T>.Default.Equals(_data.Value, value);

        public static implicit operator Option<T>(in T value) => Prelude.Optional(value);
        public static implicit operator Option<T>(Core.NoneType _) => None;
        public static implicit operator Option<T>(Unit _) => None;
        public static explicit operator T(in Option<T> ot) => ot.IsSome ? ot._data.Value : throw new OptionCastException();

        public static implicit operator bool(in Option<T> result) => result.IsSome;

        public static bool operator ==(in Option<T> option, in Option<T> other) => option.Equals(other);
        public static bool operator !=(in Option<T> option, in Option<T> other) => !option.Equals(other);

        public static bool operator ==(in Option<T> option, in T value) => option.Equals(value);
        public static bool operator !=(in Option<T> option, in T value) => !option.Equals(value);

        public static bool operator ==(in T value, in Option<T> option) => option.Equals(value);
        public static bool operator !=(in T value, in Option<T> option) => !option.Equals(value);

        public static bool operator true(in Option<T> option) => option.IsSome;
        public static bool operator false(in Option<T> option) => option.IsNone;

        public static Option<T> operator |(in Option<T> left, in Option<T> right) =>
            left.IsSome
            ? left
            : right.IsSome
            ? right
            : None;

        public static Option<U> Zip<T1, U>(in Option<T> option, Option<T1> other, Func<T, T1, U> zip)
        {
            if (option.TryGetValue(out var t) && other.TryGetValue(out var t1))
                return Option<U>.Some(zip(t, t1));

            return Option<U>.None;
        }

        public override bool Equals(object obj)
        {
            if (obj is Option<T> other)
                return Equals(other);

            if (obj is T value)
                return Equals(value);

            return false;
        }

        public override int GetHashCode() => _data.GetHashCode();

        public override string ToString() => IsSome ? $"Some({_data.Value})" : "None";

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new OptionEnumerator<T>(this);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        private Option(bool isSome, T value)
        {
            _data = (isSome, isSome ? Check.AssureNotNull(value, nameof(value)) : default);
        }
    }

    public static class Option
    {
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
        public static Option<T> None<T>() => Option<T>.None;
        public static Option<U> Select<T, U>(this in Option<T> option, Func<T, U> selector) =>
            option.Map(selector);

        public static Option<U> SelectMany<T, T1, U>(this in Option<T> option, Func<T, Option<T1>> selector, Func<T, T1, U> resultSelector) =>
            option.Bind(t => selector(t).Map(t1 => resultSelector(t, t1)));

        public static Option<T> Where<T>(this in Option<T> option, Func<T, bool> predicate) => option.Filter(predicate);

        public static Option<U> Map<T1, T2, U>(this in Option<ValueTuple<T1, T2>> option, Func<T1, T2, U> map) =>
            option.Map(t => map(t.Item1, t.Item2));

        public static Option<U> Map<T1, T2, T3, U>(this in Option<ValueTuple<T1, T2, T3>> option, Func<T1, T2, T3, U> map) =>
            option.Map(t => map(t.Item1, t.Item2, t.Item3));

        public static U Match<T1, T2, U>(this in Option<ValueTuple<T1, T2>> option, Func<T1, T2, U> onSome, Func<U> onNone) =>
            option.Match(t => onSome(t.Item1, t.Item2), onNone);

        public static U Match<T1, T2, T3, U>(this in Option<ValueTuple<T1, T2, T3>> option, Func<T1, T2, T3, U> onSome, Func<U> onNone) =>
            option.Match(t => onSome(t.Item1, t.Item2, t.Item3), onNone);

        public static Unit Iter<T>(this in Option<T> option, Action<T> onSome)
        {
            if (option.TryGetValue(out var v))
                onSome(v);

            return default;
        }

        public static Option<T> Tee<T>(this in Option<T> option, Action<T> onSome)
        {
            if (option.TryGetValue(out var v))
                onSome(v);

            return option;
        }

        public static Option<(TResult First, TResult Second)> PairBind<T, TResult>(this Option<(T First, T Second)> pair, Func<T, Option<TResult>> map) =>
            from tuple in pair
            let first = map(tuple.First)
            let second = map(tuple.Second)
            from f in first
            from s in second
            select (f, s);
    }

    public static partial class Prelude
    {
        public static Option<T> Flatten<T>(this in Option<Option<T>> stackedOption) => stackedOption.Bind(x => x);
        public static Option<Unit> When(bool condition, Option<Unit> alternative) => condition ? alternative : Option<Unit>.None;
        public static Option<Unit> Unless(bool condition, Option<Unit> alternative) => When(!condition, alternative);
    }
}
