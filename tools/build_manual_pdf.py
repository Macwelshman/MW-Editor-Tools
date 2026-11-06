import re, html
from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Table, TableStyle, Image, KeepTogether
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.pagesizes import A4
from pypdf import PdfReader

ROOT=Path(__file__).resolve().parents[1]
VERSION='0.4.259'
OUT=ROOT/f'output/pdf/MW-Editor-Tools-User-Manual-{VERSION}.pdf'
OUT.parent.mkdir(parents=True,exist_ok=True)
for name,file in [('Body','calibri.ttf'),('BodyBold','calibrib.ttf'),('BodyItalic','calibrii.ttf')]:
    pdfmetrics.registerFont(TTFont(name,'C:/Windows/Fonts/'+file))
pdfmetrics.registerFontFamily('Body',normal='Body',bold='BodyBold',italic='BodyItalic',boldItalic='BodyBold')
navy=colors.HexColor('#153b57'); teal=colors.HexColor('#07869b'); grey=colors.HexColor('#566874')
base=ParagraphStyle('body',fontName='Body',fontSize=10,leading=13,textColor=colors.HexColor('#243642'),spaceAfter=7,splitLongWords=True)
styles={
 'body':base,
 'h1':ParagraphStyle('title',parent=base,fontName='BodyBold',fontSize=31,leading=35,textColor=navy,spaceAfter=12),
 'h2':ParagraphStyle('section',parent=base,fontName='BodyBold',fontSize=21,leading=25,textColor=navy,spaceBefore=12,spaceAfter=9,keepWithNext=True),
 'h3':ParagraphStyle('subsection',parent=base,fontName='BodyBold',fontSize=13,leading=17,textColor=teal,spaceBefore=8,spaceAfter=6,keepWithNext=True),
 'table':ParagraphStyle('table',parent=base,fontSize=9.3,leading=11.8,spaceAfter=0),
 'th':ParagraphStyle('th',parent=base,fontName='BodyBold',fontSize=9.3,leading=11.8,textColor=colors.white,spaceAfter=0),
 'list':ParagraphStyle('list',parent=base,leftIndent=15,firstLineIndent=0,bulletIndent=1,spaceAfter=4),
 'code':ParagraphStyle('code',parent=base,fontName='Courier',fontSize=8.2,leading=11,backColor=colors.HexColor('#edf3f6'),borderPadding=8,spaceBefore=5,spaceAfter=12),
 'toc':ParagraphStyle('toc',parent=base,fontSize=11,leading=17,spaceBefore=3,leftIndent=0,rightIndent=18),
}
def clean(s):
    return s.replace('\u2014','-').replace('\u2013','-').replace('\u2011','-')
def slug(s):return re.sub(r'[^a-z0-9 -]','',s.lower()).replace(' ','-')
def inline(s):
    s=html.escape(clean(s))
    s=re.sub(r'`([^`]+)`',r'<font name="Courier" size="9">\1</font>',s)
    s=re.sub(r'\*\*(.+?)\*\*',r'<b>\1</b>',s)
    s=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',lambda m:'<link href="'+m[2]+'" color="#087b96">'+m[1]+'</link>',s)
    return s
class Manual(SimpleDocTemplate):
    def afterFlowable(self,f):
        if isinstance(f,Paragraph) and hasattr(f,'section_key'):
            self.canv.bookmarkPage(f.section_key)
            self.canv.addOutlineEntry(f.getPlainText(),f.section_key,0,False)
            self.notify('TOCEntry',(0,f.getPlainText(),self.page,f.section_key))
def footer(c,doc):
    w,h=A4;c.saveState()
    c.setStrokeColor(colors.HexColor('#d8e4eb'));c.line(43,39,w-43,39)
    c.setFont('Body',8);c.setFillColor(grey)
    c.drawString(43,25,'MW Editor Tools  |  User Manual  |  '+VERSION)
    c.drawRightString(w-43,25,str(doc.page))
    if doc.page>1:
        c.setFont('Body',8);c.drawString(43,h-27,'CITIES: SKYLINES II ASSET EDITOR')
    c.restoreState()

lines=(ROOT/'docs/USER_MANUAL.md').read_text(encoding='utf-8-sig').splitlines()
story=[Image(str(ROOT/'UI/src/assets/mw-editor-tools.png'),width=62,height=62,hAlign='LEFT'),Spacer(1,14),Paragraph('MW Editor Tools',styles['h1']),Paragraph('User Manual',styles['h2']),Paragraph(f'Version <b>{VERSION}</b>  |  Cities: Skylines II Asset Editor',styles['body']),Spacer(1,7),Paragraph('Element replacement, component transfer, colour variations, workspace saving and supported Undo.',styles['body']),Spacer(1,14),Paragraph('Contents',styles['h3'])]
toc=TableOfContents();toc.levelStyles=[styles['toc']];story += [toc,PageBreak()]
i=0
while i<len(lines) and lines[i]!='## Install or update':i+=1
section=0
screenshot_section=False
screenshot_count=0
while i<len(lines):
    s=lines[i].strip()
    if not s:i+=1;continue
    if s.startswith('## '):
        title=s[3:];section+=1
        screenshot_section=title=='Screenshot guide'
        if title=='Screenshot guide':story.append(PageBreak())
        p=Paragraph('<a name="'+slug(title)+'"/>'+inline(title),styles['h2']);p.section_key=slug(title);story.append(p);i+=1;continue
    if s.startswith('### '):
        if screenshot_section and screenshot_count:
            story.append(PageBreak())
        story.append(Paragraph(inline(s[4:]),styles['h3']));i+=1;continue
    picture=re.fullmatch(r'!\[([^\]]*)\]\(([^)]+)\)',s)
    if picture:
        image_path=(ROOT/'docs'/picture[2]).resolve()
        if not image_path.is_relative_to(ROOT/'docs') or not image_path.is_file():
            raise ValueError(f'Missing or invalid manual screenshot: {picture[2]}')
        screenshot=Image(str(image_path))
        scale=min((A4[0]-86)/screenshot.imageWidth,585/screenshot.imageHeight)
        screenshot.drawWidth=screenshot.imageWidth*scale
        screenshot.drawHeight=screenshot.imageHeight*scale
        screenshot.hAlign='LEFT'
        screenshot.keepWithNext=True
        story.extend([screenshot,Paragraph(inline(picture[1]),styles['body']),Spacer(1,8)])
        screenshot_count+=1
        i+=1;continue
    if s.startswith('```'):
        code=[];i+=1
        while i<len(lines) and not lines[i].strip().startswith('```'):code.append(lines[i]);i+=1
        story.append(Paragraph('<br/>'.join(html.escape(x) for x in code),styles['code']));i+=1;continue
    if s.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r':?-+:?',c) for c in cells):rows.append(cells)
            i+=1
        count=len(rows[0]);width=A4[0]-86
        first=rows[0][0]
        fractions=[.31,.69] if count==2 else [.25,.375,.375]
        if first=='Symptom':fractions=[.31,.69]
        if first=='Shortcut':fractions=[.25,.75]
        data=[[Paragraph(inline(c),styles['th'] if ri==0 else styles['table']) for c in row] for ri,row in enumerate(rows)]
        t=Table(data,colWidths=[width*f for f in fractions],repeatRows=1,hAlign='LEFT')
        t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),navy),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#eef4f7'),colors.white]),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),9),('RIGHTPADDING',(0,0),(-1,-1),9),('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5),('LINEBELOW',(0,0),(-1,0),.5,navy),('LINEBELOW',(0,1),(-1,-1),.3,colors.HexColor('#d7e3e9'))]))
        if len(rows)<=7:
            if story and isinstance(story[-1],Paragraph):story[-1].keepWithNext=True
            story += [KeepTogether([t]),Spacer(1,10)]
        else:story += [t,Spacer(1,10)]
        continue
    m=re.match(r'^(\d+\.|-)\s+(.*)',s)
    if m:
        story.append(Paragraph(inline(m[2]),styles['list'],bulletText=m[1] if m[1]!='-' else '\u2022'));i+=1;continue
    para=[s];i+=1
    while i<len(lines) and lines[i].strip() and not re.match(r'^(#|\||```|\d+\. |\s*- )',lines[i]):para.append(lines[i].strip());i+=1
    story.append(Paragraph(inline(' '.join(para)),styles['body']))
doc=Manual(str(OUT),pagesize=A4,leftMargin=43,rightMargin=43,topMargin=45,bottomMargin=54,title='MW Editor Tools - User Manual '+VERSION,author='MW Editor Tools',pageCompression=1)
doc.multiBuild(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(OUT)
text='\n'.join(p.extract_text() for p in reader.pages)
assert 'Cities2' not in text
for heading in re.findall(r'^## (.+)$','\n'.join(lines),re.M):assert heading in text,heading
assert len(reader.pages)>1
print(f'Created {OUT}: {len(reader.pages)} pages, {OUT.stat().st_size} bytes')
