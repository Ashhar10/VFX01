"""
Generate professional, high-quality, pure-white/grayscale cloud and smoke textures for VFX01.
All textures have:
- Clean RGB (pure white with internal density variation)
- True feathered Alpha channel (smooth falloff to 0)
- Zero RGB outside the alpha boundary (no bleed, no quad edges in Additive or Alpha blend)
- Generous outer padding so edges never touch the 0..1 UV borders
"""

import os
import numpy as np
from PIL import Image

def generate_fbm_noise(size, octaves=5, persistence=0.5, lacunarity=2.0, seed=123):
    """Generates smooth fractal Brownian motion noise using bicubic upsampling."""
    rng = np.random.RandomState(seed)
    field = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    freq = 6
    tot_amp = 0.0

    for _ in range(octaves):
        grid = rng.randn(freq, freq).astype(np.float32)
        img = Image.fromarray(grid, mode='F')
        upsampled = np.array(img.resize((size, size), resample=Image.Resampling.BICUBIC))
        field += amp * upsampled
        tot_amp += amp
        amp *= persistence
        freq = int(freq * lacunarity)
        if freq > size:
            break

    field /= tot_amp
    # Normalize to -1 .. 1
    std = np.std(field)
    if std > 1e-5:
        field = (field - np.mean(field)) / std
    return np.clip(field * 0.4, -1.0, 1.0)

def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0 + 1e-7), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)

def create_billowing_cloud_plume(size=512, seed=777):
    """
    Creates a voluminous, puffy, billowing smoke/cloud texture (MiasmaSmoke_Plume.png).
    Organic multi-lobed cloud puff with rich internal folds and soft dissolving edges.
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    
    # Normalized coordinates [-1, 1]
    nx = (x - cx) / (cx * 0.95)
    ny = (y - cy) / (cy * 0.95)
    
    # 5 overlapping cloud puff centers to create an organic cumulus billow shape
    lobes = [
        (0.00,  0.05, 0.58, 1.00),  # Central main body
        (-0.25, -0.10, 0.42, 0.85), # Upper-left billow
        ( 0.22, -0.12, 0.45, 0.90), # Upper-right billow
        (-0.18,  0.22, 0.44, 0.80), # Lower-left billow
        ( 0.20,  0.20, 0.40, 0.82), # Lower-right billow
    ]
    
    # Domain warp noise for organic turbulent distortion
    warp_x = generate_fbm_noise(size, octaves=4, seed=seed + 1)
    warp_y = generate_fbm_noise(size, octaves=4, seed=seed + 2)
    
    # High-frequency cloud detail noise
    detail_noise = generate_fbm_noise(size, octaves=5, persistence=0.55, seed=seed + 3)
    micro_noise = generate_fbm_noise(size, octaves=6, persistence=0.5, seed=seed + 4)
    
    combined_noise = 0.7 * detail_noise + 0.3 * micro_noise
    
    density = np.zeros((size, size), dtype=np.float32)
    
    for lx, ly, lrad, lweight in lobes:
        dx = nx - lx + 0.15 * warp_x
        dy = ny - ly + 0.15 * warp_y
        dist = np.sqrt(dx * dx + dy * dy) / lrad
        
        # Soft puff falloff
        puff = np.clip(1.0 - dist, 0.0, 1.0)
        puff = smoothstep(0.0, 1.0, puff)
        density += puff * lweight
        
    density = np.clip(density / 1.7, 0.0, 1.0)
    
    # Add billow folds and internal cloud shading
    cloud_body = density * (1.0 + 0.35 * combined_noise)
    cloud_body = np.clip(cloud_body, 0.0, 1.0)
    
    # Distance from center for radial fade to ensure borders are clean 0
    r_center = np.sqrt(nx * nx + ny * ny)
    outer_fade = smoothstep(0.92, 0.55, r_center)
    
    alpha = cloud_body * outer_fade
    # Smooth soft curve for lush cloud transparency
    alpha = np.clip(smoothstep(0.05, 0.85, alpha), 0.0, 1.0)
    
    # RGB is clean white/soft cream with internal self-shadowing in creases
    # Deep billow creases are slightly darker (~0.85), high billow peaks are pure white (1.0)
    light_shading = 0.82 + 0.18 * smoothstep(-0.4, 0.4, combined_noise)
    rgb = np.ones((size, size, 3), dtype=np.float32) * light_shading[:, :, np.newaxis]
    
    # Premultiply RGB with outer fade so borders are absolute black (0,0,0)
    rgb *= outer_fade[:, :, np.newaxis]
    
    rgba = np.dstack((rgb, alpha[:, :, np.newaxis]))
    rgba_bytes = (np.clip(rgba, 0.0, 1.0) * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def create_soft_smoke_wisp(size=512, seed=888):
    """
    Creates a soft, wispy smoke tendril / vapor curl (MiasmaSmoke_Wisp.png).
    Feathered, organic, wispy swirl (NO sharp blade/slash geometry!).
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    
    nx = (x - cx) / (cx * 0.95)
    ny = (y - cy) / (cy * 0.95)
    
    # Spiral smoke tendril path
    angle = np.arctan2(ny, nx)
    r = np.sqrt(nx * nx + ny * ny)
    
    warp1 = generate_fbm_noise(size, octaves=4, seed=seed + 10)
    warp2 = generate_fbm_noise(size, octaves=5, persistence=0.55, seed=seed + 11)
    
    # Curved arch / wisp spine
    # Arc from bottom-center curving up and around to the right
    t = (ny + 0.85) / 1.7 # 0 at bottom, 1 at top
    t = np.clip(t, 0.0, 1.0)
    
    # Desired center X along the curve: smooth S-curve / arc
    target_x = 0.35 * np.sin(t * np.pi) + 0.15 * warp1
    
    dist_to_spine = np.abs(nx - target_x)
    
    # Spine thickness tapers at bottom and top, wide in middle
    thickness = 0.28 * np.sin(t * np.pi * 0.95) + 0.06 * warp2
    thickness = np.clip(thickness, 0.02, 0.45)
    
    # Soft lateral falloff
    lateral_falloff = 1.0 - smoothstep(0.0, thickness, dist_to_spine)
    
    # Vertical fade
    vertical_fade = smoothstep(0.02, 0.25, t) * smoothstep(0.98, 0.70, t)
    
    # Smoky wisps detail
    wisps = generate_fbm_noise(size, octaves=5, persistence=0.6, seed=seed + 12)
    wisp_density = lateral_falloff * vertical_fade * (1.0 + 0.4 * wisps)
    
    r_center = np.sqrt(nx * nx + ny * ny)
    outer_fade = smoothstep(0.90, 0.60, r_center)
    
    alpha = np.clip(wisp_density * outer_fade, 0.0, 1.0)
    alpha = smoothstep(0.04, 0.80, alpha)
    
    # Clean white RGB with soft density
    shading = 0.85 + 0.15 * smoothstep(-0.3, 0.3, wisps)
    rgb = np.ones((size, size, 3), dtype=np.float32) * shading[:, :, np.newaxis]
    rgb *= outer_fade[:, :, np.newaxis]
    
    rgba = np.dstack((rgb, alpha[:, :, np.newaxis]))
    rgba_bytes = (np.clip(rgba, 0.0, 1.0) * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def create_soft_sparkle_cloud(size=256, seed=999):
    """
    Creates a soft, glowing cloud ember / sparkle (EnergyMote_CrossStar.png).
    Pure white Gaussian core with soft misty cloud halo.
    Zero RGB and Alpha at borders to eliminate any quad outline!
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    
    nx = (x - cx) / cx
    ny = (y - cy) / cy
    r = np.sqrt(nx * nx + ny * ny)
    
    # Gentle organic perturbation
    noise = generate_fbm_noise(size, octaves=3, seed=seed)
    r_warped = r + 0.06 * noise * smoothstep(0.1, 0.6, r)
    
    # Intense central core (bright white)
    core = np.exp(-((r / 0.22) ** 2))
    
    # Misty outer cloud halo
    halo = np.exp(-((r_warped / 0.52) ** 2)) * 0.45
    
    combined = core + halo
    
    # Absolute zero fade at edge (clean cutoff by radius 0.75)
    border_fade = smoothstep(0.78, 0.50, r)
    
    alpha = np.clip(combined * border_fade, 0.0, 1.0)
    
    # RGB is pure white at core, softly tinted at halo, and ZERO at borders
    rgb = np.ones((size, size, 3), dtype=np.float32) * border_fade[:, :, np.newaxis]
    
    rgba = np.dstack((rgb, alpha[:, :, np.newaxis]))
    rgba_bytes = (np.clip(rgba, 0.0, 1.0) * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def create_ethereal_soul_orb(size=256, seed=1010):
    """
    Creates an ethereal floating soul ember / orb (SoulOrb_SkullMote.png).
    Bright luminous core, misty cloudy halo, soft wispy vapor.
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    
    nx = (x - cx) / cx
    ny = (y - cy) / cy
    r = np.sqrt(nx * nx + ny * ny)
    
    noise1 = generate_fbm_noise(size, octaves=4, seed=seed)
    noise2 = generate_fbm_noise(size, octaves=4, seed=seed + 1)
    
    r_warped = r + 0.08 * noise1 * smoothstep(0.15, 0.7, r)
    
    # Inner bright sphere
    inner_core = np.exp(-((r / 0.25) ** 2))
    
    # Ethereal vapor body
    vapor_body = np.clip(1.0 - smoothstep(0.15, 0.65, r_warped), 0.0, 1.0)
    vapor = vapor_body * (1.0 + 0.3 * noise2)
    
    density = 0.6 * inner_core + 0.4 * vapor
    
    border_fade = smoothstep(0.82, 0.58, r)
    alpha = np.clip(density * border_fade, 0.0, 1.0)
    alpha = smoothstep(0.03, 0.90, alpha)
    
    rgb = np.ones((size, size, 3), dtype=np.float32) * border_fade[:, :, np.newaxis]
    
    rgba = np.dstack((rgb, alpha[:, :, np.newaxis]))
    rgba_bytes = (np.clip(rgba, 0.0, 1.0) * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def main():
    target_dir = r"D:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Textures"
    
    print("Generating MiasmaSmoke_Plume.png...")
    plume = create_billowing_cloud_plume(512, seed=777)
    plume.save(os.path.join(target_dir, "MiasmaSmoke_Plume.png"), "PNG")
    plume.save(os.path.join(target_dir, "SmokePuff.png"), "PNG")
    
    print("Generating MiasmaSmoke_Wisp.png...")
    wisp = create_soft_smoke_wisp(512, seed=888)
    wisp.save(os.path.join(target_dir, "MiasmaSmoke_Wisp.png"), "PNG")
    
    print("Generating EnergyMote_CrossStar.png (Soft Glow Ember)...")
    sparkle = create_soft_sparkle_cloud(256, seed=999)
    sparkle.save(os.path.join(target_dir, "EnergyMote_CrossStar.png"), "PNG")
    
    print("Generating SoulOrb_SkullMote.png (Ethereal Orb)...")
    soul = create_ethereal_soul_orb(256, seed=1010)
    soul.save(os.path.join(target_dir, "SoulOrb_SkullMote.png"), "PNG")
    
    print("All cloud textures successfully generated!")

if __name__ == '__main__':
    main()
