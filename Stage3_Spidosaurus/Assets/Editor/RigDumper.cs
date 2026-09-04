using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility: dumps the selected object's full Transform hierarchy to a text file and
/// flags every bone with 2+ children (a junction). Used to identify the branch point in the
/// provided Spidosaurus rig rather than clicking through the Hierarchy window by hand.
/// Menu: Tools > Dump Rig Hierarchy  (select the skeleton root first)
/// </summary>
public class RigDumper
{
    [MenuItem("Tools/Dump Rig Hierarchy")]
    static void Dump()
    {
        var root = Selection.activeTransform;
        if (root == null) { Debug.LogError("Select the skeleton root first."); return; }

        var sb = new StringBuilder();
        int bones = 0, junctions = 0, tips = 0;

        void Walk(Transform t, int depth)
        {
            bones++;
            if (t.childCount >= 2) junctions++;
            if (t.childCount == 0) tips++;

            sb.Append(new string(' ', depth * 2));
            sb.Append(t.name);
            if (t.childCount >= 2) sb.Append($"    <<<< JUNCTION ({t.childCount} children)");
            if (t.childCount == 0) sb.Append("    [tip]");
            sb.AppendLine();

            foreach (Transform c in t) Walk(c, depth + 1);
        }

        Walk(root, 0);

        string header = $"Rig dump for '{root.name}'\n" +
                        $"bones: {bones}   junctions (2+ children): {junctions}   tips: {tips}\n" +
                        new string('-', 60) + "\n";

        string path = Application.dataPath + "/../RigDump.txt";
        System.IO.File.WriteAllText(path, header + sb.ToString());
        Debug.Log($"Wrote {path}\nbones {bones}, junctions {junctions}, tips {tips}");
        EditorUtility.RevealInFinder(path);
    }
}
