"""Compose the README hero art (docs/characters.png) from character sprites.

Renders the three-card banner at 1196x652 using the same palette and layout as
the committed artwork, so the image can be regenerated instead of retouched.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "characters.png"

W, H = 1196, 652
CW, CH, GAP = 360, 580, 26
X = (W - (CW * 3 + GAP * 2)) // 2
Y = (H - CH) // 2
RADIUS = 30

FONT_REG = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"

BG_TOP = (253, 244, 248)
BG_BOTTOM = (250, 238, 244)
CARD_TOP = (255, 252, 253)
CARD_BOTTOM = (255, 249, 252)
BORDER = (244, 229, 235)
NAME = (62, 48, 56)
SUBTITLE = (142, 96, 116)
ACCENT = (214, 164, 185)

NAME_SIZE, NAME_Y = 40, 471
SUB_SIZE, SUB_Y = 21, 520
ACCENT_Y = 556


def font(path, size):
    return ImageFont.truetype(path, size)


def gradient(size, top, bottom):
    w, h = size
    line = Image.new("RGB", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        line.putpixel((0, y), tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    return line.resize((w, h), Image.Resampling.NEAREST).convert("RGBA")


def rounded_mask(size, box, radius):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)
    return mask


def soft_shadow(canvas, box, radius, blur=14, offset=(0, 8), alpha=56):
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    b = (box[0] + offset[0], box[1] + offset[1], box[2] + offset[0], box[3] + offset[1])
    ImageDraw.Draw(layer).rounded_rectangle(b, radius=radius, fill=(146, 94, 118, alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(canvas, layer)


def make_card(canvas, box, radius):
    x0, y0, x1, y1 = box
    canvas = soft_shadow(canvas, box, radius)
    mask = rounded_mask(canvas.size, box, radius)
    fill = gradient((x1 - x0, y1 - y0), CARD_TOP, CARD_BOTTOM).convert("RGBA")
    card = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    card.paste(fill, (x0, y0), mask.crop(box))
    ImageDraw.Draw(card).rounded_rectangle(box, radius=radius, outline=BORDER, width=1)
    return Image.alpha_composite(canvas, card)


def character(canvas, name, pose, center_x, top, target_h, axis_x):
    src = Image.open(ROOT / "assets" / name / f"{pose}.png").convert("RGBA")
    scale = target_h / src.height
    w = round(src.width * scale)
    im = src.resize((w, target_h), Image.Resampling.LANCZOS)
    pivot = axis_x * scale
    canvas.alpha_composite(im, (round(center_x - pivot), top))
    return canvas


def compose():
    canvas = gradient((W, H), BG_TOP, BG_BOTTOM)

    cards = [
        ("kaguya", "四宫辉夜", "温柔又有点别扭的学生会副会长"),
        ("hayasaka", "早坂爱", "可靠又爱吐槽的私人女仆"),
        ("fujiwara", "藤原", "元气满满的书记，永远好奇"),
    ]

    name_f = font(FONT_BOLD, NAME_SIZE)
    sub_f = font(FONT_REG, SUB_SIZE)

    for i, (name, display, subtitle) in enumerate(cards):
        x = X + i * (CW + GAP)
        box = (x, Y, x + CW - 1, Y + CH - 1)
        canvas = make_card(canvas, box, RADIUS)

        cx = x + CW // 2
        canvas = character(canvas, name, "idle", cx, Y + 18, 360, axis_x=214)

        d = ImageDraw.Draw(canvas)
        d.text((cx, NAME_Y), display, font=name_f, fill=NAME, anchor="mm")
        d.text((cx, SUB_Y), subtitle, font=sub_f, fill=SUBTITLE, anchor="mm")
        d.rounded_rectangle((cx - 22, ACCENT_Y, cx + 22, ACCENT_Y + 4), radius=2, fill=ACCENT)

    canvas.convert("RGB").save(OUT, optimize=True)
    print("saved", OUT, canvas.size)


if __name__ == "__main__":
    compose()
