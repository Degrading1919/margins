"""
Margins - Male NPC Base: Tripo Retopology + Authored Hands
===========================================================

Turns a Tripo-generated male figure into the reusable Margins male civilian
base, styled toward Road 96: simplified planes, readable silhouette, no
photoreal skin detail.

Method
------
The reference turnarounds are measured numerically rather than eyeballed: both
PNGs are segmented into their four view panels, thresholded to silhouettes, and
sampled row by row. The model is measured the same way - rendered as an
orthographic alpha silhouette at a known metres-per-pixel - so model and
reference are compared by identical code rather than by eye.

Body retopology is QuadriFlow, with two problems solved along the way:

1. QuadriFlow rejects voxel-remesh output directly, reporting a manifold error
   even though the mesh passes every manifold test including Blender's own
   ``select_non_manifold``. It is the QuadriFlow library, not Blender's
   pre-check, choking on near-degenerate thin geometry. Decimating to ~70k
   triangles first clears it.

2. QuadriFlow density is uniform, so the head (7.5% of surface area) would read
   as a blob. Fixed by warping the mesh through a composite of invertible
   radial fields before remeshing and inverting the warp afterwards. Geometry
   is restored exactly - only the face distribution changes. Each field must be
   monotonic in g(r) = r * s(r) or it is not invertible.

Hands are NOT remeshed - they are authored (see HANDS below).

Order matters: shape and pose are corrected on dense geometry, hands are
grafted after retopology, symmetry is enforced after any operation that can
break it, and the head is joined to the body last.

Targets
-------
Height      : 1.800 m, 7.38 heads (measured from the turnaround, not assumed)
Pose        : A-pose, arms 43.4 deg below horizontal
Budget      : 13,478 triangles (6,648 quads + 182 tris, 0 ngons)
              Ceiling is 14,300: the 18,000 LOD0 character limit minus
              clothing (2,500) and hair (1,200).
Orientation : Z-up, -Y forward, origin between the feet, 1 unit = 1 m
Output      : ONE object, MRG_Male_Base, closed (0 boundary edges),
              two material slots (skin + eye)

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
    "crown": 1.800, "ear_top": 1.688, "ear_bottom": 1.620,
    "chin": 1.556,            # -> head height 0.244 m = 7.38 heads
    "neck_base": 1.545, "acromion": 1.446, "armpit": 1.274,
    "waist": 1.152, "hip": 0.898, "crotch": 0.830, "fingertip": 0.710,
    "knee": 0.510, "calf_max": 0.410, "ankle": 0.160, "sole": 0.000,
    "head_width_skull": 0.1616, "head_width_ears": 0.1902,
    "head_depth": 0.2107, "neck_width": 0.1268,
    "shoulder_width": 0.5298, "waist_width": 0.2945,
    "hip_width": 0.3560, "foot_length": 0.2600,
}
# Cross-check: the dedicated head turnaround gives skull-width/head-height
# 0.657 and width-with-ears/head-height 0.789. Against a 0.244 m head that
# predicts 0.1603 and 0.1925 -- the body sheet measures 0.1616 and 0.1902,
# agreeing to 0.8% and 1.2%. Two independent sheets, same answer.

NORMALISE = {
    "weld_distance": 1e-5,    # GLB splits verts at every UV/normal seam
    "target_height": 1.800,
    "voxel_size": 0.0022,     # 0.004 was measurably worse for facial detail
                              # (dihedral mean 15.9 vs 11.9)
}

APOSE = {
    "shoulder_pivot": (0.180, 1.410),
    "theta_deg": 37.5,        # 82.5 deg below horizontal -> 45 (measured 43.4)
    "blend": 0.040,
    "z_full": 1.400,
    "z_zero": 1.545,          # tapered, or the deltoid steps
}

# (centre, R0, R1, K). Inside R0 the local scale is K; beyond R1 it is 1.
# Composite round-trip error on the real mesh: ~50 micrometres.
WARP_FIELDS = [
    ((0.000, 0.010, 1.680), 0.120, 0.600, 2.00),   # head
    ((0.683, -0.013, 0.966), 0.095, 0.380, 1.90),  # right hand region
    ((-0.683, -0.013, 0.966), 0.095, 0.380, 1.90), # left hand region
    ((-0.002, -0.015, 0.030), 0.230, 0.690, 1.40), # both feet
]
# Raising K forces a wider R1 to stay monotonic, which spreads the inflation
# over the whole body and defeats localisation. A hand field at K=2.3 needed
# R1=1.2 and starved the head to 165 faces. Keep the fields roughly disjoint.

RETOPO = {
    "decimate_to": 70000,
    "target_faces": 7500,     # NON-MONOTONIC: 7350->14,986 tris but
                              # 7500->13,654. Sweep candidates, never interpolate.
    "shrinkwrap_to": "MRG_Posed",   # the ORIGINAL posed surface, not the voxelised one
    "smooth_factor": 0.35, "smooth_iters": 2,
    "foot_widen_iters": 3,    # iterative: scaling changes the measurement
}

# ---------------------------------------------------------------------------
# Authored hands
# ---------------------------------------------------------------------------
# The Tripo source hand is a mitten - thumb, a part-separated index finger, and
# three fingers merged. Verified on REF_Tripo_Original before any processing.
# Raising the budget and refining the voxel both left it unchanged: there are
# no fingers upstream to preserve. So the hand is cut off at the wrist and
# rebuilt: a 12-vertex slab palm whose knuckle ring caps into exactly five
# quads (four fingers plus the thumb web), with the thumb grown from the
# mid-palm side wall. ~250 quads per hand for five real fingers, versus ~700
# for a remeshed mitten.
#
# The wrist boundary left by QuadriFlow is 22 verts and the palm ring is 12, so
# the join uses bmesh.ops.bridge_loops. That pairs the loops independently per
# side and WILL break X symmetry - symmetrise afterwards, always.

# The forearm QuadriFlow produces does NOT taper - at the wrist it is 61 mm
# across the thickness axis where a palm is 42 mm. Bridging a 22-vertex arm
# loop straight onto a 12-vertex palm ring therefore pinches badly. Taper the
# forearm into the wrist FIRST (see FOREARM_TAPER) and size palm ring 0 to
# match, then the bridge is a short clean band.
FOREARM_TAPER = {"t_range": (0.40, 0.62), "target_half_up": 0.0255,
                 "target_half_fw": 0.0300, "relax_band": (0.52, 0.66)}

HANDS = {
    "wrist_t": 0.600,          # along the arm axis from the shoulder pivot
    "palm_rings": [            # (distance, half_width, half_thick, round)
        (0.000, 0.0300, 0.0258, 0.004), (0.040, 0.0400, 0.0215, 0.010),
        (0.076, 0.0468, 0.0178, 0.008), (0.106, 0.0472, 0.0156, 0.005)],
    "fingers": [               # (name, length, splay, root scale)
        ("Little", 0.056, -0.20, 0.86), ("Ring", 0.073, -0.06, 0.96),
        ("Middle", 0.080, 0.02, 1.00), ("Index", 0.073, 0.11, 0.96)],
    # Digits are extruded as 4-sided tubes, which read as hard square rods. Fix:
    # collect the faces digit() creates and run
    #   bmesh.ops.subdivide_edges(edges=those, cuts=1, smooth=1.0, use_grid_fill=True)
    # which turns them into rounded 8-sided tubes. Build with 2 segments, not 3 -
    # subdivision doubles them - and lengthen ~15%, because smooth subdivision
    # pulls the tips in and leaves them stubby. Costs ~490 tris for both hands.
    "digit_segments": 2, "round_subdivide": dict(cuts=1, smooth=1.0),
    "finger_curl_deg": -9.0,
    "thumb_dir": (0.60, 0.66, 0.34),   # along (axis, width, -thickness)
    "thumb_segs": [0.028, 0.023, 0.017],
    # palm faces the thigh: thickness axis = arm "up", width axis = -Y (thumb forward)
    #
    # cap5 splits the knuckle ring into FIVE spans: four fingers plus the thumb
    # web. Capping that fifth span flat leaves a blunt NUB sticking out past the
    # index finger. Fix: before capping, pull the knuckle ring's thumb-side
    # corner verts (ring indices 5 and 6) back toward the wrist so the span
    # slopes into a real web. Ring 2's same corners get a smaller pull-back so
    # the thenar reads.
    "web_pullback": dict(knuckle_axis=0.0335, knuckle_width=0.0052,
                         knuckle_thick=0.0016, midpalm_axis=0.0090,
                         midpalm_width=0.0018),
    # Palm musculature is pure vertex displacement on the existing rings - it
    # costs zero triangles. Ring indices: BOT[5]=6 and BOT[4]=7 are the palmar
    # thumb side, BOT[0]=11 and BOT[1]=10 the palmar little-finger side,
    # 8 and 9 the palm centre.
    "thenar":     dict(rings=(1, 2), amounts=(0.0062, 0.0050), width_out=0.0022),
    "hypothenar": dict(rings=(1, 2), amounts=(0.0042, 0.0034), width_out=0.0016),
    "palm_hollow": dict(rings=(1, 2), amount=(0.0026, 0.0030)),
    "knuckle_rise": 0.0022,      # dorsal lift on the whole knuckle ring
}

# Fingernails are PAINTED, not modelled. digit() returns its final centre,
# direction and dorsal axis; those become nail anchors consumed by the texture
# pass, which paints an ellipse at (tip - dir*0.0085) restricted to the dorsal
# half. Anchors are recorded per-hand BEFORE symmetrise, so fold every anchor to
# +X (negate x on the position and on all three axis vectors) before painting -
# the texture pass works in |x| space and signed anchors on the left hand simply
# never match.
NAILS = dict(setback=0.0085, size_scale=1.22, dorsal_cutoff=0.0008,
             cuticle_frac=0.88, roughness_drop=0.24)

BICEP = {
    # the upper-arm underside sagged after the A-pose rotation
    "t_range": (0.04, 0.40), "under_scale": -0.16, "over_scale": 0.05,
}

FEET = {
    # deliberately mittens - always covered by shoes. Smoothed to round off the
    # pointy retopo toes rather than spending triangles on them.
    "smooth_below_z": 0.075, "iters": 7, "factor": 0.55,
}

# ---------------------------------------------------------------------------
# Face - sculpted, not remeshed
# ---------------------------------------------------------------------------
# Eyes and mouth were open holes in the Tripo source and were filled to make
# the surface watertight, leaving a blank face. Features are displaced back in
# with Gaussian falloffs around measured landmarks. Eye geometry is two low-poly
# spheres seated in deepened almond sockets, on their own material slot.
FACE = {
    "nose": dict(centre_z=1.649, half_x=0.019, sigma_z=0.022, project=0.0135),
    "alae": dict(x=0.011, z=1.629, widen=0.0040),
    "brow": dict(half_x=0.046, z=1.697, sigma=0.011, project=0.0060),
    "eye_socket": dict(x=0.0345, z=1.6800, rx=0.0255, rz=0.0092, recess=0.0062),
    "eye_ball": dict(x=0.0345, z=1.6800, radius=0.0138, proud=0.0028,
                     segments=(10, 6)),
    # The mouth needs finer geometry than QuadriFlow leaves (5 rows at ~11 mm
    # spacing). Subdivide the region once, then carve. The dark band must be
    # narrow and the x-taper long, or the per-face material assignment reads as
    # a rectangular bar with bracket ends rather than a mouth.
    "mouth_subdivide": dict(box_x=0.040, box_z=(1.592, 1.646), cuts=1),
    "mouth": dict(taper_x=0.0300, taper_soft=0.0195, z=1.6170,
                  corner_drop=0.0020, slit=0.0092, sigma=0.0030,
                  upper_lip=0.0034, lower_lip=0.0032,
                  dark_half_x=0.0235, dark_half_z=0.0021),
    "chin": dict(half_x=0.024, z=1.581, project=0.0045),
    "cheek": dict(x=0.055, z=1.663, project=0.0035),
}
# Place eye spheres from the MEASURED socket surface, never a computed one -
# estimating it put the eyeballs 8 mm proud of the face.

# ---------------------------------------------------------------------------
# Texturing - procedural, driven by a baked world-position map
# ---------------------------------------------------------------------------
# There is no hand-painting step. Instead:
#   1. Bake a world-POSITION map (Geometry.Position -> Emission, bake EMIT into
#      a 32-bit float image) and an AO map.
#   2. DILATE the position and AO maps outward past the UV island edges BEFORE
#      painting. Every texel then carries a plausible 3D coordinate, so margin
#      texels paint the correct feature colour. Dilating the finished colour
#      instead leaves skin-coloured notches through the eyebrows.
#   3. Paint in numpy using the same Gaussian/smoothstep falloffs around the
#      same measured landmarks used for the sculpt, so paint and geometry agree.
#
# Never blur a feature mask in TEXTURE space (e.g. box(brow, 2)) - a blur
# smears across UV island boundaries and tears the feature apart. All softness
# must come from the 3D falloff.
TEXTURES = {
    "resolution": 2048,
    "bake": dict(engine="CYCLES", samples=64, margin=24),
    "dilate_iters": 40,
    # Sampled from MEDIA/Reference Images/Margins_Male_Base_Body_Right.png, which
    # is warmer and darker than the turnaround it replaced (was 0.836,0.606,0.408).
    "skin_srgb": (0.769, 0.545, 0.373),
    "ao_strength": 0.66, "tonal_noise": 0.045,
    "features": ["beard shadow", "eyebrows", "eyes (sclera/iris/pupil/lash)",
                 "lid crease", "lips + mouth line", "cheek/nose/ear warmth",
                 "knuckles", "nipples", "navel", "lighter palms and soles"],
    "outputs": ["T_Margins_Male_Base_BaseColor.png (sRGB)",
                "T_Margins_Male_Base_Roughness.png (Non-Color)"],
}

# Eyes and mouth are PAINTED, not modelled. Eyeball spheres were tried and
# removed: a sphere large enough to read pushes through the eyelid (its
# vertical extent exceeds the socket recess), and one small enough to fit is
# barely visible. Painting the almond, iris, pupil and lash line into the base
# colour reads far better at gameplay distance and costs no triangles. The
# mouth keeps its carved slit for shadow but its colour is painted too, which
# is why the asset now has ONE material.

MATERIALS = {
    "M_MRG_Male_Skin": dict(base_color="T_Margins_Male_Base_BaseColor.png",
                            roughness="T_Margins_Male_Base_Roughness.png",
                            metallic=0.0, specular=0.32),
    # deliberately absent: normal map, subsurface, pore or photographic detail
}

UV = {"method": "smart_project",   # geometric seams gave 16x area distortion
      "angle_limit": math.radians(78.0),   # 66 cut islands through the eyebrows
      "island_margin": 0.020, "pack_margin": 0.016}

FBX_EXPORT = dict(
    use_selection=True, object_types={"MESH"}, global_scale=1.0,
    apply_scale_options="FBX_SCALE_NONE", axis_forward="-Z", axis_up="Y",
    apply_unit_scale=True, use_mesh_modifiers=True, mesh_smooth_type="FACE",
    use_tspace=False, add_leaf_bones=False, bake_anim=False,
    bake_space_transform=False, path_mode="COPY", embed_textures=False,
)

# ---------------------------------------------------------------------------
# Review render calibration - DO NOT judge the texture without this
# ---------------------------------------------------------------------------
# The albedo matched the reference exactly while the RENDER came back pale and
# desaturated: (0.875,0.769,0.698) against a reference of (0.765,0.545,0.369).
# Two causes, neither of them the texture:
#   1. Blender's default AgX view transform desaturates as values brighten.
#      Set view_transform="Standard", look="None" for review renders.
#   2. The studio rig was ~4x too hot, pushing skin into the top of the range
#      where AgX greys it out.
# Calibrate numerically: render, sample the figure's median skin sRGB, and scale
# all light energies by (target/measured)^2 until the median luminance lands on
# the reference. Converged in 3 iterations.
REVIEW_LIGHTING = {
    "view_transform": "Standard", "look": "None",
    "key": 211, "fill": 75, "rim": 164,        # were 900 / 320 / 700
    "reference_skin_srgb": (0.765, 0.545, 0.369), "reference_luminance": 0.574,
    "achieved_srgb": (0.753, 0.541, 0.384), "achieved_luminance": 0.575,
    "reference_contrast_p90_p10": 1.341, "achieved_contrast": 1.392,
}

GATES = {
    "triangles": 14076, "ceiling": 14300,
    "ngons": 0, "non_manifold": 0,
    "boundary_edges": 0,        # single closed object since head+body were joined
    "symmetry_mm": 0.0000,
    "material_slots": 1,
}

# Head and body are ONE object. They were two, so the head could be swapped for
# hair modules, but smooth-shading normals cannot cross an object boundary and
# that left a visible crease at the jaw in every render. Hair still binds to the
# head bone at rig time, so the split bought nothing it could not get from the
# skeleton. If a swappable head is needed later, split along the z=1.555 edge
# ring and transfer custom normals across the seam.

# Known limitations:
#   * Finger joints have no modelled bulge - the knuckles read from painted
#     creases only. Nails are painted, so they stay flush under raking light.
#   * Eyes and mouth are painted, not modelled. They read well head-on and at
#     gameplay distance, but painted eyes do not follow a gaze and will flatten
#     at extreme angles; facial animation would need real lid and lip loops
#     plus eyeball geometry.
#   * The texture is 100% procedural. It has no hand-authored detail - no
#     freckles, scars, asymmetry or wear. Every character generated from this
#     base will share the same skin unless the paint parameters are varied.
#   * Body loops are isotropic, not anatomically placed - no dedicated eyelid,
#     lip or deltoid-insertion loop. Inherent to QuadriFlow.
#   * Ears are angular where the retopo left them; not re-sculpted.
#
# Verification note: the silhouette metric measures outline width only. It
# passed a build whose renders were badly faceted, and it cannot see the face,
# the hands, or shading artefacts at all. Always look at the renders.
