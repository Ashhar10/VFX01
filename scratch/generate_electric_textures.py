import os
import math
import random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

random.seed(42)
np.random.seed(42)

textures_dir = r"d:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Grave_Bolt\Textures"
os.makedirs(textures_dir, exist_ok=True)

# -------------------------------------------------------------------------
# 1. Electric_Jagged_Arcs.png (Image 4: Sharp jagged lightning branches)
# -------------------------------------------------------------------------
# We'll create a 1024x1024 texture containing sharp vertical/directional jagged lightning forks
# that taper from a bright root to razor-sharp branching tips.
def generate_jagged_arcs():
    w, h = 1024, 1024
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # We draw 4 distinct jagged electric forks in 4 quadrants, or a full vertical lightning fork
    # For a stretched particle or sprite, a vertical lightning branch pointing upward is ideal:
    # Root at bottom center (512, 1000), tip at top (512, 50), with multiple jagged forks.
    
    def draw_branch(start_pt, end_pt, depth=0, max_depth=3, width=12):
        if depth > max_depth:
            return
        
        x0, y0 = start_pt
        x1, y1 = end_pt
        dist = math.hypot(x1 - x0, y1 - y0)
        if dist < 8:
            return
        
        steps = max(6, int(dist / 25))
        pts = [start_pt]
        
        dx = (x1 - x0) / steps
        dy = (y1 - y0) / steps
        
        # perpendicular vector
        length = math.hypot(dx, dy)
        if length == 0:
            return
        px = -dy / length
        py = dx / length
        
        for i in range(1, steps):
            envelope = math.sin((i / steps) * math.pi)
            jitter = (random.random() - 0.5) * 65.0 * envelope * (1.0 / (depth + 1))
            cur_x = x0 + dx * i + px * jitter
            cur_y = y0 + dy * i + py * jitter
            pts.append((cur_x, cur_y))
            
            # Chance to spawn a sub-fork
            if depth < max_depth and random.random() < 0.35 and i > 1 and i < steps - 1:
                fork_angle = (random.random() - 0.5) * 1.2
                fork_len = dist * random.uniform(0.3, 0.6)
                fork_dir_x = (dx * math.cos(fork_angle) - dy * math.sin(fork_angle)) / length
                fork_dir_y = (dx * math.sin(fork_angle) + dy * math.cos(fork_angle)) / length
                fork_end = (cur_x + fork_dir_x * fork_len, cur_y + fork_dir_y * fork_len)
                draw_branch((cur_x, cur_y), fork_end, depth + 1, max_depth, max(2, int(width * 0.6)))
        
        pts.append(end_pt)
        
        # Draw the line segments with glow layers
        for i in range(len(pts) - 1):
            pA = pts[i]
            pB = pts[i + 1]
            seg_w = max(1, int(width * (1.0 - (i / steps) * 0.5)))
            
            # Outer cyan glow
            draw.line([pA, pB], fill=(0, 210, 255, int(140 / (depth + 1))), width=seg_w * 4)
            # Mid electric blue/white
            draw.line([pA, pB], fill=(120, 240, 255, int(220 / (depth + 1))), width=seg_w * 2)
            # Inner pure white core
            draw.line([pA, pB], fill=(255, 255, 255, 255), width=max(1, seg_w))

    # Main trunk
    draw_branch((512, 980), (512, 60), depth=0, max_depth=3, width=14)
    # Secondary side trunks
    draw_branch((512, 850), (280, 200), depth=1, max_depth=3, width=9)
    draw_branch((512, 850), (740, 220), depth=1, max_depth=3, width=9)

    # Blur slightly for smooth bloom falloff
    glow = img.filter(ImageFilter.GaussianBlur(radius=3))
    # Composite white core on top of glow
    result = Image.alpha_composite(glow, img)
    
    path = os.path.join(textures_dir, "Electric_Jagged_Arcs.png")
    result.save(path)
    print(f"Saved: {path}")

# -------------------------------------------------------------------------
# 2. Ground_Lightning_Crawlers.png (Image 3: Ground creeping lightning branches)
# -------------------------------------------------------------------------
def generate_ground_crawlers():
    w, h = 1024, 1024
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx, cy = 512, 512
    num_branches = 16

    for b in range(num_branches):
        base_angle = (b / num_branches) * math.pi * 2.0 + random.uniform(-0.1, 0.1)
        max_dist = random.uniform(280, 480)
        
        cur_pt = (cx, cy)
        steps = int(max_dist / 22)
        cur_angle = base_angle
        
        for s in range(steps):
            cur_angle += random.uniform(-0.35, 0.35)
            step_len = random.uniform(18, 30)
            next_pt = (cur_pt[0] + math.cos(cur_angle) * step_len,
                       cur_pt[1] + math.sin(cur_angle) * step_len)
            
            width = max(1, int(10 * (1.0 - s / steps)))
            
            # Outer cyan glow
            draw.line([cur_pt, next_pt], fill=(0, 200, 255, 120), width=width * 3)
            # Core white
            draw.line([cur_pt, next_pt], fill=(255, 255, 255, 255), width=width)
            
            # Sub-tendril
            if random.random() < 0.4 and s > 2:
                sub_angle = cur_angle + random.choice([-0.6, 0.6]) + random.uniform(-0.2, 0.2)
                sub_len = random.uniform(40, 100)
                sub_end = (next_pt[0] + math.cos(sub_angle) * sub_len,
                           next_pt[1] + math.sin(sub_angle) * sub_len)
                draw.line([next_pt, sub_end], fill=(0, 220, 255, 160), width=max(1, width - 1))
                draw.line([next_pt, sub_end], fill=(255, 255, 255, 255), width=max(1, int(width * 0.5)))
                
            cur_pt = next_pt

    # Center bright white-hot core
    for r in range(45, 0, -3):
        alpha = int(255 * (1.0 - r / 45.0))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 255, 255, alpha))

    glow = img.filter(ImageFilter.GaussianBlur(radius=4))
    result = Image.alpha_composite(glow, img)

    path = os.path.join(textures_dir, "Ground_Lightning_Crawlers.png")
    result.save(path)
    print(f"Saved: {path}")

# -------------------------------------------------------------------------
# 3. Stylized_Electric_Burst.png (Image 2: Sharp anime stylized flame/electric burst)
# -------------------------------------------------------------------------
def generate_stylized_burst():
    w, h = 512, 512
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx, cy = 256, 256
    
    # Generate sharp anime spikes radiating outwards
    num_spikes = 20
    outer_pts = []
    
    for i in range(num_spikes):
        angle = (i / num_spikes) * math.pi * 2.0
        # alternate long and short spikes
        if i % 2 == 0:
            rad = random.uniform(180, 240)
        else:
            rad = random.uniform(70, 110)
        
        x = cx + math.cos(angle) * rad
        y = cy + math.sin(angle) * rad
        outer_pts.append((x, y))
    
    # Outer electric cyan/gold rim
    draw.polygon(outer_pts, fill=(0, 220, 255, 230))
    
    # Inner sharper bright core
    inner_pts = []
    for i in range(num_spikes):
        angle = (i / num_spikes) * math.pi * 2.0
        if i % 2 == 0:
            rad = random.uniform(120, 160)
        else:
            rad = random.uniform(40, 65)
        x = cx + math.cos(angle) * rad
        y = cy + math.sin(angle) * rad
        inner_pts.append((x, y))
    draw.polygon(inner_pts, fill=(200, 250, 255, 255))
    
    # Pure white hot center
    core_pts = []
    for i in range(num_spikes):
        angle = (i / num_spikes) * math.pi * 2.0
        if i % 2 == 0:
            rad = random.uniform(60, 90)
        else:
            rad = random.uniform(20, 35)
        x = cx + math.cos(angle) * rad
        y = cy + math.sin(angle) * rad
        core_pts.append((x, y))
    draw.polygon(core_pts, fill=(255, 255, 255, 255))

    glow = img.filter(ImageFilter.GaussianBlur(radius=3))
    result = Image.alpha_composite(glow, img)

    path = os.path.join(textures_dir, "Stylized_Electric_Burst.png")
    result.save(path)
    print(f"Saved: {path}")

# -------------------------------------------------------------------------
# 4. Flame_Tail_Ribbon.png (Smooth stylized flame ribbon for TrailRenderer)
# -------------------------------------------------------------------------
def generate_flame_tail_ribbon():
    w, h = 512, 128
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    
    # Create smooth horizontal gradient:
    # Left = Head (intense white/cyan, thick)
    # Right = Tail (fading to 0 alpha, tapering)
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    
    for y in range(h):
        ny = (y - h / 2.0) / (h / 2.0) # -1 to 1
        for x in range(w):
            nx = x / float(w) # 0 (head) to 1 (tail)
            
            # Width envelope: thick at head, tapering to needle at tail
            half_w = 1.0 - math.pow(nx, 0.7) * 0.95
            if abs(ny) > half_w:
                continue
            
            dist_center = abs(ny) / half_w # 0 at spine, 1 at edge
            
            # Spine intensity
            spine = math.pow(1.0 - dist_center, 1.8)
            alpha_along = math.pow(1.0 - nx, 0.8)
            
            alpha = int(255 * spine * alpha_along)
            
            # Color: White core transitioning to cyan/green glow
            core_factor = math.pow(1.0 - dist_center, 4.0) * (1.0 - nx * 0.5)
            r = int(255 * core_factor + (1.0 - core_factor) * 0)
            g = int(255 * core_factor + (1.0 - core_factor) * 230)
            b = int(255 * core_factor + (1.0 - core_factor) * 255)
            
            arr[y, x] = [r, g, b, alpha]
            
    res = Image.fromarray(arr, "RGBA")
    glow = res.filter(ImageFilter.GaussianBlur(radius=2))
    final = Image.alpha_composite(glow, res)
    
    path = os.path.join(textures_dir, "Flame_Tail_Ribbon.png")
    final.save(path)
    print(f"Saved: {path}")

generate_jagged_arcs()
generate_ground_crawlers()
generate_stylized_burst()
generate_flame_tail_ribbon()
print("All electric burst and flame tail textures generated successfully!")
