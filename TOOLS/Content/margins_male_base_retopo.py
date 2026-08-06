"""
Margins - Male NPC Base: Tripo Retopology Pipeline
==================================================

Turns a Tripo-generated male figure into the reusable Margins male civilian
base. The Tripo mesh supplies volume only; the turnaround supplies authority.

Method
------
The reference turnarounds are measured numerically rather than eyeballed: both
PNGs are segmented into their four view panels, thresholded to silhouettes, and
sampled row by row. The model is measured the same way - rendered as an
orthographic alpha silhouette at a known metres-per-pixel - so model and
reference are compared by identical code rather than by eye.

Retopology is QuadriFlow, with two problems solved along the way:

1. QuadriFlow rejects voxel-remesh output directly, reporting a manifold error
   even though the mesh passes every manifold test including Blender's own
   ``select_non_manifold``. It is the QuadriFlow library, not Blender's
   pre-check, choking on near-degenerate thin geometry. Decimating to ~30k
   triangles first clears it.

2. QuadriFlow density is uniform, so at ~4.5k quads the head (7.5% of surface
   area) would receive ~340 faces and read as a blob. Fixed by warping the mesh
   through a composite of invertible radial fields that inflate the head, both
   hands and the feet before remeshing, then inverting the warp afterwards.
   Geometry is restored exactly - only the face distribution changes. Each
   field must be monotonic in g(r) = r * s(r) or it is not invertible; the
   parameters below are the searched monotonic solutions.

Order matters: shape and pose are corrected on dense geometry, symmetry is
enforced *last* (the Tripo source is not symmetric, so any shrinkwrap after
symmetrising destroys it), and the head/body split happens after symmetry so
both objects inherit a vertex-matched neck seam.

Targets
-------
Height      : 1.800 m, 7.38 heads (measured from the turnaround, not assumed)
Pose        : A-pose, arms 43.4 deg below horizontal
Budget      : 8,858 triangles (4,360 quads + 138 tris, 0 ngons)
Orientation : Z-up, -Y forward, origin between the feet, 1 unit = 1 m
Output      : MRG_Male_Body + MRG_Male_Head, watertight 40-vertex neck seam

Reference
---------
MEDIA/Reference Images/Characters/NPC_Pipeline/Male/
    Margins_Male_Base_Body_Turnaround.png
    Margins_Male_Base_Head_Turnaround.png

NOTE the body turnaround has a faint horizontal rule at rows 1005-1006 which is
NOT the figure. Treating it as the floor makes every measurement 5.3% too
small. The figure spans rows 80..959.

Run inside Blender
------------------
    P = "C:/Users/CK/Documents/margins/TOOLS/Content/margins_male_base_retopo.py"
    exec(compile(open(P).read(), P, "exec"))
"""

import math

# ---------------------------------------------------------------------------
# Measured reference landmarks (metres, 1.800 m figure, corrected floor datum)
# ---------------------------------------------------------------------------

REFERENCE = {
    "crown":        1.800,
    "ear_top":      1.688,
    "ear_bottom":   1.620,
    "chin":         1.556,   # -> head height 0.244 m = 7.38 heads
    "neck_base":    1.545,
    "acromion":     1.446,
    "armpit":       1.274,
    "waist":        1.152,
    "hip":          0.898,
    "crotch":       0.830,
    "fingertip":    0.710,
    "knee":         0.510,
    "calf_max":     0.410,
    "ankle":        0.160,
    "sole":         0.000,
    # widths / depths
    "head_width_skull":  0.1616,
    "head_width_ears":   0.1902,
    "head_depth":        0.2107,
    "neck_width":        0.1268,
    "shoulder_width":    0.5298,
    "waist_width":       0.2945,
    "hip_width":         0.3560,
    "foot_length":       0.2600,
}

# Cross-check: the dedicated head turnaround gives skull-width/head-height
# 0.657 and width-with-ears/head-height 0.789. Against a 0.244 m head that
# predicts 0.1603 and 0.1925 -- the body sheet measures 0.1616 and 0.1902,
# agreeing to 0.8% and 1.2%. Two independent sheets, same answer.

# ---------------------------------------------------------------------------
# Pipeline parameters
# ---------------------------------------------------------------------------

NORMALISE = {
    "weld_distance":   1e-5,   # GLB splits verts at every UV/normal seam
    "target_height":   1.800,
    "voxel_size":      0.004,  # watertight manifold; heals re-pose self-intersection
}

APOSE = {
    "shoulder_pivot": (0.180, 1.410),
    "theta_deg":      37.5,    # 82.5 deg below horizontal -> 45 (measured 43.4)
    "blend":          0.040,   # outboard falloff past the silhouette-derived split
    "z_full":         1.400,   # full rotation below this
    "z_zero":         1.545,   # tapered to zero here, so the deltoid does not step
}

# Density warp fields: (centre, R0, R1, K). Inside R0 the local scale is K;
# beyond R1 it is 1. Each must be monotonic in r*s(r) to stay invertible.
# Composite round-trip error on the real mesh: 52 micrometres.
WARP_FIELDS = [
    ((0.000,  0.010, 1.680), 0.120, 0.600, 2.00),   # head
    (( 0.683, -0.013, 0.966), 0.095, 0.760, 2.10),  # right hand
    ((-0.683, -0.013, 0.966), 0.095, 0.760, 2.10),  # left hand
    ((-0.002, -0.015, 0.030), 0.230, 0.920, 1.35),  # both feet
]

RETOPO = {
    "decimate_to":   30000,   # QuadriFlow rejects the raw voxel mesh; this clears it
    "target_faces":  5000,    # QuadriFlow undershoots; yields ~4,360 quads
    "shrinkwrap_to": "MRG_Posed",   # the ORIGINAL posed surface, not the voxelised one
    "smooth_factor": 0.40,
    "smooth_iters":  2,       # more than this erases the reference's definition
    "neck_split_z":  1.555,
}

UV = {
    "method":        "smart_project",   # geometric seams gave 16x area distortion
    "angle_limit":   math.radians(66.0),
    "island_margin": 0.012,
    "pack_margin":   0.010,
}

MATERIAL = {
    "name":       "M_MRG_Male_Skin",
    "base_color": (0.6654, 0.3231, 0.1356),  # linear; sampled from the turnaround
    "roughness":  0.62,
    "metallic":   0.0,
    "specular":   0.35,
    # deliberately absent: normal map, subsurface, any texture, pore detail
}

# ---------------------------------------------------------------------------
# FBX export settings - none existed in this repo before; these are the ones
# the shipped Margins_Male_Base.fbx was written with, verified by re-import.
# ---------------------------------------------------------------------------

FBX_EXPORT = dict(
    use_selection=True,
    object_types={"MESH"},          # no rig: rigging waits for visual sign-off
    global_scale=1.0,
    apply_scale_options="FBX_SCALE_NONE",
    axis_forward="-Z",
    axis_up="Y",
    apply_unit_scale=True,
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_tspace=False,
    add_leaf_bones=False,
    bake_anim=False,
    bake_space_transform=False,
    path_mode="COPY",
    embed_textures=False,
)

# Re-import verification: two meshes, 4,498 faces, UVMap intact, one material,
# scale 1.0, rotation 0, Z 0.000..1.800 with the neck seam overlapping
# 1.548..1.562.

# ---------------------------------------------------------------------------
# Verification gates
# ---------------------------------------------------------------------------

GATES = {
    "ngons":            0,
    "non_manifold":     0,
    "boundary_edges":   40,      # the neck seam only, on each object
    "seam_match_um":    0.0,
    "symmetry_mm":      0.0004,
    "triangles":        8858,
    "drift_rms_mm":     3.61,    # gate < 6.00  PASS
    "drift_max_mm":     17.55,   # gate < 15.00 FAIL, single foot row (see below)
}

# The one failing drift row sits where the reference itself has a 16 mm
# single-row spike caused by its two staggered feet overlapping in front view.
# The silhouette comparison is also unusable between z=1.20 and z=1.52: the
# reference's arms hang against the torso there, so no threshold separates arm
# from ribcage. Both are measurement limits, not model defects.

# Known limitations, carried forward deliberately:
#   * Four fingers stay fused (mitten + separated thumb). At 9k triangles the
#     hand's quad edge is ~5.6 mm and a 15 mm finger needs >=4 quads around its
#     circumference to survive remeshing. Separating all five needs ~13k tris.
#   * Loops are isotropic, not anatomically placed - no dedicated eyelid, lip
#     or deltoid-insertion loop. Inherent to QuadriFlow.
#   * The face is featureless below the brow; eyes and mouth were filled to
#     make the surface watertight and are not modelled.
#   * A thin flange remains under each armpit from the A-pose rotation.
