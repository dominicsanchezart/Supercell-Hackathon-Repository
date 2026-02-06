using System.Collections.Generic;

[System.Serializable]
public class MapNodeData
{
	public string nodeId;
	public int row;
	public int lane;
	public EncounterType encounterType;
	public bool isCompleted;
	public bool isAvailable;
	public List<string> connectedNodeIds = new();

	// Per-node position jitter for organic feel (set during generation)
	public float offsetX;
	public float offsetY;
}
