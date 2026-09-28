"""Extracts the idle clip of the Dress-Up sample from Quaternius' Universal Animation Library (CC0).

Run headless on UAL1_Standard.fbx (the version without root motion):

    Blender --background --factory-startup --python extract_idle.py -- <UAL1_Standard.fbx> <Idle.fbx>

Writes the skeleton with the Idle_Loop clip only, no mesh. Unity imports it as Humanoid and
retargets it onto the MakeHuman character.
"""

import sys

import bpy

SOURCE, TARGET = sys.argv[sys.argv.index("--") + 1:][:2]
CLIP = "Idle_Loop"

bpy.ops.wm.read_homefile(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)

armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
for obj in [o for o in bpy.data.objects if o.type != "ARMATURE"]:
    bpy.data.objects.remove(obj)

idle = next(a for a in bpy.data.actions if a.name.split("|")[-1] == CLIP)
for action in [a for a in bpy.data.actions if a != idle]:
    bpy.data.actions.remove(action)
idle.name = "Idle"
armature.animation_data.action = idle

scene = bpy.context.scene
scene.frame_start, scene.frame_end = (int(f) for f in idle.frame_range)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=TARGET,
    use_selection=True,
    object_types={"ARMATURE"},
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    armature_nodetype="NULL",
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,
)
print("Exported", TARGET, "frames", tuple(idle.frame_range))
