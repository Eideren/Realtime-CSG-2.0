using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolMaterials : ISceneDragTool
    {
        private SelectedBrushSurface[] _hoverBrushSurfaces;
        private bool _hoverOnSelectedSurfaces;

        private List<Material> _dragMaterials;

        private bool _selectAllSurfaces;
        private GameObject _hoverOverObject;
        private MeshRenderer _hoverOverMeshRenderer;
        private int _hoverMaterialIndex;
        private GameObject _prevHoverOverObject;

        private int _dragGroup = -1;

#region ValidateDrop

        public bool ValidateDrop(SceneView sceneView)
        {
            Reset();

            if(DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
            {
                _dragMaterials = null;

                return false;
            }

            _dragMaterials = new List<Material>();

            foreach(Object obj in DragAndDrop.objectReferences)
            {
                Material material = obj as Material;

                if(material != null)
                {
                    _dragMaterials.Add(material);
                }
            }

            if(_dragMaterials.Count != 1)
            {
                _dragMaterials = null;

                return false;
            }

            _dragGroup = -1;

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

            _hoverOverObject = null;

            Camera camera = sceneView.camera;

            if(!SceneQueryUtility.FindClickWorldIntersection(camera, Event.current.mousePosition, out GameObject foundObject, out _hoverOverMeshRenderer, out _hoverMaterialIndex, ignoreDeepClick: true))
            {
                return false;
            }

            if(foundObject.GetComponent<CSGBrush>())
            {
                return true;
            }

            _hoverOverObject = foundObject;

            return true;
        }

#endregion

#region Reset

        public void Reset()
        {
            _dragGroup               = -1;
            _hoverOverObject         = null;
            _prevHoverOverObject     = null;
            _hoverBrushSurfaces      = null;
            _selectAllSurfaces       = false;
            _hoverOnSelectedSurfaces = false;
        }

#endregion

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            return _hoverOverMeshRenderer != null ? DragUpdated(sceneView) : BrushDragUpdated(transformInInspector);
        }

        private void ApplyMeshMaterial()
        {
            if(_hoverOverMeshRenderer != null)
            {
                Material[] sharedMaterials = _hoverOverMeshRenderer.sharedMaterials;
                Material   material        = GetMaterial();

                if(sharedMaterials[_hoverMaterialIndex] != material)
                {
                    InternalCSGModelManager.UpdateMeshes();
                    MeshInstanceManager.UpdateHelperSurfaceVisibility();

                    UndoInit();

                    Undo.RecordObject(_hoverOverMeshRenderer, "Modified material");
                    sharedMaterials[_hoverMaterialIndex] = material;

                    _hoverOverMeshRenderer.sharedMaterials = sharedMaterials.ToArray(); // ToArray forces Undo to recognize the change
                }
            }
        }

        public bool DragUpdated(SceneView sceneView)
        {
            if(_prevHoverOverObject != _hoverOverObject)
            {
                BrushDragExited();
                _prevHoverOverObject = _hoverOverObject;
            }

            if(_hoverOverMeshRenderer != null)
            {
                ApplyMeshMaterial();

                return true;
            }

            return _hoverOverMeshRenderer != null || BrushDragUpdated(sceneView);
        }

        public void DragPerform(SceneView sceneView)
        {
            if(_hoverOverMeshRenderer == null)
            {
                BrushDragPerform();
            }
            else
            {
                ApplyMeshMaterial();
            }
        }

        public void DragExited(SceneView sceneView)
        {
            if(_hoverOverMeshRenderer == null)
            {
                BrushDragExited();
            }
        }

#region DragUpdated

        public bool BrushDragUpdated(Transform transformInInspector)
        {
            CSGBrush[] highlightBrushes = transformInInspector.GetComponentsInChildren<CSGBrush>();

            bool prevSelectAllSurfaces = _selectAllSurfaces;
            _selectAllSurfaces = true;
            bool modified = true;

            if(_hoverBrushSurfaces != null)
            {
                if(_hoverBrushSurfaces.Length != highlightBrushes.Length)
                {
                    modified = false;
                }
                else
                {
                    modified = false;

                    for(int i = 0; i < highlightBrushes.Length; i++)
                    {
                        CSGBrush findBrush = highlightBrushes[i];
                        bool     found     = false;

                        for(int j = 0; j < _hoverBrushSurfaces.Length; j++)
                        {
                            if(_hoverBrushSurfaces[j].surfaceIndex == -1 && _hoverBrushSurfaces[j].brush == findBrush)
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
                _hoverOnSelectedSurfaces = false;

                if(_hoverBrushSurfaces != null)
                {
                    needUpdate = true;
                    RestoreMaterials(_hoverBrushSurfaces);
                }

                _hoverBrushSurfaces = HoverOnBrush(highlightBrushes, -1);

                if(_hoverBrushSurfaces != null)
                {
                    _hoverBrushSurfaces = GetCombinedBrushes(_hoverBrushSurfaces);
                    needUpdate          = true;
                    ApplyMaterial(_hoverBrushSurfaces);
                }
            }
            else
            {
                if(prevSelectAllSurfaces != _selectAllSurfaces)
                {
                    if(_hoverBrushSurfaces != null)
                    {
                        needUpdate = true;
                        ApplyMaterial(_hoverBrushSurfaces);
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
            Camera camera = sceneView.camera;

            int      highlightSurface;
            CSGBrush highlightBrush;

            if(!SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out LegacyBrushIntersection intersection))
            {
                highlightBrush   = null;
                highlightSurface = -1;
            }
            else
            {
                highlightBrush   = intersection.brush;
                highlightSurface = intersection.surfaceIndex;
            }

            bool modified = true;

            if(_hoverBrushSurfaces != null)
            {
                for(int i = 0; i < _hoverBrushSurfaces.Length; i++)
                {
                    if(_hoverBrushSurfaces[i].brush == highlightBrush && _hoverBrushSurfaces[i].surfaceIndex == highlightSurface)
                    {
                        modified = false;

                        break;
                    }
                }
            }

            bool needUpdate = false;

            if(modified)
            {
                _hoverOnSelectedSurfaces = false;

                if(_hoverBrushSurfaces != null)
                {
                    needUpdate = true;
                    RestoreMaterials(_hoverBrushSurfaces);
                }

                _hoverBrushSurfaces = HoverOnBrush(new CSGBrush[1] { highlightBrush }, highlightSurface);

                if(_hoverBrushSurfaces != null)
                {
                    _hoverBrushSurfaces = GetCombinedBrushes(_hoverBrushSurfaces);
                    needUpdate          = true;
                    ApplyMaterial(_hoverBrushSurfaces);
                }
            }
            else
            {
                bool prevSelectAllSurfaces = _selectAllSurfaces;
                _selectAllSurfaces = Event.current.shift;

                if(_hoverBrushSurfaces != null)
                {
                    needUpdate = prevSelectAllSurfaces != _selectAllSurfaces;

                    ApplyMaterial(_hoverBrushSurfaces);
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

        public void BrushDragPerform()
        {
            if(_hoverBrushSurfaces == null)
            {
                return;
            }

            RestoreMaterials(_hoverBrushSurfaces);

            ApplyMaterial(_hoverBrushSurfaces);

            HashSet<GameObject> gameObjects = new HashSet<GameObject>();

            for(int i = 0; i < _hoverBrushSurfaces.Length; i++)
            {
                gameObjects.Add(_hoverBrushSurfaces[i].brush.gameObject);
            }

            EditModeSurface surfaceTool = EditModeManager.ActiveTool as EditModeSurface;

            if(surfaceTool != null)
            {
                surfaceTool.SelectSurfaces(_hoverBrushSurfaces, gameObjects, _selectAllSurfaces);
            }
            else
            {
                Selection.objects = gameObjects.ToArray();
            }

            for(int i = 0; i < SceneView.sceneViews.Count; i++)
            {
                SceneView sceneview = SceneView.sceneViews[i] as SceneView;

                if(sceneview == null || sceneview.camera == null)
                {
                    continue;
                }

                Rect rect = sceneview.camera.pixelRect;

                if(rect.Contains(Event.current.mousePosition))
                {
                    sceneview.Focus();
                }
            }

            _hoverBrushSurfaces = null;
        }

#endregion

#region DragExited

        public void BrushDragExited()
        {
            if(_hoverBrushSurfaces != null)
            {
                UndoRevert();

                HashSet<CSGModel> updateModels  = new HashSet<CSGModel>();
                HashSet<CSGBrush> updateBrushes = new HashSet<CSGBrush>();

                for(int i = 0; i < _hoverBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = _hoverBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.model == null)
                    {
                        continue;
                    }

                    CSGModel model = brush.childData.model;
                    updateModels.Add(model);
                    updateBrushes.Add(brush);
                }

                UpdateBrushMeshes(updateBrushes, updateModels);
                RestoreMaterials(_hoverBrushSurfaces);

                InternalCSGModelManager.UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                HandleUtility.Repaint();
            }

            _hoverBrushSurfaces = null;
        }

#endregion

        private SelectedBrushSurface[] GetCombinedBrushes(SelectedBrushSurface[] hoverBrushSurfaces)
        {
            List<SelectedBrushSurface> highlightSurfaces = new List<SelectedBrushSurface>();
            HashSet<CSGBrush>          highlightBrushes  = new HashSet<CSGBrush>();

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                highlightSurfaces.Add(hoverBrushSurfaces[i]);
            }

            for(int i = 0; i < hoverBrushSurfaces.Length; i++)
            {
                CSGBrush brush   = hoverBrushSurfaces[i].brush;
                CSGNode  topNode = SceneQueryUtility.GetTopMostGroupForNode(brush);

                if(topNode.transform != brush.transform)
                {
                    foreach(CSGBrush childBrush in topNode.GetComponentsInChildren<CSGBrush>())
                    {
                        if(highlightBrushes.Add(childBrush))
                        {
                            highlightSurfaces.Add(new SelectedBrushSurface(childBrush, -1));
                        }
                    }
                }
            }

            return highlightSurfaces.ToArray();
        }

#region HoverOnBrush

        private SelectedBrushSurface[] HoverOnBrush(CSGBrush[] hoverBrushes, int surfaceIndex)
        {
            _hoverOnSelectedSurfaces = false;

            if(hoverBrushes == null || hoverBrushes.Length == 0 || hoverBrushes[0] == null)
            {
                return null;
            }

            EditModeSurface activetool = EditModeManager.ActiveTool as EditModeSurface;

            if(activetool != null)
            {
                SelectedBrushSurface[] selectedBrushSurfaces = activetool.GetSelectedSurfaces();

                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    if(selectedBrushSurfaces[i].surfaceIndex == surfaceIndex && ArrayUtility.Contains(hoverBrushes, selectedBrushSurfaces[i].brush))
                    {
                        if(i != 0 && selectedBrushSurfaces.Length > 1)
                        {
                            (selectedBrushSurfaces[0], selectedBrushSurfaces[i]) = (selectedBrushSurfaces[i], selectedBrushSurfaces[0]);
                        }

                        _hoverOnSelectedSurfaces = true;

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
                brush.EnsureInitialized();
                ShapeUtility.CheckMaterials(brush.shape);
            }

            foreach(CSGBrush brush in brushes)
            {
                InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                InternalCSGModelManager.ValidateBrush(brush);
            }

            MeshInstanceManager.UpdateHelperSurfaceVisibility(true);
        }

#endregion

        private bool UndoRevert()
        {
            if(_dragGroup != -1)
            {
                Undo.RevertAllDownToGroup(_dragGroup);
                _dragGroup = -1;

                return true;
            }

            return false;
        }

        private void UndoInit()
        {
            if(_dragGroup == -1)
            {
                Undo.IncrementCurrentGroup();
                _dragGroup = Undo.GetCurrentGroup();
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

                if(brush.childData == null || brush.childData.model == null)
                {
                    continue;
                }

                CSGModel model = brush.childData.model;
                updateModels.Add(model);
                updateBrushes.Add(brush);
            }

            UpdateBrushMeshes(updateBrushes, updateModels);
        }

#endregion

        private Material GetMaterial()
        {
            return _dragMaterials.Count > 1 ? _dragMaterials[Random.Range(0, _dragMaterials.Count)] : _dragMaterials[0];
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

                    if(brush.childData == null || brush.childData.model == null)
                    {
                        continue;
                    }

                    Shape    shape = brush.shape;
                    CSGModel model = brush.childData.model;

                    TexGen[] texGens = shape.texGens;

                    bool modified = false;

                    if(_selectAllSurfaces)
                    {
                        // assign material to all surfaces of selected brushes
                        // but don't duplicate any work if multiple surfaces of same brush are selected
                        if(!updateBrushes.Contains(brush))
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
                    else // per surface
                    {
                        // assign material to selected surface
                        int highlightSurface = hoverBrushSurfaces[i].surfaceIndex;

                        if(highlightSurface >= 0)
                        {
                            Material material        = GetMaterial();
                            int      highlightTEXGen = shape.surfaces[highlightSurface].TexGenIndex;

                            if(texGens[highlightTEXGen].RenderMaterial != material)
                            {
                                if(!modified) // TODO: inspections says always true, is this actually the case?
                                {
                                    UndoInit();
                                    Undo.RecordObject(brush, "Modifying material");
                                }

                                modified = true;

                                texGens[highlightTEXGen].RenderMaterial = material;
                            }
                        }
                    }

                    if(modified)
                    {
                        shape.texGens = texGens.ToArray();
                        updateModels.Add(model);
                        updateBrushes.Add(brush);
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
            if(_hoverOverObject != null)
            {
                return;
            }

            if(!_hoverOnSelectedSurfaces)
            {
                EditModeSurface activetool = EditModeManager.ActiveTool as EditModeSurface;

                if(activetool != null)
                {
                    SelectedBrushSurface[] selectedBrushSurfaces = activetool.GetSelectedSurfaces();

                    for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                    {
                        CSGBrush brush = selectedBrushSurfaces[i].brush;

                        int       highlightSurface    = selectedBrushSurfaces[i].surfaceIndex;
                        Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawSurfaceOutlines(brush.brushNodeID, brush.shape, brushTransformation, highlightSurface, ColorSettings.SurfaceInnerStateColor[2], ColorSettings.SurfaceOuterStateColor[2], GUIConstants.OLD_THIN_LINE_SCALE);
                    }
                }
            }

            if(_hoverBrushSurfaces != null)
            {
                for(int i = 0; i < _hoverBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = _hoverBrushSurfaces[i].brush;

                    Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

                    int highlightSurface = _hoverBrushSurfaces[i].surfaceIndex;

                    if(highlightSurface == -1 || _selectAllSurfaces)
                    {
                        CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.shape, brushTransformation, ColorSettings.WireframeOutline, 0, _selectAllSurfaces, GUIConstants.OLD_LINE_SCALE);
                    }
                    else
                    {
                        CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.shape, brushTransformation, ColorSettings.WireframeOutline, highlightSurface, _selectAllSurfaces, GUIConstants.OLD_LINE_SCALE);
                    }
                }
            }
        }

#endregion
    }
}
