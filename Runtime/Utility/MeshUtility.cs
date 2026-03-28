using UnityEngine;

namespace RealtimeCSG
{
    public static class CSGMeshUtility
    {
        public static Mesh Clone(this Mesh mesh)
        {
            return new Mesh
            {
                vertices  = mesh.vertices
              , colors    = mesh.colors
              , name      = mesh.name
              , normals   = mesh.normals
              , tangents  = mesh.tangents
              , uv        = mesh.uv
              , uv2       = mesh.uv2
              , uv3       = mesh.uv3
              , uv4       = mesh.uv4
              , uv5       = mesh.uv5
              , uv6       = mesh.uv6
              , uv7       = mesh.uv7
              , uv8       = mesh.uv8
              , triangles = mesh.triangles
              , bounds    = mesh.bounds
            };
        }
    }
}
