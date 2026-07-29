// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;

namespace Margins
{
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int z;

        public GridPosition(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public bool Equals(GridPosition other)
        {
            return x == other.x && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, z);
        }

        public override string ToString()
        {
            return $"({x},{z})";
        }
    }

    [Serializable]
    public struct GridFootprint : IEquatable<GridFootprint>
    {
        public int width;
        public int depth;

        public GridFootprint(int width, int depth)
        {
            this.width = width;
            this.depth = depth;
        }

        public bool IsValid => width > 0 && depth > 0;

        public GridFootprint Rotate(int quarterTurns)
        {
            int normalized = NormalizeQuarterTurns(quarterTurns);
            return normalized % 2 == 0
                ? this
                : new GridFootprint(depth, width);
        }

        public bool Equals(GridFootprint other)
        {
            return width == other.width && depth == other.depth;
        }

        public override bool Equals(object obj)
        {
            return obj is GridFootprint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(width, depth);
        }

        public static int NormalizeQuarterTurns(int quarterTurns)
        {
            return ((quarterTurns % 4) + 4) % 4;
        }
    }

    [Serializable]
    public sealed class FixturePlacementSnapshot : IEquatable<FixturePlacementSnapshot>
    {
        public string fixtureInstanceId;
        public GridPosition gridPosition;
        public GridFootprint unrotatedFootprint;
        public int quarterTurns;

        public FixturePlacementSnapshot(
            string fixtureInstanceId,
            GridPosition gridPosition,
            GridFootprint unrotatedFootprint,
            int quarterTurns)
        {
            this.fixtureInstanceId = fixtureInstanceId;
            this.gridPosition = gridPosition;
            this.unrotatedFootprint = unrotatedFootprint;
            this.quarterTurns = GridFootprint.NormalizeQuarterTurns(quarterTurns);
        }

        public GridFootprint RotatedFootprint => unrotatedFootprint.Rotate(quarterTurns);

        public bool Equals(FixturePlacementSnapshot other)
        {
            return other != null &&
                   string.Equals(fixtureInstanceId, other.fixtureInstanceId, StringComparison.Ordinal) &&
                   gridPosition.Equals(other.gridPosition) &&
                   unrotatedFootprint.Equals(other.unrotatedFootprint) &&
                   quarterTurns == other.quarterTurns;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FixturePlacementSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(fixtureInstanceId, gridPosition, unrotatedFootprint, quarterTurns);
        }
    }

    public enum FixturePlacementFailure
    {
        None,
        InvalidIdentifier,
        DuplicateIdentifier,
        MissingFixture,
        OperatingStateRestricted,
        InvalidFootprint,
        OutOfBounds,
        Occupied
    }

    public sealed class FixturePlacementResult
    {
        public FixturePlacementFailure Failure { get; }
        public string FixtureInstanceId { get; }
        public string ConflictingFixtureInstanceId { get; }
        public GridPosition BlockedCell { get; }
        public bool IsSuccess => Failure == FixturePlacementFailure.None;

        private FixturePlacementResult(
            FixturePlacementFailure failure,
            string fixtureInstanceId,
            string conflictingFixtureInstanceId,
            GridPosition blockedCell)
        {
            Failure = failure;
            FixtureInstanceId = fixtureInstanceId;
            ConflictingFixtureInstanceId = conflictingFixtureInstanceId;
            BlockedCell = blockedCell;
        }

        public static FixturePlacementResult Success(string fixtureInstanceId)
        {
            return new FixturePlacementResult(
                FixturePlacementFailure.None,
                fixtureInstanceId,
                null,
                default);
        }

        public static FixturePlacementResult Reject(
            FixturePlacementFailure failure,
            string fixtureInstanceId,
            GridPosition blockedCell = default,
            string conflictingFixtureInstanceId = null)
        {
            return new FixturePlacementResult(
                failure,
                fixtureInstanceId,
                conflictingFixtureInstanceId,
                blockedCell);
        }
    }

    public sealed class FixtureLayout
    {
        private readonly Dictionary<string, FixturePlacementSnapshot> placements =
            new(StringComparer.Ordinal);
        private readonly Dictionary<GridPosition, string> occupiedCells = new();

        public int Width { get; }
        public int Depth { get; }
        public int Count => placements.Count;

        public FixtureLayout(int width, int depth)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            Width = width;
            Depth = depth;
        }

        public FixturePlacementResult TryPlace(
            string fixtureInstanceId,
            GridPosition gridPosition,
            GridFootprint unrotatedFootprint,
            int quarterTurns)
        {
            FixturePlacementResult validation = PreviewPlace(
                fixtureInstanceId,
                gridPosition,
                unrotatedFootprint,
                quarterTurns);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            FixturePlacementSnapshot placement = new(
                fixtureInstanceId,
                gridPosition,
                unrotatedFootprint,
                quarterTurns);
            placements.Add(fixtureInstanceId, placement);
            Occupy(placement);
            return FixturePlacementResult.Success(fixtureInstanceId);
        }

        public FixturePlacementResult PreviewPlace(
            string fixtureInstanceId,
            GridPosition gridPosition,
            GridFootprint unrotatedFootprint,
            int quarterTurns)
        {
            if (!FirstStoreIdentifier.IsValid(fixtureInstanceId))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.InvalidIdentifier,
                    fixtureInstanceId);
            }

            if (placements.ContainsKey(fixtureInstanceId))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.DuplicateIdentifier,
                    fixtureInstanceId);
            }

            return ValidatePlacement(
                fixtureInstanceId,
                gridPosition,
                unrotatedFootprint,
                quarterTurns,
                null);
        }

        public FixturePlacementResult TryMove(
            string fixtureInstanceId,
            GridPosition gridPosition,
            int quarterTurns)
        {
            FixturePlacementResult validation = PreviewMove(
                fixtureInstanceId,
                gridPosition,
                quarterTurns);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            FixturePlacementSnapshot existing = placements[fixtureInstanceId];

            Release(existing);
            FixturePlacementSnapshot moved = new(
                fixtureInstanceId,
                gridPosition,
                existing.unrotatedFootprint,
                quarterTurns);
            placements[fixtureInstanceId] = moved;
            Occupy(moved);
            return FixturePlacementResult.Success(fixtureInstanceId);
        }

        public FixturePlacementResult PreviewMove(
            string fixtureInstanceId,
            GridPosition gridPosition,
            int quarterTurns)
        {
            if (!FirstStoreIdentifier.IsValid(fixtureInstanceId))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.InvalidIdentifier,
                    fixtureInstanceId);
            }

            if (!placements.TryGetValue(fixtureInstanceId, out FixturePlacementSnapshot existing))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.MissingFixture,
                    fixtureInstanceId);
            }

            return ValidatePlacement(
                fixtureInstanceId,
                gridPosition,
                existing.unrotatedFootprint,
                quarterTurns,
                fixtureInstanceId);
        }

        public FixturePlacementResult TryRemove(string fixtureInstanceId)
        {
            if (!FirstStoreIdentifier.IsValid(fixtureInstanceId))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.InvalidIdentifier,
                    fixtureInstanceId);
            }

            if (!placements.TryGetValue(fixtureInstanceId, out FixturePlacementSnapshot placement))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.MissingFixture,
                    fixtureInstanceId);
            }

            Release(placement);
            placements.Remove(fixtureInstanceId);
            return FixturePlacementResult.Success(fixtureInstanceId);
        }

        public bool TryGetPlacement(
            string fixtureInstanceId,
            out FixturePlacementSnapshot placement)
        {
            return placements.TryGetValue(fixtureInstanceId, out placement);
        }

        public bool TryGetOccupant(GridPosition cell, out string fixtureInstanceId)
        {
            return occupiedCells.TryGetValue(cell, out fixtureInstanceId);
        }

        public List<FixturePlacementSnapshot> CreateSnapshot()
        {
            List<FixturePlacementSnapshot> snapshot = new(placements.Values);
            snapshot.Sort((left, right) =>
                string.CompareOrdinal(left.fixtureInstanceId, right.fixtureInstanceId));
            return snapshot;
        }

        public static bool TryRestore(
            int width,
            int depth,
            IReadOnlyList<FixturePlacementSnapshot> snapshots,
            out FixtureLayout layout,
            out string error)
        {
            layout = null;
            if (width <= 0 || depth <= 0)
            {
                error = "Fixture grid dimensions must be positive.";
                return false;
            }

            if (snapshots == null)
            {
                error = "Fixture placement snapshot list is missing.";
                return false;
            }

            List<FixturePlacementSnapshot> ordered = new(snapshots);
            ordered.Sort((left, right) =>
            {
                if (left == null)
                {
                    return right == null ? 0 : -1;
                }

                if (right == null)
                {
                    return 1;
                }

                return string.CompareOrdinal(left.fixtureInstanceId, right.fixtureInstanceId);
            });

            FixtureLayout candidate = new(width, depth);
            foreach (FixturePlacementSnapshot snapshot in ordered)
            {
                if (snapshot == null)
                {
                    error = "Fixture placement snapshot contains a null record.";
                    return false;
                }

                if (snapshot.quarterTurns < 0 || snapshot.quarterTurns > 3)
                {
                    error =
                        $"Fixture '{snapshot.fixtureInstanceId}' has orientation {snapshot.quarterTurns}; expected 0-3.";
                    return false;
                }

                FixturePlacementResult result = candidate.TryPlace(
                    snapshot.fixtureInstanceId,
                    snapshot.gridPosition,
                    snapshot.unrotatedFootprint,
                    snapshot.quarterTurns);
                if (!result.IsSuccess)
                {
                    error =
                        $"Fixture '{snapshot.fixtureInstanceId}' restore failed ({result.Failure}) at {result.BlockedCell}.";
                    return false;
                }
            }

            layout = candidate;
            error = null;
            return true;
        }

        private FixturePlacementResult ValidatePlacement(
            string fixtureInstanceId,
            GridPosition gridPosition,
            GridFootprint unrotatedFootprint,
            int quarterTurns,
            string ignoredFixtureInstanceId)
        {
            if (!unrotatedFootprint.IsValid)
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.InvalidFootprint,
                    fixtureInstanceId);
            }

            GridFootprint footprint = unrotatedFootprint.Rotate(quarterTurns);
            for (int zOffset = 0; zOffset < footprint.depth; zOffset++)
            {
                for (int xOffset = 0; xOffset < footprint.width; xOffset++)
                {
                    GridPosition cell = new(
                        gridPosition.x + xOffset,
                        gridPosition.z + zOffset);
                    if (cell.x < 0 || cell.z < 0 || cell.x >= Width || cell.z >= Depth)
                    {
                        return FixturePlacementResult.Reject(
                            FixturePlacementFailure.OutOfBounds,
                            fixtureInstanceId,
                            cell);
                    }

                    if (occupiedCells.TryGetValue(cell, out string occupant) &&
                        !string.Equals(
                            occupant,
                            ignoredFixtureInstanceId,
                            StringComparison.Ordinal))
                    {
                        return FixturePlacementResult.Reject(
                            FixturePlacementFailure.Occupied,
                            fixtureInstanceId,
                            cell,
                            occupant);
                    }
                }
            }

            return FixturePlacementResult.Success(fixtureInstanceId);
        }

        private void Occupy(FixturePlacementSnapshot placement)
        {
            GridFootprint footprint = placement.RotatedFootprint;
            for (int zOffset = 0; zOffset < footprint.depth; zOffset++)
            {
                for (int xOffset = 0; xOffset < footprint.width; xOffset++)
                {
                    occupiedCells.Add(
                        new GridPosition(
                            placement.gridPosition.x + xOffset,
                            placement.gridPosition.z + zOffset),
                        placement.fixtureInstanceId);
                }
            }
        }

        private void Release(FixturePlacementSnapshot placement)
        {
            GridFootprint footprint = placement.RotatedFootprint;
            for (int zOffset = 0; zOffset < footprint.depth; zOffset++)
            {
                for (int xOffset = 0; xOffset < footprint.width; xOffset++)
                {
                    occupiedCells.Remove(
                        new GridPosition(
                            placement.gridPosition.x + xOffset,
                            placement.gridPosition.z + zOffset));
                }
            }
        }
    }
}
