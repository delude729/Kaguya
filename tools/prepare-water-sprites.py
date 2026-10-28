"""Extract the generated two-pose sheet into aligned, transparent pet sprites.

Requires Pillow. No API credentials or network calls are used here.
"""
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "output/imagegen/water-poses.png"


def extract_pose(sheet, column, name):
    width, height = sheet.size
    pose = sheet.crop((column * width // 2, 0, (column + 1) * width // 2, height)).convert("RGBA")
    pixels = []
    raw = pose.tobytes()
    for red, green, blue, alpha in zip(raw[0::4], raw[1::4], raw[2::4], raw[3::4]):
        excess = green - max(red, blue)
        if green > 100 and excess > 20:
            # Undo the green contribution on antialiased edges as well as removing
            # the solid backdrop. Costume, eyes and blue glass have no green key.
            alpha = 0 if excess > 180 else max(0, 255 - excess)
        # Remove even faint chroma spill on otherwise opaque outline pixels.
        if excess > 0:
            green = max(red, blue)
        pixels.append((red, green, blue, alpha))
    pose.putdata(pixels)
    mask = pose.getchannel("A").point(lambda value: 255 if value > 40 else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("Generated pose contains no foreground")
    pose = pose.crop(bounds)
    factor = min(510 / pose.height, 360 / pose.width)
    pose = pose.resize((round(pose.width * factor), round(pose.height * factor)), Image.Resampling.LANCZOS)
    # Anchor to the original sprite's feet/body axis, rather than the hair and
    # ribbon's asymmetric canvas center. Both frames share the same baseline.
    feet = pose.getchannel("A")
    foot_x = [x for y in range(round(pose.height * .94), pose.height)
              for x in range(pose.width) if feet.getpixel((x, y)) > 100]
    axis = sum(foot_x) / len(foot_x) if foot_x else pose.width / 2
    canvas = Image.new("RGBA", (512, 576))
    origin = (round(214 - axis), 548 - pose.height)
    canvas.alpha_composite(pose, origin)
    output = ROOT / "assets/kaguya" / f"{name}.png"
    canvas.save(output)
    print(f"{output.name}: {canvas.getbbox()}")


if __name__ == "__main__":
    with Image.open(SOURCE) as sheet:
        extract_pose(sheet, 0, "water-sip")
        extract_pose(sheet, 1, "water-offer")
