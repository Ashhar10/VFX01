import math
from PIL import Image, ImageDraw, ImageFilter

def create_enemy_skull_ring():
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size // 2, size // 2

    # Draw Outer Target Ring with 4 gaps / crosshair notches
    # Radius = 200, width = 14
    ring_radius = 200
    ring_w = 12
    for angle in range(0, 360):
        # Leave 4 gaps at 0, 90, 180, 270 (each 16 degrees wide)
        norm_angle = angle % 90
        if 8 < norm_angle < 82:
            rad = math.radians(angle)
            x1 = cx + (ring_radius - ring_w) * math.cos(rad)
            y1 = cy + (ring_radius - ring_w) * math.sin(rad)
            x2 = cx + (ring_radius + ring_w) * math.cos(rad)
            y2 = cy + (ring_radius + ring_w) * math.sin(rad)
            draw.line([(x1, y1), (x2, y2)], fill=(255, 255, 255, 255), width=3)

    # 4 Outer Pointer Ticks
    tick_len = 26
    for angle in [0, 90, 180, 270]:
        rad = math.radians(angle)
        x_start = cx + (ring_radius + 6) * math.cos(rad)
        y_start = cy + (ring_radius + 6) * math.sin(rad)
        x_end = cx + (ring_radius + 6 + tick_len) * math.cos(rad)
        y_end = cy + (ring_radius + 6 + tick_len) * math.sin(rad)
        draw.line([(x_start, y_start), (x_end, y_end)], fill=(255, 255, 255, 255), width=8)

    # Inner Skull
    # Cranium (top dome)
    cranium_r = 85
    cranium_cy = cy - 25
    draw.ellipse([cx - cranium_r, cranium_cy - cranium_r, cx + cranium_r, cranium_cy + cranium_r], fill=(255, 255, 255, 255))

    # Jaw (tapered lower section)
    jaw_pts = [
        (cx - 55, cy + 20),
        (cx + 55, cy + 20),
        (cx + 38, cy + 95),
        (cx - 38, cy + 95)
    ]
    draw.polygon(jaw_pts, fill=(255, 255, 255, 255))

    # Eye Sockets (Dark cutouts)
    eye_w = 26
    eye_h = 36
    draw.ellipse([cx - 48 - eye_w, cranium_cy + 5 - eye_h, cx - 48 + eye_w, cranium_cy + 5 + eye_h], fill=(0, 0, 0, 0))
    draw.ellipse([cx + 48 - eye_w, cranium_cy + 5 - eye_h, cx + 48 + eye_w, cranium_cy + 5 + eye_h], fill=(0, 0, 0, 0))

    # Nose Cavity (Triangle cutout)
    nose_pts = [
        (cx, cy + 5),
        (cx - 15, cy + 35),
        (cx + 15, cy + 35)
    ]
    draw.polygon(nose_pts, fill=(0, 0, 0, 0))

    # Teeth vertical slits
    for tx in [-22, 0, 22]:
        draw.line([(cx + tx, cy + 60), (cx + tx, cy + 85)], fill=(0, 0, 0, 0), width=6)

    # Soft outer glow blur
    glow = img.filter(ImageFilter.GaussianBlur(radius=3))
    final = Image.alpha_composite(glow, img)
    final.save("Assets/VFX/King Arthur_VFX/Textures/Enemy_Skull_Ring.png")
    print("Created Enemy_Skull_Ring.png")

def create_ally_shield_ring():
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size // 2, size // 2

    # Outer Circular Ring (radius 200, width 14)
    r_outer = 195
    draw.ellipse([cx - r_outer, cy - r_outer, cx + r_outer, cy + r_outer], outline=(255, 255, 255, 255), width=16)

    # 4 Outer Accent Pips (top, bottom, left, right)
    pip_r = 7
    for angle in [0, 90, 180, 270]:
        rad = math.radians(angle)
        px = cx + (r_outer + 8) * math.cos(rad)
        py = cy + (r_outer + 8) * math.sin(rad)
        draw.ellipse([px - pip_r, py - pip_r, px + pip_r, py + pip_r], fill=(255, 255, 255, 255))

    # Inner Shield Icon (Heater shield in center)
    sw = 85
    s_top = cy - 95
    s_mid = cy + 20
    s_tip = cy + 115

    shield_pts = [
        (cx - sw, s_top + 15),
        (cx - sw // 2, s_top),
        (cx, s_top - 5),
        (cx + sw // 2, s_top),
        (cx + sw, s_top + 15),
        (cx + sw, s_mid),
        (cx + int(sw * 0.7), s_mid + 50),
        (cx, s_tip),
        (cx - int(sw * 0.7), s_mid + 50),
        (cx - sw, s_mid)
    ]

    # Fill shield solid white
    draw.polygon(shield_pts, fill=(255, 255, 255, 255))

    # Dark cut-out rim inside the shield for contrast
    scale = 0.72
    inner_pts = [(cx + int((px - cx) * scale), cy + int((py - cy) * scale)) for (px, py) in shield_pts]
    draw.polygon(inner_pts, outline=(0, 0, 0, 255), width=8)

    glow = img.filter(ImageFilter.GaussianBlur(radius=3))
    final = Image.alpha_composite(glow, img)
    final.save("Assets/VFX/King Arthur_VFX/Textures/Ally_Shield_Ring.png")
    print("Regenerated Ally_Shield_Ring.png")

def create_ally_body_shield():
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size // 2, size // 2

    # High Medieval Heater Shield Shape
    w = 170
    h_top = cy - 195
    h_mid = cy + 50
    h_tip = cy + 225

    pts = [
        (cx - w, h_top + 30),
        (cx - w // 2, h_top + 5),
        (cx, h_top - 5),
        (cx + w // 2, h_top + 5),
        (cx + w, h_top + 30),
        (cx + w, h_mid),
        (cx + int(w * 0.75), h_mid + 85),
        (cx, h_tip),
        (cx - int(w * 0.75), h_mid + 85),
        (cx - w, h_mid)
    ]

    # Faint glowing translucent interior fill (character visible through it)
    draw.polygon(pts, fill=(255, 255, 255, 25))

    # Bright neon outer border
    draw.polygon(pts, outline=(255, 255, 255, 255), width=14)

    # Secondary inner glowing contour
    scale = 0.88
    inner_pts = [(cx + int((px - cx) * scale), cy + int((py - cy) * scale)) for (px, py) in pts]
    draw.polygon(inner_pts, outline=(255, 255, 255, 160), width=6)

    # Stylized vertical energy spine
    draw.line([(cx, h_top + 30), (cx, h_tip - 40)], fill=(255, 255, 255, 180), width=6)

    # Stylized horizontal crossbar
    draw.line([(cx - 65, cy - 15), (cx + 65, cy - 15)], fill=(255, 255, 255, 180), width=6)

    glow = img.filter(ImageFilter.GaussianBlur(radius=3))
    final = Image.alpha_composite(glow, img)
    final.save("Assets/VFX/King Arthur_VFX/Textures/Ally_Body_Shield.png")
    print("Regenerated Ally_Body_Shield.png")

def create_golden_spark_mote():
    size = 256
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size // 2, size // 2

    # 4-point radiant diamond star
    pts = [
        (cx, cy - 110),
        (cx + 18, cy - 18),
        (cx + 110, cy),
        (cx + 18, cy + 18),
        (cx, cy + 110),
        (cx - 18, cy + 18),
        (cx - 110, cy),
        (cx - 18, cy - 18)
    ]
    draw.polygon(pts, fill=(255, 255, 255, 255))

    # Diagonal smaller rays
    diag_len = 55
    draw.polygon([
        (cx - diag_len, cy - diag_len),
        (cx, cy - 8),
        (cx + diag_len, cy - diag_len),
        (cx + 8, cy),
        (cx + diag_len, cy + diag_len),
        (cx, cy + 8),
        (cx - diag_len, cy + diag_len),
        (cx - 8, cy)
    ], fill=(255, 255, 255, 200))

    # Center glowing ball
    draw.ellipse([cx - 30, cy - 30, cx + 30, cy + 30], fill=(255, 255, 255, 255))

    glow = img.filter(ImageFilter.GaussianBlur(radius=4))
    final = Image.alpha_composite(glow, img)
    final.save("Assets/VFX/King Arthur_VFX/Textures/Golden_Spark_Mote.png")
    print("Created Golden_Spark_Mote.png")

if __name__ == "__main__":
    create_enemy_skull_ring()
    create_ally_shield_ring()
    create_ally_body_shield()
    create_golden_spark_mote()
