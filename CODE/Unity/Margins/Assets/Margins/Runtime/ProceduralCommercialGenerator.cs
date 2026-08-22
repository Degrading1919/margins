using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Margins
{
    public static class ProceduralCommercialGenerator
    {
        private static readonly int[] PreferredBayWidthsFeet = { 24, 20, 30, 36, 40 };
        private const float Tolerance = 0.0001f;

        public static bool TryGenerate(
            ProceduralBuildingRequest request,
            out ProceduralGenerationResult result)
        {
            CommercialBuildingArchetype archetype =
                request?.Archetype ?? CommercialBuildingArchetype.StandaloneSmallCommercial;
            CommercialArchetypeProfile profile = CommercialArchetypeProfile.For(archetype);
            int seed = request?.Seed ?? 0;
            ProceduralDeterministicRandom random = new(seed);
            int storyCount = profile.minimumStories == profile.maximumStories
                ? profile.minimumStories
                : random.NextInt(profile.minimumStories, profile.maximumStories + 1);
            result = new ProceduralGenerationResult(seed, archetype, storyCount, profile);

            string configurationError = null;
            if (request == null || !request.TryValidate(out configurationError))
            {
                result.Reject(
                    "invalid-generation-input",
                    configurationError ?? "Procedural generation input is missing.");
                return false;
            }

            GeneratedFootprint footprint = GenerateFootprint(request, ref random);
            result.SetFootprint(footprint);
            if (!footprint.IsContiguous())
            {
                result.Reject(
                    "disconnected-footprint",
                    "The generated orthogonal footprint is not one contiguous usable area.");
                return false;
            }

            float wallHeight = TotalExteriorHeight(profile, storyCount);
            result.MutableWalls.AddRange(BuildExteriorWalls(
                footprint,
                request.Archetype,
                request.CornerExposure,
                wallHeight));

            BuildBays(request, footprint, result, ref random);
            if (!TrySelectCommercialUnit(request, footprint, result, ref random))
            {
                return false;
            }

            BuildExteriorOpenings(request, result, ref random);
            if (!TryBuildFunctionalLayout(request, result, ref random, out Vector2 entrancePoint))
            {
                return false;
            }

            if (!TryPlaceRecipeAssets(request, result, ref random))
            {
                return false;
            }

            if (!TryValidateGeneratedLayout(request, result, entrancePoint, out string error))
            {
                result.Reject("layout-validation-failed", error);
                return false;
            }

            float score = ScoreFinalLayout(request, result);
            result.MarkSuccess(score);
            result.AddDiagnostic(
                "generation-complete",
                ProceduralDiagnosticSeverity.Information,
                $"Generated {request.Archetype} seed {request.Seed} with " +
                $"{result.Placements.Count} asset placements and score {score:0.0}.");
            return true;
        }

        public static bool TryValidateGeneratedLayout(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result,
            out string error)
        {
            if (result == null || result.Footprint == null)
            {
                error = "Generated layout is missing its footprint.";
                return false;
            }

            GeneratedOpening entrance = result.Openings.FirstOrDefault(item =>
                item.Kind == ProceduralOpeningKind.PrimaryEntrance);
            GeneratedWallSegment wall = entrance == null
                ? null
                : result.Walls.FirstOrDefault(item => item.WallId == entrance.WallId);
            if (wall == null)
            {
                error = "Generated layout is missing a primary entrance.";
                return false;
            }

            Vector2 entrancePoint = wall.InteriorFacePointAt(entrance.OffsetMeters);
            return TryValidateGeneratedLayout(request, result, entrancePoint, out error);
        }

        private static GeneratedFootprint GenerateFootprint(
            ProceduralBuildingRequest request,
            ref ProceduralDeterministicRandom random)
        {
            float width = CommercialGenerationDimensions.Feet(request.WidthFeet);
            float depth = CommercialGenerationDimensions.Feet(request.DepthFeet);
            List<PlanRect> rectangles = new();
            switch (request.FootprintKind)
            {
                case OrthogonalFootprintKind.Rectangle:
                    rectangles.Add(new PlanRect(0f, 0f, width, depth));
                    break;
                case OrthogonalFootprintKind.LShape:
                {
                    int cutWidthFeet = Mathf.Clamp(request.WidthFeet / 3, 6, 16);
                    int cutDepthFeet = Mathf.Clamp(request.DepthFeet / 3, 6, 20);
                    float frontDepth = CommercialGenerationDimensions.Feet(
                        request.DepthFeet - cutDepthFeet);
                    float rearWidth = CommercialGenerationDimensions.Feet(
                        request.WidthFeet - cutWidthFeet);
                    rectangles.Add(new PlanRect(0f, 0f, width, frontDepth));
                    rectangles.Add(random.NextBool()
                        ? new PlanRect(0f, frontDepth, rearWidth,
                            CommercialGenerationDimensions.Feet(cutDepthFeet))
                        : new PlanRect(
                            CommercialGenerationDimensions.Feet(cutWidthFeet),
                            frontDepth,
                            rearWidth,
                            CommercialGenerationDimensions.Feet(cutDepthFeet)));
                    break;
                }
                case OrthogonalFootprintKind.SteppedRectangle:
                {
                    int insetFeet = Mathf.Clamp(request.WidthFeet / 6, 3, 8);
                    int stepDepthFeet = Mathf.Clamp(request.DepthFeet / 3, 6, 18);
                    float frontDepth = CommercialGenerationDimensions.Feet(
                        request.DepthFeet - stepDepthFeet);
                    rectangles.Add(new PlanRect(0f, 0f, width, frontDepth));
                    int leftInsetFeet = random.NextBool() ? insetFeet : insetFeet * 2;
                    int rightInsetFeet = insetFeet;
                    int rearWidthFeet = Mathf.Max(
                        6,
                        request.WidthFeet - leftInsetFeet - rightInsetFeet);
                    rectangles.Add(new PlanRect(
                        CommercialGenerationDimensions.Feet(leftInsetFeet),
                        frontDepth,
                        CommercialGenerationDimensions.Feet(rearWidthFeet),
                        CommercialGenerationDimensions.Feet(stepDepthFeet)));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new GeneratedFootprint(
                request.FootprintKind,
                request.WidthFeet,
                request.DepthFeet,
                rectangles);
        }

        private static float TotalExteriorHeight(
            CommercialArchetypeProfile profile,
            int storyCount)
        {
            if (storyCount <= 1)
            {
                return profile.parapetHeightMeters;
            }

            return profile.structuralHeightMeters +
                   CommercialGenerationDimensions.Feet(10f * (storyCount - 1) + 2f);
        }

        private static IEnumerable<GeneratedWallSegment> BuildExteriorWalls(
            GeneratedFootprint footprint,
            CommercialBuildingArchetype archetype,
            bool cornerExposure,
            float wallHeightMeters)
        {
            bool[,] occupied = new bool[footprint.WidthFeet, footprint.DepthFeet];
            for (int z = 0; z < footprint.DepthFeet; z++)
            {
                for (int x = 0; x < footprint.WidthFeet; x++)
                {
                    occupied[x, z] = footprint.Contains(new Vector2(
                        CommercialGenerationDimensions.Feet(x + 0.5f),
                        CommercialGenerationDimensions.Feet(z + 0.5f)));
                }
            }

            Dictionary<string, EdgeRun> runs = new(StringComparer.Ordinal);
            for (int z = 0; z < footprint.DepthFeet; z++)
            {
                for (int x = 0; x < footprint.WidthFeet; x++)
                {
                    if (!occupied[x, z])
                    {
                        continue;
                    }

                    if (z == 0 || !occupied[x, z - 1])
                    {
                        AddEdge(runs, true, z, x, Vector2.down);
                    }
                    if (z == footprint.DepthFeet - 1 || !occupied[x, z + 1])
                    {
                        AddEdge(runs, true, z + 1, x, Vector2.up);
                    }
                    if (x == 0 || !occupied[x - 1, z])
                    {
                        AddEdge(runs, false, x, z, Vector2.left);
                    }
                    if (x == footprint.WidthFeet - 1 || !occupied[x + 1, z])
                    {
                        AddEdge(runs, false, x + 1, z, Vector2.right);
                    }
                }
            }

            List<EdgeSegment> segments = new();
            foreach (EdgeRun run in runs.Values)
            {
                run.UnitStarts.Sort();
                int start = run.UnitStarts[0];
                int previous = start;
                for (int index = 1; index <= run.UnitStarts.Count; index++)
                {
                    bool continues = index < run.UnitStarts.Count &&
                                     run.UnitStarts[index] == previous + 1;
                    if (continues)
                    {
                        previous = run.UnitStarts[index];
                        continue;
                    }

                    segments.Add(new EdgeSegment(
                        run.Horizontal,
                        run.Constant,
                        start,
                        previous + 1,
                        run.Normal));
                    if (index < run.UnitStarts.Count)
                    {
                        start = run.UnitStarts[index];
                        previous = start;
                    }
                }
            }

            segments.Sort(EdgeSegment.Compare);
            List<GeneratedWallSegment> walls = new();
            for (int index = 0; index < segments.Count; index++)
            {
                EdgeSegment segment = segments[index];
                Vector2 startFeet = segment.Horizontal
                    ? new Vector2(segment.Start, segment.Constant)
                    : new Vector2(segment.Constant, segment.Start);
                Vector2 endFeet = segment.Horizontal
                    ? new Vector2(segment.End, segment.Constant)
                    : new Vector2(segment.Constant, segment.End);
                Vector2 startMeters = startFeet *
                                      CommercialGenerationDimensions.StructuralIncrementMeters;
                Vector2 endMeters = endFeet *
                                    CommercialGenerationDimensions.StructuralIncrementMeters;
                FrontageRole role = ClassifyFrontage(
                    segment.Normal,
                    startMeters,
                    endMeters,
                    archetype,
                    cornerExposure);
                walls.Add(new GeneratedWallSegment(
                    $"wall-{index + 1:00}",
                    startMeters,
                    endMeters,
                    segment.Normal,
                    role,
                    CommercialGenerationDimensions.ExteriorWallThicknessMeters,
                    wallHeightMeters));
            }

            return walls;
        }

        private static void AddEdge(
            IDictionary<string, EdgeRun> runs,
            bool horizontal,
            int constant,
            int unitStart,
            Vector2 normal)
        {
            string key = $"{(horizontal ? 'h' : 'v')}:{constant}:{normal.x}:{normal.y}";
            if (!runs.TryGetValue(key, out EdgeRun run))
            {
                run = new EdgeRun(horizontal, constant, normal);
                runs.Add(key, run);
            }

            run.UnitStarts.Add(unitStart);
        }

        private static FrontageRole ClassifyFrontage(
            Vector2 normal,
            Vector2 start,
            Vector2 end,
            CommercialBuildingArchetype archetype,
            bool cornerExposure)
        {
            if (normal == Vector2.down &&
                Mathf.Abs(start.y) <= Tolerance && Mathf.Abs(end.y) <= Tolerance)
            {
                return FrontageRole.PrimaryPublic;
            }

            if (normal == Vector2.up)
            {
                return FrontageRole.RearService;
            }

            if (normal == Vector2.left || normal == Vector2.right)
            {
                if (cornerExposure ||
                    archetype == CommercialBuildingArchetype.StandaloneSmallCommercial)
                {
                    return FrontageRole.SecondaryPublic;
                }

                return FrontageRole.SharedInternal;
            }

            return FrontageRole.SharedInternal;
        }

        private static void BuildBays(
            ProceduralBuildingRequest request,
            GeneratedFootprint footprint,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random)
        {
            if (request.Archetype != CommercialBuildingArchetype.StripCenterInlineRetail)
            {
                result.MutableBays.Add(new GeneratedCommercialBay(
                    "bay-01",
                    footprint.Bounds));
                return;
            }

            List<int> widthsFeet = SubdivideWidth(request.WidthFeet, ref random);
            int offsetFeet = 0;
            for (int index = 0; index < widthsFeet.Count; index++)
            {
                int widthFeet = widthsFeet[index];
                result.MutableBays.Add(new GeneratedCommercialBay(
                    $"bay-{index + 1:00}",
                    new PlanRect(
                        CommercialGenerationDimensions.Feet(offsetFeet),
                        0f,
                        CommercialGenerationDimensions.Feet(widthFeet),
                        CommercialGenerationDimensions.Feet(request.DepthFeet))));
                offsetFeet += widthFeet;
            }
        }

        private static List<int> SubdivideWidth(
            int totalWidthFeet,
            ref ProceduralDeterministicRandom random)
        {
            if (totalWidthFeet % 24 == 0 && totalWidthFeet / 24 >= 2)
            {
                return Enumerable.Repeat(24, totalWidthFeet / 24).ToList();
            }

            List<int> ordered = new(PreferredBayWidthsFeet);
            int rotation = random.NextInt(0, ordered.Count);
            ordered = ordered.Skip(rotation).Concat(ordered.Take(rotation)).ToList();
            List<int> result = new();
            int remaining = totalWidthFeet;
            while (remaining > 0)
            {
                int selected = ordered.FirstOrDefault(width =>
                    width <= remaining && (remaining - width == 0 || remaining - width >= 18));
                if (selected == 0)
                {
                    if (remaining >= 18)
                    {
                        result.Add(remaining);
                    }
                    else if (result.Count > 0)
                    {
                        result[result.Count - 1] += remaining;
                    }
                    break;
                }

                result.Add(selected);
                remaining -= selected;
            }

            if (result.Count == 0)
            {
                result.Add(totalWidthFeet);
            }

            return result;
        }

        private static bool TrySelectCommercialUnit(
            ProceduralBuildingRequest request,
            GeneratedFootprint footprint,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random)
        {
            ProceduralBusinessRecipe recipe = request.BusinessRecipe;
            if (request.Archetype != CommercialBuildingArchetype.StripCenterInlineRetail)
            {
                float frontageFeet = request.WidthFeet;
                float areaSquareFeet = footprint.AreaSquareMeters /
                                       (CommercialGenerationDimensions.StructuralIncrementMeters *
                                        CommercialGenerationDimensions.StructuralIncrementMeters);
                if (frontageFeet + Tolerance < recipe.MinimumFrontageFeet ||
                    request.DepthFeet + Tolerance < recipe.MinimumDepthFeet ||
                    areaSquareFeet + Tolerance < recipe.MinimumUsableAreaSquareFeet)
                {
                    result.Reject(
                        "business-unit-incompatible",
                        $"Recipe '{recipe.StableRecipeId}' requires at least " +
                        $"{recipe.MinimumFrontageFeet:0.#} ft frontage, " +
                        $"{recipe.MinimumDepthFeet:0.#} ft depth, and " +
                        $"{recipe.MinimumUsableAreaSquareFeet:0.#} sq ft; the generated unit cannot satisfy it.");
                    return false;
                }

                result.MutableUnits.Add(new GeneratedCommercialUnit(
                    "unit-01",
                    footprint.Bounds,
                    result.Bays.Select(item => item.BayId),
                    recipe.StableRecipeId));
                return true;
            }

            List<UnitCandidate> candidates = new();
            for (int start = 0; start < result.Bays.Count; start++)
            {
                float width = 0f;
                List<string> bayIds = new();
                for (int end = start; end < result.Bays.Count; end++)
                {
                    GeneratedCommercialBay bay = result.Bays[end];
                    width += bay.BoundsMeters.Width;
                    bayIds.Add(bay.BayId);
                    float frontageFeet = width /
                                          CommercialGenerationDimensions.StructuralIncrementMeters;
                    float areaSquareFeet = frontageFeet * request.DepthFeet;
                    if (frontageFeet + Tolerance >= recipe.MinimumFrontageFeet &&
                        request.DepthFeet + Tolerance >= recipe.MinimumDepthFeet &&
                        areaSquareFeet + Tolerance >= recipe.MinimumUsableAreaSquareFeet)
                    {
                        candidates.Add(new UnitCandidate(
                            start,
                            end,
                            width,
                            areaSquareFeet,
                            new List<string>(bayIds)));
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                result.Reject(
                    "business-unit-incompatible",
                    $"No contiguous strip-center bay merge can satisfy recipe '{recipe.StableRecipeId}'.");
                return false;
            }

            int minimumBayCount = candidates.Min(item => item.BayIds.Count);
            List<UnitCandidate> best = candidates
                .Where(item => item.BayIds.Count == minimumBayCount)
                .OrderBy(item => item.StartIndex)
                .ToList();
            UnitCandidate selected = best[random.NextInt(0, best.Count)];
            PlanRect first = result.Bays[selected.StartIndex].BoundsMeters;
            result.MutableUnits.Add(new GeneratedCommercialUnit(
                "unit-01",
                new PlanRect(
                    first.MinX,
                    first.MinZ,
                    selected.WidthMeters,
                    first.Depth),
                selected.BayIds,
                recipe.StableRecipeId));
            return true;
        }

        private static void BuildExteriorOpenings(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random)
        {
            GeneratedCommercialUnit unit = result.Units[0];
            GeneratedWallSegment primary = FindWallForBounds(
                result.Walls,
                FrontageRole.PrimaryPublic,
                unit.BoundsMeters.MinX,
                unit.BoundsMeters.MaxX);
            if (primary == null)
            {
                return;
            }

            float unitCenter = unit.BoundsMeters.Center.x;
            float lateralOffset = Mathf.Min(
                CommercialGenerationDimensions.Feet(5f),
                unit.BoundsMeters.Width * 0.2f);
            float desiredX = unitCenter + (random.NextBool() ? lateralOffset : -lateralOffset);
            float doorCenterOffset = OffsetAlongWall(primary, new Vector2(desiredX, 0f));
            float halfDoor = CommercialGenerationDimensions.StandardDoorWidthMeters * 0.5f;
            doorCenterOffset = Mathf.Clamp(
                CommercialGenerationDimensions.SnapOpening(doorCenterOffset),
                CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters + halfDoor,
                primary.LengthMeters -
                CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters - halfDoor);
            result.MutableOpenings.Add(new GeneratedOpening(
                "opening-primary-entrance",
                primary.WallId,
                unit.UnitId,
                ProceduralOpeningKind.PrimaryEntrance,
                doorCenterOffset,
                CommercialGenerationDimensions.StandardDoorWidthMeters,
                CommercialGenerationDimensions.StandardDoorHeightMeters,
                0f));

            AddStorefrontWindows(result, primary, unit, doorCenterOffset);

            if (request.BusinessRecipe.RequiresServiceEntrance)
            {
                GeneratedWallSegment rear = FindWallForBounds(
                    result.Walls,
                    FrontageRole.RearService,
                    unit.BoundsMeters.MinX,
                    unit.BoundsMeters.MaxX);
                if (rear != null && rear.LengthMeters >=
                    CommercialGenerationDimensions.Feet(7f))
                {
                    float offset = Mathf.Clamp(
                        CommercialGenerationDimensions.SnapOpening(
                            OffsetAlongWall(
                                rear,
                                new Vector2(unit.BoundsMeters.Center.x,
                                    unit.BoundsMeters.MaxZ))),
                        CommercialGenerationDimensions.Feet(3.5f),
                        rear.LengthMeters - CommercialGenerationDimensions.Feet(3.5f));
                    result.MutableOpenings.Add(new GeneratedOpening(
                        "opening-service-door",
                        rear.WallId,
                        unit.UnitId,
                        ProceduralOpeningKind.ServiceDoor,
                        offset,
                        CommercialGenerationDimensions.StandardDoorWidthMeters,
                        CommercialGenerationDimensions.StandardDoorHeightMeters,
                        0f));
                }
            }

            if (result.StoryCount > 1)
            {
                AddUpperFloorWindows(result);
            }
        }

        private static void AddStorefrontWindows(
            ProceduralGenerationResult result,
            GeneratedWallSegment primary,
            GeneratedCommercialUnit unit,
            float doorCenterOffset)
        {
            float doorHalf = CommercialGenerationDimensions.StandardDoorWidthMeters * 0.5f;
            float[] widths =
            {
                CommercialGenerationDimensions.Feet(8f),
                CommercialGenerationDimensions.Feet(6f),
                CommercialGenerationDimensions.Feet(4f)
            };
            int index = 0;
            foreach (bool beforeDoor in new[] { true, false })
            {
                float availableStart = Mathf.Max(
                    CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters,
                    OffsetAlongWall(primary, new Vector2(unit.BoundsMeters.MinX, 0f)) +
                    CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters);
                float availableEnd = Mathf.Min(
                    primary.LengthMeters -
                    CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters,
                    OffsetAlongWall(primary, new Vector2(unit.BoundsMeters.MaxX, 0f)) -
                    CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters);
                if (beforeDoor)
                {
                    availableEnd = doorCenterOffset - doorHalf -
                                   CommercialGenerationDimensions.MinimumOpeningSeparationMeters;
                }
                else
                {
                    availableStart = doorCenterOffset + doorHalf +
                                     CommercialGenerationDimensions.MinimumOpeningSeparationMeters;
                }

                float available = availableEnd - availableStart;
                float width = widths.FirstOrDefault(candidate => candidate <= available + Tolerance);
                if (width <= 0f)
                {
                    continue;
                }

                float center = CommercialGenerationDimensions.SnapOpening(
                    (availableStart + availableEnd) * 0.5f);
                result.MutableOpenings.Add(new GeneratedOpening(
                    $"opening-storefront-{++index:00}",
                    primary.WallId,
                    unit.UnitId,
                    ProceduralOpeningKind.StorefrontWindow,
                    center,
                    width,
                    result.Profile.storefrontTopMeters -
                    result.Profile.storefrontSillMeters,
                    result.Profile.storefrontSillMeters));
            }
        }

        private static void AddUpperFloorWindows(ProceduralGenerationResult result)
        {
            int openingIndex = 0;
            foreach (GeneratedWallSegment wall in result.Walls.Where(item =>
                         item.FrontageRole == FrontageRole.PrimaryPublic ||
                         item.FrontageRole == FrontageRole.SecondaryPublic))
            {
                float spacing = CommercialGenerationDimensions.Feet(6f);
                float width = CommercialGenerationDimensions.Feet(4f);
                for (int story = 1; story < result.StoryCount; story++)
                {
                    float sill = result.Profile.structuralHeightMeters +
                                 CommercialGenerationDimensions.Feet(
                                     (story - 1) * 10f + 3f);
                    for (float offset = CommercialGenerationDimensions.Feet(4f);
                         offset <= wall.LengthMeters - CommercialGenerationDimensions.Feet(4f);
                         offset += spacing)
                    {
                        result.MutableOpenings.Add(new GeneratedOpening(
                            $"opening-upper-{++openingIndex:000}",
                            wall.WallId,
                            null,
                            ProceduralOpeningKind.UpperFloorWindow,
                            CommercialGenerationDimensions.SnapOpening(offset),
                            width,
                            CommercialGenerationDimensions.Feet(5f),
                            sill));
                    }
                }
            }
        }

        private static GeneratedWallSegment FindWallForBounds(
            IReadOnlyList<GeneratedWallSegment> walls,
            FrontageRole role,
            float minimumX,
            float maximumX)
        {
            return walls
                .Where(item => item.FrontageRole == role &&
                               Mathf.Abs(item.StartMeters.y - item.EndMeters.y) <= Tolerance &&
                               Mathf.Max(item.StartMeters.x, item.EndMeters.x) >= minimumX - Tolerance &&
                               Mathf.Min(item.StartMeters.x, item.EndMeters.x) <= maximumX + Tolerance)
                .OrderByDescending(item => item.LengthMeters)
                .ThenBy(item => item.WallId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static float OffsetAlongWall(GeneratedWallSegment wall, Vector2 point)
        {
            return Vector2.Dot(point - wall.StartMeters, wall.Direction);
        }

        private static bool TryBuildFunctionalLayout(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random,
            out Vector2 entrancePoint)
        {
            entrancePoint = default;
            GeneratedCommercialUnit unit = result.Units[0];
            PlanRect usable = SelectUsableInteriorRect(result.Footprint, unit.BoundsMeters);
            float wallInset = CommercialGenerationDimensions.WallFaceOffset(
                CommercialGenerationDimensions.ExteriorWallThicknessMeters);
            usable = Inset(usable, wallInset);
            if (!usable.IsValid ||
                usable.Width < CommercialGenerationDimensions.Feet(12f) ||
                usable.Depth < CommercialGenerationDimensions.Feet(14f))
            {
                result.Reject(
                    "insufficient-usable-interior",
                    "The commercial unit has no sufficiently large contiguous interior rectangle for functional zoning.");
                return false;
            }

            GeneratedOpening entrance = result.Openings.FirstOrDefault(item =>
                item.Kind == ProceduralOpeningKind.PrimaryEntrance);
            GeneratedWallSegment entranceWall = entrance == null
                ? null
                : result.Walls.FirstOrDefault(item => item.WallId == entrance.WallId);
            if (entrance == null || entranceWall == null)
            {
                result.Reject(
                    "missing-primary-entrance",
                    "A commercial unit requires a valid primary public entrance before interior layout.");
                return false;
            }

            entrancePoint = entranceWall.InteriorFacePointAt(entrance.OffsetMeters);
            float routeWidth = CommercialGenerationDimensions.PrimaryRouteWidthMeters;
            float routeCenterX = Mathf.Clamp(
                entrancePoint.x,
                usable.MinX + routeWidth * 0.5f,
                usable.MaxX - routeWidth * 0.5f);
            PlanRect arrival = new(
                routeCenterX - CommercialGenerationDimensions.EntranceLandingSizeMeters * 0.5f,
                usable.MinZ,
                CommercialGenerationDimensions.EntranceLandingSizeMeters,
                Mathf.Min(
                    CommercialGenerationDimensions.EntranceArrivalDepthMeters,
                    usable.Depth));
            PlanRect primaryRoute = new(
                routeCenterX - routeWidth * 0.5f,
                usable.MinZ,
                routeWidth,
                usable.Depth);
            result.MutableCirculation.Add(new GeneratedCirculationPath(
                "circulation-arrival",
                arrival,
                CommercialGenerationDimensions.EntranceLandingSizeMeters,
                true));
            result.MutableCirculation.Add(new GeneratedCirculationPath(
                "circulation-primary",
                primaryRoute,
                routeWidth,
                true));

            List<ProceduralZoneRequest> rearRequests = request.BusinessRecipe.ZoneRequests
                .Where(item => item != null &&
                               item.Required &&
                               item.ZoneType != FunctionalZoneType.Public &&
                               item.ZoneType != FunctionalZoneType.Transaction &&
                               item.ZoneType != FunctionalZoneType.Circulation)
                .OrderBy(item => item.ZoneType)
                .ToList();
            float usableWidthFeet = usable.Width /
                                    CommercialGenerationDimensions.StructuralIncrementMeters;
            float totalMinimumArea = rearRequests.Sum(item => item.MinimumAreaSquareFeet);
            int rearDepthFeet = rearRequests.Count == 0
                ? 0
                : Mathf.Max(8, Mathf.CeilToInt(totalMinimumArea / usableWidthFeet));
            rearDepthFeet = Mathf.Min(
                rearDepthFeet,
                Mathf.FloorToInt(
                    usable.Depth /
                    CommercialGenerationDimensions.StructuralIncrementMeters - 12f));
            if (rearRequests.Count > 0 && rearDepthFeet < 6)
            {
                result.Reject(
                    "required-zones-do-not-fit",
                    "Required rear Work/Storage/Service/Staff zones leave no valid public floor and entrance arrival area.");
                return false;
            }

            float rearDepth = CommercialGenerationDimensions.Feet(rearDepthFeet);
            float publicDepth = usable.Depth - rearDepth;
            bool transactionRequired = request.BusinessRecipe.RequiresZone(
                FunctionalZoneType.Transaction);
            float transactionWidth = transactionRequired
                ? Mathf.Min(
                    CommercialGenerationDimensions.Feet(10f),
                    usable.Width * 0.25f)
                : 0f;
            bool transactionOnLeft = random.NextBool();
            PlanRect publicBounds = new(
                transactionRequired && transactionOnLeft
                    ? usable.MinX + transactionWidth
                    : usable.MinX,
                usable.MinZ,
                usable.Width - transactionWidth,
                publicDepth);
            if (publicBounds.Width < CommercialGenerationDimensions.Feet(8f))
            {
                result.Reject(
                    "required-zones-do-not-fit",
                    "The Transaction zone leaves less than 8 ft of usable Public zone width.");
                return false;
            }

            result.MutableZones.Add(new GeneratedFunctionalZone(
                "zone-public",
                FunctionalZoneType.Public,
                publicBounds,
                false));
            result.MutableZones.Add(new GeneratedFunctionalZone(
                "zone-circulation",
                FunctionalZoneType.Circulation,
                primaryRoute,
                false));

            if (transactionRequired)
            {
                PlanRect transaction = new(
                    transactionOnLeft ? usable.MinX : usable.MaxX - transactionWidth,
                    usable.MinZ,
                    transactionWidth,
                    Mathf.Min(publicDepth, CommercialGenerationDimensions.Feet(12f)));
                result.MutableZones.Add(new GeneratedFunctionalZone(
                    "zone-transaction",
                    FunctionalZoneType.Transaction,
                    transaction,
                    false));
            }

            if (rearRequests.Count == 0)
            {
                return ValidateRequiredZones(request.BusinessRecipe, result);
            }

            List<ProceduralZoneRequest> orderedRear = random.NextBool()
                ? rearRequests
                : rearRequests.AsEnumerable().Reverse().ToList();
            int remainingWidthFeet = Mathf.FloorToInt(usableWidthFeet);
            int cursorFeet = 0;
            List<GeneratedFunctionalZone> rearZones = new();
            for (int index = 0; index < orderedRear.Count; index++)
            {
                ProceduralZoneRequest zoneRequest = orderedRear[index];
                int zonesRemaining = orderedRear.Count - index;
                int minimumWidthFeet = Mathf.Max(
                    6,
                    Mathf.CeilToInt(zoneRequest.MinimumAreaSquareFeet / rearDepthFeet));
                int widthFeet = index == orderedRear.Count - 1
                    ? remainingWidthFeet
                    : Mathf.Min(
                        Mathf.Max(minimumWidthFeet, remainingWidthFeet / zonesRemaining),
                        remainingWidthFeet - 6 * (zonesRemaining - 1));
                if (widthFeet < minimumWidthFeet)
                {
                    result.Reject(
                        "required-zones-do-not-fit",
                        $"Required zone '{zoneRequest.ZoneType}' cannot meet its minimum area.");
                    return false;
                }

                PlanRect bounds = new(
                    usable.MinX + CommercialGenerationDimensions.Feet(cursorFeet),
                    usable.MaxZ - rearDepth,
                    CommercialGenerationDimensions.Feet(widthFeet),
                    rearDepth);
                GeneratedFunctionalZone zone = new(
                    $"zone-{zoneRequest.ZoneType.ToString().ToLowerInvariant()}",
                    zoneRequest.ZoneType,
                    bounds,
                    zoneRequest.Enclosed);
                rearZones.Add(zone);
                result.MutableZones.Add(zone);
                cursorFeet += widthFeet;
                remainingWidthFeet -= widthFeet;
            }

            BuildRearPartitions(result, usable, rearZones);
            if (request.BusinessRecipe.RequiresServiceEntrance)
            {
                GeneratedFunctionalZone serviceZone = rearZones.FirstOrDefault(item =>
                    item.ZoneType == FunctionalZoneType.Service ||
                    item.ZoneType == FunctionalZoneType.Storage);
                if (serviceZone != null)
                {
                    result.MutableCirculation.Add(new GeneratedCirculationPath(
                        "circulation-service",
                        new PlanRect(
                            serviceZone.BoundsMeters.Center.x -
                            CommercialGenerationDimensions.MinimumAisleWidthMeters * 0.5f,
                            serviceZone.BoundsMeters.MinZ,
                            CommercialGenerationDimensions.MinimumAisleWidthMeters,
                            serviceZone.BoundsMeters.Depth),
                        CommercialGenerationDimensions.MinimumAisleWidthMeters,
                        false));
                }
            }

            return ValidateRequiredZones(request.BusinessRecipe, result);
        }

        private static PlanRect SelectUsableInteriorRect(
            GeneratedFootprint footprint,
            PlanRect unitBounds)
        {
            PlanRect selected = default;
            float bestArea = -1f;
            foreach (PlanRect rectangle in footprint.Rectangles)
            {
                float minX = Mathf.Max(rectangle.MinX, unitBounds.MinX);
                float minZ = Mathf.Max(rectangle.MinZ, unitBounds.MinZ);
                float maxX = Mathf.Min(rectangle.MaxX, unitBounds.MaxX);
                float maxZ = Mathf.Min(rectangle.MaxZ, unitBounds.MaxZ);
                PlanRect intersection = new(
                    minX,
                    minZ,
                    maxX - minX,
                    maxZ - minZ);
                if (intersection.IsValid && intersection.Area > bestArea)
                {
                    selected = intersection;
                    bestArea = intersection.Area;
                }
            }

            return selected;
        }

        private static PlanRect Inset(PlanRect rectangle, float amount)
        {
            return new PlanRect(
                rectangle.MinX + amount,
                rectangle.MinZ + amount,
                rectangle.Width - amount * 2f,
                rectangle.Depth - amount * 2f);
        }

        private static void BuildRearPartitions(
            ProceduralGenerationResult result,
            PlanRect usable,
            IReadOnlyList<GeneratedFunctionalZone> rearZones)
        {
            if (rearZones.Count == 0 || !rearZones.Any(item => item.Enclosed))
            {
                return;
            }

            float boundaryZ = rearZones[0].BoundsMeters.MinZ;
            List<GeneratedOpening> frontDoors = new();
            int doorIndex = 0;
            foreach (GeneratedFunctionalZone zone in rearZones.Where(item => item.Enclosed))
            {
                float offset = CommercialGenerationDimensions.SnapOpening(
                    zone.BoundsMeters.Center.x - usable.MinX);
                frontDoors.Add(new GeneratedOpening(
                    $"opening-interior-{++doorIndex:00}",
                    "partition-rear-front",
                    result.Units[0].UnitId,
                    ProceduralOpeningKind.InteriorDoor,
                    offset,
                    CommercialGenerationDimensions.StandardDoorWidthMeters,
                    CommercialGenerationDimensions.StandardDoorHeightMeters,
                    0f));
            }
            result.MutablePartitions.Add(new GeneratedPartition(
                "partition-rear-front",
                new Vector2(usable.MinX, boundaryZ),
                new Vector2(usable.MaxX, boundaryZ),
                CommercialGenerationDimensions.InteriorPartitionThicknessMeters,
                result.Profile.finishedCeilingHeightMeters,
                frontDoors));

            for (int index = 1; index < rearZones.Count; index++)
            {
                if (!rearZones[index - 1].Enclosed && !rearZones[index].Enclosed)
                {
                    continue;
                }

                float x = rearZones[index].BoundsMeters.MinX;
                result.MutablePartitions.Add(new GeneratedPartition(
                    $"partition-rear-{index:00}",
                    new Vector2(x, boundaryZ),
                    new Vector2(x, usable.MaxZ),
                    CommercialGenerationDimensions.InteriorPartitionThicknessMeters,
                    result.Profile.finishedCeilingHeightMeters,
                    Array.Empty<GeneratedOpening>()));
            }
        }

        private static bool ValidateRequiredZones(
            ProceduralBusinessRecipe recipe,
            ProceduralGenerationResult result)
        {
            foreach (ProceduralZoneRequest required in recipe.ZoneRequests.Where(item =>
                         item != null && item.Required))
            {
                GeneratedFunctionalZone generated = result.Zones.FirstOrDefault(item =>
                    item.ZoneType == required.ZoneType);
                if (generated == null)
                {
                    result.Reject(
                        "required-zone-missing",
                        $"Required zone '{required.ZoneType}' was not generated.");
                    return false;
                }

                float squareFeet = generated.BoundsMeters.Area /
                                   (CommercialGenerationDimensions.StructuralIncrementMeters *
                                    CommercialGenerationDimensions.StructuralIncrementMeters);
                if (squareFeet + Tolerance < required.MinimumAreaSquareFeet)
                {
                    result.Reject(
                        "required-zone-too-small",
                        $"Required zone '{required.ZoneType}' has {squareFeet:0.#} sq ft but needs {required.MinimumAreaSquareFeet:0.#} sq ft.");
                    return false;
                }
            }

            return true;
        }

        private static bool TryPlaceRecipeAssets(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random)
        {
            List<ProceduralAssetRequest> requests = request.BusinessRecipe.AssetRequests
                .Where(item => item != null)
                .OrderBy(item => PlacementPhase(item.PrimaryCategory))
                .ThenByDescending(item => item.Priority)
                .ThenBy(item => item.RequestId, StringComparer.Ordinal)
                .ToList();
            Dictionary<string, int> counts = requests.ToDictionary(
                item => item.RequestId,
                _ => 0,
                StringComparer.Ordinal);

            List<ProceduralAssetRequest> unmet = requests
                .Where(item => item.Required && item.Minimum > 0)
                .ToList();
            while (unmet.Count > 0)
            {
                bool progress = false;
                foreach (ProceduralAssetRequest assetRequest in unmet.ToArray())
                {
                    if (counts[assetRequest.RequestId] >= assetRequest.Minimum)
                    {
                        unmet.Remove(assetRequest);
                        continue;
                    }

                    if (TryPlaceOne(
                            request,
                            assetRequest,
                            counts[assetRequest.RequestId],
                            result,
                            ref random,
                            out _))
                    {
                        counts[assetRequest.RequestId]++;
                        progress = true;
                        if (counts[assetRequest.RequestId] >= assetRequest.Minimum)
                        {
                            unmet.Remove(assetRequest);
                        }
                    }
                }

                if (progress)
                {
                    continue;
                }

                ProceduralAssetRequest failed = unmet[0];
                List<ProceduralAssetComponent> candidates = request.AssetRegistry.FindCandidates(
                    failed.PrimaryCategory,
                    failed.RequiredCapabilities,
                    failed.Environment);
                string reason = candidates.Count == 0
                    ? "no registry asset satisfies its category, capability, and environment requirements"
                    : "no candidate placement satisfies architecture, circulation, interaction, and clearance constraints";
                result.Reject(
                    "required-asset-unplaceable",
                    $"Required request '{failed.RequestId}' reached {counts[failed.RequestId]}/{failed.Minimum}: {reason}.");
                return false;
            }

            foreach (ProceduralAssetRequest assetRequest in requests)
            {
                int extraRange = assetRequest.Maximum - assetRequest.Preferred;
                int target = assetRequest.Preferred +
                             (extraRange > 0 ? random.NextInt(0, extraRange + 1) : 0);
                target = Mathf.Min(target, assetRequest.Maximum);
                while (counts[assetRequest.RequestId] < target)
                {
                    if (!TryPlaceOne(
                            request,
                            assetRequest,
                            counts[assetRequest.RequestId],
                            result,
                            ref random,
                            out _))
                    {
                        break;
                    }

                    counts[assetRequest.RequestId]++;
                }
            }

            return true;
        }

        private static int PlacementPhase(ProceduralAssetCategory category)
        {
            switch (category)
            {
                case ProceduralAssetCategory.Amenity:
                    return 2;
                case ProceduralAssetCategory.Decor:
                    return 3;
                default:
                    return 1;
            }
        }

        private static bool TryPlaceOne(
            ProceduralBuildingRequest buildingRequest,
            ProceduralAssetRequest assetRequest,
            int instanceIndex,
            ProceduralGenerationResult result,
            ref ProceduralDeterministicRandom random,
            out float acceptedScore)
        {
            acceptedScore = 0f;
            List<ProceduralAssetComponent> assets = buildingRequest.AssetRegistry.FindCandidates(
                assetRequest.PrimaryCategory,
                assetRequest.RequiredCapabilities,
                assetRequest.Environment);
            if (assets.Count == 0)
            {
                return false;
            }

            List<PlacementCandidate> candidates = new();
            foreach (ProceduralAssetComponent asset in assets)
            {
                AddCandidatesForAsset(
                    buildingRequest,
                    assetRequest,
                    asset,
                    instanceIndex,
                    result,
                    candidates);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort(PlacementCandidate.Compare);
            float bestScore = candidates[0].Score;
            List<PlacementCandidate> nearBest = candidates
                .TakeWhile(item => item.Score >= bestScore - 4f)
                .Take(24)
                .ToList();
            PlacementCandidate selected = nearBest[random.NextInt(0, nearBest.Count)];
            string placementId = $"{assetRequest.RequestId}-{instanceIndex + 1:00}";
            GeneratedAssetPlacement placement = new(
                placementId,
                assetRequest.RequestId,
                selected.Asset,
                assetRequest.PreferredHostZone,
                selected.MountingMode,
                selected.PositionMeters,
                selected.YawDegrees,
                selected.PhysicalBounds,
                BuildClearances(selected.Asset, selected.PositionMeters, selected.YawDegrees),
                selected.HostWallId,
                selected.ParentPlacementId,
                selected.HostSocketId);
            result.MutablePlacements.Add(placement);
            acceptedScore = selected.Score;
            return true;
        }

        private static void AddCandidatesForAsset(
            ProceduralBuildingRequest buildingRequest,
            ProceduralAssetRequest assetRequest,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            if ((asset.MountingModes & ProceduralMountingMode.Socket) != 0)
            {
                AddSocketCandidates(assetRequest, asset, instanceIndex, result, candidates);
            }
            if ((asset.MountingModes & ProceduralMountingMode.ExteriorPad) != 0)
            {
                AddExteriorPadCandidates(assetRequest, asset, instanceIndex, result, candidates);
            }
            if ((asset.MountingModes & ProceduralMountingMode.Wall) != 0)
            {
                AddWallCandidates(assetRequest, asset, instanceIndex, result, candidates);
            }
            if ((asset.MountingModes & ProceduralMountingMode.Ceiling) != 0)
            {
                AddCeilingCandidates(assetRequest, asset, instanceIndex, result, candidates);
            }
            if ((asset.MountingModes & ProceduralMountingMode.Floor) != 0)
            {
                AddFloorCandidates(
                    buildingRequest,
                    assetRequest,
                    asset,
                    instanceIndex,
                    result,
                    candidates);
            }
        }

        private static void AddFloorCandidates(
            ProceduralBuildingRequest buildingRequest,
            ProceduralAssetRequest assetRequest,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            GeneratedFunctionalZone zone = FindHostZone(result, assetRequest.PreferredHostZone);
            if (zone == null)
            {
                return;
            }

            float step = CommercialGenerationDimensions.FixturePlacementIncrementMeters;
            float firstX = Mathf.Ceil(zone.BoundsMeters.MinX / step) * step;
            float firstZ = Mathf.Ceil(zone.BoundsMeters.MinZ / step) * step;
            for (int rotation = 0; rotation < 2; rotation++)
            {
                float yaw = rotation * 90f;
                for (float z = firstZ; z <= zone.BoundsMeters.MaxZ + Tolerance; z += step)
                {
                    for (float x = firstX; x <= zone.BoundsMeters.MaxX + Tolerance; x += step)
                    {
                        Vector3 position = new(x, 0f, z);
                        PlanRect physical = BuildPhysicalBounds(asset, position, yaw);
                        List<GeneratedAssetClearance> clearances = BuildClearances(
                            asset,
                            position,
                            yaw);
                        if (!zone.BoundsMeters.Contains(physical) ||
                            !IsCandidateValid(
                                result,
                                physical,
                                clearances,
                                ProceduralMountingMode.Floor,
                                null,
                                false))
                        {
                            continue;
                        }

                        float score = ScoreFloorCandidate(
                            buildingRequest,
                            assetRequest,
                            asset,
                            instanceIndex,
                            position,
                            yaw,
                            physical,
                            zone,
                            result);
                        if (score <= -9999f)
                        {
                            continue;
                        }
                        candidates.Add(new PlacementCandidate(
                            asset,
                            ProceduralMountingMode.Floor,
                            position,
                            yaw,
                            physical,
                            score));
                    }
                }
            }
        }

        private static float ScoreFloorCandidate(
            ProceduralBuildingRequest buildingRequest,
            ProceduralAssetRequest request,
            ProceduralAssetComponent asset,
            int instanceIndex,
            Vector3 position,
            float yaw,
            PlanRect physical,
            GeneratedFunctionalZone zone,
            ProceduralGenerationResult result)
        {
            float score = 100f + request.Priority * 0.01f;
            float wallDistance = MinimumWallDistance(physical, zone.BoundsMeters);
            switch (asset.WallRelationship)
            {
                case ProceduralWallRelationship.Required:
                    if (wallDistance >
                        CommercialGenerationDimensions.OpeningIncrementMeters + Tolerance)
                    {
                        return -10000f;
                    }
                    score += 35f;
                    break;
                case ProceduralWallRelationship.StronglyPreferred:
                    score += Mathf.Max(0f, 25f - wallDistance * 8f);
                    break;
                case ProceduralWallRelationship.Preferred:
                    score += Mathf.Max(0f, 12f - wallDistance * 4f);
                    break;
                case ProceduralWallRelationship.Avoided:
                    score += Mathf.Min(15f, wallDistance * 4f);
                    break;
            }

            if (asset.PrimaryCategory == ProceduralAssetCategory.Transaction)
            {
                score -= position.z * 1.5f;
            }
            else if (asset.PrimaryCategory == ProceduralAssetCategory.Storage ||
                     asset.PrimaryCategory == ProceduralAssetCategory.Service)
            {
                score += position.z;
            }

            if ((asset.AssemblyBehavior == ProceduralAssemblyBehavior.BankOrRepeatedRow ||
                 asset.AssemblyBehavior == ProceduralAssemblyBehavior.LinearRun) &&
                instanceIndex > 0)
            {
                foreach (GeneratedAssetPlacement placed in result.Placements.Where(item =>
                             item.RequestId == request.RequestId))
                {
                    float xDifference = Mathf.Abs(
                        placed.LocalPositionMeters.x - position.x);
                    float zDifference = Mathf.Abs(
                        placed.LocalPositionMeters.z - position.z);
                    bool aligned = xDifference <= Tolerance || zDifference <= Tolerance;
                    if (aligned && Mathf.Abs(placed.YawDegrees - yaw) <= Tolerance)
                    {
                        score += 100f - Mathf.Min(80f, (xDifference + zDifference) * 10f);
                    }
                }
            }

            score += StableCandidateJitter(
                buildingRequest.Seed,
                asset.StableAssetId,
                position,
                yaw,
                instanceIndex) * 8f;
            return score;
        }

        private static float MinimumWallDistance(PlanRect physical, PlanRect zone)
        {
            return Mathf.Min(
                physical.MinX - zone.MinX,
                physical.MinZ - zone.MinZ,
                zone.MaxX - physical.MaxX,
                zone.MaxZ - physical.MaxZ);
        }

        private static void AddWallCandidates(
            ProceduralAssetRequest request,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            GeneratedFunctionalZone zone = FindHostZone(result, request.PreferredHostZone);
            if (zone == null)
            {
                return;
            }

            float halfWidth = asset.PhysicalSizeMeters.x * 0.5f;
            foreach (GeneratedWallSegment wall in result.Walls.Where(item =>
                         item.FrontageRole != FrontageRole.SharedInternal))
            {
                float minimum = CommercialGenerationDimensions.MinimumOpeningCornerDistanceMeters +
                                halfWidth;
                float maximum = wall.LengthMeters - minimum;
                for (float offset = CommercialGenerationDimensions.SnapOpening(minimum);
                     offset <= maximum + Tolerance;
                     offset += CommercialGenerationDimensions.OpeningIncrementMeters)
                {
                    Vector2 pivot = wall.InteriorFacePointAt(offset);
                    float yaw = YawForForward(wall.InwardNormal);
                    Vector3 position = new(
                        pivot.x,
                        asset.PreferredMountHeightMeters,
                        pivot.y);
                    PlanRect physical = BuildPhysicalBounds(asset, position, yaw);
                    List<GeneratedAssetClearance> clearances = BuildClearances(
                        asset,
                        position,
                        yaw);
                    if (WallMountIntersectsOpening(
                            result,
                            wall,
                            offset,
                            asset,
                            position.y) ||
                        !zone.BoundsMeters.Contains(physical) ||
                        !IsCandidateValid(
                            result,
                            physical,
                            clearances,
                            ProceduralMountingMode.Wall,
                            null,
                            false))
                    {
                        continue;
                    }

                    float score = 150f + request.Priority * 0.01f +
                                  StableCandidateJitter(
                                      result.Seed,
                                      asset.StableAssetId,
                                      position,
                                      yaw,
                                      instanceIndex) * 8f;
                    candidates.Add(new PlacementCandidate(
                        asset,
                        ProceduralMountingMode.Wall,
                        position,
                        yaw,
                        physical,
                        score,
                        wall.WallId));
                }
            }
        }

        private static bool WallMountIntersectsOpening(
            ProceduralGenerationResult result,
            GeneratedWallSegment wall,
            float offsetMeters,
            ProceduralAssetComponent asset,
            float pivotHeightMeters)
        {
            float halfWidth = asset.PhysicalSizeMeters.x * 0.5f;
            float bottom = asset.PivotConvention ==
                           ProceduralPivotConvention.WallOpeningBottomCenter
                ? pivotHeightMeters
                : pivotHeightMeters - asset.PhysicalSizeMeters.y * 0.5f;
            float top = bottom + asset.PhysicalSizeMeters.y;
            return result.Openings.Any(opening =>
                opening.WallId == wall.WallId &&
                opening.Kind != ProceduralOpeningKind.InteriorDoor &&
                offsetMeters - halfWidth < opening.EndOffsetMeters - Tolerance &&
                offsetMeters + halfWidth > opening.StartOffsetMeters + Tolerance &&
                bottom < opening.SillMeters + opening.HeightMeters - Tolerance &&
                top > opening.SillMeters + Tolerance);
        }

        private static void AddSocketCandidates(
            ProceduralAssetRequest request,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            foreach (GeneratedAssetPlacement parent in result.Placements)
            {
                ProceduralAssetComponent parentAsset = parent.Prefab == null
                    ? null
                    : parent.Prefab.GetComponent<ProceduralAssetComponent>();
                if (parentAsset == null)
                {
                    continue;
                }

                foreach (ProceduralSocketComponent socket in parentAsset.Sockets)
                {
                    if (!string.Equals(
                            socket.CompatibilityTag,
                            asset.RequiredSocketCompatibility,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Quaternion parentRotation = Quaternion.Euler(0f, parent.YawDegrees, 0f);
                    Vector3 position = parent.LocalPositionMeters +
                                       parentRotation * socket.transform.localPosition;
                    float yaw = parent.YawDegrees + socket.transform.localEulerAngles.y;
                    PlanRect physical = BuildPhysicalBounds(asset, position, yaw);
                    List<GeneratedAssetClearance> clearances = BuildClearances(
                        asset,
                        position,
                        yaw);
                    if (!result.Footprint.Contains(physical) ||
                        !IsCandidateValid(
                            result,
                            physical,
                            clearances,
                            ProceduralMountingMode.Socket,
                            parent.PlacementId,
                            false))
                    {
                        continue;
                    }

                    float score = 220f + request.Priority * 0.01f +
                                  StableCandidateJitter(
                                      result.Seed,
                                      asset.StableAssetId,
                                      position,
                                      yaw,
                                      instanceIndex) * 4f;
                    candidates.Add(new PlacementCandidate(
                        asset,
                        ProceduralMountingMode.Socket,
                        position,
                        yaw,
                        physical,
                        score,
                        null,
                        parent.PlacementId,
                        socket.SocketId));
                }
            }
        }

        private static void AddExteriorPadCandidates(
            ProceduralAssetRequest request,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            PlanRect unit = result.Units[0].BoundsMeters;
            float padDepth = CommercialGenerationDimensions.Feet(14f);
            PlanRect pad = new(
                unit.MinX,
                -padDepth - CommercialGenerationDimensions.Feet(2f),
                unit.Width,
                padDepth);
            float step = CommercialGenerationDimensions.OpeningIncrementMeters;
            float firstX = Mathf.Ceil(pad.MinX / step) * step;
            float firstZ = Mathf.Ceil(pad.MinZ / step) * step;
            for (float z = firstZ; z <= pad.MaxZ + Tolerance; z += step)
            {
                for (float x = firstX; x <= pad.MaxX + Tolerance; x += step)
                {
                    Vector3 position = new(x, 0f, z);
                    PlanRect physical = BuildPhysicalBounds(asset, position, 0f);
                    List<GeneratedAssetClearance> clearances = BuildClearances(
                        asset,
                        position,
                        0f);
                    if (!pad.Contains(physical) ||
                        clearances.Any(item => !pad.Contains(item.BoundsMeters)) ||
                        !IsCandidateValid(
                            result,
                            physical,
                            clearances,
                            ProceduralMountingMode.ExteriorPad,
                            null,
                            true))
                    {
                        continue;
                    }

                    float score = 120f + request.Priority * 0.01f +
                                  StableCandidateJitter(
                                      result.Seed,
                                      asset.StableAssetId,
                                      position,
                                      0f,
                                      instanceIndex) * 10f;
                    candidates.Add(new PlacementCandidate(
                        asset,
                        ProceduralMountingMode.ExteriorPad,
                        position,
                        0f,
                        physical,
                        score));
                }
            }
        }

        private static void AddCeilingCandidates(
            ProceduralAssetRequest request,
            ProceduralAssetComponent asset,
            int instanceIndex,
            ProceduralGenerationResult result,
            ICollection<PlacementCandidate> candidates)
        {
            GeneratedFunctionalZone zone = FindHostZone(result, request.PreferredHostZone);
            if (zone == null)
            {
                return;
            }

            Vector2 center = zone.BoundsMeters.Center;
            Vector3 position = new(
                CommercialGenerationDimensions.SnapOpening(center.x),
                result.Profile.finishedCeilingHeightMeters,
                CommercialGenerationDimensions.SnapOpening(center.y));
            PlanRect physical = BuildPhysicalBounds(asset, position, 0f);
            if (!zone.BoundsMeters.Contains(physical))
            {
                return;
            }

            candidates.Add(new PlacementCandidate(
                asset,
                ProceduralMountingMode.Ceiling,
                position,
                0f,
                physical,
                100f + request.Priority * 0.01f + instanceIndex));
        }

        private static GeneratedFunctionalZone FindHostZone(
            ProceduralGenerationResult result,
            FunctionalZoneType preferred)
        {
            return result.Zones.FirstOrDefault(item => item.ZoneType == preferred) ??
                   result.Zones.FirstOrDefault(item =>
                       item.ZoneType == FunctionalZoneType.Public);
        }

        private static PlanRect BuildPhysicalBounds(
            ProceduralAssetComponent asset,
            Vector3 position,
            float yawDegrees)
        {
            Vector2 localCenter = asset.PivotConvention ==
                                  ProceduralPivotConvention.WallMountPlaneCenter
                ? new Vector2(0f, asset.PhysicalSizeMeters.z * 0.5f)
                : Vector2.zero;
            Vector2 worldCenter = new Vector2(position.x, position.z) +
                                  Rotate(localCenter, yawDegrees);
            bool swapped = Mathf.RoundToInt(yawDegrees / 90f) % 2 != 0;
            float width = swapped
                ? asset.PhysicalSizeMeters.z
                : asset.PhysicalSizeMeters.x;
            float depth = swapped
                ? asset.PhysicalSizeMeters.x
                : asset.PhysicalSizeMeters.z;
            return new PlanRect(
                worldCenter.x - width * 0.5f,
                worldCenter.y - depth * 0.5f,
                width,
                depth);
        }

        private static List<GeneratedAssetClearance> BuildClearances(
            ProceduralAssetComponent asset,
            Vector3 position,
            float yawDegrees)
        {
            List<GeneratedAssetClearance> generated = new();
            foreach (ProceduralClearanceDefinition clearance in asset.Clearances)
            {
                Vector2 center = new Vector2(position.x, position.z) + Rotate(
                    new Vector2(
                        clearance.LocalCenterMeters.x,
                        clearance.LocalCenterMeters.z),
                    yawDegrees);
                bool swapped = Mathf.RoundToInt(yawDegrees / 90f) % 2 != 0;
                float width = swapped
                    ? clearance.SizeMeters.z
                    : clearance.SizeMeters.x;
                float depth = swapped
                    ? clearance.SizeMeters.x
                    : clearance.SizeMeters.z;
                generated.Add(new GeneratedAssetClearance(
                    clearance.ClearanceId,
                    clearance.Kind,
                    new PlanRect(
                        center.x - width * 0.5f,
                        center.y - depth * 0.5f,
                        width,
                        depth),
                    clearance.Required));
            }

            return generated;
        }

        private static Vector2 Rotate(Vector2 local, float yawDegrees)
        {
            float radians = yawDegrees * Mathf.Deg2Rad;
            float sine = Mathf.Sin(radians);
            float cosine = Mathf.Cos(radians);
            return new Vector2(
                local.x * cosine + local.y * sine,
                -local.x * sine + local.y * cosine);
        }

        private static float YawForForward(Vector2 forward)
        {
            return Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg;
        }

        private static bool IsCandidateValid(
            ProceduralGenerationResult result,
            PlanRect physical,
            IReadOnlyList<GeneratedAssetClearance> clearances,
            ProceduralMountingMode mountingMode,
            string ignoredParentPlacementId,
            bool exterior)
        {
            if (!exterior && !result.Footprint.Contains(physical))
            {
                return false;
            }

            if (!exterior)
            {
                foreach (GeneratedAssetClearance clearance in clearances.Where(item =>
                             item.Required))
                {
                    if (!result.Footprint.Contains(clearance.BoundsMeters))
                    {
                        return false;
                    }
                }

                foreach (GeneratedCirculationPath path in result.Circulation)
                {
                    if (physical.Overlaps(path.BoundsMeters))
                    {
                        return false;
                    }
                }

                foreach (GeneratedPartition partition in result.Partitions)
                {
                    if (physical.Overlaps(PartitionBounds(partition)))
                    {
                        return false;
                    }
                }
            }

            bool elevated = mountingMode == ProceduralMountingMode.Ceiling ||
                            mountingMode == ProceduralMountingMode.Socket;
            foreach (GeneratedAssetPlacement placed in result.Placements)
            {
                if (string.Equals(
                        placed.PlacementId,
                        ignoredParentPlacementId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                bool placedElevated = placed.MountingMode == ProceduralMountingMode.Ceiling ||
                                      placed.MountingMode == ProceduralMountingMode.Socket;
                if (!elevated && !placedElevated &&
                    physical.Overlaps(placed.PhysicalBoundsMeters))
                {
                    return false;
                }

                if (!elevated)
                {
                    foreach (GeneratedAssetClearance existingClearance in
                             placed.Clearances.Where(item => item.Required))
                    {
                        if (physical.Overlaps(existingClearance.BoundsMeters))
                        {
                            return false;
                        }
                    }
                }

                if (!placedElevated)
                {
                    foreach (GeneratedAssetClearance clearance in clearances.Where(item =>
                                 item.Required))
                    {
                        if (clearance.BoundsMeters.Overlaps(placed.PhysicalBoundsMeters))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static PlanRect PartitionBounds(GeneratedPartition partition)
        {
            Vector2 center = (partition.StartMeters + partition.EndMeters) * 0.5f;
            bool horizontal = Mathf.Abs(
                partition.StartMeters.y - partition.EndMeters.y) <= Tolerance;
            return new PlanRect(
                center.x - (horizontal ? partition.LengthMeters : partition.ThicknessMeters) * 0.5f,
                center.y - (horizontal ? partition.ThicknessMeters : partition.LengthMeters) * 0.5f,
                horizontal ? partition.LengthMeters : partition.ThicknessMeters,
                horizontal ? partition.ThicknessMeters : partition.LengthMeters);
        }

        private static float StableCandidateJitter(
            int seed,
            string assetId,
            Vector3 position,
            float yaw,
            int instanceIndex)
        {
            unchecked
            {
                uint hash = (uint)seed ^ 2166136261;
                foreach (char character in assetId)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                hash ^= (uint)Mathf.RoundToInt(position.x * 1000f);
                hash *= 16777619;
                hash ^= (uint)Mathf.RoundToInt(position.z * 1000f);
                hash *= 16777619;
                hash ^= (uint)Mathf.RoundToInt(yaw);
                hash *= 16777619;
                hash ^= (uint)instanceIndex;
                return (hash & 0xffff) / 65535f;
            }
        }

        private static bool TryValidateGeneratedLayout(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result,
            Vector2 entrancePoint,
            out string error)
        {
            if (!result.Footprint.IsContiguous())
            {
                error = "Footprint validation found disconnected usable floor area.";
                return false;
            }

            if (!ValidateOpeningGeometry(result, out error) ||
                !ValidateQuantities(request.BusinessRecipe, result, out error) ||
                !ValidatePlacementGeometry(result, out error) ||
                !ValidateCirculation(result, entrancePoint, out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private static bool ValidateOpeningGeometry(
            ProceduralGenerationResult result,
            out string error)
        {
            foreach (IGrouping<string, GeneratedOpening> group in result.Openings
                         .Where(item => item.Kind != ProceduralOpeningKind.InteriorDoor)
                         .GroupBy(item => item.WallId))
            {
                GeneratedWallSegment wall = result.Walls.FirstOrDefault(item =>
                    item.WallId == group.Key);
                if (wall == null)
                {
                    error = $"Opening group references missing wall '{group.Key}'.";
                    return false;
                }

                List<GeneratedOpening> ordered = group.OrderBy(item => item.StartOffsetMeters)
                    .ThenBy(item => item.SillMeters)
                    .ToList();
                for (int index = 0; index < ordered.Count; index++)
                {
                    GeneratedOpening opening = ordered[index];
                    if (!CommercialGenerationDimensions.IsOpeningAligned(opening.StartOffsetMeters) ||
                        !CommercialGenerationDimensions.IsOpeningAligned(opening.EndOffsetMeters) ||
                        opening.StartOffsetMeters < -Tolerance ||
                        opening.EndOffsetMeters > wall.LengthMeters + Tolerance ||
                        opening.SillMeters < 0f ||
                        opening.SillMeters + opening.HeightMeters > wall.HeightMeters + Tolerance)
                    {
                        error = $"Opening '{opening.OpeningId}' is off-grid or outside wall '{wall.WallId}'.";
                        return false;
                    }

                    for (int otherIndex = 0; otherIndex < index; otherIndex++)
                    {
                        GeneratedOpening other = ordered[otherIndex];
                        bool horizontalOverlap = opening.StartOffsetMeters <
                                                 other.EndOffsetMeters - Tolerance &&
                                                 opening.EndOffsetMeters >
                                                 other.StartOffsetMeters + Tolerance;
                        bool verticalOverlap = opening.SillMeters <
                                               other.SillMeters + other.HeightMeters - Tolerance &&
                                               opening.SillMeters + opening.HeightMeters >
                                               other.SillMeters + Tolerance;
                        if (horizontalOverlap && verticalOverlap)
                        {
                            error = $"Openings '{other.OpeningId}' and '{opening.OpeningId}' overlap.";
                            return false;
                        }
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool ValidateQuantities(
            ProceduralBusinessRecipe recipe,
            ProceduralGenerationResult result,
            out string error)
        {
            foreach (ProceduralAssetRequest request in recipe.AssetRequests)
            {
                int count = result.Placements.Count(item =>
                    item.RequestId == request.RequestId);
                if ((request.Required && count < request.Minimum) ||
                    count > request.Maximum)
                {
                    error = $"Request '{request.RequestId}' generated {count}; allowed required range is {request.Minimum}-{request.Maximum}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool ValidatePlacementGeometry(
            ProceduralGenerationResult result,
            out string error)
        {
            for (int index = 0; index < result.Placements.Count; index++)
            {
                GeneratedAssetPlacement placement = result.Placements[index];
                bool exterior = placement.MountingMode == ProceduralMountingMode.ExteriorPad;
                if (!exterior && !result.Footprint.Contains(placement.PhysicalBoundsMeters))
                {
                    error = $"Placement '{placement.PlacementId}' overlaps architectural boundaries.";
                    return false;
                }

                if ((placement.MountingMode == ProceduralMountingMode.Floor ||
                     placement.MountingMode == ProceduralMountingMode.Wall) &&
                    result.Circulation.Any(path =>
                        placement.PhysicalBoundsMeters.Overlaps(path.BoundsMeters)))
                {
                    error = $"Placement '{placement.PlacementId}' blocks required circulation.";
                    return false;
                }

                if (placement.MountingMode == ProceduralMountingMode.Wall)
                {
                    GeneratedWallSegment wall = result.Walls.FirstOrDefault(item =>
                        item.WallId == placement.HostWallId);
                    ProceduralAssetComponent asset = placement.Prefab == null
                        ? null
                        : placement.Prefab.GetComponent<ProceduralAssetComponent>();
                    if (wall == null || asset == null)
                    {
                        error = $"Wall placement '{placement.PlacementId}' has invalid host metadata.";
                        return false;
                    }

                    Vector2 position = new(
                        placement.LocalPositionMeters.x,
                        placement.LocalPositionMeters.z);
                    float offset = Vector2.Dot(
                        position - wall.StartMeters,
                        wall.Direction);
                    if (WallMountIntersectsOpening(
                            result,
                            wall,
                            offset,
                            asset,
                            placement.LocalPositionMeters.y))
                    {
                        error = $"Wall placement '{placement.PlacementId}' overlaps an architectural opening.";
                        return false;
                    }
                }

                for (int otherIndex = index + 1;
                     otherIndex < result.Placements.Count;
                     otherIndex++)
                {
                    GeneratedAssetPlacement other = result.Placements[otherIndex];
                    if (string.Equals(
                            placement.ParentPlacementId,
                            other.PlacementId,
                            StringComparison.Ordinal) ||
                        string.Equals(
                            other.ParentPlacementId,
                            placement.PlacementId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bool placementElevated =
                        placement.MountingMode == ProceduralMountingMode.Ceiling ||
                        placement.MountingMode == ProceduralMountingMode.Socket;
                    bool otherElevated =
                        other.MountingMode == ProceduralMountingMode.Ceiling ||
                        other.MountingMode == ProceduralMountingMode.Socket;
                    if (!placementElevated && !otherElevated &&
                        placement.PhysicalBoundsMeters.Overlaps(other.PhysicalBoundsMeters))
                    {
                        error = $"Placements '{placement.PlacementId}' and '{other.PlacementId}' overlap.";
                        return false;
                    }

                    if (!placementElevated && !otherElevated &&
                        (placement.Clearances.Where(item => item.Required).Any(clearance =>
                             clearance.BoundsMeters.Overlaps(other.PhysicalBoundsMeters)) ||
                         other.Clearances.Where(item => item.Required).Any(clearance =>
                             clearance.BoundsMeters.Overlaps(placement.PhysicalBoundsMeters))))
                    {
                        error = $"Placement '{placement.PlacementId}' or '{other.PlacementId}' blocks required interaction clearance.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool ValidateCirculation(
            ProceduralGenerationResult result,
            Vector2 entrancePoint,
            out string error)
        {
            float cellSize = CommercialGenerationDimensions.OpeningIncrementMeters;
            PlanRect bounds = result.Footprint.Bounds;
            int width = Mathf.CeilToInt(bounds.Width / cellSize);
            int depth = Mathf.CeilToInt(bounds.Depth / cellSize);
            bool[,] walkable = new bool[width, depth];
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new(
                        (x + 0.5f) * cellSize,
                        (z + 0.5f) * cellSize);
                    bool isWalkable = result.Footprint.Contains(point) &&
                                      !IsBlockedByPartition(point, result.Partitions) &&
                                      !result.Placements.Any(item =>
                                          item.MountingMode != ProceduralMountingMode.Ceiling &&
                                          item.MountingMode != ProceduralMountingMode.Socket &&
                                          item.MountingMode != ProceduralMountingMode.ExteriorPad &&
                                          item.PhysicalBoundsMeters.Contains(point));
                    walkable[x, z] = isWalkable;
                }
            }

            Vector2 inwardStart = entrancePoint + Vector2.up * cellSize;
            Vector2Int start = new(
                Mathf.Clamp(Mathf.FloorToInt(inwardStart.x / cellSize), 0, width - 1),
                Mathf.Clamp(Mathf.FloorToInt(inwardStart.y / cellSize), 0, depth - 1));
            if (!walkable[start.x, start.y])
            {
                start = FindNearestWalkable(start, walkable);
            }
            if (start.x < 0)
            {
                error = "No walkable cell exists behind the primary entrance.";
                return false;
            }

            bool[,] visited = new bool[width, depth];
            Queue<Vector2Int> open = new();
            open.Enqueue(start);
            visited[start.x, start.y] = true;
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };
            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= width ||
                        next.y < 0 || next.y >= depth ||
                        visited[next.x, next.y] || !walkable[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    open.Enqueue(next);
                }
            }

            foreach (GeneratedAssetPlacement placement in result.Placements)
            {
                if (placement.MountingMode == ProceduralMountingMode.ExteriorPad ||
                    placement.MountingMode == ProceduralMountingMode.Ceiling ||
                    placement.MountingMode == ProceduralMountingMode.Socket)
                {
                    continue;
                }

                foreach (GeneratedAssetClearance clearance in placement.Clearances.Where(item =>
                             item.Required))
                {
                    if (!AnyVisitedCell(clearance.BoundsMeters, visited, cellSize))
                    {
                        error = $"Required clearance '{clearance.ClearanceId}' for placement '{placement.PlacementId}' is unreachable.";
                        return false;
                    }
                }
            }

            foreach (GeneratedCirculationPath path in result.Circulation)
            {
                if (!CirculationEndpointsVisited(
                        path.BoundsMeters,
                        visited,
                        cellSize))
                {
                    error = $"Required circulation path '{path.PathId}' is obstructed or disconnected.";
                    return false;
                }
            }

            foreach (GeneratedFunctionalZone zone in result.Zones)
            {
                if (!AnyVisitedCell(zone.BoundsMeters, visited, cellSize))
                {
                    error = $"Functional zone '{zone.ZoneId}' is inaccessible from the primary entrance.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool CirculationEndpointsVisited(
            PlanRect bounds,
            bool[,] visited,
            float cellSize)
        {
            PlanRect start;
            PlanRect end;
            if (bounds.Depth >= bounds.Width)
            {
                start = new PlanRect(bounds.MinX, bounds.MinZ, bounds.Width, cellSize);
                end = new PlanRect(
                    bounds.MinX,
                    bounds.MaxZ - cellSize,
                    bounds.Width,
                    cellSize);
            }
            else
            {
                start = new PlanRect(bounds.MinX, bounds.MinZ, cellSize, bounds.Depth);
                end = new PlanRect(
                    bounds.MaxX - cellSize,
                    bounds.MinZ,
                    cellSize,
                    bounds.Depth);
            }

            return AnyVisitedCell(start, visited, cellSize) &&
                   AnyVisitedCell(end, visited, cellSize);
        }

        private static bool IsBlockedByPartition(
            Vector2 point,
            IReadOnlyList<GeneratedPartition> partitions)
        {
            foreach (GeneratedPartition partition in partitions)
            {
                PlanRect bounds = PartitionBounds(partition);
                if (!bounds.Contains(point))
                {
                    continue;
                }

                bool horizontal = Mathf.Abs(
                    partition.StartMeters.y - partition.EndMeters.y) <= Tolerance;
                float along = horizontal
                    ? point.x - partition.StartMeters.x
                    : point.y - partition.StartMeters.y;
                if (partition.Openings.Any(opening =>
                        along >= opening.StartOffsetMeters - Tolerance &&
                        along <= opening.EndOffsetMeters + Tolerance))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static Vector2Int FindNearestWalkable(Vector2Int origin, bool[,] walkable)
        {
            int width = walkable.GetLength(0);
            int depth = walkable.GetLength(1);
            int maximum = width + depth;
            for (int radius = 0; radius <= maximum; radius++)
            {
                for (int xOffset = -radius; xOffset <= radius; xOffset++)
                {
                    int zMagnitude = radius - Mathf.Abs(xOffset);
                    foreach (int zOffset in zMagnitude == 0
                                 ? new[] { 0 }
                                 : new[] { -zMagnitude, zMagnitude })
                    {
                        int x = origin.x + xOffset;
                        int z = origin.y + zOffset;
                        if (x >= 0 && x < width && z >= 0 && z < depth &&
                            walkable[x, z])
                        {
                            return new Vector2Int(x, z);
                        }
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }

        private static bool AnyVisitedCell(
            PlanRect bounds,
            bool[,] visited,
            float cellSize)
        {
            int minimumX = Mathf.Max(0, Mathf.FloorToInt(bounds.MinX / cellSize));
            int maximumX = Mathf.Min(
                visited.GetLength(0) - 1,
                Mathf.FloorToInt(bounds.MaxX / cellSize));
            int minimumZ = Mathf.Max(0, Mathf.FloorToInt(bounds.MinZ / cellSize));
            int maximumZ = Mathf.Min(
                visited.GetLength(1) - 1,
                Mathf.FloorToInt(bounds.MaxZ / cellSize));
            for (int z = minimumZ; z <= maximumZ; z++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    if (visited[x, z])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float ScoreFinalLayout(
            ProceduralBuildingRequest request,
            ProceduralGenerationResult result)
        {
            float score = 1000f;
            foreach (ProceduralAssetRequest assetRequest in request.BusinessRecipe.AssetRequests)
            {
                int count = result.Placements.Count(item =>
                    item.RequestId == assetRequest.RequestId);
                score += Mathf.Min(count, assetRequest.Preferred) * 10f;
                score -= Mathf.Max(0, assetRequest.Preferred - count) * 4f;
            }

            score += result.Openings.Count(item =>
                item.Kind == ProceduralOpeningKind.StorefrontWindow) * 5f;
            score += result.Footprint.Kind == OrthogonalFootprintKind.Rectangle ? 0f : 8f;
            return score;
        }

        private sealed class EdgeRun
        {
            public readonly bool Horizontal;
            public readonly int Constant;
            public readonly Vector2 Normal;
            public readonly List<int> UnitStarts = new();

            public EdgeRun(bool horizontal, int constant, Vector2 normal)
            {
                Horizontal = horizontal;
                Constant = constant;
                Normal = normal;
            }
        }

        private readonly struct EdgeSegment
        {
            public readonly bool Horizontal;
            public readonly int Constant;
            public readonly int Start;
            public readonly int End;
            public readonly Vector2 Normal;

            public EdgeSegment(
                bool horizontal,
                int constant,
                int start,
                int end,
                Vector2 normal)
            {
                Horizontal = horizontal;
                Constant = constant;
                Start = start;
                End = end;
                Normal = normal;
            }

            public static int Compare(EdgeSegment left, EdgeSegment right)
            {
                int comparison = left.Normal.y.CompareTo(right.Normal.y);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = left.Normal.x.CompareTo(right.Normal.x);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = left.Constant.CompareTo(right.Constant);
                return comparison != 0 ? comparison : left.Start.CompareTo(right.Start);
            }
        }

        private readonly struct UnitCandidate
        {
            public readonly int StartIndex;
            public readonly int EndIndex;
            public readonly float WidthMeters;
            public readonly float AreaSquareFeet;
            public readonly List<string> BayIds;

            public UnitCandidate(
                int startIndex,
                int endIndex,
                float widthMeters,
                float areaSquareFeet,
                List<string> bayIds)
            {
                StartIndex = startIndex;
                EndIndex = endIndex;
                WidthMeters = widthMeters;
                AreaSquareFeet = areaSquareFeet;
                BayIds = bayIds;
            }
        }

        private readonly struct PlacementCandidate
        {
            public readonly ProceduralAssetComponent Asset;
            public readonly ProceduralMountingMode MountingMode;
            public readonly Vector3 PositionMeters;
            public readonly float YawDegrees;
            public readonly PlanRect PhysicalBounds;
            public readonly float Score;
            public readonly string HostWallId;
            public readonly string ParentPlacementId;
            public readonly string HostSocketId;

            public PlacementCandidate(
                ProceduralAssetComponent asset,
                ProceduralMountingMode mountingMode,
                Vector3 positionMeters,
                float yawDegrees,
                PlanRect physicalBounds,
                float score,
                string hostWallId = null,
                string parentPlacementId = null,
                string hostSocketId = null)
            {
                Asset = asset;
                MountingMode = mountingMode;
                PositionMeters = positionMeters;
                YawDegrees = yawDegrees;
                PhysicalBounds = physicalBounds;
                Score = score;
                HostWallId = hostWallId;
                ParentPlacementId = parentPlacementId;
                HostSocketId = hostSocketId;
            }

            public static int Compare(PlacementCandidate left, PlacementCandidate right)
            {
                int comparison = right.Score.CompareTo(left.Score);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = string.CompareOrdinal(
                    left.Asset.StableAssetId,
                    right.Asset.StableAssetId);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = left.PositionMeters.x.CompareTo(right.PositionMeters.x);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = left.PositionMeters.z.CompareTo(right.PositionMeters.z);
                return comparison != 0
                    ? comparison
                    : left.YawDegrees.CompareTo(right.YawDegrees);
            }
        }
    }
}
