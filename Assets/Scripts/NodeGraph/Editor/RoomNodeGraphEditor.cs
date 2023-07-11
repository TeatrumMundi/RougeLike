using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System;
using System.Collections.Generic;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private GUIStyle roomNodeSelectedStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;
    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    // Node layout values
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 25;

    // Connecting line values of the child parent nodes
    private const float connectingLineWidth = 3f;
    private const float connectingLineArrowSize = 6f;

    [MenuItem("Room Node Graph Editor", menuItem ="Window/Dungeon Editor/Room Node Graph Editor")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    // Define node layout style (background img, text color, padding, border)
    private void OnEnable()
    {
        // Subscribe to the inspector selection changed event
        Selection.selectionChanged += InspectorSelectionChanged;

        // Style for not selected nodes
        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("Assets/Editor/SquareNodeTexture.png") as Texture2D;
        roomNodeStyle.normal.textColor = Color.yellow;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        // Style for selected nodes
        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("Assets/Editor/SquareNodeTextureOnEnable.png") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.yellow;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        //Load Room node types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeListSO;
    }

    private void OnDisable()
    {
        // Unsubscribe from the inspector selection changed event
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    // Open the room node graph editor window if a room node graph scriptable object asset is double clicked in the inspector 
    [OnOpenAsset(0)]
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();

            currentRoomNodeGraph = roomNodeGraph;

            return true;
        }
        return false;
    }

    // Draw Editor Gui
    private void OnGUI()
    {
        // If a scriptable object of type RoomNodeGraphSO has been selected then process
        if (currentRoomNodeGraph != null)
        {
            // Draw line if being dragged
            DrawDraggedLine();

            // Process Events
            ProcessEvents(Event.current);

            // Draw Connections Between Room Nodes
            DrawRoomConnections();

            // Draw Room Nodes
            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }

    
    private void DrawDraggedLine()
    {
        if (currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            //Draw line from node to line position
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.red, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        // Get room node that mouse is over if it's null or noty currently being dragged
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            // Moving to fuction that will tell if mouse is over node
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        // if mouse isn't over a room node or we are currently dragging a line from the room node then process graph events
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }

        // else process room node event
        else
        {
            currentRoomNode.ProcessEvents(currentEvent);
        }
        
    }

    // Function checking if mouse is over room node, if yes return this node if not return null
    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }
        return null;
    }

    // Process Room Node Graph
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            // Process Mouse Button Down Events
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            // Prcoess Mouse Button Up Event
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            // Process Mouse Drag Events
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            default:
                break;
        }
    }

    // Process mouse down events on the room node graph (not over a node)
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // Process right click mouse down on graph event (show context menu)
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
        // Process left mouse down on graph event
        else if (currentEvent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }

    // Process mouse button up event. Only if dragging line and holding right button.
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            // Check if is over another room node
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);
            if (roomNode != null)
            {
                // If yes set this room node as the child of the parent room node if it can be added
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDtoRoomNode(roomNode.id))
                {
                    // Set Parent ID in child room node
                    roomNode.AddParentRoomNodeIDtoRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }

    // Show the context menu
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select ALL Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select ALL Room Nodes"), false, DeleteSelectedRoomNodeLinks);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select ALL Room Nodes"), false, DeleteSelectedRoomNodes);


        menu.ShowAsContext();
    }

    // Process Mouse Drag Event
    private void ProcessMouseDragEvent(Event curretEvent)
    {
        // Prcess right click drag event - draw line
        if (curretEvent.button == 1)
        {
            ProcessRightMouseDragEvent(curretEvent);
        }
    }

    // Process Mouse Drag Event
    private void ProcessRightMouseDragEvent(Event curretEvent)
    {
        if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(curretEvent.delta);
            GUI.changed = true;
        }
    }

    // After Mause Drag Event drag line from the node
    private void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }


    // Create a room node at the mouse position
    private void CreateRoomNode(object mousePositionObject)
    {
        // if roomnodegraph is empty create room node enterence
        if (currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f, 200f), roomNodeTypeList.list.Find(x => x.isEntrance));
        }
        // Create room node with defoult type - none
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }

    // Create a room node at the mouse position - overloaded to also pass in RoomNodeType
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePositon = (Vector2)mousePositionObject;

        // create room node scriptable object asset
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        // add room node to current room node graph room node list
        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        // set room node values
        roomNode.Initialise(new Rect(mousePositon, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        // add room node to room node graph scriptable object asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

        AssetDatabase.SaveAssets();

        // Refresh graph node dictionary
        currentRoomNodeGraph.OnValidate();
    }

    // Delete selected room nodes
    private void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();
    }

    // Delete the links between the selected room nodes
    private void DeleteSelectedRoomNodeLinks()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0)
            {
                for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0; i--)
                {
                    // Get child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[1]);

                    // if the child node is selected
                    if (childRoomNode !=null && childRoomNode.isSelected)
                    {
                        // Remove childID from parent room node
                        roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                        // Remove parentID from the child room node
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        // Clear all selected room nodes
        ClearAllSelectedRoomNodes();
    }

    // Clear Selection from all nodes
    private void ClearAllSelectedRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.isSelected = false;

                GUI.changed = true;
            }
        }
    }

    // Select all room nodes
    private void SelectAllRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.isSelected = true;
        }
        GUI.changed = true;
    }

    // Clear line drag from a room node
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }

    // Draw line between room nodes
    private void DrawRoomConnections()
    {
        // Loop throuht all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.childRoomNodeIDList.Count > 0)
            {
                // Loop throuht all child room nodes
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    // Get child room node from dictionary
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID))
                    {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);

                        GUI.changed = true;
                    }
                }
            }
        }
    }

    // Draw line between parent and child node
    private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
    {
        // get line star and end postition
        Vector2 starPosition = parentRoomNode.rect.center;
        Vector2 endPosition = childRoomNode.rect.center;

        // calculate midway point of the line
        Vector2 midPosition = (endPosition + starPosition) / 2f;

        // Vector from the start to the end position line
        Vector2 direction = endPosition - starPosition;

        // Calcualte normalised perpendicular positions from the mid point
        Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;

        // Calculate mid point offset position for arrow head
        Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingLineArrowSize;

        // Draw Arrow
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.red, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.red, null, connectingLineWidth);

        // draw line
        Handles.DrawBezier(starPosition, endPosition, starPosition, endPosition, Color.red, null, connectingLineWidth);
        GUI.changed = true;
    }

    // Draw room nodes in the graph window
    private void DrawRoomNodes()
    {
        // Loop throught all room nodes and draw them
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.Draw(roomNodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(roomNodeStyle);
            }
        }

        GUI.changed = true;
    }

    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }
}
