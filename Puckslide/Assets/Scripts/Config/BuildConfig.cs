using UnityEngine;

namespace Puckslide
{
    [CreateAssetMenu(fileName = "BuildConfig", menuName = "Puckslide/Build Config")]
    public class BuildConfig : ScriptableObject
    {
        public bool EnableSteam = true;
        public bool EnableMirror = true;
        public bool EnableDebugOverlayInRelease = true;
    }
}
