using HarmonyLib;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony patches to <see cref="XUiC_DewCollectorWindow"/>.
    /// </summary>
    public static class XUiC_DewCollectorWindowPatches
    {
        /// <summary>
        /// Harmony patch to
        /// <see cref="XUiC_DewCollectorWindow.SetTileEntity(TileEntityDewCollector)"/>
        /// to change the container size of the <see cref="TileEntityDewCollector"/>
        /// if needed.
        /// </summary>
        [HarmonyPatch(
            typeof(XUiC_DewCollectorWindow),
            nameof(XUiC_DewCollectorWindow.SetTileEntity))]
        public static class SetTileEntity
        {
            /// <summary>
            /// Harmony prefix to
            /// <see cref="XUiC_DewCollectorWindow.SetTileEntity(TileEntityDewCollector)"/>
            /// which changes the container size of the <see cref="TileEntityDewCollector"/>
            /// if needed.
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="_te"></param>
            /// <returns></returns>
            public static bool Prefix(
                XUiC_DewCollectorWindow __instance,
                TileEntityDewCollector _te)
            {
                DewCollectorHelpers.ChangeContainerSize(_te, __instance.container);

                return true;
            }
        }
    }
}
