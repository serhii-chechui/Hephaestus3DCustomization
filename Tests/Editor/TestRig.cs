using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    /// <summary>Builds small skeletons and skinned items for the tests and destroys them afterwards.</summary>
    internal sealed class TestRig
    {
        private readonly List<Object> _objects = new List<Object>();

        /// <summary>Character: Character > Root > Hips > Spine > Head, with a wearer and a skeleton rooted at Root.</summary>
        public OutfitWearer CreateCharacter(out OutfitSkeleton skeleton)
        {
            var character = Track(new GameObject("Character"));
            var root = CreateChild("Root", character.transform, Vector3.zero);
            var hips = CreateChild("Hips", root, new Vector3(0f, 1f, 0f));
            var spine = CreateChild("Spine", hips, new Vector3(0f, 0.3f, 0f));
            CreateChild("Head", spine, new Vector3(0f, 0.4f, 0f));

            skeleton = character.AddComponent<OutfitSkeleton>();
            skeleton.SetRootBone(root);

            return character.AddComponent<OutfitWearer>();
        }

        /// <summary>
        /// Skinned item: Item > Hips > Spine > <paramref name="extraBone"/> plus Item > Mesh,
        /// whose renderer is skinned to Hips, Spine and the extra bone.
        /// </summary>
        public GameObject CreateSkinnedItem(string itemName, string extraBone = "Tail")
        {
            var item = Track(new GameObject(itemName));
            var hips = CreateChild("Hips", item.transform, new Vector3(0f, 1f, 0f));
            var spine = CreateChild("Spine", hips, new Vector3(0f, 0.3f, 0f));
            var extra = CreateChild(extraBone, spine, new Vector3(0f, 0.2f, 0f));

            var meshObject = new GameObject("Mesh");
            meshObject.transform.SetParent(item.transform, false);

            var bones = new[] { hips, spine, extra };
            var mesh = Track(new Mesh
            {
                vertices = new[] { new Vector3(0f, 1f, 0f), new Vector3(0f, 1.3f, 0f), new Vector3(0f, 1.5f, 0f) },
                triangles = new[] { 0, 1, 2 },
                boneWeights = new[]
                {
                    new BoneWeight { boneIndex0 = 0, weight0 = 1f },
                    new BoneWeight { boneIndex0 = 1, weight0 = 1f },
                    new BoneWeight { boneIndex0 = 2, weight0 = 1f }
                },
                bindposes = new[] { hips.worldToLocalMatrix, spine.worldToLocalMatrix, extra.worldToLocalMatrix }
            });

            var renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = bones;
            renderer.rootBone = hips;

            return item;
        }

        /// <summary>
        /// A renderer under <paramref name="parent"/> skinned to the character's Hips, Spine and
        /// Head, one triangle per bone pair. <paramref name="meshOrigin"/> is where the mesh space
        /// sits in the world, so renderers can be exported in different mesh spaces.
        /// </summary>
        public SkinnedMeshRenderer CreateSkinnedRenderer(OutfitSkeleton skeleton, string rendererName, Transform parent, Material material, Vector3 meshOrigin = default, Vector2 uvOffset = default)
        {
            skeleton.TryGetBone("Hips", out var hips);
            skeleton.TryGetBone("Spine", out var spine);
            skeleton.TryGetBone("Head", out var head);

            var meshToWorld = Matrix4x4.Translate(meshOrigin);
            var worldPositions = new[]
            {
                new Vector3(0f, 1f, 0f), new Vector3(0.2f, 1.3f, 0f), new Vector3(0f, 1.5f, 0.1f), new Vector3(0.1f, 1.8f, 0f)
            };
            var vertices = new Vector3[worldPositions.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = worldPositions[i] - meshOrigin;
            }

            var mesh = Track(new Mesh
            {
                vertices = vertices,
                normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
                uv = new[] { uvOffset + new Vector2(0.1f, 0.1f), uvOffset + new Vector2(0.9f, 0.1f), uvOffset + new Vector2(0.5f, 0.5f), uvOffset + new Vector2(0.5f, 0.9f) },
                triangles = new[] { 0, 1, 2, 1, 3, 2 },
                boneWeights = new[]
                {
                    new BoneWeight { boneIndex0 = 0, weight0 = 1f },
                    new BoneWeight { boneIndex0 = 1, weight0 = 0.5f, boneIndex1 = 0, weight1 = 0.5f },
                    new BoneWeight { boneIndex0 = 1, weight0 = 1f },
                    new BoneWeight { boneIndex0 = 2, weight0 = 1f }
                },
                bindposes = new[]
                {
                    hips.worldToLocalMatrix * meshToWorld, spine.worldToLocalMatrix * meshToWorld, head.worldToLocalMatrix * meshToWorld
                }
            });

            var meshObject = new GameObject(rendererName);
            meshObject.transform.SetParent(parent, false);

            var renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = new[] { hips, spine, head };
            renderer.rootBone = hips;
            renderer.sharedMaterial = material;

            return renderer;
        }

        public Material CreateMaterial(string materialName, Color color)
        {
            var texture = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = materialName });
            var pixels = new Color[64];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var material = Track(new Material(Shader.Find("Unlit/Texture")) { name = materialName });
            material.mainTexture = texture;

            return material;
        }

        /// <summary>World position of a vertex as the GPU would skin it.</summary>
        public static Vector3 Skin(SkinnedMeshRenderer renderer, int vertex)
        {
            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var weights = mesh.GetAllBoneWeights();
            var bindposes = mesh.bindposes;
            var bones = renderer.bones;
            var position = mesh.vertices[vertex];
            var start = 0;

            for (var i = 0; i < vertex; i++)
            {
                start += bonesPerVertex[i];
            }

            var result = Vector3.zero;

            for (var k = 0; k < bonesPerVertex[vertex]; k++)
            {
                var weight = weights[start + k];
                result += weight.weight * (bones[weight.boneIndex].localToWorldMatrix * bindposes[weight.boneIndex]).MultiplyPoint3x4(position);
            }

            return result;
        }

        public GameObject CreateRigidItem(string itemName, Vector3 localOffset)
        {
            var item = Track(new GameObject(itemName));
            item.transform.localPosition = localOffset;
            return item;
        }

        public Transform CreateSocket(OutfitSkeleton skeleton, string boneName, OutfitSlot slot, string socketId = null)
        {
            skeleton.TryGetBone(boneName, out var bone);

            var socket = CreateChild(slot.DisplayName + socketId + "Socket", bone, new Vector3(0f, 0.1f, 0f));
            var component = socket.gameObject.AddComponent<OutfitSocket>();
            component.Slot = slot;
            component.SocketId = socketId;
            skeleton.Rebuild();

            return socket;
        }

        public Renderer CreateBodyPart(OutfitSkeleton skeleton, OutfitBodyPart bodyPart)
        {
            var part = new GameObject(bodyPart.DisplayName, typeof(MeshRenderer));
            part.transform.SetParent(skeleton.transform, false);
            part.AddComponent<OutfitBodyPartRenderer>().BodyPart = bodyPart;
            skeleton.Rebuild();

            return part.GetComponent<Renderer>();
        }

        public T Track<T>(T target) where T : Object
        {
            _objects.Add(target);
            return target;
        }

        public void Dispose()
        {
            foreach (var target in _objects)
            {
                if (target != null) Object.DestroyImmediate(target);
            }

            _objects.Clear();
        }

        private static Transform CreateChild(string childName, Transform parent, Vector3 localPosition)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }
    }
}
