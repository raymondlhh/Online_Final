using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public static class PersistentPlayerData
    {
        public static float SavedHealth = -1f; // -1 means uninitialized
        public static bool WasDead = false;

        /// <summary>
        /// Reset all persistent data when starting a new game
        /// </summary>
        public static void ResetData()
        {
            SavedHealth = -1f;
            WasDead = false;
        }
    }

