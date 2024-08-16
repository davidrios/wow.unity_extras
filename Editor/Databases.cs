using System;
using System.Collections.Generic;

namespace WoWUnityExtras.Database
{
    [Serializable]
    public class CreatureTableRow
    {
        public int guid;
        public int id1;
        public int map;
        public int zoneId;
        public int areaId;
        public int equipment_id;
        public float position_x;
        public float position_y;
        public float position_z;
        public float orientation;
        public int spawntimesecs;
        public float wander_distance;
    }

    [Serializable]
    public class CreatureTemplateTable
    {
        public int entry;
        public int modelid1;
        public int modelid2;
        public int modelid3;
        public int modelid4;
        public string name;
        public int npcflag;
        public float detection_range;
        public float scale;
        public int MovementType;
    }

    [Serializable]
    public class Model
    {
        public List<float> GeoBox;
        public float CollisionWidth;
        public float CollisionHeight;
        public float ModelScale;
        public string FileData;
    }

    [Serializable]
    public class DisplayInfoExtra
    {
        public string BakeMaterialResourcesIDFile;
    }

    [Serializable]
    public class DisplayInfo
    {
        public int ID;
        public int ModelID;
        public float CreatureModelScale;
        public List<string> TextureVariationFileData;
        public Model model;
        public DisplayInfoExtra extra;
        public List<string> geosets;
    }

    [Serializable]
    public class CreatureData
    {
        public CreatureTemplateTable info;
        public Dictionary<string, DisplayInfo> displayInfo;
    }

    [Serializable]
    public class GameObjectTable
    {
        public int id;
        public int map;
        public int zoneId;
        public int areaId;
        public int spawnMask;
        public int phaseMask;
        public float position_x;
        public float position_y;
        public float position_z;
        public float orientation;
        public float rotation0;
        public float rotation1;
        public float rotation2;
        public float rotation3;
    }
}