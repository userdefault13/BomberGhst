#!/usr/bin/env python3
"""Build the bomber sprite sheet from Aavegotchi Paaint's layered PNGs.

The Paaint repo exports each Aavegotchi part as its own 64x64 PNG (body per
view, hands per pose, eyes, mouth, take-damage variants). This composites the
frames BomberGhst needs, halves them to a 32px grid, and writes one sheet plus
a manifest into Assets/Resources.

    python3 tools/build_gotchi_sprites.py [--paaint PATH] [--out PATH]
                                         [--eye-shape 50] [--eye-color 50]

Re-run it whenever the source art changes; the output is committed so the game
builds without the Paaint repo present.
"""

import argparse
import json
import os
import re
import struct
import sys
import zlib

FRAME = 64          # source part size
CELL = FRAME // 2   # exported cell size

# Player slot -> collateral. Chosen so the four bombers read as clearly
# different colours on a dark playfield: pink, blue, green, orange.
COLLATERALS = ["mauni", "mayfi", "mausdt", "madai"]

# Column order in the sheet. Left facing is the mirror of right at runtime.
# eye_view picks the trait eye sprite: 0 front, 2 right, None for the back view.
FRAMES = [
    ("down_open",   ["body-front-0", "hands-frontdownopen-7"], 0, "mouth-neutral-21"),
    ("down_closed", ["body-front-0", "hands-frontdownclosed-6"], 0, "mouth-neutral-21"),
    ("up_open",     ["body-back-5", "hands-frontdownopen-7"], None, None),
    ("up_closed",   ["body-back-5", "hands-frontdownclosed-6"], None, None),
    ("side",        ["body-right-4", "hands-right-10"], 2, None),
    ("dead",        ["takedamage-body-front-30", "takedamage-hands-frontup-42"], 0,
                    "mouth-surprised-24"),
]

# Aavegotchi trait rarity bands, used to turn an EYS/EYC value into an asset.
# Eye shape selects the folder (its name carries the range), eye colour selects
# the file prefix inside it.
COLOR_BANDS = [
    (0, 4, "mythicallow"), (5, 9, "rarelow"), (10, 24, "uncommonlow"),
    (25, 74, "common"), (75, 89, "uncommonhigh"), (90, 94, "rarehigh"),
    (95, 99, "mythicalhigh"),
]


def color_band(value):
    for lo, hi, name in COLOR_BANDS:
        if lo <= value <= hi:
            return name
    raise ValueError(f"eye colour {value} is outside 0-99")


def find_collateral_dir(root, collateral):
    """Paaint mixes casing between collateral folders (maUNI vs mausdt)."""
    for entry in os.listdir(root):
        if entry.lower() == collateral.lower():
            return os.path.join(root, entry)
    return None


def find_eye_dir(paaint, collateral, eye_shape):
    """The folder whose Range_lo-hi in its name covers the eye shape value."""
    root = os.path.join(paaint, "PNGs", "Eyes")
    base = find_collateral_dir(root, collateral)
    if base is None:
        return None
    for group in sorted(os.listdir(base)):
        group_dir = os.path.join(base, group)
        if not os.path.isdir(group_dir):
            continue
        for entry in sorted(os.listdir(group_dir)):
            m = re.search(r"Range_(\d+)-(\d+)", entry)
            if m and int(m.group(1)) <= eye_shape <= int(m.group(2)):
                return os.path.join(group_dir, entry)
    return None


# ----------------------------------------------------------------- png io

def read_png(path):
    data = open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG")
    pos, idat, plte, trns = 8, b"", None, None
    w = h = ct = bd = None
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if kind == b"IHDR":
            w, h, bd, ct, _, _, interlace = struct.unpack(">IIBBBBB", body)
            if interlace:
                raise ValueError(f"{path}: interlaced PNGs are not supported")
        elif kind == b"IDAT":
            idat += body
        elif kind == b"PLTE":
            plte = body
        elif kind == b"tRNS":
            trns = body
        elif kind == b"IEND":
            break

    raw = zlib.decompress(idat)
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ct]
    bpp = max(1, channels * bd // 8)
    stride = (w * channels * bd + 7) // 8

    out, prev, i = bytearray(), bytearray(stride), 0
    for _ in range(h):
        filt = raw[i]; i += 1
        line = bytearray(raw[i:i + stride]); i += stride
        if filt == 1:
            for x in range(bpp, stride):
                line[x] = (line[x] + line[x - bpp]) & 255
        elif filt == 2:
            for x in range(stride):
                line[x] = (line[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(stride):
                left = line[x - bpp] if x >= bpp else 0
                line[x] = (line[x] + ((left + prev[x]) >> 1)) & 255
        elif filt == 4:
            for x in range(stride):
                a = line[x - bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x - bpp] if x >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pred = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pred) & 255
        out += line
        prev = line

    pixels = []
    for y in range(h):
        row = []
        for x in range(w):
            o = (y * w + x) * channels
            if ct == 6:
                row.append(tuple(out[o:o + 4]))
            elif ct == 2:
                row.append((out[o], out[o + 1], out[o + 2], 255))
            elif ct == 3:
                idx = out[o]
                r, g, b = plte[idx * 3:idx * 3 + 3]
                a = trns[idx] if trns and idx < len(trns) else 255
                row.append((r, g, b, a))
            elif ct == 4:
                row.append((out[o], out[o], out[o], out[o + 1]))
            else:
                row.append((out[o], out[o], out[o], 255))
        pixels.append(row)
    return w, h, pixels


def write_png(path, w, h, pixels):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        for x in range(w):
            raw += bytes(pixels[y][x])

    def chunk(kind, body):
        return (struct.pack(">I", len(body)) + kind + body +
                struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff))

    blob = b"\x89PNG\r\n\x1a\n"
    blob += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    blob += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    blob += chunk(b"IEND", b"")
    open(path, "wb").write(blob)


# ------------------------------------------------------------ compositing

def stack(out, path):
    if not os.path.exists(path):
        raise FileNotFoundError(path)
    w, h, px = read_png(path)
    if (w, h) != (FRAME, FRAME):
        raise ValueError(f"{path}: expected {FRAME}x{FRAME}, got {w}x{h}")
    for y in range(FRAME):
        for x in range(FRAME):
            c = px[y][x]
            if c[3] > 8:
                out[y][x] = c
    return out


def composite(base_dir, collateral, layers, eye_path=None, mouth=None):
    """Stack the part PNGs, first layer at the bottom."""
    out = [[(0, 0, 0, 0)] * FRAME for _ in range(FRAME)]
    for layer in layers:
        stack(out, os.path.join(base_dir, f"base-{collateral}-{layer}.png"))
    if eye_path:
        stack(out, eye_path)
    if mouth:
        stack(out, os.path.join(base_dir, f"base-{collateral}-{mouth}.png"))
    return out


def halve(src):
    """2x box downscale with a hard alpha cut, which keeps the art crisp."""
    out = [[(0, 0, 0, 0)] * CELL for _ in range(CELL)]
    for y in range(CELL):
        for x in range(CELL):
            acc, count, alpha = [0, 0, 0], 0, 0
            for dy in range(2):
                for dx in range(2):
                    c = src[y * 2 + dy][x * 2 + dx]
                    alpha += c[3]
                    if c[3] > 8:
                        acc[0] += c[0]; acc[1] += c[1]; acc[2] += c[2]
                        count += 1
            if count == 0 or alpha // 4 < 110:
                continue
            out[y][x] = (acc[0] // count, acc[1] // count, acc[2] // count, 255)
    return out


def bounds(cell):
    xs = [x for y in range(CELL) for x in range(CELL) if cell[y][x][3] > 8]
    ys = [y for y in range(CELL) for x in range(CELL) if cell[y][x][3] > 8]
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--paaint", default=os.path.expanduser("~/Dev/Aseprite-AavegotchiPaaint"))
    ap.add_argument("--out", default=os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources"))
    ap.add_argument("--eye-shape", type=int, default=50,
                    help="EYS trait, picks the eye shape band (default 50, common)")
    ap.add_argument("--eye-color", type=int, default=50,
                    help="EYC trait, picks the eye colour band (default 50, common)")
    args = ap.parse_args()

    band = color_band(args.eye_color)

    root = os.path.join(args.paaint, "PNGs", "Base")
    if not os.path.isdir(root):
        sys.exit(f"Aavegotchi Paaint art not found at {root}")

    sheet_w, sheet_h = CELL * len(FRAMES), CELL * len(COLLATERALS)
    sheet = [[(0, 0, 0, 0)] * sheet_w for _ in range(sheet_h)]
    manifest = {
        "cell": CELL,
        "eyeShape": args.eye_shape,
        "eyeColor": args.eye_color,
        "eyeBand": band,
        "frames": [f[0] for f in FRAMES],
        "players": [],
        "source": "Aseprite-AavegotchiPaaint/PNGs/Base",
    }

    for row, collateral in enumerate(COLLATERALS):
        base_dir = os.path.join(root, f"base-{collateral}")
        if not os.path.isdir(base_dir):
            sys.exit(f"missing collateral art: {base_dir}")

        eye_dir = find_eye_dir(args.paaint, collateral, args.eye_shape)
        if eye_dir is None:
            sys.exit(f"no eye art for {collateral} at shape {args.eye_shape}")

        accent = None
        for col, (name, layers, eye_view, mouth) in enumerate(FRAMES):
            eye_path = (os.path.join(eye_dir, f"eyes-{band}-eyes-{eye_view}.png")
                        if eye_view is not None else None)
            cell = halve(composite(base_dir, collateral, layers, eye_path, mouth))
            box = bounds(cell)
            if box is None:
                sys.exit(f"{collateral}/{name} composited to nothing")
            for y in range(CELL):
                for x in range(CELL):
                    sheet[row * CELL + y][col * CELL + x] = cell[y][x]
            if name == "down_open":
                counts = {}
                for y in range(CELL):
                    for x in range(CELL):
                        c = cell[y][x]
                        if c[3] > 8 and c[:3] != (255, 255, 255):
                            counts[c[:3]] = counts.get(c[:3], 0) + 1
                accent = max(counts, key=counts.get) if counts else (255, 255, 255)
                manifest["players"].append({
                    "slot": row,
                    "collateral": collateral,
                    "accent": "#%02x%02x%02x" % accent,
                    "bounds": {"x": box[0], "y": box[1], "w": box[2] - box[0] + 1, "h": box[3] - box[1] + 1},
                })
        print(f"  slot {row}: {collateral:8s} accent #%02x%02x%02x" % accent +
              f"  eyes {os.path.basename(eye_dir)}/{band}")

    out_dir = os.path.abspath(args.out)
    os.makedirs(out_dir, exist_ok=True)
    png_path = os.path.join(out_dir, "GotchiSprites.png")
    json_path = os.path.join(out_dir, "GotchiSprites.json")
    write_png(png_path, sheet_w, sheet_h, sheet)
    open(json_path, "w").write(json.dumps(manifest, indent=2) + "\n")
    print(f"wrote {png_path} ({sheet_w}x{sheet_h}) and {json_path}")


if __name__ == "__main__":
    main()
