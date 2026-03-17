using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using System.Linq;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolMaterials : ISceneDragTool
    {
        private SelectedBrushSurface[] hoverBrushSurfaces = null;
        private bool hoverOnSelectedSurfaces = false;

        private List<Material> dragMaterials = null;

        private bool selectAllSurfaces = false;
        private GameObject hoverOverObject;
        private MeshRenderer hoverOverMeshRenderer;
        private int hoverMaterialIndex;
        private GameObject prevHoverOverObject;

        private int dragGroup = -1;

#region ValidateDrop

        public bool ValidateDrop(SceneView sceneView)
        {
            Reset();

            if(DragAndDrop.objectReferences == null ||
               DragAndDrop.objectReferences.Length == 0)
            {
                dragMaterials = null;

                return false;
            }

            dragMaterials = new List<Material>();

            foreach(Object obj in DragAndDrop.objectReferences)
            {
                Material material = obj as Material;

                if(material != null)
                {
                    dragMaterials.Add(material);
                }
            }

            if(dragMaterials.Count != 1)
            {
                dragMaterials = null;

                return false;
            }

            dragGroup = -1;

            return true;
        }

#endregion

#region ValidateDropPoint

        public bool ValidateDropPoint(SceneView sceneView)
        {
            if(!sceneView)
            {
                return false;
            }

            hoverOverObject = null;
            Camera     camera = sceneView.camera;
            GameObject foundObject;

            if(!SceneQueryUtility.FindClickWorldIntersection(
                   camera, Event.current.mousePosition, out foundObject, out hoverOverMeshRenderer
                 , out hoverMaterialIndex, ignoreDeepClick: true
               ))
            {
                return false;
            }

            if(foundObject.GetComponent<CSGBrush>())
            {
                return true;
            }

            hoverOverObject = foundObject;

            return true;
        }

#endregion

#region Reset

        public void Reset()
        {
            dragGroup               = -1;
            hoverOverObject         = null;
            prevHoverOverObject     = null;
            hoverBrushSurfaces      = null;
            selectAllSurfaces       = false;
            hoverOnSelectedSurfaces = false;
        }

#endregion

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            if(hoverOverMeshRenderer != null)
            {
                return DragUpdated(sceneView);
            }

            return BrushDragUpdated(sceneView, transformInInspector, selectionRect);
        }

        private void ApplyMeshMaterial()
        {
            if(hoverOverMeshRenderer != null)
            {
                Material[] sharedMaterials = hoverOverMeshRenderer.sharedMaterials;
                Material   material        = GetMaterial();

                if(sharedMaterials[hoverMaterialIndex] != material)
                {
                    //if (UndoRevert())
                    {
                        InternalCSGModelManager.UpdateMeshes();
                        MeshInstanceManager.UpdateHelperSurfaceVisibility();
                    }

                    UndoInit();

                    Undo.RecordObject(hoverOverMeshRenderer, "Modified material");
                    sharedMaterials[hoverMaterialIndex] = material;

                    hoverOverMeshRenderer.sharedMaterials =
                        sharedMaterials.ToArray(); // ToArray forces Undo to recognize the change
                }
            }
        }

        public bool DragUpdated(SceneView sceneView)
        {
            if(prevHoverOverObject != hoverOverObject)
            {
                BrushDragExited(sceneView);
                prevHoverOverObject = hoverOverObject;
            }

            if(hoverOverMeshRenderer != null)
            {
                ApplyMeshMaterial();

                return true;
            }

            if(hoverOverMeshRenderer == null)
            {
                return BrushDragUpdated(sceneView);
            }

            return true;
        }

        public void DragPerform(SceneView sceneView)
        {
            if(hoverOverMeshRenderer == null)
            {
                BrushDragPerform(sceneView);
            }
            else
            {
                ApplyMeshMaterial();
            }
        }

        public void DragExited(SceneView sceneView)
        {
            if(hoverOverMeshRenderer == null)
            {
                BrushDragExited(sceneView);
            }
        }

#region DragUpdated

        public bool BrushDragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            CSGBrush[] highlight_brushes = transformInInspector.GetComponentsInChildren<CSGBrush>();

            bool prevSelectAllSurfaces = selectAllSurfaces;
            selectAllSurfaces = true;
            bool modified = true;

            if(hoverBrushSurfaces != null)
            {
                if(hoverBrushSurfaces.Length != highlight_brushes.Length)
                {
                    modified = false;
                }
                else
                {
                    modified = false;

                    for(int i = 0; i < highlight_brushes.Length; i++)
                    {
                        CSGBrush find_brush = highlight_brushes[i];
                        bool     found      = false;

                        for(int j = 0; j < hoverBrushSurfaces.Length; j++)
                        {
                            if(hoverBrushSurfaces[j].surfaceIndex == -1 &&
                               hoverBrushSurfaces[j].brush == find_brush)
                            {
                                found = true;

                                break;
                            }
                        }

                        if(!found)
                        {
                            modified = true;

                            break;
                        }
                    }
                }
            }

            bool needUpdate = false;

            if(modified)
            {
                hoverOnSelectedSurfaces = false;

                if(hoverBrushSurfaces != null)
                {
                    needUpdate = true;
                    RestoreMaterials(hoverBrushSurfaces);
                }

                hoverBrushSurfaces = HoverOnBrush(highlight_brushes, -1);

                if(hoverBrushSurfaces != null)
                {
                    hoverBrushSurfaces = GetCombinedBrushes(hoverBrushSurfaces);
                    needUpdate         = true;
                    ApplyMaterial(hoverBrushSurfaces);
                }
            }
            else
            {
                if(prevSelectAllSurfaces != selectAllSurfaces)
                {
                    if(hoverBrushSurfaces != null)
                    {
                        needUpdate = true;
                        ApplyMaterial(hoverBrushSurfaces);
                    }
                }
            }

            if(needUpdate)
            {
                InternalCSGModelManager.UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
            }

            return needUpdate;
        }

        public bool BrushDragUpdated(SceneView sceneView)
        {
            Camera                  camera = sceneView.camera;
            LegacyBrushIntersection intersection;

            int      highlight_surface;
            CSGBrush highlight_brush;

            if(!SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
            {
                highlight_brush   = null;
                highlight_surface = -1;
            }
            else
            {
                highlight_brush   = intersection.brush;
                highlight_surface = intersection.surfaceIndex;
            }

            bool modified = true;

            if(hoverBrushSurfaces != null)
            {
                for(int i = 0; i < hoverBrushSurfaces.Length; i++)
                {
                    if(hoverBrushSurfaces[i].brush == highlight_brush &&
                       hoverBrushSurfaces[i].surfaceIndex == highlight_surface)
                    {
                        modified = false;

                        break;
                    }
                }
            }

            bool needUpdate = false;

            if(modified)
            {
                hoverOnSelectedSurfaces = false;

                if(hoverBrushSurfaces != null)
                {
                    needUpdate = true;
                    RestoreMaterials(hoverBrushSurfaces);
                }

                hoverBrushSurfaces = HoverOnBrush(new CSGBrush[1] { highlight_brush }, highlight_surface);

                if(hoverBrushSurfaces != null)
                {
                    hoverBrushSurfaces = GetCombinedBrushes(hoverBrushSurfaces);
                    needUpdate         = true;
                    ApplyMaterial(hoverBrushSurfaces);
                }
            }
            else
            {
                bool prevSelectAllSurfaces = selectAllSurfaces;
                selectAllSurfaces = Event.current.shift;

                //if (prevSelectAllSurfaces != selectAllSurfaces)
                {
                    if(hoverBrushSurfaces != null)
                    {
                        needUpdate = prevSelectAllSurfaces != selectAllSurfaces;

                        ApplyMaterial(hoverBrushSurfaces);
                    }
                }
            }

            if(needUpdate)
            {
                InternalCSGModelManager.UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
            }

            return needUpdate;
        }

#endregion

#region DragPerform

        public void BrushDragPerform(SceneView sceneView)
        {
            if(hoverBrushSurfaces == null)
            {
                return;
            }

            RestoreMaterials(hoverBrushSurfaces);

            ApplyMaterial(hoverBrushSurfaces);

            HashSet<GameObject> gameObjects = new HashSet<GameObject>();

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                gameObjects.Add(hoverBrushSurfaces[i].brush.gameObject);
            }

            EditModeSurface surfaceTool = EditModeManager.ActiveTool as EditModeSurface;

            if(surfaceTool != null)
            {
                surfaceTool.SelectSurfaces(hoverBrushSurfaces, gameObjects, selectAllSurfaces);
            }
            else
            {
                Selection.objects = gameObjects.ToArray();
            }

            for(int i = 0; i < SceneView.sceneViews.Count; i++)
            {
                SceneView sceneview = SceneView.sceneViews[i] as SceneView;

                if(sceneview == null ||
                   sceneview.camera == null)
                {
                    continue;
                }

                Rect rect = sceneview.camera.pixelRect;

                if(rect.Contains(Event.current.mousePosition))
                {
                    sceneview.Focus();
                }
            }

            hoverBrushSurfaces = null;
        }

#endregion

#region DragExited

        public void BrushDragExited(SceneView sceneView)
        {
            if(hoverBrushSurfaces != null)
            {
                UndoRevert();

                HashSet<CSGModel> updateModels  = new HashSet<CSGModel>();
                HashSet<CSGBrush> updateBrushes = new HashSet<CSGBrush>();

                for(int i = 0; i < hoverBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = hoverBrushSurfaces[i].brush;

                    if(brush.ChildData == null ||
                       brush.ChildData.Model == null)
                    {
                        continue;
                    }

                    try
                    {
                        CSGModel model = brush.ChildData.Model;
                        updateModels.Add(model);
                        updateBrushes.Add(brush);
                    }
                    finally
                    {
                    }
                }

                UpdateBrushMeshes(updateBrushes, updateModels);
                RestoreMaterials(hoverBrushSurfaces);

                InternalCSGModelManager.UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                HandleUtility.Repaint();
            }

            hoverBrushSurfaces = null;
        }

#endregion

        private SelectedBrushSurface[] GetCombinedBrushes(SelectedBrushSurface[] hoverBrushSurfaces)
        {
            List<SelectedBrushSurface> highlight_surfaces = new List<SelectedBrushSurface>();
            HashSet<CSGBrush>          highlight_brushes  = new HashSet<CSGBrush>();

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                highlight_surfaces.Add(hoverBrushSurfaces[i]);
            }

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                CSGBrush brush    = hoverBrushSurfaces[i].brush;
                CSGNode  top_node = SceneQueryUtility.GetTopMostGroupForNode(brush);

                if(top_node.transform != brush.transform)
                {
                    foreach(CSGBrush childBrush in top_node.GetComponentsInChildren<CSGBrush>())
                    {
                        if(highlight_brushes.Add(childBrush))
                        {
                            highlight_surfaces.Add(new SelectedBrushSurface(childBrush, -1));
                        }
                    }
                }
            }

            return highlight_surfaces.ToArray();
        }

#region HoverOnBrush

        private SelectedBrushSurface[] HoverOnBrush(CSGBrush[] hoverBrushes, int surfaceIndex)
        {
            hoverOnSelectedSurfaces = false;

            if(hoverBrushes == null ||
               hoverBrushes.Length == 0 ||
               hoverBrushes[0] == null)
            {
                return null;
            }

            EditModeSurface activetool = EditModeManager.ActiveTool as EditModeSurface;

            if(activetool != null)
            {
                SelectedBrushSurface[] selectedBrushSurfaces = activetool.GetSelectedSurfaces();

                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    if(selectedBrushSurfaces[i].surfaceIndex == surfaceIndex &&
                       ArrayUtility.Contains(hoverBrushes, selectedBrushSurfaces[i].brush))
                    {
                        if(i != 0 && selectedBrushSurfaces.Length > 1)
                        {
                            SelectedBrushSurface temp = selectedBrushSurfaces[0];
                            selectedBrushSurfaces[0] = selectedBrushSurfaces[i];
                            selectedBrushSurfaces[i] = temp;
                        }

                        hoverOnSelectedSurfaces = true;

                        return selectedBrushSurfaces;
                    }
                }
            }

            SelectedBrushSurface[] surfaces = new SelectedBrushSurface[hoverBrushes.Length];

            for(int i = 0; i < hoverBrushes.Length; i++)
            {
                surfaces[i] = new SelectedBrushSurface(hoverBrushes[i], surfaceIndex);
            }

            return surfaces;
        }

#endregion

#region UpdateBrushMeshes

        private void UpdateBrushMeshes(HashSet<CSGBrush> brushes, HashSet<CSGModel> models)
        {
            foreach(CSGBrush brush in brushes)
            {
                try
                {
                    brush.EnsureInitialized();
                    ShapeUtility.CheckMaterials(brush.Shape);
                }
                finally
                {
                }
            }

            foreach(CSGBrush brush in brushes)
            {
                try
                {
                    InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                    InternalCSGModelManager.ValidateBrush(brush);
                }
                finally
                {
                }
            }

            MeshInstanceManager.UpdateHelperSurfaceVisibility(true);
        }

#endregion

        private static string MaterialToString(Material mat)
        {
            if(ReferenceEquals(mat, null))
            {
                return "null";
            }

            if(!mat)
            {
                return "invalid";
            }

            return mat.name + " " + mat.GetInstanceID().ToString();
        }

        private bool UndoRevert()
        {
            if(dragGroup != -1)
            {
                Undo.RevertAllDownToGroup(dragGroup);
                dragGroup = -1;

                return true;
            }

            return false;
        }

        private void UndoInit()
        {
            if(dragGroup == -1)
            {
                Undo.IncrementCurrentGroup();
                dragGroup = Undo.GetCurrentGroup();
            }
        }

#region RestoreMaterials

        private void RestoreMaterials(SelectedBrushSurface[] hoverBrushSurfaces)
        {
            if(hoverBrushSurfaces == null)
            {
                return;
            }

            UndoRevert();

            HashSet<CSGModel> updateModels  = new HashSet<CSGModel>();
            HashSet<CSGBrush> updateBrushes = new HashSet<CSGBrush>();

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                CSGBrush brush = hoverBrushSurfaces[i].brush;

                if(brush.ChildData == null ||
                   brush.ChildData.Model == null)
                {
                    continue;
                }

                try
                {
                    CSGModel model = brush.ChildData.Model;
                    updateModels.Add(model);
                    updateBrushes.Add(brush);
                }
                finally
                {
                }
            }

            UpdateBrushMeshes(updateBrushes, updateModels);
        }

#endregion

        private Material GetMaterial()
        {
            if(dragMaterials.Count > 1)
            {
                return dragMaterials[Random.Range(0, dragMaterials.Count)];
            }
            else
            {
                return dragMaterials[0];
            }
        }

#region ApplyMaterial

        private void ApplyMaterial(SelectedBrushSurface[] hoverBrushSurfaces)
        {
            if(hoverBrushSurfaces == null)
            {
                return;
            }

            HashSet<CSGModel> updateModels  = new HashSet<CSGModel>();
            HashSet<CSGBrush> updateBrushes = new HashSet<CSGBrush>();

            try
            {
                for(int i = 0; i < hoverBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = hoverBrushSurfaces[i].brush;

                    if(brush.ChildData == null ||
                       brush.ChildData.Model == null)
                    {
                        continue;
                    }

                    try
                    {
                        Shape    shape = brush.Shape;
                        CSGModel model = brush.ChildData.Model;

                        TexGen[] texGens = shape.TexGens;

                        bool modified = false;

                        if(selectAllSurfaces)
                        {
                            // assign material to all surfaces of selected brushes
                            if(!updateBrushes.Contains(
                                   brush
                               )) // but don't duplicate any work if multiple surfaces of same brush are selected
                            {
                                for(int m = 0; m < texGens.Length; m++)
                                {
                                    Material material = GetMaterial();

                                    if(texGens[m].RenderMaterial != material)
                                    {
                                        if(modified)
                                        {
                                            UndoInit();
                                            Undo.RecordObject(brush, "Modifying material");
                                        }

                                        modified                  = true;
                                        texGens[m].RenderMaterial = material;
                                    }
                                }
                            }
                        }
                        else
                            // per surface
                        {
                            // assign material to selected surface
                            int highlight_surface = hoverBrushSurfaces[i].surfaceIndex;

                            if(highlight_surface >= 0)
                            {
                                Material material         = GetMaterial();
                                int      highlight_texGen = shape.Surfaces[highlight_surface].TexGenIndex;

                                if(texGens[highlight_texGen].RenderMaterial != material)
                                {
                                    if(!modified)
                                    {
                                        UndoInit();
                                        Undo.RecordObject(brush, "Modifying material");
                                    }

                                    modified                                 = true;
                                    texGens[highlight_texGen].RenderMaterial = material;
                                }
                            }
                        }

                        if(modified)
                        {
                            shape.TexGens = texGens.ToArray();
                            updateModels.Add(model);
                            updateBrushes.Add(brush);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            finally
            {
                UpdateBrushMeshes(updateBrushes, updateModels);
            }
        }

#endregion

#region Paint

        public void OnPaint(Camera camera)
        {
            if(hoverOverObject != null)
            {
                return;
            }

            if(!hoverOnSelectedSurfaces)
            {
                EditModeSurface activetool = EditModeManager.ActiveTool as EditModeSurface;

                if(activetool != null)
                {
                    SelectedBrushSurface[] selectedBrushSurfaces = activetool.GetSelectedSurfaces();

                    for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                    {
                        CSGBrush brush = selectedBrushSurfaces[i].brush;

                        int       highlight_surface    = selectedBrushSurfaces[i].surfaceIndex;
                        Matrix4x4 brush_transformation = brush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawSurfaceOutlines(
                            brush.brushNodeID, brush.Shape,
                            brush_transformation, highlight_surface,
                            ColorSettings.SurfaceInnerStateColor[2],
                            ColorSettings.SurfaceOuterStateColor[2],
                            GUIConstants.OLD_THIN_LINE_SCALE
                        );
                    }
                }
            }

            if(hoverBrushSurfaces != null)
            {
                for(int i = 0; i < hoverBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = hoverBrushSurfaces[i].brush;

                    Matrix4x4 brush_transformation = brush.compareTransformation.localToWorldMatrix;

                    int highlight_surface = hoverBrushSurfaces[i].surfaceIndex;

                    if(highlight_surface == -1 || selectAllSurfaces)
                    {
                        CSGRenderer.DrawSelectedBrush(
                            brush.brushNodeID, brush.Shape, brush_transformation, ColorSettings.WireframeOutline, 0
                          , selectAllSurfaces, GUIConstants.OLD_LINE_SCALE
                        );
                    }
                    else
                    {
                        CSGRenderer.DrawSelectedBrush(
                            brush.brushNodeID, brush.Shape, brush_transformation, ColorSettings.WireframeOutline
                          , highlight_surface, selectAllSurfaces, GUIConstants.OLD_LINE_SCALE
                        );
                    }
                }
            }
        }

#endregion
    }
}
