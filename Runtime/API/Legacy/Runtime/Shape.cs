using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealtimeCSG.Legacy
{
    /// <summary>Defines the <see cref="RealtimeCSG.Legacy.Surface"/>s and its properties for a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
    /// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
    /// <seealso cref="RealtimeCSG.Legacy.Surface"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
    [Serializable]
    public sealed class Shape
    {
        /// <value>Version of the Shape</value>
        [SerializeField, FormerlySerializedAs("Version")] public float version = 1.0f;

        /// <value>An array of <see cref="RealtimeCSG.Legacy.Surface"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.Surface"/> are used to store information about the orientation of the polygons that are associated with this surface.</remarks>
        [SerializeField, FormerlySerializedAs("Surfaces")]
        public Surface[] surfaces = Array.Empty<Surface>();

        /// <value>An array of <see cref="RealtimeCSG.Legacy.TexGen"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.TexGen"/> are used to generate texture coordinates for the polygons that are associated with this surface.</remarks>
        [SerializeField, FormerlySerializedAs("TexGens")]
        public TexGen[] texGens = Array.Empty<TexGen>();

        /// <value>An array of <see cref="RealtimeCSG.Legacy.TexGenFlags"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.TexGenFlags"/> are used to define if a polygon is visible, part of a collision mesh etc.</remarks>
        [SerializeField, FormerlySerializedAs("TexGenFlags")]
        public TexGenFlags[] texGenFlags = Array.Empty<TexGenFlags>();

        // Do not use this anymore, it's deprecated
        [Obsolete("Materials are now stored in TexGens"), HideInInspector, SerializeField]
        internal Material[] materials = Array.Empty<Material>();

#if UNITY_EDITOR
        /// <summary>Constructor for an unintialized <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        public Shape()
        {
        }

        /// <summary>Constructor that takes a <see cref="RealtimeCSG.Legacy.Shape"/> to copy from</summary>
        public Shape(Shape other)
        {
            CopyFrom(other);
        }

        /// <summary>Creates a <see cref="RealtimeCSG.Legacy.Shape"/> and initializes it with <paramref name="polygonCount"/> number of surfaces</summary>
        /// <param name="polygonCount">The number of surfaces to create for this <see cref="RealtimeCSG.Legacy.Shape"/></param>
        public Shape(int polygonCount)
        {
            surfaces    = new Surface[polygonCount];
            texGenFlags = new TexGenFlags[polygonCount];
            texGens     = new TexGen[polygonCount];
        }

        /// <summary>Clear all surfaces in this <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        public void Reset()
        {
            surfaces    = Array.Empty<Surface>();
            texGens     = Array.Empty<TexGen>();
            texGenFlags = Array.Empty<TexGenFlags>();
        }

        /// <summary>Copy all contents from another <see cref="RealtimeCSG.Legacy.Shape"/> into this <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        /// <param name="other">The <see cref="RealtimeCSG.Legacy.Shape"/> to copy the contents from.</param>
        public void CopyFrom(Shape other)
        {
            if(other == null)
            {
                Reset();

                return;
            }

            if(surfaces != null)
            {
                if(surfaces == null || surfaces.Length != other.surfaces.Length)
                {
                    surfaces = new Surface[other.surfaces.Length];
                }

                Array.Copy(other.surfaces, surfaces, other.surfaces.Length);
            }
            else
            {
                surfaces = null;
            }

            if(texGens != null)
            {
                if(texGens == null || texGens.Length != other.texGens.Length)
                {
                    texGens = new TexGen[other.texGens.Length];
                }

                Array.Copy(other.texGens, texGens, other.texGens.Length);
            }
            else
            {
                texGens = null;
            }

            if(texGenFlags != null)
            {
                if(texGenFlags == null || texGenFlags.Length != other.texGenFlags.Length)
                {
                    texGenFlags = new TexGenFlags[other.texGenFlags.Length];
                }

                Array.Copy(other.texGenFlags, texGenFlags, other.texGenFlags.Length);
            }
            else
            {
                texGenFlags = null;
            }
        }

        /// <summary>Creates a copy of this <see cref="RealtimeCSG.Legacy.Shape"/>.</summary>
        /// <returns>The copy of this <see cref="RealtimeCSG.Legacy.Shape"/></returns>
        public Shape Clone()
        {
            return new Shape(this);
        }

        /// <summary>Validates if the materials are correct on this <see cref="RealtimeCSG.Legacy.Shape"/>, and fixes them if they are not.</summary>
        /// <returns>*true* if the shape has been modified, *false* if nothing changed</returns>
        public bool CheckMaterials()
        {
            bool dirty = false;

            if(surfaces == null || surfaces.Length == 0)
            {
                Debug.LogWarning("Surfaces == null || Surfaces.Length == 0");

                return true;
            }

            int maxTexGenIndex = 0;

            for(int i = 0; i < surfaces.Length; i++)
            {
                maxTexGenIndex = Mathf.Max(maxTexGenIndex, surfaces[i].TexGenIndex);
            }

            maxTexGenIndex++;

            if(texGens == null || texGens.Length < maxTexGenIndex)
            {
                dirty = true;
                TexGen[]      newTexGens     = new TexGen[maxTexGenIndex];
                TexGenFlags[] newTexGenFlags = new TexGenFlags[maxTexGenIndex];

                if(texGens != null && texGens.Length > 0)
                {
                    for(int i = 0; i < texGens.Length; i++)
                    {
                        newTexGens[i]     = texGens[i];
                        newTexGenFlags[i] = texGenFlags[i];
                    }
                }

                texGens     = newTexGens;
                texGenFlags = newTexGenFlags;
            }

            return dirty;
        }

#endif
    }
}
