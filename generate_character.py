import math
import os

def create_obj():
    vertices = []
    normals = []
    uvs = []
    materials = {}
    current_group = "default"

    def add_box(center, size, mat_name):
        nonlocal current_group
        if mat_name not in materials:
            materials[mat_name] = []
        
        cx, cy, cz = center
        sx, sy, sz = [s * 0.5 for s in size]

        # 8 corners
        # 0: - - -
        # 1: + - -
        # 2: + + -
        # 3: - + -
        # 4: - - +
        # 5: + - +
        # 6: + + +
        # 7: - + +
        base_v = len(vertices) + 1

        v_corners = [
            (cx - sx, cy - sy, cz - sz),
            (cx + sx, cy - sy, cz - sz),
            (cx + sx, cy + sy, cz - sz),
            (cx - sx, cy + sy, cz - sz),
            (cx - sx, cy - sy, cz + sz),
            (cx + sx, cy - sy, cz + sz),
            (cx + sx, cy + sy, cz + sz),
            (cx - sx, cy + sy, cz + sz),
        ]
        vertices.extend(v_corners)

        # 6 faces: (quads as 2 tris)
        # Front (+Z), Back (-Z), Left (-X), Right (+X), Top (+Y), Bottom (-Y)
        faces = [
            # Front (+Z): 4, 5, 6, 7
            (4, 5, 6, 7),
            # Back (-Z): 1, 0, 3, 2
            (1, 0, 3, 2),
            # Top (+Y): 3, 2, 6, 7
            (3, 2, 6, 7),
            # Bottom (-Y): 0, 1, 5, 4
            (0, 1, 5, 4),
            # Right (+X): 5, 1, 2, 6
            (5, 1, 2, 6),
            # Left (-X): 0, 4, 7, 3
            (0, 4, 7, 3),
        ]

        for quad in faces:
            # tri 1: 0, 1, 2; tri 2: 0, 2, 3
            q = [base_v + idx for idx in quad]
            materials[mat_name].append((q[0], q[1], q[2]))
            materials[mat_name].append((q[0], q[2], q[3]))

    def add_cylinder(center, radius, height, mat_name, axis='y', segments=12):
        nonlocal current_group
        if mat_name not in materials:
            materials[mat_name] = []

        cx, cy, cz = center
        base_v = len(vertices) + 1

        # Generates cylinder along chosen axis
        half_h = height * 0.5
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            cos_a = math.cos(angle) * radius
            sin_a = math.sin(angle) * radius
            if axis == 'y':
                vertices.append((cx + cos_a, cy - half_h, cz + sin_a))
                vertices.append((cx + cos_a, cy + half_h, cz + sin_a))
            elif axis == 'x':
                vertices.append((cx - half_h, cy + cos_a, cz + sin_a))
                vertices.append((cx + half_h, cy + cos_a, cz + sin_a))
            else: # z
                vertices.append((cx + cos_a, cy + sin_a, cz - half_h))
                vertices.append((cx + cos_a, cy + sin_a, cz + half_h))

        # Bottom center & top center
        center_bot = len(vertices) + 1
        center_top = len(vertices) + 2
        if axis == 'y':
            vertices.append((cx, cy - half_h, cz))
            vertices.append((cx, cy + half_h, cz))
        elif axis == 'x':
            vertices.append((cx - half_h, cy, cz))
            vertices.append((cx + half_h, cy, cz))
        else:
            vertices.append((cx, cy, cz - half_h))
            vertices.append((cx, cy, cz + half_h))

        for i in range(segments):
            next_i = (i + 1) % segments
            b1 = base_v + i * 2
            t1 = base_v + i * 2 + 1
            b2 = base_v + next_i * 2
            t2 = base_v + next_i * 2 + 1

            # Side quad
            materials[mat_name].append((b1, t1, t2))
            materials[mat_name].append((b1, t2, b2))

            # Bottom cap & top cap
            materials[mat_name].append((center_bot, b2, b1))
            materials[mat_name].append((center_top, t1, t2))

    # --- BUILD CHARACTER ---
    # Torso & Jacket
    add_box((0, 1.15, -0.45), (0.48, 0.65, 0.3), "Character_Jacket")
    add_box((0, 1.15, -0.30), (0.10, 0.62, 0.04), "Character_Shirt") # zipper strip
    
    # Head & Cap
    add_box((0, 1.66, -0.45), (0.34, 0.34, 0.32), "Character_Skin")
    add_box((0, 1.84, -0.44), (0.36, 0.14, 0.36), "Character_Cap")
    add_box((0, 1.80, -0.24), (0.32, 0.04, 0.16), "Character_Cap") # visor
    # Goggles / Sunglasses
    add_box((0, 1.68, -0.28), (0.30, 0.10, 0.06), "Character_Goggles")

    # Legs & Pants
    add_box((-0.16, 0.52, -0.46), (0.16, 0.65, 0.22), "Character_Pants")
    add_box((0.16, 0.52, -0.46), (0.16, 0.65, 0.22), "Character_Pants")
    # Kneepads
    add_box((-0.16, 0.52, -0.34), (0.14, 0.16, 0.06), "Cart_Bumper")
    add_box((0.16, 0.52, -0.34), (0.14, 0.16, 0.06), "Cart_Bumper")

    # Sneakers
    add_box((-0.16, 0.10, -0.40), (0.18, 0.18, 0.34), "Character_Shoes")
    add_box((0.16, 0.10, -0.40), (0.18, 0.18, 0.34), "Character_Shoes")

    # Arms (angled forward to grip cart handle)
    add_box((-0.28, 1.30, -0.32), (0.14, 0.16, 0.28), "Character_Jacket")
    add_box((0.28, 1.30, -0.32), (0.14, 0.16, 0.28), "Character_Jacket")
    add_box((-0.28, 1.08, -0.16), (0.12, 0.14, 0.30), "Character_Skin") # forearms
    add_box((0.28, 1.08, -0.16), (0.12, 0.14, 0.30), "Character_Skin")
    # Hands / Gloves
    add_box((-0.28, 0.98, -0.02), (0.12, 0.12, 0.14), "Character_Gloves")
    add_box((0.28, 0.98, -0.02), (0.12, 0.12, 0.14), "Character_Gloves")

    # --- BUILD COMBAT CART ---
    # Handlebar
    add_box((0, 0.98, -0.02), (0.76, 0.06, 0.06), "Cart_Bumper")
    add_box((-0.28, 0.98, -0.02), (0.18, 0.08, 0.08), "Cart_Gripper")
    add_box((0.28, 0.98, -0.02), (0.18, 0.08, 0.08), "Cart_Gripper")

    # Rear Support Struts connecting Handle to Chassis
    add_box((-0.34, 0.60, -0.02), (0.05, 0.74, 0.05), "Cart_Metal")
    add_box((0.34, 0.60, -0.02), (0.05, 0.74, 0.05), "Cart_Metal")

    # Wire Basket Bottom & Sides
    add_box((0, 0.46, 0.55), (0.72, 0.04, 1.06), "Cart_Metal")
    add_box((0, 0.72, 1.06), (0.72, 0.48, 0.04), "Cart_Metal") # Front
    add_box((0, 0.72, 0.04), (0.72, 0.48, 0.04), "Cart_Metal") # Back
    add_box((-0.35, 0.72, 0.55), (0.04, 0.48, 1.06), "Cart_Metal") # Left
    add_box((0.35, 0.72, 0.55), (0.04, 0.48, 1.06), "Cart_Metal") # Right

    # Basket Rim Bumper (Red protective guard along top edge)
    add_box((0, 0.96, 1.06), (0.76, 0.06, 0.06), "Cart_Bumper")
    add_box((-0.35, 0.96, 0.55), (0.06, 0.06, 1.08), "Cart_Bumper")
    add_box((0.35, 0.96, 0.55), (0.06, 0.06, 1.08), "Cart_Bumper")

    # Lower Chassis & Undercarriage Tray
    add_box((0, 0.22, 0.55), (0.64, 0.04, 1.10), "Cart_Metal")
    add_box((-0.32, 0.22, 0.55), (0.06, 0.06, 1.20), "Cart_Metal")
    add_box((0.32, 0.22, 0.55), (0.06, 0.06, 1.20), "Cart_Metal")

    # 4 Wheels
    wheel_y = 0.10
    wheel_r = 0.09
    wheel_w = 0.06
    # Front-Left, Front-Right, Rear-Left, Rear-Right
    add_cylinder((-0.32, wheel_y, 1.05), wheel_r, wheel_w, "Cart_Wheels", axis='x')
    add_cylinder((0.32, wheel_y, 1.05), wheel_r, wheel_w, "Cart_Wheels", axis='x')
    add_cylinder((-0.32, wheel_y, 0.05), wheel_r, wheel_w, "Cart_Wheels", axis='x')
    add_cylinder((0.32, wheel_y, 0.05), wheel_r, wheel_w, "Cart_Wheels", axis='x')

    # Front Combat Rammer / Spikes
    add_box((0, 0.35, 1.18), (0.84, 0.16, 0.10), "Cart_Bumper")
    add_box((-0.36, 0.35, 1.26), (0.12, 0.14, 0.14), "Cart_Metal") # Left Ram Spike
    add_box((0.36, 0.35, 1.26), (0.12, 0.14, 0.14), "Cart_Metal") # Right Ram Spike
    add_box((0, 0.35, 1.30), (0.18, 0.14, 0.20), "Cart_Metal") # Center Ram Wedge

    # Export OBJ
    out_obj = r"C:\Users\Ammad\Cart Combat\Assets\Models\ShopperCartFighter.obj"
    out_mtl = r"C:\Users\Ammad\Cart Combat\Assets\Models\ShopperCartFighter.mtl"

    with open(out_obj, "w") as f:
        f.write("# Cart Combat Shopper Character & Combat Cart 3D Model\n")
        f.write("mtllib ShopperCartFighter.mtl\n\n")
        
        for v in vertices:
            f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
        f.write("\n")

        for mat, tris in materials.items():
            f.write(f"usemtl {mat}\n")
            f.write(f"g {mat}\n")
            for t in tris:
                f.write(f"f {t[0]} {t[1]} {t[2]}\n")
            f.write("\n")

    # Export MTL
    mtl_colors = {
        "Character_Skin": (0.92, 0.72, 0.58),
        "Character_Jacket": (0.18, 0.38, 0.72), # Electric blue jacket
        "Character_Shirt": (0.95, 0.95, 0.95),
        "Character_Cap": (0.85, 0.15, 0.15),   # Red cap
        "Character_Goggles": (0.15, 0.15, 0.18),
        "Character_Pants": (0.22, 0.25, 0.30),  # Dark combat pants
        "Character_Shoes": (0.90, 0.20, 0.20),  # Red sneakers
        "Character_Gloves": (0.20, 0.20, 0.22),
        "Cart_Metal": (0.80, 0.82, 0.85),       # Chrome steel
        "Cart_Bumper": (0.90, 0.18, 0.12),      # Combat red bumper
        "Cart_Gripper": (0.12, 0.12, 0.12),     # Black rubber grip
        "Cart_Wheels": (0.15, 0.15, 0.16)       # Dark rubber wheels
    }

    with open(out_mtl, "w") as f:
        f.write("# Cart Combat Materials\n\n")
        for name, col in mtl_colors.items():
            f.write(f"newmtl {name}\n")
            f.write(f"Kd {col[0]:.3f} {col[1]:.3f} {col[2]:.3f}\n")
            f.write("Ka 0.2 0.2 0.2\n")
            f.write("Ks 0.5 0.5 0.5\n")
            f.write("Ns 32.0\n")
            f.write("d 1.0\n")
            f.write("illum 2\n\n")

    print(f"Generated {out_obj} with {len(vertices)} vertices and {sum(len(t) for t in materials.values())} triangles.")

if __name__ == "__main__":
    create_obj()
