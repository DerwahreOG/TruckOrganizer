using Photon.Pun;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Defensive wrappers around SemiFunc. Some game managers do not exist in
    /// every scene (main menu, loading); a throwing helper must never break
    /// the vanilla methods our Harmony patches are attached to.
    /// </summary>
    public static class SafeGame
    {
        public static bool IsHost()
        {
            try
            {
                return SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch
            {
                return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
            }
        }

        public static bool IsMultiplayer()
        {
            try
            {
                return SemiFunc.IsMultiplayer();
            }
            catch
            {
                return PhotonNetwork.InRoom;
            }
        }

        public static bool RunIsShop()
        {
            try { return SemiFunc.RunIsShop(); }
            catch { return false; }
        }

        public static bool RunIsLevel()
        {
            try { return SemiFunc.RunIsLevel(); }
            catch { return false; }
        }
    }
}
