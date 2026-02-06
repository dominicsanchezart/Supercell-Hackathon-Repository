using System.Collections.Generic;
using UnityEngine;

public class MapView : MonoBehaviour
{
	[Header("References")]
	public Transform nodeContainer;
	public Transform lineContainer;
	public GameObject nodeViewPrefab;
	public GameObject linePrefab;
	public MapIconSet iconSet;

	[Header("Layout")]
	public float rowSpacing = 1.5f;
	public float laneSpacing = 2.0f;
	public float mapCenterX = 0f;

	[Header("Scrolling")]
	public float scrollSpeed = 5f;
	public float scrollSmooth = 8f;
	public float scrollDragSpeed = 0.01f;

	MapData mapData;
	readonly Dictionary<string, MapNodeView> nodeViews = new();
	readonly List<MapLineRenderer> lines = new();

	float targetScrollY;
	float currentScrollY;
	float minScrollY;
	float maxScrollY;

	bool isDragging;
	float lastDragY;

	public void Initialize(MapData data)
	{
		mapData = data;

		ClearExisting();
		SpawnNodes();
		SpawnLines();
		RefreshAvailability();

		// Calculate scroll bounds
		float mapHeight = mapData.totalRows * rowSpacing;
		maxScrollY = 0f;
		minScrollY = -(mapHeight) + 5f;

		// Start scroll at bottom (showing row 0)
		targetScrollY = maxScrollY;
		currentScrollY = maxScrollY;
		ApplyScroll();

		// If player has progressed, scroll to their position
		if (!string.IsNullOrEmpty(mapData.currentNodeId))
		{
			MapNodeData currentNode = mapData.GetNode(mapData.currentNodeId);
			if (currentNode != null)
				ScrollToRow(currentNode.row);
		}
	}

	void ClearExisting()
	{
		foreach (var kvp in nodeViews)
		{
			if (kvp.Value != null)
				Destroy(kvp.Value.gameObject);
		}
		nodeViews.Clear();

		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i] != null)
				Destroy(lines[i].gameObject);
		}
		lines.Clear();
	}

	void SpawnNodes()
	{
		if (nodeViewPrefab == null || iconSet == null) return;

		for (int i = 0; i < mapData.nodes.Count; i++)
		{
			MapNodeData nodeData = mapData.nodes[i];
			Vector3 localPos = GetNodeLocalPosition(nodeData);

			GameObject obj = Instantiate(nodeViewPrefab, nodeContainer);
			obj.transform.localPosition = localPos;
			obj.name = $"Node_{nodeData.nodeId}";

			MapNodeView view = obj.GetComponent<MapNodeView>();
			if (view != null)
			{
				view.owner = this;
				Sprite icon = iconSet.GetIcon(nodeData.encounterType);
				view.Initialize(nodeData, icon, iconSet.nodeBackground);
				nodeViews[nodeData.nodeId] = view;
			}
		}
	}

	void SpawnLines()
	{
		if (linePrefab == null) return;

		for (int i = 0; i < mapData.nodes.Count; i++)
		{
			MapNodeData sourceNode = mapData.nodes[i];

			for (int j = 0; j < sourceNode.connectedNodeIds.Count; j++)
			{
				string targetId = sourceNode.connectedNodeIds[j];
				MapNodeData targetNode = mapData.GetNode(targetId);
				if (targetNode == null) continue;

				// Lines use local space (useWorldSpace = false)
				// so positions are relative to lineContainer
				Vector3 startPos = GetNodeLocalPosition(sourceNode);
				Vector3 endPos = GetNodeLocalPosition(targetNode);

				GameObject lineObj = Instantiate(linePrefab, lineContainer);
				lineObj.name = $"Line_{sourceNode.nodeId}_to_{targetId}";

				MapLineRenderer lineRenderer = lineObj.GetComponent<MapLineRenderer>();
				if (lineRenderer != null)
				{
					bool isActive = IsConnectionOnAvailablePath(sourceNode, targetNode);
					lineRenderer.Initialize(startPos, endPos, isActive);
					lines.Add(lineRenderer);
				}
			}
		}
	}

	Vector3 GetNodeLocalPosition(MapNodeData node)
	{
		// Lane 1 = center, lane 0 = left, lane 2 = right
		// Sub-lanes: -1 = far left, 3 = far right
		float x = mapCenterX + (node.lane - 1) * laneSpacing + node.offsetX;
		float y = node.row * rowSpacing + node.offsetY;
		return new Vector3(x, y, 0f);
	}

	public void RefreshAvailability()
	{
		if (mapData == null) return;

		mapData.UpdateAvailability();

		foreach (var kvp in nodeViews)
		{
			MapNodeData nodeData = mapData.GetNode(kvp.Key);
			if (nodeData != null)
			{
				kvp.Value.SetAvailability(nodeData.isAvailable, nodeData.isCompleted);
			}
		}
	}

	bool IsConnectionOnAvailablePath(MapNodeData source, MapNodeData target)
	{
		// A connection is "active" if source is completed and target is available,
		// or if source is available (pre-first-move starting connections)
		if (source.isCompleted && target.isAvailable) return true;
		if (source.isAvailable && !source.isCompleted) return true;
		return false;
	}

	public void OnNodeClicked(MapNodeView nodeView)
	{
		MapNodeData data = nodeView.GetNodeData();
		if (data == null || !data.isAvailable) return;

		if (RunManager.Instance != null)
		{
			RunManager.Instance.OnEncounterSelected(data);
		}
		else
		{
			// Debug mode: just mark as completed and refresh
			Debug.Log($"Node selected: {data.nodeId} ({data.encounterType})");
			data.isCompleted = true;
			mapData.currentNodeId = data.nodeId;
			RefreshAvailability();
			RebuildLines();
		}
	}

	void RebuildLines()
	{
		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i] != null)
				Destroy(lines[i].gameObject);
		}
		lines.Clear();
		SpawnLines();
	}

	void Update()
	{
		HandleScrollInput();
		SmoothScroll();
	}

	void HandleScrollInput()
	{
		// Mouse wheel scroll — scroll up (positive delta) moves the map DOWN
		// to reveal higher rows, which means subtracting from Y offset
		float scroll = Input.mouseScrollDelta.y;
		if (Mathf.Abs(scroll) > 0.01f)
		{
			targetScrollY -= scroll * scrollSpeed;
			targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);
		}

		// Mouse drag scroll
		if (Input.GetMouseButtonDown(0) && !IsOverNode())
		{
			isDragging = true;
			lastDragY = Input.mousePosition.y;
		}

		if (Input.GetMouseButtonUp(0))
		{
			isDragging = false;
		}

		if (isDragging)
		{
			float dragDelta = Input.mousePosition.y - lastDragY;
			targetScrollY += dragDelta * scrollDragSpeed;
			targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);
			lastDragY = Input.mousePosition.y;
		}
	}

	bool IsOverNode()
	{
		// Simple check: cast ray from mouse to see if we hit a node collider
		Camera cam = Camera.main;
		if (cam == null) return false;

		Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
		return hit.collider != null;
	}

	void SmoothScroll()
	{
		currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, Time.deltaTime * scrollSmooth);
		ApplyScroll();
	}

	void ApplyScroll()
	{
		if (nodeContainer != null)
			nodeContainer.localPosition = new Vector3(0f, currentScrollY, 0f);
		if (lineContainer != null)
			lineContainer.localPosition = new Vector3(0f, currentScrollY, 0f);
	}

	public void ScrollToRow(int row)
	{
		targetScrollY = -(row * rowSpacing) + 3f;
		targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);
	}
}
