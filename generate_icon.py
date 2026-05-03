"""
Generate DocuFiller application icon (app.ico + app.png).

Design: A stylized Word document (blue rectangle with folded corner)
        with a green checkmark overlay indicating "filled/completed".
"""
from PIL import Image, ImageDraw
import os

SIZE = 256
BG = (0, 0, 0, 0)  # transparent

# Professional color palette
DOC_BODY = (43, 87, 154, 255)       # #2B579A - Word blue
DOC_DARK = (30, 65, 120, 255)       # darker blue for depth
DOC_LIGHT = (74, 144, 226, 255)     # #4A90D9 - accent blue
FOLD_COLOR = (190, 210, 235, 255)   # light blue for corner fold
CHECK_GREEN = (76, 175, 80, 255)    # #4CAF50
CHECK_DARK = (56, 142, 60, 255)     # darker green for stroke
LINE_BLUE = (200, 220, 240, 255)    # light blue lines on document
BADGE_BG = (76, 175, 80, 255)       # green badge background


def draw_icon(size=256):
    """Draw the DocuFiller icon at the given size."""
    img = Image.new('RGBA', (size, size), BG)
    d = ImageDraw.Draw(img)

    # Scale factor relative to 256
    s = size / 256.0

    # Document rectangle with rounded corners
    doc_left = int(40 * s)
    doc_top = int(20 * s)
    doc_right = int(196 * s)
    doc_bottom = int(236 * s)
    fold_size = int(36 * s)
    radius = int(8 * s)

    # Draw document shadow
    shadow_offset = int(4 * s)
    d.rounded_rectangle(
        [doc_left + shadow_offset, doc_top + shadow_offset,
         doc_right + shadow_offset, doc_bottom + shadow_offset],
        radius=radius, fill=(0, 0, 0, 40)
    )

    # Draw document body
    d.rounded_rectangle(
        [doc_left, doc_top, doc_right, doc_bottom],
        radius=radius, fill=DOC_BODY
    )

    # Draw corner fold (top-right)
    fold_points = [
        (doc_right - fold_size, doc_top),
        (doc_right, doc_top + fold_size),
        (doc_right - fold_size, doc_top + fold_size),
    ]
    d.polygon(fold_points, fill=FOLD_COLOR)

    # Draw fold shadow line
    d.line(
        [(doc_right - fold_size, doc_top),
         (doc_right - fold_size, doc_top + fold_size),
         (doc_right, doc_top + fold_size)],
        fill=DOC_DARK, width=max(1, int(1.5 * s))
    )

    # Draw "text lines" on document to suggest content
    line_left = int(58 * s)
    line_right = int(170 * s)
    line_height = int(8 * s)
    line_gap = int(18 * s)
    y_start = int(80 * s)

    for i in range(6):
        y = y_start + i * line_gap
        # Last line is shorter (like end of paragraph)
        xr = line_right if i < 5 else int(130 * s)
        d.rounded_rectangle(
            [line_left, y, xr, y + line_height],
            radius=max(1, int(2 * s)),
            fill=LINE_BLUE
        )

    # Draw green checkmark badge (bottom-right overlay circle)
    badge_cx = int(180 * s)
    badge_cy = int(210 * s)
    badge_r = int(38 * s)

    # Badge shadow
    d.ellipse(
        [badge_cx - badge_r + int(2 * s), badge_cy - badge_r + int(2 * s),
         badge_cx + badge_r + int(2 * s), badge_cy + badge_r + int(2 * s)],
        fill=(0, 0, 0, 50)
    )

    # Badge background
    d.ellipse(
        [badge_cx - badge_r, badge_cy - badge_r,
         badge_cx + badge_r, badge_cy + badge_r],
        fill=BADGE_BG
    )

    # Draw checkmark inside badge
    check_w = max(2, int(5 * s))
    # Check points scaled to badge center
    pts = [
        (badge_cx - int(16 * s), badge_cy + int(2 * s)),
        (badge_cx - int(4 * s), badge_cy + int(14 * s)),
        (badge_cx + int(18 * s), badge_cy - int(12 * s)),
    ]
    d.line(pts[:2], fill=(255, 255, 255, 255), width=check_w)
    d.line(pts[1:], fill=(255, 255, 255, 255), width=check_w)

    return img


def main():
    out_dir = os.path.join(os.path.dirname(__file__), 'Resources')
    os.makedirs(out_dir, exist_ok=True)

    # Generate 256x256 master image
    master = draw_icon(256)

    # Save PNG
    png_path = os.path.join(out_dir, 'app.png')
    master.save(png_path, 'PNG')
    print(f'Saved {png_path}')

    # Generate multi-resolution ICO
    ico_sizes = [16, 32, 48, 64, 128, 256]
    ico_images = []
    for sz in ico_sizes:
        resized = master.resize((sz, sz), Image.LANCZOS)
        ico_images.append(resized)

    ico_path = os.path.join(out_dir, 'app.ico')
    # Save ICO with all sizes
    ico_images[0].save(
        ico_path,
        format='ICO',
        append_images=ico_images[1:],
        sizes=[(sz, sz) for sz in ico_sizes]
    )
    print(f'Saved {ico_path} with sizes: {ico_sizes}')

    # Verify
    verify = Image.open(ico_path)
    print(f'Verification: ICO info sizes = {verify.info.get("sizes", "unknown")}')
    print(f'Verification: PNG size = {master.size}')


if __name__ == '__main__':
    main()
