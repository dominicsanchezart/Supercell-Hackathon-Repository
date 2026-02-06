using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MapData
{
	public List<MapNodeData> nodes = new();
	public int totalRows;
	public string currentNodeId;
	public List<string> startingNodeIds = new();

	public MapNodeData GetNode(string id)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].nodeId == id)
				return nodes[i];
		}
		return null;
	}

	public List<MapNodeData> GetNodesInRow(int row)
	{
		List<MapNodeData> result = new();
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].row == row)
				result.Add(nodes[i]);
		}
		return result;
	}

	public List<MapNodeData> GetConnectedNodes(string nodeId)
	{
		MapNodeData node = GetNode(nodeId);
		if (node == null) return new List<MapNodeData>();

		List<MapNodeData> result = new();
		for (int i = 0; i < node.connectedNodeIds.Count; i++)
		{
			MapNodeData connected = GetNode(node.connectedNodeIds[i]);
			if (connected != null)
				result.Add(connected);
		}
		return result;
	}

	public void UpdateAvailability()
	{
		// Reset all availability
		for (int i = 0; i < nodes.Count; i++)
			nodes[i].isAvailable = false;

		if (string.IsNullOrEmpty(currentNodeId))
		{
			// No node selected yet: starting nodes are available
			for (int i = 0; i < startingNodeIds.Count; i++)
			{
				MapNodeData node = GetNode(startingNodeIds[i]);
				if (node != null && !node.isCompleted)
					node.isAvailable = true;
			}
		}
		else
		{
			// Nodes connected to current node that aren't completed
			MapNodeData current = GetNode(currentNodeId);
			if (current == null) return;

			for (int i = 0; i < current.connectedNodeIds.Count; i++)
			{
				MapNodeData connected = GetNode(current.connectedNodeIds[i]);
				if (connected != null && !connected.isCompleted)
					connected.isAvailable = true;
			}
		}
	}

	// Get all nodes that feed into the given node (for path highlighting)
	public List<string> GetParentNodeIds(string nodeId)
	{
		List<string> parents = new();
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].connectedNodeIds.Contains(nodeId))
				parents.Add(nodes[i].nodeId);
		}
		return parents;
	}
}
