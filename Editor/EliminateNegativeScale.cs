using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityToCustomEngineExporter.Editor
{
    public class EliminateNegativeScale
    {
        [MenuItem("Tools/Export To Custom Engine/Eliminate Negative Scale")]
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            Visit(scene.GetRootGameObjects());
            Undo.FlushUndoRecordObjects();
        }

        private static void Visit(IEnumerable<GameObject> gos)
        {
            foreach (var gameObject in gos)
            {
                Visit(gameObject);
            }
        }

        private static void Visit(GameObject go)
        {
            var scale = go.transform.localScale;

            // Double negative scale gives a correct normals.
            if (scale.x * scale.y * scale.z > 0)
            {
                return;
            }
            Undo.RegisterCompleteObjectUndo(go.transform, "Eliminate Negative Scale");
            Vector3 pivot;
            var allRenderers = go.GetComponents<Renderer>().Concat(go.GetComponentsInChildren<Renderer>()).ToList();
            if (allRenderers.Count > 0)
            {
                var bounds = allRenderers.First().bounds;
                foreach (var renderer in allRenderers.Skip(1))
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                pivot = bounds.center;
            }
            else
            {
                pivot = go.transform.position;
            }

            var localPivot = go.transform.InverseTransformPoint(pivot);
            var worldUp = go.transform.TransformDirection(Vector3.up);
            var worldRight = go.transform.TransformDirection(Vector3.right);

            if (scale.x < 0)
            {
                go.transform.RotateAround(pivot, worldUp, 180);
                scale.x = -scale.x;
            }
            else if (scale.z < 0)
            {
                go.transform.RotateAround(pivot, worldUp, 180);
                scale.z = -scale.z;
            }
            else if (scale.y < 0)
            {
                go.transform.RotateAround(pivot, worldRight, 180);
                scale.y = -scale.y;
            }
            go.transform.localScale = scale;
            var worldPivot = go.transform.TransformPoint(localPivot);
            go.transform.position += pivot - worldPivot;
            Visit(Enumerable.Range(0, go.transform.childCount).Select(_ => go.transform.GetChild(_))
                .Select(_ => _.gameObject));
        }
    }
}