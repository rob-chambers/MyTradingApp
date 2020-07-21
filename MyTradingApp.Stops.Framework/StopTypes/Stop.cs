using MyTradingApp.Domain;
using System;

namespace MyTradingApp.Stops.StopTypes
{
    public abstract class Stop : IComparable<Stop>, IEquatable<Stop>
    {
        public abstract StopType Type { get; }

        public double? InitiateAtGainPercentage { get; set; }

        public double Price { get; protected set; }        

        public abstract void CalculatePrice(Position position, double gainPercentage, double high, double low);

        public int CompareTo(Stop other)
        {
            if (other != null)
            {
                var a = InitiateAtGainPercentage.GetValueOrDefault();
                var b = other.InitiateAtGainPercentage.GetValueOrDefault();

                return a == b 
                    ? 0
                    : a < b
                        ? -1
                        : 1;
            }

            return 1;
        }

        public bool Equals(Stop other)
        {
            if (other == null)
            {
                return false;
            }

            return Type == other.Type && InitiateAtGainPercentage == other.InitiateAtGainPercentage && Price == other.Price;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Stop);
        }

        public override int GetHashCode()
        {
            return InitiateAtGainPercentage.GetValueOrDefault().GetHashCode();
        }

        public virtual void Reset()
        {
        }
    }
}