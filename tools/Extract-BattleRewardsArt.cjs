// Reassemble AssetStudio's packed painting rectangles using their original OBJ UVs.
// This preserves the original art (including the mesh's one-pixel edge trims),
// rather than redrawing it. Requires sharp (NODE_PATH).
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const sourceRoot = process.argv[2] || 'D:/GitClone/AzurLane/data/painting';
const outputRoot = process.argv[3] || path.resolve(__dirname, '../JanusSpire2/images/ui/battle_rewards');
// Split-painting skins keep the character in the *_rw bundle, separately from
// the mirror/table/bed. The regular *_n bundle alone can be background-only.
const sources = ['yanusi_n', 'yanusi_3_n', 'yanusi_4_n_rw', 'yanusi_5_n', 'yanusi_6_rw', 'yanusi_7_n_rw', 'yanusi_h_n_rw'];
const targets = ['yanusi', 'yanusi_3', 'yanusi_4', 'yanusi_5', 'yanusi_6', 'yanusi_7', 'yanusi_h'];
const median = values => values.sort((a, b) => a - b)[Math.floor(values.length / 2)];
async function restore(name, target, save = true) {
  const prefix = path.join(sourceRoot, name + '_tex', name);
  const text = fs.readFileSync(prefix + '-mesh.obj', 'utf8');
  const vertices = [], uvs = [];
  for (const line of text.split(/\r?\n/)) {
    const p = line.split(/\s+/);
    if (p[0] === 'v') vertices.push({ x: -Number(p[1]), y: -Number(p[2]) });
    if (p[0] === 'vt') uvs.push({ u: Number(p[1]), v: Number(p[2]) });
  }
  if (!vertices.length || vertices.length % 4 || vertices.length !== uvs.length) throw Error('Unexpected mesh: ' + name);
  const quads = [];
  for (let i = 0; i < vertices.length; i += 4) {
    const v = vertices.slice(i, i + 4), uv = uvs.slice(i, i + 4);
    const x = Math.min(...v.map(p => p.x)), y = Math.min(...v.map(p => p.y));
    const w = Math.max(...v.map(p => p.x)) - x, h = Math.max(...v.map(p => p.y)) - y;
    const u = Math.min(...uv.map(p => p.u)), loV = Math.min(...uv.map(p => p.v));
    const du = Math.max(...uv.map(p => p.u)) - u, dv = Math.max(...uv.map(p => p.v)) - loV;
    if (!w || !h || !du || !dv) throw Error('Degenerate quad: ' + name);
    for (const p of v) if (![x, x + w].includes(p.x) || ![y, y + h].includes(p.y)) throw Error('Nonrectangular mesh: ' + name);
    const left = v.findIndex(p => p.x === x), top = v.findIndex(p => p.y === y);
    quads.push({ x, y, w, h, u, v: loV, du, dv, flop: uv[left].u > u + du / 2, flip: uv[top].v < loV + dv / 2 });
  }
  // Some exported PNGs omit right-side padding; infer the original UV extent.
  const texW = Math.round(median(quads.map(q => q.w / q.du)));
  const texH = Math.round(median(quads.map(q => q.h / q.dv)));
  const metadata = await sharp(prefix + '.png').metadata();
  const minX = Math.min(...quads.map(q => q.x)), minY = Math.min(...quads.map(q => q.y));
  const width = Math.round(Math.max(...quads.map(q => q.x + q.w)) - minX);
  const height = Math.round(Math.max(...quads.map(q => q.y + q.h)) - minY);
  const parts = [];
  for (const q of quads) {
    if (Math.abs(q.du * texW - q.w) > 1.02 || Math.abs(q.dv * texH - q.h) > 1.02) throw Error('Scaled or rotated UVs: ' + name);
    const crop = { left: Math.round(q.u * texW), top: Math.round((1 - q.v - q.dv) * texH), width: Math.round(q.du * texW), height: Math.round(q.dv * texH) };
    if (crop.left < 0 || crop.top < 0 || crop.left + crop.width > metadata.width || crop.top + crop.height > metadata.height) throw Error('UV outside texture: ' + name + JSON.stringify(crop));
    const tile = await sharp(prefix + '.png').extract(crop).png().toBuffer();
    let image = sharp(tile);
    if (q.flop) image = image.flop();
    if (q.flip) image = image.flip();
    if (crop.width !== Math.round(q.w) || crop.height !== Math.round(q.h)) image = image.resize(Math.round(q.w), Math.round(q.h), { kernel: 'nearest' });
    parts.push({ input: await image.png().toBuffer(), left: Math.round(q.x - minX), top: Math.round(q.y - minY) });
  }
  const assembled = await sharp({ create: { width, height, channels: 4, background: '#00000000' } }).composite(parts).png({ compressionLevel: 9 }).toBuffer();
  const trimmed = await sharp(assembled).trim().png({ compressionLevel: 9 }).toBuffer({ resolveWithObject: true });
  // Shipped portraits retain their original mesh canvas/transparent margins.
  // Keep that reference stable; trimmed scenery has a different origin/size.
  if (save) fs.writeFileSync(path.join(outputRoot, target + '_painting.png'), assembled);
  console.log(JSON.stringify({ source: name, target, quads: quads.length, uvSize: [texW, texH], output: [width, height] }));
  return { data: trimmed.data, x: minX - (trimmed.info.trimOffsetLeft || 0), y: minY - (trimmed.info.trimOffsetTop || 0), width: trimmed.info.width, height: trimmed.info.height,
    canvas: { data: assembled, x: minX, y: minY, width, height } };
}

async function backgrounds() {
  const layout = {};
  const only = process.argv.find(argument => argument.startsWith('--only='))?.slice(7).split(',');
  if (only?.some(name => !targets.includes(name))) throw Error('Unknown --only target');
  const layoutPath = path.join(outputRoot, 'background_layout.tres');
  if (only) {
    // A targeted refresh must not overwrite other skins or their alignment.
    const previous = fs.readFileSync(layoutPath, 'utf8');
    for (const match of previous.matchAll(/metadata\/(\w+) = Rect2\(([^)]+)\)/g))
      layout[match[1]] = match[2].split(',').map(Number);
  }
  // Original RectTransform sizes/anchors/pivots from the painting prefabs.
  // The split meshes use a 2048-wide UV coordinate space, not the cropped PNG
  // dimensions. Preserve that mapping instead of independently fitting layers.
  const split = {
    yanusi_4: { root: [2956,3138], position: [-672,-124], raw: [1607,2048], layer: [0,0] },
    yanusi_6: { root: [6440,3832], position: [-15.700012,557.400024], raw: [1894,2048], layer: [0,0], meshWidth: 1894 },
    yanusi_7: { root: [3660,2134], position: [-89,161.800003], raw: [2048,1466], layer: [0,0] },
    yanusi_h: { root: [2730,1584], position: [-403,-2.94], raw: [2048,1242], layer: [404,386.799988] }
  };
  for (let i = 0; i < targets.length; i++) {
    if (only && !only.includes(targets[i])) continue;
    const target = targets[i], foreground = (await restore(sources[i], target, false)).canvas;
    let data, rect;
    if (!split[target]) {
      const full = await restore(target, target, false);
      // The complete static art already contains the identical character (pixel
      // registration verified against *_n). Draw it once as a single artwork.
      // Cutting out antialiased foreground alpha and overlaying it again loses
      // coverage and leaves dark seams/ghosts at transparent edges.
      data = full.data;
      rect = [full.x-foreground.x, full.y-foreground.y, full.width, full.height];
    } else {
      const frame = split[target];
      data = fs.readFileSync(path.join(sourceRoot, target+'_tex', target+'.png'));
      const meshScale = frame.raw[0] / (frame.meshWidth || 2048);
      // Unity: child anchors refer to the parent's center; mesh origin is its
      // bottom-left. Convert to top-down pixels relative to the trimmed portrait.
      const pivot = [0.49067598581314087, 0.66650390625];
      const childLeft = frame.layer[0] + (0.5-pivot[0])*frame.root[0] + frame.position[0] - pivot[0]*frame.raw[0];
      const childBottom = frame.layer[1] + (0.5-pivot[1])*frame.root[1] + frame.position[1] - pivot[1]*frame.raw[1];
      rect = [(-pivot[0]*frame.root[0]-childLeft)/meshScale-foreground.x,
        (-(1-pivot[1])*frame.root[1]+childBottom)/meshScale-foreground.y,
        frame.root[0]/meshScale, frame.root[1]/meshScale];
      if (target === 'yanusi_4') {
        // The supplied static illustration includes the COMPLETE room from
        // yanusi_4_tex, not only the mirror/props retained in yanusi_4_n_tex.
        // Register the room to the static *_n_rw character using the reference
        // screenshot's shelves, chair, floor and character landmarks. The
        // prefab root dimensions are not this static artwork's canvas mapping.
        const room = await sharp(data).metadata();
        const characterInRoom = { scale: 0.6504347826, x: 7.2546583851, y: 293.1677018634 };
        rect = [-characterInRoom.x/characterInRoom.scale, -characterInRoom.y/characterInRoom.scale,
          room.width/characterInRoom.scale, room.height/characterInRoom.scale];
      } else if (target === 'yanusi_7') {
        // This skin continues to use its separate transparent static scenery.
        // foreground must be the original canvas, not the trim() result:
        // *_n_rw trims 5 pixels off both left/top, visibly shifting the shadow.
        const scenery = await restore(target+'_n', target, false);
        const meshToForeground = rect[2] / 2048;
        data = scenery.data;
        rect = [rect[0] + scenery.x*meshToForeground,
          rect[1] + rect[3] + scenery.y*meshToForeground,
          scenery.width*meshToForeground, scenery.height*meshToForeground];
      }
    }
    await sharp(data).png({ compressionLevel: 9 }).toFile(path.join(outputRoot, target+'_background.png'));
    layout[target] = rect.map(value => Math.round(value*10000)/10000);
  }
  fs.writeFileSync(layoutPath,
    '[gd_resource type="Resource" format=3]\n\n[resource]\n'+Object.entries(layout)
      .map(([name, rect]) => `metadata/${name} = Rect2(${rect.join(', ')})`).join('\n')+'\n');
}
module.exports = { restore };
if (require.main === module) (async () => {
  fs.mkdirSync(outputRoot, { recursive: true });
  if (process.argv.includes('--backgrounds')) await backgrounds();
  else for (let i = 0; i < sources.length; i++) await restore(sources[i], targets[i]);
})().catch(error => { console.error(error); process.exitCode = 1; });
