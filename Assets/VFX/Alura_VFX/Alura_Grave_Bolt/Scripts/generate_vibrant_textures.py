import os
import numpy as np
from PIL import Image

output_dir = r"d:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Grave_Bolt\Textures"
os.makedirs(output_dir, exist_ok=True)

# Necrotic electric green color palette
CORE_COLOR = np.array([255, 255, 255], dtype=np.float32)       # Pure White-hot
NEON_GREEN = np.array([64, 255, 115], dtype=np.float32)       # Vivid Electric Green (#40FF73)
EMERALD_GREEN = np.array([15, 168, 84], dtype=np.float32)     # Deep Toxic Emerald (#0FA854)

def generate_vibrant_glowing_ball(size=512):
    """
    Creates a brilliant, ultra-smooth HDR glowing ball with:
    - Pure white-hot core
    - Lush, radiant electric neon green corona (#40FF73)
    - Deep toxic emerald outer halo
    """
    y, x = np.ogrid[:size, :size]
    cx, cy = (size - 1) / 2.0, (size - 1) / 2.0
    r = np.sqrt((x - cx)**2 + (y - cy)**2)
    max_r = size / 2.0
    norm_r = r / max_r

    core = np.clip(1.0 - (norm_r / 0.28)**1.6, 0.0, 1.0)
    corona = np.clip(1.0 - (norm_r / 0.65)**1.8, 0.0, 1.0)
    halo = np.clip(1.0 - norm_r, 0.0, 1.0)**2.2

    # Color interpolation: core -> neon green -> emerald
    rgb = np.zeros((size, size, 3), dtype=np.float32)
    for c in range(3):
        # Blend core (white) with corona (neon green) and halo (emerald)
        rgb[:, :, c] = (core * CORE_COLOR[c] + 
                        (corona * (1.0 - core)) * NEON_GREEN[c] + 
                        (halo * (1.0 - corona)) * EMERALD_GREEN[c]) / 255.0

    alpha = np.clip(core * 1.0 + corona * 0.85 + halo * 0.45, 0.0, 1.0)

    # Premultiplied RGBA
    img_data = np.zeros((size, size, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(rgb[:, :, c] * alpha * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    img = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Glow_Ball_Core.png")
    img.save(path, "PNG")
    print(f"Saved {path}")

def generate_vibrant_asterisk_blast(size=512):
    """
    Creates an asterisk '*' electric blast:
    - 16 dense jagged electric lightning forks spreading outward in 360 degrees
    - Random sub-branching tendrils crackling off prongs
    - White-hot core transitioning to vivid electric neon green and emerald
    """
    intensity_map = np.zeros((size, size), dtype=np.float32)
    core_map = np.zeros((size, size), dtype=np.float32)
    cx, cy = (size - 1) / 2.0, (size - 1) / 2.0

    np.random.seed(1337)
    num_prongs = 16
    angles = np.linspace(0, 2 * np.pi, num_prongs, endpoint=False)

    for i, base_angle in enumerate(angles):
        # Alternating prong lengths (long primary, medium secondary)
        is_primary = (i % 2 == 0)
        prong_len = 0.95 if is_primary else 0.70
        prong_len += 0.08 * (np.random.rand() - 0.5)
        max_dist = (size / 2.0 - 10) * prong_len

        steps = 40
        cur_x, cur_y = cx, cy
        path = [(cur_x, cur_y)]

        for s in range(1, steps + 1):
            t = s / float(steps)
            dist = t * max_dist
            jitter_amp = (6.0 if is_primary else 4.0) * np.sin(t * np.pi) * (1.0 + 0.6 * np.random.randn())
            perp_angle = base_angle + np.pi / 2.0

            target_x = cx + np.cos(base_angle) * dist + np.cos(perp_angle) * jitter_amp
            target_y = cy + np.sin(base_angle) * dist + np.sin(perp_angle) * jitter_amp
            path.append((target_x, target_y))

            # Add occasional sub-branch
            if is_primary and (s == 16 or s == 28) and np.random.rand() > 0.3:
                branch_angle = base_angle + (0.5 if np.random.rand() > 0.5 else -0.5)
                b_dist = max_dist * 0.35
                b_steps = 12
                bx, by = target_x, target_y
                b_path = [(bx, by)]
                for bs in range(1, b_steps + 1):
                    bt = bs / float(b_steps)
                    b_jitter = 3.0 * np.sin(bt * np.pi) * np.random.randn()
                    b_perp = branch_angle + np.pi / 2.0
                    b_tx = target_x + np.cos(branch_angle) * (bt * b_dist) + np.cos(b_perp) * b_jitter
                    b_ty = target_y + np.sin(branch_angle) * (bt * b_dist) + np.sin(b_perp) * b_jitter
                    b_path.append((b_tx, b_ty))

                for pi in range(len(b_path) - 1):
                    x1, y1 = b_path[pi]
                    x2, y2 = b_path[pi + 1]
                    stamp_electric_line(intensity_map, core_map, x1, y1, x2, y2, width=1.5, falloff=3.5, intensity=0.7)

        for pi in range(len(path) - 1):
            x1, y1 = path[pi]
            x2, y2 = path[pi + 1]
            t_along = pi / float(len(path))
            w = max(1.2, 3.5 * (1.0 - t_along * 0.7))
            fall = max(3.0, 8.5 * (1.0 - t_along * 0.6))
            stamp_electric_line(intensity_map, core_map, x1, y1, x2, y2, width=w, falloff=fall, intensity=1.0)

    # Big glowing center ball
    y, x = np.ogrid[:size, :size]
    r = np.sqrt((x - cx)**2 + (y - cy)**2)
    max_ball_r = size * 0.22
    norm_ball_r = np.clip(r / max_ball_r, 0.0, 1.0)
    center_ball = np.clip(1.0 - norm_ball_r**1.6, 0.0, 1.0) * 1.0
    center_core = np.clip(1.0 - (r / (size * 0.12))**1.4, 0.0, 1.0) * 1.0

    intensity_map = np.maximum(intensity_map, center_ball)
    core_map = np.maximum(core_map, center_core)

    # Colorize: core_map = pure white-hot, intensity_map = neon green fading to emerald
    rgb = np.zeros((size, size, 3), dtype=np.float32)
    for c in range(3):
        # Core is white
        c_core = core_map * CORE_COLOR[c]
        # Glow is neon green -> emerald
        outer_blend = np.clip(intensity_map - core_map, 0.0, 1.0)
        c_glow = outer_blend * NEON_GREEN[c]
        rgb[:, :, c] = (c_core + c_glow) / 255.0

    alpha = np.clip(intensity_map, 0.0, 1.0)

    img_data = np.zeros((size, size, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(rgb[:, :, c] * alpha * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    res = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Electric_Asterisk_Blast.png")
    res.save(path, "PNG")
    print(f"Saved {path}")

def stamp_electric_line(intensity_map, core_map, x1, y1, x2, y2, width, falloff, intensity):
    size = intensity_map.shape[0]
    min_x = max(0, int(min(x1, x2) - falloff - 2))
    max_x = min(size, int(max(x1, x2) + falloff + 3))
    min_y = max(0, int(min(y1, y2) - falloff - 2))
    max_y = min(size, int(max(y1, y2) + falloff + 3))

    if min_x >= max_x or min_y >= max_y:
        return

    yy, xx = np.mgrid[min_y:max_y, min_x:max_x]
    dx = x2 - x1
    dy = y2 - y1
    len_sq = dx*dx + dy*dy
    if len_sq < 1e-4:
        return

    t = np.clip(((xx - x1)*dx + (yy - y1)*dy) / len_sq, 0.0, 1.0)
    proj_x = x1 + t * dx
    proj_y = y1 + t * dy
    dist = np.sqrt((xx - proj_x)**2 + (yy - proj_y)**2)

    val_falloff = np.clip(1.0 - (dist / falloff), 0.0, 1.0)**1.7 * intensity
    val_core = np.clip(1.0 - (dist / width), 0.0, 1.0)**2.2 * intensity

    intensity_map[min_y:max_y, min_x:max_x] = np.maximum(intensity_map[min_y:max_y, min_x:max_x], val_falloff)
    core_map[min_y:max_y, min_x:max_x] = np.maximum(core_map[min_y:max_y, min_x:max_x], val_core)

def generate_vibrant_helix_rope(width=512, height=128):
    """
    Generates an electric serpentine rope texture with glowing neon green braided strands
    and intense electric core nodes.
    """
    x = np.linspace(0, 2 * np.pi * 3, width)
    y = np.linspace(-1, 1, height)
    X, Y = np.meshgrid(x, y)

    strand1 = np.sin(X) * 0.45
    strand2 = np.sin(X + np.pi) * 0.45

    dist1 = np.abs(Y - strand1)
    dist2 = np.abs(Y - strand2)

    glow1 = np.clip(1.0 - dist1 / 0.38, 0.0, 1.0)**2.0
    glow2 = np.clip(1.0 - dist2 / 0.38, 0.0, 1.0)**2.0
    core1 = np.clip(1.0 - dist1 / 0.12, 0.0, 1.0)**3.0
    core2 = np.clip(1.0 - dist2 / 0.12, 0.0, 1.0)**3.0

    glow = np.clip(glow1 + glow2, 0.0, 1.0)
    core = np.clip(core1 + core2, 0.0, 1.0)

    edge_fade = np.clip(1.0 - np.abs(Y)**2.0, 0.0, 1.0)
    glow = glow * edge_fade
    core = core * edge_fade

    rgb = np.zeros((height, width, 3), dtype=np.float32)
    for c in range(3):
        rgb[:, :, c] = (core * CORE_COLOR[c] + (glow * (1.0 - core)) * NEON_GREEN[c]) / 255.0

    alpha = np.clip(glow * 0.8 + core * 1.0, 0.0, 1.0)

    img_data = np.zeros((height, width, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(rgb[:, :, c] * alpha * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    res = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Electric_Helix_Rope.png")
    res.save(path, "PNG")
    print(f"Saved {path}")

generate_vibrant_glowing_ball()
generate_vibrant_asterisk_blast()
generate_vibrant_helix_rope()
print("All vibrant textures generated successfully!")
