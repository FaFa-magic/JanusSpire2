// Pixel-level registration regression: catches trimmed/untrimmed canvas drift
// and accidental reuse of an alpha-punched scenery behind a second character.
// Run with NODE_PATH pointing to sharp's node_modules directory.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../../JanusSpire2/images/ui/battle_rewards');
const layout = fs.readFileSync(path.join(root, 'background_layout.tres'), 'utf8');
async function run() {
  for (const name of ['yanusi', 'yanusi_3', 'yanusi_5']) {
    const match = layout.match(new RegExp(`metadata/${name} = Rect2\\(([^)]+)\\)`));
    assert(match, `Missing layout for ${name}`);
    const [offsetX, offsetY, width, height] = match[1].split(',').map(Number);
    const portrait = await sharp(path.join(root, name+'_painting.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    const background = await sharp(path.join(root, name+'_background.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    assert.equal(background.info.width, width);
    assert.equal(background.info.height, height);
    let aligned = 0;
    for (let y=0; y<portrait.info.height-3; y+=9) for (let x=0; x<portrait.info.width-3; x+=9) {
      const bx=x-offsetX, by=y-offsetY;
      if(bx<0 || by<0 || bx+3>=width || by+3>=height) continue;
      let identical=true, variation=0;
      for(let yy=0; yy<3 && identical; yy++) for(let xx=0; xx<3 && identical; xx++) {
        const p=((y+yy)*portrait.info.width+x+xx)*4;
        const b=((by+yy)*width+bx+xx)*4;
        if(portrait.data[p+3]!==255 || background.data[b+3]!==255) { identical=false; break; }
        for(let c=0; c<3; c++) {
          if(portrait.data[p+c]!==background.data[b+c]) identical=false;
          variation+=Math.abs(portrait.data[p+c]-portrait.data[(y*portrait.info.width+x)*4+c]);
        }
      }
      if(identical && variation>100) aligned++;
    }
    assert(aligned>400, `${name}: only ${aligned} shared detail patches align; check original PNG canvas and complete artwork`);
    console.log(`${name}: ${aligned} identical 3x3 detail patches aligned`);
  }
  // The changing-room reference includes the walls, upper shelves, chair,
  // cabinet and wooden floor. *_n's transparent mirror-only mesh is incomplete.
  const room = await sharp(path.join(root,'yanusi_4_background.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(room.info.width,1911);
  assert.equal(room.info.height,2048);
  for(const [label,x,y] of [['shelves',600,250],['wall',1100,700],['chair',1100,1550],['cabinet',1750,1550],['floor',1000,1920]])
    assert(room.data[(y*room.info.width+x)*4+3]>245, `Missing changing-room ${label}`);
  const roomFrame = layout.match(/metadata\/yanusi_4 = Rect2\(([^)]+)\)/)[1].split(',').map(Number);
  const characterToRoom = room.info.width/roomFrame[2];
  assert(Math.abs(characterToRoom-0.6504347826)<0.0001,'Changing-room character/room scale no longer matches reference');
  assert(Math.abs(-roomFrame[0]*characterToRoom-7.2546583851)<0.01,'Changing-room horizontal registration drifted');
  assert(Math.abs(-roomFrame[1]*characterToRoom-293.1677018634)<0.01,'Changing-room vertical registration drifted');
  console.log('yanusi_4: complete room landmarks and reference registration verified');
  // Water skin uses an untrimmed 2047x1403 character canvas. Its alpha trim
  // begins at (5,5); using that origin leaves a 2-3 screen-pixel shadow ghost.
  const waterFrame = layout.match(/metadata\/yanusi_7 = Rect2\(([^)]+)\)/)[1].split(',').map(Number);
  const expectedWaterFrame = [-736.0956,3.4162,3658.2129,1651.2891];
  waterFrame.forEach((value,index) => assert(Math.abs(value-expectedWaterFrame[index])<0.0001,'Water skin background origin/scale drifted from the original portrait canvas'));
  const waterPortrait = await sharp(path.join(root,'yanusi_7_painting.png')).metadata();
  assert.equal(waterPortrait.width,2047);
  assert.equal(waterPortrait.height,1403);
  console.log('yanusi_7: untrimmed portrait origin / contact-shadow registration verified');
  console.log('ART_REGISTRATION_TESTS passed');
}
run().catch(error => { console.error(error); process.exitCode=1; });
