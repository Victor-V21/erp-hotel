#!/usr/bin/env python3
"""Construye los cuatro tutoriales por rol siguiendo la skill ERP Video Tutorials."""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT = Path(__file__).resolve().parents[1]
SOURCE = Path(__file__).resolve().parent
ROLE_DEFINITIONS = SOURCE / "roles.json"
RECORDER = SOURCE / "record_scene.py"
SKILL = Path("/home/vm/.codex/skills/erp-video-tutorials")
NARRATOR = SKILL / "scripts/generate_narration.py"
ASSEMBLER = SKILL / "scripts/assemble_video.py"
CACHE = Path.home() / ".cache/erp-video-tutorials"
CREDS = CACHE / "credentials.json"
AUTH = CACHE / "auth"
WIDTH = 1920
HEIGHT = 1080
VOICE = "es-MX-JorgeNeural"
RATE = "-6%"


def run(command: list[str], *, capture: bool = False) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, check=True, text=True, capture_output=capture)


def duration(path: Path) -> float:
    result = subprocess.check_output(
        ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)],
        text=True,
    )
    return float(result.strip())


def estimate_step_seconds(scene: dict, credential: dict) -> float:
    total = 0.0
    for step in scene["steps"]:
        action = step["action"]
        if action == "goto":
            total += 2.0
        elif action == "wait":
            total += float(step.get("seconds", 1))
        elif action == "click":
            total += (step.get("glide_ms", 650) + step.get("settle_ms", 900) + 120) / 1000
        elif action == "hover":
            total += (step.get("glide_ms", 650) + step.get("hold_ms", 1100)) / 1000
        elif action == "type":
            value = step.get("text", credential.get(step.get("value_key", ""), ""))
            total += len(value) * step.get("char_delay_ms", 55) / 1000
        elif action == "scroll":
            total += step.get("settle_ms", 700) / 1000
        if step.get("hold_ms") and action != "hover":
            total += step["hold_ms"] / 1000
    return total


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf"
    return ImageFont.truetype(f"/usr/share/fonts/truetype/dejavu/{name}", size)


def centered_text(draw: ImageDraw.ImageDraw, text: str, y: int, text_font: ImageFont.FreeTypeFont, fill: str) -> None:
    box = draw.textbbox((0, 0), text, font=text_font)
    x = (WIDTH - (box[2] - box[0])) // 2
    draw.text((x, y), text, font=text_font, fill=fill)


def make_card(path: Path, role: str, kind: str) -> None:
    image = Image.new("RGB", (WIDTH, HEIGHT), "#191a1d")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, WIDTH, 18), fill="#c69c4b")
    draw.rectangle((0, HEIGHT - 18, WIDTH, HEIGHT), fill="#c69c4b")

    center_x = WIDTH // 2
    diamond = [(center_x, 175), (center_x + 58, 233), (center_x, 291), (center_x - 58, 233)]
    draw.polygon(diamond, outline="#c69c4b", width=6)
    centered_text(draw, "H", 190, font(62, bold=True), "#c69c4b")

    if kind == "intro":
        centered_text(draw, "HOTEL MAYA CENTRAL", 345, font(30, bold=True), "#c69c4b")
        centered_text(draw, f"Tutorial del rol {role}", 425, font(68, bold=True), "#ffffff")
        centered_text(draw, "Sistema de Gestión Hotelera y Facturación SAR", 535, font(30), "#d8d8d8")
        centered_text(draw, "Capacitación operativa", 710, font(26), "#a9aaad")
    else:
        centered_text(draw, "Fin del tutorial", 390, font(64, bold=True), "#ffffff")
        centered_text(draw, f"Rol {role}", 500, font(36, bold=True), "#c69c4b")
        centered_text(draw, "Cierre siempre su sesión al terminar el turno", 645, font(28), "#d8d8d8")
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def card_video(image: Path, video: Path, seconds: float) -> None:
    run([
        "ffmpeg", "-y", "-loglevel", "error",
        "-loop", "1", "-i", str(image),
        "-f", "lavfi", "-i", "anullsrc=r=24000:cl=stereo",
        "-t", str(seconds), "-r", "25",
        "-vf", f"fade=t=in:st=0:d=0.45,fade=t=out:st={seconds - 0.45}:d=0.45",
        "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
        "-c:a", "aac", "-ar", "24000", "-shortest", str(video),
    ])


def split_scenes(role_dir: Path, role_data: dict) -> list[Path]:
    scene_dir = role_dir / "escenas"
    scene_dir.mkdir(parents=True, exist_ok=True)
    (role_dir / "escenas.json").write_text(json.dumps(role_data["scenes"], ensure_ascii=False, indent=2), encoding="utf-8")
    paths = []
    for scene in role_data["scenes"]:
        path = scene_dir / f"{scene['id']}.json"
        path.write_text(json.dumps(scene, ensure_ascii=False, indent=2), encoding="utf-8")
        paths.append(path)
    return paths


def build_role(role_key: str, role_data: dict, credential: dict, *, rebuild: bool) -> dict:
    print(f"\n=== {role_data['title']} ===", flush=True)
    role_dir = PROJECT / role_key
    audio_dir = role_dir / "audio"
    video_dir = role_dir / "video"
    capture_dir = role_dir / "capturas"
    runtime_dir = role_dir / "runtime"
    asset_dir = role_dir / "assets"
    build_dir = role_dir / "build"
    for directory in [audio_dir, video_dir, capture_dir, runtime_dir, asset_dir, build_dir]:
        directory.mkdir(parents=True, exist_ok=True)

    scene_sources = split_scenes(role_dir, role_data)
    needs_recording = rebuild or any(
        not (video_dir / f"{scene['id']}.webm").exists()
        or not (capture_dir / f"{scene['id']}.png").exists()
        for scene in role_data["scenes"]
    )

    intro_png = asset_dir / "intro.png"
    outro_png = asset_dir / "outro.png"
    intro_mp4 = asset_dir / "intro.mp4"
    outro_mp4 = asset_dir / "outro.mp4"
    make_card(intro_png, role_data["role"], "intro")
    make_card(outro_png, role_data["role"], "outro")
    card_video(intro_png, intro_mp4, 3.8)
    card_video(outro_png, outro_mp4, 3.8)

    state_path = AUTH / f"{role_key}-pipeline.json"
    state_path.unlink(missing_ok=True)
    AUTH.mkdir(parents=True, exist_ok=True)
    manifest_scenes = []
    recording_log = []

    for index, source_path in enumerate(scene_sources):
        scene = json.loads(source_path.read_text(encoding="utf-8"))
        scene_id = scene["id"]
        audio = audio_dir / f"{scene_id}.mp3"
        srt = audio_dir / f"{scene_id}.srt"
        video = video_dir / f"{scene_id}.webm"
        screenshot = capture_dir / f"{scene_id}.png"
        runtime_scene = runtime_dir / f"{scene_id}.json"

        if rebuild or not audio.exists() or not srt.exists() or srt.stat().st_size == 0:
            run([
                sys.executable, str(NARRATOR), "--text", scene["narracion"],
                "--out", str(audio), "--srt", str(srt), "--engine", "edge",
                "--voice", VOICE, f"--rate={RATE}",
            ])
        audio_seconds = duration(audio)
        estimated = estimate_step_seconds(scene, credential)
        paced_scene = json.loads(json.dumps(scene))
        paced_scene["steps"].append({"action": "wait", "seconds": round(max(0.8, audio_seconds + 0.8 - estimated), 2)})
        runtime_scene.write_text(json.dumps(paced_scene, ensure_ascii=False, indent=2), encoding="utf-8")

        command = [
            sys.executable, str(RECORDER), "--scene", str(runtime_scene),
            "--out", str(video), "--credentials", str(CREDS), "--role-key", role_key,
            "--state-out", str(state_path), "--screenshot", str(screenshot),
            "--width", str(WIDTH), "--height", str(HEIGHT),
        ]
        if index > 0:
            command += ["--storage-state", str(state_path)]
        if not scene.get("allows_login_redirect", False):
            command.append("--reject-login-redirect")
        if needs_recording:
            result = run(command, capture=True)
            payload = json.loads(result.stdout.strip().splitlines()[-1])
        else:
            payload = {
                "scene": scene_id,
                "video": str(video),
                "final_url": "conservada de la grabación anterior",
                "errors": [],
            }
        payload["audio_seconds"] = round(audio_seconds, 3)
        recording_log.append(payload)
        print(f"  {scene_id}: audio {audio_seconds:.1f}s, grabación lista", flush=True)
        manifest_scenes.append({"video": str(video), "audio": str(audio), "srt": str(srt)})

    final_video = role_dir / f"hotel_erp_tutorial_{role_key}_es.mp4"
    manifest = {
        "workdir": str(build_dir),
        "scenes": manifest_scenes,
        "intro": str(intro_mp4),
        "outro": str(outro_mp4),
        "out": str(final_video),
    }
    manifest_path = role_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    run([sys.executable, str(ASSEMBLER), "--manifest", str(manifest_path)])
    (role_dir / "registro-grabacion.json").write_text(json.dumps(recording_log, ensure_ascii=False, indent=2), encoding="utf-8")
    state_path.unlink(missing_ok=True)

    return {
        "role": role_data["role"],
        "file": str(final_video),
        "duration_seconds": round(duration(final_video), 3),
        "scenes": len(scene_sources),
        "recording_console_errors": sum(len(item["errors"]) for item in recording_log),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--role", action="append", choices=["administrador", "recepcion", "caja", "contador"])
    parser.add_argument("--rebuild", action="store_true")
    args = parser.parse_args()

    if not CREDS.exists():
        raise SystemExit(f"Falta el archivo temporal de credenciales: {CREDS}")
    roles = json.loads(ROLE_DEFINITIONS.read_text(encoding="utf-8"))
    credentials = json.loads(CREDS.read_text(encoding="utf-8"))
    selected = args.role or list(roles)
    reports = [build_role(key, roles[key], credentials[key], rebuild=args.rebuild) for key in selected]
    report_path = PROJECT / "control-calidad-construccion.json"
    report_path.write_text(
        json.dumps({"generated_at": datetime.now(timezone.utc).isoformat(), "videos": reports}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"\nReporte: {report_path}", flush=True)


if __name__ == "__main__":
    main()
