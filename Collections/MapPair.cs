using PolyType;
using System;
using System.Collections.Generic;

namespace AssetManager.Collections
{
    public readonly struct MapPair<TFirst, TSecond> : IEquatable<MapPair<TFirst, TSecond>>
        where TFirst : notnull
    {
        [PropertyShape(Name = "first", Order = 0)]
        public readonly TFirst first;

        [PropertyShape(Name = "second", Order = 1)]
        public readonly TSecond second;

        [ConstructorShape]
        public MapPair(TFirst first, TSecond second)
        {
            this.first = first;
            this.second = second;
        }

        public void Deconstruct(out TFirst first, out TSecond second)
        {
            first = this.first;
            second = this.second;
        }

        public bool Equals(MapPair<TFirst, TSecond> other)
        {
            return EqualityComparer<TFirst>.Default.Equals(first, other.first)
                && EqualityComparer<TSecond>.Default.Equals(second, other.second);
        }

        public override bool Equals(object obj)
        {
            return obj is MapPair<TFirst, TSecond> other && Equals(other);
        }

        public static bool operator ==(
            MapPair<TFirst, TSecond> left,
            MapPair<TFirst, TSecond> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            MapPair<TFirst, TSecond> left,
            MapPair<TFirst, TSecond> right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = first is null
                    ? 0
                    : EqualityComparer<TFirst>.Default.GetHashCode(first);

                int hash2 = second is null
                    ? 0
                    : EqualityComparer<TSecond>.Default.GetHashCode(second);

                return (hash1 * 397) ^ hash2;
            }
        }
    }
}
