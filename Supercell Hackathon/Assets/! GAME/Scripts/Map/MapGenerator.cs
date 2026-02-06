using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
	public static MapData Generate(MapConfig config, int seed)
	{
		System.Random rng = new System.Random(seed);
		MapData map = new MapData();
		map.totalRows = config.totalRows;

		int lastRow = config.totalRows - 1;

		// Track active split sub-lanes: key = lane index (-1 or 3), value = rows remaining
		Dictionary<int, int> activeSplits = new();

		// --- ROW 0: Starting nodes (always 3 BattleMinion) ---
		for (int lane = 0; lane < config.baseLanes; lane++)
		{
			MapNodeData node = CreateNode(0, lane, EncounterType.BattleMinion, rng);
			map.nodes.Add(node);
			map.startingNodeIds.Add(node.nodeId);
		}

		// --- MIDDLE ROWS ---
		for (int row = 1; row < lastRow; row++)
		{
			bool isCampRow = row >= lastRow - config.campRowsBeforeBoss;

			// Determine active lane positions for this row
			List<int> lanePositions = new List<int>();
			for (int lane = 0; lane < config.baseLanes; lane++)
				lanePositions.Add(lane);

			// Update existing splits
			List<int> expiredSplits = new();
			foreach (var kvp in activeSplits)
			{
				if (kvp.Value > 0)
					lanePositions.Add(kvp.Key);
			}

			// Decay splits
			List<int> splitKeys = new List<int>(activeSplits.Keys);
			for (int i = 0; i < splitKeys.Count; i++)
			{
				int key = splitKeys[i];
				activeSplits[key] = activeSplits[key] - 1;
				if (activeSplits[key] <= 0)
					expiredSplits.Add(key);
			}
			for (int i = 0; i < expiredSplits.Count; i++)
				activeSplits.Remove(expiredSplits[i]);

			// Try to create new splits on outer lanes
			if (!isCampRow && row > 1 && row < lastRow - 2)
			{
				// Left split (lane 0 -> sub-lane -1)
				if (!activeSplits.ContainsKey(-1) && rng.NextDouble() < config.laneSplitChance)
				{
					int duration = 2 + rng.Next(2); // 2-3 rows
					activeSplits[-1] = duration;
					lanePositions.Add(-1);
				}

				// Right split (lane 2 -> sub-lane 3)
				if (!activeSplits.ContainsKey(3) && rng.NextDouble() < config.laneSplitChance)
				{
					int duration = 2 + rng.Next(2);
					activeSplits[3] = duration;
					lanePositions.Add(3);
				}
			}

			lanePositions.Sort();

			// Track which lanes converge this row
			HashSet<int> convergedLanes = new();

			// Try lane convergence (two adjacent base lanes share a node)
			if (!isCampRow && row > 2 && rng.NextDouble() < config.laneConvergeChance)
			{
				// Pick two adjacent base lanes to converge
				int convergeLane = rng.Next(config.baseLanes - 1); // 0 or 1
				convergedLanes.Add(convergeLane + 1); // the right lane gets absorbed
			}

			// Place nodes
			bool shopPlacedThisRow = false;

			for (int i = 0; i < lanePositions.Count; i++)
			{
				int lane = lanePositions[i];

				// Skip if this lane is absorbed by convergence
				if (convergedLanes.Contains(lane))
					continue;

				// Node skip chance (but not for base lanes on early rows or camp rows)
				if (!isCampRow && row > 2 && lane >= 0 && lane < config.baseLanes)
				{
					if (rng.NextDouble() < config.nodeSkipChance)
					{
						// Only skip if there are still nodes in adjacent lanes to connect through
						continue;
					}
				}

				// Determine encounter type
				EncounterType encounterType;
				if (isCampRow)
				{
					encounterType = EncounterType.Camp;
				}
				else if (row <= 1)
				{
					// Early rows: only BattleMinion or Event
					encounterType = rng.NextDouble() < 0.7f
						? EncounterType.BattleMinion
						: EncounterType.Event;
				}
				else
				{
					encounterType = RollEncounterType(rng, config, shopPlacedThisRow);
					if (encounterType == EncounterType.Shop)
						shopPlacedThisRow = true;
				}

				MapNodeData node = CreateNode(row, lane, encounterType, rng);
				map.nodes.Add(node);
			}
		}

		// --- FINAL ROW: Boss node ---
		{
			MapNodeData bossNode = CreateNode(lastRow, 1, EncounterType.BattleBoss, null);
			map.nodes.Add(bossNode);
		}

		// --- CONNECTIONS ---
		GenerateConnections(map, config, rng);

		// --- ENFORCE NO CROSSING ---
		EnforceNoCrossing(map);

		// --- VERIFY & PATCH REACHABILITY ---
		VerifyAndPatchReachability(map);

		// --- ENFORCE NO 3+ CONSECUTIVE BATTLES ---
		FixConsecutiveBattles(map, rng, config);

		// --- CLEAN UP: remove any backward or same-row connections ---
		CleanBackwardConnections(map);

		// --- SET INITIAL AVAILABILITY ---
		map.UpdateAvailability();

		return map;
	}

	static MapNodeData CreateNode(int row, int lane, EncounterType encounterType, System.Random rng = null)
	{
		float ox = 0f;
		float oy = 0f;

		// Add jitter for organic feel (skip row 0 and boss row)
		if (rng != null && row > 0)
		{
			ox = ((float)rng.NextDouble() - 0.5f) * 0.7f; // +/- 0.35 units
			oy = ((float)rng.NextDouble() - 0.5f) * 0.4f; // +/- 0.2 units
		}

		return new MapNodeData
		{
			nodeId = $"r{row}_l{lane}",
			row = row,
			lane = lane,
			encounterType = encounterType,
			isCompleted = false,
			isAvailable = false,
			connectedNodeIds = new List<string>(),
			offsetX = ox,
			offsetY = oy
		};
	}

	static EncounterType RollEncounterType(System.Random rng, MapConfig config, bool shopAlreadyInRow)
	{
		float totalWeight = config.battleMinionWeight + config.eventWeight + config.campWeight;
		if (!shopAlreadyInRow)
			totalWeight += config.shopWeight;

		float roll = (float)(rng.NextDouble() * totalWeight);

		roll -= config.battleMinionWeight;
		if (roll <= 0) return EncounterType.BattleMinion;

		roll -= config.eventWeight;
		if (roll <= 0) return EncounterType.Event;

		roll -= config.campWeight;
		if (roll <= 0) return EncounterType.Camp;

		if (!shopAlreadyInRow)
			return EncounterType.Shop;

		return EncounterType.BattleMinion;
	}

	static void GenerateConnections(MapData map, MapConfig config, System.Random rng)
	{
		int lastRow = map.totalRows - 1;

		for (int row = 0; row < lastRow; row++)
		{
			List<MapNodeData> currentRowNodes = map.GetNodesInRow(row);
			List<MapNodeData> nextRowNodes = map.GetNodesInRow(row + 1);

			if (nextRowNodes.Count == 0)
				continue;

			for (int i = 0; i < currentRowNodes.Count; i++)
			{
				MapNodeData node = currentRowNodes[i];
				bool connected = false;

				// Primary connection: same lane in next row
				MapNodeData sameLaneNext = FindNodeInLane(nextRowNodes, node.lane);
				if (sameLaneNext != null)
				{
					node.connectedNodeIds.Add(sameLaneNext.nodeId);
					connected = true;
				}

				// Secondary connection: adjacent lane in next row
				if (rng.NextDouble() < config.adjacentConnectionChance || !connected)
				{
					MapNodeData adjacent = FindNearestAdjacentNode(nextRowNodes, node.lane, sameLaneNext);
					if (adjacent != null)
					{
						node.connectedNodeIds.Add(adjacent.nodeId);
						connected = true;
					}
				}

				// Fallback: if still not connected, connect to nearest node in next row
				if (!connected && nextRowNodes.Count > 0)
				{
					MapNodeData nearest = FindNearestNode(nextRowNodes, node.lane);
					if (nearest != null)
						node.connectedNodeIds.Add(nearest.nodeId);
				}
			}
		}
	}

	static MapNodeData FindNodeInLane(List<MapNodeData> nodes, int lane)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].lane == lane)
				return nodes[i];
		}
		return null;
	}

	static MapNodeData FindNearestAdjacentNode(List<MapNodeData> nextRowNodes, int currentLane, MapNodeData exclude)
	{
		MapNodeData best = null;
		int bestDist = int.MaxValue;

		for (int i = 0; i < nextRowNodes.Count; i++)
		{
			MapNodeData candidate = nextRowNodes[i];
			if (candidate == exclude) continue;
			if (candidate.lane == currentLane) continue;

			int dist = Mathf.Abs(candidate.lane - currentLane);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = candidate;
			}
		}
		return best;
	}

	static MapNodeData FindNearestNode(List<MapNodeData> nodes, int lane)
	{
		MapNodeData best = null;
		int bestDist = int.MaxValue;

		for (int i = 0; i < nodes.Count; i++)
		{
			int dist = Mathf.Abs(nodes[i].lane - lane);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = nodes[i];
			}
		}
		return best;
	}

	static void EnforceNoCrossing(MapData map)
	{
		int lastRow = map.totalRows - 1;

		for (int row = 0; row < lastRow; row++)
		{
			List<MapNodeData> currentRowNodes = map.GetNodesInRow(row);
			currentRowNodes.Sort((a, b) => a.lane.CompareTo(b.lane));

			// Collect all edges: (sourceLane, targetLane, sourceNode, targetNodeId)
			List<(int srcLane, int dstLane, MapNodeData src, string dstId)> edges = new();

			for (int i = 0; i < currentRowNodes.Count; i++)
			{
				MapNodeData src = currentRowNodes[i];
				for (int j = 0; j < src.connectedNodeIds.Count; j++)
				{
					MapNodeData dst = map.GetNode(src.connectedNodeIds[j]);
					if (dst != null && dst.row == row + 1)
					{
						edges.Add((src.lane, dst.lane, src, dst.nodeId));
					}
				}
			}

			// Sort edges by source lane, then by target lane
			edges.Sort((a, b) =>
			{
				int cmp = a.srcLane.CompareTo(b.srcLane);
				if (cmp != 0) return cmp;
				return a.dstLane.CompareTo(b.dstLane);
			});

			// Check for crossings: two edges cross if src1 < src2 but dst1 > dst2
			for (int i = 0; i < edges.Count; i++)
			{
				for (int j = i + 1; j < edges.Count; j++)
				{
					if (edges[i].srcLane <= edges[j].srcLane && edges[i].dstLane > edges[j].dstLane)
					{
						// Crossing detected: remove the secondary connection (the one farther from same-lane)
						var edgeToRemove = edges[j];
						if (Mathf.Abs(edges[i].srcLane - edges[i].dstLane) > Mathf.Abs(edges[j].srcLane - edges[j].dstLane))
							edgeToRemove = edges[i];

						edgeToRemove.src.connectedNodeIds.Remove(edgeToRemove.dstId);
						edges.RemoveAt(edges.IndexOf(edgeToRemove));
						j--;
					}
				}
			}
		}
	}

	static void VerifyAndPatchReachability(MapData map)
	{
		int lastRow = map.totalRows - 1;
		List<MapNodeData> bossNodes = map.GetNodesInRow(lastRow);
		if (bossNodes.Count == 0) return;

		string bossId = bossNodes[0].nodeId;

		// BFS from each starting node to verify it can reach the boss
		for (int s = 0; s < map.startingNodeIds.Count; s++)
		{
			if (CanReach(map, map.startingNodeIds[s], bossId))
				continue;

			// Path is broken: walk forward from this start and patch
			PatchPathForward(map, map.startingNodeIds[s]);
		}

		// Also verify all nodes in second-to-last row connect to boss
		List<MapNodeData> preBossRow = map.GetNodesInRow(lastRow - 1);
		for (int i = 0; i < preBossRow.Count; i++)
		{
			if (!preBossRow[i].connectedNodeIds.Contains(bossId))
			{
				preBossRow[i].connectedNodeIds.Add(bossId);
			}
		}
	}

	static bool CanReach(MapData map, string fromId, string toId)
	{
		HashSet<string> visited = new();
		Queue<string> queue = new();
		queue.Enqueue(fromId);
		visited.Add(fromId);

		while (queue.Count > 0)
		{
			string currentId = queue.Dequeue();
			if (currentId == toId) return true;

			MapNodeData current = map.GetNode(currentId);
			if (current == null) continue;

			for (int i = 0; i < current.connectedNodeIds.Count; i++)
			{
				string nextId = current.connectedNodeIds[i];
				if (!visited.Contains(nextId))
				{
					visited.Add(nextId);
					queue.Enqueue(nextId);
				}
			}
		}
		return false;
	}

	static void PatchPathForward(MapData map, string startId)
	{
		MapNodeData current = map.GetNode(startId);
		if (current == null) return;

		int maxRow = map.totalRows - 1;

		while (current.row < maxRow)
		{
			if (current.connectedNodeIds.Count > 0)
			{
				// Follow first connection
				current = map.GetNode(current.connectedNodeIds[0]);
				if (current == null) return;
				continue;
			}

			// No connections: find nearest node in next row and connect
			List<MapNodeData> nextRow = map.GetNodesInRow(current.row + 1);
			if (nextRow.Count == 0)
			{
				// No nodes in next row at all: create one
				MapNodeData bridge = CreateNode(current.row + 1, current.lane, EncounterType.BattleMinion, null);
				map.nodes.Add(bridge);
				current.connectedNodeIds.Add(bridge.nodeId);
				current = bridge;
			}
			else
			{
				MapNodeData nearest = FindNearestNode(nextRow, current.lane);
				if (nearest != null)
				{
					current.connectedNodeIds.Add(nearest.nodeId);
					current = nearest;
				}
				else
				{
					break;
				}
			}
		}
	}

	static void FixConsecutiveBattles(MapData map, System.Random rng, MapConfig config)
	{
		// For each starting node, walk all paths and check for 3+ consecutive battles
		for (int s = 0; s < map.startingNodeIds.Count; s++)
		{
			CheckPathForConsecutiveBattles(map, map.startingNodeIds[s], 0, rng, config);
		}
	}

	static void CheckPathForConsecutiveBattles(MapData map, string nodeId, int consecutiveBattles, System.Random rng, MapConfig config)
	{
		MapNodeData node = map.GetNode(nodeId);
		if (node == null) return;

		bool isBattle = node.encounterType == EncounterType.BattleMinion;
		int newCount = isBattle ? consecutiveBattles + 1 : 0;

		// If 3+ consecutive battles, swap this one to Event or Camp
		if (newCount >= 3)
		{
			node.encounterType = rng.NextDouble() < 0.6 ? EncounterType.Event : EncounterType.Camp;
			newCount = 0;
		}

		for (int i = 0; i < node.connectedNodeIds.Count; i++)
		{
			CheckPathForConsecutiveBattles(map, node.connectedNodeIds[i], newCount, rng, config);
		}
	}

	static void CleanBackwardConnections(MapData map)
	{
		for (int i = 0; i < map.nodes.Count; i++)
		{
			MapNodeData node = map.nodes[i];
			for (int j = node.connectedNodeIds.Count - 1; j >= 0; j--)
			{
				MapNodeData target = map.GetNode(node.connectedNodeIds[j]);
				// Remove if target doesn't exist, is in same row, or is in a previous row
				if (target == null || target.row <= node.row)
				{
					node.connectedNodeIds.RemoveAt(j);
				}
			}

			// Remove duplicate connections
			HashSet<string> seen = new();
			for (int j = node.connectedNodeIds.Count - 1; j >= 0; j--)
			{
				if (!seen.Add(node.connectedNodeIds[j]))
					node.connectedNodeIds.RemoveAt(j);
			}
		}
	}
}
