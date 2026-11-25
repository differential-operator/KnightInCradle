using UnityEngine;

namespace KnightInCradle.CharmUi
{
    internal static class OverlayEscape
    {
        private static int blockUntil = -1;
        private static bool drainingEscape;
        internal static bool Consumed
        {
            get
            {
                if (!drainingEscape) return false;
                // Native PR.isMenuPD(2) keeps buffered presses beyond the Unity key-down frame.
                if (Input.GetKey(KeyCode.Escape)) blockUntil = Time.frameCount + 2;
                if (Time.frameCount <= blockUntil) return true;
                drainingEscape = false;
                return false;
            }
        }

        private static void Consume()
        {
            blockUntil = Time.frameCount + 2;
            drainingEscape = true;
        }

        internal static bool Handle()
        {
            if (Consumed) return true;
            if (!Input.GetKeyDown(KeyCode.Escape)) return false;
            if (KeyRebindLayer.CloseForEscape())
            {
                Consume();
                return true;
            }
            var charms = CharmUiController.Instance;
            if (charms == null || !charms.IsOpen) return false;
            Consume();
            charms.CloseAndHide();
            return true;
        }
    }
}
