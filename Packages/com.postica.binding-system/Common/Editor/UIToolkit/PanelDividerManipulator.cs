using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PanelDividerManipulator : MouseManipulator
{
    private VisualElement leftPanel;
    private VisualElement container;
    private float startWidth;
    private Vector2 startMousePosition;
    private bool isDragging;

    public float minWidth = 100f;
    public float maxWidth = 400f;

    public PanelDividerManipulator(VisualElement leftPanel, VisualElement container)
    {
        this.leftPanel = leftPanel;
        this.container = container;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (!CanStartManipulation(evt) 
            || DragAndDrop.paths?.Length > 0 
            || DragAndDrop.objectReferences?.Length > 0) return;
        
        startWidth = leftPanel.resolvedStyle.width;
        // Use screen position instead of local position
        startMousePosition = evt.mousePosition;
        isDragging = true;
        target.CaptureMouse();
        evt.StopPropagation();
            
        target.AddToClassList("dragging");
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (!isDragging || !target.HasMouseCapture()) return;

        // Calculate delta based on screen coordinates
        float delta = evt.mousePosition.x - startMousePosition.x;
        float newWidth = Mathf.Clamp(startWidth + delta, minWidth, maxWidth);

        // Apply the new width
        leftPanel.style.width = newWidth;
        
        // Force immediate layout update
        container.MarkDirtyRepaint();
        
        evt.StopPropagation();
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (!isDragging || !target.HasMouseCapture()) return;

        isDragging = false;
        target.ReleaseMouse();
        evt.StopPropagation();
        
        target.RemoveFromClassList("dragging");
    }

    private void OnMouseCaptureOut(MouseCaptureOutEvent evt)
    {
        isDragging = false;
        target.RemoveFromClassList("dragging");
    }
}