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
	public Camera mapCamera;
	public MapTransition mapTransition;

	[Header("Layout")]
	public float rowSpacing = 1.5f;
	public float laneSpacing = 2.0f;
	public float mapCenterX = 0f;

	[Header("Node Size")]
	[Range(0.2f, 3f)] public float nodeScale = 1f;

	[Header("Scrolling")]
	public float scrollSpeed = 5f;
	public float scrollSmooth = 8f;
	public float scrollDragSpeed = 0.01f;

	[Header("Zoom")]
	[Range(1f, 20f)] public float zoomLevel = 5f;
	public float zoomSpeed = 1f;
	public float zoomMin = 2f;
	public float zoomMax = 15f;

	MapData mapData;
	readonly Dictionary<string, MapNodeView> nodeViews = new();
	readonly List<MapLineRenderer> lines = new();

	// When false, disables node clicks and scroll/zoom input
	[HideInInspector] public bool interactable = true;

	float targetScrollY;
	float currentScrollY;
	float minScrollY;
	float maxScrollY;

	float targetScrollX;
	float currentScrollX;
	float minScrollX;
	float maxScrollX;

	bool isDragging;
	float lastDragY;

	bool isPanning;
	float lastPanX;
	float lastPanY;

	float lastNodeScale;

	public void Initialize(MapData data)
	{
		mapData = data;

		ClearExisting();
		SpawnNodes();
		SpawnLines();
		RefreshAvailability();

		lastNodeScale = nodeScale;

		// Calculate scroll bounds
		float mapHeight = mapData.totalRows * rowSpacing;
		maxScrollY = 0f;
		minScrollY = -(mapHeight) + 5f;

		// Horizontal scroll bounds based on grid width
		float halfWidth = GetColumnCount() * laneSpacing * 0.5f + 2f;
		minScrollX = -halfWidth;
		maxScrollX = halfWidth;
		targetScrollX = 0f;
		currentScrollX = 0f;

		// Start scroll at bottom (showing row 0)
		targetScrollY = maxScrollY;
		currentScrollY = maxScrollY;
		ApplyScroll();

		// Apply initial zoom
		if (mapCamera != null)
			mapCamera.orthographicSize = zoomLevel;

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
			obj.transform.localScale = Vector3.one * nodeScale;
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
		// Center the grid: offset by half the total column count so middle column sits at mapCenterX
		float centerOffset = (mapData != null) ? (mapData.totalRows > 0 ? GetColumnCount() : 1f) : 1f;
		float x = mapCenterX + (node.lane - centerOffset * 0.5f + 0.5f) * laneSpacing + node.offsetX;
		float y = node.row * rowSpacing + node.offsetY;
		return new Vector3(x, y, 0f);
	}

	int GetColumnCount()
	{
		// Determine max column from the actual node data
		int maxCol = 0;
		for (int i = 0; i < mapData.nodes.Count; i++)
		{
			if (mapData.nodes[i].lane > maxCol)
				maxCol = mapData.nodes[i].lane;
		}
		return maxCol + 1;
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
		// Only highlight lines from the current node to its available children
		if (mapData == null || string.IsNullOrEmpty(mapData.currentNodeId))
			return false;

		return source.nodeId == mapData.currentNodeId && target.isAvailable;
	}

	public void OnNodeClicked(MapNodeView nodeView)
	{
		if (!interactable) return;
		if (mapTransition != null && mapTransition.IsTransitioning) return;

		MapNodeData data = nodeView.GetNodeData();
		if (data == null || !data.isAvailable) return;

		// Disable further input immediately
		interactable = false;

		if (mapTransition != null)
		{
			// Play pop + fade, then load scene
			mapTransition.PlayTransition(nodeView.transform, () =>
			{
				if (RunManager.Instance != null)
				{
					RunManager.Instance.OnEncounterSelected(data);
				}
				else
				{
					Debug.Log($"Node selected: {data.nodeId} ({data.encounterType})");
					data.isCompleted = true;
					mapData.currentNodeId = data.nodeId;
					RefreshAvailability();
					RebuildLines();
					interactable = true;
				}
			});
		}
		else
		{
			// No transition — immediate
			if (RunManager.Instance != null)
			{
				RunManager.Instance.OnEncounterSelected(data);
			}
			else
			{
				Debug.Log($"Node selected: {data.nodeId} ({data.encounterType})");
				data.isCompleted = true;
				mapData.currentNodeId = data.nodeId;
				RefreshAvailability();
				RebuildLines();
				interactable = true;
			}
		}
	}

	/// <summary>
	/// Called by MapSceneController when returning from an encounter to fade back in.
	/// </summary>
	public void PlayFadeIn()
	{
		if (mapTransition != null)
			mapTransition.FadeIn();
	}

	public void RebuildLines()
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
		HandleZoomInput();
		SmoothScroll();
		UpdateNodeScaleIfChanged();
	}

	void HandleScrollInput()
	{
		if (!interactable) return;

		float scroll = Input.mouseScrollDelta.y;

		// If holding Ctrl, zoom instead of scroll
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			return;

		// Mouse wheel: vertical scroll
		if (Mathf.Abs(scroll) > 0.01f)
		{
			targetScrollY -= scroll * scrollSpeed;
			targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);
		}

		// Left mouse drag: vertical scroll only
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

		// Middle mouse drag: pan both X and Y
		if (Input.GetMouseButtonDown(2))
		{
			isPanning = true;
			lastPanX = Input.mousePosition.x;
			lastPanY = Input.mousePosition.y;
		}

		if (Input.GetMouseButtonUp(2))
		{
			isPanning = false;
		}

		if (isPanning)
		{
			float panDeltaX = Input.mousePosition.x - lastPanX;
			float panDeltaY = Input.mousePosition.y - lastPanY;

			targetScrollX += panDeltaX * scrollDragSpeed;
			targetScrollY += panDeltaY * scrollDragSpeed;

			targetScrollX = Mathf.Clamp(targetScrollX, minScrollX, maxScrollX);
			targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);

			lastPanX = Input.mousePosition.x;
			lastPanY = Input.mousePosition.y;
		}
	}

	void HandleZoomInput()
	{
		if (!interactable) return;

		// Ctrl + scroll to zoom
		if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
			return;

		float scroll = Input.mouseScrollDelta.y;
		if (Mathf.Abs(scroll) > 0.01f)
		{
			zoomLevel -= scroll * zoomSpeed;
			zoomLevel = Mathf.Clamp(zoomLevel, zoomMin, zoomMax);
		}

		if (mapCamera != null)
			mapCamera.orthographicSize = Mathf.Lerp(mapCamera.orthographicSize, zoomLevel, Time.deltaTime * scrollSmooth);
	}

	void UpdateNodeScaleIfChanged()
	{
		if (Mathf.Approximately(nodeScale, lastNodeScale)) return;

		lastNodeScale = nodeScale;
		foreach (var kvp in nodeViews)
		{
			if (kvp.Value != null)
			{
				kvp.Value.transform.localScale = Vector3.one * nodeScale;
				kvp.Value.UpdateBaseScale(nodeScale);
			}
		}
	}

	bool IsOverNode()
	{
		Camera cam = mapCamera != null ? mapCamera : Camera.main;
		if (cam == null) return false;

		Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
		return hit.collider != null;
	}

	void SmoothScroll()
	{
		currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, Time.deltaTime * scrollSmooth);
		currentScrollX = Mathf.Lerp(currentScrollX, targetScrollX, Time.deltaTime * scrollSmooth);
		ApplyScroll();

		// Smooth zoom
		if (mapCamera != null)
			mapCamera.orthographicSize = Mathf.Lerp(mapCamera.orthographicSize, zoomLevel, Time.deltaTime * scrollSmooth);
	}

	void ApplyScroll()
	{
		if (nodeContainer != null)
			nodeContainer.localPosition = new Vector3(currentScrollX, currentScrollY, 0f);
		if (lineContainer != null)
			lineContainer.localPosition = new Vector3(currentScrollX, currentScrollY, 0f);
	}

	public void ScrollToRow(int row)
	{
		targetScrollY = -(row * rowSpacing) + 3f;
		targetScrollY = Mathf.Clamp(targetScrollY, minScrollY, maxScrollY);
	}
}
