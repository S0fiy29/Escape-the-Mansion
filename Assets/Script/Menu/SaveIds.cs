using System.Text;
using UnityEngine;

// IDs for saved objects that have no unique name, like several levers that are all called "Lever".
// The ID is the object's place in the scene hierarchy, so it is the same every time the scene loads.
public static class SaveIds
{
    public static string PathOf(Component component)
    {
        var path = new StringBuilder();
        for (Transform t = component.transform; t != null; t = t.parent)
        {
            if (path.Length > 0) path.Insert(0, '/');
            path.Insert(0, $"{t.name}[{t.GetSiblingIndex()}]");
        }
        return path.ToString();
    }
}
