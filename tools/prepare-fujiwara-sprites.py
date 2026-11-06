"""Prepare aligned Fujiwara poses from the generated 3-by-2 sheet (local only)."""
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'output/fujiwara/96c42b2ee0b64c9291b69ad3f8b170fc.png'
def main():
    sheet=Image.open(SOURCE).convert('RGBA')
    target=ROOT/'assets/fujiwara';target.mkdir(exist_ok=True)
    for i,name in enumerate(['idle','happy','tea','sleep','water-offer','water-sip']):
        w,h=sheet.size;c=i%3;r=i//3
        pose=sheet.crop((c*w//3,r*h//2,(c+1)*w//3,(r+1)*h//2))
        pixels=[]
        for red,green,blue,alpha in pose.getdata():
            excess=max(0,green-max(red,blue))
            if excess>20 and green>100:
                alpha=0 if excess>100 else round(255*(1-excess/100))
                green=min(green,max(red,blue))
            pixels.append((red,green,blue,alpha))
        pose.putdata(pixels)
        bounds=pose.getchannel('A').point(lambda a:255 if a>40 else 0).getbbox()
        pose=pose.crop(bounds)
        factor=min(510/pose.height,360/pose.width)
        pose=pose.resize((round(pose.width*factor),round(pose.height*factor)),Image.Resampling.LANCZOS)
        mask=pose.getchannel('A');feet=[x for y in range(round(pose.height*.94),pose.height) for x in range(pose.width) if mask.getpixel((x,y))>100]
        axis=sum(feet)/len(feet) if feet else pose.width/2
        canvas=Image.new('RGBA',(512,576));canvas.alpha_composite(pose,(round(214-axis),548-pose.height));canvas.save(target/(name+'.png'))
        print(name,canvas.getbbox())
if __name__=='__main__':main()
