using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
        internal static void ClearCaches()
        {
            foreach(CSGBrush brush in Brushes)
            {
                brush.ClearCache();
            }

            foreach(CSGOperation operation in Operations)
            {
                operation.ClearCache();
            }

            foreach(CSGModel model in Models)
            {
                model.ClearCache();
            }
        }
    }
}
