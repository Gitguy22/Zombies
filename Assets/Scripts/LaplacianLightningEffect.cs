using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LaplacianLightningEffect : MonoBehaviour
{
    [Header("Laplacian Growth Settings")]
    public float eta = 6.3f; // Controls dimension (1D to 3D growth)
    public float R1 = 0.5f; // Inner radius for potential calculation
    public float chargeStrength = 1.0f;
    public int maxGrowthSites = 1000;
    public float gridCellSize = 0.1f;
    public bool useAdaptiveGridSize = true;
    public float minGridSize = 0.05f;
    public float maxGridSize = 0.3f;

    [Header("Environmental Influences")]
    public bool useGroundPlane = true;
    public float groundHeight = 0f;
    public float groundChargeStrength = -1f;
    public List<Transform> attractors = new List<Transform>();
    public List<Transform> repulsors = new List<Transform>();

    [Header("Performance Settings")]
    public bool useOctree = true;
    public int maxCandidatesPerFrame = 100;
    public float potentialCacheTime = 0.1f;

    private class GrowthSite
    {
        public Vector3 position;
        public float potential;
        public float lastUpdateTime;
        public List<GrowthSite> neighbors = new List<GrowthSite>();
        public int generation = 0;
    }

    private List<Vector3> chargePositions = new List<Vector3>();
    private Dictionary<Vector3Int, GrowthSite> candidateSites = new Dictionary<Vector3Int, GrowthSite>();
    private List<GrowthSite> aggregate = new List<GrowthSite>();
    private List<Vector3> optimizedPath = new List<Vector3>();

    // Cache for performance
    private Dictionary<Vector3Int, float> potentialCache = new Dictionary<Vector3Int, float>();
    private float lastCacheTime = 0f;

    public List<Vector3> GenerateLightning(Vector3 start, Vector3 end)
    {
        // Clear previous state
        Initialize();

        // Add initial charge
        AddCharge(start);

        // Set up target
        Vector3 targetDirection = (end - start).normalized;
        float totalDistance = Vector3.Distance(start, end);

        // Create initial candidate sites
        CreateCandidateSites(start, 0);

        // Main growth loop
        int iterations = 0;
        while (aggregate.Count < maxGrowthSites && candidateSites.Count > 0 && iterations < maxGrowthSites * 2)
        {
            iterations++;

            // Update grid size based on distance to target
            if (useAdaptiveGridSize && aggregate.Count > 0)
            {
                float distanceToTarget = Vector3.Distance(aggregate.Last().position, end);
                float progress = 1f - (distanceToTarget / totalDistance);
                gridCellSize = Mathf.Lerp(maxGridSize, minGridSize, progress);
            }

            // Calculate potentials for all candidate sites
            UpdatePotentials(end);

            // Select growth site based on weighted probability
            GrowthSite selectedSite = SelectGrowthSite(end);
            if (selectedSite == null) break;

            // Add to aggregate
            aggregate.Add(selectedSite);
            AddCharge(selectedSite.position);

            // Remove from candidates
            Vector3Int gridPos = GetGridPosition(selectedSite.position);
            candidateSites.Remove(gridPos);

            // Create new candidate sites around the selected site
            CreateCandidateSites(selectedSite.position, selectedSite.generation + 1);

            // Check if we've reached the target
            if (Vector3.Distance(selectedSite.position, end) < gridCellSize * 2)
            {
                aggregate.Add(new GrowthSite { position = end, generation = selectedSite.generation + 1 });
                break;
            }

            // Bias towards target after certain progress
            if (aggregate.Count > maxGrowthSites * 0.7f)
            {
                PruneCandidatesAwayFromTarget(end);
            }
        }

        // Convert aggregate to optimized path
        return OptimizePath(start, end);
    }

    void Initialize()
    {
        chargePositions.Clear();
        candidateSites.Clear();
        aggregate.Clear();
        optimizedPath.Clear();
        potentialCache.Clear();
        lastCacheTime = Time.time;
    }

    void AddCharge(Vector3 position)
    {
        chargePositions.Add(position);
        potentialCache.Clear(); // Invalidate cache when charges change
    }

    Vector3Int GetGridPosition(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x / gridCellSize),
            Mathf.RoundToInt(worldPos.y / gridCellSize),
            Mathf.RoundToInt(worldPos.z / gridCellSize)
        );
    }

    Vector3 GetWorldPosition(Vector3Int gridPos)
    {
        return new Vector3(
            gridPos.x * gridCellSize,
            gridPos.y * gridCellSize,
            gridPos.z * gridCellSize
        );
    }

    void CreateCandidateSites(Vector3 center, int generation)
    {
        // 26-connected neighborhood in 3D
        int[] offsets = { -1, 0, 1 };

        foreach (int dx in offsets)
        {
            foreach (int dy in offsets)
            {
                foreach (int dz in offsets)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    Vector3 offset = new Vector3(dx, dy, dz) * gridCellSize;
                    Vector3 newPos = center + offset;
                    Vector3Int gridPos = GetGridPosition(newPos);

                    // Check if position is already occupied or exists as candidate
                    if (!candidateSites.ContainsKey(gridPos) && !IsPositionOccupied(newPos))
                    {
                        GrowthSite newSite = new GrowthSite
                        {
                            position = newPos,
                            generation = generation,
                            lastUpdateTime = 0f
                        };
                        candidateSites[gridPos] = newSite;
                    }
                }
            }
        }
    }

    bool IsPositionOccupied(Vector3 pos)
    {
        Vector3Int gridPos = GetGridPosition(pos);

        // Check if any charge is at this grid position
        foreach (var chargePos in chargePositions)
        {
            if (GetGridPosition(chargePos) == gridPos)
                return true;
        }

        return false;
    }

    void UpdatePotentials(Vector3 target)
    {
        if (Time.time - lastCacheTime > potentialCacheTime)
        {
            potentialCache.Clear();
            lastCacheTime = Time.time;
        }

        float maxPotential = float.MinValue;
        float minPotential = float.MaxValue;

        // Calculate raw potentials for all candidate sites
        List<KeyValuePair<Vector3Int, GrowthSite>> sites = candidateSites.ToList();

        foreach (var kvp in sites)
        {
            Vector3Int gridPos = kvp.Key;
            GrowthSite site = kvp.Value;

            // Use cached value if available and recent
            if (potentialCache.ContainsKey(gridPos) && Time.time - site.lastUpdateTime < potentialCacheTime)
            {
                site.potential = potentialCache[gridPos];
            }
            else
            {
                site.potential = CalculatePotential(site.position, target);
                potentialCache[gridPos] = site.potential;
                site.lastUpdateTime = Time.time;
            }

            maxPotential = Mathf.Max(maxPotential, site.potential);
            minPotential = Mathf.Min(minPotential, site.potential);
        }

        // Normalize potentials to [0,1] range
        float range = maxPotential - minPotential;
        if (range > 0)
        {
            foreach (var site in candidateSites.Values)
            {
                site.potential = (site.potential - minPotential) / range;
            }
        }
    }

    float CalculatePotential(Vector3 position, Vector3 target)
    {
        float potential = 0;

        // Contribution from point charges (aggregate)
        foreach (var chargePos in chargePositions)
        {
            float distance = Vector3.Distance(position, chargePos);
            if (distance > 0.001f)
            {
                // Using the paper's formula: phi = 1 - R1/r
                potential += chargeStrength * (1f - R1 / distance);
            }
        }

        // Contribution from ground plane
        if (useGroundPlane)
        {
            float distanceToGround = Mathf.Abs(position.y - groundHeight);
            if (distanceToGround > 0.001f)
            {
                potential += groundChargeStrength / distanceToGround;
            }
        }

        // Contribution from explicit attractors
        foreach (var attractor in attractors)
        {
            if (attractor != null)
            {
                float distance = Vector3.Distance(position, attractor.position);
                if (distance > 0.001f)
                {
                    potential += 2f / distance;
                }
            }
        }

        // Contribution from repulsors
        foreach (var repulsor in repulsors)
        {
            if (repulsor != null)
            {
                float distance = Vector3.Distance(position, repulsor.position);
                if (distance > 0.001f)
                {
                    potential -= 2f / distance;
                }
            }
        }

        // Bias towards target (helps guide the lightning)
        float targetDistance = Vector3.Distance(position, target);
        float targetBias = 0.5f / (targetDistance + 1f);
        potential += targetBias;

        return potential;
    }

    GrowthSite SelectGrowthSite(Vector3 target)
    {
        if (candidateSites.Count == 0) return null;

        // Filter candidates to those closer to target than current aggregate
        List<GrowthSite> validCandidates = new List<GrowthSite>();
        float currentClosestDistance = float.MaxValue;

        if (aggregate.Count > 0)
        {
            currentClosestDistance = aggregate.Min(a => Vector3.Distance(a.position, target));
        }

        foreach (var site in candidateSites.Values)
        {
            float distanceToTarget = Vector3.Distance(site.position, target);
            // Allow some backtracking but prefer forward progress
            if (distanceToTarget < currentClosestDistance + gridCellSize * 5)
            {
                validCandidates.Add(site);
            }
        }

        if (validCandidates.Count == 0)
            validCandidates = candidateSites.Values.ToList();

        // Calculate probability distribution based on potential
        float[] probabilities = new float[validCandidates.Count];
        float totalProbability = 0;

        for (int i = 0; i < validCandidates.Count; i++)
        {
            // Using the paper's formula: p_i = phi^eta / Sum(phi^eta)
            probabilities[i] = Mathf.Pow(validCandidates[i].potential, eta);
            totalProbability += probabilities[i];
        }

        // Handle edge case where all probabilities are zero
        if (totalProbability == 0)
        {
            return validCandidates[Random.Range(0, validCandidates.Count)];
        }

        // Normalize probabilities
        for (int i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] /= totalProbability;
        }

        // Select based on weighted random
        float random = Random.value;
        float cumulative = 0;

        for (int i = 0; i < validCandidates.Count; i++)
        {
            cumulative += probabilities[i];
            if (random <= cumulative)
            {
                return validCandidates[i];
            }
        }

        return validCandidates[validCandidates.Count - 1];
    }

    void PruneCandidatesAwayFromTarget(Vector3 target)
    {
        List<Vector3Int> toRemove = new List<Vector3Int>();
        float maxAllowedDistance = aggregate.Count > 0 ?
            aggregate.Min(a => Vector3.Distance(a.position, target)) * 1.5f :
            float.MaxValue;

        foreach (var kvp in candidateSites)
        {
            float distance = Vector3.Distance(kvp.Value.position, target);
            if (distance > maxAllowedDistance)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            candidateSites.Remove(key);
        }
    }

    List<Vector3> OptimizePath(Vector3 start, Vector3 end)
    {
        if (aggregate.Count == 0)
            return new List<Vector3> { start, end };

        optimizedPath.Clear();
        optimizedPath.Add(start);

        // Sort aggregate points by distance along main direction
        Vector3 mainDirection = (end - start).normalized;
        var sortedPoints = aggregate
            .OrderBy(a => Vector3.Dot(a.position - start, mainDirection))
            .ToList();

        // Add points ensuring connectivity
        Vector3 currentPos = start;
        float connectionDistance = gridCellSize * 2f;

        foreach (var point in sortedPoints)
        {
            if (Vector3.Distance(currentPos, point.position) <= connectionDistance * 3f)
            {
                optimizedPath.Add(point.position);
                currentPos = point.position;
            }
        }

        // Ensure we connect to the end
        if (Vector3.Distance(currentPos, end) > connectionDistance)
        {
            // Find closest point to end
            var closestToEnd = aggregate
                .OrderBy(a => Vector3.Distance(a.position, end))
                .FirstOrDefault();

            if (closestToEnd != null && !optimizedPath.Contains(closestToEnd.position))
            {
                optimizedPath.Add(closestToEnd.position);
            }
        }

        optimizedPath.Add(end);

        // Smooth the path
        return SmoothPath(optimizedPath);
    }

    List<Vector3> SmoothPath(List<Vector3> path)
    {
        if (path.Count <= 2) return path;

        List<Vector3> smoothed = new List<Vector3>();
        smoothed.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 prev = path[i - 1];
            Vector3 current = path[i];
            Vector3 next = path[i + 1];

            // Simple averaging for smoothing
            Vector3 smoothedPoint = (prev + current * 2f + next) / 4f;
            smoothed.Add(smoothedPoint);
        }

        smoothed.Add(path[path.Count - 1]);
        return smoothed;
    }

    // Public methods for external influence
    public void AddAttractor(Transform attractor)
    {
        if (!attractors.Contains(attractor))
            attractors.Add(attractor);
    }

    public void AddRepulsor(Transform repulsor)
    {
        if (!repulsors.Contains(repulsor))
            repulsors.Add(repulsor);
    }

    public void ClearInfluences()
    {
        attractors.Clear();
        repulsors.Clear();
    }
}