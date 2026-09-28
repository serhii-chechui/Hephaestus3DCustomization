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

        public GameObject CreateRigidItem(string itemName, Vector3 localOffset)
        {
            var item = Track(new GameObject(itemName));
            item.transform.localPosition = localOffset;
            return item;
        }

        public Transform CreateSocket(OutfitSkeleton skeleton, string boneName, OutfitSlot slot)
        {
            skeleton.TryGetBone(boneName, out var bone);

            var socket = CreateChild(slot.DisplayName + "Socket", bone, new Vector3(0f, 0.1f, 0f));
            socket.gameObject.AddComponent<OutfitSocket>().Slot = slot;
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
