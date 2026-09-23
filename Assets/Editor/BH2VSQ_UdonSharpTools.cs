using UnityEditor;

namespace BH2VSQ.Editor
{
    public static class BH2VSQ_UdonSharpTools
    {
        [MenuItem("Tools/BH2VSQ BASE/Recompile UdonSharp Programs")]
        private static void RecompileUdonSharpPrograms()
        {
            EditorApplication.ExecuteMenuItem("Tools/UdonSharp/Compile All UdonSharp Scripts");
        }
    }
}
