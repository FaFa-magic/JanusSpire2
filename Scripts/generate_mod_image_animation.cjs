// Original art is read-only. Only the AI-generated feather sprites are animated.
// Requires Node.js, sharp and pngjs. Run from any directory.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('sharp');
const { PNG } = require('pngjs');
const root = path.resolve(__dirname, '..');
const source = path.join(root, 'JanusSpire2/mod_image.png');
const sheet = path.join(root, 'workshop/animation_assets/white_feathers.png');
const destination = path.join(root, 'JanusSpire2/mod_image_feathers');
const previewDir = path.join(root, 'temp/mod_image_animation');
const FRAMES = 300;
const DELAY = 50;
const FEATHERS = 18;
const TAU = 2 * Math.PI;
// Built-in imagegen prompt used for the feather sheet (not for the background):
const assetPrompt = 'Use case: stylized-concept. Asset type: sprite sheet for subtle falling-feather animation over an existing anime illustration. Generate exactly four separate small white down feathers on a genuinely transparent background, arranged in a precise 2 by 2 grid, each wholly inside its quadrant with wide empty padding, no overlap. Each feather has a slightly different organic silhouette: slim curved, softly rounded, lightly twisted, asymmetric fluffy. Delicate translucent milky-white barbs with very faint cool pale-blue shadows and a fine curved white shaft, light and airy, elegant hand-painted anime effect, clearly recognizable as feathers at 25-45 pixel display size. Full feathers, vertical or gently diagonal, crisp clean edges with a few soft wisps, simple forms without excessive tiny details. No scene, no background color, no checkerboard painted in, no text, no borders, no sparkles, no other objects. Square sprite sheet.';

function chunks(png) {
  const result = [];
  for (let p = 8; p < png.length;) {
    const length = png.readUInt32BE(p);
    result.push({ type: png.toString('ascii', p + 4, p + 8), data: png.subarray(p + 8, p + 8 + length) });
    p += length + 12;
  }
  return result;
}
const crcTable = Uint32Array.from({ length: 256 }, (_, n) => {
  for (let k = 0; k < 8; k++) n = n & 1 ? 0xedb88320 ^ (n >>> 1) : n >>> 1;
  return n >>> 0;
});
function chunk(type, data) {
  const out = Buffer.alloc(data.length + 12);
  out.writeUInt32BE(data.length);
  out.write(type, 4);
  data.copy(out, 8);
  let crc = 0xffffffff;
  for (let i = 4; i < out.length - 4; i++) crc = crcTable[(crc ^ out[i]) & 255] ^ (crc >>> 8);
  out.writeUInt32BE((crc ^ 0xffffffff) >>> 0, out.length - 4);
  return out;
}
function u16(n) { const b = Buffer.alloc(2); b.writeUInt16LE(n); return b; }

// Keep LZW codes at nine bits, resetting before the dictionary grows to ten bits.
// This also makes transparent delta frames small without palette changes/flicker.
function lzw(indices) {
  const bytes = [];
  let bits = 0, bitCount = 0;
  function emit(code) {
    bits |= code << bitCount;
    bitCount += 9;
    while (bitCount >= 8) { bytes.push(bits & 255); bits >>>= 8; bitCount -= 8; }
  }
  let dictionary = new Map(), next = 258, prefix = indices[0];
  emit(256);
  for (let i = 1; i < indices.length; i++) {
    const value = indices[i], key = prefix * 256 + value;
    const existing = dictionary.get(key);
    if (existing !== undefined) { prefix = existing; continue; }
    emit(prefix);
    if (next < 510) dictionary.set(key, next++);
    else { emit(256); dictionary = new Map(); next = 258; }
    prefix = value;
  }
  emit(prefix); emit(257);
  if (bitCount) bytes.push(bits & 255);
  const data = Buffer.from(bytes), blocks = [Buffer.from([8])];
  for (let i = 0; i < data.length; i += 255) {
    const part = data.subarray(i, i + 255);
    blocks.push(Buffer.from([part.length]), part);
  }
  blocks.push(Buffer.from([0]));
  return Buffer.concat(blocks);
}

async function main() {
  fs.mkdirSync(previewDir, { recursive: true });
  const originalHash = crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex');
  const { data: background, info } = await sharp(source).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width: w, height: h } = info;
  const count = w * h;
  const palettePng = await sharp(source).png({ palette: true, colours: 255, effort: 10, dither: 1 }).toBuffer();
  // Quantizers may round 255 colours to 256. Reserve index 255 explicitly.
  const palette = chunks(palettePng).find(c => c.type === 'PLTE').data.subarray(0, 255 * 3);
  const paletteRgb = new Map();
  for (let i = 0; i < palette.length / 3; i++) paletteRgb.set(palette.readUIntBE(i * 3, 3), i);
  const quantized = PNG.sync.read(palettePng).data;
  const baseIndices = Buffer.alloc(count);
  const nearestCache = new Map();
  function nearest(r, g, b) {
    const key = r * 65536 + g * 256 + b;
    if (nearestCache.has(key)) return nearestCache.get(key);
    let best = 0, error = Infinity;
    for (let i = 0; i < palette.length / 3; i++) {
      const d = 2 * (r - palette[i * 3]) ** 2 + 3 * (g - palette[i * 3 + 1]) ** 2 + (b - palette[i * 3 + 2]) ** 2;
      if (d < error) { best = i; error = d; }
    }
    nearestCache.set(key, best);
    return best;
  }
  for (let p = 0; p < count; p++) {
    const q = p * 4;
    baseIndices[p] = paletteRgb.get(quantized.readUIntBE(q, 3)) ?? nearest(quantized[q], quantized[q + 1], quantized[q + 2]);
  }
  const meta = await sharp(sheet).metadata();
  const sprites = [];
  const tile = Math.floor(meta.width / 2);
  for (let i = 0; i < 4; i++) {
    const quadrant = await sharp(sheet).extract({ left: (i % 2) * tile, top: Math.floor(i / 2) * tile, width: tile, height: tile }).png().toBuffer();
    sprites.push(await sharp(quadrant).trim({ threshold: 8 }).png().toBuffer());
  }
  let seed = 91721;
  const random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed / 4294967296; };
  const particles = Array.from({ length: FEATHERS }, (_, i) => ({
    sprite: i % 4, x: 0.03 + 0.94 * random(), phase: (i + random()) / FEATHERS,
    size: Math.round(51 + 33 * random()), opacity: 0.66 + 0.18 * random(),
    sway: 12 + 18 * random(), rotation: -30 + 60 * random(), wobble: random() * TAU
  }));
  const spriteCache = new Map();
  async function transformed(particle, angle) {
    angle = Math.round(angle / 3) * 3;
    const key = `${particle.sprite}/${particle.size}/${angle}`;
    if (!spriteCache.has(key)) spriteCache.set(key, await sharp(sprites[particle.sprite])
      .resize({ height: particle.size }).rotate(angle, { background: '#00000000' })
      .ensureAlpha().raw().toBuffer({ resolveWithObject: true }));
    return spriteCache.get(key);
  }
  const gifTable = Buffer.alloc(768);
  palette.copy(gifTable);
  const gif = [Buffer.from('GIF89a'), u16(w), u16(h), Buffer.from([0xf7, 0, 0]), gifTable,
    Buffer.from([0x21, 0xff, 0x0b]), Buffer.from('NETSCAPE2.0'), Buffer.from([3, 1, 0, 0, 0])];
  const apng = [Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])];
  let sequence = 0, firstFrame, firstIndices, previousIndices, firstChanged;
  let untouchedPixelChecks = 0;
  for (let f = 0; f <= FRAMES; f++) {
    let frame, current, changed;
    if (f === FRAMES) {
      frame = Buffer.from(firstFrame); current = Buffer.from(firstIndices); changed = new Set(firstChanged);
    } else {
      frame = Buffer.from(background); changed = new Set();
      for (const particle of particles) {
        const phase = (f / FRAMES + particle.phase) % 1;
        const wave = phase * TAU + particle.wobble;
        const sprite = await transformed(particle, particle.rotation + Math.sin(wave) * 23);
        const sw = sprite.info.width, sh = sprite.info.height;
        const left = Math.round(particle.x * w + particle.sway * Math.sin(wave) - sw / 2);
        const top = Math.round(-65 + phase * (h + 130) - sh / 2);
        // Small opacity modulation; no fade-to-black or whole-image transition.
        const opacity = particle.opacity * (0.84 + 0.16 * Math.cos(wave));
        for (let sy = 0; sy < sh; sy++) for (let sx = 0; sx < sw; sx++) {
          const x = left + sx, y = top + sy;
          if (x < 0 || y < 0 || x >= w || y >= h) continue;
          const s = (sy * sw + sx) * 4, alpha = sprite.data[s + 3] / 255 * opacity;
          if (alpha < 1 / 510) continue;
          const pixel = y * w + x, d = pixel * 4;
          for (let c = 0; c < 3; c++) frame[d + c] = Math.round(frame[d + c] * (1 - alpha) + sprite.data[s + c] * alpha);
          changed.add(pixel);
        }
      }
      current = Buffer.from(baseIndices);
      for (const p of changed) {
        const d = p * 4;
        if (frame[d] === background[d] && frame[d + 1] === background[d + 1] && frame[d + 2] === background[d + 2]) continue;
        // Retain the background's existing quantization error to avoid a flat halo
        // around the delicate feather edges. The palette never changes per frame.
        const q = baseIndices[p] * 3;
        const rgb = [0, 1, 2].map(c => Math.max(0, Math.min(255, palette[q + c] + frame[d + c] - background[d + c])));
        current[p] = nearest(...rgb);
      }
    }
    if (f === 0) {
      firstFrame = Buffer.from(frame); firstIndices = Buffer.from(current); firstChanged = new Set(changed);
    }
    // GIF delta frames update only changed pixels. Background pixels remain stable.
    let x0 = w, y0 = h, x1 = 0, y1 = 0;
    for (let p = 0; p < count; p++) if (!previousIndices || current[p] !== previousIndices[p]) {
      const x = p % w, y = Math.floor(p / w);
      x0 = Math.min(x0, x); x1 = Math.max(x1, x); y0 = Math.min(y0, y); y1 = Math.max(y1, y);
    }
    if (x0 > x1) x0 = y0 = x1 = y1 = 0;
    const rw = x1 - x0 + 1, rh = y1 - y0 + 1, indices = Buffer.alloc(rw * rh, 255);
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const p = y * w + x;
      if (!previousIndices || current[p] !== previousIndices[p]) indices[(y - y0) * rw + x - x0] = current[p];
    }
    // Split the endpoint duration over two identical frames to avoid a loop pause.
    const delay = f === 0 ? 2 : f === FRAMES ? 3 : DELAY / 10;
    gif.push(Buffer.from([0x21, 0xf9, 4, 5]), u16(delay), Buffer.from([255, 0, 0x2c]), u16(x0), u16(y0), u16(rw), u16(rh), Buffer.from([0]), lzw(indices));
    previousIndices = current;

    // APNG overlays restore the first frame's feather pixels, then draw the new
    // feathers. PREVIOUS disposal keeps a lossless persistent background.
    let pngData;
    if (f === 0) pngData = frame;
    else {
      pngData = Buffer.alloc(count * 4);
      const touched = new Set([...firstChanged, ...changed]);
      for (const p of touched) frame.copy(pngData, p * 4, p * 4, p * 4 + 4);
    }
    const encoded = PNG.sync.write({ width: w, height: h, data: pngData }, { colorType: 6 });
    const encodedChunks = chunks(encoded);
    if (f === 0) {
      apng.push(chunk('IHDR', encodedChunks.find(c => c.type === 'IHDR').data));
      const actl = Buffer.alloc(8); actl.writeUInt32BE(FRAMES + 1); apng.push(chunk('acTL', actl));
    }
    const control = Buffer.alloc(26);
    control.writeUInt32BE(sequence++, 0); control.writeUInt32BE(w, 4); control.writeUInt32BE(h, 8);
    control.writeUInt16BE(delay, 20); control.writeUInt16BE(100, 22);
    control[24] = f === 0 ? 0 : 2; control[25] = f === 0 ? 0 : 1;
    apng.push(chunk('fcTL', control));
    const compressed = Buffer.concat(encodedChunks.filter(c => c.type === 'IDAT').map(c => c.data));
    if (f === 0) apng.push(chunk('IDAT', compressed));
    else { const seq = Buffer.alloc(4); seq.writeUInt32BE(sequence++); apng.push(chunk('fdAT', Buffer.concat([seq, compressed]))); }
    if ([0, 75, 150, 225, FRAMES].includes(f)) {
      fs.writeFileSync(path.join(previewDir, `frame_${String(f).padStart(3, '0')}.png`), PNG.sync.write({ width: w, height: h, data: frame }));
      for (let p = 0; p < count; p++) if (!changed.has(p)) {
        for (let c = 0; c < 4; c++) if (frame[p * 4 + c] !== background[p * 4 + c]) throw new Error('Background changed outside feathers');
        untouchedPixelChecks++;
      }
    }
    if (f % 50 === 0) console.log(`Rendered ${f}/${FRAMES}`);
  }
  gif.push(Buffer.from([0x3b])); apng.push(chunk('IEND', Buffer.alloc(0)));
  fs.writeFileSync(destination + '.gif', Buffer.concat(gif));
  fs.writeFileSync(destination + '.apng', Buffer.concat(apng));
  if (crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex') !== originalHash) throw new Error('Source changed');
  const firstDecoded = await sharp(destination + '.gif', { page: 0 }).raw().toBuffer();
  const lastDecoded = await sharp(destination + '.gif', { page: FRAMES }).raw().toBuffer();
  if (!firstDecoded.equals(lastDecoded)) throw new Error('GIF endpoints differ');
  const report = { width: w, height: h, frames: FRAMES + 1, durationSeconds: FRAMES * DELAY / 1000,
    loop: 'infinite', firstAndLastGifFramesIdentical: true, originalSha256: originalHash,
    backgroundUnchangedOutsideFeathers: true, untouchedPixelChecks, featherCount: FEATHERS,
    gifBytes: fs.statSync(destination + '.gif').size, apngBytes: fs.statSync(destination + '.apng').size,
    assetGeneration: 'built-in imagegen', assetPrompt };
  fs.writeFileSync(path.join(previewDir, 'verification.json'), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
