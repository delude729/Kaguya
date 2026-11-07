"""Match offering-water cyan to Kaguya while preserving silhouettes and alpha.

Local Pillow edit; backups under output/water-color-originals make reruns stable.
"""
from pathlib import Path
from statistics import median
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
BOXES = {
    'kaguya': (175, 280, 217, 324),
    'hayasaka': (118, 249, 174, 328),
    'fujiwara': (121, 257, 168, 314),
}

def is_water(p):
    return p[3] > 0 and p[2] - p[0] > 25 and p[1] - p[0] > 15

def water_median(im, box):
    pixels = [p for p in im.crop(box).get_flattened_data() if is_water(p)]
    return [median(p[c] for p in pixels) for c in range(3)]

def main():
    backup = ROOT / 'output/water-color-originals'
    backup.mkdir(parents=True, exist_ok=True)
    reference = Image.open(ROOT / 'assets/kaguya/water-offer.png').convert('RGBA')
    target = water_median(reference, BOXES['kaguya'])
    previews = []
    for name in ('hayasaka', 'fujiwara'):
        path = ROOT / 'assets' / name / 'water-offer.png'
        original = backup / (name + '.png')
        if not original.exists():
            original.write_bytes(path.read_bytes())
        before = Image.open(original).convert('RGBA')
        after = before.copy()
        box = BOXES[name]
        source = water_median(before, box)
        count = 0
        for y in range(box[1], box[3]):
            for x in range(box[0], box[2]):
                p = before.getpixel((x, y))
                if not is_water(p):
                    continue
                # Fade the adjustment at neutral outlines and reflections.
                weight = min(1.0, (p[2]-p[0]-25)/20, (p[1]-p[0]-15)/15)
                rgb = [round(p[c]*(1-weight) + max(0,min(255,target[c]+.45*(p[c]-source[c])))*weight) for c in range(3)]
                after.putpixel((x,y), (*rgb,p[3]))
                count += 1
        assert after.getchannel('A').tobytes() == before.getchannel('A').tobytes()
        after.save(path)
        print(name, 'edited pixels:', count, 'water median:', water_median(after,box))
        previews.extend([before,after])
    canvas = Image.new('RGBA',(512*5,576),(250,248,245,255))
    for i,im in enumerate([reference]+previews):
        canvas.alpha_composite(im,(i*512,0))
    canvas.convert('RGB').save(ROOT/'verification/water-color-comparison.jpg')

if __name__ == '__main__':
    main()
