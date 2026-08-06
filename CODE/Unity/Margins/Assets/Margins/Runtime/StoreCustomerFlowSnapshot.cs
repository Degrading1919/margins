using System;
using System.Collections.Generic;

namespace Margins
{
    public enum StoreCustomerState
    {
        Entering = 0,
        Shopping = 1,
        Queueing = 2,
        Checkout = 3,
        Leaving = 4
    }

    [Serializable]
    public sealed class StoreCustomerSnapshot : IEquatable<StoreCustomerSnapshot>
    {
        public string customerId;
        public StoreCustomerState state;
        public List<string> requestedProductIds = new();
        public List<string> reservedPhysicalUnitIds = new();
        public float patienceSeconds;
        public float phaseSeconds;
        public float positionX;
        public float positionY;
        public float positionZ;
        public bool wasAbandoned;

        public StoreCustomerSnapshot()
        {
        }

        public StoreCustomerSnapshot(
            string customerId,
            StoreCustomerState state,
            IEnumerable<string> requestedProductIds,
            IEnumerable<string> reservedPhysicalUnitIds,
            float patienceSeconds,
            float phaseSeconds,
            float positionX,
            float positionY,
            float positionZ,
            bool wasAbandoned)
        {
            this.customerId = customerId;
            this.state = state;
            this.requestedProductIds = requestedProductIds == null
                ? new List<string>()
                : new List<string>(requestedProductIds);
            this.reservedPhysicalUnitIds = reservedPhysicalUnitIds == null
                ? new List<string>()
                : new List<string>(reservedPhysicalUnitIds);
            this.patienceSeconds = patienceSeconds;
            this.phaseSeconds = phaseSeconds;
            this.positionX = positionX;
            this.positionY = positionY;
            this.positionZ = positionZ;
            this.wasAbandoned = wasAbandoned;
        }

        public bool Equals(StoreCustomerSnapshot other)
        {
            return other != null &&
                   string.Equals(customerId, other.customerId, StringComparison.Ordinal) &&
                   state == other.state &&
                   AreEqual(requestedProductIds, other.requestedProductIds) &&
                   AreEqual(reservedPhysicalUnitIds, other.reservedPhysicalUnitIds) &&
                   patienceSeconds.Equals(other.patienceSeconds) &&
                   phaseSeconds.Equals(other.phaseSeconds) &&
                   positionX.Equals(other.positionX) &&
                   positionY.Equals(other.positionY) &&
                   positionZ.Equals(other.positionZ) &&
                   wasAbandoned == other.wasAbandoned;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StoreCustomerSnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(customerId, StringComparer.Ordinal);
            hash.Add(state);
            hash.Add(patienceSeconds);
            hash.Add(phaseSeconds);
            hash.Add(positionX);
            hash.Add(positionY);
            hash.Add(positionZ);
            hash.Add(wasAbandoned);
            AddRange(ref hash, requestedProductIds);
            AddRange(ref hash, reservedPhysicalUnitIds);
            return hash.ToHashCode();
        }

        private static bool AreEqual(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private static void AddRange(ref HashCode hash, IEnumerable<string> values)
        {
            if (values == null)
            {
                hash.Add(0);
                return;
            }

            foreach (string value in values)
            {
                hash.Add(value, StringComparer.Ordinal);
            }
        }
    }

    [Serializable]
    public sealed class StoreCustomerFlowSnapshot :
        IEquatable<StoreCustomerFlowSnapshot>
    {
        public int nextCustomerOrdinal = 1;
        public float secondsUntilNextArrival;
        public List<StoreCustomerSnapshot> customers = new();

        public StoreCustomerFlowSnapshot()
        {
        }

        public StoreCustomerFlowSnapshot(
            int nextCustomerOrdinal,
            float secondsUntilNextArrival,
            IEnumerable<StoreCustomerSnapshot> customers)
        {
            this.nextCustomerOrdinal = nextCustomerOrdinal;
            this.secondsUntilNextArrival = secondsUntilNextArrival;
            this.customers = customers == null
                ? new List<StoreCustomerSnapshot>()
                : new List<StoreCustomerSnapshot>(customers);
        }

        public bool Equals(StoreCustomerFlowSnapshot other)
        {
            if (other == null ||
                nextCustomerOrdinal != other.nextCustomerOrdinal ||
                !secondsUntilNextArrival.Equals(other.secondsUntilNextArrival) ||
                customers == null || other.customers == null ||
                customers.Count != other.customers.Count)
            {
                return false;
            }

            for (int index = 0; index < customers.Count; index++)
            {
                if (!customers[index].Equals(other.customers[index]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StoreCustomerFlowSnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(nextCustomerOrdinal);
            hash.Add(secondsUntilNextArrival);
            if (customers != null)
            {
                foreach (StoreCustomerSnapshot customer in customers)
                {
                    hash.Add(customer);
                }
            }
            return hash.ToHashCode();
        }

        public static StoreCustomerFlowSnapshot Empty()
        {
            return new StoreCustomerFlowSnapshot(1, 0f, null);
        }
    }
}
