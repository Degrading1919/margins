"""
Margins - Male NPC Base Mesh Builder
====================================

Parametric, script-authored male base for the Margins NPC pipeline.

Method
------
Quad-only Catmull-Clark control cage. Only the +X half is built; a Mirror
modifier plus a live Subdivision Surface modifier produce the smooth mesh
non-destructively. Every vertex position derives from the landmark table in
``P`` below, so proportions stay editable: change a number, re-run, get a new
body. The numbers are visual hypotheses, not approved targets.

Targets
-------
Height      : 1.800 m, 7.75 heads
Pose        : A-pose, arms 45 deg below horizontal, slight elbow/knee bend
Budget      : cage ~1.2k quads -> Subsurf 1 ~9k tris (LOD0)
Orientation : Z-up, -Y forward, origin between the feet, 1 unit = 1 m

Reference
---------
MEDIA/Reference Images/Characters/NPC_Pipeline/Male/
    Margins_Male_Base_Body_Turnaround.png
    Margins_Male_Base_Head_Turnaround.png

Deliberately NOT built here: hair, clothing, textures, materials, UVs.

Run inside Blender
------------------
    P = "C:/Users/CK/Documents/margins/TOOLS/Content/margins_male_base_builder.py"
    exec(compile(open(P).read(), P, "exec"))
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

TAU = math.tau

# ---------------------------------------------------------------------------
# Landmark parameters (metres)
# ---------------------------------------------------------------------------

P = {
    "height": 1.800,
    "heads": 7.75,
    # A-pose
    "arm_angle_deg": 45.0,      # below horizontal
    "upper_arm_len": 0.320,
    "forearm_len": 0.262,
    "elbow_bend_deg": 5.0,      # slight, so IK knows the direction
    "stance_x": 0.095,          # foot centre offset from midline
}

# Normalised half cross-section, front-centre -> +X side -> back-centre.
# x is a fraction of half-width, y a fraction of depth (negative = front).
BASE = [
    (0.00, -1.00),   # 0 front centre (sternum / linea alba)
    (0.42, -0.93),   # 1
    (0.75, -0.72),   # 2  <- armhole front column
    (0.95, -0.40),   # 3
    (1.00,  0.00),   # 4  widest point
    (0.95,  0.40),   # 5  <- armhole back column
    (0.75,  0.72),   # 6
    (0.42,  0.93),   # 7
    (0.00,  1.00),   # 8 back centre (spine)
]
NPROF = len(BASE)

# Torso cross-sections, bottom to top. dz gives per-vertex height offsets so a
# ring can tilt (the shoulder and trapezius are not horizontal slices).
TORSO_RINGS = [
    # z,     w,     df,    db,    dz
    (0.855, 0.150, 0.098, 0.112, None),                     # 0  crotch / pelvis floor
    (0.940, 0.172, 0.106, 0.118, None),                     # 1  hip, widest
    (1.045, 0.158, 0.100, 0.108, None),                     # 2  iliac crest
    (1.115, 0.146, 0.094, 0.100, None),                     # 3  natural waist
    (1.215, 0.155, 0.102, 0.110, None),                     # 4  lower ribs
    (1.280, 0.164, 0.110, 0.115, None),                     # 5  lower chest
    (1.315, 0.172, 0.115, 0.118, None),                     # 6  armpit
    (1.360, 0.176, 0.120, 0.118, None),                     # 7  chest / nipple
    (1.408, 0.180, 0.112, 0.116, None),                     # 8  upper chest
    (1.455, 0.185, 0.095, 0.105,                            # 9  acromion
     [-0.012, -0.010, -0.004, 0.002, 0.006, 0.006, 0.004, 0.002, 0.000]),
    (1.472, 0.112, 0.070, 0.085,                            # 10 trapezius
     [-0.022, -0.019, -0.010, 0.000, 0.008, 0.012, 0.014, 0.014, 0.012]),
    (1.492, 0.072, 0.058, 0.068,                            # 11 neck base
     [-0.034, -0.030, -0.018, -0.004, 0.004, 0.010, 0.014, 0.016, 0.016]),
    (1.512, 0.063, 0.055, 0.060, None),                     # 12 neck seam (shared with head)
]

ARMHOLE_ROWS = (6, 7, 8)     # face rows between rings 6-7, 7-8, 8-9
ARMHOLE_COLS = (2, 3, 4)     # faces spanning profile verts 2..5

# Crotch septum: three verts on the centreline arching under the perineum.
CROTCH = [
    (0.0, -0.045, 0.878),
    (0.0,  0.012, 0.888),
    (0.0,  0.064, 0.880),
]

# Arm rings: (distance along the arm axis from the shoulder pivot, radius_up,
# radius_front). Slightly elliptical, because arms are not round.
ARM_RINGS = [
    (0.055, 0.062, 0.058),   # deltoid
    (0.140, 0.050, 0.048),   # mid upper arm  <- short-sleeve hem
    (0.240, 0.044, 0.042),
    (0.320, 0.039, 0.041),   # elbow
    (0.400, 0.043, 0.040),   # forearm belly
    (0.500, 0.032, 0.031),
    (0.582, 0.021, 0.028),   # wrist          <- glove line; wider than thick
]
NARM = 12

# Leg rings: (z, centre_x, radius_x, radius_y)
LEG_RINGS = [
    (0.800, 0.093, 0.080, 0.084),   # upper thigh
    (0.700, 0.094, 0.073, 0.078),
    (0.660, 0.094, 0.069, 0.075),   # mid thigh   <- shorts hem
    (0.560, 0.095, 0.060, 0.066),
    (0.500, 0.095, 0.053, 0.058),   # knee
    (0.440, 0.095, 0.053, 0.060),
    (0.360, 0.095, 0.055, 0.061),   # mid calf    <- sock line
    (0.260, 0.095, 0.045, 0.050),
    (0.160, 0.095, 0.037, 0.041),
    (0.085, 0.095, 0.035, 0.038),   # ankle
]
NLEG = 12

# Hand rings: (distance from the wrist, half-thickness, half-width). The palm
# flattens by trading thickness for width.
HAND_RINGS = [
    (0.020, 0.017, 0.030),
    (0.058, 0.011, 0.041),
    (0.095, 0.010, 0.042),   # knuckle line
]
THUMB_BAND = 1    # palm band whose radial side wall becomes the thumb socket
THUMB_FACE = 10   # face index on that ring, radial and slightly palmar

# cap5 order runs little finger -> index; the fifth quad is the first web space.
# (length, splay along the width axis, root scale)
FINGER_NAMES = ["Little", "Ring", "Middle", "Index"]
FINGERS = [
    (0.062, -0.20, 0.78),    # little
    (0.078, -0.06, 0.92),    # ring
    (0.085,  0.02, 0.96),    # middle
    (0.079,  0.11, 0.92),    # index
]

# Foot rings: (tilt about X, cx, cy, cz, rx, ry, per-vertex offsets).
# Tilt runs 0 -> -90 deg so the ring plane turns from horizontal at the ankle
# to vertical at the toes. Index 6 faces back-and-down, i.e. the heel.
FOOT_RINGS = [
    (-30.0, 0.095, -0.006, 0.056, 0.037, 0.044,
     {6: (0.0, 0.030, -0.026), 5: (0.0, 0.020, -0.018), 7: (0.0, 0.020, -0.018)}),
    (-60.0, 0.095, -0.030, 0.042, 0.040, 0.038,
     {6: (0.0, 0.016, -0.010)}),
    (-85.0, 0.095, -0.078, 0.030, 0.042, 0.030, None),
    (-90.0, 0.095, -0.128, 0.024, 0.045, 0.024, None),
    (-90.0, 0.095, -0.162, 0.019, 0.041, 0.019, None),   # toe line
]

# cap5 order runs big toe -> little toe: (length, lateral splay, root scale)
TOES = [
    (0.032, -0.10, 1.18),
    (0.028,  0.00, 0.92),
    (0.025,  0.04, 0.84),
    (0.021,  0.08, 0.76),
    (0.017,  0.13, 0.66),
]

P["finger_curl_deg"] = 8.0


# ---------------------------------------------------------------------------
# Mesh building utilities
# ---------------------------------------------------------------------------

class Cage:
    """Accumulates verts and quads, then hands them to a Blender mesh.

    Indices are shared explicitly rather than welded after the fact, so the
    topology is exactly what the code says it is.
    """

    def __init__(self):
        self.verts = []
        self.faces = []

    def add(self, co):
        self.verts.append(Vector(co))
        return len(self.verts) - 1

    def ring(self, points):
        return [self.add(p) for p in points]

    def quad(self, a, b, c, d):
        self.faces.append((a, b, c, d))

    def bridge(self, ring_a, ring_b, closed=True):
        """Bridge two equal-length rings into quads."""
        n = len(ring_a)
        assert len(ring_b) == n, f"ring mismatch {n} vs {len(ring_b)}"
        span = n if closed else n - 1
        for k in range(span):
            k2 = (k + 1) % n
            self.quad(ring_a[k], ring_a[k2], ring_b[k2], ring_b[k])

    def to_object(self, name):
        me = bpy.data.meshes.new(name)
        me.from_pydata([v[:] for v in self.verts], [], self.faces)
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.collection.objects.link(ob)
        return ob


def profile_points(z, w, df, db, dz=None, cx=0.0):
    """One half cross-section: 9 points, front-centre to back-centre."""
    pts = []
    for i, (fx, fy) in enumerate(BASE):
        depth = df if fy < 0.0 else db
        pts.append(Vector((cx + fx * w, fy * depth, z + (dz[i] if dz else 0.0))))
    return pts


def circle_points(centre, up, fwd, n, r_up, r_fwd, theta0, direction=-1.0):
    """A ring of n points in the plane spanned by up/fwd."""
    pts = []
    for k in range(n):
        a = theta0 + direction * (TAU * k / n)
        pts.append(centre + up * (math.cos(a) * r_up) + fwd * (math.sin(a) * r_fwd))
    return pts


def frame_ring(centre, sx, sy, n, rx, ry):
    """Ring in an arbitrary frame; index 0 sits on +sy, winding toward +sx."""
    return [centre + sx * (math.sin(TAU * k / n) * rx)
                   + sy * (math.cos(TAU * k / n) * ry) for k in range(n)]


# A 12-vert ring caps into exactly five quads. Those five become the five toes,
# or four fingers plus the thumb web. The index pattern depends on where the
# ring's vertex 0 sits, so each convention gets its own pair of rows.
CAP5_LEG = ([10, 11, 0, 1, 2, 3], [9, 8, 7, 6, 5, 4])   # frame_ring: 0 on +sy
CAP5_ARM = ([5, 4, 3, 2, 1, 0], [6, 7, 8, 9, 10, 11])   # circle_points theta0=60


def slab_ring(centre, wide_axis, thick_axis, pattern, half_w, half_t):
    """Flattened ring whose cap5 quads come out uniform.

    An ellipse gives wildly uneven cap quads at its extremes - fingers end up
    as thin fins. Spacing the six top and six bottom verts evenly across the
    width instead gives every digit root the same rectangle.
    """
    top, bot = pattern
    pts = [None] * (len(top) + len(bot))
    for i in range(len(top)):
        f = -1.0 + 2.0 * i / (len(top) - 1)
        pts[top[i]] = centre + wide_axis * (f * half_w) + thick_axis * half_t
        pts[bot[i]] = centre + wide_axis * (f * half_w) - thick_axis * half_t
    return pts


def cap5(ring, pattern):
    """Split a 12-vert ring into five quad loops spanning its width."""
    top, bot = pattern
    return [(ring[top[i]], ring[top[i + 1]], ring[bot[i + 1]], ring[bot[i]])
            for i in range(5)]


def extrude_digit(cage, root, direction, seg_len, seg_scale, root_scale=1.0,
                  curl_axis=None, curl_deg=0.0):
    """Grow a finger or toe out of a quad loop, with an optional curl."""
    pts = [cage.verts[i].copy() for i in root]
    origin = sum(pts, Vector()) / len(pts)
    offsets = [p - origin for p in pts]

    d = Vector(direction).normalized()
    rot = Matrix.Identity(3)
    centre = origin.copy()
    prev = list(root)
    joints = [origin.copy()]

    for length, scale in zip(seg_len, seg_scale):
        if curl_axis is not None and curl_deg:
            step = Matrix.Rotation(math.radians(curl_deg), 3, curl_axis)
            rot = step @ rot
            d = (step @ d).normalized()
        centre = centre + d * length
        s = root_scale * scale
        ring = cage.ring([centre + (rot @ o) * s for o in offsets])
        cage.bridge(prev, ring)
        prev = ring
        joints.append(centre.copy())

    cage.quad(*prev)     # flat tip cap
    return joints


# ---------------------------------------------------------------------------
# Body
# ---------------------------------------------------------------------------

def build_torso(cage):
    """Torso tube from the pelvis floor to the neck seam.

    Returns (rings, armhole_loop, leg_loop). The armhole is a 3x3 block of
    faces left unbuilt, which yields a clean 12-edge boundary; the leg hole is
    bounded by the pelvis ring plus the crotch septum, also 12 edges.
    """
    rings = []
    for (z, w, df, db, dz) in TORSO_RINGS:
        rings.append(cage.ring(profile_points(z, w, df, db, dz)))

    for i in range(len(rings) - 1):
        for j in range(NPROF - 1):
            if i in ARMHOLE_ROWS and j in ARMHOLE_COLS:
                continue
            cage.quad(rings[i][j], rings[i][j + 1],
                      rings[i + 1][j + 1], rings[i + 1][j])

    # Armhole boundary, cyclic: top-front -> top-back -> bottom-back -> bottom-front.
    top, bot = ARMHOLE_ROWS[-1] + 1, ARMHOLE_ROWS[0]
    c0, c1 = ARMHOLE_COLS[0], ARMHOLE_COLS[-1] + 1
    armhole = (
        [rings[top][j] for j in range(c0, c1 + 1)]
        + [rings[i][c1] for i in range(top - 1, bot, -1)]
        + [rings[bot][j] for j in range(c1, c0 - 1, -1)]
        + [rings[i][c0] for i in range(bot + 1, top)]
    )

    # Leg hole: pelvis arc outbound, then back along the crotch septum.
    septum = [cage.add(c) for c in CROTCH]
    leg_loop = rings[0][:] + [septum[2], septum[1], septum[0]]

    return rings, armhole, leg_loop


def build_arm(cage, armhole):
    """Loft the arm from the armhole along the A-pose axis."""
    centre = sum((cage.verts[i] for i in armhole), Vector()) / len(armhole)

    ang = math.radians(P["arm_angle_deg"])
    axis_u = Vector((math.cos(ang), 0.0, -math.sin(ang))).normalized()
    up = (Vector((0, 0, 1)) - axis_u * Vector((0, 0, 1)).dot(axis_u)).normalized()
    fwd = axis_u.cross(up).normalized()          # points -Y, i.e. forward

    # Forearm deviates slightly so the elbow has a defined bend direction.
    bend = math.radians(P["elbow_bend_deg"])
    axis_f = (axis_u * math.cos(bend) + fwd * math.sin(bend)).normalized()
    elbow_t = P["upper_arm_len"]
    elbow_c = centre + axis_u * elbow_t

    prev = armhole
    rings = []
    frame = (axis_u, up, fwd)
    for (t, r_up, r_fwd) in ARM_RINGS:
        if t <= elbow_t:
            c = centre + axis_u * t
            u, f = up, fwd
            ax = axis_u
        else:
            c = elbow_c + axis_f * (t - elbow_t)
            u = (Vector((0, 0, 1)) - axis_f * Vector((0, 0, 1)).dot(axis_f)).normalized()
            f = axis_f.cross(u).normalized()
            ax = axis_f
        ring = cage.ring(circle_points(c, u, f, NARM, r_up, r_fwd,
                                       theta0=math.radians(60.0)))
        cage.bridge(prev, ring)
        prev = ring
        rings.append(ring)
        frame = (ax, u, f)

    wrist_centre = sum((cage.verts[i] for i in prev), Vector()) / NARM
    joints = {"Shoulder": centre.copy(), "Elbow": elbow_c.copy(),
              "Wrist": wrist_centre.copy()}
    return rings, prev, wrist_centre, frame, joints


def build_leg(cage, leg_loop):
    """Loft the leg downward from the pelvis opening."""
    sx, sy = Vector((1, 0, 0)), Vector((0, -1, 0))
    prev = leg_loop
    rings = []
    for (z, cx, rx, ry) in LEG_RINGS:
        ring = cage.ring(frame_ring(Vector((cx, 0.0, z)), sx, sy, NLEG, rx, ry))
        cage.bridge(prev, ring)
        prev = ring
        rings.append(ring)
    return rings, prev


def build_hand(cage, wrist_ring, wrist_centre, frame):
    """Palm slab from the wrist ring, four fingers and an opposed thumb.

    The palm flattens by swapping the ring's aspect: thin along `up`
    (dorsal-palmar) and wide along `fwd` (thumb to little finger). A 12-vert
    knuckle ring caps into five quads - four fingers plus the first web space.
    """
    axis, up, fwd = frame
    prev = wrist_ring
    palm = []
    for (t, half_t, half_w) in HAND_RINGS:
        ring = cage.ring(slab_ring(wrist_centre + axis * t, fwd, up,
                                   CAP5_ARM, half_w, half_t))
        # The thumb grows out of the radial side wall, so that face is skipped
        # on the band where the metacarpal actually sits.
        if palm and len(palm) == THUMB_BAND:
            for k in range(NARM):
                if k == THUMB_FACE:
                    continue
                k2 = (k + 1) % NARM
                cage.quad(prev[k], prev[k2], ring[k2], ring[k])
            thumb_root = (prev[THUMB_FACE], prev[(THUMB_FACE + 1) % NARM],
                          ring[(THUMB_FACE + 1) % NARM], ring[THUMB_FACE])
        else:
            cage.bridge(prev, ring)
        prev = ring
        palm.append(ring)

    quads = cap5(prev, CAP5_ARM)
    cage.quad(*quads[4])                     # first web space, capped flat

    curl = -abs(P["finger_curl_deg"])
    joints = {}
    for i, (length, splay, scale) in enumerate(FINGERS):
        segs = [length * f for f in (0.40, 0.33, 0.27)]
        direction = (axis + fwd * splay).normalized()
        joints[FINGER_NAMES[i]] = extrude_digit(
            cage, quads[i], direction, segs, (0.94, 0.88, 0.80),
            root_scale=scale, curl_axis=fwd, curl_deg=curl)

    thumb_dir = (axis * 0.74 + fwd * 0.48 - up * 0.30).normalized()
    joints["Thumb"] = extrude_digit(cage, thumb_root, thumb_dir,
                                    [0.032, 0.028, 0.022], (1.00, 0.92, 0.84),
                                    curl_axis=up, curl_deg=-5.0)
    return joints


def build_foot(cage, ankle_ring):
    """Mitred bend from the ankle to the toe line, then five toes.

    Rings stay perpendicular to a path that turns 90 degrees forward, so the
    heel forms on the outside of the bend with no extra poles.
    """
    sx = Vector((1, 0, 0))
    prev = ankle_ring
    last = len(FOOT_RINGS) - 1
    for i, (theta, cx, cy, cz, rx, ry, offs) in enumerate(FOOT_RINGS):
        rot = Matrix.Rotation(math.radians(theta), 3, "X")
        sy = rot @ Vector((0, -1, 0))
        centre = Vector((cx, cy, cz))
        if i == last:
            # Toe line is a slab so all five toe roots come out the same size.
            pts = slab_ring(centre, sx, sy, CAP5_LEG, rx, ry)
        else:
            pts = frame_ring(centre, sx, sy, NLEG, rx, ry)
        for k, delta in (offs or {}).items():
            pts[k] = pts[k] + Vector(delta)
        ring = cage.ring(pts)
        cage.bridge(prev, ring)
        prev = ring

    quads = cap5(prev, CAP5_LEG)
    toe_joints = []
    for i, (length, splay, scale) in enumerate(TOES):
        direction = Vector((splay, -1.0, 0.05)).normalized()
        toe_joints.append(extrude_digit(cage, quads[i], direction,
                                        [length * 0.55, length * 0.45],
                                        (0.94, 0.86), root_scale=scale))
    # One Unity "Toes" bone drives all five; use the big toe's base as its pivot.
    return toe_joints[0]


# ---------------------------------------------------------------------------
# Head
# ---------------------------------------------------------------------------

HEAD = {
    "n": 8,                          # grid divisions per cube face
    "centre": Vector((0.0, 0.006, 1.686)),
    "rx": 0.077, "ry": 0.101, "rz": 0.116,
    "neck_block": (2, 6),            # central 4x4 of the -Z panel -> 16 boundary
}

# Panels, in (axis, sign) form. -Y is forward, so that panel carries the face.
PANEL_FACE = (1, -1)
PANEL_NECK = (2, -1)
PANEL_EAR = (0, 1)

# Head silhouette, sampled bottom (skull base) to top (crown):
# (height fraction, half-width, depth forward of centre, depth behind centre)
HEAD_PROFILE = [
    (0.00, 0.038, 0.052, 0.044),   # skull base / behind the jaw
    (0.15, 0.054, 0.084, 0.062),   # chin and mandible
    (0.30, 0.064, 0.092, 0.082),   # mouth
    (0.45, 0.072, 0.092, 0.098),   # nose base, cheekbone
    (0.58, 0.076, 0.090, 0.100),   # eyes and brow, widest
    (0.72, 0.076, 0.085, 0.099),   # forehead
    (0.86, 0.071, 0.073, 0.092),   # parietal - kept wide, or the skull towers
    (1.00, 0.044, 0.044, 0.052),   # crown
]

JAW = {
    "chin_top": 1.650, "chin_falloff": 0.080,
    "chin_drop": 0.034, "chin_project": 0.014,
    "gonion_top": 1.682, "gonion_falloff": 0.066, "gonion_width": 0.016,
    "brow_z": 1.703, "brow_falloff": 0.014, "brow_project": 0.0060,
    "cheek_z": 1.672, "cheek_falloff": 0.018, "cheek_out": 0.0055,
}

NOSE = {"project": 0.020, "alar_width": 0.007, "tip_drop": 0.004}

# Ear shell rings: (outward offset, front-back scale, vertical scale, lean back).
# Narrow and tall, leaning back - a uniform scale reads as a square sticker.
EAR_SHELL = [(0.011, 0.82, 1.34, 0.003), (0.018, 0.54, 1.02, 0.007)]
EAR_HUB = (0.007, 0.008)   # concha centre, tucked back in and rearward

# Neck rings between the skull base and the body seam: (z, centre_y, rx, ry)
NECK_RINGS = [
    (1.556, 0.014, 0.049, 0.053),   # under the mandible
    (1.534, 0.011, 0.056, 0.058),   # mid neck
]


def profile_at(h):
    """Interpolate the head silhouette table at height fraction h."""
    table = HEAD_PROFILE
    h = min(max(h, table[0][0]), table[-1][0])
    for i in range(len(table) - 1):
        h0, *a = table[i]
        h1, *b = table[i + 1]
        if h <= h1:
            t = 0.0 if h1 == h0 else (h - h0) / (h1 - h0)
            return [x + (y - x) * t for x, y in zip(a, b)]
    return table[-1][1:]


def shape_head(bm):
    """Reshape the ellipsoid onto the head silhouette.

    Each vertex keeps its angular slot around the current cross-section and is
    re-placed at the target width and depth for its height, so the cranium,
    cheekbones, jaw and chin all come from one readable table instead of
    hundreds of typed coordinates.
    """
    c, rx, ry, rz = HEAD["centre"], HEAD["rx"], HEAD["ry"], HEAD["rz"]
    z0, z1 = c.z - rz, c.z + rz
    for v in bm.verts:
        w = max(-1.0, min(1.0, (v.co.z - c.z) / rz))
        k = max(math.sqrt(max(0.0, 1.0 - w * w)), 0.06)
        nx = (v.co.x - c.x) / (rx * k)
        ny = (v.co.y - c.y) / (ry * k)
        r = math.hypot(nx, ny)
        if r < 1e-7:
            continue
        half_w, depth_f, depth_b = profile_at((v.co.z - z0) / (z1 - z0))
        angle = math.atan2(ny, nx)
        depth = depth_f if ny < 0.0 else depth_b
        v.co.x = c.x + math.cos(angle) * half_w * r
        v.co.y = c.y + math.sin(angle) * depth * r


def sculpt_features(bm):
    """Forms the silhouette table cannot express, because they are local.

    A cross-section table gives the cranium and cheekbones, but the chin, the
    mandible corner and the brow ridge sit at particular places on the surface
    rather than at particular heights, so they get their own falloffs.
    """
    c = HEAD["centre"]
    for v in bm.verts:
        p = v.co
        forward = min(1.0, max(0.0, -(p.y - c.y) / (HEAD["ry"] * 0.85)))
        lateral = min(1.0, abs(p.x - c.x) / HEAD["rx"])

        # Chin and jawline: the sphere's front-bottom has to become a mandible.
        low = min(1.0, max(0.0, (JAW["chin_top"] - p.z) / JAW["chin_falloff"]))
        if low > 0.0 and forward > 0.0:
            t = low * forward
            p.z -= JAW["chin_drop"] * t
            p.y -= JAW["chin_project"] * t

        # Mandible corner: widen the jaw without widening the chin itself.
        gonion = min(1.0, max(0.0, (JAW["gonion_top"] - p.z) / JAW["gonion_falloff"]))
        if gonion > 0.0 and p.x != c.x:
            spread = gonion * lateral * (1.0 - 0.55 * forward)
            p.x += math.copysign(JAW["gonion_width"] * spread, p.x - c.x)

        # Brow ridge: a narrow horizontal band pushed forward.
        brow = math.exp(-(((p.z - JAW["brow_z"]) / JAW["brow_falloff"]) ** 2))
        p.y -= JAW["brow_project"] * brow * forward

        # Zygomatic arch: widen a band at cheekbone height, off the midline.
        cheek = math.exp(-(((p.z - JAW["cheek_z"]) / JAW["cheek_falloff"]) ** 2))
        if p.x != c.x:
            p.x += math.copysign(
                JAW["cheek_out"] * cheek * lateral * forward, p.x - c.x)


def cube_sphere(n):
    """Spherified cube: all quads, and its only poles are the 8 cube corners.

    Those corners land on the upper skull (under every hair module) and beneath
    the mandible, which is why this beats a UV sphere or a hand-authored patch
    layout - the unavoidable extraordinary vertices go somewhere harmless.
    """
    bm = bmesh.new()
    lut = {}

    def vert(ix, iy, iz):
        key = (ix, iy, iz)
        if key not in lut:
            x, y, z = ix / n, iy / n, iz / n
            lut[key] = bm.verts.new((
                x * math.sqrt(max(0.0, 1 - y * y / 2 - z * z / 2 + y * y * z * z / 3)),
                y * math.sqrt(max(0.0, 1 - z * z / 2 - x * x / 2 + z * z * x * x / 3)),
                z * math.sqrt(max(0.0, 1 - x * x / 2 - y * y / 2 + x * x * y * y / 3)),
            ))
        return lut[key]

    panels = {}
    for axis in range(3):
        for sign in (-1, 1):
            grid = {}
            for a in range(n):
                for b in range(n):
                    corners = []
                    for da, db in ((0, 0), (1, 0), (1, 1), (0, 1)):
                        c = [0, 0, 0]
                        c[axis] = sign * n
                        c[(axis + 1) % 3] = -n + 2 * (a + da)
                        c[(axis + 2) % 3] = -n + 2 * (b + db)
                        corners.append(vert(*c))
                    grid[(a, b)] = bm.faces.new(corners)
            panels[(axis, sign)] = grid

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm, panels


def block(panels, panel, a0, a1, b0, b1):
    """Rectangular selection of faces from a panel grid."""
    grid = panels[panel]
    return [grid[(a, b)] for a in range(a0, a1) for b in range(b0, b1)]


def build_head():
    """Head as a spherified cube: feature loops cut by inset, form by landmarks.

    Only what must be a hole gets cut - eyes, mouth, ears, neck. Brow, cheek,
    jaw and chin arrive as displacement, not as authored connectivity, which is
    what keeps a blind script from producing a folded face.
    """
    n = HEAD["n"]
    bm, panels = cube_sphere(n)

    # Ellipsoid, then move into place under the crown.
    for v in bm.verts:
        v.co.x *= HEAD["rx"]
        v.co.y *= HEAD["ry"]
        v.co.z *= HEAD["rz"]
        v.co += HEAD["centre"]

    # Silhouette first, features second: cutting on the shaped head keeps the
    # eye, nose and mouth loops on the surface they actually belong to.
    shape_head(bm)
    sculpt_features(bm)

    # Every selection is taken while the panel grids are still intact.
    # Row a runs bottom to top of the face panel: 0 mouth, 1-2 nose,
    # 3-4 eyes and brow, 5-7 forehead. The panel only spans the central ~70%
    # of the head's width, so the eyes have to reach its outer columns.
    lo, hi = HEAD["neck_block"]
    neck_faces = block(panels, PANEL_NECK, lo, hi, lo, hi)
    eye_l = block(panels, PANEL_FACE, 3, 5, 0, 3)
    eye_r = block(panels, PANEL_FACE, 3, 5, 5, 8)
    mouth = block(panels, PANEL_FACE, 0, 1, 2, 6)
    nose = block(panels, PANEL_FACE, 1, 3, 3, 5)
    ears = [block(panels, (0, s), 3, 5, 2, 4) for s in (-1, 1)]

    # --- feature loops. Insetting generates concentric rings by construction,
    # so no face tuple is ever typed by hand - that is what keeps a blind
    # script from producing a folded face.
    for eye in (eye_l, eye_r):
        inset(bm, eye, 0.0075)
        inset(bm, eye, 0.0045)
    for _ in range(2):
        inset(bm, mouth, 0.0050)
    inset(bm, nose, 0.0060)

    # Sockets and the nose are pushed once the rims exist.
    push(eye_l + eye_r, Vector((0.0, 0.006, 0.0)))
    push(mouth, Vector((0.0, 0.0035, 0.0)))
    shape_nose(bm, nose)

    # Ears are closed immediately, so the neck hole stays the only boundary.
    for sign, ear in zip((-1, 1), ears):
        build_ear(bm, ear, sign)

    bmesh.ops.delete(bm, geom=neck_faces, context="FACES")

    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    return bm


def bm_bridge(bm, ring_a, ring_b):
    n = len(ring_a)
    for k in range(n):
        k2 = (k + 1) % n
        bm.faces.new((ring_a[k], ring_a[k2], ring_b[k2], ring_b[k]))


def build_ear(bm, faces, sign):
    """Replace a 2x2 panel block with a protruding ear shell.

    The block's 8-vertex perimeter alternates corner, midpoint, so the shell
    caps with four quads around a single centre vertex - no n-gon, no triangle.
    """
    fset = set(faces)
    vs = {v for f in faces for v in f.verts}
    centre = sum((v.co for v in vs), Vector()) / len(vs)
    core = [v for v in vs if len(set(v.link_faces) & fset) == 4]
    ring = [v for v in vs if v not in core]

    bmesh.ops.delete(bm, geom=list(faces), context="FACES")
    # The centre vertex is usually swept up with the faces; only delete it if
    # it survived as a loose vertex.
    stale = [v for v in core if v.is_valid and not v.link_faces]
    if stale:
        bmesh.ops.delete(bm, geom=stale, context="VERTS")
    ring = [v for v in ring if v.is_valid]

    ring.sort(key=lambda v: math.atan2(v.co.z - centre.z,
                                       (v.co.y - centre.y) * sign))
    # Start on a corner so the four cap quads land correctly.
    far = max(range(len(ring)), key=lambda i: (ring[i].co - centre).length)
    ring = ring[far:] + ring[:far]

    out = Vector((sign, 0.0, 0.0))
    prev = ring
    for offset, scale_y, scale_z, lean in EAR_SHELL:
        new = [bm.verts.new(centre + out * offset
                            + Vector((0.0,
                                      (v.co.y - centre.y) * scale_y + lean,
                                      (v.co.z - centre.z) * scale_z)))
               for v in ring]
        bm_bridge(bm, prev, new)
        prev = new

    hub = bm.verts.new(centre + out * EAR_HUB[0]
                       + Vector((0.0, EAR_HUB[1], 0.0)))
    for i in range(0, len(prev), 2):
        bm.faces.new((prev[i], prev[i + 1], hub, prev[i - 1]))


def inset(bm, faces, thickness, depth=0.0):
    """Concentric quad ring around a face selection; originals stay as the core."""
    bmesh.ops.inset_region(bm, faces=faces, thickness=thickness, depth=depth,
                           use_even_offset=True, use_interpolate=True)
    return faces


def push(faces, delta):
    """Translate the vertices of a face selection."""
    for v in {v for f in faces for v in f.verts}:
        v.co += delta


def shape_nose(bm, faces):
    """Project the nose, with the tip further out than the root.

    A flat translation gives a ridge, not a nose - the give-away in profile.
    Projection ramps from nothing at the root to full at the tip, and the base
    widens for the alae.
    """
    vs = {v for f in faces for v in f.verts}
    top = max(v.co.z for v in vs)
    bottom = min(v.co.z for v in vs)
    span = max(top - bottom, 1e-6)
    for v in vs:
        t = 1.0 - (v.co.z - bottom) / span          # 1 at the base, 0 at the root
        v.co.y -= NOSE["project"] * (0.25 + 0.75 * t * t)
        if abs(v.co.x) > 1e-6:
            v.co.x += math.copysign(NOSE["alar_width"] * t * t, v.co.x)
        v.co.z -= NOSE["tip_drop"] * t * t


def body_neck_ring_pts():
    """The 16 seam points, derived once from the body's top ring.

    The head consumes these rather than recomputing them, so the two meshes
    cannot drift apart.
    """
    z, w, df, db, dz = TORSO_RINGS[-1]
    half = profile_points(z, w, df, db, dz)
    return half + [Vector((-p.x, p.y, p.z)) for p in reversed(half[1:-1])]


def boundary_loop(bm):
    return [v for v in bm.verts if any(e.is_boundary for e in v.link_edges)]


def shape_loop(verts, centre, rx, ry, blend=1.0):
    """Pull an open loop onto an ellipse, keeping each vertex's angular slot."""
    c = sum((v.co for v in verts), Vector()) / len(verts)
    for v in verts:
        a = math.atan2(v.co.y - c.y, v.co.x - c.x)
        target = Vector((centre.x + math.cos(a) * rx,
                         centre.y + math.sin(a) * ry, centre.z))
        v.co = v.co.lerp(target, blend)


def grow_neck(bm, rings):
    """Extrude the neck down from the skull base toward the body seam."""
    for (cz, cy, rx, ry) in rings:
        edges = [e for e in bm.edges if e.is_boundary]
        ret = bmesh.ops.extrude_edge_only(bm, edges=edges)
        new = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMVert)]
        shape_loop(new, Vector((0.0, cy, cz)), rx, ry)
    return boundary_loop(bm)


def snap_neck_seam(bm, neck_ring_pts):
    """Move the head's neck boundary onto the body's ring, vertex for vertex.

    Both loops are sorted by angle about the neck axis so the correspondence is
    unambiguous; after this the seam is watertight by construction.
    """
    boundary = [v for v in bm.verts
                if any(e.is_boundary for e in v.link_edges)]
    if len(boundary) != len(neck_ring_pts):
        raise RuntimeError(
            f"neck seam mismatch: head {len(boundary)} vs body {len(neck_ring_pts)}")

    hc = sum((v.co for v in boundary), Vector()) / len(boundary)
    bc = sum(neck_ring_pts, Vector()) / len(neck_ring_pts)
    boundary.sort(key=lambda v: math.atan2(v.co.y - hc.y, v.co.x - hc.x))
    targets = sorted(neck_ring_pts,
                     key=lambda p: math.atan2(p.y - bc.y, p.x - bc.x))
    for v, p in zip(boundary, targets):
        v.co = p.copy()
    return boundary


# ---------------------------------------------------------------------------
# Scene setup, validation, entry point
# ---------------------------------------------------------------------------

def reset_scene():
    bpy.ops.wm.read_homefile(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = "METRIC"
    scn.unit_settings.scale_length = 1.0
    scn.unit_settings.length_unit = "METERS"


def finish_object(ob, subsurf_levels=1, mirror=True):
    """Mirror + subsurf, smooth shading, clean transforms."""
    if mirror:
        mir = ob.modifiers.new("Mirror", "MIRROR")
        mir.use_axis = (True, False, False)
        mir.use_clip = True
        mir.use_mirror_merge = True
        mir.merge_threshold = 0.0005

    sub = ob.modifiers.new("Subdivision", "SUBSURF")
    sub.levels = subsurf_levels
    sub.render_levels = subsurf_levels
    sub.use_limit_surface = True

    with bpy.context.temp_override(object=ob, selected_objects=[ob]):
        bpy.ops.object.shade_smooth()
    return ob


def clean_mesh(ob):
    """Snap the centre seam, drop loose verts, make normals consistent."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)

    for v in bm.verts:
        if abs(v.co.x) < 1e-4:
            v.co.x = 0.0

    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.update()
    return len(loose)


def report(ob):
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()

    tris = sum(1 for f in bm.faces if len(f.verts) == 3)
    ngons = sum(1 for f in bm.faces if len(f.verts) > 4)
    quads = sum(1 for f in bm.faces if len(f.verts) == 4)
    boundary = sum(1 for e in bm.edges if len(e.link_faces) == 1)
    nonmanifold = sum(1 for e in bm.edges if len(e.link_faces) > 2)
    loose = sum(1 for v in bm.verts if not v.link_faces)
    zs = [v.co.z for v in bm.verts]
    bm.free()

    print(f"  {ob.name}: {len(me.vertices)} verts | {quads} quads "
          f"| tris {tris} | ngons {ngons}")
    print(f"    boundary edges {boundary} | non-manifold {nonmanifold} "
          f"| loose verts {loose}")
    print(f"    z range {min(zs):.3f} .. {max(zs):.3f}")
    return dict(quads=quads, tris=tris, ngons=ngons,
                boundary=boundary, nonmanifold=nonmanifold, loose=loose)


VIEWS = {
    "front": (0.7071, 0.7071, 0.0, 0.0),
    "back": (0.0, 0.0, 0.7071, 0.7071),
    "right": (0.5, 0.5, 0.5, 0.5),
    "left": (0.5, 0.5, -0.5, -0.5),
}


AXIS = {"front": "FRONT", "back": "BACK", "right": "RIGHT", "left": "LEFT",
        "top": "TOP"}


def set_view(name="front", dist=1.30, centre=(0.0, 0.0, 0.79), tq=0.0,
             focus=None, zoom=1.0):
    """Orthographic review camera, matcap shaded, overlays off.

    The view axis has to go through the operator: assigning view_perspective
    directly leaves region_3d.is_perspective stale and the framing wrong.
    """
    from mathutils import Quaternion
    win, area = next(((w, a) for w in bpy.context.window_manager.windows
                      for a in w.screen.areas if a.type == "VIEW_3D"),
                     (None, None))
    if area is None:
        return
    sp = area.spaces.active
    region = next(r for r in area.regions if r.type == "WINDOW")
    with bpy.context.temp_override(window=win, area=area, region=region,
                                   space_data=sp):
        bpy.ops.view3d.view_axis(type=AXIS[name], align_active=False)
        sp.region_3d.view_perspective = "ORTHO"
        if focus:
            for ob in bpy.context.view_layer.objects:
                ob.select_set(ob.name in focus)
            bpy.ops.view3d.view_selected()
    if tq:
        sp.region_3d.view_rotation = (sp.region_3d.view_rotation
                                      @ Quaternion((0.0, 0.0, 1.0), math.radians(tq)))
    if not focus:
        sp.region_3d.view_location = centre
        sp.region_3d.view_distance = dist
    else:
        sp.region_3d.view_distance *= zoom
    # region_3d caches its matrices; without update() the view stays perspective
    # and the screenshot comes back stale.
    sp.region_3d.update()
    sp.shading.type = "SOLID"
    sp.shading.light = "MATCAP"
    sp.shading.studio_light = "clay_brown.exr"
    sp.shading.color_type = "SINGLE"
    sp.shading.single_color = (0.62, 0.55, 0.50)
    sp.shading.show_cavity = True
    sp.shading.cavity_type = "BOTH"
    sp.overlay.show_overlays = False
    sp.show_gizmo = False
    area.tag_redraw()
    with bpy.context.temp_override(window=win, area=area, region=region):
        bpy.ops.wm.redraw_timer(type="DRAW_WIN_SWAP", iterations=1)


def build():
    reset_scene()

    cage = Cage()
    _rings, armhole, leg_loop = build_torso(cage)
    _arm, wrist_ring, wrist_centre, frame = build_arm(cage, armhole)
    build_hand(cage, wrist_ring, wrist_centre, frame)
    _leg, ankle_ring = build_leg(cage, leg_loop)
    build_foot(cage, ankle_ring)

    body = cage.to_object("MRG_Male_Body")
    dropped = clean_mesh(body)
    finish_object(body, subsurf_levels=1)

    # --- head, sharing the body's seam points rather than recomputing them
    seam_pts = body_neck_ring_pts()
    hbm = build_head()
    grow_neck(hbm, NECK_RINGS)
    snap_neck_seam(hbm, seam_pts)
    bmesh.ops.recalc_face_normals(hbm, faces=hbm.faces[:])

    hme = bpy.data.meshes.new("MRG_Male_Head")
    hbm.to_mesh(hme)
    hbm.free()
    head = bpy.data.objects.new("MRG_Male_Head", hme)
    bpy.context.collection.objects.link(head)
    finish_object(head, subsurf_levels=1, mirror=False)

    print("=" * 62)
    print("Margins male NPC base - cage build")
    print("=" * 62)
    if dropped:
        print(f"  dropped {dropped} loose verts (armhole interior)")
    report(body)
    report(head)
    set_view("front")
    bpy.app.driver_namespace["set_view"] = set_view
    return body


if __name__ == "__main__" or True:
    build()
