"""
Procedural Texture Generator for VFX01 Motes
Generates:
1. EnergyMote_CrossStar.png: Soft glowing cloud orb with emissive white/cream core and organic falloff.
2. SoulOrb_SkullMote.png: Ethereal floating soul orb with bone-white core, vibrant lavender/purple rim, and smoky dissolving edges.
"""

import os
import numpy as np
from PIL import Image

def generate_smooth_noise(size, res=8, octaves=4, persistence=0.5, seed=42):
    """Generates multi-octave bicubic-interpolated value noise."""
    rng = np.random.RandomState(seed)
    field = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    tot_amp = 0.0
    
    current_res = res
    for _ in range(octaves):
        rand_grid = rng.randn(current_res, current_res).astype(np.float32)
        img = Image.fromarray(rand_grid, mode='F')
        upsampled = np.array(img.resize((size, size), resample=Image.Resampling.BICUBIC))
        
        field += amp * upsampled
        tot_amp += amp
        amp *= persistence
        current_res *= 2
        if current_res > size:
            break
            
    field = field / tot_amp
    std = np.std(field)
    if std > 1e-6:
        field = (field - np.mean(field)) / std
    return np.clip(field * 0.45, -1.0, 1.0)

def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0 + 1e-7), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)

def create_energy_cloud_mote(size=256, seed=42):
    """
    Generates EnergyMote_CrossStar.png:
    - Circular soft cloud / smoke puff shape (NOT a sharp star)
    - Gentle organic edges with subtle noise variation
    - Very bright white core that falls off to softer edges
    - Alpha channel: smooth radial falloff from ~0.90 in center to 0.0 at edges
    - Subtle internal cloud texture using fractal noise
    - RGB: bright white/cream center for additive emissive glow
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    dx = (x - cx) / cx
    dy = (y - cy) / cy
    r = np.sqrt(dx**2 + dy**2)
    
    # Mild domain warp for organic cloud boundaries
    warp_x = generate_smooth_noise(size, res=4, octaves=3, seed=seed+1)
    warp_y = generate_smooth_noise(size, res=4, octaves=3, seed=seed+2)
    
    dx_w = dx + 0.08 * warp_x
    dy_w = dy + 0.08 * warp_y
    r_warped = np.sqrt(dx_w**2 + dy_w**2)
    
    # Organic edge modulation noise
    edge_noise = generate_smooth_noise(size, res=8, octaves=4, persistence=0.55, seed=seed+3)
    micro_edge = generate_smooth_noise(size, res=16, octaves=3, persistence=0.5, seed=seed+4)
    combined_edge = 0.7 * edge_noise + 0.3 * micro_edge
    
    r_eff = r_warped + 0.07 * combined_edge * smoothstep(0.15, 0.60, r)
    
    # Radial falloff
    core_gaussian = np.exp(-((r / 0.28)**2))
    body_falloff = 1.0 - smoothstep(0.15, 0.80, r_eff)
    outer_fade = smoothstep(0.85, 0.68, r)
    
    base_density = (0.55 * core_gaussian + 0.45 * body_falloff) * outer_fade
    
    # Subtle internal cloud texture
    cloud_internal = generate_smooth_noise(size, res=8, octaves=4, persistence=0.5, seed=seed+10)
    center_weight = np.clip(1.0 - r / 0.25, 0.0, 1.0)
    density = base_density * (1.0 + 0.15 * (1.0 - 0.7 * center_weight) * cloud_internal)
    
    # Alpha falloff: ~0.90 in center, smooth radial fade
    alpha = np.clip(density, 0.0, 1.0) ** 1.10
    alpha = alpha * 0.90
    alpha = np.clip(alpha, 0.0, 0.90)
    alpha *= smoothstep(0.84, 0.70, r)
    
    # RGB: Emissive white core transitioning to warm cream
    t_c = smoothstep(0.10, 0.70, r_eff)[:, :, np.newaxis]
    c_white = np.array([1.0, 1.0, 1.0])
    c_cream = np.array([1.0, 0.985, 0.92])
    c_outer = np.array([0.98, 0.955, 0.89])
    
    rgb = c_white * (1.0 - t_c) + c_cream * t_c
    t_outer = smoothstep(0.50, 0.82, r_eff)[:, :, np.newaxis]
    rgb = rgb * (1.0 - t_outer) + c_outer * t_outer
    
    # Emissive core boost
    rgb = np.clip(rgb + 0.08 * core_gaussian[:, :, np.newaxis], 0.0, 1.0)
    
    rgba = np.dstack((rgb, alpha[:, :, np.newaxis]))
    rgba_bytes = (rgba * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def create_soul_cloud_orb(size=256, seed=2024):
    """
    Generates SoulOrb_SkullMote.png:
    - Ethereal floating soul ember (NOT a skull)
    - Bright bone-white core
    - Lavender / purple rim glow
    - Smoky cloud-like dissolving edges
    - Alpha falloff with organic cloud irregularity
    """
    y, x = np.ogrid[:size, :size]
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    dx = (x - cx) / cx
    dy = (y - cy) / cy
    r = np.sqrt(dx**2 + dy**2)
    
    # Domain warping for ethereal smoky ripples
    wx1 = generate_smooth_noise(size, res=4, octaves=3, seed=seed+1)
    wy1 = generate_smooth_noise(size, res=4, octaves=3, seed=seed+2)
    wx2 = generate_smooth_noise(size, res=8, octaves=3, seed=seed+3)
    wy2 = generate_smooth_noise(size, res=8, octaves=3, seed=seed+4)
    
    warp_amount = smoothstep(0.12, 0.45, r)
    dx_w = dx + warp_amount * (0.10 * wx1 + 0.05 * wx2)
    dy_w = dy + warp_amount * (0.10 * wy1 + 0.05 * wy2)
    r_warped = np.sqrt(dx_w**2 + dy_w**2)
    
    # Smoky wisps
    smoke1 = generate_smooth_noise(size, res=8, octaves=4, persistence=0.55, seed=seed+11)
    smoke2 = generate_smooth_noise(size, res=16, octaves=3, persistence=0.5, seed=seed+12)
    wisps = 0.65 * np.abs(smoke1) + 0.35 * smoke2
    
    # Falloff & Density
    core_profile = np.exp(-((r / 0.28)**2))
    body_falloff = 1.0 - smoothstep(0.18, 0.75, r_warped + 0.08 * wisps)
    outer_fade = smoothstep(0.85, 0.65, r)
    
    base_density = (0.52 * core_profile + 0.48 * body_falloff) * outer_fade
    
    rim_weight = smoothstep(0.18, 0.50, r)
    density = base_density * (1.0 + 0.22 * rim_weight * (wisps - 0.25))
    
    # Alpha: ~0.94 at bone-white center, ~0.55-0.65 in lavender rim, dissolving softly to 0
    alpha = np.clip(density, 0.0, 1.0) ** 1.15
    alpha = alpha * 0.94
    alpha = np.clip(alpha, 0.0, 0.94)
    alpha *= smoothstep(0.86, 0.68, r)
    
    # Colors:
    c_bone = np.array([0.99, 0.985, 1.00])
    c_lavender = np.array([0.88, 0.62, 1.00])
    c_purple = np.array([0.74, 0.38, 0.96])
    c_violet = np.array([0.58, 0.24, 0.88])
    
    t1 = smoothstep(0.12, 0.32, r_warped)[:, :, np.newaxis]
    t2 = smoothstep(0.32, 0.55, r_warped)[:, :, np.newaxis]
    t3 = smoothstep(0.55, 0.78, r_warped)[:, :, np.newaxis]
    
    col = c_bone * (1.0 - t1) + c_lavender * t1
    col = col * (1.0 - t2) + c_purple * t2
    col = col * (1.0 - t3) + c_violet * t3
    
    col = np.clip(col + 0.08 * core_profile[:, :, np.newaxis], 0.0, 1.0)
    
    rgba = np.dstack((col, alpha[:, :, np.newaxis]))
    rgba_bytes = (rgba * 255.0).astype(np.uint8)
    return Image.fromarray(rgba_bytes, 'RGBA')

def main():
    target_dir = r"D:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Textures"
    
    energy_path = os.path.join(target_dir, "EnergyMote_CrossStar.png")
    energy_img = create_energy_cloud_mote(256)
    energy_img.save(energy_path, "PNG")
    print(f"Saved: {energy_path}")
    
    soul_path = os.path.join(target_dir, "SoulOrb_SkullMote.png")
    soul_img = create_soul_cloud_orb(256)
    soul_img.save(soul_path, "PNG")
    print(f"Saved: {soul_path}")

if __name__ == '__main__':
    main()
