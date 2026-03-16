TEXTURE FOLDER
==============

Place your custom textures here. They will be automatically loaded at runtime.

FILE NAMING:
- grass.png (or .jpg) - Ground/grass texture
- road.png (or .jpg)  - Road/asphalt texture
- car_wrap.png        - Player car body texture

RECOMMENDED SPECS:

GRASS TEXTURE:
- Size: 512x512 or 1024x1024 pixels
- Format: PNG or JPG
- Style: Seamless/tileable (edges match when repeated)
- Tips: Use realistic grass or stylized depending on your look

ROAD TEXTURE:
- Size: 512x512 or 1024x1024 pixels
- Format: PNG or JPG
- Style: Seamless/tileable asphalt pattern
- Tips: Include some weathering, cracks, or tire marks for realism

CAR WRAP:
- Size: 1024x1024 or 2048x2048 pixels
- Format: PNG (supports transparency for decals)
- Style: Can be solid color, racing stripes, sponsor logos, etc.
- Tips: The wrap applies to all main body panels

TILING:
- Ground tiles 50x across the terrain
- Road tiles 10x along each road segment
- Adjust in TextureManager.cs if needed

After adding textures, Unity will auto-import them.
Press Play to see them applied!
