"""Generates the art of the Dress-Up sample with MPFB (MakeHuman for Blender).

Requirements: Blender 4.2+, the MPFB 2 extension and the MakeHuman system assets pack
(makehuman_system_assets_cc0.zip, CC0) loaded into MPFB. Run headless:

    Blender --background --python generate_dressup_assets.py -- <output folder>

Writes:
- Character.fbx: the game_engine rig in T-pose (for Unity's Humanoid retargeting), the body
  split into parts that clothes can hide (Body, Body_TorsoAndLegs, Body_Arms, Body_Feet)
  and the eyes. The sample animates it with a Humanoid clip (Animation/Idle.anim).
- One FBX per skinned item (suits and shoes). Every item carries its own copy of the rig,
  the way clothes are exported for rebinding to the character's skeleton at runtime.
- One FBX per hat. Hats are rigid (no rig) and stay where they sit on the head, so the
  Unity side can derive the hat socket from them.
- The JPEG textures next to the FBX files (Unity finds them by name and builds the
  materials for the project's render pipeline).
- PhotoZone.fbx: a concave cylinder (normals facing inward) whose floor curves into the
  wall through a wide rounded bevel, a seamless backdrop for the character.
- body_parts.json: which body parts every item hides.
"""

import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix

from bl_ext.blender_org.mpfb.services.exportservice import ExportService
from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.locationservice import LocationService
from bl_ext.blender_org.mpfb.services.rigservice import RigService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService

OUTPUT = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
TEXTURES = os.path.join(OUTPUT, "_source_textures")
DATA = LocationService.get_user_data()
TEXTURE_SIZE = 1024
MPFB_DATA = os.path.dirname(os.path.dirname(os.path.abspath(sys.modules[RigService.__module__].__file__)))
T_POSE = os.path.join(MPFB_DATA, "data", "poses", "game_engine_fk", "t-pose.json")

# Item name in the sample -> (MakeHuman asset, long sleeves).
SUITS = {
    "DenimShirt": ("male_casualsuit01", True),
    "TShirtJeans": ("male_casualsuit04", False),
    "BlackSuit": ("male_elegantsuit01", True),
    "Overalls": ("male_worksuit01", False),
}
SHOES = {"BrownShoes": "shoes01", "Boots": "shoes03", "Sneakers": "shoes06"}
HATS = {"Fedora": "fedora01", "CockedFedora": "fedora_cocked"}
SKIN = "skins/young_caucasian_male/young_lightskinned_male_diffuse.png"
EYES = "eyes/low-poly/low-poly.mhclo"
EYE_TEXTURE = "eyes/materials/brown_eye.png"


def asset_path(folder, name):
    return os.path.join(DATA, folder, name, name + ".mhclo")


def diffuse_of(mhclo_path):
    folder = os.path.dirname(mhclo_path)
    for file in os.listdir(folder):
        if file.endswith(".mhmat"):
            with open(os.path.join(folder, file)) as mhmat:
                for line in mhmat:
                    if line.startswith("diffuseTexture"):
                        return os.path.join(folder, line.split(None, 1)[1].strip())
    raise FileNotFoundError(f"No diffuse texture next to {mhclo_path}")


def textured_material(name, texture_path):
    """A plain Principled material with a downscaled base color texture, which FBX and Unity understand."""
    image = bpy.data.images.load(texture_path)
    width, height = image.size
    scale = TEXTURE_SIZE / max(width, height)
    if scale < 1:
        image.scale(int(width * scale), int(height * scale))
    os.makedirs(TEXTURES, exist_ok=True)
    settings = bpy.context.scene.render.image_settings
    settings.file_format, settings.quality, settings.color_mode = "JPEG", 85, "RGB"
    jpeg_path = os.path.join(TEXTURES, name + ".jpg")
    image.save_render(jpeg_path)
    bpy.data.images.remove(image)
    image = bpy.data.images.load(jpeg_path)

    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Roughness"].default_value = 0.8
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    material.node_tree.links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    return material


def set_material(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0


def detach(obj):
    """Unparents and bakes the object transform into the mesh. Skinning stays on the Armature modifier."""
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.data.transform(world)
    obj.matrix_world = Matrix.Identity(4)


def hidden_vertices(body, modifier):
    group = body.vertex_groups[modifier.vertex_group]
    members = {v.index for v in body.data.vertices if any(g.group == group.index and g.weight > 0 for g in v.groups)}
    return members if modifier.invert_vertex_group else {v.index for v in body.data.vertices} - members


def split_body(body, regions):
    """Splits the body into one object per region; faces outside every region stay in 'Body'."""
    # Custom normals keep the shading smooth across the cuts.
    body.data.normals_split_custom_set_from_vertices([v.normal for v in body.data.vertices])

    parts = {}
    for region_name, vertices in regions.items():
        part = body.copy()
        part.data = body.data.copy()
        part.name = part.data.name = "Body_" + region_name
        bpy.context.scene.collection.objects.link(part)
        keep_faces(part, lambda face: all(v.index in vertices for v in face.verts))
        parts[region_name] = part

    # Every face goes to exactly one object: a face that straddles two regions stays in the body.
    keep_faces(body, lambda face: not any(all(v.index in vertices for v in face.verts) for vertices in regions.values()))
    body.name = body.data.name = "Body"
    return parts


def keep_faces(obj, predicate):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not predicate(f)], context="FACES_ONLY")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()


def create_photo_zone(radius=3.2, height=3.6, bevel=1.1, segments=96, bevel_steps=16):
    """A surface of revolution: floor disk, rounded bevel, wall. Faces point toward the axis."""
    profile = [(r, 0.0) for r in (0.35, 0.8, 1.3, radius - bevel)]
    for step in range(1, bevel_steps + 1):
        angle = -math.pi / 2 + (math.pi / 2) * step / bevel_steps
        profile.append((radius - bevel + bevel * math.cos(angle), bevel + bevel * math.sin(angle)))
    profile += [(radius, z) for z in (1.8, 2.6, height)]

    vertices = [(0.0, 0.0, 0.0)]
    for r, z in profile:
        for j in range(segments):
            theta = 2 * math.pi * j / segments
            vertices.append((r * math.cos(theta), r * math.sin(theta), z))

    def index(ring, j):
        return 1 + ring * segments + j % segments

    # The winding makes the normals point up on the floor and toward the axis on the wall.
    faces = [(0, index(0, j), index(0, j + 1)) for j in range(segments)]
    for ring in range(len(profile) - 1):
        for j in range(segments):
            faces.append((index(ring, j), index(ring + 1, j), index(ring + 1, j + 1), index(ring, j + 1)))

    mesh = bpy.data.meshes.new("PhotoZone")
    mesh.from_pydata(vertices, [], faces)
    for polygon in mesh.polygons:
        polygon.use_smooth = True

    material = bpy.data.materials.new("M_PhotoZone")
    material.use_nodes = True
    bsdf = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    # HSB (0, 0, 16%). Unity reads the FBX colour as is into its sRGB material colour,
    # so the value is written unconverted (Blender shows it lighter).
    bsdf.inputs["Base Color"].default_value = (0.16, 0.16, 0.16, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.9
    mesh.materials.append(material)

    zone = bpy.data.objects.new("PhotoZone", mesh)
    bpy.context.scene.collection.objects.link(zone)
    return zone


def apply_t_pose(rig):
    """Makes the MakeHuman T-pose the rest pose of the rig, the body and every fitted item."""
    with open(T_POSE) as file:
        pose = json.load(file)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="POSE")
    RigService.set_pose_from_dict(rig, pose)
    bpy.ops.object.mode_set(mode="OBJECT")
    RigService.apply_pose_as_rest_pose(rig)


def export(file_name, objects, rig=None):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    if rig:
        rig.select_set(True)
        rig.data.pose_position = "REST"
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUTPUT, file_name + ".fbx"),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=False,
        mesh_smooth_type="OFF",
        use_custom_props=False,
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        armature_nodetype="NULL",
        path_mode="COPY",
        embed_textures=False,
        bake_anim=False,
    )
    print("Exported", file_name)


def main():
    os.makedirs(OUTPUT, exist_ok=True)
    bpy.ops.wm.read_homefile(use_empty=True)

    macro = TargetService.get_default_macro_info_dict()
    macro["gender"] = 1.0
    body = HumanService.create_human(macro_detail_dict=macro)
    HumanService.add_builtin_rig(body, "game_engine")
    rig = body.parent
    rig.name = rig.data.name = "Rig"

    eyes = HumanService.add_mhclo_asset(os.path.join(DATA, EYES), body, asset_type="Eyes", subdiv_levels=0)
    eyes.name = eyes.data.name = "Eyes"

    items = {}
    for item_name, asset in list({n: a for n, (a, _) in SUITS.items()}.items()) + list(SHOES.items()) + list(HATS.items()):
        path = asset_path("clothes", asset)
        item = HumanService.add_mhclo_asset(path, body, asset_type="Clothes", subdiv_levels=0)
        item.name = item.data.name = item_name
        set_material(item, textured_material("M_" + item_name, diffuse_of(path)))
        items[item_name] = item

    apply_t_pose(rig)

    # Bake the body shape and drop the helper geometry; the clothes keep their fitted shape.
    delete_masks = {m.name: m for m in body.modifiers if m.type == "MASK" and m.vertex_group != "body"}
    ExportService.bake_modifiers_remove_helpers(body, bake_masks=False, remove_helpers=True, also_proxy=False)
    # Removing the helpers re-applies the shape keys; bake them, or the FBX carries the neutral basis mesh.
    TargetService.bake_targets(body)
    hidden = {}
    for item_name, asset in list({n: a for n, (a, _) in SUITS.items()}.items()) + list(SHOES.items()):
        modifier = next(m for m in body.modifiers if m.type == "MASK" and asset in m.vertex_group)
        hidden[item_name] = hidden_vertices(body, modifier)
    for modifier in [m for m in body.modifiers if m.type == "MASK"]:
        body.modifiers.remove(modifier)

    # A body part is hidden only where every item that hides it covers the skin.
    torso_and_legs = set.intersection(*(hidden[n] for n in SUITS))
    arms = set.intersection(*(hidden[n] for n, (_, long_sleeves) in SUITS.items() if long_sleeves)) - torso_and_legs
    feet = set.intersection(*(hidden[n] for n in SHOES)) - torso_and_legs - arms
    split_body(body, {"TorsoAndLegs": torso_and_legs, "Arms": arms, "Feet": feet})
    body_parts = [o for o in bpy.data.objects if o.name == "Body" or o.name.startswith("Body_")]

    skin = textured_material("M_Skin", os.path.join(DATA, SKIN))
    for part in body_parts:
        set_material(part, skin)
    set_material(eyes, textured_material("M_Eyes", os.path.join(DATA, EYE_TEXTURE)))

    for obj in body_parts + [eyes] + list(items.values()):
        detach(obj)
    for hat_name in HATS:
        hat = items[hat_name]
        hat.modifiers.clear()
        hat.vertex_groups.clear()

    export("Character", body_parts + [eyes], rig)
    for item_name in list(SUITS) + list(SHOES):
        export(item_name, [items[item_name]], rig)
    for hat_name in HATS:
        export(hat_name, [items[hat_name]])
    export("PhotoZone", [create_photo_zone()])

    hides = {name: ["TorsoAndLegs"] + (["Arms"] if long_sleeves else []) for name, (_, long_sleeves) in SUITS.items()}
    hides.update({name: ["Feet"] for name in SHOES})
    with open(os.path.join(OUTPUT, "body_parts.json"), "w") as file:
        json.dump(hides, file, indent=2)

    print("Bones:", len(rig.data.bones), "| body parts:", {p.name: len(p.data.vertices) for p in body_parts},
          "| items:", {n: len(i.data.vertices) for n, i in items.items()})


main()
