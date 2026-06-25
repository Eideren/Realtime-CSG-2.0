using System.Collections.Generic;
using UnityEngine;

//Used to emulate the functionality of InstanceIDs when using EntityIDs
public static class EntityIdUtility
{
    private static Dictionary<EntityId, int> Int_Lookup = new Dictionary<EntityId, int>();
    private static Dictionary<int, EntityId> EntityId_Lookup = new Dictionary<int, EntityId>();

    public static int GetIntegerID(this Object _object)
    {
        if(_object.GetEntityId().IsValid())
        {
            if(Int_Lookup.TryGetValue(_object.GetEntityId(), out int ID))
            {
                return ID;
            }
            else
            {
                int newID = Int_Lookup.Count + 1;
                EntityId objectID = _object.GetEntityId();

                Int_Lookup.Add(objectID, newID);
                EntityId_Lookup.Add(newID, objectID);

                return newID;
            }
        }

        return 0;
    }

    public static EntityId GetEntityId(int _intID)
    {
        if(_intID == 0)
        {
            return EntityId.None;
        }

        if (EntityId_Lookup.TryGetValue(_intID, out EntityId _entityID))
        {
            return _entityID;
        }

        return EntityId.None;
    }
}