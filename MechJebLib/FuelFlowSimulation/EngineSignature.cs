/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;

namespace MechJebLib.FuelFlowSimulation
{
    public readonly struct EngineSignature : IEquatable<EngineSignature>, IComparable<EngineSignature>
    {
        public readonly uint PartPersistentID;
        public readonly int ModuleIndex;

        public EngineSignature(uint partPersistentID, int moduleIndex)
        {
            PartPersistentID = partPersistentID;
            ModuleIndex = moduleIndex;
        }

        public bool Equals(EngineSignature other) => PartPersistentID == other.PartPersistentID && ModuleIndex == other.ModuleIndex;

        public override bool Equals(object? obj) => obj is EngineSignature other && Equals(other);

        public int CompareTo(EngineSignature other)
        {
            int partComparison = PartPersistentID.CompareTo(other.PartPersistentID);
            return partComparison != 0 ? partComparison : ModuleIndex.CompareTo(other.ModuleIndex);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)PartPersistentID * 397) ^ ModuleIndex;
            }
        }

        public static bool operator ==(EngineSignature lhs, EngineSignature rhs) => lhs.Equals(rhs);

        public static bool operator !=(EngineSignature lhs, EngineSignature rhs) => !lhs.Equals(rhs);
    }
}
