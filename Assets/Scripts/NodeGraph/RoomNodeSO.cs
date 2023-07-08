using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    #region Editor Code

#if UNITY_EDITOR

    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    // Initialise node
    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;

        // Load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeListSO;
    }

    // Draw node with the nodestyle
    public void Draw(GUIStyle nodeStyle)
    {
        // Draw Node Box Using Begin Area
        GUILayout.BeginArea(rect, nodeStyle);

        // Start Region To Detect Popup Selection Changes
        EditorGUI.BeginChangeCheck();

        // Display a popup using the RoomNodeType name values that can be selected from (deafault to the currentyl set roomNodeType)

        int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

        int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

        roomNodeType = roomNodeTypeList.list[selection];

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(this);

        GUILayout.EndArea();
    }

    // Populate a string array with the room node types to display that can be selected
    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }

        return roomArray;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            // Process Mouse Down Events
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            // Process Mouse Up Events
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

    // Process Mouse Down Events
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // left button of the mouse is clicked
        if (currentEvent.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        // right button of the mouse is clicked
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }

    // What happend after left mouse button is clicked
    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;

        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    // What happend after right mouse button is clicked
    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    // Process Mouse Up Events
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        //left click depressed
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    // What happend after left click is up
    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }

    // Process mouse drag event
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    // After left mouse button is pressed and draged event
    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        // Delta is telling program where are u dragging 
        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    // This function moves node including delta
    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    // This function add passed child ID to the node (returns true if the node has been added, false otherwise)
    public bool AddChildRoomNodeIDtoRoomNode(string childID)
    {
        childRoomNodeIDList.Add(childID);
        return true;
    }

    // This function add passed child ID to the node (returns true if the node has been added, false otherwise)
    public bool AddParentRoomNodeIDtoRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

#endif

    #endregion Editor Code

}
