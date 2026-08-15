using System.Runtime.InteropServices;

namespace SuccuPet.Infrastructure.Persistence
{
    public static class WebGLFileSystemSync
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SuccuPet_SyncFileSystem();
#endif

        public static void FlushPendingWrites()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SuccuPet_SyncFileSystem();
#endif
        }
    }
}