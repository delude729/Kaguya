"""Extract the two actual frames returned by image2 as a native GIF; no API calls."""
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'output/hayasaka-headpat/7053794e176844e2be9f8fa1edfef244.gif'
def match_idle_proportions(canvas):
    # Measured on the aligned 512x576 sprites: crown, chin, collar, hem, soles.
    # Keep crown and soles fixed; match idle's head height and shorten the legs.
    source_y = [0, 38, 253, 290, 465, 548, 576]
    target_y = [0, 38, 270, 308, 484, 548, 576]
    def inverse_y(y):
        for i in range(len(target_y)-1):
            if y <= target_y[i+1]:
                return source_y[i]+(y-target_y[i])*(source_y[i+1]-source_y[i])/(target_y[i+1]-target_y[i])
        return y
    def horizontal_scale(y):
        # Widen the head/shoulders to idle's width; preserve skirt and feet width.
        amount=max(0.0,min(1.0,(y-308)/176))
        amount=amount*amount*(3-2*amount)
        return 1.075*(1-amount)+amount
    mesh=[]
    for top in range(0,576,4):
        bottom=min(576,top+4)
        a,b=inverse_y(top),inverse_y(bottom)
        sa,sb=horizontal_scale(top),horizontal_scale(bottom)
        left_a,right_a=214-214/sa,214+(512-214)/sa
        left_b,right_b=214-214/sb,214+(512-214)/sb
        mesh.append(((0,top,512,bottom),(left_a,a,left_b,b,right_b,b,right_a,a)))
    return canvas.convert('RGBa').transform((512,576),Image.Transform.MESH,mesh,Image.Resampling.BICUBIC).convert('RGBA')

def main():
    sheet=Image.open(SOURCE)
    if sheet.n_frames!=2:raise ValueError('Expected the two returned animation frames')
    previews=[]
    for i,name in enumerate(['pat-notice','pat-relaxed']):
        sheet.seek(i);pose=sheet.convert('RGBA');pixels=[]
        for red,green,blue,alpha in pose.getdata():
            excess=max(0,green-max(red,blue))
            if excess>20 and green>100:
                alpha=0 if excess>100 else round(255*(1-excess/100));green=min(green,max(red,blue))
            pixels.append((red,green,blue,alpha))
        pose.putdata(pixels);bounds=pose.getchannel('A').point(lambda a:255 if a>40 else 0).getbbox();pose=pose.crop(bounds)
        factor=min(510/pose.height,360/pose.width);pose=pose.resize((round(pose.width*factor),round(pose.height*factor)),Image.Resampling.LANCZOS)
        mask=pose.getchannel('A');feet=[x for y in range(round(pose.height*.94),pose.height) for x in range(pose.width) if mask.getpixel((x,y))>100]
        axis=sum(feet)/len(feet) if feet else pose.width/2
        canvas=Image.new('RGBA',(512,576));canvas.alpha_composite(pose,(round(214-axis),548-pose.height));canvas=match_idle_proportions(canvas);canvas.save(ROOT/'assets/hayasaka'/f'{name}.png')
        preview=Image.new('RGBA',canvas.size,'#f4eff3');preview.alpha_composite(canvas);previews.append(preview.convert('RGB'))
        print(name,canvas.getbbox())
    previews[0].save(ROOT/'output/hayasaka-headpat/preview.gif',save_all=True,append_images=previews[1:],duration=[600,2600],loop=0)
if __name__=='__main__':main()
