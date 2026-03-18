from mpmath import *
import glob
from PIL import Image, ImageDraw, ImageFont
import math
from flint import acb
from concurrent.futures import ProcessPoolExecutor
import os

width = 1080
height = 1920
fsize = 18
font = ImageFont.load_default()
font = ImageFont.truetype("C:\\Windows\\Fonts\\Arial.ttf", size=fsize)
font2 = ImageFont.truetype("C:\\Windows\\Fonts\\Arial.ttf", size=52)


def decimal_range(start, stop, increment):
    while start < stop: # and not math.isclose(start, stop): Py>3.5
        yield start
        start += increment
'''        
def center_text_x(draw, x, y, txt):
    text_width, text_height = draw.textsize(txt, font)
    draw.text((x-text_width/2, y), txt, fill="gray", font=font)
    
def center_text_y(draw, x, y, txt):
    text_width, text_height = draw.textsize(txt, font)
    draw.text((x-text_width, y-text_height/2), txt, fill="gray", font=font)
'''

def center_text_x(draw, x, y, txt):
    bbox = draw.textbbox((0, 0), txt, font=font)
    text_width  = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    draw.text((x - text_width/2, y), txt, fill="gray", font=font)


def center_text_y(draw, x, y, txt):
    bbox = draw.textbbox((0, 0), txt, font=font)
    text_width  = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    draw.text((x - text_width, y - text_height/2), txt, fill="gray", font=font)


            
def create_plot(r):
    img = Image.new('RGB', (width, height))
    draw = ImageDraw.Draw(img, "RGB") 
    draw.line((0,height/2, width,height/2), fill=(128,128,128), width=2)
    draw.line((width/2, 0, width/2, height), fill=(128,128,128), width=2)
    scale = 200;
    draw.text((50, 50), f"(x, yi) = \U000003B6(a+bi)", fill="yellow", font=font2, stroke_width=1)
    draw.text((50, 120), f"a={r:.2f}", fill="yellow", font=font2, stroke_width=1)
    #draw.text((50, 190), f"b=-10000 .. 10000", fill="yellow", font=font2, stroke_width=1)
    
    draw.text((width/2 - fsize, height/2 +1 ), "0", fill="gray", font=font)
    for n in range(10):
        draw.line((width/2+n*scale, height/2-3, width/2+n*scale, height/2+3), fill=(128,128,128), width=2)
        
        draw.line((width/2-n*scale, height/2-3, width/2-n*scale, height/2+3), fill=(128,128,128), width=2)
        
        draw.line((width/2-3, height/2+n*scale, width/2+3, height/2+n*scale), fill=(128,128,128), width=2)
        draw.line((width/2-3, height/2-n*scale, width/2+3, height/2-n*scale), fill=(128,128,128), width=2)
        if n>0:
            center_text_x(draw, width/2+n*scale, height/2+3, str(n))
            center_text_x(draw, width/2-n*scale, height/2+3, str(-n))
            center_text_y(draw, width/2-3, height/2+n*scale, str(-n))
            center_text_y(draw, width/2-3, height/2-n*scale, str(n))
    
    opx1 = 0
    opy1 = 0
    opx2 = 0
    opy2 = 0
    
    #pc = 1000
    #step = 200.0/pc
    
    #pc = 10000
    #step = 200.0 / pc
    
    pc = 20000          #40000 ? 20000
    step = 400.0 / pc
    
    for n in range(pc):
        k = n * step
        
        #z1 = zeta(r+k*j)
        #z2 = zeta(r-k*j)
        
        
        a1 = acb(r+k*j)
        z1 = a1.zeta()
        
        a2 = acb(r-k*j)
        z2 = a2.zeta()
        
        
        x1 = re(z1)
        y1 = im(z1)
        
        x2 = re(z2)
        y2 = im(z2)
        
        px1 = int(width/2 + x1*scale)
        py1 = int(height/2 - y1*scale)
        
        px2 = int(width/2 + x2*scale)
        py2 = int(height/2 - y2*scale)    
       
        if (n>0):
            if (abs(px1) < 10000) and (abs(py1) < 10000):
                try:
                    draw.line((opx1,opy1, px1,py1), fill=(255,255,255), width=3)
                except:
                    None
            if (abs(px1) < 10000) and (abs(py1) < 10000):
                try:
                    draw.line((opx2,opy2, px2,py2), fill=(255,255,255), width=3)
                except:
                    None
        opx1 = px1
        opy1 = py1
        opx2 = px2
        opy2 = py2 
    return img
    



def slowed_function(x):
    return math.tan((x-0.5)*0.12)

def map_range(x, a, b, c, d):
    return c + (x - a) * (d - c) / (b - a)
    

def draw_plot_for_counter(counter, total_frames):
    r = map_range(counter, 1, total_frames, -2.5, 2.5);
    #r = map_range(counter, 1, total_frames, 0.5, 2.5);
    print(f"frame={counter}: r={r}")
    if (r!=1.0):
        img = create_plot(r)
        img.save(f"out/zeta-{counter:04}.png", "PNG")
        
        
      

def main():
    total_frames = 2000
    with ProcessPoolExecutor(max_workers=os.cpu_count()) as executor:
        executor.map(
            draw_plot_for_counter,
            range(1, total_frames),
            [total_frames] * (total_frames - 1)
        )   

if __name__ == "__main__":
    print("start")
    main()
    print("done")
    