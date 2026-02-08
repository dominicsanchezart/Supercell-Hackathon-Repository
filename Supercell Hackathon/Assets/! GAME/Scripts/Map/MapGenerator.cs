using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StS-inspired map generator using path tracing.
/// 1. Define a grid of potential node positions (columns × rows)
/// 2. Trace N independent paths from row 0 to the boss row
/// 3. Only nodes touched by at least one path survive
/// 4. Assign encounter types AFTER the graph structure is built
/// 5. Apply constraint rules (no consecutive same-type, floor restrictions, etc.)
/// </summary>
public static class MapGenerator
{
	// ─────────────────────────────────────────────
	// PUBLIC API (unchanged signature)
	// ─────────────────────────────────────────────

	public static MapData Generate(MapConfig config, int seed)
	{
		System.Random rng = new System.Random(seed);

		int rows = config.totalRows;
		int cols = config.columns;
		int lastRow = rows - 1;

		// Fixed starting columns: always exactly 3, evenly spaced across grid
		// e.g. for 5 cols: [1, 2, 3] — centered, skipping edges
		int[] startCols = GetFixedStartColumns(cols);

		// Phase 1: Trace paths through the grid
		HashSet<long> aliveCells = new();
		Dictionary<long, HashSet<long>> edges = new();

		// Pre-place the 3 starting cells so they always exist
		for (int i = 0; i < startCols.Length; i++)
			aliveCells.Add(CellKey(0, startCols[i]));

		for (int p = 0; p < config.pathCount; p++)
		{
			TracePath(rng, config, p, rows, cols, lastRow, aliveCells, edges, startCols);
		}

		// Prune any row-0 cells that aren't in the fixed 3 (shouldn't happen, but safety)
		HashSet<long> validStarts = new();
		for (int i = 0; i < startCols.Length; i++)
			validStarts.Add(CellKey(0, startCols[i]));
		aliveCells.RemoveWhere(key => (int)(key >> 16) == 0 && !validStarts.Contains(key));

		// Phase 2: Build MapData from alive cells and edges
		MapData map = new MapData();
		map.totalRows = rows;

		Dictionary<long, MapNodeData> cellToNode = new();

		// Sort alive cells so node order is deterministic (row then col)
		List<long> sortedCells = new(aliveCells);
		sortedCells.Sort();

		for (int i = 0; i < sortedCells.Count; i++)
		{
			long cellKey = sortedCells[i];
			int row = (int)(cellKey >> 16);
			int col = (int)(cellKey & 0xFFFF);

			// Jitter for organic feel (not on first row or boss row)
			float ox = 0f, oy = 0f;
			if (row > 0 && row < lastRow)
			{
				ox = ((float)rng.NextDouble() - 0.5f) * 0.7f;
				oy = ((float)rng.NextDouble() - 0.5f) * 0.4f;
			}

			MapNodeData node = new MapNodeData
			{
				nodeId = $"r{row}_c{col}",
				row = row,
				lane = col,
				encounterType = EncounterType.BattleMinion, // placeholder, assigned in Phase 3
				isCompleted = false,
				isAvailable = false,
				connectedNodeIds = new List<string>(),
				offsetX = ox,
				offsetY = oy
			};

			cellToNode[cellKey] = node;
			map.nodes.Add(node);

			if (row == 0)
				map.startingNodeIds.Add(node.nodeId);
		}

		// Wire up connections from edges
		foreach (var kvp in edges)
		{
			if (!cellToNode.TryGetValue(kvp.Key, out MapNodeData sourceNode))
				continue;

			foreach (long destKey in kvp.Value)
			{
				if (cellToNode.TryGetValue(destKey, out MapNodeData destNode))
				{
					if (!sourceNode.connectedNodeIds.Contains(destNode.nodeId))
						sourceNode.connectedNodeIds.Add(destNode.nodeId);
				}
			}
		}

		// Ensure all pre-boss nodes connect to the boss
		EnsureBossConnections(map, lastRow);

		// Phase 3: Assign encounter types based on floor rules
		AssignEncounterTypes(map, config, rng, lastRow);

		// Phase 4: Apply constraint rules
		EnforceConstraints(map, config, rng, lastRow);

		// Phase 5: Set initial availability
		map.UpdateAvailability();

		return map;
	}

	// ─────────────────────────────────────────────
	// PHASE 1: PATH TRACING
	// ─────────────────────────────────────────────

	static long CellKey(int row, int col) => ((long)row << 16) | (long)(ushort)col;

	/// <summary>
	/// Returns exactly 3 adjacent columns in the center of the grid.
	/// For 5 cols: [1, 2, 3]. For 7 cols: [2, 3, 4]. Always the middle 3.
	/// </summary>
	static int[] GetFixedStartColumns(int cols)
	{
		int mid = cols / 2;
		return new int[] { mid - 1, mid, mid + 1 };
	}

	static void TracePath(
		System.Random rng, MapConfig config, int pathIndex,
		int rows, int cols, int lastRow,
		HashSet<long> aliveCells,
		Dictionary<long, HashSet<long>> edges,
		int[] fixedStartCols)
	{
		// Pick starting column: always one of the 3 fixed starts
		int startCol;
		if (pathIndex < fixedStartCols.Length)
		{
			// First 3 paths each get a unique fixed start
			startCol = fixedStartCols[pathIndex];
		}
		else
		{
			// Additional paths pick randomly from the 3 fixed starts
			startCol = fixedStartCols[rng.Next(fixedStartCols.Length)];
		}

		int currentCol = startCol;
		long currentKey = CellKey(0, currentCol);
		aliveCells.Add(currentKey);

		float centerCol = (cols - 1) * 0.5f;

		// Trace upward from row 0 to lastRow
		for (int row = 1; row <= lastRow; row++)
		{
			int nextCol;

			if (row == lastRow)
			{
				// Boss row: always go to the middle column
				nextCol = cols / 2;
			}
			else
			{
				// Calculate center bias: increases as we approach the top
				// 0.0 at bottom rows → ~0.35 at the top rows
				float progress = (float)row / lastRow;
				float centerBias = progress * 0.35f;

				nextCol = PickNextColumn(rng, currentCol, cols, row, aliveCells, edges, currentKey, centerCol, centerBias);
			}

			long nextKey = CellKey(row, nextCol);
			aliveCells.Add(nextKey);

			if (!edges.ContainsKey(currentKey))
				edges[currentKey] = new HashSet<long>();
			edges[currentKey].Add(nextKey);

			currentCol = nextCol;
			currentKey = nextKey;
		}
	}

	static int PickNextColumn(
		System.Random rng, int currentCol, int cols,
		int nextRow, HashSet<long> aliveCells,
		Dictionary<long, HashSet<long>> edges, long sourceKey,
		float centerCol, float centerBias)
	{
		// Candidates: current col, col-1, col+1 (clamped to grid)
		List<int> candidates = new();
		for (int dc = -1; dc <= 1; dc++)
		{
			int c = currentCol + dc;
			if (c >= 0 && c < cols)
				candidates.Add(c);
		}

		// Sort candidates by distance to center (closest first), with randomness
		// centerBias controls how strongly we prefer center (0 = pure random, 1 = always center)
		candidates.Sort((a, b) =>
		{
			float distA = Mathf.Abs(a - centerCol);
			float distB = Mathf.Abs(b - centerCol);
			return distA.CompareTo(distB);
		});

		// With (1 - centerBias) probability, shuffle to add randomness
		if ((float)rng.NextDouble() > centerBias)
		{
			// Shuffle for randomness
			for (int i = candidates.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(candidates[i], candidates[j]) = (candidates[j], candidates[i]);
			}
		}

		// Pick first candidate that doesn't cause a crossing
		int sourceRow = (int)(sourceKey >> 16);

		foreach (int c in candidates)
		{
			if (!WouldCross(currentCol, c, sourceRow, edges, aliveCells, nextRow))
				return c;
		}

		// Fallback: straight up
		return Mathf.Clamp(currentCol, 0, cols - 1);
	}

	/// <summary>
	/// Checks if connecting (sourceCol → destCol) on rows (sourceRow → sourceRow+1)
	/// would cross any existing edge in the same row transition.
	/// Two edges cross if: src1.col < src2.col but dst1.col > dst2.col (or vice versa).
	/// </summary>
	static bool WouldCross(
		int srcCol, int dstCol, int srcRow,
		Dictionary<long, HashSet<long>> edges,
		HashSet<long> aliveCells, int dstRow)
	{
		// Check all existing edges from srcRow to dstRow
		// We need to iterate all alive cells in srcRow that have edges to dstRow
		foreach (var kvp in edges)
		{
			int existingSrcRow = (int)(kvp.Key >> 16);
			if (existingSrcRow != srcRow) continue;

			int existingSrcCol = (int)(kvp.Key & 0xFFFF);

			foreach (long destKey in kvp.Value)
			{
				int existingDstRow = (int)(destKey >> 16);
				if (existingDstRow != dstRow) continue;

				int existingDstCol = (int)(destKey & 0xFFFF);

				// Check for crossing: one goes left-to-right while other goes right-to-left
				if (existingSrcCol < srcCol && existingDstCol > dstCol) return true;
				if (existingSrcCol > srcCol && existingDstCol < dstCol) return true;

				// Also prevent two edges from sharing the same destination if they came from different
				// sides (this creates visual overlaps, not strictly "crossing" but looks bad)
				// Actually this is fine — shared nodes are intentional merges
			}
		}

		return false;
	}

	// ─────────────────────────────────────────────
	// PHASE 2 HELPERS: BOSS CONNECTIONS
	// ─────────────────────────────────────────────

	static void EnsureBossConnections(MapData map, int lastRow)
	{
		List<MapNodeData> bossNodes = map.GetNodesInRow(lastRow);
		if (bossNodes.Count == 0) return;

		string bossId = bossNodes[0].nodeId;

		List<MapNodeData> preBossRow = map.GetNodesInRow(lastRow - 1);
		for (int i = 0; i < preBossRow.Count; i++)
		{
			if (!preBossRow[i].connectedNodeIds.Contains(bossId))
				preBossRow[i].connectedNodeIds.Add(bossId);
		}
	}

	// ─────────────────────────────────────────────
	// PHASE 3: ASSIGN ENCOUNTER TYPES
	// ─────────────────────────────────────────────

	static void AssignEncounterTypes(MapData map, MapConfig config, System.Random rng, int lastRow)
	{
		// Build set of guaranteed camp rows
		HashSet<int> campRows = new();
		if (config.guaranteedCampRows != null)
		{
			for (int i = 0; i < config.guaranteedCampRows.Length; i++)
				campRows.Add(config.guaranteedCampRows[i]);
		}
		// Camp rows before boss
		for (int r = lastRow - config.campRowsBeforeBoss; r < lastRow; r++)
			campRows.Add(r);

		// Build set of guaranteed treasure rows
		HashSet<int> treasureRows = new();
		if (config.guaranteedTreasureRows != null)
		{
			for (int i = 0; i < config.guaranteedTreasureRows.Length; i++)
				treasureRows.Add(config.guaranteedTreasureRows[i]);
		}

		// Assign per-row
		for (int row = 0; row <= lastRow; row++)
		{
			List<MapNodeData> rowNodes = map.GetNodesInRow(row);

			if (row == 0)
			{
				// Floor 0: always BattleMinion
				for (int i = 0; i < rowNodes.Count; i++)
					rowNodes[i].encounterType = EncounterType.BattleMinion;
			}
			else if (row == lastRow)
			{
				// Boss floor
				for (int i = 0; i < rowNodes.Count; i++)
					rowNodes[i].encounterType = EncounterType.BattleBoss;
			}
			else if (campRows.Contains(row))
			{
				// Guaranteed camp floors
				for (int i = 0; i < rowNodes.Count; i++)
					rowNodes[i].encounterType = EncounterType.Camp;
			}
			else if (treasureRows.Contains(row))
			{
				// Guaranteed treasure floors
				for (int i = 0; i < rowNodes.Count; i++)
					rowNodes[i].encounterType = EncounterType.Treasure;
			}
			else if (row <= 1)
			{
				// Early floors: BattleMinion only
				for (int i = 0; i < rowNodes.Count; i++)
					rowNodes[i].encounterType = EncounterType.BattleMinion;
			}
			else
			{
				// Open floors: weighted roll with floor restrictions
				bool isEarlyFloor = row < config.minRowForAdvancedTypes;
				int shopsThisRow = 0;

				for (int i = 0; i < rowNodes.Count; i++)
				{
					bool shopAllowed = shopsThisRow < config.maxShopsPerRow;
					rowNodes[i].encounterType = RollEncounterType(rng, config, isEarlyFloor, shopAllowed);
					if (rowNodes[i].encounterType == EncounterType.Shop)
						shopsThisRow++;
				}
			}
		}
	}

	static EncounterType RollEncounterType(System.Random rng, MapConfig config, bool isEarlyFloor, bool shopAllowed)
	{
		// Build weight table based on restrictions
		float battleW = config.battleMinionWeight;
		float eventW = config.eventWeight;
		float campW = isEarlyFloor ? 0f : config.campWeight;
		float shopW = (isEarlyFloor || !shopAllowed) ? 0f : config.shopWeight;
		float eliteW = isEarlyFloor ? 0f : config.eliteWeight;
		float treasureW = isEarlyFloor ? 0f : config.treasureWeight;

		float total = battleW + eventW + campW + shopW + eliteW + treasureW;
		float roll = (float)(rng.NextDouble() * total);

		roll -= battleW;
		if (roll <= 0) return EncounterType.BattleMinion;

		roll -= eventW;
		if (roll <= 0) return EncounterType.Event;

		roll -= campW;
		if (roll <= 0) return EncounterType.Camp;

		roll -= shopW;
		if (roll <= 0) return EncounterType.Shop;

		roll -= treasureW;
		if (roll <= 0) return EncounterType.Treasure;

		roll -= eliteW;
		if (roll <= 0) return EncounterType.BattleMinion; // elite mapped to BattleMinion for now (no Elite enum)

		return EncounterType.BattleMinion;
	}

	// ─────────────────────────────────────────────
	// PHASE 4: ENFORCE CONSTRAINTS
	// ─────────────────────────────────────────────

	static void EnforceConstraints(MapData map, MapConfig config, System.Random rng, int lastRow)
	{
		// Constraint 1: No consecutive same-type (except BattleMinion and BattleBoss)
		// Walk each path from starting nodes and check
		FixConsecutiveSameType(map, config, rng);

		// Constraint 2: If a node has 2+ outgoing connections, try to make destinations different types
		DiversifyBranches(map, rng, config);

		// Constraint 3: No camp→camp connections (extra safety beyond same-type rule)
		FixConsecutiveCamps(map, config, rng);
	}

	static void FixConsecutiveSameType(MapData map, MapConfig config, System.Random rng)
	{
		// For each node, check all children: if the child is the same non-battle type,
		// re-roll the child's type
		int lastRow = map.totalRows - 1;

		for (int row = 0; row < lastRow; row++)
		{
			List<MapNodeData> rowNodes = map.GetNodesInRow(row);
			for (int i = 0; i < rowNodes.Count; i++)
			{
				MapNodeData parent = rowNodes[i];
				if (parent.encounterType == EncounterType.BattleMinion) continue;
				if (parent.encounterType == EncounterType.BattleBoss) continue;

				for (int j = 0; j < parent.connectedNodeIds.Count; j++)
				{
					MapNodeData child = map.GetNode(parent.connectedNodeIds[j]);
					if (child == null) continue;
					if (child.encounterType == EncounterType.BattleBoss) continue;

					// Check if forced row — don't change guaranteed types
					if (IsGuaranteedRow(child.row, config, lastRow)) continue;

					if (child.encounterType == parent.encounterType)
					{
						// Re-roll the child to something different
						child.encounterType = GetDifferentType(rng, config, parent.encounterType, child.row, lastRow);
					}
				}
			}
		}

		// Also enforce max consecutive battles along paths
		for (int s = 0; s < map.startingNodeIds.Count; s++)
		{
			FixConsecutiveBattlesAlongPath(map, map.startingNodeIds[s], 0, config, rng, new HashSet<string>());
		}
	}

	static void FixConsecutiveBattlesAlongPath(MapData map, string nodeId, int streak, MapConfig config, System.Random rng, HashSet<string> visited)
	{
		if (!visited.Add(nodeId)) return;

		MapNodeData node = map.GetNode(nodeId);
		if (node == null) return;

		int lastRow = map.totalRows - 1;
		bool isBattle = node.encounterType == EncounterType.BattleMinion;
		int newStreak = isBattle ? streak + 1 : 0;

		if (newStreak >= config.maxConsecutiveBattles && !IsGuaranteedRow(node.row, config, lastRow))
		{
			node.encounterType = rng.NextDouble() < 0.5 ? EncounterType.Shop : EncounterType.Treasure;
			newStreak = 0;
		}

		for (int i = 0; i < node.connectedNodeIds.Count; i++)
			FixConsecutiveBattlesAlongPath(map, node.connectedNodeIds[i], newStreak, config, rng, visited);
	}

	static void DiversifyBranches(MapData map, System.Random rng, MapConfig config)
	{
		int lastRow = map.totalRows - 1;

		for (int i = 0; i < map.nodes.Count; i++)
		{
			MapNodeData node = map.nodes[i];
			if (node.connectedNodeIds.Count < 2) continue;

			// Collect children types
			List<MapNodeData> children = new();
			for (int j = 0; j < node.connectedNodeIds.Count; j++)
			{
				MapNodeData child = map.GetNode(node.connectedNodeIds[j]);
				if (child != null) children.Add(child);
			}

			// If all children same type and more than 1, diversify
			HashSet<EncounterType> types = new();
			for (int j = 0; j < children.Count; j++)
				types.Add(children[j].encounterType);

			if (types.Count < children.Count && children.Count > 1)
			{
				for (int j = 1; j < children.Count; j++)
				{
					if (IsGuaranteedRow(children[j].row, config, lastRow)) continue;
					if (children[j].encounterType == EncounterType.BattleBoss) continue;

					// Check if this type is already used by a sibling
					bool duplicate = false;
					for (int k = 0; k < j; k++)
					{
						if (children[k].encounterType == children[j].encounterType)
						{
							duplicate = true;
							break;
						}
					}

					if (duplicate)
					{
						children[j].encounterType = GetDifferentType(rng, config, children[j].encounterType, children[j].row, lastRow);
					}
				}
			}
		}
	}

	static void FixConsecutiveCamps(MapData map, MapConfig config, System.Random rng)
	{
		int lastRow = map.totalRows - 1;

		for (int row = 0; row < map.totalRows; row++)
		{
			List<MapNodeData> rowNodes = map.GetNodesInRow(row);
			for (int i = 0; i < rowNodes.Count; i++)
			{
				MapNodeData node = rowNodes[i];
				if (node.encounterType != EncounterType.Camp) continue;

				for (int j = 0; j < node.connectedNodeIds.Count; j++)
				{
					MapNodeData child = map.GetNode(node.connectedNodeIds[j]);
					if (child == null) continue;
					if (child.encounterType == EncounterType.BattleBoss) continue;

					// Never overwrite guaranteed camp rows
					if (IsGuaranteedRow(child.row, config, lastRow)) continue;

					if (child.encounterType == EncounterType.Camp)
					{
						child.encounterType = EncounterType.BattleMinion;
					}
				}
			}
		}
	}

	// ─────────────────────────────────────────────
	// HELPERS
	// ─────────────────────────────────────────────

	static bool IsGuaranteedRow(int row, MapConfig config, int lastRow)
	{
		if (row == 0) return true;
		if (row == lastRow) return true;

		// Camp rows before boss
		if (row >= lastRow - config.campRowsBeforeBoss) return true;

		// Guaranteed camp rows
		if (config.guaranteedCampRows != null)
		{
			for (int i = 0; i < config.guaranteedCampRows.Length; i++)
			{
				if (config.guaranteedCampRows[i] == row) return true;
			}
		}

		// Guaranteed treasure rows
		if (config.guaranteedTreasureRows != null)
		{
			for (int i = 0; i < config.guaranteedTreasureRows.Length; i++)
			{
				if (config.guaranteedTreasureRows[i] == row) return true;
			}
		}

		return false;
	}

	static EncounterType GetDifferentType(System.Random rng, MapConfig config, EncounterType avoid, int row, int lastRow)
	{
		bool isEarly = row < config.minRowForAdvancedTypes;

		List<EncounterType> options = new() { EncounterType.BattleMinion };

		if (!isEarly)
		{
			options.Add(EncounterType.Camp);
			options.Add(EncounterType.Shop);
			options.Add(EncounterType.Treasure);
		}

		// Remove the type we want to avoid
		options.Remove(avoid);

		if (options.Count == 0) return EncounterType.BattleMinion;
		return options[rng.Next(options.Count)];
	}
}
