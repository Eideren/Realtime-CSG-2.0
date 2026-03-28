using System;
using System.Collections.Generic;
using System.Linq;
using RealtimeCSG.Components;
using UnityEditor;

namespace RealtimeCSG
{
    internal sealed class UndoGroup : IDisposable
    {
        private CSGBrush[] _brushes;
        private readonly int _undoGroupIndex;

        public UndoGroup(SelectedBrushSurface[] selectedBrushSurfaces, string name, bool ignoreGroup = false)
        {
            if(selectedBrushSurfaces == null)
            {
                return;
            }

            _undoGroupIndex = -1;

            if(_brushes.Length > 0)
            {
                if(!ignoreGroup)
                {
                    _undoGroupIndex = Undo.GetCurrentGroup();
                    Undo.IncrementCurrentGroup();
                }

                Undo.RegisterCompleteObjectUndo(_brushes, name);

                for(int i = 0; i < _brushes.Length; i++)
                {
                    if(!_brushes[i])
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(_brushes[i]);
                }
            }
        }

        private bool _disposedValue;

        public void Dispose(bool disposing)
        {
            if(!_disposedValue)
            {
                if(disposing)
                {
                    if(_brushes == null)
                    {
                        return;
                    }

                    if(_brushes.Length > 0)
                    {
                        for(int i = 0; i < _brushes.Length; i++)
                        {
                            if(!_brushes[i])
                            {
                                continue;
                            }

                            _brushes[i].EnsureInitialized();
                            ShapeUtility.CheckMaterials(_brushes[i].shape);
                        }

                        for(int i = 0; i < _brushes.Length; i++)
                        {
                            if(!_brushes[i])
                            {
                                continue;
                            }

                            InternalCSGModelManager.CheckSurfaceModifications(_brushes[i], true);
                        }

                        if(_undoGroupIndex != -1)
                        {
                            Undo.CollapseUndoOperations(_undoGroupIndex);
                            Undo.FlushUndoRecordObjects();
                        }
                    }
                }

                _brushes       = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
