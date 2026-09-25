import os
import numpy as np
from PIL import Image

output_dir = r"d:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Grave_Bolt\Textures"
os.makedirs(output_dir, exist_ok=True)

def generate_glowing_ball(size=512):
    """
    Creates a brilliant, ultra-smooth HDR glowing ball / orb for the center of the blast.
    Pure white-hot core with a smooth quadratic / exponential bloom falloff.
    """
    y, x = np.ogrid[:size, :size]
    cx, cy = (size - 1) / 2.0, (size - 1) / 2.0
    r = np.sqrt((x - cx)**2 + (y - cy)**2)
    max_r = size / 2.0
    norm_r = r / max_r

    # Smooth spherical core + soft wide bloom halo
    core = np.clip(1.0 - (norm_r / 0.35)**1.8, 0.0, 1.0)
    halo = np.clip(1.0 - norm_r, 0.0, 1.0)**2.5

    intensity = core * 0.7 + halo * 0.3
    intensity = np.clip(intensity, 0.0, 1.0)

    # Color: White core transitioning to vivid electric cyan / green glow
    # (Since materials use Tint, pure white luminance in RGB allows complete color control)
    rgb = np.ones((size, size, 3), dtype=np.float32)
    alpha = intensity

    # Premultiplied
    img_data = np.zeros((size, size, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(rgb[:, :, c] * alpha * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    img = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Glow_Ball_Core.png")
    img.save(path, "PNG")
    print(f"Saved {path}")

def generate_asterisk_electric_blast(size=512):
    """
    Creates an asterisk '*' blast:
    - 8 primary electric lightning prongs spreading outward in all directions
    - Secondary jagged sub-branches crackling off each prong
    - Big glowing center ball blending into the roots
    - Pure black background with clean transparent alpha
    """
    img = np.zeros((size, size), dtype=np.float32)
    cx, cy = (size - 1) / 2.0, (size - 1) / 2.0

    # 1. Generate 8 electric jagged prongs (0, 45, 90, 135, 180, 225, 270, 315 deg)
    np.random.seed(42) # Consistent, beautiful artistic lightning pattern
    num_prongs = 8
    angles = np.linspace(0, 2 * np.pi, num_prongs, endpoint=False)

    for i, base_angle in enumerate(angles):
        # Vary prong length slightly for organic electric energy
        prong_len = 0.88 + 0.12 * np.sin(i * 1.5)
        max_dist = (size / 2.0 - 15) * prong_len

        # Generate jagged path from center out to max_dist
        steps = 45
        cur_x, cur_y = cx, cy
        path = [(cur_x, cur_y)]

        for s in range(1, steps + 1):
            t = s / float(steps)
            dist = t * max_dist
            # Small jagged jitter perpendicular to main ray
            jitter_amp = 7.0 * np.sin(t * np.pi) * (1.0 + 0.5 * np.random.randn())
            perp_angle = base_angle + np.pi / 2.0

            target_x = cx + np.cos(base_angle) * dist + np.cos(perp_angle) * jitter_amp
            target_y = cy + np.sin(base_angle) * dist + np.sin(perp_angle) * jitter_amp
            path.append((target_x, target_y))

            # Add occasional sub-branch branching off the prong
            if s == 18 or s == 30:
                branch_angle = base_angle + (0.45 if np.random.rand() > 0.5 else -0.45)
                b_dist = max_dist * 0.35
                b_steps = 15
                bx, by = target_x, target_y
                b_path = [(bx, by)]
                for bs in range(1, b_steps + 1):
                    bt = bs / float(b_steps)
                    b_jitter = 3.5 * np.sin(bt * np.pi) * np.random.randn()
                    b_perp = branch_angle + np.pi / 2.0
                    b_tx = target_x + np.cos(branch_angle) * (bt * b_dist) + np.cos(b_perp) * b_jitter
                    b_ty = target_y + np.sin(branch_angle) * (bt * b_dist) + np.sin(b_perp) * b_jitter
                    b_path.append((b_tx, b_ty))

                # Stamp sub-branch onto img
                for pi in range(len(b_path) - 1):
                    x1, y1 = b_path[pi]
                    x2, y2 = b_path[pi + 1]
                    stamp_line(img, x1, y1, x2, y2, width=1.5, falloff=3.5, intensity=0.65)

        # Stamp main prong onto img
        for pi in range(len(path) - 1):
            x1, y1 = path[pi]
            x2, y2 = path[pi + 1]
            t_along = pi / float(len(path))
            w = max(1.2, 3.8 * (1.0 - t_along * 0.7))
            fall = max(3.0, 9.0 * (1.0 - t_along * 0.6))
            stamp_line(img, x1, y1, x2, y2, width=w, falloff=fall, intensity=1.0)

    # 2. Add the big glowing center ball
    y, x = np.ogrid[:size, :size]
    r = np.sqrt((x - cx)**2 + (y - cy)**2)
    max_ball_r = size * 0.22
    norm_ball_r = np.clip(r / max_ball_r, 0.0, 1.0)
    center_ball = np.clip(1.0 - norm_ball_r**1.6, 0.0, 1.0) * 1.0
    center_halo = np.clip(1.0 - (r / (size * 0.40)), 0.0, 1.0)**2.5 * 0.6

    img = np.maximum(img, center_ball)
    img = np.clip(img + center_halo * 0.5, 0.0, 1.0)

    # Create RGBA
    img_data = np.zeros((size, size, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(img * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(img * 255.0, 0, 255).astype(np.uint8)

    res = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Electric_Asterisk_Blast.png")
    res.save(path, "PNG")
    print(f"Saved {path}")

def stamp_line(img, x1, y1, x2, y2, width, falloff, intensity):
    size = img.shape[0]
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

    val = np.clip(1.0 - (dist / falloff), 0.0, 1.0)**1.8 * intensity
    img[min_y:max_y, min_x:max_x] = np.maximum(img[min_y:max_y, min_x:max_x], val)

def generate_helix_rope_texture(width=512, height=128):
    """
    Generates a glowing electric rope / braided spark ribbon texture.
    Repeats seamlessly horizontally so as it scrolls, it looks like a glowing rotating electrical cord.
    """
    x = np.linspace(0, 2 * np.pi * 3, width) # 3 full waves
    y = np.linspace(-1, 1, height)
    X, Y = np.meshgrid(x, y)

    # Braided sinusoidal strands
    strand1 = np.sin(X) * 0.4
    strand2 = np.sin(X + np.pi) * 0.4

    dist1 = np.abs(Y - strand1)
    dist2 = np.abs(Y - strand2)

    glow1 = np.clip(1.0 - dist1 / 0.35, 0.0, 1.0)**2.0
    glow2 = np.clip(1.0 - dist2 / 0.35, 0.0, 1.0)**2.0
    core1 = np.clip(1.0 - dist1 / 0.12, 0.0, 1.0)**3.0
    core2 = np.clip(1.0 - dist2 / 0.12, 0.0, 1.0)**3.0

    total = np.clip((glow1 + glow2) * 0.6 + (core1 + core2) * 0.7, 0.0, 1.0)

    # Edge fade along Y
    edge_fade = np.clip(1.0 - np.abs(Y)**2.0, 0.0, 1.0)
    total = total * edge_fade

    img_data = np.zeros((height, width, 4), dtype=np.uint8)
    for c in range(3):
        img_data[:, :, c] = np.clip(total * 255.0, 0, 255).astype(np.uint8)
    img_data[:, :, 3] = np.clip(total * 255.0, 0, 255).astype(np.uint8)

    res = Image.fromarray(img_data, mode='RGBA')
    path = os.path.join(output_dir, "Electric_Helix_Rope.png")
    res.save(path, "PNG")
    print(f"Saved {path}")

generate_glowing_ball()
generate_asterisk_electric_blast()
generate_helix_rope_texture()
print("All textures generated successfully!")
