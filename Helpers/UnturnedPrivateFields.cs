using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.SleepingPlayers
{
    class UnturnedPrivateFields
    {
        private static FieldInfo Storage_x;
        private static FieldInfo Storage_y;
        public static bool setStorageX(ItemStorageAsset storageAsset, byte storage_x)
        {
            if (Storage_x != null)
            {
                Storage_x.SetValue(storageAsset, storage_x);
                return true;
            }
            return false;
        }
        public static bool setStorageY(ItemStorageAsset storageAsset, byte storage_y)
        {
            if (Storage_y != null)
            {
                Storage_y.SetValue(storageAsset, storage_y);
                return true;
            }
            return false;
        }
        public static void init()
        {
            Type type;

            type = typeof(ItemStorageAsset);
            Storage_x = type.GetField("_storage_x", BindingFlags.NonPublic | BindingFlags.Instance);
            Storage_y = type.GetField("_storage_y", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
